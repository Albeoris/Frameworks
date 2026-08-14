using System.Runtime.Versioning;
using Albeoris.Games.FF8.Toolset.Infrastructure;
using Albeoris.Games.SteamLibrary;
using Albeoris.Games.SteamLibrary.Abstractions.Models;

namespace Albeoris.Games.FF8.Toolset.Installations;

[SupportedOSPlatform("windows")]
internal sealed class SteamInstallationSource(IApplicationLogger logger) : IInstallationSource
{
    private const UInt32 SteamReleaseAppId = 39150;
    private const UInt32 SteamRemasteredAppId = 1026680;

    private readonly IApplicationLogger logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public IReadOnlyList<InstallationCandidate> FindCandidates()
    {
        List<InstallationCandidate> candidates = [];
        SteamLibraryAccessor? steam = SteamLibraryAccessor.FindInstalled();
        if (steam is null)
        {
            logger.Information("Steam installation was not found.");
            return candidates;
        }

        logger.Information($"Steam installation: {steam.SteamDirectoryPath}");
        AddCandidate(steam, SteamReleaseAppId, FinalFantasy8Release.Steam2013, candidates);
        AddCandidate(steam, SteamRemasteredAppId, FinalFantasy8Release.SteamRemastered2019, candidates);
        return candidates;
    }

    private void AddCandidate(
        SteamLibraryAccessor steam,
        UInt32 appId,
        FinalFantasy8Release release,
        ICollection<InstallationCandidate> candidates)
    {
        SteamApplication? application = steam.FindApplicationById(appId);
        if (application is null)
        {
            logger.Information($"Steam application {appId} was not found.");
            return;
        }

        logger.Information($"Steam application {appId} candidate: {application.InstallationDirectoryPath}");
        candidates.Add(new InstallationCandidate(release, application.InstallationDirectoryPath));
    }
}
