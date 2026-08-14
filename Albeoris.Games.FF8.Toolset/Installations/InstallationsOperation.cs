using Albeoris.Games.FF8.Toolset.Infrastructure;

namespace Albeoris.Games.FF8.Toolset.Installations;

internal sealed class InstallationsOperation(TextWriter output, IApplicationLogger logger)
{
    private readonly TextWriter output = output ?? throw new ArgumentNullException(nameof(output));
    private readonly IApplicationLogger logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public void Execute(InstallationsPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        logger.Information("Displaying prepared installations data.");

        if (plan.Installations.Count == 0)
        {
            output.WriteLine("No Final Fantasy VIII installations were found.");
            return;
        }

        foreach (FinalFantasy8Installation installation in plan.Installations)
        {
            output.WriteLine($"[{installation.ReleaseName}] {installation.Path}");
        }
    }
}
