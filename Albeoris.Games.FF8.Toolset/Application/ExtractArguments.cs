namespace Albeoris.Games.FF8.Toolset.Application;

internal sealed class ExtractArguments
{
    public String? GamePath { get; init; }

    public IReadOnlyList<String> GameArchives { get; init; } = [];

    public String? OutputPath { get; init; }

    public IReadOnlyList<String> Masks { get; init; } = [];

    public Boolean? Recursive { get; init; }
}
