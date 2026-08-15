namespace Albeoris.Games.FF8.Toolset.Analysis.Model;

internal sealed class TranslatableFile(
    String archivePath,
    String path,
    UInt64 size,
    IReadOnlyList<TranslationCategory> categories)
{
    public String ArchivePath { get; } = archivePath;

    public String Path { get; } = path;

    public UInt64 Size { get; } = size;

    public IReadOnlyList<TranslationCategory> Categories { get; } = categories;
}
