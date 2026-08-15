namespace Albeoris.Games.FF8.Toolset.Analysis.Model;

internal sealed class ArchiveAnalysis(
    String name,
    String path,
    String format,
    UInt64 size,
    IReadOnlyList<AnalysisNode> children)
{
    public String Name { get; } = name;

    public String Path { get; } = path;

    public String Format { get; } = format;

    public UInt64 Size { get; } = size;

    public IReadOnlyList<AnalysisNode> Children { get; } = children;
}
