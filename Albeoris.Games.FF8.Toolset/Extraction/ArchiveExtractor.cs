using Albeoris.Games.FF8.FlArchives;
using Albeoris.Games.FF8.FlArchives.Abstractions;
using Albeoris.Games.FF8.Toolset.Analysis;
using Albeoris.Games.FF8.Toolset.Infrastructure;
using Albeoris.Games.FF8.ZzzArchives;
using Albeoris.Games.FF8.ZzzArchives.Abstractions;

namespace Albeoris.Games.FF8.Toolset.Extraction;

internal sealed class ArchiveExtractor(IApplicationLogger logger)
{
    private const Int32 MaximumContainerDepth = 32;
    private readonly IApplicationLogger logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public Int32 Extract(ExtractionSource source, ExtractionPlan plan, OperationProgressTracker? progress)
    {
        String outputRoot = GetSafeOutputPath(plan.OutputPath, source.OutputRelativePath);
        Directory.CreateDirectory(outputRoot);
        progress?.Start(source.Path, $"Extracting {source.Kind.ToString().ToUpperInvariant()}");
        try
        {
            Int32 extractedCount = source.Kind switch
            {
                ArchiveWorkItemKind.Zzz => ExtractZzzFile(source.Path, outputRoot, plan, source.Path, progress),
                ArchiveWorkItemKind.Fl => ExtractFlFile(source.Path, outputRoot, plan, source.Path, progress),
                _ => throw new InvalidOperationException($"Unsupported archive kind '{source.Kind}'."),
            };
            logger.Information($"Extracted {extractedCount} file(s) from: {source.Path}");
            return extractedCount;
        }
        finally
        {
            progress?.Complete(source.Path);
        }
    }

    private Int32 ExtractZzzFile(
        String archivePath,
        String outputRoot,
        ExtractionPlan plan,
        String progressKey,
        OperationProgressTracker? progress)
    {
        using IZzzArchive archive = ZzzArchive.OpenForRead(archivePath);
        return ExtractEntries(ToEntries(archive.Entries), outputRoot, String.Empty, plan, progressKey, progress, 0);
    }

    private Int32 ExtractFlFile(
        String archivePath,
        String outputRoot,
        ExtractionPlan plan,
        String progressKey,
        OperationProgressTracker? progress)
    {
        using IFlArchive archive = FlArchive.OpenForRead(archivePath, FlArchiveRepresentation.Files);
        return ExtractEntries(ToEntries(archive.Entries), outputRoot, String.Empty, plan, progressKey, progress, 0);
    }

    private Int32 ExtractEntries(
        IReadOnlyList<Entry> entries,
        String outputRoot,
        String prefix,
        ExtractionPlan plan,
        String progressKey,
        OperationProgressTracker? progress,
        Int32 depth)
    {
        if (depth > MaximumContainerDepth)
            throw new InvalidDataException($"Container nesting exceeds {MaximumContainerDepth} levels.");

        Dictionary<String, Entry> byPath = entries.ToDictionary(entry => Normalize(entry.Path), StringComparer.OrdinalIgnoreCase);
        HashSet<String> nestedFlComponents = plan.Recursive ? FindNestedFlComponents(byPath) : [];
        Int32 extractedCount = 0;

        foreach ((String path, Entry entry) in byPath)
        {
            String relativePath = Combine(prefix, path);
            if (plan.Recursive && TryGetFlTriplet(path, byPath, out Entry? listing, out Entry? indices, out Entry? content))
            {
                progress?.Update(progressKey, $"Extracting {relativePath}");
                String containerOutput = GetSafeOutputPath(outputRoot, relativePath);
                Directory.CreateDirectory(containerOutput);
                using TemporaryFlArchiveLease lease = TemporaryFlArchiveLease.Create(
                    plan.TempPath,
                    listing!.OpenForRead,
                    indices!.OpenForRead,
                    content!.OpenForRead);
                extractedCount += ExtractEntries(
                    ToEntries(lease.Archive.Entries),
                    outputRoot,
                    relativePath,
                    plan,
                    progressKey,
                    progress,
                    depth + 1);
                continue;
            }

            if (plan.Recursive && nestedFlComponents.Contains(path))
                continue;

            if (plan.Recursive && path.EndsWith(".zzz", StringComparison.OrdinalIgnoreCase))
            {
                progress?.Update(progressKey, $"Extracting {relativePath}");
                String containerOutput = GetSafeOutputPath(outputRoot, relativePath);
                Directory.CreateDirectory(containerOutput);
                using TemporaryArchiveStream temporary = TemporaryArchiveStream.Create(plan.TempPath, entry.OpenForRead, ".zzz");
                using IZzzArchive nested = ZzzArchive.Open(temporary.Stream, leaveOpen: true);
                extractedCount += ExtractEntries(
                    ToEntries(nested.Entries),
                    outputRoot,
                    relativePath,
                    plan,
                    progressKey,
                    progress,
                    depth + 1);
                continue;
            }

            if (!plan.Matcher.IsMatch(relativePath))
                continue;

            WriteEntry(entry, GetSafeOutputPath(outputRoot, relativePath));
            extractedCount++;
        }

        return extractedCount;
    }

    private static void WriteEntry(Entry entry, String outputPath)
    {
        String? directory = Path.GetDirectoryName(outputPath);
        if (!String.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        using Stream input = entry.OpenForRead();
        using FileStream output = new(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read, 81920, FileOptions.SequentialScan);
        input.CopyTo(output);
    }

    private static HashSet<String> FindNestedFlComponents(IReadOnlyDictionary<String, Entry> entries)
    {
        HashSet<String> result = new(StringComparer.OrdinalIgnoreCase);
        foreach (String path in entries.Keys)
        {
            if (!TryGetFlTriplet(path, entries, out _, out Entry? indices, out Entry? content))
                continue;
            result.Add(path);
            result.Add(Normalize(indices!.Path));
            result.Add(Normalize(content!.Path));
        }
        return result;
    }

    private static Boolean TryGetFlTriplet(
        String path,
        IReadOnlyDictionary<String, Entry> entries,
        out Entry? listing,
        out Entry? indices,
        out Entry? content)
    {
        listing = null;
        indices = null;
        content = null;
        if (!path.EndsWith(".fl", StringComparison.OrdinalIgnoreCase))
            return false;
        String basePath = path[..^3];
        return entries.TryGetValue(path, out listing) &&
               entries.TryGetValue(basePath + ".fi", out indices) &&
               entries.TryGetValue(basePath + ".fs", out content);
    }

    private static IReadOnlyList<Entry> ToEntries(IReadOnlyList<IZzzArchiveEntry> entries) =>
        entries.Select(entry => new Entry(entry.RelativePath, entry.Size, entry.OpenForRead)).ToArray();

    private static IReadOnlyList<Entry> ToEntries(IReadOnlyList<IFlArchiveEntry> entries) =>
        entries.Select(entry => new Entry(entry.RelativePath, entry.Size, entry.OpenForRead)).ToArray();

    private static String GetSafeOutputPath(String rootPath, String relativePath)
    {
        String root = Path.GetFullPath(rootPath);
        String candidate = Path.GetFullPath(Path.Combine(root, Normalize(relativePath).Replace('/', Path.DirectorySeparatorChar)));
        String rootPrefix = Path.EndsInDirectorySeparator(root) ? root : root + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Archive entry '{relativePath}' points outside the output directory.");
        return candidate;
    }

    private static String Normalize(String path) => path.Replace('\\', '/').TrimStart('/');

    private static String Combine(String prefix, String path) => String.IsNullOrEmpty(prefix) ? path : $"{prefix}/{path}";

    private sealed record Entry(String Path, UInt64 Size, Func<Stream> OpenForRead);
}
