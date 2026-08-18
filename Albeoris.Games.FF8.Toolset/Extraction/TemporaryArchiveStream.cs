namespace Albeoris.Games.FF8.Toolset.Extraction;

internal sealed class TemporaryArchiveStream : IDisposable
{
    private TemporaryArchiveStream(FileStream stream) => Stream = stream;

    public FileStream Stream { get; }

    public static TemporaryArchiveStream Create(String tempPath, Func<Stream> openSource, String extension)
    {
        String path = Path.Combine(tempPath, $"ff8-{Guid.NewGuid():N}{extension}");
        FileStream? destination = null;
        try
        {
            destination = new FileStream(
                path,
                FileMode.CreateNew,
                FileAccess.ReadWrite,
                FileShare.Read | FileShare.Delete,
                81920,
                FileOptions.DeleteOnClose | FileOptions.SequentialScan);
            using Stream source = openSource();
            source.CopyTo(destination);
            destination.Position = 0;
            return new TemporaryArchiveStream(destination);
        }
        catch
        {
            destination?.Dispose();
            throw;
        }
    }

    public void Dispose() => Stream.Dispose();
}
