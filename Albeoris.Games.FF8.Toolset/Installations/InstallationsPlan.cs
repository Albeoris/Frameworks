namespace Albeoris.Games.FF8.Toolset.Installations;

internal sealed class InstallationsPlan(IReadOnlyList<FinalFantasy8Installation> installations)
{
    public IReadOnlyList<FinalFantasy8Installation> Installations { get; } =
        installations ?? throw new ArgumentNullException(nameof(installations));
}
