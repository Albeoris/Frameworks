using System.Windows.Forms;

namespace Albeoris.Games.FF8.Toolset.Infrastructure;

internal sealed class NativePathDialogService
{
    private readonly ConsoleWindowDialogHost dialogHost = new();

    public String? SelectGameDirectory()
    {
        using FolderBrowserDialog dialog = new()
        {
            Description = "Select the Final Fantasy VIII installation directory",
            ShowNewFolderButton = false,
            UseDescriptionForTitle = true,
        };
        return dialogHost.Show(dialog) == DialogResult.OK ? dialog.SelectedPath : null;
    }

    public String? SelectReportPath()
    {
        using SaveFileDialog dialog = new()
        {
            Title = "Save the Final Fantasy VIII analysis report",
            Filter = "HTML report (*.html)|*.html|JSON report (*.json)|*.json",
            AddExtension = true,
            DefaultExt = "html",
            OverwritePrompt = true,
            RestoreDirectory = true,
        };
        return dialogHost.Show(dialog) == DialogResult.OK ? dialog.FileName : null;
    }

    public IReadOnlyList<String>? SelectGameArchives()
    {
        using OpenFileDialog dialog = new()
        {
            Title = "Select Final Fantasy VIII archives",
            Filter = "Final Fantasy VIII archives (*.zzz;*.fl)|*.zzz;*.fl|ZZZ archives (*.zzz)|*.zzz|FL archives (*.fl)|*.fl",
            Multiselect = true,
            CheckFileExists = true,
            RestoreDirectory = true,
        };
        return dialogHost.Show(dialog) == DialogResult.OK ? dialog.FileNames : null;
    }

    public String? SelectExtractionDirectory()
    {
        using FolderBrowserDialog dialog = new()
        {
            Description = "Select the extraction output directory",
            ShowNewFolderButton = true,
            UseDescriptionForTitle = true,
        };
        return dialogHost.Show(dialog) == DialogResult.OK ? dialog.SelectedPath : null;
    }
}
