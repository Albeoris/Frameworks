using System.Security.Cryptography;
using Albeoris.Games.FF8.MngrpBin.Abstractions;

namespace Albeoris.Games.FF8.MngrpBin.Tests;

/// <summary>
/// A flat snapshot of everything an archive exposes through its properties, keyed by a stable
/// path such as <c>slot87/table/entry12/text</c>. Comparing two snapshots shows exactly which
/// values an edit-and-save cycle changed.
/// </summary>
public static class MngrpArchiveInventory
{
    public static IReadOnlyDictionary<String, String> Capture(MngrpArchive archive)
    {
        ArgumentNullException.ThrowIfNull(archive);

        Dictionary<String, String> values = [];
        foreach (IMngrpSection section in archive.Sections)
        {
            String slot = $"slot{section.SlotIndex}";
            switch (section)
            {
                case MngrpOpaqueSection opaque:
                    values[$"{slot}/content"] = Convert.ToHexString(SHA256.HashData(opaque.Content));
                    break;

                case MngrpStringTableSection stringTable:
                    CaptureTable(values, $"{slot}/table", stringTable.Table);
                    break;

                case MngrpStringTableGroupSection group:
                    for (Int32 i = 0; i < group.Tables.Count; i++)
                    {
                        if (group.Tables[i] is MngrpStringTable table)
                        {
                            CaptureTable(values, $"{slot}/table{i}", table);
                        }
                        else
                        {
                            values[$"{slot}/table{i}"] = "<absent>";
                        }
                    }

                    values[$"{slot}/leadingBytes"] = Convert.ToHexString(group.LeadingBytes);
                    break;

                case MngrpTextBlockSection blocks:
                    for (Int32 i = 0; i < blocks.Blocks.Count; i++)
                    {
                        MngrpTextBlock block = blocks.Blocks[i];
                        values[$"{slot}/block{i}/links"] = $"{block.OriginId},{block.LeftId},{block.RightId}";
                        values[$"{slot}/block{i}/trailingBytes"] = Convert.ToHexString(block.TrailingBytes);
                        for (Int32 j = 0; j < block.Texts.Count; j++)
                        {
                            values[$"{slot}/block{i}/text{j}"] = block.Texts[j].Value;
                        }
                    }

                    values[$"{slot}/trailingData"] = Convert.ToHexString(blocks.TrailingData);
                    break;

                case MngrpTextBlockMapSection map:
                    for (Int32 i = 0; i < map.References.Count; i++)
                    {
                        MngrpTextBlockReference reference = map.References[i];
                        values[$"{slot}/reference{i}"] = reference.BlockIndex is Int32 blockIndex
                            ? $"section{reference.SectionNumber}/block{blockIndex}"
                            : $"section{reference.SectionNumber}/unresolved:{reference.StoredOffset}";
                    }

                    values[$"{slot}/trailingData"] = Convert.ToHexString(map.TrailingData);
                    break;

                case MngrpTextRecordSection records:
                    for (Int32 i = 0; i < records.Records.Count; i++)
                    {
                        MngrpTextRecord record = records.Records[i];
                        values[$"{slot}/record{i}/text"] = record.Text.Value;
                        values[$"{slot}/record{i}/payload"] = Convert.ToHexString(record.Payload);
                        values[$"{slot}/record{i}/textTrailingBytes"] = Convert.ToHexString(record.TextTrailingBytes);
                    }

                    values[$"{slot}/trailingData"] = Convert.ToHexString(records.TrailingData);
                    break;

                default:
                    throw new NotSupportedException($"Unexpected section type '{section.GetType().Name}'.");
            }
        }

        return values;
    }

    private static void CaptureTable(Dictionary<String, String> values, String path, MngrpStringTable table)
    {
        for (Int32 i = 0; i < table.Entries.Count; i++)
        {
            MngrpTextEntry entry = table.Entries[i];
            values[$"{path}/entry{i}/text"] = entry.Text?.Value ?? "<absent>";
            values[$"{path}/entry{i}/trailingBytes"] = Convert.ToHexString(entry.TrailingBytes);
        }

        values[$"{path}/leadingBytes"] = Convert.ToHexString(table.LeadingBytes);
    }
}
