using System.Text;
using Albeoris.Games.FF8.TextEncoding;
using Albeoris.Games.FF8.MngrpBin.Abstractions;
using Xunit;

namespace Albeoris.Games.FF8.MngrpBin.Tests;

/// <summary>
/// Covers the string-table corner cases with hand-crafted bytes: the offset-zero sentinel, a
/// reserved span holding nothing but NUL bytes, a text the packer prefixed with a NUL byte, and
/// garbage the packer left behind a terminator.
/// </summary>
public class MngrpStringTableReadWriteTests
{
    private const Int32 StringTableSlot = 87;

    private readonly Encoding _encoding = FF8Encoding.CreateEuropean();

    [Fact]
    public void Read_ReportsAnAbsentEntry_ForTheOffsetZeroSentinel()
    {
        MngrpStringTable table = ReadTable(Text("Alpha"), null, Text("Beta"));

        Assert.Null(table[1]);
        Assert.Empty(table.Entries[1].TrailingBytes);
    }

    [Fact]
    public void Read_ReportsAnEmptyText_ForASpanOfNulBytesOnly()
    {
        MngrpStringTable table = ReadTable(Span(0, 0, 0, 0), Text("Beta"));

        Assert.Equal(String.Empty, table[0]);
        Assert.Equal([0, 0, 0, 0], table.Entries[0].TrailingBytes);
    }

    [Fact]
    public void Read_KeepsALeadingNulByte_AsPartOfTheText()
    {
        Byte[] encodedText = _encoding.GetBytes("Alpha");

        MngrpStringTable table = ReadTable(Span([0, .. encodedText, 0]), Text("Beta"));

        Assert.Equal(_encoding.GetString([0, .. encodedText]), table[0]);
        Assert.Equal([0], table.Entries[0].TrailingBytes);
    }

    [Fact]
    public void Read_TellsALeadingNulTextApartFromAnEmptyOne_ByTheSecondByte()
    {
        Byte[] encodedText = _encoding.GetBytes("Alpha");

        MngrpStringTable table = ReadTable(Span([0, .. encodedText, 0]), Span([0, 0, 0]), Text("Beta"));

        Assert.NotEqual(String.Empty, table[0]);
        Assert.Equal(String.Empty, table[1]);
    }

    [Fact]
    public void Read_PreservesNonZeroBytes_LeftBehindATerminator()
    {
        Byte[] encodedText = _encoding.GetBytes("Alpha");

        MngrpStringTable table = ReadTable(Span([.. encodedText, 0, 0x11, 0x22]), Text("Beta"));

        Assert.Equal("Alpha", table[0]);
        Assert.Equal([0, 0x11, 0x22], table.Entries[0].TrailingBytes);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Write_ReproducesTheSourceBytes_ForEveryCornerCase(Boolean withOverAllocatedSlot)
    {
        Byte[] encodedText = _encoding.GetBytes("Alpha");
        Byte[] body = MngrpFileBuilder.StringTable(
            Span([0, .. encodedText, 0]),
            null,
            Span(0, 0, 0, 0),
            Span([.. encodedText, 0, 0x11, 0x22]),
            Text("Beta"));
        MngrpFilePair source = MngrpFileBuilder.Build((StringTableSlot, withOverAllocatedSlot ? [.. body, .. new Byte[MngrpFileBuilder.SectorSize]] : body));

        MngrpFilePair written = MngrpArchive.Read(source, _encoding).Write();

        Assert.Equal(source.Content, written.Content);
        Assert.Equal(source.Header, written.Header);
    }

    [Fact]
    public void Write_RecalculatesTheOffsetsOfEveryFollowingEntry_WhenATextGrows()
    {
        MngrpFilePair source = MngrpFileBuilder.Build((StringTableSlot, MngrpFileBuilder.StringTable(Text("Alpha"), Text("Beta"), Text("Gamma"))));
        MngrpArchive archive = MngrpArchive.Read(source, _encoding);
        archive.GetSection<MngrpStringTableSection>(StringTableSlot).Table[0] = "Alpha grown much longer";

        MngrpStringTable reloaded = MngrpArchive.Read(archive.Write(), _encoding).GetSection<MngrpStringTableSection>(StringTableSlot).Table;

        Assert.Equal(["Alpha grown much longer", "Beta", "Gamma"], reloaded.Entries.Select(entry => entry.Text?.Value));
    }

    [Fact]
    public void Write_DropsAnEntry_WhenItsTextIsSetToNull()
    {
        MngrpFilePair source = MngrpFileBuilder.Build((StringTableSlot, MngrpFileBuilder.StringTable(Text("Alpha"), Text("Beta"))));
        MngrpArchive archive = MngrpArchive.Read(source, _encoding);
        MngrpStringTable table = archive.GetSection<MngrpStringTableSection>(StringTableSlot).Table;
        table.Entries[0].Text = null;
        table.Entries[0].TrailingBytes = [];

        MngrpStringTable reloaded = MngrpArchive.Read(archive.Write(), _encoding).GetSection<MngrpStringTableSection>(StringTableSlot).Table;

        Assert.Null(reloaded[0]);
        Assert.Equal("Beta", reloaded[1]);
    }

    private MngrpStringTable ReadTable(params ReadOnlyMemory<Byte>?[] entrySpans)
    {
        MngrpFilePair source = MngrpFileBuilder.Build((StringTableSlot, MngrpFileBuilder.StringTable(entrySpans)));
        return MngrpArchive.Read(source, _encoding).GetSection<MngrpStringTableSection>(StringTableSlot).Table;
    }

    private ReadOnlyMemory<Byte> Text(String value) => Span([.. _encoding.GetBytes(value), 0]);

    private static ReadOnlyMemory<Byte> Span(params Byte[] bytes) => bytes;
}
