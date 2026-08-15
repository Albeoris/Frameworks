using Albeoris.Games.FF8.Toolset.Analysis.Model;

namespace Albeoris.Games.FF8.Toolset.Analysis;

internal sealed class ArchiveTreeBuilder(String archivePath, TranslationFileClassifier classifier)
{
    private readonly String archivePath = TranslationFileClassifier.Normalize(archivePath);
    private readonly TranslationFileClassifier classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
    private readonly List<AnalysisNode> roots = [];

    public IReadOnlyList<AnalysisNode> Roots => roots;

    public void AddFile(String relativePath, UInt64 size)
    {
        Add(relativePath, AnalysisNodeKind.File, size);
    }

    public void AddArchive(String relativePath, UInt64 size)
    {
        Add(relativePath, AnalysisNodeKind.Archive, size);
    }

    private void Add(String relativePath, AnalysisNodeKind leafKind, UInt64 size)
    {
        String normalizedPath = TranslationFileClassifier.Normalize(relativePath);
        String[] segments = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
            return;

        List<AnalysisNode> siblings = roots;
        String currentPath = String.Empty;
        for (Int32 index = 0; index < segments.Length; index++)
        {
            String segment = segments[index];
            currentPath = currentPath.Length == 0 ? segment : $"{currentPath}/{segment}";
            Boolean isLeaf = index == segments.Length - 1;
            AnalysisNodeKind kind = isLeaf ? leafKind : GetContainerKind(segment);
            AnalysisNode? node = siblings.FirstOrDefault(candidate =>
                candidate.Name.Equals(segment, StringComparison.OrdinalIgnoreCase));
            if (node is null)
            {
                node = new AnalysisNode(segment, currentPath, kind, isLeaf ? size : null);
                siblings.Add(node);
            }
            else if (isLeaf)
            {
                node.Kind = kind;
                node.Size = size;
            }

            if (isLeaf && kind == AnalysisNodeKind.File)
                node.TranslationCategories.AddRange(classifier.Classify($"{archivePath}/{normalizedPath}"));
            siblings = node.Children;
        }
    }

    private static AnalysisNodeKind GetContainerKind(String segment)
    {
        return segment.EndsWith(".fl", StringComparison.OrdinalIgnoreCase) ||
               segment.EndsWith(".zzz", StringComparison.OrdinalIgnoreCase)
            ? AnalysisNodeKind.Archive
            : AnalysisNodeKind.Directory;
    }
}
