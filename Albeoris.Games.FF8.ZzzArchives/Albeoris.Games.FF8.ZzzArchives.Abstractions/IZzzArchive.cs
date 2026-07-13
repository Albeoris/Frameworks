namespace Albeoris.Games.FF8.ZzzArchives.Abstractions;

public interface IZzzArchive : IDisposable, IAsyncDisposable
{
    IReadOnlyList<IZzzArchiveEntry> Entries { get; }
    
    IZzzArchiveEntry AddEntry(String relativePath);
    void RemoveEntry(String relativePath);
    void Flush();
}