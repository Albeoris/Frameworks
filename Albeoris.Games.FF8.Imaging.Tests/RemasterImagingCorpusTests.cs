using System.Collections.Concurrent;
using Albeoris.Games.FF8.Imaging.Map;
using Albeoris.Games.FF8.Imaging.Mim;
using Albeoris.Games.FF8.Imaging.MimMap;
using Albeoris.Games.FF8.Imaging.Tdw;
using Albeoris.Games.FF8.Imaging.Tex;
using Albeoris.Games.FF8.Imaging.Tim;
using Albeoris.Games.FF8.Imaging.Workspace;
using Xunit;
using Xunit.Sdk;

namespace Albeoris.Games.FF8.Imaging.Tests;

public sealed class RemasterImagingCorpusTests
{
    /// <summary>The value must be the path to the directory containing the unpacked main.zzz archive.</summary>
    private const String RootEnvironmentVariable = "FF8_REMASTER_MAIN_ZZZ";
    private const Int32 MaximumConcurrentCorpusFiles = 4;

    private static readonly IReadOnlyDictionary<String, Int32> ExpectedCounts =
        new Dictionary<String, Int32>(StringComparer.OrdinalIgnoreCase)
        {
            [".tim"] = 279,
            [".tdw"] = 892,
            [".tex"] = 305,
            [".mim"] = 1_068,
            [".map"] = 1_067,
        };

    [Fact]
    public void AllRealFiles_ReadEveryByteRoundTripAndDecodeTextures()
    {
        String? root = Environment.GetEnvironmentVariable(RootEnvironmentVariable);
        if (String.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            throw SkipException.ForSkip($"Imaging corpus is unavailable. Set {RootEnvironmentVariable} to the directory containing the unpacked main.zzz archive.");

        List<String> paths = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(path => ExpectedCounts.ContainsKey(Path.GetExtension(path)))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
        Dictionary<String, Int32> counts = ExpectedCounts.Keys.ToDictionary(extension => extension, extension => paths.Count(path => String.Equals(Path.GetExtension(path), extension, StringComparison.OrdinalIgnoreCase)));
        ConcurrentQueue<CorpusFailure> failures = new();
        ParallelWorkspaceExportScheduler scheduler = new();

        Int32 validCount = 0;
        Parallel.ForEach(paths, new ParallelOptions { MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount, MaximumConcurrentCorpusFiles) }, path =>
        {
            String extension = Path.GetExtension(path);
            try
            {
                ValidateFile(path, extension, scheduler);
                Interlocked.Increment(ref validCount);
            }
            catch (Exception exception)
            {
                failures.Enqueue(new CorpusFailure(path, exception));
            }
        });

        foreach ((String extension, Int32 expected) in ExpectedCounts)
        {
            if (counts[extension] != expected)
            {
                failures.Enqueue(new CorpusFailure($"<{extension} corpus>", new InvalidDataException($"Found {counts[extension]} files instead of {expected}.")));
            }
        }

        if (failures.Count > 0)
        {
            String failedFiles = String.Join(
                Environment.NewLine,
                failures.Select((failure, index) =>
                    $"{index + 1}. {failure.Path} ({GetErrorSummary(failure.Exception)})"));
            Assert.Fail(
                $"{failures.Count} of {counts.Values.Sum()} imaging corpus files failed validation:"
                + Environment.NewLine
                + failedFiles);
        }
        
        TestContext.Current.TestOutputHelper?.WriteLine($"All {validCount} imaging corpus files are valid.");
    }

    private static void ValidateFile(String path, String extension, IWorkspaceExportScheduler scheduler)
    {
        Byte[] content = File.ReadAllBytes(path);
        Byte[] written = extension.ToLowerInvariant() switch
        {
            ".tim" => ValidateTim(content),
            ".tdw" => ValidateTdw(content),
            ".tex" => ValidateTex(content),
            ".mim" => ValidateMim(content),
            ".map" => ValidateMap(path, content, scheduler),
            _ => throw new InvalidOperationException($"Unexpected corpus extension {extension}."),
        };
        if (!content.AsSpan().SequenceEqual(written))
        {
            Int32 commonLength = Math.Min(content.Length, written.Length);
            Int32 difference = 0;
            while (difference < commonLength && content[difference] == written[difference])
                difference++;
            String values = difference < commonLength
                ? $"0x{content[difference]:X2} became 0x{written[difference]:X2}"
                : "one representation ended";
            throw new InvalidDataException(
                $"Round-trip differs at offset {difference}: {values}; lengths are {content.Length} and {written.Length}.");
        }
    }

    private static Byte[] ValidateTim(Byte[] content)
    {
        TimFile file = TimFile.Read(content);
        foreach (TimImage image in file.Images)
        {
            if (image.CanDecode)
                _ = image.Decode();
        }
        String workspace = GetWorkspacePath();
        TimWorkspaceExporter.Export(file, CreateExportSpec(workspace));
        Byte[] result = TimWorkspaceImporter.Import(CreateImportSpec(workspace)).Write();
        File.Delete(workspace);
        return result;
    }

    private static Byte[] ValidateTdw(Byte[] content)
    {
        TdwFile file = TdwFile.Read(content);
        foreach (TimImage image in file.Images)
        {
            if (image.CanDecode)
                _ = image.Decode();
        }
        String workspace = GetWorkspacePath();
        TdwWorkspaceExporter.Export(file, CreateExportSpec(workspace));
        Byte[] result = TdwWorkspaceImporter.Import(CreateImportSpec(workspace)).Write();
        File.Delete(workspace);
        return result;
    }

    private static Byte[] ValidateTex(Byte[] content)
    {
        TexFile file = TexFile.Read(content);
        _ = file.Decode();
        String workspace = GetWorkspacePath();
        TexWorkspaceExporter.Export(file, CreateExportSpec(workspace));
        Byte[] result = TexWorkspaceImporter.Import(CreateImportSpec(workspace)).Write();
        File.Delete(workspace);
        return result;
    }

    private static Byte[] ValidateMim(Byte[] content)
    {
        MimFile file = MimFile.Read(content);
        _ = file.GetColor(0, 0);
        return file.Write();
    }

    private static Byte[] ValidateMap(String mapPath, Byte[] mapContent, IWorkspaceExportScheduler? scheduler = null)
    {
        String mimPath = Path.ChangeExtension(mapPath, ".mim");
        if (!File.Exists(mimPath))
            throw new FileNotFoundException("The MAP has no accompanying MIM file.", mimPath);
        Byte[] mimContent = File.ReadAllBytes(mimPath);
        MimMapFile file = MimMapFile.Read(mimContent, mapContent);
        String workspace = GetWorkspacePath();
        MimMapWorkspaceExporter.Export(file, CreateExportSpec(workspace, scheduler));
        MimMapFile result = MimMapWorkspaceImporter.Import(CreateImportSpec(workspace));
        File.Delete(workspace);
        if (!mimContent.AsSpan().SequenceEqual(result.Mim.Write()))
            throw new InvalidDataException("The accompanying MIM differs after its workspace round-trip.");
        return result.Map.Write();
    }

    private static String GetErrorSummary(Exception exception)
    {
        Int32 lineEnd = exception.Message.IndexOfAny(['\r', '\n']);
        String message = lineEnd >= 0 ? exception.Message[..lineEnd] : exception.Message;
        return $"{exception.GetType().Name}: {message}";
    }

    private static String GetWorkspacePath() =>
        Path.Combine(Path.GetTempPath(), $"Albeoris.Imaging.Corpus.{Guid.NewGuid():N}.ora");

    private static WorkspaceExportSpec CreateExportSpec(String path, IWorkspaceExportScheduler? scheduler = null) => new(new WorkspaceLocation(path, WorkspaceContainerFormat.OpenRaster)) { Scheduler = scheduler ?? InlineWorkspaceExportScheduler.Instance };

    private static WorkspaceImportSpec CreateImportSpec(String path) => new(new WorkspaceLocation(path, WorkspaceContainerFormat.OpenRaster));

    private sealed record CorpusFailure(String Path, Exception Exception);
}
