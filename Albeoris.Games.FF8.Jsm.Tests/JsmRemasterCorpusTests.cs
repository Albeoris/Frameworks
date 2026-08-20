using Xunit;
using Xunit.Sdk;

namespace Albeoris.Games.FF8.Jsm.Tests;

public sealed class JsmRemasterCorpusTests
{
    private const Int32 ExpectedFileCount = 5_003;
    private const String CorruptedFileName = "gwpool2_fr";
    private const String RootEnvironmentVariable = "FF8_REMASTER_MAIN_ZZZ";
    private static readonly String MapListRelativePath = Path.Combine(
        "data",
        "field.fl",
        "x",
        "field",
        "mapdata.fl",
        "maplist");

    [Fact]
    public void AllSupportedFiles_ReadAndWriteByteIdentical()
    {
        String? rootPath = Environment.GetEnvironmentVariable(RootEnvironmentVariable);
        if (!Directory.Exists(rootPath))
        {
            throw SkipException.ForSkip(
                $"JSM corpus is unavailable. Set {RootEnvironmentVariable} to the extracted main.zzz directory.");
        }

        String mapListPath = Path.Combine(rootPath, MapListRelativePath);
        HashSet<String> supportedFields = File.ReadLines(mapListPath)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        Int32 processedCount = 0;
        List<JsmCorpusFailure> failures = [];

        foreach (String path in EnumerateCorpus(rootPath, supportedFields))
        {
            processedCount++;

            try
            {
                Byte[] content = File.ReadAllBytes(path);
                JsmDocument document = Jsm.File.ReadDocument(content);

                Assert.Equal(content, document.Write());
            }
            catch (Exception exception)
            {
                failures.Add(new JsmCorpusFailure(path, exception));
            }
        }

        if (failures.Count > 0)
        {
            String failedFiles = String.Join(
                Environment.NewLine,
                failures.Select((failure, index) =>
                    $"{index + 1}. {failure.Path} ({GetErrorSummary(failure.Exception)})"));

            Assert.Fail(
                $"{failures.Count} of {processedCount} JSM files failed lossless round-trip:{Environment.NewLine}{failedFiles}");
        }

        Assert.Equal(ExpectedFileCount, processedCount);
    }

    private static IEnumerable<String> EnumerateCorpus(
        String rootPath,
        IReadOnlySet<String> supportedFields)
    {
        foreach (String path in Directory.EnumerateFiles(rootPath, "*.jsm", SearchOption.AllDirectories))
        {
            String fileName = Path.GetFileNameWithoutExtension(path);
            if (fileName.Equals(CorruptedFileName, StringComparison.OrdinalIgnoreCase))
                continue;

            Int32 separator = fileName.LastIndexOf('_');
            if (separator <= 0)
                continue;

            String fieldName = fileName[..separator];
            if (fieldName.StartsWith("test", StringComparison.OrdinalIgnoreCase)
                || !supportedFields.Contains(fieldName))
            {
                continue;
            }

            yield return path;
        }
    }

    private static String GetErrorSummary(Exception exception)
    {
        Int32 lineEnd = exception.Message.IndexOfAny(['\r', '\n']);
        String message = lineEnd >= 0 ? exception.Message[..lineEnd] : exception.Message;
        return $"{exception.GetType().Name}: {message}";
    }

    private sealed record JsmCorpusFailure(String Path, Exception Exception);
}
