using System.Security;
using Albeoris.Games.FF8.Toolset.Infrastructure;

namespace Albeoris.Games.FF8.Toolset.Installations;

internal sealed class FinalFantasy8InstallationFinder
{
    private readonly IReadOnlyList<IInstallationSource> sources;
    private readonly IApplicationLogger logger;

    internal FinalFantasy8InstallationFinder(
        IReadOnlyList<IInstallationSource> sources,
        IApplicationLogger logger)
    {
        ArgumentNullException.ThrowIfNull(sources);
        this.sources = sources;
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public static FinalFantasy8InstallationFinder CreateDefault(IApplicationLogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        return new FinalFantasy8InstallationFinder(
            [new ClassicRegistryInstallationSource(logger), new SteamInstallationSource(logger)],
            logger);
    }

    public IReadOnlyList<FinalFantasy8Installation> FindInstalled()
    {
        List<FinalFantasy8Installation> installations = [];
        HashSet<String> uniquePaths = new(StringComparer.OrdinalIgnoreCase);

        foreach (IInstallationSource source in sources)
        {
            logger.Information($"Inspecting installation source: {source.GetType().Name}.");
            IReadOnlyList<InstallationCandidate> candidates;
            try
            {
                candidates = source.FindCandidates();
            }
            catch (Exception exception)
            {
                throw new InstallationDiscoveryException("Could not inspect installed games.", exception);
            }

            foreach (InstallationCandidate candidate in candidates)
            {
                String? path = GetUsableFullPath(candidate.Path);
                if (path is null)
                {
                    logger.Warning($"Ignoring missing or empty game directory: {candidate.Path}");
                    continue;
                }

                if (!uniquePaths.Add(path))
                {
                    logger.Information($"Ignoring duplicate game directory: {path}");
                    continue;
                }

                logger.Information($"Found {candidate.Release}: {path}");
                installations.Add(new FinalFantasy8Installation(candidate.Release, path));
            }
        }

        logger.Information($"Found {installations.Count} unique installation(s).");
        return installations;
    }

    private String? GetUsableFullPath(String candidatePath)
    {
        try
        {
            String cleanedPath = Environment.ExpandEnvironmentVariables(candidatePath.Trim().Trim('"'));
            String fullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(cleanedPath));
            if (!Directory.Exists(fullPath))
                return null;

            using IEnumerator<String> entries = Directory.EnumerateFileSystemEntries(fullPath).GetEnumerator();
            return entries.MoveNext() ? fullPath : null;
        }
        catch (Exception exception) when (
            exception is ArgumentException or IOException or NotSupportedException or SecurityException or UnauthorizedAccessException)
        {
            logger.Error($"Could not inspect game directory '{candidatePath}'.", exception);
            return null;
        }
    }
}
