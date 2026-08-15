using System.Text.Json;
using Albeoris.Games.FF8.Toolset.Analysis.Model;
using Albeoris.Games.FF8.Toolset.Analysis.Reports;
using Xunit;

namespace Albeoris.Games.FF8.Toolset.Tests;

public sealed class AnalysisReportFormatterTests : IDisposable
{
    private readonly String rootPath = Path.Combine(Path.GetTempPath(), $"FF8ReportTests.{Guid.NewGuid():N}");

    public AnalysisReportFormatterTests()
    {
        Directory.CreateDirectory(rootPath);
    }

    [Fact]
    public async Task JsonFormatter_WritesStructuredReport()
    {
        String path = Path.Combine(rootPath, "report.json");

        await new JsonAnalysisReportFormatter().WriteAsync(
            CreateReport(),
            path,
            TestContext.Current.CancellationToken);

        using JsonDocument document = JsonDocument.Parse(await File.ReadAllTextAsync(
            path,
            TestContext.Current.CancellationToken));
        Assert.Equal(1, document.RootElement.GetProperty("schemaVersion").GetInt32());
        Assert.Single(document.RootElement.GetProperty("archives").EnumerateArray());
        Assert.Single(document.RootElement.GetProperty("translatableFiles").EnumerateArray());
    }

    [Fact]
    public async Task HtmlFormatter_WritesSelfContainedInteractiveReport()
    {
        String path = Path.Combine(rootPath, "report.html");

        await new HtmlAnalysisReportFormatter().WriteAsync(
            CreateReport(),
            path,
            TestContext.Current.CancellationToken);

        String html = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
        Assert.Contains("id=\"report-data\"", html);
        Assert.Contains("Include path patterns", html);
        Assert.Contains("Exclude path patterns", html);
        Assert.Contains("Apply filters", html);
        Assert.Contains("File types", html);
        Assert.Contains("Languages", html);
        Assert.Contains("function treeNode", html);
        Assert.Contains("function translatableTree", html);
        Assert.DoesNotContain("addEventListener('input'", html);
        Assert.DoesNotContain("<script src=", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HtmlFormatter_DoesNotTruncateReportsLargerThanOneMegabyte()
    {
        String path = Path.Combine(rootPath, "large-report.html");
        AnalysisReport report = new(
            new String('x', 1_100_000),
            DateTimeOffset.UnixEpoch,
            [],
            []);

        await new HtmlAnalysisReportFormatter().WriteAsync(
            report,
            path,
            TestContext.Current.CancellationToken);

        String html = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
        Assert.True(html.Length > 1_100_000);
        Assert.EndsWith("</html>", html.TrimEnd(), StringComparison.Ordinal);
    }

    public void Dispose()
    {
        Directory.Delete(rootPath, recursive: true);
    }

    private static AnalysisReport CreateReport()
    {
        AnalysisNode file = new("kernel.bin", "data/lang-en/kernel.bin", AnalysisNodeKind.File, 42);
        file.TranslationCategories.Add(TranslationCategory.SystemTextAndUi);
        ArchiveAnalysis archive = new("main.zzz", "main.zzz", "zzz", 100, [file]);
        TranslatableFile translatable = new(
            "main.zzz",
            "main.zzz/data/lang-en/kernel.bin",
            42,
            [TranslationCategory.SystemTextAndUi]);
        return new AnalysisReport("C:\\Games\\FF8", DateTimeOffset.UnixEpoch, [archive], [translatable]);
    }
}
