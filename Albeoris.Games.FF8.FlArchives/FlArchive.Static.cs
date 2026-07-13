using System.Text;
using Albeoris.Games.Core.Abstractions.NsCapacityCalculator;
using Albeoris.Games.Core.NsCapacityCalculator;
using Albeoris.Games.Core.NsDisposables;
using Albeoris.Games.Core.NsStreams;
using Albeoris.Games.FF8.FlArchives.Abstractions;

namespace Albeoris.Games.FF8.FlArchives;

/// <summary>
/// A three-file archive format used in Final Fantasy VIII.
/// </summary>
/// <remarks>
/// Each archive is composed of three files that must share the same base name:
/// <list type="bullet">
///   <item>
///     <term>.fl — Listing file</term>
///     <description>
///       ASCII text; one entry per line. Each line is the full internal path of the stored file,
///       prefixed by <see cref="InternalPathPrefix"/> (default: <c>c:\ff8\data\</c>).
///       Lines may use either LF or CRLF line endings.
///     </description>
///   </item>
///   <item>
///     <term>.fi — Metrics (index) file</term>
///     <description>
///       Binary; no file header. Contains one 12-byte record per entry, in the same order as the
///       listing file. Record layout (little-endian):
///       <c>[UInt32 uncompressedSize][UInt32 contentOffset][Int32 compressionMethod]</c>.
///       <see cref="FlCompressionMethod"/> values: 0 = None, 1 = LZS, 2 = LZ4.
///       The entry count is derived from <c>fileSize / 12</c>.
///     </description>
///   </item>
///   <item>
///     <term>.fs — Content file</term>
///     <description>
///       Binary; raw file data at the offsets recorded in the metrics file.
///       Content layout depends on <see cref="FlCompressionMethod"/>:
///       <list type="bullet">
///         <item><term>None</term><description>Raw bytes, <c>uncompressedSize</c> bytes at <c>contentOffset</c>.</description></item>
///         <item><term>LZS</term><description><c>[UInt32 compressedSize][compressed bytes]</c> at <c>contentOffset</c>.</description></item>
///         <item><term>LZ4</term><description><c>[UInt32 totalSize = compressedSize + 8][UInt32 magic 0x5F4C5A34][UInt32 uncompressedSize][compressed bytes]</c> at <c>contentOffset</c>.</description></item>
///       </list>
///       Zero-size entries (<c>uncompressedSize == 0</c>) have a meaningless <c>contentOffset</c> and
///       occupy no space in the content file.
///     </description>
///   </item>
/// </list>
/// Write operations always store content as <see cref="FlCompressionMethod.None"/>. When the new
/// content fits in the existing slot, it is written in-place. Otherwise the content is appended to
/// the end of the <c>.fs</c> file, leaving the old slot as unused space.
/// </remarks>
public sealed partial class FlArchive
{
    public static Encoding PathEncoding { get; set; } = Encoding.ASCII;
    public static StringComparer PathComparer { get; set; } = StringComparer.OrdinalIgnoreCase;
    public static String InternalPathPrefix { get; set; } = @"c:\ff8\data\";
    public static Int32 MovingBufferSize { get; set; } = 64 * 1024 * 1024;

    public static IFlArchive OpenForRead(String archivePath) => Open(archivePath, File.OpenRead, leaveOpen: false);
    public static IFlArchive OpenForWrite(String archivePath) => Open(archivePath, f => new FileStream(f, FileMode.Open, FileAccess.ReadWrite), leaveOpen: false);
    public static IFlArchive Create(String archivePath) => Open(archivePath, File.Create, leaveOpen: false);

    private static IFlArchive Open(String archivePath, Func<String, Stream> streamFactory, Boolean leaveOpen)
    {
        String listingPath = Path.ChangeExtension(archivePath, ".fl");
        String indicesPath = Path.ChangeExtension(archivePath, ".fi");
        String contentPath = Path.ChangeExtension(archivePath, ".fs");

        using (DisposableStack disposableStack = new(capacity: 3))
        {
            Stream listingStream = disposableStack.Add(streamFactory(listingPath));
            Stream indicesStream = disposableStack.Add(streamFactory(indicesPath));
            Stream contentStream = disposableStack.Add(streamFactory(contentPath));
            disposableStack.Clear();
            return Open(listingStream, indicesStream, contentStream, leaveOpen);
        }
    }

    public static IFlArchive Open(Stream listingStream, Stream indicesStream, Stream contentStream, Boolean leaveOpen)
    {
        ArgumentNullException.ThrowIfNull(listingStream);
        ArgumentNullException.ThrowIfNull(indicesStream);
        ArgumentNullException.ThrowIfNull(contentStream);

        if (!listingStream.CanSeek) throw new ArgumentException("The stream must be seekable.", nameof(listingStream));
        if (!indicesStream.CanSeek) throw new ArgumentException("The stream must be seekable.", nameof(indicesStream));
        if (!contentStream.CanSeek) throw new ArgumentException("The stream must be seekable.", nameof(contentStream));

        if (leaveOpen) listingStream = new RestrictedStream(listingStream) { CanFlush = false, CanDispose = false };
        if (leaveOpen) indicesStream = new RestrictedStream(indicesStream) { CanFlush = false, CanDispose = false };
        if (leaveOpen) contentStream = new RestrictedStream(contentStream) { CanFlush = false, CanDispose = false };

        return new FlArchive(listingStream, indicesStream, contentStream);
    }

    private static void ValidateEntry(IFlArchiveEntry entry, ICapacityCalculator capacityCalculator)
    {
        if (entry.Offset < 0)
            throw new FormatException($"Invalid offset {entry.Offset} of entry [{entry.RelativePath}]");

        Int64 capacity = capacityCalculator.GetCapacity(entry.Offset);
        if (false && entry.Size > capacity)
            throw new FormatException($"The content of the entry is out of bounds. Entry: {entry.RelativePath}, Offset: {entry.Offset}, Size: {entry.Size}, Capacity: {capacity}");
    }
}