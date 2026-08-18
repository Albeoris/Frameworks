using Spectre.Console;
using Spectre.Console.Rendering;

namespace Albeoris.Games.FF8.Toolset.Infrastructure;

internal sealed class OperationProgressPresenter(IAnsiConsole console)
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMilliseconds(200);
    private readonly IAnsiConsole console = console ?? throw new ArgumentNullException(nameof(console));

    public async Task<T> ShowUntilCompletedAsync<T>(
        String title,
        Task<T> operation,
        OperationProgressTracker progress,
        CancellationToken cancellationToken)
    {
        await console.Live(CreateDisplay(title, progress.GetSnapshot()))
            .AutoClear(false)
            .Overflow(VerticalOverflow.Ellipsis)
            .StartAsync(async context =>
            {
                while (!operation.IsCompleted)
                {
                    context.UpdateTarget(CreateDisplay(title, progress.GetSnapshot()));
                    context.Refresh();
                    await Task.Delay(RefreshInterval, cancellationToken);
                }
                context.UpdateTarget(CreateDisplay(title, progress.GetSnapshot()));
                context.Refresh();
            });
        return await operation;
    }

    private static IRenderable CreateDisplay(String title, OperationProgressSnapshot snapshot)
    {
        Markup summary = new(
            $"[bold]{Markup.Escape(title)}[/]  Completed: [green]{snapshot.CompletedCount}[/]/{snapshot.TotalCount}  " +
            $"Active: [yellow]{snapshot.ActiveItems.Count}[/]  " +
            $"Queued: {snapshot.QueuedCount}  Remaining: {snapshot.RemainingCount}");
        if (snapshot.ActiveItems.Count == 0)
            return summary;

        Table table = new Table()
            .Border(TableBorder.None)
            .HideHeaders()
            .AddColumn(new TableColumn("State").NoWrap())
            .AddColumn(new TableColumn("Archive"));
        foreach (ActiveOperationItem item in snapshot.ActiveItems)
            table.AddRow(new Markup($"[yellow]{Markup.Escape(item.State)}[/]"), new Text(item.Path));
        return new Rows(summary, table);
    }
}
