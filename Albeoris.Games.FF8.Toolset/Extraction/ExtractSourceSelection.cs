namespace Albeoris.Games.FF8.Toolset.Extraction;

internal sealed record ExtractSourceSelection(String? GamePath, IReadOnlyList<String> ArchivePaths)
{
    public static ExtractSourceSelection ForGame(String path) => new(path, []);

    public static ExtractSourceSelection ForArchives(IReadOnlyList<String> paths) => new(null, paths);
}
