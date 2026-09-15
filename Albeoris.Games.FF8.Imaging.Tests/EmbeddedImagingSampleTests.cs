using System.IO.Compression;
using Albeoris.Games.FF8.Imaging.Map;
using Albeoris.Games.FF8.Imaging.MimMap;
using Albeoris.Games.FF8.Imaging.Tdw;
using Albeoris.Games.FF8.Imaging.Tex;
using Albeoris.Games.FF8.Imaging.Tim;
using Albeoris.Games.FF8.Imaging.Workspace;
using ImageMagick;
using Xunit;

namespace Albeoris.Games.FF8.Imaging.Tests;

public sealed class EmbeddedImagingSampleTests
{
    [Fact]
    public void MultiImageTim_ReadsDecodesAndRoundTripsWorkspace()
    {
        ImagingSample sample = ImagingSample.MultiImageTim;
        TimFile source = TimFile.Read(sample.Content);

        Assert.Equal(2, source.Images.Count);
        Assert.All(source.Images, image => Assert.True(image.CanDecode));
        Assert.All(source.Images, image => _ = image.Decode());
        Assert.Equal(sample.Content, source.Write());

        String workspace = GetWorkspacePath(WorkspaceContainerFormat.ZipArchive);
        try
        {
            WorkspaceLocation location = new(workspace, WorkspaceContainerFormat.ZipArchive);
            TimWorkspaceExporter.Export(source, new WorkspaceExportSpec(location));
            ValidateDefaultPaletteImagesMatchPreviews(workspace, source.Images);
            Assert.Equal(sample.Content, TimWorkspaceImporter.Import(new WorkspaceImportSpec(location)).Write());
        }
        finally
        {
            File.Delete(workspace);
        }
    }

    [Fact]
    public void JapaneseSystemFontTdw_ReadsEntireImageAndRoundTripsWorkspace()
    {
        ImagingSample sample = ImagingSample.JapaneseSystemFontTdw;
        TdwFile source = TdwFile.Read(sample.Content);
        TimImage image = Assert.Single(source.Images);

        Assert.Equal(TimPixelMode.Indexed4, image.PixelMode);
        Assert.Equal(256, image.PixelWidth);
        Assert.Equal(252, image.Height);
        Assert.Equal(64_512, image.Pixels.Length);
        Assert.Equal(32, image.Palettes.Count);
        Assert.All(image.Palettes, palette => Assert.Equal(16, palette.Count));
        _ = image.Decode();
        Assert.Equal(sample.Content, source.Write());

        String workspace = GetWorkspacePath(WorkspaceContainerFormat.ZipArchive);
        try
        {
            WorkspaceLocation location = new(workspace, WorkspaceContainerFormat.ZipArchive);
            TdwWorkspaceExporter.Export(source, new WorkspaceExportSpec(location));
            Assert.Equal(sample.Content, TdwWorkspaceImporter.Import(new WorkspaceImportSpec(location)).Write());
        }
        finally
        {
            File.Delete(workspace);
        }
    }

    [Fact]
    public void MultiPaletteTex_ReadsDecodesAndRoundTripsWorkspace()
    {
        ImagingSample sample = ImagingSample.MultiPaletteTex;
        TexFile source = TexFile.Read(sample.Content);

        Assert.Equal(16, source.Header.PaletteCount);
        Assert.Equal(16, source.Header.ColorsPerPalette);
        Assert.Equal(128, source.Header.Width);
        Assert.Equal(64, source.Header.Height);
        Assert.Equal(16, source.Palettes.Count);
        _ = source.Decode(15);
        Assert.Equal(sample.Content, source.Write());

        String workspace = GetWorkspacePath(WorkspaceContainerFormat.ZipArchive);
        try
        {
            WorkspaceLocation location = new(workspace, WorkspaceContainerFormat.ZipArchive);
            TexWorkspaceExporter.Export(source, new WorkspaceExportSpec(location) { DefaultPaletteIndex = 15 });
            Assert.Equal(sample.Content, TexWorkspaceImporter.Import(new WorkspaceImportSpec(location)).Write());
        }
        finally
        {
            File.Delete(workspace);
        }
    }

    [Fact]
    public void ComplexMimMap_ReadsAndRoundTripsOpenRasterWorkspace()
    {
        ImagingSample mimSample = ImagingSample.ComplexFieldMim;
        ImagingSample mapSample = ImagingSample.ComplexFieldMap;
        MimMapFile source = MimMapFile.Read(mimSample.Content, mapSample.Content);

        Assert.Equal(3_510, source.Map.Tiles.Count);
        Assert.All(source.Map.Tiles, tile => Assert.Equal(MapBlendMode.None, tile.BlendMode));
        Assert.Equal(mimSample.Content, source.Mim.Write());
        Assert.Equal(mapSample.Content, source.Map.Write());

        String workspace = GetWorkspacePath(WorkspaceContainerFormat.OpenRaster);
        try
        {
            WorkspaceLocation location = new(workspace, WorkspaceContainerFormat.OpenRaster);
            MimMapWorkspaceExporter.Export(source, new WorkspaceExportSpec(location) { Scheduler = new ParallelWorkspaceExportScheduler() });
            MimMapFile imported = MimMapWorkspaceImporter.Import(new WorkspaceImportSpec(location));
            Assert.Equal(mimSample.Content, imported.Mim.Write());
            Assert.Equal(mapSample.Content, imported.Map.Write());
        }
        finally
        {
            File.Delete(workspace);
        }
    }

    private static void ValidateDefaultPaletteImagesMatchPreviews(String workspace, IReadOnlyList<TimImage> images)
    {
        using ZipArchive archive = ZipFile.OpenRead(workspace);
        for (Int32 imageIndex = 0; imageIndex < images.Count; imageIndex++)
        {
            String imagePath = $"{imageIndex + 1:D3}.png";
            String previewPath = $"{imageIndex + 1:D3}_previews.png";
            ZipArchiveEntry imageEntry = archive.GetEntry(imagePath) ?? throw new InvalidDataException($"The workspace has no '{imagePath}'.");
            ZipArchiveEntry previewEntry = archive.GetEntry(previewPath) ?? throw new InvalidDataException($"The workspace has no '{previewPath}'.");
            using Stream imageStream = imageEntry.Open();
            using Stream previewStream = previewEntry.Open();
            using MagickImage image = new(imageStream);
            using MagickImage preview = new(previewStream);
            Assert.Equal(preview.Width, image.Width);
            Assert.Equal(checked((UInt32)(image.Height * images[imageIndex].Palettes.Count)), preview.Height);
            Assert.Equal(GetRgbaPixels(preview)[..checked((Int32)(image.Width * image.Height * 4))], GetRgbaPixels(image));
        }
    }

    private static Byte[] GetRgbaPixels(MagickImage image)
    {
        using IPixelCollection<Byte> pixels = image.GetPixels();
        return pixels.ToByteArray(PixelMapping.RGBA) ?? throw new InvalidDataException("The PNG decoder did not return pixel data.");
    }

    private static String GetWorkspacePath(WorkspaceContainerFormat format)
    {
        String extension = format == WorkspaceContainerFormat.OpenRaster ? ".ora" : ".zip";
        return Path.Combine(Path.GetTempPath(), $"Albeoris.Imaging.Embedded.{Guid.NewGuid():N}{extension}");
    }
}
