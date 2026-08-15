using Albeoris.Games.FF8.FlArchives;
using Albeoris.Games.FF8.FlArchives.Abstractions;

namespace Albeoris.Games.FF8.Toolset.Analysis;

internal sealed class TemporaryFlArchiveLease : IDisposable
{
    private readonly FileStream listingStream;
    private readonly FileStream indicesStream;
    private readonly FileStream contentStream;
    private Boolean disposed;

    private TemporaryFlArchiveLease(
        FileStream listingStream,
        FileStream indicesStream,
        FileStream contentStream,
        IFlArchive archive)
    {
        this.listingStream = listingStream;
        this.indicesStream = indicesStream;
        this.contentStream = contentStream;
        Archive = archive;
    }

    public IFlArchive Archive { get; }

    public static TemporaryFlArchiveLease Create(
        String tempPath,
        Func<Stream> openListing,
        Func<Stream> openIndices,
        Func<Stream> openContent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tempPath);
        ArgumentNullException.ThrowIfNull(openListing);
        ArgumentNullException.ThrowIfNull(openIndices);
        ArgumentNullException.ThrowIfNull(openContent);

        String baseName = Path.Combine(tempPath, $"ff8-{Guid.NewGuid():N}");
        FileStream? listing = null;
        FileStream? indices = null;
        FileStream? content = null;
        IFlArchive? archive = null;
        try
        {
            listing = CreateTemporaryFile(baseName + ".fl");
            indices = CreateTemporaryFile(baseName + ".fi");
            content = CreateTemporaryFile(baseName + ".fs");
            CopyTo(openListing, listing);
            CopyTo(openIndices, indices);
            CopyTo(openContent, content);
            archive = FlArchive.Open(
                listing,
                indices,
                content,
                leaveOpen: true,
                FlArchiveRepresentation.Folder);
            return new TemporaryFlArchiveLease(listing, indices, content, archive);
        }
        catch
        {
            DisposeOwnedResources(archive, content, indices, listing);
            throw;
        }
    }

    public void Dispose()
    {
        if (disposed)
            return;

        try
        {
            Archive.Dispose();
        }
        finally
        {
            try
            {
                contentStream.Dispose();
            }
            finally
            {
                try
                {
                    indicesStream.Dispose();
                }
                finally
                {
                    listingStream.Dispose();
                    disposed = true;
                }
            }
        }
    }

    private static FileStream CreateTemporaryFile(String path)
    {
        return new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.ReadWrite,
            FileShare.Read | FileShare.Delete,
            bufferSize: 81920,
            FileOptions.DeleteOnClose | FileOptions.SequentialScan);
    }

    private static void CopyTo(Func<Stream> openSource, FileStream destination)
    {
        using Stream source = openSource();
        source.CopyTo(destination);
        destination.Position = 0;
    }

    private static void DisposeOwnedResources(
        IDisposable? archive,
        IDisposable? content,
        IDisposable? indices,
        IDisposable? listing)
    {
        try
        {
            archive?.Dispose();
        }
        finally
        {
            try
            {
                content?.Dispose();
            }
            finally
            {
                try
                {
                    indices?.Dispose();
                }
                finally
                {
                    listing?.Dispose();
                }
            }
        }
    }
}
