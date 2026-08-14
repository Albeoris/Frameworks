using Albeoris.Games.FF8.Toolset.Application;
using Albeoris.Games.FF8.Toolset.Infrastructure;
using Spectre.Console;
using WindowsApplication = System.Windows.Forms.Application;

namespace Albeoris.Games.FF8.Toolset;

public static class Program
{
    [STAThread]
    public static Int32 Main(String[] arguments)
    {
        try
        {
            WindowsApplication.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            WindowsApplication.EnableVisualStyles();
            WindowsApplication.SetCompatibleTextRenderingDefault(false);

            using FileApplicationLogger logger = FileApplicationLogger.Create();
            return ToolsetApplication.CreateDefault(logger).Run(arguments);
        }
        catch (Exception)
        {
            AnsiConsole.MarkupLine("[red]Error:[/] Could not start the application.");
            return (Int32)ExitCode.ExecutionError;
        }
    }
}
