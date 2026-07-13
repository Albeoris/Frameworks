using System.Text;
using Albeoris.Games.Core.NSCompression.LZS;
using Xunit;

namespace Albeoris.Games.Core.Tests.NsCompression.LZS;

/// <summary>
/// Tests for the LZSCompressionStream.
/// </summary>
public class LZSCompressionStreamTests
{
    /// <summary>
    /// Tests that data compressed with LZSCompressionStream and then decompressed with LZSDecompressionStream
    /// matches the original data (literal-only data).
    /// </summary>
    [Fact]
    public void CompressDecompress_LiteralsOnly_ReturnsOriginalData()
    {
        String original = "Hello";
        Byte[] originalBytes = Encoding.ASCII.GetBytes(original);

        using (var compressedStream = new MemoryStream())
        {
            // Compress the data
            using (var compressionStream = new LZSCompressionStream(compressedStream, leaveOpen: true))
            {
                compressionStream.Write(originalBytes, 0, originalBytes.Length);
                compressionStream.Flush();
            }

            // Prepare for decompression
            compressedStream.Position = 0;
            using (var decompressionStream = new LZSDecompressionStream(compressedStream, (Int32)compressedStream.Length, originalBytes.Length, leaveOpen: false))
            {
                Byte[] decompressedBytes = new Byte[originalBytes.Length];
                Int32 bytesRead = decompressionStream.Read(decompressedBytes, 0, decompressedBytes.Length);
                Assert.Equal(originalBytes.Length, bytesRead);
                Assert.Equal(originalBytes, decompressedBytes);
            }
        }
    }

    /// <summary>
    /// Tests that data with repeating patterns (which should trigger pointer tokens) compresses and decompresses correctly.
    /// </summary>
    [Fact]
    public void CompressDecompress_RepeatingPattern_ReturnsOriginalData()
    {
        String original = "ABCABCABCABCABCABC";
        Byte[] originalBytes = Encoding.ASCII.GetBytes(original);

        using (var compressedStream = new MemoryStream())
        {
            // Compress the data
            using (var compressionStream = new LZSCompressionStream(compressedStream, leaveOpen: true))
            {
                compressionStream.Write(originalBytes, 0, originalBytes.Length);
                compressionStream.Flush();
            }

            // Prepare for decompression
            compressedStream.Position = 0;
            using (var decompressionStream = new LZSDecompressionStream(compressedStream, (Int32)compressedStream.Length, originalBytes.Length, leaveOpen: false))
            {
                Byte[] decompressedBytes = new Byte[originalBytes.Length];
                Int32 totalRead = 0;
                while (totalRead < originalBytes.Length)
                {
                    Int32 read = decompressionStream.Read(decompressedBytes, totalRead, originalBytes.Length - totalRead);
                    if (read == 0)
                        break;
                    totalRead += read;
                }

                Assert.Equal(originalBytes.Length, totalRead);
                Assert.Equal(originalBytes, decompressedBytes);
            }
        }
    }

    /// <summary>
    /// Tests compression and decompression when writing data in multiple chunks.
    /// </summary>
    [Fact]
    public void CompressDecompress_MultipleChunks_ReturnsOriginalData()
    {
        String original = "The quick brown fox jumps over the lazy lazy lazy dog.";
        Byte[] originalBytes = Encoding.ASCII.GetBytes(original);

        using (var compressedStream = new MemoryStream())
        {
            // Compress the data in chunks.
            using (var compressionStream = new LZSCompressionStream(compressedStream, leaveOpen: true))
            {
                Int32 chunkSize = 10;
                for (Int32 i = 0; i < originalBytes.Length; i += chunkSize)
                {
                    Int32 count = Math.Min(chunkSize, originalBytes.Length - i);
                    compressionStream.Write(originalBytes, i, count);
                }

                compressionStream.Flush();
            }

            // Prepare for decompression.
            compressedStream.Position = 0;
            using (var decompressionStream = new LZSDecompressionStream(compressedStream, (Int32)compressedStream.Length, originalBytes.Length, leaveOpen: false))
            {
                Byte[] decompressedBytes = new Byte[originalBytes.Length];
                Int32 totalRead = 0;
                while (totalRead < originalBytes.Length)
                {
                    Int32 read = decompressionStream.Read(decompressedBytes, totalRead, originalBytes.Length - totalRead);
                    if (read == 0)
                        break;
                    totalRead += read;
                }

                Assert.Equal(originalBytes.Length, totalRead);
                Assert.Equal(originalBytes, decompressedBytes);
            }
        }
    }

    /// <summary>
    /// Tests asynchronous compression and decompression.
    /// </summary>
    [Fact]
    public async Task CompressDecompressAsync_ReturnsOriginalData()
    {
        String original = "Hello, hello, asynchronous world!";
        Byte[] originalBytes = Encoding.ASCII.GetBytes(original);

        using (var compressedStream = new MemoryStream())
        {
            // Compress the data asynchronously.
            await using (var compressionStream = new LZSCompressionStream(compressedStream, leaveOpen: true))
            {
                await compressionStream.WriteAsync(originalBytes, 0, originalBytes.Length, TestContext.Current.CancellationToken);
                await compressionStream.FlushAsync(TestContext.Current.CancellationToken);
            }

            // Prepare for decompression.
            compressedStream.Position = 0;
            await using (var decompressionStream = new LZSDecompressionStream(compressedStream, (Int32)compressedStream.Length, originalBytes.Length, leaveOpen: false))
            {
                Byte[] decompressedBytes = new Byte[originalBytes.Length];
                Int32 totalRead = 0;
                while (totalRead < originalBytes.Length)
                {
                    Int32 read = await decompressionStream.ReadAsync(decompressedBytes, totalRead, originalBytes.Length - totalRead, TestContext.Current.CancellationToken);
                    if (read == 0)
                        break;
                    totalRead += read;
                }

                Assert.Equal(originalBytes.Length, totalRead);
                Assert.Equal(originalBytes, decompressedBytes);
            }
        }
    }
}