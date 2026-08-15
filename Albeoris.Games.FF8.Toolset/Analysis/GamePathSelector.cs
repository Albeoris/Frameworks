using Albeoris.Games.FF8.Toolset.Application;
using Albeoris.Games.FF8.Toolset.Infrastructure;
using Albeoris.Games.FF8.Toolset.Installations;
using Spectre.Console;

namespace Albeoris.Games.FF8.Toolset.Analysis;

internal sealed class GamePathSelector(
    IAnsiConsole console,
    FinalFantasy8InstallationFinder installationFinder,
    NativePathDialogService dialogs)
{
    private readonly IAnsiConsole console = console ?? throw new ArgumentNullException(nameof(console));
    private readonly FinalFantasy8InstallationFinder installationFinder =
        installationFinder ?? throw new ArgumentNullException(nameof(installationFinder));
    private readonly NativePathDialogService dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));

    public String Select()
    {
        IReadOnlyList<FinalFantasy8Installation> installations = installationFinder.FindInstalled();
        List<GamePathChoice> choices = installations
            .Select(installation => GamePathChoice.ForInstallation(installation))
            .ToList();
        choices.Add(GamePathChoice.Manual());
        choices.Add(GamePathChoice.Cancel());

        SelectionPrompt<GamePathChoice> prompt = new SelectionPrompt<GamePathChoice>()
            .Title("Select a Final Fantasy VIII installation:")
            .PageSize(Math.Min(Math.Max(choices.Count, 3), 12))
            .UseConverter(choice => choice.DisplayName)
            .AddChoices(choices);

        GamePathChoice selected = console.Prompt(prompt);
        if (selected.Kind == GamePathChoiceKind.Cancel)
            throw new ReturnToModeSelectionException();
        if (selected.Kind == GamePathChoiceKind.Manual)
            return dialogs.SelectGameDirectory() ?? throw new ReturnToModeSelectionException();
        return selected.Path!;
    }

    private enum GamePathChoiceKind
    {
        Installation,
        Manual,
        Cancel,
    }

    private sealed class GamePathChoice(String displayName, String? path, GamePathChoiceKind kind)
    {
        public String DisplayName { get; } = displayName;

        public String? Path { get; } = path;

        public GamePathChoiceKind Kind { get; } = kind;

        public static GamePathChoice ForInstallation(FinalFantasy8Installation installation)
        {
            return new GamePathChoice(
                $"[green]{Markup.Escape($"[{installation.ReleaseName}]")}[/] {Markup.Escape(installation.Path)}",
                installation.Path,
                GamePathChoiceKind.Installation);
        }

        public static GamePathChoice Manual() => new("Select a directory manually...", null, GamePathChoiceKind.Manual);

        public static GamePathChoice Cancel() => new("Back", null, GamePathChoiceKind.Cancel);
    }
}
