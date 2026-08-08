using System.Buffers.Binary;

namespace Albeoris.Games.FF8.MngrpBin.Tests;

/// <summary>
/// The occupied slots of a <c>mngrphd.bin</c>, decoded independently of the library so that tests
/// can look at the raw bytes a section was parsed from.
/// </summary>
public sealed class MngrpSlotMap
{
    private readonly Dictionary<Int32, (Int32 Offset, Int32 Size)> _slots;

    private MngrpSlotMap(Dictionary<Int32, (Int32 Offset, Int32 Size)> slots)
    {
        _slots = slots;
    }

    public IEnumerable<Int32> OccupiedSlots => _slots.Keys.Order();

    public static MngrpSlotMap Read(ReadOnlySpan<Byte> header)
    {
        Dictionary<Int32, (Int32, Int32)> slots = [];
        for (Int32 slotIndex = 0; slotIndex * 8 < header.Length; slotIndex++)
        {
            UInt32 rawOffset = BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(slotIndex * 8, 4));
            UInt32 size = BinaryPrimitives.ReadUInt32LittleEndian(header.Slice(slotIndex * 8 + 4, 4));
            if (rawOffset is not (UInt32.MaxValue or 0) && size != 0)
            {
                slots[slotIndex] = ((Int32)(rawOffset - 1), (Int32)size);
            }
        }

        return new MngrpSlotMap(slots);
    }

    /// <summary>Returns the bytes slot <paramref name="slotIndex"/> occupies in the content file.</summary>
    public ReadOnlySpan<Byte> GetBody(Int32 slotIndex, ReadOnlySpan<Byte> content)
    {
        (Int32 offset, Int32 size) = _slots[slotIndex];
        return content.Slice(offset, size);
    }
}
