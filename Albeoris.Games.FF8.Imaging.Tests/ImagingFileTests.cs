using System.Buffers.Binary;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Albeoris.Games.FF8.Imaging.Map;
using Albeoris.Games.FF8.Imaging.Mim;
using Albeoris.Games.FF8.Imaging.MimMap;
using Albeoris.Games.FF8.Imaging.Tdw;
using Albeoris.Games.FF8.Imaging.Tex;
using Albeoris.Games.FF8.Imaging.Tim;
using Albeoris.Games.FF8.Imaging.Workspace;
using ImageMagick;
using Xunit;

namespace Albeoris.Games.FF8.Imaging.Tests;

public sealed class ImagingFileTests
{
    [Fact]
    public void Rgba32_HasFourByteRgbaMemoryLayout()
    {
        Rgba32[] colors = [new(1, 2, 3, 4)];

        Assert.Equal([1, 2, 3, 4], MemoryMarshal.AsBytes(colors.AsSpan()).ToArray());
    }

    [Theory]
    [InlineData(WorkspaceContainerFormat.Directory)]
    [InlineData(WorkspaceContainerFormat.ZipArchive)]
    public void TimWorkspace_RoundTripsDuplicatePaletteColorsAndMultipleImages(WorkspaceContainerFormat format)
    {
        UInt16[] colors = new UInt16[32];
        colors[1] = 0x001F;
        colors[2] = 0x001F;
        colors[17] = 0x03E0;
        colors[18] = 0x03E0;
        TimClut clut = new(4, 5, 16, 2, colors);
        TimImage indexed = new(TimPixelMode.Indexed4, 1, 2, 1, 1, [1, 2, 2, 1], clut, TimFlags.Reserved25);
        TimImage direct = new(TimPixelMode.Direct16, 3, 4, 1, 1, [0x1F, 0x00]);
        TimFile source = new([indexed, direct]);
        String workspace = GetTemporaryWorkspacePath(format);

        TimWorkspaceExporter.Export(source, new WorkspaceExportSpec(new WorkspaceLocation(workspace, format))
        {
            GeneratePreviews = false,
            DefaultPaletteIndex = 1,
        });
        TimFile imported = TimWorkspaceImporter.Import(
            new WorkspaceImportSpec(new WorkspaceLocation(workspace, format)));
        DeleteWorkspace(workspace, format);

        Assert.Equal(source.Write(), imported.Write());
    }

    [Fact]
    public void TimWorkspace_ExportsNumberedFilesClutAndIndividualPalettes()
    {
        UInt16[] colors = new UInt16[32];
        colors[1] = 0x001F;
        colors[17] = 0x03E0;
        TimImage indexed = new(TimPixelMode.Indexed4, 0, 0, 1, 1, [1, 1, 1, 1], new TimClut(0, 0, 16, 2, colors));
        TimImage direct = new(TimPixelMode.Direct16, 0, 0, 1, 1, [0x1F, 0x00]);
        String workspace = GetTemporaryWorkspacePath(WorkspaceContainerFormat.Directory);

        TimWorkspaceExporter.Export(new TimFile([indexed, direct]), new WorkspaceExportSpec(new WorkspaceLocation(workspace, WorkspaceContainerFormat.Directory)));

        Assert.Equal(["001.png", "001_clut.png", "001_previews.png", "002.png", "manifest.json"], Directory.GetFiles(workspace).Select(Path.GetFileName).Order(StringComparer.Ordinal));
        Assert.Equal(["01.png", "02.png"], Directory.GetFiles(Path.Combine(workspace, "palettes", "001")).Select(Path.GetFileName).Order(StringComparer.Ordinal));
        Byte[] indexedPng = File.ReadAllBytes(Path.Combine(workspace, "001.png"));
        Assert.Equal(3, indexedPng[25]);
        using MagickImage clut = new(Path.Combine(workspace, "001_clut.png"));
        using MagickImage palette = new(Path.Combine(workspace, "palettes", "001", "01.png"));
        using MagickImage previews = new(Path.Combine(workspace, "001_previews.png"));
        Assert.Equal(16U, clut.Width);
        Assert.Equal(2U, clut.Height);
        Assert.Equal(16U, palette.Width);
        Assert.Equal(1U, palette.Height);
        Assert.Equal(4U, previews.Width);
        Assert.Equal(2U, previews.Height);
        using IPixelCollection<Byte> previewPixels = previews.GetPixels();
        Byte[] previewRgba = previewPixels.ToByteArray(PixelMapping.RGBA) ?? throw new InvalidDataException("The PNG decoder did not return pixel data.");
        Assert.All(previewRgba[..16].Chunk(4), pixel => Assert.Equal([255, 0, 0, 255], pixel));
        Assert.All(previewRgba[16..].Chunk(4), pixel => Assert.Equal([0, 255, 0, 255], pixel));
        DeleteWorkspace(workspace, WorkspaceContainerFormat.Directory);
    }

    [Fact]
    public void TimWorkspace_RoundTripsTrailingClutColorsOutsideCompletePalettes()
    {
        UInt16[] colors = new UInt16[272];
        colors[1] = 0x001F;
        colors[^1] = 0x03E0;
        TimFile source = new([new TimImage(TimPixelMode.Indexed8, 0, 0, 1, 1, [1, 1], new TimClut(0, 0, 272, 1, colors))]);
        String workspace = GetTemporaryWorkspacePath(WorkspaceContainerFormat.Directory);

        TimWorkspaceExporter.Export(source, new WorkspaceExportSpec(new WorkspaceLocation(workspace, WorkspaceContainerFormat.Directory)) { GeneratePreviews = false });
        TimFile imported = TimWorkspaceImporter.Import(new WorkspaceImportSpec(new WorkspaceLocation(workspace, WorkspaceContainerFormat.Directory)));

        using MagickImage clut = new(Path.Combine(workspace, "001_clut.png"));
        Assert.Equal(256U, clut.Width);
        Assert.Equal(1U, clut.Height);
        Assert.Equal(source.Write(), imported.Write());
        DeleteWorkspace(workspace, WorkspaceContainerFormat.Directory);
    }

    [Fact]
    public void TdwWorkspace_RoundTripsWidthsAndTim()
    {
        TimImage image = new(TimPixelMode.Direct16, 0, 0, 1, 1, [0x1F, 0x00]);
        TdwFile source = new([1, 12, 3, 15], new TimFile([image]));
        String workspace = GetTemporaryWorkspacePath(WorkspaceContainerFormat.ZipArchive);

        TdwWorkspaceExporter.Export(source, new WorkspaceExportSpec(
            new WorkspaceLocation(workspace, WorkspaceContainerFormat.ZipArchive)) { GeneratePreviews = false });
        TdwFile imported = TdwWorkspaceImporter.Import(new WorkspaceImportSpec(
            new WorkspaceLocation(workspace, WorkspaceContainerFormat.ZipArchive)));
        DeleteWorkspace(workspace, WorkspaceContainerFormat.ZipArchive);

        Assert.Equal(source.Write(), imported.Write());
    }

    [Fact]
    public void TdwWorkspace_RoundTripsAWidthOnlyFile()
    {
        TdwFile source = new([1, 2], null);
        String workspace = GetTemporaryWorkspacePath(WorkspaceContainerFormat.Directory);

        TdwWorkspaceExporter.Export(source, new WorkspaceExportSpec(
            new WorkspaceLocation(workspace, WorkspaceContainerFormat.Directory)) { GeneratePreviews = false });
        TdwFile imported = TdwWorkspaceImporter.Import(new WorkspaceImportSpec(
            new WorkspaceLocation(workspace, WorkspaceContainerFormat.Directory)));
        DeleteWorkspace(workspace, WorkspaceContainerFormat.Directory);

        Assert.Equal(source.Write(), imported.Write());
    }

    [Fact]
    public void TimWorkspace_RequiresExactPaletteColorsOrMapsThemWhenRequested()
    {
        UInt16[] redColors = new UInt16[16];
        redColors[1] = 0x001F;
        UInt16[] blueColors = new UInt16[16];
        blueColors[1] = 0x7C00;
        TimFile red = new([new TimImage(
            TimPixelMode.Indexed4,
            0,
            0,
            1,
            1,
            [1, 1, 1, 1],
            new TimClut(0, 0, 16, 1, redColors))]);
        TimFile blue = new([new TimImage(
            TimPixelMode.Indexed4,
            0,
            0,
            1,
            1,
            [1, 1, 1, 1],
            new TimClut(0, 0, 16, 1, blueColors))]);
        String redWorkspace = GetTemporaryWorkspacePath(WorkspaceContainerFormat.Directory);
        String blueWorkspace = GetTemporaryWorkspacePath(WorkspaceContainerFormat.Directory);
        TimWorkspaceExporter.Export(red, new WorkspaceExportSpec(
            new WorkspaceLocation(redWorkspace, WorkspaceContainerFormat.Directory)) { GeneratePreviews = false });
        TimWorkspaceExporter.Export(blue, new WorkspaceExportSpec(
            new WorkspaceLocation(blueWorkspace, WorkspaceContainerFormat.Directory)) { GeneratePreviews = false });
        File.Copy(
            Path.Combine(blueWorkspace, "001_clut.png"),
            Path.Combine(redWorkspace, "001_clut.png"),
            overwrite: true);

        WorkspaceLocation location = new(redWorkspace, WorkspaceContainerFormat.Directory);
        Assert.Throws<InvalidDataException>(() => TimWorkspaceImporter.Import(new WorkspaceImportSpec(location)));
        TimFile imported = TimWorkspaceImporter.Import(new WorkspaceImportSpec(location)
        {
            MissingPaletteColor = PaletteColorHandling.FindNearest,
            DistanceAlgorithm = PaletteDistanceAlgorithm.Oklab,
        });
        DeleteWorkspace(redWorkspace, WorkspaceContainerFormat.Directory);
        DeleteWorkspace(blueWorkspace, WorkspaceContainerFormat.Directory);

        Assert.All(Assert.Single(imported.Images).Pixels, index => Assert.Equal(1, index));
    }

    [Fact]
    public void Tim_RoundTripsAndDecodesLowNibbleFirst()
    {
        UInt16[] colors = new UInt16[16];
        colors[1] = 0x001F;
        colors[2] = 0x03E0;
        TimClut clut = new(0, 0, 16, 1, colors);
        TimImage sourceImage = new(TimPixelMode.Indexed4, 0, 0, 1, 1, [1, 2, 2, 1], clut);
        TimFile source = new([sourceImage]);

        Byte[] content = source.Write();
        TimFile parsed = TimFile.Read(content);
        TimImage parsedImage = Assert.Single(parsed.Images);
        PixelImage image = parsedImage.Decode();

        Assert.Equal(content, parsed.Write());
        Assert.Single(parsedImage.Palettes);
        Assert.Equal(16, parsedImage.Palettes[0].Count);
        Assert.Equal(4, image.Width);
        Assert.Equal(new Rgba32(255, 0, 0), image[0, 0]);
        Assert.Equal(new Rgba32(0, 255, 0), image[1, 0]);
    }

    [Fact]
    public void Clut_CreatesAPaletteAcrossMultipleRows()
    {
        TimClut clut = new(0, 0, 16, 16, new UInt16[256]);

        TimPalette palette = Assert.Single(clut.GetPalettes(TimPixelMode.Indexed8));

        Assert.Equal(256, palette.Count);
    }

    [Fact]
    public void Tdw_RoundTripsExpandedWidthsAndEmbeddedTim()
    {
        TimImage timImage = new(TimPixelMode.Direct16, 0, 0, 1, 1, [0x1F, 0x00]);
        TdwFile source = new([1, 12, 3, 15], new TimFile([timImage]));

        Byte[] content = source.Write();
        TdwFile parsed = TdwFile.Read(content);

        Assert.Equal([1, 12, 3, 15], parsed.Widths);
        Assert.Single(parsed.Images);
        Assert.Equal(content, parsed.Write());
    }

    [Fact]
    public void Tim_Reads4BppPixelsFromGeometryAndPreservesAnIncorrectDeclaredSize()
    {
        TimClut clut = new(0, 0, 16, 1, new UInt16[16]);
        TimFile source = new([new TimImage(
            TimPixelMode.Indexed4,
            0,
            0,
            1,
            2,
            [0, 1, 2, 3, 4, 5, 6, 7],
            clut)]);
        Byte[] content = source.Write();

        Int32 imageHeaderOffset = 8 + 12 + clut.Colors.Length * sizeof(UInt16);
        BitConverter.GetBytes(14).CopyTo(content, imageHeaderOffset);

        TimFile parsed = TimFile.Read(content);
        TimImage image = Assert.Single(parsed.Images);

        Assert.Equal([0, 1, 2, 3, 4, 5, 6, 7], image.Pixels);
        Assert.Equal(14, image.DeclaredBlockSize);
        Assert.Equal(content, parsed.Write());
    }

    [Fact]
    public void Tim_RejectsBytesThatDoNotStartAnotherImage()
    {
        TimImage image = new(TimPixelMode.Direct16, 0, 0, 1, 1, [0x1F, 0x00]);
        Byte[] content = [.. new TimFile([image]).Write(), 0, 0, 0, 0, 0, 0, 0, 0];

        Assert.Throws<InvalidDataException>(() => TimFile.Read(content));
    }

    [Fact]
    public void Tex_RoundTripsAndAppliesPaletteColorKeyInSoftware()
    {
        TexHeader header = CreateIndexedTexHeader(2, 1, 2);
        header.ColorKeyIndex = 0;
        UInt32[] palette = [0xFFFF0000, 0xFF00FF00];
        TexFile source = new(header, [palette], [0, 1]);

        Byte[] content = source.Write();
        TexFile parsed = TexFile.Read(content);
        PixelImage image = parsed.Decode();

        Assert.Equal(content, parsed.Write());
        Assert.Equal(new Rgba32(255, 0, 0, 0), image[0, 0]);
        Assert.Equal(new Rgba32(0, 255, 0), image[1, 0]);
    }

    [Fact]
    public void Tex_RoundTripsUnknownFlagBits()
    {
        TexHeader header = CreateDirectTexHeader(1, 1);
        header.PaletteFlags = (TexPaletteFlags)(1 << 4);
        header.ConversionFlags = (TexConversionFlags)(1 << 5);
        header.ColorKeyArrayFlags = (TexColorKeyArrayFlags)(1 << 6);
        TexFile source = new(header, [], [0, 0]);

        TexFile parsed = TexFile.Read(source.Write());

        Assert.Equal(header.PaletteFlags, parsed.Header.PaletteFlags);
        Assert.Equal(header.ConversionFlags, parsed.Header.ConversionFlags);
        Assert.Equal(header.ColorKeyArrayFlags, parsed.Header.ColorKeyArrayFlags);
    }

    [Theory]
    [InlineData(WorkspaceContainerFormat.Directory)]
    [InlineData(WorkspaceContainerFormat.ZipArchive)]
    public void TexWorkspace_RoundTripsIndexedPixelsPalettesAndColorKeys(WorkspaceContainerFormat format)
    {
        TexHeader header = CreateIndexedTexHeader(4, 1, 3);
        header.PaletteCount = 2;
        header.PaletteSize = 6;
        header.ColorKeyIndex = 0;
        header.ColorKeyArrayFlags = TexColorKeyArrayFlags.HasColorKeyArray;
        header.RuntimeData24 = 0x12345678;
        header.Unknown59 = 42;
        UInt32[] firstPalette = [0xFFFF0000, 0xFFFF0000, 0xFF00FF00];
        UInt32[] secondPalette = [0xFF0000FF, 0xFF0000FF, 0xFFFFFFFF];
        TexFile source = new(header, [firstPalette, secondPalette], [0, 1, 1, 2], [1, 2]);
        String workspace = GetTemporaryWorkspacePath(format);

        TexWorkspaceExporter.Export(source, new WorkspaceExportSpec(new WorkspaceLocation(workspace, format))
        {
            DefaultPaletteIndex = 1,
        });
        if (format == WorkspaceContainerFormat.Directory)
        {
            Assert.True(File.Exists(Path.Combine(workspace, "palettes.png")));
            Assert.Equal(["01.png", "02.png"], Directory.GetFiles(Path.Combine(workspace, "palettes", "001")).Select(Path.GetFileName).Order(StringComparer.Ordinal));
        }
        TexFile imported = TexWorkspaceImporter.Import(
            new WorkspaceImportSpec(new WorkspaceLocation(workspace, format)));
        DeleteWorkspace(workspace, format);

        Assert.Equal(source.Write(), imported.Write());
    }

    [Theory]
    [InlineData(WorkspaceContainerFormat.Directory)]
    [InlineData(WorkspaceContainerFormat.ZipArchive)]
    public void TexWorkspace_RoundTripsDirectRgb555A1(WorkspaceContainerFormat format)
    {
        TexHeader header = CreateDirectTexHeader(2, 1);
        header.ColorKeyIndex = 0x801F;
        header.RuntimeData54 = 0x10203040;
        TexFile source = new(header, [], [0x1F, 0x80, 0xE0, 0x03]);
        String workspace = GetTemporaryWorkspacePath(format);

        TexWorkspaceExporter.Export(source, new WorkspaceExportSpec(new WorkspaceLocation(workspace, format)));
        TexFile imported = TexWorkspaceImporter.Import(
            new WorkspaceImportSpec(new WorkspaceLocation(workspace, format)));
        DeleteWorkspace(workspace, format);

        Assert.Equal(source.Write(), imported.Write());
    }

    [Fact]
    public void TexWorkspace_DerivesDimensionsAndPalettePresenceFromPngFiles()
    {
        TexFile source = new(CreateDirectTexHeader(2, 1), [], [0x1F, 0x80, 0xE0, 0x03]);
        String workspace = GetTemporaryWorkspacePath(WorkspaceContainerFormat.Directory);
        TexWorkspaceExporter.Export(source, new WorkspaceExportSpec(
            new WorkspaceLocation(workspace, WorkspaceContainerFormat.Directory)));
        String imagePath = Path.Combine(workspace, "image.png");
        using (MagickImage image = new(imagePath))
        {
            Byte[] rgba = GetRgbaPixels(image);
            PixelReadSettings settings = new(1, 2, StorageType.Char, PixelMapping.RGBA);
            using MagickImage reshaped = new(rgba, settings);
            reshaped.Write(imagePath);
        }

        TexFile imported = TexWorkspaceImporter.Import(new WorkspaceImportSpec(
            new WorkspaceLocation(workspace, WorkspaceContainerFormat.Directory)));
        DeleteWorkspace(workspace, WorkspaceContainerFormat.Directory);

        Assert.Equal(1, imported.Header.Width);
        Assert.Equal(2, imported.Header.Height);
        Assert.Equal(TexPaletteFlags.None, imported.Header.PaletteFlags);
        Assert.Empty(imported.Palettes);
    }

    [Fact]
    public void MapAndMim_RoundTripAndDecodeA4BitTileInSoftware()
    {
        MapTile tile = new() { PaletteId = 0, SourceX = 0, SourceY = 0 };
        MapFile map = new([tile], [0xFF, 0x7F]);
        UInt16[] colors = new UInt16[MimFile.PaletteCount * MimFile.ColorsPerPalette];
        colors[1] = 0x001F;
        Byte[] texture = new Byte[MimFile.AtlasStride * MimFile.TileSize];
        for (Int32 y = 0; y < MimFile.TileSize; y++)
            Array.Fill(texture, (Byte)0x11, y * MimFile.AtlasStride, MimFile.TileSize / 2);
        MimFile mim = new(new Byte[MimFile.HeaderSize], colors, texture);

        MapFile parsedMap = MapFile.Read(map.Write());
        MimFile parsedMim = MimFile.Read(mim.Write());
        PixelImage image = parsedMim.DecodeTile(parsedMap.Tiles.Single());

        Assert.Equal(map.Write(), parsedMap.Write());
        Assert.Equal(mim.Write(), parsedMim.Write());
        Assert.All(image.Pixels, pixel => Assert.Equal(new Rgba32(255, 0, 0), pixel));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MimMapWorkspace_RoundTripsBothLayoutsAsOpenRaster(Boolean alternateLayout)
    {
        Int32 headerSize = alternateLayout ? 0 : MimFile.HeaderSize;
        Int32 textureStride = alternateLayout ? MimFile.AlternateAtlasStride : MimFile.AtlasStride;
        Byte[] header = Enumerable.Range(0, headerSize).Select(index => (Byte)index).ToArray();
        UInt16[] colors = Enumerable.Range(0, MimFile.PaletteCount * MimFile.ColorsPerPalette).Select(index => (UInt16)index).ToArray();
        Byte[] texture = Enumerable.Range(0, textureStride * MimFile.AtlasHeight).Select(index => (Byte)index).ToArray();
        MimMapFile source = new(new MimFile(header, colors, texture), alternateLayout ? MapFile.Read(CreateAlternateMap()) : CreateStandardMap());
        String workspace = GetTemporaryWorkspacePath(WorkspaceContainerFormat.OpenRaster);
        WorkspaceLocation location = new(workspace, WorkspaceContainerFormat.OpenRaster);

        MimMapWorkspaceExporter.Export(source, new WorkspaceExportSpec(location));
        using (ZipArchive archive = ZipFile.OpenRead(workspace))
        {
            Assert.Equal("mimetype", archive.Entries[0].FullName);
            Assert.Equal(archive.Entries[0].Length, archive.Entries[0].CompressedLength);
            Assert.Equal("image/openraster", ReadEntryText(archive, "mimetype"));
            Assert.NotNull(archive.GetEntry("stack.xml"));
            Assert.NotNull(archive.GetEntry("mergedimage.png"));
            Assert.NotNull(archive.GetEntry("Thumbnails/thumbnail.png"));
            Assert.Null(archive.GetEntry("preview.png"));
            Assert.NotNull(archive.GetEntry("data/palettes.png"));
            Assert.NotNull(archive.GetEntry("data/layers/source-mim.png"));
            Assert.NotNull(archive.GetEntry("data/layers/source-map.png"));
        }
        Assert.Equal(0, BinaryPrimitives.ReadUInt16LittleEndian(File.ReadAllBytes(workspace).AsSpan(8)));
        MimMapFile imported = MimMapWorkspaceImporter.Import(new WorkspaceImportSpec(location));

        Assert.Equal(source.Mim.Write(), imported.Mim.Write());
        Assert.Equal(source.Map.Write(), imported.Map.Write());
        DeleteWorkspace(workspace, WorkspaceContainerFormat.OpenRaster);
    }

    [Fact]
    public void MimMapWorkspace_ImportsEditedComposedTextureAndRequiresBothSources()
    {
        UInt16[] colors = new UInt16[MimFile.PaletteCount * MimFile.ColorsPerPalette];
        colors[1] = new Rgba32(255, 0, 0).ToPsxBgr555();
        colors[2] = new Rgba32(0, 255, 0).ToPsxBgr555();
        Byte[] texture = new Byte[MimFile.AtlasStride * MimFile.AtlasHeight];
        for (Int32 y = 0; y < MimMapTileSize; y++)
            Array.Fill(texture, (Byte)0x11, y * MimFile.AtlasStride, MimMapTileSize / 2);
        MimMapFile source = new(new MimFile(new Byte[MimFile.HeaderSize], colors, texture), CreateStandardMap());
        String workspace = GetTemporaryWorkspacePath(WorkspaceContainerFormat.OpenRaster);
        WorkspaceLocation location = new(workspace, WorkspaceContainerFormat.OpenRaster);
        MimMapWorkspaceExporter.Export(source, new WorkspaceExportSpec(location));
        String texturePath = GetSingleLayerPath(workspace);
        EditPng(workspace, texturePath, image =>
        {
            Byte[] rgba = GetRgbaPixels(image);
            rgba[0] = 0;
            rgba[1] = Byte.MaxValue;
            rgba[2] = 0;
            rgba[3] = Byte.MaxValue;
            PixelReadSettings settings = new(image.Width, image.Height, StorageType.Char, PixelMapping.RGBA);
            using MagickImage edited = new(rgba, settings);
            return edited.ToByteArray(MagickFormat.Png);
        });

        MimMapFile imported = MimMapWorkspaceImporter.Import(new WorkspaceImportSpec(location));
        Assert.Equal(new Rgba32(0, 255, 0), imported.Mim.DecodeTile(imported.Map.Tiles.Single())[0, 0]);
        DeleteEntry(workspace, "data/albeoris/source.map");
        DeleteEntry(workspace, "data/layers/source-map.png");
        Assert.Throws<FileNotFoundException>(() => MimMapWorkspaceImporter.Import(new WorkspaceImportSpec(location)));
        DeleteWorkspace(workspace, WorkspaceContainerFormat.OpenRaster);
        MimMapWorkspaceExporter.Export(source, new WorkspaceExportSpec(location));
        DeleteEntry(workspace, "data/albeoris/source.mim");
        DeleteEntry(workspace, "data/layers/source-mim.png");
        Assert.Throws<FileNotFoundException>(() => MimMapWorkspaceImporter.Import(new WorkspaceImportSpec(location)));
        DeleteWorkspace(workspace, WorkspaceContainerFormat.OpenRaster);
    }

    [Fact]
    public void MimMapWorkspace_ImportsEditedPaletteLayer()
    {
        MimMapFile source = CreateSingleColorMimMap(new Rgba32(255, 0, 0));
        String workspace = GetTemporaryWorkspacePath(WorkspaceContainerFormat.OpenRaster);
        WorkspaceLocation location = new(workspace, WorkspaceContainerFormat.OpenRaster);
        MimMapWorkspaceExporter.Export(source, new WorkspaceExportSpec(location));
        EditPng(workspace, "data/palettes.png", image =>
        {
            Byte[] rgba = GetRgbaPixels(image);
            rgba[4] = 0;
            rgba[5] = Byte.MaxValue;
            rgba[6] = 0;
            rgba[7] = Byte.MaxValue;
            using MagickImage edited = new(rgba, new PixelReadSettings(image.Width, image.Height, StorageType.Char, PixelMapping.RGBA));
            return edited.ToByteArray(MagickFormat.Png);
        });

        MimMapFile imported = MimMapWorkspaceImporter.Import(new WorkspaceImportSpec(location));

        Assert.Equal(new Rgba32(0, 255, 0), imported.Mim.GetColor(0, 1));
        Assert.Equal(new Rgba32(0, 255, 0), imported.Mim.DecodeTile(imported.Map.Tiles.Single())[0, 0]);
        Assert.Equal(source.Mim.TextureData, imported.Mim.TextureData);
        DeleteWorkspace(workspace, WorkspaceContainerFormat.OpenRaster);
    }

    [Fact]
    public void MimMapWorkspace_ExportsCompositedPreviewAndVisibleLayers()
    {
        UInt16[] colors = new UInt16[MimFile.PaletteCount * MimFile.ColorsPerPalette];
        colors[1] = new Rgba32(255, 0, 0).ToPsxBgr555();
        colors[2] = new Rgba32(0, 255, 0).ToPsxBgr555();
        Byte[] texture = new Byte[MimFile.AtlasStride * MimFile.AtlasHeight];
        for (Int32 y = 0; y < MimMapTileSize; y++)
        {
            Array.Fill(texture, (Byte)0x11, y * MimFile.AtlasStride, MimMapTileSize / 2);
            Array.Fill(texture, (Byte)0x22, y * MimFile.AtlasStride + MimMapTileSize / 2, MimMapTileSize / 2);
        }
        MapTile lower = new() { X = 0, Y = 0, PackedTexture = 0x10, PackedPalette = 0, SourceX = 0, SourceY = 0, PackedLayer = 0, BlendMode = MapBlendMode.None, Parameter = Byte.MaxValue, State = 0 };
        MapTile upper = new() { X = 0, Y = 0, PackedTexture = 0x10, PackedPalette = 0, SourceX = MimMapTileSize, SourceY = 0, PackedLayer = 2, BlendMode = MapBlendMode.None, Parameter = Byte.MaxValue, State = 0 };
        MimMapFile source = new(new MimFile(new Byte[MimFile.HeaderSize], colors, texture), new MapFile([lower, upper], [0xFF, 0x7F]));
        String workspace = GetTemporaryWorkspacePath(WorkspaceContainerFormat.OpenRaster);
        WorkspaceLocation location = new(workspace, WorkspaceContainerFormat.OpenRaster);

        MimMapWorkspaceExporter.Export(source, new WorkspaceExportSpec(location));

        using (ZipArchive archive = ZipFile.OpenRead(workspace))
        {
            using MagickImage preview = new(ReadEntry(archive, "mergedimage.png"));
            Assert.Equal(new Byte[] { 0, Byte.MaxValue, 0, Byte.MaxValue }, GetRgbaPixels(preview)[..4]);
            String stack = ReadEntryText(archive, "stack.xml");
            Assert.Equal(2, stack.Split("visibility=\"visible\"", StringSplitOptions.None).Length - 1);
        }
        DeleteWorkspace(workspace, WorkspaceContainerFormat.OpenRaster);
    }

    [Fact]
    public void MimMapWorkspace_RendersStpColorsAsOpaqueAndPreservesPaletteBits()
    {
        Rgba32 storedColor = new(255, 0, 0, 128);
        MimMapFile source = CreateSingleColorMimMap(storedColor);
        String workspace = GetTemporaryWorkspacePath(WorkspaceContainerFormat.OpenRaster);
        WorkspaceLocation location = new(workspace, WorkspaceContainerFormat.OpenRaster);

        MimMapWorkspaceExporter.Export(source, new WorkspaceExportSpec(location));

        using (ZipArchive archive = ZipFile.OpenRead(workspace))
        {
            using MagickImage texture = new(ReadEntry(archive, GetSingleLayerPath(workspace)));
            using MagickImage mergedImage = new(ReadEntry(archive, "mergedimage.png"));
            using MagickImage palette = new(ReadEntry(archive, "data/palettes.png"));
            Assert.Equal(new Byte[] { 255, 0, 0, 255 }, GetRgbaPixels(texture)[..4]);
            Assert.Equal(new Byte[] { 255, 0, 0, 255 }, GetRgbaPixels(mergedImage)[..4]);
            Assert.Equal(new Byte[] { 255, 0, 0, 128 }, GetRgbaPixels(palette)[4..8]);
        }
        MimMapFile imported = MimMapWorkspaceImporter.Import(new WorkspaceImportSpec(location));
        Assert.Equal(source.Mim.Write(), imported.Mim.Write());
        Assert.Equal(source.Map.Write(), imported.Map.Write());
        DeleteWorkspace(workspace, WorkspaceContainerFormat.OpenRaster);
    }

    [Fact]
    public void MimMapWorkspace_MapsOpaqueTextureEditsToStpPaletteColors()
    {
        UInt16[] colors = new UInt16[MimFile.PaletteCount * MimFile.ColorsPerPalette];
        colors[1] = new Rgba32(255, 0, 0, 128).ToPsxBgr555();
        colors[2] = new Rgba32(0, 255, 0, 128).ToPsxBgr555();
        Byte[] texture = new Byte[MimFile.AtlasStride * MimFile.AtlasHeight];
        for (Int32 y = 0; y < MimMapTileSize; y++)
            Array.Fill(texture, (Byte)0x11, y * MimFile.AtlasStride, MimMapTileSize / 2);
        MimMapFile source = new(new MimFile(new Byte[MimFile.HeaderSize], colors, texture), CreateStandardMap());
        String workspace = GetTemporaryWorkspacePath(WorkspaceContainerFormat.OpenRaster);
        WorkspaceLocation location = new(workspace, WorkspaceContainerFormat.OpenRaster);
        MimMapWorkspaceExporter.Export(source, new WorkspaceExportSpec(location));
        EditPng(workspace, GetSingleLayerPath(workspace), image =>
        {
            Byte[] rgba = GetRgbaPixels(image);
            rgba[0] = 0;
            rgba[1] = Byte.MaxValue;
            rgba[2] = 0;
            rgba[3] = Byte.MaxValue;
            using MagickImage edited = new(rgba, new PixelReadSettings(image.Width, image.Height, StorageType.Char, PixelMapping.RGBA));
            return edited.ToByteArray(MagickFormat.Png);
        });

        MimMapFile imported = MimMapWorkspaceImporter.Import(new WorkspaceImportSpec(location));

        Assert.Equal(colors, imported.Mim.Colors);
        Assert.Equal(new Rgba32(0, 255, 0), imported.Mim.DecodeTile(imported.Map.Tiles.Single())[0, 0]);
        DeleteWorkspace(workspace, WorkspaceContainerFormat.OpenRaster);
    }

    [Fact]
    public void MimMapWorkspace_SynthesizesMissingPaletteFromRgbLayer()
    {
        MimMapFile source = CreateSingleColorMimMap(new Rgba32(255, 0, 0));
        String workspace = GetTemporaryWorkspacePath(WorkspaceContainerFormat.OpenRaster);
        WorkspaceLocation location = new(workspace, WorkspaceContainerFormat.OpenRaster);
        MimMapWorkspaceExporter.Export(source, new WorkspaceExportSpec(location));
        String texturePath = GetSingleLayerPath(workspace);
        EditPng(workspace, texturePath, image =>
        {
            Byte[] rgba = GetRgbaPixels(image);
            rgba[0] = 0;
            rgba[1] = 0;
            rgba[2] = Byte.MaxValue;
            rgba[3] = Byte.MaxValue;
            using MagickImage edited = new(rgba, new PixelReadSettings(image.Width, image.Height, StorageType.Char, PixelMapping.RGBA));
            return edited.ToByteArray(MagickFormat.Png);
        });
        DeleteEntry(workspace, "data/palettes.png");

        MimMapFile imported = MimMapWorkspaceImporter.Import(new WorkspaceImportSpec(location));

        Assert.Equal(new Rgba32(0, 0, 255), imported.Mim.DecodeTile(imported.Map.Tiles.Single())[0, 0]);
        Assert.Contains(new Rgba32(0, 0, 255), Enumerable.Range(0, 16).Select(index => imported.Mim.GetColor(0, index)));
        DeleteWorkspace(workspace, WorkspaceContainerFormat.OpenRaster);
    }

    [Fact]
    public void MimMapWorkspace_RoundTripsFromStandardLayersAfterEditorDropsPrivateEntries()
    {
        MimMapFile source = CreateSingleColorMimMap(new Rgba32(255, 0, 0));
        String workspace = GetTemporaryWorkspacePath(WorkspaceContainerFormat.OpenRaster);
        WorkspaceLocation location = new(workspace, WorkspaceContainerFormat.OpenRaster);
        MimMapWorkspaceExporter.Export(source, new WorkspaceExportSpec(location));
        String texturePath = GetSingleLayerPath(workspace);
        DeleteEntry(workspace, "data/albeoris/source.mim");
        DeleteEntry(workspace, "data/albeoris/source.map");
        DeleteEntry(workspace, "data/albeoris/manifest.json");
        RenameEntry(workspace, "data/layers/source-mim.png", "data/layer0.png");
        RenameEntry(workspace, "data/layers/source-map.png", "data/layer1.png");
        RenameEntry(workspace, "data/palettes.png", "data/layer2.png");
        RenameEntry(workspace, texturePath, "data/layer3.png");
        ReplaceEntryText(workspace, "stack.xml", text => text.Replace("data/layers/source-mim.png", "data/layer0.png", StringComparison.Ordinal).Replace("data/layers/source-map.png", "data/layer1.png", StringComparison.Ordinal).Replace("data/palettes.png", "data/layer2.png", StringComparison.Ordinal).Replace(texturePath, "data/layer3.png", StringComparison.Ordinal));

        MimMapFile imported = MimMapWorkspaceImporter.Import(new WorkspaceImportSpec(location));

        Assert.Equal(source.Mim.Write(), imported.Mim.Write());
        Assert.Equal(source.Map.Write(), imported.Map.Write());
        DeleteWorkspace(workspace, WorkspaceContainerFormat.OpenRaster);
    }

    private const Int32 MimMapTileSize = 16;

    private static MimMapFile CreateSingleColorMimMap(Rgba32 color)
    {
        UInt16[] colors = new UInt16[MimFile.PaletteCount * MimFile.ColorsPerPalette];
        colors[1] = color.ToPsxBgr555();
        Byte[] texture = new Byte[MimFile.AtlasStride * MimFile.AtlasHeight];
        for (Int32 y = 0; y < MimMapTileSize; y++)
            Array.Fill(texture, (Byte)0x11, y * MimFile.AtlasStride, MimMapTileSize / 2);
        return new MimMapFile(new MimFile(new Byte[MimFile.HeaderSize], colors, texture), CreateStandardMap());
    }

    private static MapFile CreateStandardMap()
    {
        MapTile tile = new() { X = 0, Y = 0, PackedTexture = 0x10, PackedPalette = 0, SourceX = 0, SourceY = 0, PackedLayer = 0, BlendMode = MapBlendMode.None, Parameter = Byte.MaxValue, State = 0 };
        return new MapFile([tile], [0xFF, 0x7F]);
    }

    private static Byte[] CreateAlternateMap()
    {
        Byte[] result = new Byte[MapTile.Size + sizeof(UInt16)];
        BinaryPrimitives.WriteUInt16LittleEndian(result.AsSpan(10), 0x10);
        result[14] = Byte.MaxValue;
        BinaryPrimitives.WriteUInt16LittleEndian(result.AsSpan(MapTile.Size), 0x7FFF);
        return result;
    }

    private static TexHeader CreateIndexedTexHeader(Int32 width, Int32 height, Int32 colors)
    {
        TexHeader header = new()
        {
            PaletteFlags = TexPaletteFlags.HasPalette,
            PaletteCount = 1,
            ColorsPerPalette = colors,
            RepeatedColorsPerPalette = colors,
            PaletteSize = colors,
            Width = width,
            Height = height,
            BitDepth = 8,
            BitsPerIndex = 8,
            BitsPerPixel = 8,
            BytesPerPixel = 1,
            ReferenceAlpha = 255,
        };
        return header;
    }

    private static TexHeader CreateDirectTexHeader(Int32 width, Int32 height) => new()
    {
        Width = width,
        Height = height,
        BitDepth = 16,
        BitsPerPixel = 16,
        BytesPerPixel = 2,
        RedBits = 5,
        GreenBits = 5,
        BlueBits = 5,
        AlphaBits = 1,
        RedMask = 0x001F,
        GreenMask = 0x03E0,
        BlueMask = 0x7C00,
        AlphaMask = 0x8000,
        GreenShift = 5,
        BlueShift = 10,
        AlphaShift = 15,
        RedBitsConstant = 3,
        GreenBitsConstant = 3,
        BlueBitsConstant = 3,
        AlphaBitsConstant = 7,
        RedMaximum = 31,
        GreenMaximum = 31,
        BlueMaximum = 31,
        AlphaMaximum = 1,
        ReferenceAlpha = 255,
    };

    private static String GetTemporaryWorkspacePath(WorkspaceContainerFormat format)
    {
        String name = $"Albeoris.Imaging.Tests.{Guid.NewGuid():N}";
        return format switch
        {
            WorkspaceContainerFormat.Directory => Path.Combine(Path.GetTempPath(), name),
            WorkspaceContainerFormat.OpenRaster => Path.Combine(Path.GetTempPath(), name + ".ora"),
            _ => Path.Combine(Path.GetTempPath(), name + ".zip"),
        };
    }

    private static String GetSingleLayerPath(String workspace)
    {
        using ZipArchive archive = ZipFile.OpenRead(workspace);
        return Assert.Single(archive.Entries, entry => entry.FullName.StartsWith("data/layers/", StringComparison.Ordinal) && entry.FullName.EndsWith(".png", StringComparison.Ordinal) && !entry.Name.StartsWith("source-", StringComparison.Ordinal)).FullName;
    }

    private static String ReadEntryText(ZipArchive archive, String path)
    {
        ZipArchiveEntry entry = archive.GetEntry(path) ?? throw new FileNotFoundException(path);
        using StreamReader reader = new(entry.Open());
        return reader.ReadToEnd();
    }

    private static Byte[] ReadEntry(ZipArchive archive, String path)
    {
        ZipArchiveEntry entry = archive.GetEntry(path) ?? throw new FileNotFoundException(path);
        using MemoryStream output = new();
        using Stream input = entry.Open();
        input.CopyTo(output);
        return output.ToArray();
    }

    private static void EditPng(String workspace, String path, Func<MagickImage, Byte[]> edit)
    {
        Byte[] content;
        using (ZipArchive archive = ZipFile.OpenRead(workspace))
        {
            ZipArchiveEntry entry = archive.GetEntry(path) ?? throw new FileNotFoundException(path);
            using MemoryStream output = new();
            using Stream input = entry.Open();
            input.CopyTo(output);
            content = output.ToArray();
        }
        Byte[] edited;
        using (MagickImage image = new(content))
            edited = edit(image);
        using ZipArchive update = ZipFile.Open(workspace, ZipArchiveMode.Update);
        update.GetEntry(path)?.Delete();
        using Stream destination = update.CreateEntry(path, CompressionLevel.Optimal).Open();
        destination.Write(edited);
    }

    private static void DeleteEntry(String workspace, String path)
    {
        using ZipArchive archive = ZipFile.Open(workspace, ZipArchiveMode.Update);
        (archive.GetEntry(path) ?? throw new FileNotFoundException(path)).Delete();
    }

    private static void RenameEntry(String workspace, String sourcePath, String destinationPath)
    {
        using ZipArchive archive = ZipFile.Open(workspace, ZipArchiveMode.Update);
        ZipArchiveEntry source = archive.GetEntry(sourcePath) ?? throw new FileNotFoundException(sourcePath);
        using MemoryStream content = new();
        using (Stream input = source.Open())
            input.CopyTo(content);
        source.Delete();
        using Stream output = archive.CreateEntry(destinationPath, CompressionLevel.Optimal).Open();
        output.Write(content.ToArray());
    }

    private static void ReplaceEntryText(String workspace, String path, Func<String, String> replace)
    {
        String content;
        using (ZipArchive archive = ZipFile.OpenRead(workspace))
            content = ReadEntryText(archive, path);
        Byte[] updated = System.Text.Encoding.UTF8.GetBytes(replace(content));
        using ZipArchive destination = ZipFile.Open(workspace, ZipArchiveMode.Update);
        (destination.GetEntry(path) ?? throw new FileNotFoundException(path)).Delete();
        using Stream output = destination.CreateEntry(path, CompressionLevel.Optimal).Open();
        output.Write(updated);
    }

    private static Byte[] GetRgbaPixels(MagickImage image)
    {
        using IPixelCollection<Byte> pixels = image.GetPixels();
        return pixels.ToByteArray(PixelMapping.RGBA)
            ?? throw new InvalidDataException("The PNG decoder did not return pixel data.");
    }

    private static void DeleteWorkspace(String path, WorkspaceContainerFormat format)
    {
        if (format == WorkspaceContainerFormat.Directory)
            Directory.Delete(path, recursive: true);
        else
            File.Delete(path);
    }
}
