using Albeoris.Games.FF8.FlArchives;
using Albeoris.Games.FF8.FlArchives.Abstractions;
using Albeoris.Games.FF8.Toolset.Analysis.Model;
using Albeoris.Games.FF8.Toolset.Infrastructure;
using Albeoris.Games.FF8.ZzzArchives;
using Albeoris.Games.FF8.ZzzArchives.Abstractions;

namespace Albeoris.Games.FF8.Toolset.Analysis;

internal sealed class ArchiveContainerAnalyzer(
    TranslationFileClassifier classifier,
    IApplicationLogger logger)
{
    private const Int32 MaximumContainerDepth = 32;

    private readonly TranslationFileClassifier classifier =
        classifier ?? throw new ArgumentNullException(nameof(classifier));
    private readonly IApplicationLogger logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public ArchiveAnalysis Analyze(ArchiveWorkItem item, String tempPath)
    {
        ArgumentNullException.ThrowIfNull(item);
        logger.Information($"Analyzing {item.Kind} archive: {item.Path}");
        ArchiveTreeBuilder tree = new(item.RelativePath, classifier);

        switch (item.Kind)
        {
            case ArchiveWorkItemKind.Zzz:
                using (IZzzArchive archive = ZzzArchive.OpenForRead(item.Path))
                    AnalyzeZzzEntries(archive, tree, String.Empty, tempPath, depth: 0);
                break;
            case ArchiveWorkItemKind.Fl:
                using (IFlArchive archive = FlArchive.OpenForRead(item.Path, FlArchiveRepresentation.Folder))
                    AnalyzeFlEntries(archive, tree, String.Empty, tempPath, depth: 0);
                break;
            default:
                throw new InvalidOperationException($"Unsupported archive kind '{item.Kind}'.");
        }

        FileInfo file = new(item.Path);
        return new ArchiveAnalysis(
            file.Name,
            item.RelativePath,
            item.Kind == ArchiveWorkItemKind.Zzz ? "zzz" : "fl",
            checked((UInt64)file.Length),
            tree.Roots);
    }

    private void AnalyzeZzzEntries(
        IZzzArchive archive,
        ArchiveTreeBuilder tree,
        String prefix,
        String tempPath,
        Int32 depth)
    {
        EnsureDepth(depth);
        Dictionary<String, IZzzArchiveEntry> entries = archive.Entries.ToDictionary(
            entry => TranslationFileClassifier.Normalize(entry.RelativePath),
            StringComparer.OrdinalIgnoreCase);

        foreach ((String path, IZzzArchiveEntry entry) in entries)
        {
            String targetPath = Combine(prefix, path);
            if (IsZzz(path) || IsFlTriplet(path, entries))
                tree.AddArchive(targetPath, entry.Size);
            else
                tree.AddFile(targetPath, entry.Size);
        }

        foreach ((String path, IZzzArchiveEntry entry) in entries)
        {
            String targetPath = Combine(prefix, path);
            if (IsZzz(path))
            {
                using MemoryStream nestedStream = CopyToMemory(entry.OpenForRead);
                using IZzzArchive nested = ZzzArchive.Open(nestedStream, leaveOpen: true);
                AnalyzeZzzEntries(nested, tree, targetPath, tempPath, depth + 1);
                continue;
            }

            if (!TryGetFlTriplet(path, entries, out IZzzArchiveEntry? listing, out IZzzArchiveEntry? indices, out IZzzArchiveEntry? content))
                continue;

            using TemporaryFlArchiveLease lease = TemporaryFlArchiveLease.Create(
                tempPath,
                listing!.OpenForRead,
                indices!.OpenForRead,
                content!.OpenForRead);
            AnalyzeFlEntries(lease.Archive, tree, targetPath, tempPath, depth + 1);
        }
    }

    private void AnalyzeFlEntries(
        IFlArchive archive,
        ArchiveTreeBuilder tree,
        String prefix,
        String tempPath,
        Int32 depth)
    {
        EnsureDepth(depth);
        Dictionary<String, IFlArchiveEntry> entries = archive.Entries.ToDictionary(
            entry => TranslationFileClassifier.Normalize(entry.RelativePath),
            StringComparer.OrdinalIgnoreCase);

        foreach ((String path, IFlArchiveEntry entry) in entries)
        {
            String targetPath = Combine(prefix, path);
            if (IsZzz(path) || IsFlTriplet(path, entries))
                tree.AddArchive(targetPath, entry.Size);
            else
                tree.AddFile(targetPath, entry.Size);
        }

        foreach ((String path, IFlArchiveEntry entry) in entries)
        {
            String targetPath = Combine(prefix, path);
            if (IsZzz(path))
            {
                using MemoryStream nestedStream = CopyToMemory(entry.OpenForRead);
                using IZzzArchive nested = ZzzArchive.Open(nestedStream, leaveOpen: true);
                AnalyzeZzzEntries(nested, tree, targetPath, tempPath, depth + 1);
                continue;
            }

            if (!TryGetFlTriplet(path, entries, out IFlArchiveEntry? listing, out IFlArchiveEntry? indices, out IFlArchiveEntry? content))
                continue;

            using TemporaryFlArchiveLease lease = TemporaryFlArchiveLease.Create(
                tempPath,
                listing!.OpenForRead,
                indices!.OpenForRead,
                content!.OpenForRead);
            AnalyzeFlEntries(lease.Archive, tree, targetPath, tempPath, depth + 1);
        }
    }

    private static MemoryStream CopyToMemory(Func<Stream> openSource)
    {
        MemoryStream result = new();
        using Stream source = openSource();
        source.CopyTo(result);
        result.Position = 0;
        return result;
    }

    private static Boolean IsZzz(String path) => path.EndsWith(".zzz", StringComparison.OrdinalIgnoreCase);

    private static Boolean IsFlTriplet<TEntry>(String path, IReadOnlyDictionary<String, TEntry> entries)
    {
        return TryGetFlTriplet(path, entries, out _, out _, out _);
    }

    private static Boolean TryGetFlTriplet<TEntry>(
        String path,
        IReadOnlyDictionary<String, TEntry> entries,
        out TEntry? listing,
        out TEntry? indices,
        out TEntry? content)
    {
        listing = default;
        indices = default;
        content = default;
        if (!path.EndsWith(".fl", StringComparison.OrdinalIgnoreCase))
            return false;

        String basePath = path[..^3];
        return entries.TryGetValue(path, out listing) &&
               entries.TryGetValue(basePath + ".fi", out indices) &&
               entries.TryGetValue(basePath + ".fs", out content);
    }

    private static String Combine(String prefix, String path)
    {
        return String.IsNullOrEmpty(prefix) ? path : $"{prefix}/{path}";
    }

    private static void EnsureDepth(Int32 depth)
    {
        if (depth > MaximumContainerDepth)
            throw new InvalidDataException($"Container nesting exceeds {MaximumContainerDepth} levels.");
    }
}
