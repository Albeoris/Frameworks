using System.Buffers.Binary;
using System.Text;
using Albeoris.Games.FF8.TextEncoding;
using Albeoris.Games.FF8.MngrpBin.Abstractions;
using Xunit;

namespace Albeoris.Games.FF8.MngrpBin.Tests;

/// <summary>
/// Verifies that malformed input is rejected with a diagnosable error instead of being parsed
/// into a silently wrong model.
/// </summary>
public class MngrpArchiveValidationTests
{
    private const Int32 StringTableSlot = 87;
    private const Int32 OpaqueSlot = 9;

    private readonly Encoding _encoding = FF8Encoding.CreateEuropean();

    [Fact]
    public void Read_Rejects_AHeaderOfTheWrongSize()
    {
        MngrpFilePair source = MngrpFileBuilder.Build((StringTableSlot, MngrpFileBuilder.StringTable()));

        InvalidDataException exception = Assert.Throws<InvalidDataException>(() => MngrpArchive.Read(source.Content, source.Header.AsSpan(8), _encoding));

        Assert.Contains("2048", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_Rejects_ASlotPointingOutsideTheContentFile()
    {
        MngrpFilePair source = MngrpFileBuilder.Build((StringTableSlot, MngrpFileBuilder.StringTable()));
        BinaryPrimitives.WriteUInt32LittleEndian(source.Header.AsSpan(StringTableSlot * 8 + 4), 0x10000);

        InvalidDataException exception = Assert.Throws<InvalidDataException>(() => MngrpArchive.Read(source, _encoding));

        Assert.Contains($"Slot {StringTableSlot}", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_Rejects_AHalfVacantSlot()
    {
        MngrpFilePair source = MngrpFileBuilder.Build((StringTableSlot, MngrpFileBuilder.StringTable()));
        BinaryPrimitives.WriteUInt32LittleEndian(source.Header.AsSpan(StringTableSlot * 8 + 4), 0);

        InvalidDataException exception = Assert.Throws<InvalidDataException>(() => MngrpArchive.Read(source, _encoding));

        Assert.Contains("half-vacant", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_Rejects_AStringTableDeclaringMoreEntriesThanItHolds()
    {
        Byte[] body = MngrpFileBuilder.StringTable();
        BinaryPrimitives.WriteUInt16LittleEndian(body, UInt16.MaxValue);

        InvalidDataException exception = Assert.Throws<InvalidDataException>(() => MngrpArchive.Read(MngrpFileBuilder.Build((StringTableSlot, body)), _encoding));

        Assert.Contains($"Slot {StringTableSlot}", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_Rejects_AStringTableEntryPointingOutsideTheSection()
    {
        Byte[] body = MngrpFileBuilder.StringTable(new Byte[] { 0x41, 0 });
        BinaryPrimitives.WriteUInt16LittleEndian(body.AsSpan(2), UInt16.MaxValue);

        InvalidDataException exception = Assert.Throws<InvalidDataException>(() => MngrpArchive.Read(MngrpFileBuilder.Build((StringTableSlot, body)), _encoding));

        Assert.Contains("out of order or out of bounds", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_Rejects_ATextBlockWithAnImpossibleLength()
    {
        Byte[] body = MngrpFileBuilder.TextBlock(1, 2, 3, _encoding.GetBytes("Alpha"));
        BinaryPrimitives.WriteUInt16LittleEndian(body.AsSpan(6), 3);

        InvalidDataException exception = Assert.Throws<InvalidDataException>(() => MngrpArchive.Read(MngrpFileBuilder.Build((128, body)), _encoding));

        Assert.Contains("invalid length", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Read_PreservesAnUnknownSlot_AsAnOpaqueSection()
    {
        Byte[] body = [.. Enumerable.Range(0, 64).Select(value => (Byte)value)];
        MngrpFilePair source = MngrpFileBuilder.Build((OpaqueSlot, body));

        MngrpArchive archive = MngrpArchive.Read(source, _encoding);

        MngrpOpaqueSection section = archive.GetSection<MngrpOpaqueSection>(OpaqueSlot);
        Assert.Equal(MngrpFileBuilder.SectorSize, section.Content.Length);
        Assert.Equal(body, section.Content[..body.Length]);
        Assert.Equal(source.Content, archive.Write().Content);
    }

    [Fact]
    public void GetSection_Reports_AMissingOrMistypedSlot()
    {
        MngrpArchive archive = MngrpArchive.Read(MngrpFileBuilder.Build((StringTableSlot, MngrpFileBuilder.StringTable())), _encoding);

        Assert.Throws<KeyNotFoundException>(() => archive.GetSection<MngrpStringTableSection>(OpaqueSlot));
        Assert.Throws<InvalidCastException>(() => archive.GetSection<MngrpOpaqueSection>(StringTableSlot));
    }

    [Fact]
    public void Write_Reports_ATextTableGrownPastTheSixteenBitOffsetLimit()
    {
        MngrpArchive archive = MngrpArchive.Read(MngrpFileBuilder.Build((StringTableSlot, MngrpFileBuilder.StringTable(new Byte[] { 0x41, 0 }, new Byte[] { 0x42, 0 }))), _encoding);
        archive.GetSection<MngrpStringTableSection>(StringTableSlot).Table[0] = new String('A', UInt16.MaxValue);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(archive.Write);

        Assert.Contains("does not fit in 16 bits", exception.Message, StringComparison.Ordinal);
    }
}
