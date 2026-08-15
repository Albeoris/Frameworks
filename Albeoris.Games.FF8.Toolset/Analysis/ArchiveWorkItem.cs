namespace Albeoris.Games.FF8.Toolset.Analysis;

internal enum ArchiveWorkItemKind
{
    Zzz,
    Fl,
}

internal sealed class ArchiveWorkItem(String path, String relativePath, ArchiveWorkItemKind kind)
{
    public String Path { get; } = path;

    public String RelativePath { get; } = relativePath;

    public ArchiveWorkItemKind Kind { get; } = kind;
}
