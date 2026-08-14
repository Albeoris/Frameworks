using System.Reflection;
using Albeoris.Games.FF8.C0mDat.Abstractions;
using Xunit;

namespace Albeoris.Games.FF8.C0mDat.Tests;

/// <summary>Verifies lossless parsing and every level of the native offset hierarchy.</summary>
public class C0mFileRoundTripTests
{
    [Theory]
    [MemberData(nameof(C0mSample.All), MemberType = typeof(C0mSample))]
    public void Write_ReproducesSourceFileByteForByte(C0mSample sample)
    {
        C0mFile file = sample.Read();

        Byte[] written = file.Write();

        Assert.Equal(sample.Content, written);
        C0mNativeLayout.AssertMatchesModel(written, file);
    }

    [Theory]
    [MemberData(nameof(C0mSample.All), MemberType = typeof(C0mSample))]
    public void Write_IsIdempotentAcrossRepeatedCycles(C0mSample sample)
    {
        Byte[] first = sample.Read().Write();

        Byte[] second = C0mFile.Read(first, sample.NewEncoding()).Write();

        Assert.Equal(first, second);
    }

    [Theory]
    [MemberData(nameof(C0mSample.All), MemberType = typeof(C0mSample))]
    public void Read_ResolvesEverySectionWithoutExposingStoredOffsets(C0mSample sample)
    {
        C0mFile file = sample.Read();

        Assert.Equal(11, file.Sections.Count);
        Assert.Equal(Enumerable.Range(1, 11), file.Sections.Select(section => section.Index));
        Assert.NotEmpty(file.Information.MonsterName.Value);
        Assert.Equal(2, file.BattleScript.Texts.Count);

        Type[] modelTypes = typeof(C0mFile).Assembly.GetExportedTypes()
            .Concat(typeof(IC0mSection).Assembly.GetExportedTypes())
            .ToArray();
        const BindingFlags PublicMembers = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
        Assert.DoesNotContain(
            modelTypes.SelectMany(type => type
                .GetProperties(PublicMembers)
                .Cast<MemberInfo>()
                .Concat(type.GetFields(PublicMembers))),
            member => member.Name.Contains("Offset", StringComparison.Ordinal));
    }
}
