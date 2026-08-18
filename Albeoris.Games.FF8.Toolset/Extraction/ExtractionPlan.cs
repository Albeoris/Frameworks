namespace Albeoris.Games.FF8.Toolset.Extraction;

internal sealed record ExtractionPlan(
    IReadOnlyList<ExtractionSource> Sources,
    String OutputPath,
    String TempPath,
    Boolean Recursive,
    ArchivePathMatcher Matcher);
