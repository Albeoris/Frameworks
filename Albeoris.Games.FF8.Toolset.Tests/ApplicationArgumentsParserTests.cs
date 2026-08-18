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

    [Fact]
    public void Parse_AnalysisArguments_ReturnsLocalModel()
    {
        ApplicationArguments result = parser.Parse(
            ["analysis", "-gp", @"C:\Games\FF8", "--output", "report.html", "-tp", @"C:\Temp", "-ni"]);

        Assert.Equal(OperationMode.Analysis, result.Mode);
        Assert.True(result.NonInteractive);
        AnalysisArguments analysis = Assert.IsType<AnalysisArguments>(result.Analysis);
        Assert.Equal(@"C:\Games\FF8", analysis.GamePath);
        Assert.Equal("report.html", analysis.OutputPath);
        Assert.Equal(@"C:\Temp", analysis.TempPath);
    }

    [Fact]
    public void Parse_AnalysisOptionWithoutValue_ReportsOption()
    {
        CommandLineException exception = Assert.Throws<CommandLineException>(
            () => parser.Parse(["analysis", "--game-path", "--output", "report.json"]));

        Assert.Equal("--game-path requires a value.", exception.Message);
    }

    [Fact]
    public void Parse_ExtractArguments_PreservesRepeatedArchivesAndMasks()
    {
        ApplicationArguments result = parser.Parse([
            "extract", "-ga", "main.zzz", "--game-archive", "other.zzz",
            "--output", "Files", "--mask", "*.bin;*.msd", "--recursive", "-ni"]);

        Assert.Equal(OperationMode.Extract, result.Mode);
        ExtractArguments extract = Assert.IsType<ExtractArguments>(result.Extract);
        Assert.Equal(["main.zzz", "other.zzz"], extract.GameArchives);
        Assert.Equal(["*.bin;*.msd"], extract.Masks);
        Assert.True(extract.Recursive);
        Assert.Equal("Files", extract.OutputPath);
    }

    [Fact]
    public void Parse_ExtractConflictingRecursiveSwitches_ReportsConflict()
    {
        CommandLineException exception = Assert.Throws<CommandLineException>(() =>
            parser.Parse(["extract", "--recursive", "--no-recursive"]));

        Assert.Equal("Specify either --recursive or --no-recursive once.", exception.Message);
    }
}
