namespace Albeoris.Games.FF8.Toolset.Infrastructure;

internal interface IApplicationLogger
{
    String LogPath { get; }

    void Information(String message);

    void Warning(String message);

    void Error(String message, Exception exception);
}
