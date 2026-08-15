using System.Text;
using Albeoris.Games.FF8.FlArchives;
using Albeoris.Games.FF8.FlArchives.Abstractions;
using Albeoris.Games.FF8.Toolset.Analysis;
using Albeoris.Games.FF8.Toolset.Analysis.Model;
using Albeoris.Games.FF8.Toolset.Infrastructure;
using Albeoris.Games.FF8.ZzzArchives;
using Albeoris.Games.FF8.ZzzArchives.Abstractions;
using Xunit;

namespace Albeoris.Games.FF8.Toolset.Tests;

public sealed class GameAnalyzerTests : IDisposable
{
    private readonly String rootPath = Path.Combine(Path.GetTempPath(), $"FF8AnalyzerTests.{Guid.NewGuid():N}");
    private readonly String gamePath;
    private readonly String tempPath;

    public GameAnalyzerTests()
    {
        gamePath = Path.Combine(rootPath, "Game");
        tempPath = Path.Combine(rootPath, "Temp");
        Directory.CreateDirectory(gamePath);
        Directory.CreateDirectory(tempPath);
    }

    [Fact]
    public async Task AnalyzeAsync_ExpandsEmbeddedFlAndCollectsTranslatableFiles()
    {
        (Byte[] fl, Byte[] fi, Byte[] fs) = CreateFlArchive();
        String zzzPath = Path.Combine(gamePath, "main.zzz");
        using (IZzzArchive archive = ZzzArchive.Create(zzzPath))
        {
            AddEntry(archive, "data/lang-en/kernel.bin", "Kernel"u8);
            AddEntry(archive, "data/lang-en/field.fl", fl);
            AddEntry(archive, "data/lang-en/field.fi", fi);
            AddEntry(archive, "data/lang-en/field.fs", fs);
        }

        StubLogger logger = new();
        GameAnalyzer analyzer = new(
            new GameArchiveScanner(),
            new ArchiveContainerAnalyzer(new TranslationFileClassifier(), logger),
            new AnalysisReportFactory(),
            logger);
        AnalysisPlan plan = new(gamePath, Path.Combine(rootPath, "report.json"), tempPath, AnalysisReportFormat.Json);

        AnalysisReport report = await analyzer.AnalyzeAsync(plan, TestContext.Current.CancellationToken);

        ArchiveAnalysis archiveResult = Assert.Single(report.Archives);
        Assert.Contains(report.TranslatableFiles, file =>
            file.Path.EndsWith("kernel.bin", StringComparison.OrdinalIgnoreCase) &&
            file.Categories.Contains(TranslationCategory.SystemTextAndUi));
        Assert.Contains(report.TranslatableFiles, file =>
            file.Path.EndsWith("dialog.msd", StringComparison.OrdinalIgnoreCase) &&
            file.Categories.Contains(TranslationCategory.Dialogues));
        Assert.Contains(Flatten(archiveResult.Children), node =>
            node.Name.Equals("field.fl", StringComparison.OrdinalIgnoreCase) &&
            node.Kind == AnalysisNodeKind.Archive);
        Assert.Empty(Directory.EnumerateFiles(tempPath));
    }

    public void Dispose()
    {
        Directory.Delete(rootPath, recursive: true);
    }

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

    private static IEnumerable<AnalysisNode> Flatten(IEnumerable<AnalysisNode> nodes)
    {
        foreach (AnalysisNode node in nodes)
        {
            yield return node;
            foreach (AnalysisNode child in Flatten(node.Children))
                yield return child;
        }
    }

    private sealed class StubLogger : IApplicationLogger
    {
        public String LogPath => String.Empty;

        public void Information(String message)
        {
        }

        public void Warning(String message)
        {
        }

        public void Error(String message, Exception exception)
        {
        }
    }
}
