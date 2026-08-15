namespace Albeoris.Games.FF8.Toolset.Analysis;

internal sealed class GameArchiveScanner
{
    public IReadOnlyList<ArchiveWorkItem> Find(String gamePath)
    {
        List<ArchiveWorkItem> result = [];
        foreach (String path in Directory.EnumerateFiles(gamePath, "*.zzz", SearchOption.AllDirectories))
        {
            result.Add(new ArchiveWorkItem(
                path,
                NormalizeRelativePath(gamePath, path),
                ArchiveWorkItemKind.Zzz));
        }

        String dataPath = Path.Combine(gamePath, "Data");
        if (Directory.Exists(dataPath))
        {
            foreach (String languagePath in Directory.EnumerateDirectories(dataPath, "lang-*", SearchOption.TopDirectoryOnly))
            {
                foreach (String path in Directory.EnumerateFiles(languagePath, "*.fl", SearchOption.TopDirectoryOnly))
                {
                    result.Add(new ArchiveWorkItem(
                        path,
                        NormalizeRelativePath(gamePath, path),
                        ArchiveWorkItemKind.Fl));
                }
            }
        }

        return result
            .OrderBy(item => item.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static String NormalizeRelativePath(String rootPath, String path)
    {
        return TranslationFileClassifier.Normalize(Path.GetRelativePath(rootPath, path));
    }
}
