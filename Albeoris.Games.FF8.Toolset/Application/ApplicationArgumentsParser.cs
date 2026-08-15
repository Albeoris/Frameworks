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

            if (mode != OperationMode.Analysis)
                throw new CommandLineException($"Unknown argument '{argument}'.");

            if (IsOption(argument, "-gp", "--game-path"))
            {
                gamePath = ReadValue(arguments, ref index, "--game-path", gamePath);
                continue;
            }
            if (IsOption(argument, "-o", "--output"))
            {
                outputPath = ReadValue(arguments, ref index, "--output", outputPath);
                continue;
            }
            if (IsOption(argument, "-tp", "--temp-path"))
            {
                tempPath = ReadValue(arguments, ref index, "--temp-path", tempPath);
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

    private static Boolean IsKnownOption(String argument)
    {
        return IsHelpArgument(argument) || IsNonInteractiveArgument(argument) ||
               IsOption(argument, "-gp", "--game-path") || IsOption(argument, "-o", "--output") ||
               IsOption(argument, "-tp", "--temp-path");
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
