using Albeoris.Games.FF8.C0mDat.Abstractions;

namespace Albeoris.Games.FF8.C0mDat.Internal;

/// <summary>Constants and checked numeric conversions shared by the native codecs.</summary>
internal static class C0mFormat
{
    public const Int32 SectionCount = 11;
    public const Int32 InformationSectionSize = 0x17C;
    public const Int32 InformationNameSize = 24;
    public const Int32 InformationStatDataSize = InformationSectionSize - InformationNameSize;
    public const Int32 BattleHeaderSize = 16;
    public const Int32 AiScriptCount = 5;
    public const Int32 AiOffsetTableSize = AiScriptCount * sizeof(UInt32);
    public const UInt32 BattleSubsectionCount = 3;

    public static Int32 FileHeaderSize => sizeof(UInt32) + SectionCount * sizeof(UInt32) + sizeof(UInt32);

    public static Int32 AlignToFour(Int32 length) => checked(length + 3) & ~3;

    public static UInt16 ToUInt16(Int32 value, String description)
    {
        if (value is < 0 or > UInt16.MaxValue)
        {
            throw new InvalidOperationException($"The {description} ({value}) does not fit in 16 bits.");
        }

        return (UInt16)value;
    }

    public static UInt32 ToUInt32(Int64 value, String description)
    {
        if (value is < 0 or > UInt32.MaxValue)
        {
            throw new InvalidOperationException($"The {description} ({value}) does not fit in 32 bits.");
        }

        return (UInt32)value;
    }

    public static C0mSectionKind GetSectionKind(Int32 index)
    {
        if (index is < 1 or > SectionCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return (C0mSectionKind)index;
    }
}
