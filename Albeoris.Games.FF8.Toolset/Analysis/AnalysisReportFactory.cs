using Albeoris.Games.FF8.Toolset.Analysis.Model;

namespace Albeoris.Games.FF8.Toolset.Analysis;

internal sealed class AnalysisReportFactory
{
    public AnalysisReport Create(String gamePath, IReadOnlyList<ArchiveAnalysis> archives)
    {
        List<TranslatableFile> translatableFiles = [];
        foreach (ArchiveAnalysis archive in archives)
            CollectTranslatableFiles(archive, archive.Children, translatableFiles);

        return new AnalysisReport(gamePath, DateTimeOffset.UtcNow, archives, translatableFiles);
    }

    private static void CollectTranslatableFiles(
        ArchiveAnalysis archive,
        IEnumerable<AnalysisNode> nodes,
        ICollection<TranslatableFile> destination)
    {
        foreach (AnalysisNode node in nodes)
        {
            if (node.Kind == AnalysisNodeKind.File && node.TranslationCategories.Count > 0)
            {
                destination.Add(new TranslatableFile(
                    archive.Path,
                    $"{archive.Path}/{node.Path}",
                    node.Size ?? 0,
                    node.TranslationCategories));
            }
            CollectTranslatableFiles(archive, node.Children, destination);
        }
    }
}
