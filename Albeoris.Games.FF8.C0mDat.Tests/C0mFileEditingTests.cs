using Albeoris.Games.FF8.C0mDat.Abstractions;
using Xunit;

namespace Albeoris.Games.FF8.C0mDat.Tests;

/// <summary>Verifies that individual text edits survive relocation without changing other data.</summary>
public class C0mFileEditingTests
{
    [Theory]
    [MemberData(nameof(C0mSample.All), MemberType = typeof(C0mSample))]
    public void Write_ChangesOnlyTheMonsterName_WhenNameIsEdited(C0mSample sample)
    {
        C0mFile file = sample.Read();
        String[] originalBattleTexts = [.. file.BattleScript.Texts.Select(text => text.Value)];
        String replacement = originalBattleTexts[^1];

        file.Information.MonsterName.Value = replacement;
        Byte[] written = file.Write();
        C0mFile reloaded = C0mFile.Read(written, sample.NewEncoding());

        Assert.Equal(replacement, reloaded.Information.MonsterName.Value);
        Assert.Equal(originalBattleTexts, reloaded.BattleScript.Texts.Select(text => text.Value));
        Assert.Equal(sample.Content.Length, written.Length);

        Int32 nameStart = C0mNativeLayout.GetSectionStart(sample.Content, 7);
        Assert.Equal(sample.Content[..nameStart], written[..nameStart]);
        Assert.Equal(sample.Content[(nameStart + 24)..], written[(nameStart + 24)..]);
    }

    [Theory]
    [MemberData(nameof(C0mSample.AllBattleTexts), MemberType = typeof(C0mSample))]
    public void Write_PreservesAnIndividualBattleTextEdit(C0mSample sample, Int32 textIndex)
    {
        C0mFile file = sample.Read();
        String originalName = file.Information.MonsterName.Value;
        String[] originalTexts = [.. file.BattleScript.Texts.Select(text => text.Value)];
        String replacement = String.Concat(Enumerable.Repeat(originalName, textIndex + 3));

        file.BattleScript.Texts[textIndex].Value = replacement;
        Byte[] written = file.Write();
        C0mFile reloaded = C0mFile.Read(written, sample.NewEncoding());

        Assert.Equal(originalName, reloaded.Information.MonsterName.Value);
        Assert.Equal(replacement, reloaded.BattleScript.Texts[textIndex].Value);
        for (Int32 index = 0; index < originalTexts.Length; index++)
        {
            if (index != textIndex)
            {
                Assert.Equal(originalTexts[index], reloaded.BattleScript.Texts[index].Value);
            }
        }

        AssertPreservedNonTextData(file, reloaded);
        C0mNativeLayout.AssertMatchesModel(written, file);
    }

    [Theory]
    [MemberData(nameof(C0mSample.All), MemberType = typeof(C0mSample))]
    public void Write_RelocatesFollowingSections_WhenBattleTextGrows(C0mSample sample)
    {
        C0mFile file = sample.Read();
        Int32 originalSection9Start = C0mNativeLayout.GetSectionStart(sample.Content, 9);
        Byte[] originalSection9 = C0mNativeLayout.GetSection(sample.Content, 9);
        file.BattleScript.Texts[0].Value = String.Concat(Enumerable.Repeat(file.Information.MonsterName.Value, 40));

        Byte[] written = file.Write();
        Int32 writtenSection9Start = C0mNativeLayout.GetSectionStart(written, 9);
        C0mFile reloaded = C0mFile.Read(written, sample.NewEncoding());

        Assert.True(writtenSection9Start > originalSection9Start);
        Assert.Equal(originalSection9, C0mNativeLayout.GetSection(written, 9));
        Assert.Equal(file.BattleScript.Texts[0].Value, reloaded.BattleScript.Texts[0].Value);
        C0mNativeLayout.AssertMatchesModel(written, file);
    }

    [Theory]
    [MemberData(nameof(C0mSample.All), MemberType = typeof(C0mSample))]
    public void Write_RecalculatesTextTable_WhenTextIsAdded(C0mSample sample)
    {
        C0mFile file = sample.Read();
        String added = file.Information.MonsterName.Value;
        file.BattleScript.Texts.Add(new C0mText(added));

        Byte[] written = file.Write();
        C0mFile reloaded = C0mFile.Read(written, sample.NewEncoding());

        Assert.Equal(3, reloaded.BattleScript.Texts.Count);
        Assert.Equal(added, reloaded.BattleScript.Texts[^1].Value);
        C0mNativeLayout.AssertMatchesModel(written, file);
    }

    private static void AssertPreservedNonTextData(C0mFile expected, C0mFile actual)
    {
        Assert.Equal(expected.Information.StatData, actual.Information.StatData);

        IReadOnlyList<Byte[]> expectedScripts = expected.BattleScript.AiScripts.InFileOrder;
        IReadOnlyList<Byte[]> actualScripts = actual.BattleScript.AiScripts.InFileOrder;
        Assert.Equal(expectedScripts.Count, actualScripts.Count);
        for (Int32 index = 0; index < expectedScripts.Count; index++)
        {
            Assert.Equal(expectedScripts[index], actualScripts[index]);
        }

        C0mOpaqueSection[] expectedOpaque = expected.Sections.OfType<C0mOpaqueSection>().ToArray();
        C0mOpaqueSection[] actualOpaque = actual.Sections.OfType<C0mOpaqueSection>().ToArray();
        Assert.Equal(expectedOpaque.Select(section => section.Kind), actualOpaque.Select(section => section.Kind));
        for (Int32 index = 0; index < expectedOpaque.Length; index++)
        {
            Assert.Equal(expectedOpaque[index].Content, actualOpaque[index].Content);
        }
    }
}
