using System.Collections.Concurrent;

namespace Albeoris.Games.FF8.Toolset.Analysis;

internal sealed class AnalysisProgressTracker
{
    private readonly ConcurrentDictionary<String, ActiveAnalysisItem> activeItems =
        new(StringComparer.OrdinalIgnoreCase);
    private Int32 totalCount;
    private Int32 startedCount;
    private Int32 completedCount;

    public void Initialize(IReadOnlyCollection<ArchiveWorkItem> workItems) =>
        Interlocked.Exchange(ref totalCount, workItems.Count);

    public void Start(ArchiveWorkItem workItem)
    {
        activeItems[workItem.Path] = new ActiveAnalysisItem(
            workItem.Path,
            $"Analyzing {workItem.Kind.ToString().ToUpperInvariant()}");
        Interlocked.Increment(ref startedCount);
    }

    public void Complete(ArchiveWorkItem workItem)
    {
        activeItems.TryRemove(workItem.Path, out _);
        Interlocked.Increment(ref completedCount);
    }

    public AnalysisProgressSnapshot GetSnapshot()
    {
        Int32 total = Volatile.Read(ref totalCount);
        Int32 started = Math.Min(Volatile.Read(ref startedCount), total);
        Int32 completed = Math.Min(Volatile.Read(ref completedCount), total);
        ActiveAnalysisItem[] active = activeItems.Values
            .OrderBy(static item => item.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new AnalysisProgressSnapshot(
            total,
            completed,
            Math.Max(0, total - started),
            Math.Max(0, total - completed),
            active);
    }
}

internal sealed record ActiveAnalysisItem(String Path, String State);

internal sealed record AnalysisProgressSnapshot(
    Int32 TotalCount,
    Int32 CompletedCount,
    Int32 QueuedCount,
    Int32 RemainingCount,
    IReadOnlyList<ActiveAnalysisItem> ActiveItems);
