using Albeoris.Games.FF8.Toolset.Analysis;
using Albeoris.Games.FF8.Toolset.Application;
using Albeoris.Games.FF8.Toolset.Infrastructure;
using Spectre.Console;

namespace Albeoris.Games.FF8.Toolset.Extraction;

internal sealed class ExtractionPlanBuilder(
    ExtractSourceSelector sourceSelector,
    NativePathDialogService dialogs,
    IAnsiConsole console,
    IApplicationLogger logger)
{
    private readonly ExtractSourceSelector sourceSelector = sourceSelector ?? throw new ArgumentNullException(nameof(sourceSelector));
    private readonly NativePathDialogService dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
    private readonly IAnsiConsole console = console ?? throw new ArgumentNullException(nameof(console));
    private readonly IApplicationLogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly GameArchiveScanner scanner = new();

    public ExtractionPlan Build(ExtractArguments arguments, Boolean interactive)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        logger.Information("Preparing extraction data.");

        (String? gamePath, IReadOnlyList<String> archivePaths) = ResolveInput(arguments, interactive);
        IReadOnlyList<ExtractionSource> sources = gamePath is not null
            ? FindGameArchives(gamePath)
            : ResolveArchiveSources(archivePaths);
        String outputPath = ResolveOutput(arguments.OutputPath, interactive);
        Boolean recursive = ResolveRecursive(arguments.Recursive, interactive);
        ArchivePathMatcher matcher = ArchivePathMatcher.Create(arguments.Masks);
        String tempPath = Path.Combine(Path.GetTempPath(), "Albeoris.FF8Toolset", "Extract");
        CreateDirectory(tempPath, "temporary");
        EnsureUniqueOutputPaths(sources);

        logger.Information($"Extraction source count: {sources.Count}");
        foreach (ExtractionSource source in sources)
            logger.Information($"Extraction source: {source.Path}");
        logger.Information($"Extraction output path: {outputPath}");
        logger.Information($"Recursive extraction: {recursive}");
        logger.Information($"Extraction masks: {(arguments.Masks.Count == 0 ? "<none>" : String.Join("; ", arguments.Masks))}");
        return new ExtractionPlan(sources, outputPath, tempPath, recursive, matcher);
    }

    private (String? GamePath, IReadOnlyList<String> ArchivePaths) ResolveInput(ExtractArguments arguments, Boolean interactive)
    {
        String? gamePath = CleanPath(arguments.GamePath);
        IReadOnlyList<String> archivePaths = arguments.GameArchives;
        if (gamePath is not null && archivePaths.Count > 0)
            throw new PreparationException("Specify either --game-path or --game-archive, not both.");
        if (gamePath is not null)
            return (ResolveGamePath(gamePath), []);
        if (archivePaths.Count > 0)
            return (null, archivePaths);
        if (!interactive)
            throw new PreparationException("--game-path or --game-archive is required in non-interactive mode.");

        ExtractSourceSelection selected = sourceSelector.Select();
        return selected.GamePath is not null
            ? (ResolveGamePath(selected.GamePath), [])
            : (null, selected.ArchivePaths);
    }

    private IReadOnlyList<ExtractionSource> FindGameArchives(String gamePath)
    {
        ExtractionSource[] sources = scanner.Find(gamePath)
            .Select(item => new ExtractionSource(item.Path, item.RelativePath, item.Kind))
            .ToArray();
        if (sources.Length == 0)
            throw new PreparationException("No .zzz or .fl archives were found in the game directory.");
        return sources;
    }

    private static IReadOnlyList<ExtractionSource> ResolveArchiveSources(IReadOnlyList<String> archivePaths)
    {
        List<ExtractionSource> result = [];
        HashSet<String> uniquePaths = new(StringComparer.OrdinalIgnoreCase);
        foreach (String value in archivePaths)
        {
            String path = GetFullPath(value, "game archive path");
            if (!File.Exists(path))
                throw new PreparationException($"Game archive '{path}' does not exist.");
            ArchiveWorkItemKind kind = ParseArchiveKind(path);
            if (kind == ArchiveWorkItemKind.Fl)
                EnsureFlCompanions(path);
            if (uniquePaths.Add(path))
                result.Add(new ExtractionSource(path, Path.GetFileName(path), kind));
        }
        if (result.Count == 0)
            throw new PreparationException("At least one game archive is required.");
        return result;
    }

    private String ResolveOutput(String? argument, Boolean interactive)
    {
        String? value = CleanPath(argument);
        if (value is null)
        {
            if (!interactive)
                throw new PreparationException("--output is required in non-interactive mode.");
            value = dialogs.SelectExtractionDirectory() ?? throw new OperationCanceledException();
        }
        String path = GetFullPath(value, "output path");
        CreateDirectory(path, "output");
        return path;
    }

    private Boolean ResolveRecursive(Boolean? argument, Boolean interactive)
    {
        if (argument is not null)
            return argument.Value;
        if (!interactive)
            throw new PreparationException("Specify --recursive or --no-recursive in non-interactive mode.");
        return console.Confirm("Extract nested archives recursively?", defaultValue: true);
    }

    private static String ResolveGamePath(String value)
    {
        String path = GetFullPath(value, "game path");
        if (!Directory.Exists(path))
            throw new PreparationException($"Game directory '{path}' does not exist.");
        return path;
    }

    private static ArchiveWorkItemKind ParseArchiveKind(String path)
    {
        String extension = Path.GetExtension(path);
        if (extension.Equals(".zzz", StringComparison.OrdinalIgnoreCase))
            return ArchiveWorkItemKind.Zzz;
        if (extension.Equals(".fl", StringComparison.OrdinalIgnoreCase))
            return ArchiveWorkItemKind.Fl;
        throw new PreparationException($"Game archive '{path}' must have a .zzz or .fl extension.");
    }

    private static void EnsureFlCompanions(String path)
    {
        String basePath = path[..^3];
        if (!File.Exists(basePath + ".fi") || !File.Exists(basePath + ".fs"))
            throw new PreparationException($"FL archive '{path}' requires matching .fi and .fs files.");
    }

    private static void EnsureUniqueOutputPaths(IEnumerable<ExtractionSource> sources)
    {
        HashSet<String> paths = new(StringComparer.OrdinalIgnoreCase);
        foreach (ExtractionSource source in sources)
        {
            if (!paths.Add(source.OutputRelativePath))
                throw new PreparationException($"Multiple archives would use the output path '{source.OutputRelativePath}'.");
        }
    }

    private static String GetFullPath(String value, String description)
    {
        try { return Path.TrimEndingDirectorySeparator(Path.GetFullPath(CleanPath(value)!)); }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        { throw new PreparationException($"The {description} is invalid.", exception); }
    }

    private static void CreateDirectory(String path, String description)
    {
        try { Directory.CreateDirectory(path); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        { throw new PreparationException($"Could not create the {description} directory '{path}'.", exception); }
    }

    private static String? CleanPath(String? value) => String.IsNullOrWhiteSpace(value) ? null : value.Trim().Trim('"');
}
