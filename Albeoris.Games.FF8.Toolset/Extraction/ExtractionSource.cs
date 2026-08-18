using Albeoris.Games.FF8.Toolset.Analysis;

namespace Albeoris.Games.FF8.Toolset.Extraction;

internal sealed record ExtractionSource(String Path, String OutputRelativePath, ArchiveWorkItemKind Kind);
