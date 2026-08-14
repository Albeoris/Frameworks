using Albeoris.Games.SteamLibrary.Abstractions.Models;
using Albeoris.Games.SteamLibrary.Tests.TestInfrastructure;
using Xunit;

namespace Albeoris.Games.SteamLibrary.Tests;

public sealed class SteamLibraryAccessorTests
{
    [Fact]
    public void EnumerateInstalledApplications_ReadsMultipleLibrariesAndIgnoresMissingManifests()
    {
        using TemporarySteamInstallation installation = new();
        SteamLibraryAccessor accessor = new(installation.RootPath);

        SteamApplication[] applications = accessor.EnumerateInstalledApplications().ToArray();

        Assert.Equal(2, applications.Length);
        Assert.Contains(applications, application => application.AppId == 42);
        Assert.Contains(applications, application => application.AppId == 7);
    }

    [Fact]
    public void FindApplicationById_ResolvesPathsFromContainingLibrary()
    {
        using TemporarySteamInstallation installation = new();
        SteamLibraryAccessor accessor = new(installation.RootPath);

        SteamApplication application = Assert.IsType<SteamApplication>(accessor.FindApplicationById(7));

        Assert.Equal("Second Game", application.Name);
        Assert.Equal(
            Path.Combine(installation.SecondLibraryPath, "steamapps", "common", "SecondGame"),
            application.InstallationDirectoryPath);
        Assert.Equal(new Uri("steam://uninstall/7"), application.UninstallationUri);
        Assert.Equal($"\"{accessor.SteamExecutablePath}\" steam://uninstall/7", application.UninstallationCommandLine);
    }

    [Fact]
    public void FindApplicationById_ReturnsNullWhenManifestIsMissing()
    {
        using TemporarySteamInstallation installation = new();
        SteamLibraryAccessor accessor = new(installation.RootPath);

        Assert.Null(accessor.FindApplicationById(999));
    }

    [Fact]
    public void Constructor_RejectsMissingDirectory()
    {
        String missingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        Assert.Throws<DirectoryNotFoundException>(() => new SteamLibraryAccessor(missingPath));
    }
}
