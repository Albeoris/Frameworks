using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Albeoris.Games.Core.NsStreams;
using Xunit;

namespace Albeoris.Games.Core.Tests.NsStreams;

public class StreamReaderExtensionsTests
{
    public static IEnumerable<object[]> Encodings() => new List<object[]>
    {
        new object[] { Encoding.ASCII },
        new object[] { Encoding.Latin1 },
        new object[] { Encoding.UTF8 },
        new object[] { Encoding.Unicode }, // UTF-16 LE
        new object[] { Encoding.BigEndianUnicode },
        new object[] { Encoding.UTF32 }
    };

    [Theory]
    [MemberData(nameof(Encodings))]
    public void GetBinaryPosition_MatchesExpected_ForVariousEncodings(Encoding encoding)
    {
        // Arrange
        string payload = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZабвгдΩ🙂";
        string text = string.Concat(Enumerable.Repeat(payload, 8)); // make it reasonably large
        byte[] bytes = encoding.GetBytes(text);

        using var ms = new MemoryStream(bytes);
        using var sr = new StreamReader(ms, encoding, detectEncodingFromByteOrderMarks: false, bufferSize: 32, leaveOpen: true);

        // Read several characters and accumulate them to compute expected byte count
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < 5; i++)
        {
            int ch = sr.Read();
            if (ch == -1) break;
            sb.Append((char)ch);
        }

        // Act
        long actual = sr.GetBinaryPosition();

        // Expected: number of bytes consumed for the characters we read
        long expected = encoding.GetByteCount(sb.ToString());

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetBinaryPosition_EmptyStream_ReturnsZero()
    {
        using var ms = new MemoryStream(Array.Empty<byte>());
        using var sr = new StreamReader(ms, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 16, leaveOpen: true);
        Assert.Equal(0, sr.GetBinaryPosition());
    }

    [Theory]
    [MemberData(nameof(Encodings))]
    public void GetBinaryPosition_BufferBoundaryCases(Encoding encoding)
    {
        // Prepare a string whose encoded bytes align with buffer boundaries
        string text = string.Concat(Enumerable.Repeat("ABCDEFGH", 4));
        byte[] bytes = encoding.GetBytes(text);

        using var ms = new MemoryStream(bytes);
        using var sr = new StreamReader(ms, encoding, detectEncodingFromByteOrderMarks: false, bufferSize: 32, leaveOpen: true);

        var sb = new System.Text.StringBuilder();
        // Read many single chars to advance through buffer boundaries
        for (int i = 0; i < 30; i++)
        {
            int ch = sr.Read();
            if (ch == -1) break;
            sb.Append((char)ch);
        }

        long actual = sr.GetBinaryPosition();

        long expected = encoding.GetByteCount(sb.ToString());
        Assert.Equal(expected, actual);
    }
}
