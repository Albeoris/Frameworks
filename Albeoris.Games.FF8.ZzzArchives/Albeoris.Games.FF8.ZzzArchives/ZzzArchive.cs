using Albeoris.Games.FF8.ZzzArchives.Abstractions;

namespace Albeoris.Games.FF8.ZzzArchives;

public sealed partial class ZzzArchive : IZzzArchive
{
    private readonly Stream _archiveStream;
    private readonly EntryCollection _entries;

    private ZzzArchive(Stream archiveStream)
    {
        ArgumentNullException.ThrowIfNull(archiveStream);

        _archiveStream = archiveStream;
        _entries = archiveStream.Position == archiveStream.Length
            ? EntryCollection.CreateEmpty(archiveStream)
            : EntryCollectionReader.Read(archiveStream);
    }

    public IReadOnlyList<IZzzArchiveEntry> Entries => _entries.Entries;

    public IZzzArchiveEntry AddEntry(String relativePath) => _entries.AddEntry(relativePath);
    public void RemoveEntry(String relativePath) => _entries.RemoveEntry(relativePath);

    public void Flush() => _entries.Flush();

    public void Dispose()
    {
        Flush();
        _archiveStream.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        Flush();
        return _archiveStream.DisposeAsync();
    }
}