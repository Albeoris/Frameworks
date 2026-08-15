using Albeoris.Games.FF8.Toolset.Analysis.Model;
using Albeoris.Games.FF8.Toolset.Analysis.Reports;
using Albeoris.Games.FF8.Toolset.Infrastructure;
using Spectre.Console;

namespace Albeoris.Games.FF8.Toolset.Analysis;

internal sealed class AnalysisOperation(
    GameAnalyzer analyzer,
    AnalysisReportFormatterFactory formatterFactory,
    IAnsiConsole console,
    TextWriter output,
    IApplicationLogger logger)
{
    private readonly GameAnalyzer analyzer = analyzer ?? throw new ArgumentNullException(nameof(analyzer));
    private readonly AnalysisReportFormatterFactory formatterFactory =
        formatterFactory ?? throw new ArgumentNullException(nameof(formatterFactory));
    private readonly AnalysisProgressPresenter progressPresenter = new(
        console ?? throw new ArgumentNullException(nameof(console)));
    private readonly TextWriter output = output ?? throw new ArgumentNullException(nameof(output));
    private readonly IApplicationLogger logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task ExecuteAsync(
        AnalysisPlan plan,
        Boolean showProgress,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        AnalysisReport report;
        try
        {
            Boolean displayProgress = showProgress && !Console.IsOutputRedirected;
            AnalysisProgressTracker? progress = displayProgress ? new AnalysisProgressTracker() : null;
            Task<AnalysisReport> analysis = analyzer.AnalyzeAsync(plan, cancellationToken, progress);
            report = displayProgress
                ? await progressPresenter.ShowUntilCompletedAsync(analysis, progress!, cancellationToken)
                : await analysis.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new AnalysisExecutionException("Could not analyze the game files.", exception);
        }

        logger.Information($"Analysis produced {report.Archives.Count} archive(s) and {report.TranslatableFiles.Count} translatable file(s).");
        try
        {
            IAnalysisReportFormatter formatter = formatterFactory.Create(plan.ReportFormat);
            await formatter.WriteAsync(report, plan.OutputPath, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new AnalysisExecutionException("Could not write the analysis report.", exception);
        }

        logger.Information($"Analysis report written: {plan.OutputPath}");
        output.WriteLine($"Report written to: {plan.OutputPath}");
    }
}
