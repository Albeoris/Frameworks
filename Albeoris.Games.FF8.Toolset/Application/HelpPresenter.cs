using System.Reflection;
using Spectre.Console;

namespace Albeoris.Games.FF8.Toolset.Application;

internal sealed class HelpPresenter(IAnsiConsole console)
{
    private readonly IAnsiConsole console = console ?? throw new ArgumentNullException(nameof(console));

    public void Show()
    {
        String version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";

        console.MarkupLine($"[bold]Albeoris.Games.FF8.Toolset[/] {Markup.Escape(version)}");
        console.WriteLine("Tools for different Final Fantasy VIII releases.");
        console.WriteLine();
        console.MarkupLine("[bold]Usage[/]");
        console.WriteLine("  Albeoris.Games.FF8.Toolset");
        console.WriteLine("  Albeoris.Games.FF8.Toolset installations [--non-interactive]");
        console.WriteLine("  Albeoris.Games.FF8.Toolset analysis [options]");
        console.WriteLine("  Albeoris.Games.FF8.Toolset /? | -h | --help");
        console.WriteLine();
        console.MarkupLine("[bold]Modes[/]");
        console.MarkupLine("  [cyan]installations[/]  List locally installed Final Fantasy VIII releases.");
        console.MarkupLine("  [cyan]analysis[/]       Analyze files in a Final Fantasy VIII installation.");
        console.WriteLine();
        console.MarkupLine("[bold]Options[/]");
        console.MarkupLine("  [cyan]-ni, --non-interactive[/]  Disable prompts and the exit pause.");
        console.MarkupLine("  [cyan]-gp, --game-path PATH[/]   Game installation directory (analysis).");
        console.MarkupLine("  [cyan]-o, --output PATH[/]       Output .html or .json report path (analysis).");
        console.MarkupLine("  [cyan]-tp, --temp-path PATH[/]   Temporary directory (analysis).");
        console.MarkupLine("  [cyan]/?, -h, --help[/]          Show help.");
    }
}
