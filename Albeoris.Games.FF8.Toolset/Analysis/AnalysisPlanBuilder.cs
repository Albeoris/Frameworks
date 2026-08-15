using Albeoris.Games.FF8.Toolset.Application;
using Albeoris.Games.FF8.Toolset.Infrastructure;

namespace Albeoris.Games.FF8.Toolset.Analysis;

internal sealed class AnalysisPlanBuilder(
    GamePathSelector gamePathSelector,
    NativePathDialogService dialogs,
    IApplicationLogger logger)
{
    private readonly GamePathSelector gamePathSelector =
        gamePathSelector ?? throw new ArgumentNullException(nameof(gamePathSelector));
    private readonly NativePathDialogService dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
    private readonly IApplicationLogger logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public AnalysisPlan Build(AnalysisArguments arguments, Boolean interactive)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        logger.Information("Preparing analysis data.");

        String gamePath = ResolveGamePath(arguments.GamePath, interactive);
        String outputPath = ResolveOutputPath(arguments.OutputPath, interactive);
        String tempPath = ResolveTempPath(arguments.TempPath);
        AnalysisReportFormat format = ParseFormat(outputPath);

        logger.Information($"Analysis game path: {gamePath}");
        logger.Information($"Analysis output path: {outputPath}");
        logger.Information($"Analysis temporary path: {tempPath}");
        logger.Information($"Analysis report format: {format}");
        return new AnalysisPlan(gamePath, outputPath, tempPath, format);
    }

    private String ResolveGamePath(String? argument, Boolean interactive)
    {
        String? value = CleanPath(argument);
        if (value is null)
        {
            if (!interactive)
                throw new PreparationException("--game-path is required in non-interactive mode.");
            value = gamePathSelector.Select();
        }

        String fullPath = GetFullPath(value, "game path");
        if (!Directory.Exists(fullPath))
            throw new PreparationException($"Game directory '{fullPath}' does not exist.");
        return fullPath;
    }

    private String ResolveOutputPath(String? argument, Boolean interactive)
    {
        String? value = CleanPath(argument);
        if (value is null)
        {
            if (!interactive)
                throw new PreparationException("--output is required in non-interactive mode.");
            value = dialogs.SelectReportPath();
            if (value is null)
                throw new OperationCanceledException();
        }

        String fullPath = GetFullPath(value, "output path");
        _ = ParseFormat(fullPath);
        String? directoryPath = Path.GetDirectoryName(fullPath);
        if (!String.IsNullOrWhiteSpace(directoryPath))
            CreateDirectory(directoryPath, "output");
        return fullPath;
    }

    private String ResolveTempPath(String? argument)
    {
        String value = CleanPath(argument) ?? Path.Combine(Path.GetTempPath(), "Albeoris.FF8Toolset");
        String fullPath = GetFullPath(value, "temporary path");
        CreateDirectory(fullPath, "temporary");
        return fullPath;
    }

    private static AnalysisReportFormat ParseFormat(String outputPath)
    {
        String extension = Path.GetExtension(outputPath);
        if (String.IsNullOrWhiteSpace(extension))
            throw new PreparationException("The output path needs a .html or .json extension.");
        if (extension.Equals(".html", StringComparison.OrdinalIgnoreCase))
            return AnalysisReportFormat.Html;
        if (extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
            return AnalysisReportFormat.Json;
        throw new PreparationException("The output path must use the .html or .json extension.");
    }

    private static String GetFullPath(String path, String description)
    {
        try
        {
            return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new PreparationException($"The {description} is invalid.", exception);
        }
    }

    private static void CreateDirectory(String path, String description)
    {
        try
        {
            Directory.CreateDirectory(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new PreparationException($"Could not create the {description} directory '{path}'.", exception);
        }
    }

    private static String? CleanPath(String? value) =>
        String.IsNullOrWhiteSpace(value) ? null : value.Trim().Trim('"');
}
