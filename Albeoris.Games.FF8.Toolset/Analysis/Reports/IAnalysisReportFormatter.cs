using Albeoris.Games.FF8.Toolset.Analysis.Model;

namespace Albeoris.Games.FF8.Toolset.Analysis.Reports;

internal interface IAnalysisReportFormatter
{
    Task WriteAsync(AnalysisReport report, String outputPath, CancellationToken cancellationToken = default);
}
