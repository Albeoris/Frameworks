using Albeoris.Games.FF8.MngrpBin.Abstractions;
using Xunit;

namespace Albeoris.Games.FF8.MngrpBin.Tests;

/// <summary>
/// Verifies that editing texts changes exactly those texts. Every edit moves the data that
/// follows it, so a save-and-reload cycle only survives intact if all stored offsets — inside
/// string tables, across nested tables, from the block map into the block sections and from the
/// records into their companion text slot — are recalculated from the model.
/// </summary>
public class MngrpArchiveEditingTests
{
    [Theory]
    [MemberData(nameof(MngrpSample.All), MemberType = typeof(MngrpSample))]
    public void Write_ChangesOnlyTheEditedTexts(MngrpSample sample)
    {
        MngrpArchive archive = sample.Read();
        IReadOnlyDictionary<String, String> before = MngrpArchiveInventory.Capture(archive);

        IReadOnlyDictionary<String, String> edits = ApplyEdits(archive);

        IReadOnlyDictionary<String, String> after = MngrpArchiveInventory.Capture(MngrpArchive.Read(archive.Write(), sample.NewEncoding()));

        Assert.Equal(before.Keys.Order(), after.Keys.Order());
        Assert.All(edits, edit => Assert.Equal(edit.Value, after[edit.Key]));
        Assert.All(after.Where(value => !edits.ContainsKey(value.Key)), value => Assert.Equal(before[value.Key], value.Value));
    }

    [Theory]
    [MemberData(nameof(MngrpSample.All), MemberType = typeof(MngrpSample))]
    public void Write_GrowsASection_WhenItsTextsNoLongerFitTheOriginalSectors(MngrpSample sample)
    {
        MngrpArchive archive = sample.Read();
        MngrpStringTableSection section = archive.SectionsOfType<MngrpStringTableSection>().First(HasEditableEntry);
        Int32 entryIndex = FindEditableEntry(section.Table);
        Int32 originalSize = section.ReservedSize;

        String grownText = Repeat(section.Table[entryIndex]!, 2 * MngrpSectorSize);
        section.Table[entryIndex] = grownText;

        MngrpArchive reloaded = MngrpArchive.Read(archive.Write(), sample.NewEncoding());

        MngrpStringTableSection reloadedSection = reloaded.GetSection<MngrpStringTableSection>(section.SlotIndex);
        Assert.True(reloadedSection.ReservedSize > originalSize, $"The section still occupies {reloadedSection.ReservedSize} bytes after growing past {originalSize}.");
        Assert.Equal(grownText, reloadedSection.Table[entryIndex]);
    }

    [Theory]
    [MemberData(nameof(MngrpSample.All), MemberType = typeof(MngrpSample))]
    public void Write_KeepsTextBlockReferencesOnTarget_WhenEarlierBlocksChangeLength(MngrpSample sample)
    {
        MngrpArchive archive = sample.Read();
        MngrpTextBlockMapSection map = archive.SectionsOfType<MngrpTextBlockMapSection>().Single();
        MngrpTextBlockSection blocks = archive.SectionsOfType<MngrpTextBlockSection>().First();
        MngrpTextBlockReference reference = map.References.First(candidate => candidate.SectionNumber == 0 && candidate.BlockIndex > 0);
        Int32 referenceIndex = map.References.IndexOf(reference);
        String referencedText = blocks.Blocks[reference.BlockIndex!.Value].Texts[0].Value;

        // Emptying the first block moves every later block, and with them the map's offsets.
        blocks.Blocks[0].Texts[0] = new MngrpText(String.Empty);

        MngrpArchive reloaded = MngrpArchive.Read(archive.Write(), sample.NewEncoding());

        MngrpTextBlockReference reloadedReference = reloaded.SectionsOfType<MngrpTextBlockMapSection>().Single().References[referenceIndex];
        MngrpTextBlockSection reloadedBlocks = reloaded.SectionsOfType<MngrpTextBlockSection>().First();
        Assert.Equal(reference.BlockIndex, reloadedReference.BlockIndex);
        Assert.Equal(referencedText, reloadedBlocks.Blocks[reloadedReference.BlockIndex!.Value].Texts[0].Value);
    }

    /// <summary>
    /// Replaces one text in every editable layout with another text taken from the same sample,
    /// which keeps the replacement encodable in the sample's localization. Returns the expected
    /// inventory values of the edited paths.
    /// </summary>
    private static IReadOnlyDictionary<String, String> ApplyEdits(MngrpArchive archive)
    {
        Dictionary<String, String> edits = [];

        MngrpStringTableSection stringTable = archive.SectionsOfType<MngrpStringTableSection>().First(section => CountEditableEntries(section.Table) >= 2);
        Int32 targetIndex = FindEditableEntry(stringTable.Table);
        Int32 sourceIndex = FindEditableEntry(stringTable.Table, targetIndex + 1);
        stringTable.Table[targetIndex] = stringTable.Table[sourceIndex];
        edits[$"slot{stringTable.SlotIndex}/table/entry{targetIndex}/text"] = stringTable.Table[targetIndex]!;

        MngrpStringTableGroupSection group = archive.SectionsOfType<MngrpStringTableGroupSection>().First(section => section.Tables.Any(IsEditable));
        Int32 tableNumber = group.Tables.FindIndex(IsEditable);
        MngrpStringTable nestedTable = group.Tables[tableNumber]!;
        Int32 nestedIndex = FindEditableEntry(nestedTable);
        nestedTable[nestedIndex] = stringTable.Table[sourceIndex];
        edits[$"slot{group.SlotIndex}/table{tableNumber}/entry{nestedIndex}/text"] = nestedTable[nestedIndex]!;

        MngrpTextBlockSection blocks = archive.SectionsOfType<MngrpTextBlockSection>().Last();
        MngrpTextBlock block = blocks.Blocks[0];
        block.Texts[0] = new MngrpText(blocks.Blocks.SelectMany(candidate => candidate.Texts).First(text => IsSelfContained(text.Value)).Value);
        edits[$"slot{blocks.SlotIndex}/block0/text0"] = block.Texts[0].Value;

        MngrpTextRecordSection records = archive.SectionsOfType<MngrpTextRecordSection>().First();
        records.Records[0].Text = new MngrpText(records.Records.Select(record => record.Text).First(text => IsSelfContained(text.Value)).Value);
        edits[$"slot{records.SlotIndex}/record0/text"] = records.Records[0].Text.Value;

        return edits;
    }

    private const Int32 MngrpSectorSize = 0x800;

    /// <summary>
    /// Whether a text can be stored anywhere a text is expected. Texts containing the encoding's
    /// NUL-producing tag only survive at the start of a reserved span, where the format allows a
    /// leading NUL byte; elsewhere the NUL would terminate the text early.
    /// </summary>
    private static Boolean IsSelfContained(String text) => !text.Contains("{End}", StringComparison.Ordinal);

    private static Boolean IsEditable(MngrpStringTable? table) => table is not null && CountEditableEntries(table) >= 1;

    private static Boolean HasEditableEntry(MngrpStringTableSection section) => CountEditableEntries(section.Table) >= 1;

    private static Int32 CountEditableEntries(MngrpStringTable table) => table.Entries.Count(entry => entry.Text is not null && IsSelfContained(entry.Text.Value) && entry.Text.Value.Length > 0);

    private static Int32 FindEditableEntry(MngrpStringTable table, Int32 startIndex = 0)
    {
        for (Int32 i = startIndex; i < table.Entries.Count; i++)
        {
            if (table.Entries[i].Text is MngrpText text && IsSelfContained(text.Value) && text.Value.Length > 0)
            {
                return i;
            }
        }

        throw new InvalidOperationException("The table holds no further editable entry.");
    }

    private static String Repeat(String text, Int32 minimumLength) => String.Concat(Enumerable.Repeat(text, minimumLength / text.Length + 1));
}
