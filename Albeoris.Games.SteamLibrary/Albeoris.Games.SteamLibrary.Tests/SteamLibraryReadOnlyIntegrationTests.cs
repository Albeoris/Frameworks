using System.Runtime.Versioning;
using Albeoris.Games.SteamLibrary.Abstractions.Models;
using Xunit;

namespace Albeoris.Games.SteamLibrary.Tests;

public sealed class SteamLibraryReadOnlyIntegrationTests
{
    [Fact]
    [SupportedOSPlatform("windows")]
    public void ConfiguredSteamInstallation_CanBeReadWithoutWriting()
    {
        SteamLibraryAccessor? accessor = SteamLibraryAccessor.FindInstalled();
        if (accessor is null)
            return;

        IReadOnlyList<SteamLibraryFolder> folders = accessor.ReadLibraryFolders();
        SteamApplication[] applications = accessor.EnumerateInstalledApplications().ToArray();

        Assert.NotEmpty(folders);
        Assert.NotEmpty(applications);
        Assert.All(applications, application => Assert.NotEqual(0U, application.AppId));
    }
}
