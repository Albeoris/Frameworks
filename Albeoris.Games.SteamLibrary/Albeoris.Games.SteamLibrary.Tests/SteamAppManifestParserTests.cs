using System.Text;
using Albeoris.Games.SteamLibrary.Abstractions.Models;
using Xunit;

namespace Albeoris.Games.SteamLibrary.Tests;

public sealed class SteamAppManifestParserTests
{
    [Fact]
    public void Parse_ReadsCurrentManifestFormat()
    {
        using MemoryStream input = CreateStream(
            """
            "AppState"
            {
                "appid" "1182900"
                "universe" "1"
                "LauncherPath" "C:\\Steam\\steam.exe"
                "name" "A Plague Tale: Requiem"
                "StateFlags" "4"
                "installdir" "A Plague Tale Requiem"
                "LastUpdated" "1785630468"
                "LastPlayed" "1785719718"
                "SizeOnDisk" "53070236837"
                "buildid" "11415435"
                "LastOwner" "76561198022390750"
                "InstalledDepots"
                {
                    "1182901"
                    {
                        "manifest" "3367036266289852265"
                        "size" "53070236837"
                    }
                }
                "InstallScripts"
                {
                    "1182901" "installscript.vdf"
                }
                "UserConfig"
                {
                    "language" "english"
                    "BetaKey" "preview"
                    "highqualityaudio" "1"
                }
                "MountedConfig"
                {
                    "language" "english"
                }
            }
            """);

        SteamAppManifest manifest = SteamAppManifestParser.Parse(input);

        Assert.Equal(1182900U, manifest.Main.AppId);
        Assert.Equal(1UL, manifest.Main.Universe);
        Assert.Equal(SteamAppState.FullyInstalled, manifest.Main.State);
        Assert.Equal(@"C:\Steam\steam.exe", manifest.Main.LauncherPath);
        Assert.Equal(53070236837UL, manifest.Storage.SizeOnDisk);
        Assert.Equal(76561198022390750UL, manifest.Update.LastOwnerId);
        Assert.Equal(3367036266289852265UL, manifest.Update.InstalledDepots["1182901"].ManifestId);
        Assert.Equal("installscript.vdf", manifest.Update.InstallScripts["1182901"]);
        Assert.Equal("english", manifest.Configuration.User["LANGUAGE"]);
        Assert.Equal("preview", manifest.Configuration.User.BetaKey);
        Assert.True(manifest.Configuration.User.HighQualityAudio);
    }

    [Fact]
    public void Parse_AllowsOptionalFieldsAndSectionsToBeAbsent()
    {
        using MemoryStream input = CreateStream(
            """
            "AppState"
            {
                "appid" "10"
                "universe" "1"
                "name" "Minimal application"
                "installdir" "Minimal"
            }
            """);

        SteamAppManifest manifest = SteamAppManifestParser.Parse(input);

        Assert.Equal(SteamAppState.Invalid, manifest.Main.State);
        Assert.Null(manifest.Main.LauncherPath);
        Assert.Null(manifest.Storage.SizeOnDisk);
        Assert.Empty(manifest.Update.InstalledDepots);
        Assert.Empty(manifest.Configuration.User);
        Assert.Null(manifest.Configuration.User.HighQualityAudio);
    }

    private static MemoryStream CreateStream(String value) => new(Encoding.UTF8.GetBytes(value));
}
