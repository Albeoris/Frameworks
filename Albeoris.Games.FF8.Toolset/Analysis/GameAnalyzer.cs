using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;
using Albeoris.Games.FF8.Toolset.Analysis.Model;
using Albeoris.Games.FF8.Toolset.Infrastructure;

namespace Albeoris.Games.FF8.Toolset.Analysis;

internal sealed class GameAnalyzer(
    GameArchiveScanner scanner,
    ArchiveContainerAnalyzer archiveAnalyzer,
    AnalysisReportFactory reportFactory,
    IApplicationLogger logger)
{
    private readonly GameArchiveScanner scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
    private readonly ArchiveContainerAnalyzer archiveAnalyzer =
        archiveAnalyzer ?? throw new ArgumentNullException(nameof(archiveAnalyzer));
    private readonly AnalysisReportFactory reportFactory = reportFactory ?? throw new ArgumentNullException(nameof(reportFactory));
    private readonly IApplicationLogger logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<AnalysisReport> AnalyzeAsync(
        AnalysisPlan plan,
        CancellationToken cancellationToken = default,
        AnalysisProgressTracker? progress = null)
    {
        ArgumentNullException.ThrowIfNull(plan);
        IReadOnlyList<ArchiveWorkItem> workItems = scanner.Find(plan.GamePath);
        progress?.Initialize(workItems);
        Int32 parallelism = Math.Max(Environment.ProcessorCount, 1);
        logger.Information($"Found {workItems.Count} top-level archive(s). Dataflow parallelism: {parallelism}.");

        ConcurrentBag<ArchiveAnalysis> results = [];
        ExecutionDataflowBlockOptions options = new()
        {
            MaxDegreeOfParallelism = parallelism,
            EnsureOrdered = false,
            BoundedCapacity = parallelism * 2,
            CancellationToken = cancellationToken,
        };
        TransformBlock<ArchiveWorkItem, ArchiveAnalysis> analyzeBlock = new(
            item => AnalyzeArchive(item, plan.TempPath, progress),
            options);
        ActionBlock<ArchiveAnalysis> collectBlock = new(results.Add, new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = 1,
            CancellationToken = cancellationToken,
        });
        analyzeBlock.LinkTo(collectBlock, new DataflowLinkOptions { PropagateCompletion = true });

        foreach (ArchiveWorkItem item in workItems)
        {
            if (!await analyzeBlock.SendAsync(item, cancellationToken).ConfigureAwait(false))
                throw new InvalidOperationException("The analysis pipeline rejected an archive.");
        }
        analyzeBlock.Complete();
        await collectBlock.Completion.ConfigureAwait(false);

        ArchiveAnalysis[] orderedResults = results
            .OrderBy(result => result.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return reportFactory.Create(plan.GamePath, orderedResults);
    }

    private ArchiveAnalysis AnalyzeArchive(
        ArchiveWorkItem workItem,
        String tempPath,
        AnalysisProgressTracker? progress)
    {
        progress?.Start(workItem);
        try
        {
            return archiveAnalyzer.Analyze(workItem, tempPath);
        }
        finally
        {
            progress?.Complete(workItem);
        }
    }
}
