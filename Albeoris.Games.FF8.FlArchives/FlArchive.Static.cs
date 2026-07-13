using System.Text;
using Albeoris.Games.Core.Abstractions.NsCapacityCalculator;
using Albeoris.Games.Core.NsCapacityCalculator;
using Albeoris.Games.Core.NsDisposables;
using Albeoris.Games.Core.NsStreams;
using Albeoris.Games.FF8.FlArchives.Abstractions;

namespace Albeoris.Games.FF8.FlArchives;

public sealed partial class FlArchive
{
    public static Encoding PathEncoding { get; set; } = Encoding.ASCII;
    public static StringComparer PathComparer { get; set; } = StringComparer.OrdinalIgnoreCase;
    public static String InternalPathPrefix { get; set; } = @"c:\ff8\data\";
    public static Int32 MovingBufferSize { get; set; } = 64 * 1024 * 1024;

    public static IFlArchive OpenForRead(String archivePath) => Open(archivePath, File.OpenRead, leaveOpen: false);
    public static IFlArchive OpenForWrite(String archivePath) => Open(archivePath, File.OpenWrite, leaveOpen: false);
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