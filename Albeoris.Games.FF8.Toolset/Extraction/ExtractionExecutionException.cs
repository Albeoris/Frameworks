namespace Albeoris.Games.FF8.Toolset.Extraction;

internal sealed class ExtractionExecutionException(String message, Exception innerException)
    : Exception(message, innerException);
