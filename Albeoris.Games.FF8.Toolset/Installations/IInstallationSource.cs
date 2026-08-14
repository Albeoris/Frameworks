namespace Albeoris.Games.FF8.Toolset.Installations;

internal interface IInstallationSource
{
    IReadOnlyList<InstallationCandidate> FindCandidates();
}
