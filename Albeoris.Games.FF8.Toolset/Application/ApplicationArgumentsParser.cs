namespace Albeoris.Games.FF8.Toolset.Application;

internal sealed class ApplicationArgumentsParser
{
    private static readonly String[] HelpArguments = ["/?", "-h", "--help"];
    private static readonly String[] NonInteractiveArguments = ["-ni", "--non-interactive"];

    public ApplicationArguments Parse(IReadOnlyList<String> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        if (arguments.Count == 0)
            return new ApplicationArguments();

        if (IsHelpArgument(arguments[0]))
        {
            if (arguments.Count > 1)
                throw new CommandLineException("Help cannot be combined with other arguments.");
            return new ApplicationArguments { HelpRequested = true };
        }

        OperationMode mode = ParseMode(arguments[0]);
        Boolean nonInteractive = false;
        Boolean helpRequested = false;
        String? gamePath = null;
        String? outputPath = null;
        String? tempPath = null;
        List<String> gameArchives = [];
        List<String> masks = [];
        Boolean? recursive = null;

        for (Int32 index = 1; index < arguments.Count; index++)
        {
            String argument = arguments[index];
            if (IsHelpArgument(argument))
            {
                helpRequested = true;
                continue;
            }

            if (IsNonInteractiveArgument(argument))
            {
                if (nonInteractive)
                    throw new CommandLineException("--non-interactive was specified more than once.");
                nonInteractive = true;
                continue;
            }

            if (IsOption(argument, "-gp", "--game-path"))
            {
                EnsureMode(mode, argument, OperationMode.Analysis, OperationMode.Extract);
                gamePath = ReadValue(arguments, ref index, "--game-path", gamePath);
                continue;
            }
            if (IsOption(argument, "-o", "--output"))
            {
                EnsureMode(mode, argument, OperationMode.Analysis, OperationMode.Extract);
                outputPath = ReadValue(arguments, ref index, "--output", outputPath);
                continue;
            }
            if (IsOption(argument, "-tp", "--temp-path"))
            {
                EnsureMode(mode, argument, OperationMode.Analysis);
                tempPath = ReadValue(arguments, ref index, "--temp-path", tempPath);
                continue;
            }
            if (IsOption(argument, "-ga", "--game-archive"))
            {
                EnsureMode(mode, argument, OperationMode.Extract);
                gameArchives.Add(ReadValue(arguments, ref index, "--game-archive"));
                continue;
            }
            if (IsOption(argument, "-m", "--mask"))
            {
                EnsureMode(mode, argument, OperationMode.Extract);
                masks.Add(ReadValue(arguments, ref index, "--mask"));
                continue;
            }
            if (argument.Equals("--recursive", StringComparison.OrdinalIgnoreCase))
            {
                EnsureMode(mode, argument, OperationMode.Extract);
                recursive = ReadBooleanSwitch(recursive, true);
                continue;
            }
            if (argument.Equals("--no-recursive", StringComparison.OrdinalIgnoreCase))
            {
                EnsureMode(mode, argument, OperationMode.Extract);
                recursive = ReadBooleanSwitch(recursive, false);
                continue;
            }

            throw new CommandLineException($"Unknown argument '{argument}'.");
        }

        return new ApplicationArguments
        {
            Mode = mode,
            NonInteractive = nonInteractive,
            HelpRequested = helpRequested,
            Analysis = mode == OperationMode.Analysis
                ? new AnalysisArguments { GamePath = gamePath, OutputPath = outputPath, TempPath = tempPath }
                : null,
            Extract = mode == OperationMode.Extract
                ? new ExtractArguments
                {
                    GamePath = gamePath,
                    GameArchives = gameArchives,
                    OutputPath = outputPath,
                    Masks = masks,
                    Recursive = recursive,
                }
                : null,
        };
    }

    public Boolean RequestsNonInteractive(IReadOnlyList<String> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        return arguments.Any(IsNonInteractiveArgument);
    }

    public Boolean RequestsHelp(IReadOnlyList<String> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        return arguments.Any(IsHelpArgument);
    }

    private static OperationMode ParseMode(String argument)
    {
        if (argument.Equals("installations", StringComparison.OrdinalIgnoreCase))
            return OperationMode.Installations;
        if (argument.Equals("analysis", StringComparison.OrdinalIgnoreCase))
            return OperationMode.Analysis;
        if (argument.Equals("extract", StringComparison.OrdinalIgnoreCase))
            return OperationMode.Extract;
        if (argument.StartsWith('-') || argument.StartsWith('/'))
            throw new CommandLineException("The first argument must be a mode.");
        throw new CommandLineException($"Unknown mode '{argument}'.");
    }

    private static String ReadValue(
        IReadOnlyList<String> arguments,
        ref Int32 index,
        String optionName,
        String? currentValue)
    {
        if (currentValue is not null)
            throw new CommandLineException($"{optionName} was specified more than once.");
        if (++index >= arguments.Count || IsKnownOption(arguments[index]))
            throw new CommandLineException($"{optionName} requires a value.");
        if (String.IsNullOrWhiteSpace(arguments[index]))
            throw new CommandLineException($"{optionName} cannot be empty.");
        return arguments[index];
    }

    private static String ReadValue(IReadOnlyList<String> arguments, ref Int32 index, String optionName)
    {
        if (++index >= arguments.Count || IsKnownOption(arguments[index]))
            throw new CommandLineException($"{optionName} requires a value.");
        if (String.IsNullOrWhiteSpace(arguments[index]))
            throw new CommandLineException($"{optionName} cannot be empty.");
        return arguments[index];
    }

    private static Boolean ReadBooleanSwitch(Boolean? currentValue, Boolean value)
    {
        if (currentValue is not null)
            throw new CommandLineException("Specify either --recursive or --no-recursive once.");
        return value;
    }

    private static void EnsureMode(OperationMode mode, String argument, params OperationMode[] supportedModes)
    {
        if (!supportedModes.Contains(mode))
            throw new CommandLineException($"Unknown argument '{argument}'.");
    }

    private static Boolean IsKnownOption(String argument)
    {
        return IsHelpArgument(argument) || IsNonInteractiveArgument(argument) ||
               IsOption(argument, "-gp", "--game-path") || IsOption(argument, "-o", "--output") ||
               IsOption(argument, "-tp", "--temp-path") || IsOption(argument, "-ga", "--game-archive") ||
               IsOption(argument, "-m", "--mask") ||
               argument.Equals("--recursive", StringComparison.OrdinalIgnoreCase) ||
               argument.Equals("--no-recursive", StringComparison.OrdinalIgnoreCase);
    }

    private static Boolean IsOption(String argument, String shortName, String fullName)
    {
        return argument.Equals(shortName, StringComparison.OrdinalIgnoreCase) ||
               argument.Equals(fullName, StringComparison.OrdinalIgnoreCase);
    }

    private static Boolean IsHelpArgument(String argument) =>
        HelpArguments.Contains(argument, StringComparer.OrdinalIgnoreCase);

    private static Boolean IsNonInteractiveArgument(String argument) =>
        NonInteractiveArguments.Contains(argument, StringComparer.OrdinalIgnoreCase);
}
