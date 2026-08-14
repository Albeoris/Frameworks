using System.Buffers.Binary;
using Albeoris.Games.FF8.C0mDat.Abstractions;
using Xunit;

namespace Albeoris.Games.FF8.C0mDat.Tests;

/// <summary>Assertions over stored offsets, kept outside the public offset-free model.</summary>
internal static class C0mNativeLayout
{
    private const Int32 SectionCount = 11;
    private const Int32 HeaderSize = 52;
    private const Int32 BattleSectionIndex = 8;

    public static void AssertMatchesModel(Byte[] content, C0mFile file)
    {
        Int32[] starts = ReadSectionStarts(content);

        Assert.Equal((UInt32)SectionCount, BinaryPrimitives.ReadUInt32LittleEndian(content));
        Assert.Equal(HeaderSize, starts[0]);
        Assert.Equal(content.Length, ReadUInt32(content, 48));
        Assert.Equal(starts.Order(), starts);

        C0mBattleScriptSection battle = file.BattleScript;
        Int32 battleStart = starts[BattleSectionIndex - 1];
        Int32 battleEnd = starts[BattleSectionIndex];
        Int32 aiStart = ReadUInt32(content, battleStart + 4);
        Int32 textOffsetsStart = ReadUInt32(content, battleStart + 8);
        Int32 textStart = ReadUInt32(content, battleStart + 12);

        Assert.Equal(3, ReadUInt32(content, battleStart));
        Assert.Equal(16, aiStart);

        Int32 expectedScriptStart = 20;
        IReadOnlyList<Byte[]> scripts = battle.AiScripts.InFileOrder;
        for (Int32 index = 0; index < scripts.Count; index++)
        {
            Assert.Equal(expectedScriptStart, ReadUInt32(content, battleStart + aiStart + index * sizeof(UInt32)));
            expectedScriptStart += scripts[index].Length;
        }

        Assert.Equal(aiStart + expectedScriptStart, textOffsetsStart);
        Assert.Equal(textOffsetsStart + AlignToFour(battle.Texts.Count * sizeof(UInt16)), textStart);

        Int32 expectedTextStart = 0;
        for (Int32 index = 0; index < battle.Texts.Count; index++)
        {
            Assert.Equal(expectedTextStart, ReadUInt16(content, battleStart + textOffsetsStart + index * sizeof(UInt16)));
            expectedTextStart += GetEncodedLength(battle.Texts[index], file) + 1;
        }

        Assert.Equal(battleStart + textStart + AlignToFour(expectedTextStart), battleEnd);
        Assert.Equal(content.Length, GetSectionEnd(content, SectionCount));
    }

    public static Byte[] GetSection(Byte[] content, Int32 sectionIndex)
    {
        Int32[] starts = ReadSectionStarts(content);
        Int32 start = starts[sectionIndex - 1];
        Int32 end = sectionIndex < SectionCount ? starts[sectionIndex] : content.Length;
        return content[start..end];
    }

    public static Int32 GetSectionStart(Byte[] content, Int32 sectionIndex)
    {
        return ReadSectionStarts(content)[sectionIndex - 1];
    }

    private static Int32[] ReadSectionStarts(Byte[] content)
    {
        Int32[] starts = new Int32[SectionCount];
        for (Int32 index = 0; index < starts.Length; index++)
        {
            starts[index] = ReadUInt32(content, sizeof(UInt32) + index * sizeof(UInt32));
        }

        return starts;
    }

    private static Int32 GetSectionEnd(Byte[] content, Int32 sectionIndex)
    {
        return sectionIndex < SectionCount
            ? GetSectionStart(content, sectionIndex + 1)
            : ReadUInt32(content, 48);
    }

    private static Int32 GetEncodedLength(C0mText text, C0mFile file)
    {
        return text.EncodedValue?.Length ?? file.Encoding.GetByteCount(text.Value);
    }

    private static Int32 ReadUInt16(Byte[] content, Int32 position)
    {
        return BinaryPrimitives.ReadUInt16LittleEndian(content.AsSpan(position, sizeof(UInt16)));
    }

    private static Int32 ReadUInt32(Byte[] content, Int32 position)
    {
        UInt32 value = BinaryPrimitives.ReadUInt32LittleEndian(content.AsSpan(position, sizeof(UInt32)));
        return checked((Int32)value);
    }

    private static Int32 AlignToFour(Int32 value) => checked(value + 3) & ~3;
}
