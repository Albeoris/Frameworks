using Albeoris.Games.FF8.FlArchives.Abstractions;
using Xunit;

namespace Albeoris.Games.FF8.FlArchives.Tests;

/// <summary>
/// Tests for <see cref="FlArchive.Compact"/> and <see cref="FlArchive.Optimize"/>.
/// </summary>
public class FlArchiveOptimizeTests
{
    // ── Compact ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Compact must remove unused space between entries so that the resulting content file is
    /// exactly as large as the sum of all entry sizes.
    /// </summary>
    [Fact]
    public void Compact_RemovesHoles()
    {
        Byte[] small = "Hi"u8.ToArray();         // 2 bytes
        Byte[] large = "Hello, World!"u8.ToArray(); // 13 bytes

        (Byte[] flData, Byte[] fiData, Byte[] fsData) = BuildTestArchive(
            [("small.txt", small), ("data.bin", large)]);

        using MemoryStream fl = new(); fl.Write(flData);
        using MemoryStream fi = new(); fi.Write(fiData);
        using MemoryStream fs = new(); fs.Write(fsData);
        fl.Position = 0; fi.Position = 0; fs.Position = 0;

        // Create a hole by overwriting small.txt with large content → old slot stays wasted.
        using (IFlArchive archive = FlArchive.Open(fl, fi, fs, leaveOpen: true, FlArchiveRepresentation.Files))
        {
            IFlArchiveEntry entry = archive.Entries.First(e => e.RelativePath == "small.txt");
            using (Stream s = entry.OpenForWrite((UInt32)large.Length))
                s.Write(large);
        }

        Int64 lengthWithHole = fs.Length;

        // Compact to a temp directory.
        String dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        try
        {
            String compactPath = Path.Combine(dir, "compact");

            fl.Position = 0; fi.Position = 0; fs.Position = 0;
            using (FlArchive source = (FlArchive)FlArchive.Open(fl, fi, fs, leaveOpen: true, FlArchiveRepresentation.Files))
                source.Compact(compactPath);

            // The compacted content file must equal the sum of current entry sizes (no holes).
            Int64 compactFsLength = new FileInfo(compactPath + ".fs").Length;
            UInt32 expectedSize = (UInt32)large.Length + (UInt32)large.Length; // small→large + data.bin
            Assert.Equal(expectedSize, compactFsLength);

            // Verify all entries are still readable with correct content.
            using (IFlArchive compacted = FlArchive.OpenForRead(compactPath, FlArchiveRepresentation.Files))
            {
                Assert.Equal(2, compacted.Entries.Count);

                IFlArchiveEntry smallEntry = compacted.Entries.First(e => e.RelativePath == "small.txt");
                ReadAndAssert(smallEntry, large); // content was updated before compacting

                IFlArchiveEntry dataEntry = compacted.Entries.First(e => e.RelativePath == "data.bin");
                ReadAndAssert(dataEntry, large);
            }
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>
    /// Compact must produce an archive whose entries are readable with the same content as the
    /// source archive, preserving their order.
    /// </summary>
    [Fact]
    public void Compact_PreservesContent()
    {
        Byte[] a = "AAAA"u8.ToArray();
        Byte[] b = "BBBBBB"u8.ToArray();
        Byte[] c = ""u8.ToArray();

        (Byte[] flData, Byte[] fiData, Byte[] fsData) = BuildTestArchive(
            [("a.bin", a), ("b.bin", b), ("empty.txt", c)]);

        String dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        try
        {
            String compactPath = Path.Combine(dir, "compact");

            using MemoryStream fl = new(flData);
            using MemoryStream fi = new(fiData);
            using MemoryStream fs = new(fsData);

            using (FlArchive source = (FlArchive)FlArchive.Open(fl, fi, fs, leaveOpen: true, FlArchiveRepresentation.Files))
                source.Compact(compactPath);

            using (IFlArchive compacted = FlArchive.OpenForRead(compactPath, FlArchiveRepresentation.Files))
            {
                Assert.Equal(3, compacted.Entries.Count);
                ReadAndAssert(compacted.Entries[0], a);
                ReadAndAssert(compacted.Entries[1], b);
                Assert.Equal(0u, compacted.Entries[2].Size);
            }
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    // ── Optimize ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Each entry's slot in the optimized content file must be at least as large as
    /// (size + absolute reserve + size * relative fraction).
    /// </summary>
    [Fact]
    public void Optimize_ContentSlotsArePadded()
    {
        Byte[] a = "Hello"u8.ToArray();   // 5 bytes
        Byte[] b = "World!"u8.ToArray();  // 6 bytes

        (Byte[] flData, Byte[] fiData, Byte[] fsData) = BuildTestArchive(
            [("a.txt", a), ("b.txt", b)]);

        FlOptimizeSpec spec = new FlOptimizeSpec
        {
            AbsoluteReserveBytes    = 100,
            RelativeReserveFraction = 0.5f,
            ExpectedNewEntries      = 0,
        };

        String dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        try
        {
            String optPath = Path.Combine(dir, "opt");

            using MemoryStream fl = new(flData);
            using MemoryStream fi = new(fiData);
            using MemoryStream fs = new(fsData);

            using (FlArchive source = (FlArchive)FlArchive.Open(fl, fi, fs, leaveOpen: true, FlArchiveRepresentation.Files))
                source.Optimize(spec, optPath);

            // Verify the total content file length reflects padding.
            // aSlot = 5 + 100 + (int)(5*0.5) = 107, bSlot = 6 + 100 + (int)(6*0.5) = 109
            UInt32 aSlot = (UInt32)(a.Length + spec.AbsoluteReserveBytes + (Int32)(a.Length * spec.RelativeReserveFraction));
            UInt32 bSlot = (UInt32)(b.Length + spec.AbsoluteReserveBytes + (Int32)(b.Length * spec.RelativeReserveFraction));
            Int64 expectedFsLength = aSlot + bSlot;

            Int64 actualFsLength = new FileInfo(optPath + ".fs").Length;
            Assert.Equal(expectedFsLength, actualFsLength);

            // After optimization the entry Size equals the padded slot size, not the original
            // content size. Verify by reading exactly the first <original> bytes of each slot.
            using (IFlArchive opt = FlArchive.OpenForRead(optPath, FlArchiveRepresentation.Files))
            {
                Byte[] headA = new Byte[a.Length];
                using (Stream s = opt.Entries[0].OpenForRead())
                    s.ReadExactly(headA);
                Assert.Equal(a, headA);

                Byte[] headB = new Byte[b.Length];
                using (Stream s = opt.Entries[1].OpenForRead())
                    s.ReadExactly(headB);
                Assert.Equal(b, headB);
            }
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>
    /// After optimization with <see cref="FlOptimizeSpec.ExpectedNewEntries"/> &gt; 0, the
    /// metrics file must contain pre-allocated zero-records at the end, and opening the archive
    /// must still yield only the original entries (pre-allocated bytes are ignored by the reader).
    /// </summary>
    [Fact]
    public void Optimize_MetricsPreAllocated_ReaderIgnoresExtraBytes()
    {
        Byte[] content = "data"u8.ToArray();
        (Byte[] flData, Byte[] fiData, Byte[] fsData) = BuildTestArchive([("entry.bin", content)]);

        FlOptimizeSpec spec = new FlOptimizeSpec
        {
            AbsoluteReserveBytes    = 0,
            RelativeReserveFraction = 0f,
            ExpectedNewEntries      = 5,
        };

        String dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        try
        {
            String optPath = Path.Combine(dir, "opt");

            using MemoryStream fl = new(flData);
            using MemoryStream fi = new(fiData);
            using MemoryStream fs = new(fsData);

            using (FlArchive source = (FlArchive)FlArchive.Open(fl, fi, fs, leaveOpen: true, FlArchiveRepresentation.Files))
                source.Optimize(spec, optPath);

            // Metrics file must be 1 valid record (12 bytes) + 5 pre-allocated records (60 bytes) = 72 bytes.
            Int64 expectedFiLength = (1 + 5) * 12L;
            Int64 actualFiLength = new FileInfo(optPath + ".fi").Length;
            Assert.Equal(expectedFiLength, actualFiLength);

            // Opening the archive must yield exactly 1 entry (pre-allocated records are ignored).
            using (IFlArchive opt = FlArchive.OpenForRead(optPath, FlArchiveRepresentation.Files))
            {
                Assert.Single(opt.Entries);
                ReadAndAssert(opt.Entries[0], content);
            }
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>
    /// An archive with pre-allocated metrics space must accept new entries which overwrite
    /// the pre-allocated records in-place without growing the metrics file.
    /// </summary>
    [Fact]
    public void Optimize_PreAllocatedArchive_CanAddEntries()
    {
        Byte[] original = "original"u8.ToArray();
        Byte[] added    = "added"u8.ToArray();

        (Byte[] flData, Byte[] fiData, Byte[] fsData) = BuildTestArchive([("original.bin", original)]);

        FlOptimizeSpec spec = new FlOptimizeSpec
        {
            AbsoluteReserveBytes    = 0,
            RelativeReserveFraction = 0f,
            ExpectedNewEntries      = 2,
        };

        String dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        try
        {
            String optPath = Path.Combine(dir, "opt");

            using MemoryStream fl = new(flData);
            using MemoryStream fi = new(fiData);
            using MemoryStream fs = new(fsData);

            using (FlArchive source = (FlArchive)FlArchive.Open(fl, fi, fs, leaveOpen: true, FlArchiveRepresentation.Files))
                source.Optimize(spec, optPath);

            Int64 fiBefore = new FileInfo(optPath + ".fi").Length; // 3 * 12 = 36

            // Open the optimised archive and add one new entry.
            using (IFlArchive opt = FlArchive.OpenForWrite(optPath, FlArchiveRepresentation.Files))
            {
                IFlArchiveEntry newEntry = opt.AddEntry("added.bin");
                using (Stream s = newEntry.OpenForWrite((UInt32)added.Length))
                    s.Write(added);
            }

            // Metrics file must NOT have grown (entry written into pre-allocated space).
            Int64 fiAfter = new FileInfo(optPath + ".fi").Length;
            Assert.Equal(fiBefore, fiAfter);

            // Both entries must be readable.
            using (IFlArchive opt = FlArchive.OpenForRead(optPath, FlArchiveRepresentation.Files))
            {
                Assert.Equal(2, opt.Entries.Count);
                ReadAndAssert(opt.Entries.First(e => e.RelativePath == "original.bin"), original);
                ReadAndAssert(opt.Entries.First(e => e.RelativePath == "added.bin"), added);
            }
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────────────────────

    private static (Byte[] fl, Byte[] fi, Byte[] fs) BuildTestArchive(
        (String RelativePath, Byte[] Content)[] entries)
    {
        using MemoryStream flMs = new();
        using MemoryStream fiMs = new();
        using MemoryStream fsMs = new();

        UInt32 offset = 0;
        foreach ((String path, Byte[] data) in entries)
        {
            AppendListingLine(flMs, path);
            WriteMetricsEntry(fiMs, (UInt32)data.Length, offset);
            fsMs.Write(data);
            offset += (UInt32)data.Length;
        }

        return (flMs.ToArray(), fiMs.ToArray(), fsMs.ToArray());
    }

    private static void AppendListingLine(Stream stream, String relativePath)
    {
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

    private static void ReadAndAssert(IFlArchiveEntry entry, Byte[] expected)
    {
        Byte[] actual = new Byte[entry.Size];
        if (entry.Size > 0)
        {
            using Stream s = entry.OpenForRead();
            s.ReadExactly(actual);
        }

        Assert.Equal(expected, actual);
    }
}
