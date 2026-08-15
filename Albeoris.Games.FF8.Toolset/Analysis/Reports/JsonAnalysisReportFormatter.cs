using System.Text.Json;
using Albeoris.Games.FF8.Toolset.Analysis.Model;

namespace Albeoris.Games.FF8.Toolset.Analysis.Reports;

internal sealed class JsonAnalysisReportFormatter : IAnalysisReportFormatter
{
    private readonly JsonSerializerOptions options = AnalysisJsonSerializerOptions.Create(indented: true);

    public async Task WriteAsync(
        AnalysisReport report,
        String outputPath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);
        await using FileStream output = new(
            outputPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        await JsonSerializer.SerializeAsync(output, report, options, cancellationToken).ConfigureAwait(false);
        await output.FlushAsync(cancellationToken).ConfigureAwait(false);
    }
}
