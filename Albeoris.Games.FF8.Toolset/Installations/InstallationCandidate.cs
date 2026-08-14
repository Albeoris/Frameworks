namespace Albeoris.Games.FF8.Toolset.Installations;

internal sealed class InstallationCandidate(FinalFantasy8Release release, String path)
{
    public FinalFantasy8Release Release { get; } = release;

    public String Path { get; } = path;
}
