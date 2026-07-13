using System.Security.Cryptography;
using Albeoris.Games.FF8.ZzzArchives.Abstractions;
using Xunit;

namespace Albeoris.Games.FF8.ZzzArchives.Tests;

/// <summary>
/// Contains unit tests for the <see cref="ZzzArchive"/> class.
/// </summary>
public class ZzzArchiveTests
{
    [Fact]
    public void Do()
    {
        // Arrange
        Byte[] sourceData = Convert.FromBase64String(@"BAAAAAkAAABlbXB0eS50eHQdAAAAAAAAAAAAAAANAAAAZmY4LWVuX2h3LnJlZ4ECAAAAAAAAJgIAABEAAAB0ZXh0dXJlcy9udWxsLnBuZ6cEAAAAAAAAqQAAABYAAABkYXRhL2xhbmctZW4vdGV4dHMudHh0UAUAAAAAAAAbAAAAT0ZUV0FSRVxTcXVhcmUgU29mdCwgSW5jXEZJTkFMIEZBTlRBU1kgVklJSV0NCg0KW0hLRVlfTE9DQUxfTUFDSElORVxTT0ZUV0FSRVxTcXVhcmUgU29mdCwgSW5jXEZJTkFMIEZBTlRBU1kgVklJSVwxLjAwXQ0KIk1pZGlPcHRpb25zIj1kd29yZDowMDAwMDAwMQ0KIkluc3RhbGxPcHRpb25zIj1kd29yZDowMDAwMDBmZg0KIlNvdW5kT3B0aW9ucyI9ZHdvcmQ6MDAwMDAwMDANCiJEYXRhRHJpdmUiPSJkOiINCiJNSURJR1VJRCI9aGV4OmQwLGI0LGMyLDU4LGU3LDQ2LGQxLDExLDg5LGFjLDAwLGEwLGM5LDA1LDQxLDI5DQoiU291bmRHVUlEIj1oZXg6MDAsMDAsMDAsMDAsMDAsMDAsMDAsMDAsMDAsMDAsMDAsMDAsMDAsMDAsMDAsMDANCiJHcmFwaGljc0dVSUQiPWhleDowMCwwMCwwMCwwMCwwMCwwMCwwMCwwMCwwMCwwMCwwMCwwMCwwMCwwMCwwMCwwMA0KIkFwcFBhdGgiPSJjOlxnYW1lIg0KIkdyYXBoaWNzIj1kd29yZDoxMDEwMDAyMQ0KDQoAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABbSEtFWV9MT0NBTF9NQUNISU5FXFNPRlRXQVJFXFNxdWFyZSBTb2Z0LCBJbmNdDQoNCltIS0VZX0xPQ0FMX01BQ0hJTkVcU09GVFdBUkVcU3F1YXJlIFNvZnQsIEluY1xGSU5BTCBGQU5UQVNZIFZJSUldDQoNCltIS0VZX0xPQ0FMX01BQ0hJTkVcU09GVFdBUkVcU3F1YXJlIFNvZnQsIEluY1xGSU5BTCBGQU5UQVNZIFZJSUlcMS4wMF0NCiJNaWRpT3B0aW9ucyI9ZHdvcmQ6MDAwMDAwMDENCiJJbnN0YWxsT3B0aW9ucyI9ZHdvcmQ6MDAwMDAwZmYNCiJTb3VuZE9wdGlvbnMiPWR3b3JkOjAwMDAwMDAwDQoiRGF0YURyaXZlIj0iZDoiDQoiTUlESUdVSUQiPWhleDpkMCxiNCxjMiw1OCxlNyw0NixkMSwxMSw4OSxhYywwMCxhMCxjOSwwNSw0MSwyOQ0KIlNvdW5kR1VJRCI9aGV4OjAwLDAwLDAwLDAwLDAwLDAwLDAwLDAwLDAwLDAwLDAwLDAwLDAwLDAwLDAwLDAwDQoiR3JhcGhpY3NHVUlEIj1oZXg6MDAsMDAsMDAsMDAsMDAsMDAsMDAsMDAsMDAsMDAsMDAsMDAsMDAsMDAsMDAsMDANCiJBcHBQYXRoIj0iYzpcZ2FtZSINCiJHcmFwaGljcyI9ZHdvcmQ6MTAxMDAwMjENCg0KiVBORw0KGgoAAAANSUhEUgAAAAQAAAAECAYAAACp8Z5+AAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAadEVYdFNvZnR3YXJlAFBhaW50Lk5FVCB2My41LjExR/NCNwAAABhJREFUCNdjYGBg+A/CMMCAIYAkA8HoAgA/SyfZslvpSgAAAABJRU5ErkJggk5FVyBHQU1FDQpDb250aW51ZQ0KQ1JFRElUUw==");

        Dictionary<String, Byte[]> fileHashes = new()
        {
            {"empty.txt", Convert.FromBase64String("47DEQpj8HBSa+/TImW+5JCeuQeRkm5NMpJWZG3hSuFU=")},
            {"ff8-en_hw.reg", Convert.FromBase64String("sA2oLNRCPFhqQZJ8Tmx3gvq1Zaun/BtRAfZvzUFjpCE=")},
            {"textures/null.png", Convert.FromBase64String("ppmpjbHzmdD87ykI5lsZ8Po6xApNABeUWuFOP/e7cZ4=")},
            {"data/lang-en/texts.txt", Convert.FromBase64String("HxA2hTX6vT67p1uWhD/i/XyS5jHNp94EtnnKDFMZ7Ls=")},
        };
        
        // Act
        for (Int32 i = 0; i < 2; i++)
        {
            using (MemoryStream source = new(sourceData))
            using (MemoryStream target = new())
            {
                using (IZzzArchive sourceArchive = ZzzArchive.Open(source, leaveOpen: true))
                using (IZzzArchive targetArchive = ZzzArchive.Open(target, leaveOpen: true))
                {
                    foreach (IZzzArchiveEntry sourceEntry in sourceArchive.Entries)
                    {
                        Byte[] content = new Byte[sourceEntry.Size];
                        using (Stream input = sourceEntry.OpenForRead())
                            input.ReadExactly(content);
                        
                        Byte[] expectedHash = fileHashes[sourceEntry.RelativePath];
                        Byte[] readHash = SHA256.HashData(content);
                        Assert.Equal(expectedHash, readHash);

                        IZzzArchiveEntry targetEntry = targetArchive.AddEntry(sourceEntry.RelativePath);
                        using (Stream output = targetEntry.OpenForWrite(sourceEntry.Size))
                            output.Write(content);
                    }
                }

                sourceData = target.ToArray(); // Check repacked archive
            }
        }
    }
}