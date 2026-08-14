namespace Albeoris.Games.FF8.Toolset.Installations;

internal sealed class FinalFantasy8Installation(FinalFantasy8Release release, String path)
{
    public FinalFantasy8Release Release { get; } = release;

    public String Path { get; } = path;

    public String ReleaseName => Release switch
    {
        FinalFantasy8Release.ClassicPc => "PC (2000)",
        FinalFantasy8Release.Steam2013 => "Steam (2013)",
        FinalFantasy8Release.SteamRemastered2019 => "Steam Remastered (2019)",
        _ => throw new InvalidOperationException($"Unsupported release '{Release}'."),
    };
}
