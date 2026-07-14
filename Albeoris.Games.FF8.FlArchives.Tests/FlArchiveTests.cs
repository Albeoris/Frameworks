using Albeoris.Games.Core.NsFileSystem;
using Albeoris.Games.FF8.FlArchives.Abstractions;
using Xunit;

namespace Albeoris.Games.FF8.FlArchives.Tests;

/// <summary>
/// Contains unit tests for the <see cref="FlArchive"/> class.
/// </summary>
public class FlArchiveTests
{
    /// <summary>
    /// Verifies that a synthetically built FL archive can be read, copied entry-by-entry into a
    /// new archive, and that the re-packed archive remains fully readable with identical content.
    /// The cycle is performed twice to confirm idempotent re-packing. Corner cases covered:
    /// zero-size entry, normal uncompressed entry, entry in a sub-directory.
    /// </summary>
    [Fact]
    public void PackUnpack_RoundTrip()
    {
        // Known content for each entry.
        Byte[] helloContent  = "Hello, World!"u8.ToArray();            // 13 bytes
        Byte[] binaryContent = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08]; // 8 bytes

        Dictionary<String, Byte[]> expectedContents = new()
        {
            ["empty.txt"]       = [],
            ["hello.txt"]       = helloContent,
            ["data/binary.bin"] = binaryContent,
        };

        (Byte[] flData, Byte[] fiData, Byte[] fsData) = BuildTestArchive(helloContent, binaryContent);

        // Two cycles: first with the hand-crafted binary, second with the re-packed output.
        for (Int32 cycle = 0; cycle < 2; cycle++)
        {
            using MemoryStream flSource = new(flData);
            using MemoryStream fiSource = new(fiData);
            using MemoryStream fsSource = new(fsData);

            using MemoryStream flTarget = new();
            using MemoryStream fiTarget = new();
            using MemoryStream fsTarget = new();

            using (IFlArchive source = FlArchive.Open(flSource, fiSource, fsSource, leaveOpen: true, FlArchiveRepresentation.Files))
            using (IFlArchive target = FlArchive.Open(flTarget, fiTarget, fsTarget, leaveOpen: true, FlArchiveRepresentation.Files))
            {
                Assert.Equal(expectedContents.Count, source.Entries.Count);

                foreach (IFlArchiveEntry sourceEntry in source.Entries)
                {
                    Assert.True(expectedContents.ContainsKey(sourceEntry.RelativePath),
                        $"Unexpected entry: {sourceEntry.RelativePath}");
                    Assert.Equal((UInt32)expectedContents[sourceEntry.RelativePath].Length, sourceEntry.Size);

                    Byte[] content = new Byte[sourceEntry.Size];
                    if (sourceEntry.Size > 0)
                    {
                        using Stream input = sourceEntry.OpenForRead();
                        input.ReadExactly(content);
                    }

                    Assert.Equal(expectedContents[sourceEntry.RelativePath], content);

                    IFlArchiveEntry targetEntry = target.AddEntry(sourceEntry.RelativePath);
                    using (Stream output = targetEntry.OpenForWrite(sourceEntry.Size))
                        output.Write(content);
                }
            }
            
            // Promote target to source for the next cycle.
            flData = flTarget.ToArray();
            fiData = fiTarget.ToArray();
            fsData = fsTarget.ToArray();
        }
    }

    /// <summary>
    /// Verifies that writing content larger than the original slot causes the entry to be
    /// relocated to the end of the content file, while leaving the old slot as unused space.
    /// The entry is subsequently readable with the new content.
    /// </summary>
    [Fact]
    public void Write_LargerContent_RelocatesToEndOfFile()
    {
        Byte[] smallContent = "Hi"u8.ToArray();      // 2 bytes
        Byte[] largeContent = "Hello, World!"u8.ToArray(); // 13 bytes

        (Byte[] flData, Byte[] fiData, Byte[] fsData) = BuildTestArchive(smallContent, largeContent);

        using MemoryStream fl = new();
        using MemoryStream fi = new();
        using MemoryStream fs = new();
        
        fl.Write(flData);
        fi.Write(fiData);
        fs.Write(fsData);

        fl.Position = 0;
        fi.Position = 0;
        fs.Position = 0;

        using IFlArchive archive = FlArchive.Open(fl, fi, fs, leaveOpen: true, FlArchiveRepresentation.Files);

        // hello.txt starts at offset 0 with 2 bytes; rewrite with 13 bytes forces relocation.
        IFlArchiveEntry helloEntry = archive.Entries.First(e => e.RelativePath == "hello.txt");
        using (Stream output = helloEntry.OpenForWrite((UInt32)largeContent.Length))
            output.Write(largeContent);

        // Verify the updated entry reads back the new content.
        Byte[] readBack = new Byte[helloEntry.Size];
        using (Stream input = helloEntry.OpenForRead())
            input.ReadExactly(readBack);

        Assert.Equal(largeContent, readBack);

        // Content file must be longer than the original (old slot + new slot at end).
        Assert.True(fs.Length > fsData.Length, "Content file should have grown after relocation.");
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds the three raw byte arrays (.fl, .fi, .fs) for a minimal archive containing:
    /// <list type="bullet">
    ///   <item>empty.txt — zero bytes</item>
    ///   <item>hello.txt — <paramref name="helloContent"/> bytes at content offset 0</item>
    ///   <item>data/binary.bin — <paramref name="binaryContent"/> bytes immediately after hello.txt</item>
    /// </list>
    /// </summary>
    private static (Byte[] fl, Byte[] fi, Byte[] fs) BuildTestArchive(Byte[] helloContent, Byte[] binaryContent)
    {
        using MemoryStream flMs = new();
        AppendListingLine(flMs, "empty.txt");
        AppendListingLine(flMs, "hello.txt");
        AppendListingLine(flMs, "data/binary.bin");

        using MemoryStream fiMs = new();
        // empty.txt: size=0, offset=0, compression=None
        WriteMetricsEntry(fiMs, size: 0, offset: 0);
        // hello.txt: size=N, offset=0, compression=None
        WriteMetricsEntry(fiMs, size: (UInt32)helloContent.Length, offset: 0);
        // data/binary.bin: size=M, offset=N, compression=None
        WriteMetricsEntry(fiMs, size: (UInt32)binaryContent.Length, offset: (UInt32)helloContent.Length);

        using MemoryStream fsMs = new();
        fsMs.Write(helloContent);
        fsMs.Write(binaryContent);

        return (flMs.ToArray(), fiMs.ToArray(), fsMs.ToArray());
    }

    private static void AppendListingLine(Stream stream, String relativePath)
    {
        // Use LF line endings so that listing positions are platform-independent.
        Byte[] bytes = FlArchive.PathEncoding.GetBytes(FlArchive.InternalPathPrefix + relativePath + "\n");
        stream.Write(bytes);
    }

    private static void WriteMetricsEntry(Stream stream, UInt32 size, UInt32 offset,
        FlCompressionMethod compression = FlCompressionMethod.None)
    {
        WriteUInt32LE(stream, size);
        WriteUInt32LE(stream, offset);
        WriteUInt32LE(stream, (UInt32)compression);
    }

    private static void WriteUInt32LE(Stream stream, UInt32 value)
    {
        Byte[] bytes = BitConverter.GetBytes(value);
        if (!BitConverter.IsLittleEndian) Array.Reverse(bytes);
        stream.Write(bytes, 0, 4);
    }
}