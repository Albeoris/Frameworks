using Albeoris.Games.FF8.FlArchives.Abstractions;
using Xunit;

namespace Albeoris.Games.FF8.FlArchives.Tests;

/// <summary>
/// Tests for <see cref="FlArchive"/> opened in <see cref="FlArchiveRepresentation.Folder"/> mode
/// (transparent sub-archive expansion).
/// </summary>
public class FlArchiveFolderTests
{
    // ── Read ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A parent archive that contains a sub-archive triplet must expose the sub-archive's inner
    /// entries prefixed with the sub-archive's .fl relative path, in original declaration order.
    /// The raw .fl/.fi/.fs component entries must not appear directly in Entries.
    /// </summary>
    [Fact]
    public void Read_SubArchive_ExposesFlatEntries()
    {
        Byte[] innerContent1 = "TextureData"u8.ToArray();
        Byte[] innerContent2 = "SoundData"u8.ToArray();

        (Byte[] parentFl, Byte[] parentFi, Byte[] parentFs) = BuildParentWithSubArchive(
            parentExtra: [("direct.tim", "DirectEntry"u8.ToArray())],
            subArchiveKey: "sub/archive.fl",
            innerEntries: [("tex/tex0.tim", innerContent1), ("tex/tex1.tim", innerContent2)]);

        using MemoryStream fl = new(parentFl);
        using MemoryStream fi = new(parentFi);
        using MemoryStream fs = new(parentFs);

        using IFlArchive archive = FlArchive.Open(fl, fi, fs, leaveOpen: true, FlArchiveRepresentation.Folder);

        // Expected: sub-archive entries with prefix, then direct entry (or in declaration order).
        // The parent declared the sub-archive triplet first, then direct.tim.
        Assert.Equal(3, archive.Entries.Count);

        IFlArchiveEntry e0 = archive.Entries[0];
        Assert.Equal("sub/archive.fl/tex/tex0.tim", e0.RelativePath);
        Assert.Equal((UInt32)innerContent1.Length, e0.Size);

        IFlArchiveEntry e1 = archive.Entries[1];
        Assert.Equal("sub/archive.fl/tex/tex1.tim", e1.RelativePath);
        Assert.Equal((UInt32)innerContent2.Length, e1.Size);

        IFlArchiveEntry e2 = archive.Entries[2];
        Assert.Equal("direct.tim", e2.RelativePath);
    }

    /// <summary>
    /// Reading content through a sub-archive composite entry must return the correct bytes.
    /// </summary>
    [Fact]
    public void Read_SubArchive_ContentIsCorrect()
    {
        Byte[] expected = "Hello from sub-archive!"u8.ToArray();

        (Byte[] fl, Byte[] fi, Byte[] fs) = BuildParentWithSubArchive(
            parentExtra: [],
            subArchiveKey: "data/inner.fl",
            innerEntries: [("file.bin", expected)]);

        using MemoryStream flMs = new(fl);
        using MemoryStream fiMs = new(fi);
        using MemoryStream fsMs = new(fs);

        using IFlArchive archive = FlArchive.Open(flMs, fiMs, fsMs, leaveOpen: true, FlArchiveRepresentation.Folder);

        Assert.Single(archive.Entries);
        IFlArchiveEntry entry = archive.Entries[0];
        Assert.Equal("data/inner.fl/file.bin", entry.RelativePath);

        Byte[] actual = new Byte[entry.Size];
        using (Stream s = entry.OpenForRead())
            s.ReadExactly(actual);

        Assert.Equal(expected, actual);
    }

    // ── Write ────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Writing to an existing sub-archive entry and flushing must persist the new content so
    /// that it can be read back after reopening in Folder mode.
    /// </summary>
    [Fact]
    public void Write_SubArchiveEntry_RoundTrip()
    {
        Byte[] original = "Original"u8.ToArray();
        Byte[] updated  = "Updated content here"u8.ToArray();

        (Byte[] fl, Byte[] fi, Byte[] fs) = BuildParentWithSubArchive(
            parentExtra: [],
            subArchiveKey: "inner.fl",
            innerEntries: [("data.bin", original)]);

        using MemoryStream flMs = new();
        using MemoryStream fiMs = new();
        using MemoryStream fsMs = new();
        flMs.Write(fl); fiMs.Write(fi); fsMs.Write(fs);
        flMs.Position = 0; fiMs.Position = 0; fsMs.Position = 0;

        // Write phase.
        using (IFlArchive archive = FlArchive.Open(flMs, fiMs, fsMs, leaveOpen: true, FlArchiveRepresentation.Folder))
        {
            IFlArchiveEntry entry = archive.Entries.Single(e => e.RelativePath == "inner.fl/data.bin");
            using (Stream s = entry.OpenForWrite((UInt32)updated.Length))
                s.Write(updated);
        }

        // Read-back phase.
        flMs.Position = 0; fiMs.Position = 0; fsMs.Position = 0;
        using (IFlArchive archive = FlArchive.Open(flMs, fiMs, fsMs, leaveOpen: true, FlArchiveRepresentation.Folder))
        {
            IFlArchiveEntry entry = archive.Entries.Single(e => e.RelativePath == "inner.fl/data.bin");
            Byte[] actual = new Byte[entry.Size];
            using (Stream s = entry.OpenForRead())
                s.ReadExactly(actual);
            Assert.Equal(updated, actual);
        }
    }

    /// <summary>
    /// When new content for a sub-archive entry is larger than the current slot in the parent
    /// content file, the parent must grow (entry relocated to end of parent .fs).
    /// </summary>
    [Fact]
    public void Write_SubArchiveEntry_LargerContent_ParentContentGrows()
    {
        Byte[] small = "Hi"u8.ToArray();
        Byte[] large = new Byte[4096]; // much larger than small
        large.AsSpan().Fill(0xAB);

        (Byte[] fl, Byte[] fi, Byte[] fs) = BuildParentWithSubArchive(
            parentExtra: [],
            subArchiveKey: "child.fl",
            innerEntries: [("payload.bin", small)]);

        Int64 originalFsLength = fs.Length;

        using MemoryStream flMs = new();
        using MemoryStream fiMs = new();
        using MemoryStream fsMs = new();
        flMs.Write(fl); fiMs.Write(fi); fsMs.Write(fs);
        flMs.Position = 0; fiMs.Position = 0; fsMs.Position = 0;

        using (IFlArchive archive = FlArchive.Open(flMs, fiMs, fsMs, leaveOpen: true, FlArchiveRepresentation.Folder))
        {
            IFlArchiveEntry entry = archive.Entries.Single(e => e.RelativePath == "child.fl/payload.bin");
            using (Stream s = entry.OpenForWrite((UInt32)large.Length))
                s.Write(large);
        }

        Assert.True(fsMs.Length > originalFsLength, "Parent content file must grow after relocation.");
    }

    /// <summary>
    /// Adding a new entry through a path containing a .fl segment must route it to the
    /// matching sub-archive, making it visible on the next open.
    /// </summary>
    [Fact]
    public void AddEntry_RoutedToSubArchive_VisibleOnReopen()
    {
        Byte[] existingContent = "existing"u8.ToArray();
        Byte[] newContent      = "brand new"u8.ToArray();

        (Byte[] fl, Byte[] fi, Byte[] fs) = BuildParentWithSubArchive(
            parentExtra: [],
            subArchiveKey: "arc.fl",
            innerEntries: [("existing.bin", existingContent)]);

        using MemoryStream flMs = new();
        using MemoryStream fiMs = new();
        using MemoryStream fsMs = new();
        flMs.Write(fl); fiMs.Write(fi); fsMs.Write(fs);
        flMs.Position = 0; fiMs.Position = 0; fsMs.Position = 0;

        // Add a new entry routed into the sub-archive.
        using (IFlArchive archive = FlArchive.Open(flMs, fiMs, fsMs, leaveOpen: true, FlArchiveRepresentation.Folder))
        {
            IFlArchiveEntry newEntry = archive.AddEntry("arc.fl/new.bin");
            using (Stream s = newEntry.OpenForWrite((UInt32)newContent.Length))
                s.Write(newContent);
        }

        // Reopen and verify.
        flMs.Position = 0; fiMs.Position = 0; fsMs.Position = 0;
        using (IFlArchive archive = FlArchive.Open(flMs, fiMs, fsMs, leaveOpen: true, FlArchiveRepresentation.Folder))
        {
            Assert.Equal(2, archive.Entries.Count);

            IFlArchiveEntry newEntry = archive.Entries.Single(e => e.RelativePath == "arc.fl/new.bin");
            Byte[] actual = new Byte[newEntry.Size];
            using (Stream s = newEntry.OpenForRead())
                s.ReadExactly(actual);
            Assert.Equal(newContent, actual);
        }
    }

    // ── Write: create new sub-archive ────────────────────────────────────────────────────────────

    /// <summary>
    /// Calling <see cref="IFlArchive.AddEntry"/> with a path that contains a <c>.fl</c> segment
    /// on a parent archive that has no such sub-archive yet must create the sub-archive triplet
    /// (<c>.fl</c>, <c>.fi</c>, <c>.fs</c>) in the parent and expose the new entry in the current
    /// session and after a reopen in <see cref="FlArchiveRepresentation.Folder"/> mode.
    /// </summary>
    [Fact]
    public void AddEntry_CreatesNewSubArchive_VisibleAfterReopen()
    {
        Byte[] content = "NewSubArchiveContent"u8.ToArray();

        using MemoryStream flMs = new();
        using MemoryStream fiMs = new();
        using MemoryStream fsMs = new();

        // ── Write phase ──────────────────────────────────────────────────────────────────────────
        using (IFlArchive archive = FlArchive.Open(flMs, fiMs, fsMs, leaveOpen: true, FlArchiveRepresentation.Folder))
        {
            IFlArchiveEntry entry = archive.AddEntry("sub.fl/data.bin");

            // Entry must be immediately visible in the current session.
            Assert.Single(archive.Entries);
            Assert.Equal("sub.fl/data.bin", entry.RelativePath);
            Assert.Equal("sub.fl/data.bin", archive.Entries[0].RelativePath);

            using (Stream s = entry.OpenForWrite((UInt32)content.Length))
                s.Write(content);
        }

        // ── Structural check on the raw parent ───────────────────────────────────────────────────
        // After flush the parent must contain exactly the three sub-archive component entries,
        // each with non-zero content.
        flMs.Position = 0; fiMs.Position = 0; fsMs.Position = 0;
        using (IFlArchive rawParent = FlArchive.Open(flMs, fiMs, fsMs, leaveOpen: true, FlArchiveRepresentation.Files))
        {
            Assert.Equal(3, rawParent.Entries.Count);
            Assert.True(rawParent.Entries.Any(e => e.RelativePath == "sub.fl"));
            Assert.True(rawParent.Entries.Any(e => e.RelativePath == "sub.fi"));
            Assert.True(rawParent.Entries.Any(e => e.RelativePath == "sub.fs"));
            Assert.True(rawParent.Entries.All(e => e.Size > 0), "All three sub-archive component entries must have non-zero content.");
        }

        // ── Read phase ───────────────────────────────────────────────────────────────────────────
        flMs.Position = 0; fiMs.Position = 0; fsMs.Position = 0;
        using (IFlArchive archive = FlArchive.Open(flMs, fiMs, fsMs, leaveOpen: true, FlArchiveRepresentation.Folder))
        {
            Assert.Single(archive.Entries);
            IFlArchiveEntry entry = archive.Entries[0];
            Assert.Equal("sub.fl/data.bin", entry.RelativePath);
            Assert.Equal((UInt32)content.Length, entry.Size);

            Byte[] actual = new Byte[entry.Size];
            using (Stream s = entry.OpenForRead())
                s.ReadExactly(actual);
            Assert.Equal(content, actual);
        }
    }

    /// <summary>
    /// Multiple entries can be added to the same new sub-archive in a single session and all
    /// must be readable after a reopen.
    /// </summary>
    [Fact]
    public void AddEntry_MultipleEntriesInNewSubArchive_AllVisibleAfterReopen()
    {
        Byte[] content1 = "FirstEntry"u8.ToArray();
        Byte[] content2 = "SecondEntry"u8.ToArray();
        Byte[] content3 = "ThirdEntry"u8.ToArray();

        using MemoryStream flMs = new();
        using MemoryStream fiMs = new();
        using MemoryStream fsMs = new();

        using (IFlArchive archive = FlArchive.Open(flMs, fiMs, fsMs, leaveOpen: true, FlArchiveRepresentation.Folder))
        {
            IFlArchiveEntry e1 = archive.AddEntry("new.fl/a.bin");
            using (Stream s = e1.OpenForWrite((UInt32)content1.Length))
                s.Write(content1);

            IFlArchiveEntry e2 = archive.AddEntry("new.fl/b.bin");
            using (Stream s = e2.OpenForWrite((UInt32)content2.Length))
                s.Write(content2);

            IFlArchiveEntry e3 = archive.AddEntry("new.fl/c.bin");
            using (Stream s = e3.OpenForWrite((UInt32)content3.Length))
                s.Write(content3);

            Assert.Equal(3, archive.Entries.Count);
        }

        flMs.Position = 0; fiMs.Position = 0; fsMs.Position = 0;
        using (IFlArchive archive = FlArchive.Open(flMs, fiMs, fsMs, leaveOpen: true, FlArchiveRepresentation.Folder))
        {
            Assert.Equal(3, archive.Entries.Count);

            IFlArchiveEntry e1 = archive.Entries.Single(e => e.RelativePath == "new.fl/a.bin");
            IFlArchiveEntry e2 = archive.Entries.Single(e => e.RelativePath == "new.fl/b.bin");
            IFlArchiveEntry e3 = archive.Entries.Single(e => e.RelativePath == "new.fl/c.bin");

            Byte[] a1 = new Byte[e1.Size]; using (Stream s = e1.OpenForRead()) s.ReadExactly(a1);
            Byte[] a2 = new Byte[e2.Size]; using (Stream s = e2.OpenForRead()) s.ReadExactly(a2);
            Byte[] a3 = new Byte[e3.Size]; using (Stream s = e3.OpenForRead()) s.ReadExactly(a3);

            Assert.Equal(content1, a1);
            Assert.Equal(content2, a2);
            Assert.Equal(content3, a3);
        }
    }

    /// <summary>
    /// Adding entries to a brand-new sub-archive while the parent also has direct (non-sub-archive)
    /// entries must not interfere with either set.
    /// </summary>
    [Fact]
    public void AddEntry_NewSubArchiveBesideDirectEntries_BothVisible()
    {
        Byte[] directContent = "DirectEntry"u8.ToArray();
        Byte[] innerContent  = "InnerEntry"u8.ToArray();

        using MemoryStream flMs = new();
        using MemoryStream fiMs = new();
        using MemoryStream fsMs = new();

        using (IFlArchive archive = FlArchive.Open(flMs, fiMs, fsMs, leaveOpen: true, FlArchiveRepresentation.Folder))
        {
            // Direct entry (no sub-archive routing).
            IFlArchiveEntry direct = archive.AddEntry("direct.bin");
            using (Stream s = direct.OpenForWrite((UInt32)directContent.Length))
                s.Write(directContent);

            // Entry routed into a brand-new sub-archive.
            IFlArchiveEntry inner = archive.AddEntry("root/pkg.fl/inner.bin");
            using (Stream s = inner.OpenForWrite((UInt32)innerContent.Length))
                s.Write(innerContent);

            // Two visible entries: direct.bin and pkg.fl/inner.bin.
            Assert.Equal(2, archive.Entries.Count);
        }

        flMs.Position = 0; fiMs.Position = 0; fsMs.Position = 0;
        using (IFlArchive archive = FlArchive.Open(flMs, fiMs, fsMs, leaveOpen: true, FlArchiveRepresentation.Files))
        {
            Assert.Equal(4, archive.Entries.Count);

            IFlArchiveEntry direct = archive.Entries.Single(e => e.RelativePath == "direct.bin");
            IFlArchiveEntry inner  = archive.Entries.Single(e => e.RelativePath == "root/pkg.fs");

            Byte[] ad = new Byte[direct.Size]; using (Stream s = direct.OpenForRead()) s.ReadExactly(ad);
            Byte[] ai = new Byte[inner.Size];  using (Stream s = inner.OpenForRead())  s.ReadExactly(ai);

            Assert.Equal(directContent, ad);
            Assert.Equal(innerContent,  ai);
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a parent archive (.fl/.fi/.fs byte arrays) that contains:
    /// <list type="bullet">
    ///   <item>A sub-archive triplet (<c>.fl</c>, <c>.fi</c>, <c>.fs</c>) for
    ///   <paramref name="subArchiveKey"/> holding <paramref name="innerEntries"/>.</item>
    ///   <item>Any additional <paramref name="parentExtra"/> direct entries.</item>
    /// </list>
    /// </summary>
    private static (Byte[] fl, Byte[] fi, Byte[] fs) BuildParentWithSubArchive(
        (String RelativePath, Byte[] Content)[] parentExtra,
        String subArchiveKey,
        (String RelativePath, Byte[] Content)[] innerEntries)
    {
        // Inner entries must carry the same root-relative prefix as the sub-archive key itself.
        // The prefix is the sub-archive key without the .fl extension, followed by the separator.
        // e.g. "sub/archive.fl" → prefix "sub/archive/"
        String innerDir = subArchiveKey.Substring(0, subArchiveKey.Length - 3) + "/";

        // Build inner archive streams.
        using MemoryStream innerFl = new();
        using MemoryStream innerFi = new();
        using MemoryStream innerFs = new();

        UInt32 innerOffset = 0;
        foreach ((String innerPath, Byte[] innerData) in innerEntries)
        {
            AppendListingLine(innerFl, innerDir + innerPath);
            WriteMetricsEntry(innerFi, (UInt32)innerData.Length, innerOffset);
            innerFs.Write(innerData);
            innerOffset += (UInt32)innerData.Length;
        }

        Byte[] innerFlBytes = innerFl.ToArray();
        Byte[] innerFiBytes = innerFi.ToArray();
        Byte[] innerFsBytes = innerFs.ToArray();

        // Derive sibling paths from subArchiveKey (strip .fl, add .fi / .fs).
        String basePath = subArchiveKey.Substring(0, subArchiveKey.Length - 3);
        String fiPath   = basePath + ".fi";
        String fsPath   = basePath + ".fs";

        // Build parent archive: sub-archive triplet first, then extras.
        using MemoryStream parentFl = new();
        using MemoryStream parentFi = new();
        using MemoryStream parentFs = new();

        UInt32 parentOffset = 0;

        void AppendParentEntry(String relativePath, Byte[] content)
        {
            AppendListingLine(parentFl, relativePath);
            WriteMetricsEntry(parentFi, (UInt32)content.Length, parentOffset);
            parentFs.Write(content);
            parentOffset += (UInt32)content.Length;
        }

        AppendParentEntry(subArchiveKey, innerFlBytes);
        AppendParentEntry(fiPath, innerFiBytes);
        AppendParentEntry(fsPath, innerFsBytes);

        foreach ((String extraPath, Byte[] extraData) in parentExtra)
            AppendParentEntry(extraPath, extraData);

        return (parentFl.ToArray(), parentFi.ToArray(), parentFs.ToArray());
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
}
