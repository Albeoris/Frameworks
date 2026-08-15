namespace Albeoris.Games.FF8.Toolset.Analysis;

internal sealed class AnalysisExecutionException(String message, Exception innerException) :
    Exception(message, innerException);
