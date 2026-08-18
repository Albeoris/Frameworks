using System.Collections.Concurrent;

namespace Albeoris.Games.FF8.Toolset.Infrastructure;

internal sealed class OperationProgressTracker
{
    private readonly ConcurrentDictionary<String, ActiveOperationItem> activeItems = new(StringComparer.OrdinalIgnoreCase);
    private Int32 totalCount;
    private Int32 startedCount;
    private Int32 completedCount;

    public void Initialize(Int32 count) => Interlocked.Exchange(ref totalCount, count);

    public void Start(String key, String state)
    {
        activeItems[key] = new ActiveOperationItem(key, state);
        Interlocked.Increment(ref startedCount);
    }

    public void Update(String key, String state) => activeItems[key] = new ActiveOperationItem(key, state);

    public void Complete(String key)
    {
        activeItems.TryRemove(key, out _);
        Interlocked.Increment(ref completedCount);
    }

    public OperationProgressSnapshot GetSnapshot()
    {
        Int32 total = Volatile.Read(ref totalCount);
        Int32 started = Math.Min(Volatile.Read(ref startedCount), total);
        Int32 completed = Math.Min(Volatile.Read(ref completedCount), total);
        ActiveOperationItem[] active = activeItems.Values
            .OrderBy(static item => item.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return new OperationProgressSnapshot(
            total,
            completed,
            Math.Max(0, total - started),
            Math.Max(0, total - completed),
            active);
    }
}

internal sealed record ActiveOperationItem(String Path, String State);

internal sealed record OperationProgressSnapshot(
    Int32 TotalCount,
    Int32 CompletedCount,
    Int32 QueuedCount,
    Int32 RemainingCount,
    IReadOnlyList<ActiveOperationItem> ActiveItems);
