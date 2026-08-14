using Spectre.Console;

namespace Albeoris.Games.FF8.Toolset.Application;

internal sealed class PausePresenter(IAnsiConsole console)
{
    private readonly IAnsiConsole console = console ?? throw new ArgumentNullException(nameof(console));

    public void WaitForExit()
    {
        console.MarkupLine("[grey]Press ENTER to exit...[/]");
        Console.ReadLine();
    }
}
