using Spectre.Console;

namespace Albeoris.Games.FF8.Toolset.Application;

internal sealed class ModeSelector(IAnsiConsole console)
{
    private readonly IAnsiConsole console = console ?? throw new ArgumentNullException(nameof(console));

    public OperationMode? Select()
    {
        if (Console.IsInputRedirected)
        {
            throw new InteractiveInputException(
                "Interactive input is unavailable. Specify a mode as the first argument.");
        }

        SelectionPrompt<ModeChoice> prompt = new SelectionPrompt<ModeChoice>()
            .Title("Select a mode:")
            .PageSize(5)
            .UseConverter(choice => choice.DisplayName)
            .AddChoices(
                new ModeChoice("Installed Final Fantasy VIII releases", OperationMode.Installations),
                new ModeChoice("Cancel", null));

        return console.Prompt(prompt).Mode;
    }

    private sealed class ModeChoice(String displayName, OperationMode? mode)
    {
        public String DisplayName { get; } = displayName;

        public OperationMode? Mode { get; } = mode;
    }
}
