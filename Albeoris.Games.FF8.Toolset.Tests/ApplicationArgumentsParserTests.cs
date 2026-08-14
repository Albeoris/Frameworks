using Albeoris.Games.FF8.Toolset.Application;
using Xunit;

namespace Albeoris.Games.FF8.Toolset.Tests;

public sealed class ApplicationArgumentsParserTests
{
    private readonly ApplicationArgumentsParser parser = new();

    [Fact]
    public void Parse_WithoutArguments_LeavesModeForInteractiveSelection()
    {
        ApplicationArguments result = parser.Parse([]);

        Assert.Null(result.Mode);
        Assert.False(result.NonInteractive);
        Assert.False(result.HelpRequested);
    }

    [Theory]
    [InlineData("--non-interactive")]
    [InlineData("-ni")]
    public void Parse_InstallationsWithNonInteractiveOption_ReturnsCompleteArguments(String option)
    {
        ApplicationArguments result = parser.Parse(["installations", option]);

        Assert.Equal(OperationMode.Installations, result.Mode);
        Assert.True(result.NonInteractive);
    }

    [Theory]
    [InlineData("/?")]
    [InlineData("-h")]
    [InlineData("--help")]
    public void Parse_HelpArgument_RequestsHelp(String option)
    {
        ApplicationArguments result = parser.Parse([option]);

        Assert.True(result.HelpRequested);
    }

    [Fact]
    public void Parse_UnknownMode_ReportsMode()
    {
        CommandLineException exception = Assert.Throws<CommandLineException>(() => parser.Parse(["unknown"]));

        Assert.Equal("Unknown mode 'unknown'.", exception.Message);
    }

    [Fact]
    public void Parse_ModeArgument_ReportsUnexpectedArgument()
    {
        CommandLineException exception = Assert.Throws<CommandLineException>(
            () => parser.Parse(["installations", "--game-path"]));

        Assert.Equal("Unknown argument '--game-path'.", exception.Message);
    }
}
