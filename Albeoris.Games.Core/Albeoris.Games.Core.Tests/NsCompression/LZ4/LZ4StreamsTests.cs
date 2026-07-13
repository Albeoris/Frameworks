using Albeoris.Games.Core.NSCompression.LZ4;

namespace Albeoris.Games.Core.Tests.NsCompression.LZ4;

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

/// <summary>
/// Tests for LZ4CompressionStream and LZ4DecompressionStream.
/// </summary>
public class LZ4StreamsTests
{
    [Fact]
    public void CompressDecompress_LiteralsOnly_ReturnsOriginalData()
    {
        String original = "Hello, LZ4!";
        Byte[] originalBytes = Encoding.UTF8.GetBytes(original);
        Byte[] decompressed;

        using (var msCompressed = new MemoryStream())
        {
            using (var compStream = new LZ4CompressionStream(msCompressed, leaveOpen: true))
            {
                compStream.Write(originalBytes, 0, originalBytes.Length);
                compStream.Flush();
            }

            msCompressed.Position = 0;
            using (var decompStream = new LZ4DecompressionStream(msCompressed, originalBytes.Length, leaveOpen: true))
            {
                decompressed = new Byte[originalBytes.Length];
                Int32 read = decompStream.Read(decompressed, 0, decompressed.Length);
                Assert.Equal(originalBytes.Length, read);
            }
        }

        Assert.Equal(originalBytes, decompressed);
    }

    [Fact]
    public void CompressDecompress_RepeatingPattern_ReturnsOriginalData()
    {
        String original = "ABCABCABCABCABCABCABCABC";
        Byte[] originalBytes = Encoding.ASCII.GetBytes(original);
        Byte[] decompressed;

        using (var msCompressed = new MemoryStream())
        {
            using (var compStream = new LZ4CompressionStream(msCompressed, leaveOpen: true))
            {
                compStream.Write(originalBytes, 0, originalBytes.Length);
                compStream.Flush();
            }

            msCompressed.Position = 0;
            using (var decompStream = new LZ4DecompressionStream(msCompressed, originalBytes.Length, leaveOpen: true))
            {
                decompressed = new Byte[originalBytes.Length];
                Int32 totalRead = 0;
                Int32 read;
                while ((read = decompStream.Read(decompressed, totalRead, decompressed.Length - totalRead)) > 0)
                    totalRead += read;
                Assert.Equal(originalBytes.Length, totalRead);
            }
        }

        Assert.Equal(originalBytes, decompressed);
    }

    [Fact]
    public async Task CompressDecompressAsync_ReturnsOriginalData()
    {
        String original = "Async LZ4 compression test.";
        Byte[] originalBytes = Encoding.ASCII.GetBytes(original);
        Byte[] decompressed;

        using (var msCompressed = new MemoryStream())
        {
            using (var compStream = new LZ4CompressionStream(msCompressed, leaveOpen: true))
            {
                await compStream.WriteAsync(originalBytes, 0, originalBytes.Length);
                await compStream.FlushAsync(default);
            }

            msCompressed.Position = 0;
            using (var decompStream = new LZ4DecompressionStream(msCompressed, originalBytes.Length, leaveOpen: true))
            {
                decompressed = new Byte[originalBytes.Length];
                Int32 totalRead = 0;
                Int32 read;
                while ((read = await decompStream.ReadAsync(decompressed, totalRead, decompressed.Length - totalRead)) > 0)
                    totalRead += read;
                Assert.Equal(originalBytes.Length, totalRead);
            }
        }

        Assert.Equal(originalBytes, decompressed);
    }

    [Fact]
    public void CompressDecompress_MultipleWrites_ReturnsOriginalData()
    {
        String part1 = "First part of data. ";
        String part2 = "Second part of data.";
        String original = part1 + part2;
        Byte[] originalBytes = Encoding.UTF8.GetBytes(original);
        Byte[] decompressed;

        using (var msCompressed = new MemoryStream())
        {
            using (var compStream = new LZ4CompressionStream(msCompressed, leaveOpen: true))
            {
                Byte[] bytes1 = Encoding.UTF8.GetBytes(part1);
                Byte[] bytes2 = Encoding.UTF8.GetBytes(part2);
                compStream.Write(bytes1, 0, bytes1.Length);
                compStream.Write(bytes2, 0, bytes2.Length);
                compStream.Flush();
            }

            msCompressed.Position = 0;
            using (var decompStream = new LZ4DecompressionStream(msCompressed, originalBytes.Length, leaveOpen: true))
            {
                decompressed = new Byte[originalBytes.Length];
                Int32 totalRead = 0;
                Int32 read;
                while ((read = decompStream.Read(decompressed, totalRead, decompressed.Length - totalRead)) > 0)
                    totalRead += read;
                Assert.Equal(originalBytes.Length, totalRead);
            }
        }

        Assert.Equal(originalBytes, decompressed);
    }
}