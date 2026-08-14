using Albeoris.Games.FF8.Toolset.Infrastructure;
using Albeoris.Games.FF8.Toolset.Installations;
using Xunit;

namespace Albeoris.Games.FF8.Toolset.Tests;

public sealed class FinalFantasy8InstallationFinderTests : IDisposable
{
    private readonly String rootPath = Path.Combine(Path.GetTempPath(), $"FF8ToolsetTests.{Guid.NewGuid():N}");

    public FinalFantasy8InstallationFinderTests()
    {
        Directory.CreateDirectory(rootPath);
    }

    [Fact]
    public void FindInstalled_ReturnsUniqueExistingNonEmptyDirectories()
    {
        String installationPath = Path.Combine(rootPath, "Game");
        String emptyPath = Path.Combine(rootPath, "Empty");
        Directory.CreateDirectory(installationPath);
        Directory.CreateDirectory(emptyPath);
        File.WriteAllText(Path.Combine(installationPath, "FF8.exe"), String.Empty);

        StubInstallationSource firstSource = new(
            new InstallationCandidate(FinalFantasy8Release.ClassicPc, installationPath),
            new InstallationCandidate(FinalFantasy8Release.Steam2013, emptyPath));
        StubInstallationSource secondSource = new(
            new InstallationCandidate(
                FinalFantasy8Release.SteamRemastered2019,
                installationPath.ToUpperInvariant()));
        FinalFantasy8InstallationFinder finder = new(
            [firstSource, secondSource],
            new StubLogger());

        IReadOnlyList<FinalFantasy8Installation> result = finder.FindInstalled();

        FinalFantasy8Installation installation = Assert.Single(result);
        Assert.Equal(FinalFantasy8Release.ClassicPc, installation.Release);
        Assert.Equal(installationPath, installation.Path);
    }

    [Fact]
    public void FindInstalled_WhenSourceFails_ReportsDiscoveryFailure()
    {
        FinalFantasy8InstallationFinder finder = new(
            [new FailingInstallationSource()],
            new StubLogger());

        InstallationDiscoveryException exception = Assert.Throws<InstallationDiscoveryException>(
            finder.FindInstalled);

        Assert.Equal("Could not inspect installed games.", exception.Message);
        Assert.IsType<IOException>(exception.InnerException);
    }

    public void Dispose()
    {
        Directory.Delete(rootPath, recursive: true);
    }

    private sealed class StubInstallationSource(params InstallationCandidate[] candidates) : IInstallationSource
    {
        public IReadOnlyList<InstallationCandidate> FindCandidates()
        {
            return candidates;
        }
    }

    private sealed class FailingInstallationSource : IInstallationSource
    {
        public IReadOnlyList<InstallationCandidate> FindCandidates()
        {
            throw new IOException("Test failure.");
        }
    }

    private sealed class StubLogger : IApplicationLogger
    {
        public String LogPath => String.Empty;

        public void Information(String message)
        {
        }

        public void Warning(String message)
        {
        }

        public void Error(String message, Exception exception)
        {
        }
    }
}
