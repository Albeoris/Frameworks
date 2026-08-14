using Albeoris.Games.FF8.Toolset.Infrastructure;

namespace Albeoris.Games.FF8.Toolset.Installations;

internal sealed class InstallationsPlanBuilder(
    FinalFantasy8InstallationFinder finder,
    IApplicationLogger logger)
{
    private readonly FinalFantasy8InstallationFinder finder =
        finder ?? throw new ArgumentNullException(nameof(finder));
    private readonly IApplicationLogger logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public InstallationsPlan Build()
    {
        logger.Information("Preparing installations data.");
        IReadOnlyList<FinalFantasy8Installation> installations = finder.FindInstalled();
        return new InstallationsPlan(installations);
    }
}
