namespace Albeoris.Games.FF8.Toolset.Analysis.Reports;

internal sealed class AnalysisReportFormatterFactory
{
    public IAnalysisReportFormatter Create(AnalysisReportFormat format)
    {
        return format switch
        {
            AnalysisReportFormat.Html => new HtmlAnalysisReportFormatter(),
            AnalysisReportFormat.Json => new JsonAnalysisReportFormatter(),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null),
        };
    }
}
