using System.Text;
using Albeoris.Games.FF8.MngrpBin.Abstractions;
using Xunit;

namespace Albeoris.Games.FF8.MngrpBin.Tests;

/// <summary>
/// Verifies that no byte of a sample is silently dropped while reading: for every occupied slot,
/// the number of bytes the parsed section's properties describe covers everything up to the
/// slot's trailing zero padding, so nothing meaningful is left untracked.
/// </summary>
public class MngrpArchiveCoverageTests
{
    [Theory]
    [MemberData(nameof(MngrpSample.All), MemberType = typeof(MngrpSample))]
    public void Read_LeavesNoMeaningfulByteUnrepresented(MngrpSample sample)
    {
        MngrpSlotMap slots = MngrpSlotMap.Read(sample.Header);
        Encoding encoding = sample.NewEncoding();
        MngrpArchive archive = sample.Read();

        foreach (IMngrpSection section in archive.Sections)
        {
            AssertCovers(section.SlotIndex, MeasureSection(section, encoding), sample, slots);
            if (section is MngrpTextRecordSection records)
            {
                AssertCovers(records.TextSlotIndex, MeasureRecordTexts(records, encoding), sample, slots);
            }
        }
    }

    [Theory]
    [MemberData(nameof(MngrpSample.All), MemberType = typeof(MngrpSample))]
    public void Read_KeepsTheGarbageTheOriginalPackerLeftBehindStrings(MngrpSample sample)
    {
        MngrpArchive archive = sample.Read();

        IEnumerable<MngrpTextEntry> entries = archive.SectionsOfType<MngrpStringTableSection>()
            .Select(section => section.Table)
            .Concat(archive.SectionsOfType<MngrpStringTableGroupSection>().SelectMany(section => section.Tables).OfType<MngrpStringTable>())
            .SelectMany(table => table.Entries);

        Assert.Contains(entries, entry => entry.TrailingBytes.AsSpan().IndexOfAnyExcept((Byte)0) >= 0);
    }

    [Theory]
    [MemberData(nameof(MngrpSample.All), MemberType = typeof(MngrpSample))]
    public void Read_ResolvesEveryTextBlockReference(MngrpSample sample)
    {
        MngrpArchive archive = sample.Read();

        IEnumerable<MngrpTextBlockReference> references = archive.SectionsOfType<MngrpTextBlockMapSection>().SelectMany(section => section.References);

        Assert.All(references, reference => Assert.NotNull(reference.BlockIndex));
    }

    private static void AssertCovers(Int32 slotIndex, Int32 describedLength, MngrpSample sample, MngrpSlotMap slots)
    {
        ReadOnlySpan<Byte> body = slots.GetBody(slotIndex, sample.Content);
        Assert.True(describedLength <= body.Length, $"Slot {slotIndex}: the parsed section describes {describedLength} bytes, more than the {body.Length} bytes the slot holds.");

        Int32 lastMeaningfulByte = body.LastIndexOfAnyExcept((Byte)0) + 1;
        Assert.True(describedLength >= lastMeaningfulByte, $"Slot {slotIndex}: the parsed section describes only {describedLength} bytes, leaving non-zero bytes up to offset {lastMeaningfulByte} untracked.");
    }

    private static Int32 MeasureSection(IMngrpSection section, Encoding encoding) => section switch
    {
        MngrpOpaqueSection opaque => opaque.Content.Length,
        MngrpStringTableSection stringTable => MeasureTable(stringTable.Table, encoding),
        MngrpStringTableGroupSection group => 2 + group.Tables.Count * 2 + group.LeadingBytes.Length
            + group.Tables.OfType<MngrpStringTable>().Sum(table => MeasureTable(table, encoding)),
        MngrpTextBlockSection blocks => blocks.Blocks.Aggregate(0, (position, block) => AlignToFour(position + MeasureBlock(block, encoding))) + blocks.TrailingData.Length,
        MngrpTextBlockMapSection map => 4 + map.References.Count * 4 + map.TrailingData.Length,
        MngrpTextRecordSection records => records.Records.Count * 8 + records.TrailingData.Length,
        _ => throw new NotSupportedException($"Unexpected section type '{section.GetType().Name}'."),
    };

    private static Int32 MeasureTable(MngrpStringTable table, Encoding encoding)
    {
        return 2 + table.Entries.Count * 2 + table.LeadingBytes.Length
            + table.Entries.Sum(entry => entry.Text is null ? 0 : MeasureText(entry.Text, encoding) + entry.TrailingBytes.Length);
    }

    private static Int32 MeasureBlock(MngrpTextBlock block, Encoding encoding)
    {
        return 8 + block.Texts.Sum(text => MeasureText(text, encoding) + 1) + block.TrailingBytes.Length;
    }

    private static Int32 MeasureRecordTexts(MngrpTextRecordSection records, Encoding encoding)
    {
        return records.Records.Sum(record => MeasureText(record.Text, encoding) + record.TextTrailingBytes.Length);
    }

    private static Int32 MeasureText(MngrpText text, Encoding encoding)
    {
        return text.EncodedValue?.Length ?? encoding.GetByteCount(text.Value);
    }

    private static Int32 AlignToFour(Int32 position) => (position + 3) & ~3;
}
