using System.Runtime.Versioning;
using System.Security;
using Albeoris.Games.FF8.Toolset.Infrastructure;
using Microsoft.Win32;

namespace Albeoris.Games.FF8.Toolset.Installations;

[SupportedOSPlatform("windows")]
internal sealed class ClassicRegistryInstallationSource(IApplicationLogger logger) : IInstallationSource
{
    private const String ValueName = "AppPath";

    private static readonly RegistryLocation[] Locations =
    [
        new(RegistryHive.LocalMachine, @"SOFTWARE\Square Soft, Inc\FINAL FANTASY VIII\1.00"),
        new(RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Square Soft, Inc\FINAL FANTASY VIII\1.00"),
        new(RegistryHive.CurrentUser, @"VirtualStore\Machine\SOFTWARE\Square Soft, Inc\FINAL FANTASY VIII\1.00"),
        new(RegistryHive.CurrentUser, @"VirtualStore\Machine\SOFTWARE\Wow6432Node\Square Soft, Inc\FINAL FANTASY VIII\1.00"),
        new(RegistryHive.ClassesRoot, @"VirtualStore\MACHINE\SOFTWARE\Wow6432Node\Square Soft, Inc\FINAL FANTASY VIII\1.00"),
        new(RegistryHive.CurrentUser, @"Software\Classes\VirtualStore\MACHINE\SOFTWARE\Wow6432Node\Square Soft, Inc\FINAL FANTASY VIII\1.00"),
    ];

    private readonly IApplicationLogger logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public IReadOnlyList<InstallationCandidate> FindCandidates()
    {
        List<InstallationCandidate> candidates = [];

        foreach (RegistryLocation location in Locations)
        {
            String? path = ReadPath(location);
            if (String.IsNullOrWhiteSpace(path))
                continue;

            logger.Information($"Classic release registry candidate: {path}");
            candidates.Add(new InstallationCandidate(FinalFantasy8Release.ClassicPC, path));
        }

        return candidates;
    }

    private String? ReadPath(RegistryLocation location)
    {
        try
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(location.Hive, RegistryView.Default);
            using RegistryKey? key = baseKey.OpenSubKey(location.SubKeyName);
            return key?.GetValue(ValueName) as String;
        }
        catch (Exception exception) when (exception is IOException or SecurityException or UnauthorizedAccessException)
        {
            logger.Error($"Could not read {location.Hive}\\{location.SubKeyName}.", exception);
            return null;
        }
    }

    private sealed class RegistryLocation(RegistryHive hive, String subKeyName)
    {
        public RegistryHive Hive { get; } = hive;

        public String SubKeyName { get; } = subKeyName;
    }
}
