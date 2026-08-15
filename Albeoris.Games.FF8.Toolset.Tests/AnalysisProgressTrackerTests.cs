using Albeoris.Games.FF8.Toolset.Analysis;
using Xunit;

namespace Albeoris.Games.FF8.Toolset.Tests;

public sealed class AnalysisProgressTrackerTests
{
    [Fact]
    public void Snapshot_CountsMoveOnlyForward()
    {
        ArchiveWorkItem first = new("C:\\Game\\main.zzz", "main.zzz", ArchiveWorkItemKind.Zzz);
        ArchiveWorkItem second = new("C:\\Game\\Data\\lang-en\\main.fl", "Data/lang-en/main.fl", ArchiveWorkItemKind.Fl);
        AnalysisProgressTracker tracker = new();

        tracker.Initialize([first, second]);
        AnalysisProgressSnapshot initial = tracker.GetSnapshot();
        tracker.Start(first);
        AnalysisProgressSnapshot active = tracker.GetSnapshot();
        tracker.Complete(first);
        AnalysisProgressSnapshot completed = tracker.GetSnapshot();

        Assert.Equal((0, 2, 2), (initial.CompletedCount, initial.QueuedCount, initial.RemainingCount));
        Assert.Equal((0, 1, 2), (active.CompletedCount, active.QueuedCount, active.RemainingCount));
        Assert.Single(active.ActiveItems);
        Assert.Equal("Analyzing ZZZ", active.ActiveItems[0].State);
        Assert.Equal((1, 1, 1), (completed.CompletedCount, completed.QueuedCount, completed.RemainingCount));
        Assert.Empty(completed.ActiveItems);
    }
}
