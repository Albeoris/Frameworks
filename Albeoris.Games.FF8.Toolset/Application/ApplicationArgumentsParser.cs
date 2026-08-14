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
            EnsureNoAdditionalArguments(arguments);
            return new ApplicationArguments { HelpRequested = true };
        }

        OperationMode mode = ParseMode(arguments[0]);
        Boolean nonInteractive = false;
        Boolean helpRequested = false;

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

            throw new CommandLineException($"Unknown argument '{argument}'.");
        }

        return new ApplicationArguments
        {
            Mode = mode,
            NonInteractive = nonInteractive,
            HelpRequested = helpRequested,
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

        if (argument.StartsWith('-') || argument.StartsWith('/'))
            throw new CommandLineException("The first argument must be a mode.");

        throw new CommandLineException($"Unknown mode '{argument}'.");
    }

    private static Boolean IsHelpArgument(String argument)
    {
        return HelpArguments.Contains(argument, StringComparer.OrdinalIgnoreCase);
    }

    private static Boolean IsNonInteractiveArgument(String argument)
    {
        return NonInteractiveArguments.Contains(argument, StringComparer.OrdinalIgnoreCase);
    }

    private static void EnsureNoAdditionalArguments(IReadOnlyList<String> arguments)
    {
        if (arguments.Count > 1)
            throw new CommandLineException("Help cannot be combined with other arguments.");
    }
}
