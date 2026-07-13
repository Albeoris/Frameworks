namespace Albeoris.Games.FF8.ZzzArchives.Abstractions;

public interface IZzzArchiveEntry
{
    String RelativePath { get; }
    Int64 Offset { get; }
    UInt32 Size { get; }
    
    Stream OpenForRead();
    Stream OpenForWrite(UInt32 desiredSize);
}