using Albeoris.Games.FF8.Toolset.Application;
using Albeoris.Games.FF8.Toolset.Infrastructure;
using Albeoris.Games.FF8.Toolset.Installations;
using Spectre.Console;

namespace Albeoris.Games.FF8.Toolset.Extraction;

internal sealed class ExtractSourceSelector(
    IAnsiConsole console,
    FinalFantasy8InstallationFinder installationFinder,
    NativePathDialogService dialogs)
{
    private readonly IAnsiConsole console = console ?? throw new ArgumentNullException(nameof(console));
    private readonly FinalFantasy8InstallationFinder installationFinder =
        installationFinder ?? throw new ArgumentNullException(nameof(installationFinder));
    private readonly NativePathDialogService dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));

    public ExtractSourceSelection Select()
    {
        List<Choice> choices = installationFinder.FindInstalled()
            .Select(Choice.ForInstallation)
            .ToList();
        choices.Add(new Choice("Select an installation directory manually...", ChoiceKind.Game, null));
        choices.Add(new Choice("Select one or more archives manually...", ChoiceKind.Archives, null));
        choices.Add(new Choice("Back", ChoiceKind.Cancel, null));

        Choice selected = console.Prompt(new SelectionPrompt<Choice>()
            .Title("Select what to extract:")
            .PageSize(Math.Min(Math.Max(choices.Count, 4), 12))
            .UseConverter(static choice => choice.DisplayName)
            .AddChoices(choices));

        return selected.Kind switch
        {
            ChoiceKind.Installation => ExtractSourceSelection.ForGame(selected.Path!),
            ChoiceKind.Game => SelectGameManually(),
            ChoiceKind.Archives => SelectArchivesManually(),
            _ => throw new ReturnToModeSelectionException(),
        };
    }

    private ExtractSourceSelection SelectGameManually()
    {
        String path = dialogs.SelectGameDirectory() ?? throw new ReturnToModeSelectionException();
        return ExtractSourceSelection.ForGame(path);
    }

    private ExtractSourceSelection SelectArchivesManually()
    {
        IReadOnlyList<String> paths = dialogs.SelectGameArchives() ?? throw new ReturnToModeSelectionException();
        return ExtractSourceSelection.ForArchives(paths);
    }

    private enum ChoiceKind { Installation, Game, Archives, Cancel }

    private sealed record Choice(String DisplayName, ChoiceKind Kind, String? Path)
    {
        public static Choice ForInstallation(FinalFantasy8Installation installation) => new(
            $"[green]{Markup.Escape($"[{installation.ReleaseName}]")}[/] {Markup.Escape(installation.Path)}",
            ChoiceKind.Installation,
            installation.Path);
    }
}
