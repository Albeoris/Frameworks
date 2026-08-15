namespace Albeoris.Games.FF8.Toolset.Analysis;

internal sealed class AnalysisPlan(
    String gamePath,
    String outputPath,
    String tempPath,
    AnalysisReportFormat reportFormat)
{
    public String GamePath { get; } = gamePath;

    public String OutputPath { get; } = outputPath;

    public String TempPath { get; } = tempPath;

    public AnalysisReportFormat ReportFormat { get; } = reportFormat;
}
