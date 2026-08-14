namespace Albeoris.Games.FF8.Toolset.Installations;

internal sealed class InstallationDiscoveryException(String message, Exception innerException) :
    Exception(message, innerException);
