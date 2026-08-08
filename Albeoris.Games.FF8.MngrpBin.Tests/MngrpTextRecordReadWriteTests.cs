using System.Text;
using Albeoris.Games.FF8.TextEncoding;
using Albeoris.Games.FF8.MngrpBin.Abstractions;
using Xunit;

namespace Albeoris.Games.FF8.MngrpBin.Tests;

/// <summary>
/// Covers the two-slot text-record layout: the records and their texts are exposed as one
/// section, and the offsets linking them are recalculated on write.
/// </summary>
public class MngrpTextRecordReadWriteTests
{
    private const Int32 RecordSlot = 188;
    private const Int32 TextSlot = 196;

    private readonly Encoding _encoding = FF8Encoding.CreateEuropean();

    [Fact]
    public void Read_PresentsBothSlots_AsOneSection()
    {
        MngrpArchive archive = MngrpArchive.Read(BuildSample(), _encoding);

        MngrpTextRecordSection section = archive.GetSection<MngrpTextRecordSection>(RecordSlot);

        Assert.Equal(TextSlot, section.TextSlotIndex);
        Assert.Equal(["Alpha", "Beta"], section.Records.Select(record => record.Text.Value));
        Assert.Equal([[1, 2, 3, 4, 5, 6], [7, 8, 9, 10, 11, 12]], section.Records.Select(record => record.Payload));
        Assert.DoesNotContain(archive.Sections, candidate => candidate.SlotIndex == TextSlot);
    }

    [Fact]
    public void Write_ReproducesTheSourceBytes()
    {
        MngrpFilePair source = BuildSample();

        MngrpFilePair written = MngrpArchive.Read(source, _encoding).Write();

        Assert.Equal(source.Content, written.Content);
        Assert.Equal(source.Header, written.Header);
    }

    [Fact]
    public void Write_RecalculatesTextOffsets_WhenAnEarlierTextChangesLength()
    {
        MngrpArchive archive = MngrpArchive.Read(BuildSample(), _encoding);
        archive.GetSection<MngrpTextRecordSection>(RecordSlot).Records[0].Text = new MngrpText("Alpha grown much longer");

        MngrpTextRecordSection reloaded = MngrpArchive.Read(archive.Write(), _encoding).GetSection<MngrpTextRecordSection>(RecordSlot);

        Assert.Equal(["Alpha grown much longer", "Beta"], reloaded.Records.Select(record => record.Text.Value));
        Assert.Equal([[1, 2, 3, 4, 5, 6], [7, 8, 9, 10, 11, 12]], reloaded.Records.Select(record => record.Payload));
    }

    [Fact]
    public void Read_FallsBackToAnOpaqueSection_WhenTheCompanionTextSlotIsVacant()
    {
        MngrpFilePair source = MngrpFileBuilder.Build((RecordSlot, BuildRecords()));

        MngrpArchive archive = MngrpArchive.Read(source, _encoding);

        Assert.IsType<MngrpOpaqueSection>(archive.GetSection<MngrpOpaqueSection>(RecordSlot));
        Assert.Equal(source.Content, archive.Write().Content);
    }

    private MngrpFilePair BuildSample()
    {
        List<Byte> texts = [];
        texts.AddRange(_encoding.GetBytes("Alpha"));
        texts.Add(0);
        texts.AddRange(_encoding.GetBytes("Beta"));
        texts.Add(0);

        return MngrpFileBuilder.Build((RecordSlot, BuildRecords()), (TextSlot, [.. texts]));
    }

    private Byte[] BuildRecords()
    {
        Int32 secondTextOffset = _encoding.GetByteCount("Alpha") + 1;
        return
        [
            0, 0, 1, 2, 3, 4, 5, 6,
            (Byte)secondTextOffset, 0, 7, 8, 9, 10, 11, 12,
            0, 0, 0, 0, 0, 0, 0, 0,
        ];
    }
}
