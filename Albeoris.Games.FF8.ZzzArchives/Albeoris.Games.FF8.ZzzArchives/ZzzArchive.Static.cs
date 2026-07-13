using System.Text;
using Albeoris.Games.Core.Abstractions.NsCapacityCalculator;
using Albeoris.Games.Core.NsCapacityCalculator;
using Albeoris.Games.Core.NsStreams;
using Albeoris.Games.FF8.ZzzArchives.Abstractions;

namespace Albeoris.Games.FF8.ZzzArchives;

public sealed partial class ZzzArchive
{
    public static Encoding PathEncoding { get; set; } = Encoding.UTF8;
    public static StringComparer PathComparer { get; set; } = StringComparer.OrdinalIgnoreCase;
    public static Int32 HeaderPadding { get; set; } = 5 * 1024 * 1024; // 5 MB
    public static Int32 MovingBufferSize { get; set; } = 64 * 1024 * 1024;
    
    public static IZzzArchive OpenForRead(String archivePath) => Open(File.OpenRead(archivePath), leaveOpen:false);
    public static IZzzArchive OpenForWrite(String archivePath) => Open(File.OpenWrite(archivePath), leaveOpen:false);
    public static IZzzArchive Create(String archivePath) => Open(File.Create(archivePath), leaveOpen:false);

    public static IZzzArchive Open(Stream archiveStream, Boolean leaveOpen)
    {
        ArgumentNullException.ThrowIfNull(archiveStream);
        
        if (!archiveStream.CanSeek)
            throw new ArgumentException("The stream must be seekable.", nameof(archiveStream));

        if (leaveOpen)
            archiveStream = new RestrictedStream(archiveStream) { CanFlush = false, CanDispose = false };
        
        return new ZzzArchive(archiveStream);
    }
    
    private static void ValidateEntry(ZzzArchiveEntry entry, ICapacityCalculator capacityCalculator)
    {
        if (entry.Offset < 0)
            throw new FormatException($"Invalid offset {entry.Offset} of entry [{entry.RelativePath}]");

        if (entry.Size != 0)
        {
            Int64 capacity = capacityCalculator.GetCapacity(entry.Offset);
            if (entry.Size > capacity)
                throw new FormatException($"The content of the entry is out of bounds. Entry: {entry.RelativePath}, Offset: {entry.Offset}, Size: {entry.Size}, Capacity: {capacity}");
        }
    }
}