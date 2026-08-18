using System.Text;
using Albeoris.Games.FF8.FlArchives;
using Albeoris.Games.FF8.FlArchives.Abstractions;
using Albeoris.Games.FF8.Toolset.Analysis;
using Albeoris.Games.FF8.Toolset.Extraction;
using Albeoris.Games.FF8.Toolset.Infrastructure;
using Albeoris.Games.FF8.ZzzArchives;
using Albeoris.Games.FF8.ZzzArchives.Abstractions;
using Xunit;

namespace Albeoris.Games.FF8.Toolset.Tests;

public sealed class ArchiveExtractorTests : IDisposable
{
    private readonly String rootPath = Path.Combine(Path.GetTempPath(), $"FF8ExtractorTests.{Guid.NewGuid():N}");
    private readonly String outputPath;
    private readonly String tempPath;

    public ArchiveExtractorTests()
    {
        outputPath = Path.Combine(rootPath, "Output");
        tempPath = Path.Combine(rootPath, "Temp");
        Directory.CreateDirectory(outputPath);
        Directory.CreateDirectory(tempPath);
    }

    [Fact]
    public void Extract_RecursiveModeExpandsFlTripletAndAppliesFileNameMask()
    {
        (Byte[] fl, Byte[] fi, Byte[] fs) = CreateFlArchive();
        String archivePath = Path.Combine(rootPath, "main.zzz");
        using (IZzzArchive archive = ZzzArchive.Create(archivePath))
        {
            AddEntry(archive, "data/lang-en/kernel.bin", "Kernel"u8);
            AddEntry(archive, "data/lang-en/field.fl", fl);
            AddEntry(archive, "data/lang-en/field.fi", fi);
            AddEntry(archive, "data/lang-en/field.fs", fs);
        }
        ExtractionSource source = new(archivePath, "main.zzz", ArchiveWorkItemKind.Zzz);
        ExtractionPlan plan = new([source], outputPath, tempPath, true, ArchivePathMatcher.Create(["*.msd"]));

        Int32 count = new ArchiveExtractor(new StubLogger()).Extract(source, plan, null);

        Assert.Equal(1, count);
        Assert.True(File.Exists(Path.Combine(outputPath, "main.zzz", "data", "lang-en", "field.fl", "field", "dialog.msd")));
        Assert.False(File.Exists(Path.Combine(outputPath, "main.zzz", "data", "lang-en", "kernel.bin")));
        Assert.Empty(Directory.EnumerateFiles(tempPath));
    }

    public void Dispose() => Directory.Delete(rootPath, recursive: true);

    private static (Byte[] Fl, Byte[] Fi, Byte[] Fs) CreateFlArchive()
    {
        using MemoryStream fl = new();
        using MemoryStream fi = new();
        using MemoryStream fs = new();
        using (IFlArchive archive = FlArchive.Open(fl, fi, fs, leaveOpen: true, FlArchiveRepresentation.Files))
        {
            IFlArchiveEntry entry = archive.AddEntry("field/dialog.msd");
            Byte[] content = Encoding.UTF8.GetBytes("Dialogue");
            using Stream output = entry.OpenForWrite((UInt32)content.Length);
            output.Write(content);
        }
        return (fl.ToArray(), fi.ToArray(), fs.ToArray());
    }

    private static void AddEntry(IZzzArchive archive, String path, ReadOnlySpan<Byte> content)
    {
        IZzzArchiveEntry entry = archive.AddEntry(path);
        using Stream output = entry.OpenForWrite((UInt32)content.Length);
        output.Write(content);
    }

    private sealed class StubLogger : IApplicationLogger
    {
        public String LogPath => String.Empty;
        public void Information(String message) { }
        public void Warning(String message) { }
        public void Error(String message, Exception exception) { }
    }
}
