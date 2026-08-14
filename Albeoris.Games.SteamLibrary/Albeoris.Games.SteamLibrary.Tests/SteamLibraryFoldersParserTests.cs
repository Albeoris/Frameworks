using System.Text;
using Albeoris.Games.SteamLibrary.Abstractions.Models;
using Xunit;

namespace Albeoris.Games.SteamLibrary.Tests;

public sealed class SteamLibraryFoldersParserTests
{
    [Fact]
    public void Parse_ReadsVerifiedTimestampUsedByCurrentSteamClients()
    {
        using MemoryStream input = CreateStream(
            """
            "libraryfolders"
            {
                "0"
                {
                    "path" "C:\\Steam"
                    "label" "Fast"
                    "contentid" "2519437954397554169"
                    "totalsize" "989700550656"
                    "update_clean_bytes_tally" "3063670327"
                    "time_last_update_verified" "1786407958"
                    "apps"
                    {
                        "1182900" "53070236837"
                    }
                }
            }
            """);

        SteamLibraryFolder folder = Assert.Single(SteamLibraryFoldersParser.Parse(input));

        Assert.Equal(@"C:\Steam", folder.Path);
        Assert.Equal("Fast", folder.Label);
        Assert.Equal(2519437954397554169UL, folder.ContentId);
        Assert.Equal(1786407958L, folder.LastUpdateVerifiedUnixTime);
        Assert.Null(folder.LastUpdateCorruptionUnixTime);
        Assert.Equal(53070236837UL, folder.Applications[1182900]);
    }

    [Fact]
    public void Parse_AllowsOptionalFolderMetadataToBeAbsent()
    {
        using MemoryStream input = CreateStream(
            """
            "libraryfolders"
            {
                "0"
                {
                    "path" "D:\\Games"
                }
            }
            """);

        SteamLibraryFolder folder = Assert.Single(SteamLibraryFoldersParser.Parse(input));

        Assert.Equal(@"D:\Games", folder.Path);
        Assert.Null(folder.ContentId);
        Assert.Empty(folder.Applications);
    }

    private static MemoryStream CreateStream(String value) => new(Encoding.UTF8.GetBytes(value));
}
