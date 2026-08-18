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
        console.WriteLine("  Albeoris.Games.FF8.Toolset extract [options]");
        console.WriteLine("  Albeoris.Games.FF8.Toolset /? | -h | --help");
        console.WriteLine();
        console.MarkupLine("[bold]Modes[/]");
        console.MarkupLine("  [cyan]installations[/]  List locally installed Final Fantasy VIII releases.");
        console.MarkupLine("  [cyan]analysis[/]       Analyze files in a Final Fantasy VIII installation.");
        console.MarkupLine("  [cyan]extract[/]        Extract Final Fantasy VIII archives.");
        console.WriteLine();
        console.MarkupLine("[bold]Options[/]");
        console.MarkupLine("  [cyan]-ni, --non-interactive[/]  Disable prompts and the exit pause.");
        console.MarkupLine("  [cyan]-gp, --game-path PATH[/]   Game installation directory (analysis, extract).");
        console.MarkupLine("  [cyan]-o, --output PATH[/]       Report file or extraction directory.");
        console.MarkupLine("  [cyan]-tp, --temp-path PATH[/]   Temporary directory (analysis).");
        console.MarkupLine("  [cyan]-ga, --game-archive PATH[/] Input .zzz or .fl archive; may be repeated (extract).");
        console.MarkupLine("  [cyan]-m, --mask MASKS[/]         Semicolon-separated extraction masks (extract).");
        console.MarkupLine("  [cyan]--recursive[/]              Extract nested archives (extract).");
        console.MarkupLine("  [cyan]--no-recursive[/]           Do not extract nested archives (extract).");
        console.MarkupLine("  [cyan]/?, -h, --help[/]          Show help.");
    }
}
