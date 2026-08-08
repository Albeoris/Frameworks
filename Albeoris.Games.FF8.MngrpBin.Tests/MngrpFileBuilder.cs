using System.Buffers.Binary;

namespace Albeoris.Games.FF8.MngrpBin.Tests;

/// <summary>
/// Assembles hand-crafted <c>mngrp.bin</c>/<c>mngrphd.bin</c> pairs so that corner cases can be
/// expressed as exact bytes instead of being hunted for in the shipped samples.
/// </summary>
public static class MngrpFileBuilder
{
    public const Int32 SectorSize = 0x800;
    private const Int32 SlotCount = 256;

    /// <summary>
    /// Builds a pair holding <paramref name="sections"/>, each padded to the size given, or to
    /// the next sector boundary when no size is given.
    /// </summary>
    public static MngrpFilePair Build(params (Int32 Slot, Byte[] Body)[] sections)
    {
        ArgumentNullException.ThrowIfNull(sections);

        Byte[] header = new Byte[SlotCount * 8];
        for (Int32 slotIndex = 0; slotIndex < SlotCount; slotIndex++)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(slotIndex * 8, 4), UInt32.MaxValue);
        }

        List<Byte> content = [];
        foreach ((Int32 slot, Byte[] body) in sections.OrderBy(section => section.Slot))
        {
            Int32 size = (body.Length + SectorSize - 1) / SectorSize * SectorSize;
            BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(slot * 8, 4), (UInt32)content.Count + 1);
            BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(slot * 8 + 4, 4), (UInt32)size);
            content.AddRange(body);
            content.AddRange(new Byte[size - body.Length]);
        }

        return new MngrpFilePair([.. content], header);
    }

    /// <summary>Lays out a string table: a count, one offset per entry and the entries' spans.</summary>
    public static Byte[] StringTable(params ReadOnlyMemory<Byte>?[] entrySpans)
    {
        ArgumentNullException.ThrowIfNull(entrySpans);

        List<Byte> body = [];
        AddUInt16(body, (UInt16)entrySpans.Length);
        Int32 position = 2 + entrySpans.Length * 2;
        foreach (ReadOnlyMemory<Byte>? span in entrySpans)
        {
            AddUInt16(body, span is null ? (UInt16)0 : (UInt16)position);
            position += span?.Length ?? 0;
        }

        foreach (ReadOnlyMemory<Byte>? span in entrySpans)
        {
            if (span is ReadOnlyMemory<Byte> present)
            {
                body.AddRange(present.Span);
            }
        }

        return [.. body];
    }

    /// <summary>Lays out one text block: three link ids, the total length, then the payload.</summary>
    public static Byte[] TextBlock(UInt16 originId, UInt16 leftId, UInt16 rightId, params Byte[][] texts)
    {
        ArgumentNullException.ThrowIfNull(texts);

        List<Byte> payload = [];
        foreach (Byte[] text in texts)
        {
            payload.AddRange(text);
            payload.Add(0);
        }

        List<Byte> block = [];
        AddUInt16(block, originId);
        AddUInt16(block, leftId);
        AddUInt16(block, rightId);
        AddUInt16(block, (UInt16)(8 + payload.Count));
        block.AddRange(payload);
        while (block.Count % 4 != 0)
        {
            block.Add(0);
        }

        return [.. block];
    }

    /// <summary>Lays out a text block map from (section number, block offset) pairs.</summary>
    public static Byte[] TextBlockMap(params (Int32 SectionNumber, Int32 BlockOffset)[] references)
    {
        ArgumentNullException.ThrowIfNull(references);

        List<Byte> body = [];
        body.AddRange(BitConverter.GetBytes((UInt32)references.Length));
        foreach ((Int32 sectionNumber, Int32 blockOffset) in references)
        {
            AddUInt16(body, (UInt16)blockOffset);
            AddUInt16(body, (UInt16)sectionNumber);
        }

        return [.. body];
    }

    private static void AddUInt16(List<Byte> target, UInt16 value)
    {
        target.Add((Byte)value);
        target.Add((Byte)(value >> 8));
    }
}
