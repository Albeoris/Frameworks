namespace Albeoris.Games.FF8.Toolset.Analysis.Model;

internal sealed class AnalysisReport(
    String gamePath,
    DateTimeOffset generatedAtUtc,
    IReadOnlyList<ArchiveAnalysis> archives,
    IReadOnlyList<TranslatableFile> translatableFiles)
{
    public Int32 SchemaVersion { get; } = 1;

    public String GamePath { get; } = gamePath;

    public DateTimeOffset GeneratedAtUtc { get; } = generatedAtUtc;

    public IReadOnlyList<ArchiveAnalysis> Archives { get; } = archives;

    public IReadOnlyList<TranslatableFile> TranslatableFiles { get; } = translatableFiles;
}
