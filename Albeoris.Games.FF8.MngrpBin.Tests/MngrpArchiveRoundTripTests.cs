using System.Buffers.Binary;
using Albeoris.Games.FF8.MngrpBin.Abstractions;
using Xunit;

namespace Albeoris.Games.FF8.MngrpBin.Tests;

/// <summary>
/// Verifies that parsing and re-serializing a shipped file pair reproduces it exactly. The
/// samples contain the corner cases the format's original packer produced: strings prefixed with
/// a NUL byte, strings whose whole reserved span is NUL, absent entries, non-zero garbage behind
/// terminators and slots padded with more sectors than their content needs.
/// </summary>
public class MngrpArchiveRoundTripTests
{
    [Theory]
    [MemberData(nameof(MngrpSample.All), MemberType = typeof(MngrpSample))]
    public void Write_ReproducesTheSourceFiles_ByteForByte(MngrpSample sample)
    {
        MngrpArchive archive = sample.Read();

        MngrpFilePair written = archive.Write();

        Assert.Equal(sample.Content, written.Content);
        Assert.Equal(sample.Header, written.Header);
    }

    [Theory]
    [MemberData(nameof(MngrpSample.All), MemberType = typeof(MngrpSample))]
    public void Write_IsIdempotent_AcrossRepeatedCycles(MngrpSample sample)
    {
        MngrpFilePair written = sample.Read().Write();

        MngrpFilePair rewritten = MngrpArchive.Read(written, sample.NewEncoding()).Write();

        Assert.Equal(written.Content, rewritten.Content);
        Assert.Equal(written.Header, rewritten.Header);
    }

    [Theory]
    [MemberData(nameof(MngrpSample.All), MemberType = typeof(MngrpSample))]
    public void Read_ProducesTheSameModel_FromTheReserializedFiles(MngrpSample sample)
    {
        IReadOnlyDictionary<String, String> original = MngrpArchiveInventory.Capture(sample.Read());

        MngrpArchive reloaded = MngrpArchive.Read(sample.Read().Write(), sample.NewEncoding());

        Assert.Equal(original, MngrpArchiveInventory.Capture(reloaded));
    }

    [Theory]
    [MemberData(nameof(MngrpSample.All), MemberType = typeof(MngrpSample))]
    public void Read_ParsesEveryKnownLayout(MngrpSample sample)
    {
        MngrpArchive archive = sample.Read();

        Assert.All(
            Enum.GetValues<MngrpSectionLayout>(),
            layout => Assert.Contains(archive.Sections, section => section.Layout == layout));
    }

    [Theory]
    [MemberData(nameof(MngrpSample.All), MemberType = typeof(MngrpSample))]
    public void Read_RepresentsEveryOccupiedSlot(MngrpSample sample)
    {
        HashSet<Int32> occupiedSlots = [.. EnumerateOccupiedSlots(sample.Header)];

        MngrpArchive archive = sample.Read();

        HashSet<Int32> representedSlots = [];
        foreach (IMngrpSection section in archive.Sections)
        {
            Assert.True(representedSlots.Add(section.SlotIndex), $"Slot {section.SlotIndex} is represented twice.");
            if (section is MngrpTextRecordSection records)
            {
                Assert.True(representedSlots.Add(records.TextSlotIndex), $"Slot {records.TextSlotIndex} is represented twice.");
            }
        }

        Assert.Equal(occupiedSlots.Order(), representedSlots.Order());
    }

    private static IEnumerable<Int32> EnumerateOccupiedSlots(Byte[] header)
    {
        for (Int32 slotIndex = 0; slotIndex * 8 < header.Length; slotIndex++)
        {
            UInt32 rawOffset = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(slotIndex * 8, 4));
            UInt32 size = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(slotIndex * 8 + 4, 4));
            if (rawOffset is not (UInt32.MaxValue or 0) && size != 0)
            {
                yield return slotIndex;
            }
        }
    }
}
