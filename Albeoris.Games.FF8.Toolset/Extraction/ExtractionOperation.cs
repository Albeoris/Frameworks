using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;
using Albeoris.Games.FF8.Toolset.Infrastructure;
using Spectre.Console;

namespace Albeoris.Games.FF8.Toolset.Extraction;

internal sealed class ExtractionOperation(
    ArchiveExtractor extractor,
    IAnsiConsole console,
    TextWriter output,
    IApplicationLogger logger)
{
    private readonly ArchiveExtractor extractor = extractor ?? throw new ArgumentNullException(nameof(extractor));
    private readonly OperationProgressPresenter progressPresenter = new(console ?? throw new ArgumentNullException(nameof(console)));
    private readonly TextWriter output = output ?? throw new ArgumentNullException(nameof(output));
    private readonly IApplicationLogger logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task ExecuteAsync(ExtractionPlan plan, Boolean showProgress, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        Boolean displayProgress = showProgress && !Console.IsOutputRedirected;
        OperationProgressTracker? progress = displayProgress ? new OperationProgressTracker() : null;
        progress?.Initialize(plan.Sources.Count);

        try
        {
            Task<Int32> extraction = ExtractAllAsync(plan, progress, cancellationToken);
            Int32 extractedCount = displayProgress
                ? await progressPresenter.ShowUntilCompletedAsync("Extraction", extraction, progress!, cancellationToken)
                : await extraction.ConfigureAwait(false);
            logger.Information($"Extraction completed. Extracted files: {extractedCount}.");
            output.WriteLine($"Extracted {extractedCount} file(s) to: {plan.OutputPath}");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new ExtractionExecutionException("Could not extract the game archives.", exception);
        }
    }

    private async Task<Int32> ExtractAllAsync(
        ExtractionPlan plan,
        OperationProgressTracker? progress,
        CancellationToken cancellationToken)
    {
        ConcurrentBag<Int32> counts = [];
        Int32 parallelism = Math.Max(1, Environment.ProcessorCount);
        logger.Information($"Extraction Dataflow parallelism: {parallelism}.");
        TransformBlock<ExtractionSource, Int32> extractBlock = new(
            source => extractor.Extract(source, plan, progress),
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = parallelism,
                EnsureOrdered = false,
                BoundedCapacity = parallelism * 2,
                CancellationToken = cancellationToken,
            });
        ActionBlock<Int32> collectBlock = new(counts.Add, new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = 1,
            CancellationToken = cancellationToken,
        });
        extractBlock.LinkTo(collectBlock, new DataflowLinkOptions { PropagateCompletion = true });

        foreach (ExtractionSource source in plan.Sources)
        {
            if (!await extractBlock.SendAsync(source, cancellationToken).ConfigureAwait(false))
                throw new InvalidOperationException("The extraction pipeline rejected an archive.");
        }
        extractBlock.Complete();
        await collectBlock.Completion.ConfigureAwait(false);
        return counts.Sum();
    }
}
