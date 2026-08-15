namespace Albeoris.Games.FF8.Toolset.Application;

internal sealed class PreparationException : Exception
{
    public PreparationException(String message) : base(message)
    {
    }

    public PreparationException(String message, Exception innerException) : base(message, innerException)
    {
    }
}
