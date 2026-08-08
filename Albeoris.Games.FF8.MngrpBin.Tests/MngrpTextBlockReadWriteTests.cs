using System.Text;
using Albeoris.Games.FF8.TextEncoding;
using Albeoris.Games.FF8.MngrpBin.Abstractions;
using Xunit;

namespace Albeoris.Games.FF8.MngrpBin.Tests;

/// <summary>
/// Covers the text-block layout and the map that addresses its blocks by byte offset, including
/// a map reference whose offset matches no block — the shape a corrupted shipped file takes.
/// </summary>
public class MngrpTextBlockReadWriteTests
{
    private const Int32 MapSlot = 127;
    private const Int32 FirstBlockSlot = 128;
    private const Int32 SecondBlockSlot = 129;

    private readonly Encoding _encoding = FF8Encoding.CreateEuropean();

    [Fact]
    public void Read_ResolvesReferences_ToBlockIndexes()
    {
        MngrpArchive archive = MngrpArchive.Read(BuildSample(unresolvableOffset: null), _encoding);

        MngrpTextBlockMapSection map = archive.SectionsOfType<MngrpTextBlockMapSection>().Single();

        Assert.Equal([(0, 0), (0, 1), (1, 0)], map.References.Select(reference => (reference.SectionNumber, reference.BlockIndex)));
    }

    [Fact]
    public void Read_KeepsAReferenceUnresolved_WhenItsOffsetMatchesNoBlock()
    {
        MngrpArchive archive = MngrpArchive.Read(BuildSample(unresolvableOffset: 9), _encoding);

        MngrpTextBlockReference reference = archive.SectionsOfType<MngrpTextBlockMapSection>().Single().References[^1];

        Assert.Null(reference.BlockIndex);
        Assert.Equal(9, reference.StoredOffset);
    }

    [Fact]
    public void Write_ReproducesTheSourceBytes_IncludingAnUnresolvableReference()
    {
        MngrpFilePair source = BuildSample(unresolvableOffset: 9);

        MngrpFilePair written = MngrpArchive.Read(source, _encoding).Write();

        Assert.Equal(source.Content, written.Content);
        Assert.Equal(source.Header, written.Header);
    }

    [Fact]
    public void Write_RepointsReferences_WhenAnEarlierBlockChangesLength()
    {
        MngrpArchive archive = MngrpArchive.Read(BuildSample(unresolvableOffset: null), _encoding);
        MngrpTextBlockSection blocks = archive.GetSection<MngrpTextBlockSection>(FirstBlockSlot);
        blocks.Blocks[0].Texts[0] = new MngrpText("Alpha grown much longer");

        MngrpArchive reloaded = MngrpArchive.Read(archive.Write(), _encoding);

        MngrpTextBlockMapSection map = reloaded.SectionsOfType<MngrpTextBlockMapSection>().Single();
        MngrpTextBlockSection reloadedBlocks = reloaded.GetSection<MngrpTextBlockSection>(FirstBlockSlot);
        Assert.Equal(1, map.References[1].BlockIndex);
        Assert.Equal("Beta", reloadedBlocks.Blocks[map.References[1].BlockIndex!.Value].Texts[0].Value);
        Assert.Equal("Alpha grown much longer", reloadedBlocks.Blocks[0].Texts[0].Value);
    }

    [Fact]
    public void Read_KeepsEveryTextOfABlock()
    {
        MngrpArchive archive = MngrpArchive.Read(BuildSample(unresolvableOffset: null), _encoding);

        MngrpTextBlock block = archive.GetSection<MngrpTextBlockSection>(FirstBlockSlot).Blocks[0];

        Assert.Equal(["Alpha", "Caption"], block.Texts.Select(text => text.Value));
        Assert.Equal([1, 2, 3], new[] { block.OriginId, block.LeftId, block.RightId });
    }

    /// <summary>
    /// Builds two block sections plus a map referencing them. <paramref name="unresolvableOffset"/>
    /// replaces the last reference's offset with one that matches no block start.
    /// </summary>
    private MngrpFilePair BuildSample(Int32? unresolvableOffset)
    {
        Byte[] firstBlock = MngrpFileBuilder.TextBlock(1, 2, 3, _encoding.GetBytes("Alpha"), _encoding.GetBytes("Caption"));
        Byte[] secondBlock = MngrpFileBuilder.TextBlock(4, 5, 6, _encoding.GetBytes("Beta"));

        return MngrpFileBuilder.Build(
            (FirstBlockSlot, [.. firstBlock, .. secondBlock]),
            (SecondBlockSlot, MngrpFileBuilder.TextBlock(7, 8, 9, _encoding.GetBytes("Gamma"))),
            (MapSlot, MngrpFileBuilder.TextBlockMap((0, 0), (0, firstBlock.Length), (1, unresolvableOffset ?? 0))));
    }
}
