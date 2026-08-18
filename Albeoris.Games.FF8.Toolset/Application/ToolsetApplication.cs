using Albeoris.Games.FF8.Toolset.Analysis;
using Albeoris.Games.FF8.Toolset.Analysis.Reports;
using Albeoris.Games.FF8.Toolset.Installations;
using Albeoris.Games.FF8.Toolset.Infrastructure;
using Albeoris.Games.FF8.Toolset.Extraction;
using Spectre.Console;

namespace Albeoris.Games.FF8.Toolset.Application;

internal sealed class ToolsetApplication
{
    private readonly ApplicationArgumentsParser argumentsParser;
    private readonly ModeSelector modeSelector;
    private readonly HelpPresenter helpPresenter;
    private readonly InstallationsPlanBuilder installationsPlanBuilder;
    private readonly InstallationsOperation installationsOperation;
    private readonly AnalysisPlanBuilder analysisPlanBuilder;
    private readonly AnalysisOperation analysisOperation;
    private readonly ExtractionPlanBuilder extractionPlanBuilder;
    private readonly ExtractionOperation extractionOperation;
    private readonly PausePresenter pausePresenter;
    private readonly IAnsiConsole console;
    private readonly IApplicationLogger logger;

    private ToolsetApplication(
        ApplicationArgumentsParser argumentsParser,
        ModeSelector modeSelector,
        HelpPresenter helpPresenter,
        InstallationsPlanBuilder installationsPlanBuilder,
        InstallationsOperation installationsOperation,
        AnalysisPlanBuilder analysisPlanBuilder,
        AnalysisOperation analysisOperation,
        ExtractionPlanBuilder extractionPlanBuilder,
        ExtractionOperation extractionOperation,
        PausePresenter pausePresenter,
        IAnsiConsole console,
        IApplicationLogger logger)
    {
        this.argumentsParser = argumentsParser;
        this.modeSelector = modeSelector;
        this.helpPresenter = helpPresenter;
        this.installationsPlanBuilder = installationsPlanBuilder;
        this.installationsOperation = installationsOperation;
        this.analysisPlanBuilder = analysisPlanBuilder;
        this.analysisOperation = analysisOperation;
        this.extractionPlanBuilder = extractionPlanBuilder;
        this.extractionOperation = extractionOperation;
        this.pausePresenter = pausePresenter;
        this.console = console;
        this.logger = logger;
    }

    public static ToolsetApplication CreateDefault(FileApplicationLogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        IAnsiConsole console = AnsiConsole.Console;
        FinalFantasy8InstallationFinder finder = FinalFantasy8InstallationFinder.CreateDefault(logger);
        NativePathDialogService dialogs = new();
        TranslationFileClassifier classifier = new();
        ArchiveContainerAnalyzer archiveAnalyzer = new(classifier, logger);
        GameAnalyzer gameAnalyzer = new(
            new GameArchiveScanner(),
            archiveAnalyzer,
            new AnalysisReportFactory(),
            logger);
        return new ToolsetApplication(
            new ApplicationArgumentsParser(),
            new ModeSelector(console),
            new HelpPresenter(console),
            new InstallationsPlanBuilder(finder, logger),
            new InstallationsOperation(Console.Out, logger),
            new AnalysisPlanBuilder(new GamePathSelector(console, finder, dialogs), dialogs, logger),
            new AnalysisOperation(gameAnalyzer, new AnalysisReportFormatterFactory(), console, Console.Out, logger),
            new ExtractionPlanBuilder(new ExtractSourceSelector(console, finder, dialogs), dialogs, console, logger),
            new ExtractionOperation(new ArchiveExtractor(logger), console, Console.Out, logger),
            new PausePresenter(console),
            console,
            logger);
    }

    public Int32 Run(IReadOnlyList<String> rawArguments)
    {
        ArgumentNullException.ThrowIfNull(rawArguments);

        Boolean interactive = !argumentsParser.RequestsNonInteractive(rawArguments);
        Boolean pauseOnExit = interactive && !argumentsParser.RequestsHelp(rawArguments);

        try
        {
            logger.Information($"Arguments: {FormatArguments(rawArguments)}");
            logger.Information($"Interactive mode: {interactive}.");
            ApplicationArguments arguments = argumentsParser.Parse(rawArguments);

            if (arguments.HelpRequested)
            {
                logger.Information("Showing help.");
                helpPresenter.Show();
                return (Int32)ExitCode.Success;
            }

            OperationMode? requestedMode = arguments.Mode;
            while (true)
            {
                OperationMode? mode = requestedMode;
                if (mode is null)
                {
                    logger.Information("Selecting a mode interactively.");
                    mode = modeSelector.Select();
                    if (mode is null)
                    {
                        logger.Information("The user cancelled mode selection.");
                        return (Int32)ExitCode.Cancelled;
                    }
                }

                logger.Information($"Selected mode: {mode}.");
                try
                {
                    Execute(mode.Value, arguments, interactive);
                }
                catch (ReturnToModeSelectionException) when (interactive)
                {
                    logger.Information("Returning to mode selection.");
                    requestedMode = null;
                    continue;
                }

                logger.Information("The operation completed successfully.");

                if (interactive)
                    pausePresenter.WaitForExit();

                return (Int32)ExitCode.Success;
            }
        }
        catch (CommandLineException exception)
        {
            logger.Error("Argument parsing failed.", exception);
            return ShowInputError(exception.Message, pauseOnExit);
        }
        catch (InteractiveInputException exception)
        {
            logger.Error("Interactive input failed.", exception);
            return ShowInputError(exception.Message, pauseOnExit);
        }
        catch (PreparationException exception)
        {
            logger.Error("Operation preparation failed.", exception);
            return ShowInputError(exception.Message, pauseOnExit);
        }
        catch (OperationCanceledException)
        {
            logger.Information("The operation was cancelled.");
            return (Int32)ExitCode.Cancelled;
        }
        catch (InstallationDiscoveryException exception)
        {
            logger.Error("Installation discovery failed.", exception);
            return ShowExecutionError(exception.Message, pauseOnExit);
        }
        catch (AnalysisExecutionException exception)
        {
            logger.Error("Analysis failed.", exception);
            return ShowExecutionError(exception.Message, pauseOnExit);
        }
        catch (ExtractionExecutionException exception)
        {
            logger.Error("Extraction failed.", exception);
            return ShowExecutionError(exception.Message, pauseOnExit);
        }
        catch (Exception exception)
        {
            logger.Error("The operation failed.", exception);
            return ShowExecutionError("The operation failed.", pauseOnExit);
        }
    }

    private void Execute(OperationMode mode, ApplicationArguments arguments, Boolean interactive)
    {
        switch (mode)
        {
            case OperationMode.Installations:
                InstallationsPlan plan = installationsPlanBuilder.Build();
                installationsOperation.Execute(plan);
                return;
            case OperationMode.Analysis:
                AnalysisPlan analysisPlan = analysisPlanBuilder.Build(
                    arguments.Analysis ?? new AnalysisArguments(),
                    interactive);
                analysisOperation.ExecuteAsync(analysisPlan, interactive).GetAwaiter().GetResult();
                return;
            case OperationMode.Extract:
                ExtractionPlan extractionPlan = extractionPlanBuilder.Build(
                    arguments.Extract ?? new ExtractArguments(),
                    interactive);
                extractionOperation.ExecuteAsync(extractionPlan, interactive).GetAwaiter().GetResult();
                return;
            default:
                throw new InvalidOperationException($"Unsupported mode '{mode}'.");
        }
    }

    private Int32 ShowInputError(String message, Boolean pauseOnExit)
    {
        console.MarkupLine($"[red]Error:[/] {Markup.Escape(message)}");
        if (pauseOnExit)
            pausePresenter.WaitForExit();

        return (Int32)ExitCode.ArgumentError;
    }

    private Int32 ShowExecutionError(String message, Boolean pauseOnExit)
    {
        console.MarkupLine($"[red]Error:[/] {Markup.Escape(message)}");
        console.MarkupLine($"[grey]Details: {Markup.Escape(logger.LogPath)}[/]");

        if (pauseOnExit)
            pausePresenter.WaitForExit();

        return (Int32)ExitCode.ExecutionError;
    }

    private static String FormatArguments(IReadOnlyList<String> arguments)
    {
        if (arguments.Count == 0)
            return "<none>";

        return String.Join(' ', arguments.Select(argument => $"\"{argument.Replace("\"", "\\\"")}\""));
    }
}
