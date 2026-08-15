namespace Albeoris.Games.FF8.Toolset.Analysis.Model;

internal sealed class AnalysisNode
{
    public AnalysisNode(String name, String path, AnalysisNodeKind kind, UInt64? size = null)
    {
        Name = name;
        Path = path;
        Kind = kind;
        Size = size;
    }

    public String Name { get; }

    public String Path { get; }

    public AnalysisNodeKind Kind { get; internal set; }

    public UInt64? Size { get; internal set; }

    public List<TranslationCategory> TranslationCategories { get; } = [];

    public List<AnalysisNode> Children { get; } = [];
}
