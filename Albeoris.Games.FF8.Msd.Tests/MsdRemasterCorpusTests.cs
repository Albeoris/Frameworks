using System.Buffers.Binary;
using Albeoris.Games.FF8.TextEncoding;
using Albeoris.Games.FF8.TextEncoding.Tags;
using Xunit;
using Xunit.Sdk;

namespace Albeoris.Games.FF8.Msd.Tests;

public sealed class MsdRemasterCorpusTests
{
    private const Int32 ExpectedFileCount = 5_003;
    private const String CorruptedFileName = "gwpool2_fr";
    private const String RootEnvironmentVariable = "FF8_REMASTER_MAIN_ZZZ";
    private static readonly String MapListRelativePath = Path.Combine("data", "field.fl", "x", "field", "mapdata.fl", "maplist");

    [Fact]
    public void RoundTripComparison_NormalizesAnUnknownJapaneseFieldCharacter()
    {
        DefaultJapaneseFieldCharacters provider = new();
        FieldCharacterSet fieldCharacters = provider.Get("tgview1");
        FF8Encoding encoding = FF8Encoding.CreateJapanese(provider, "tgview1");
        FF8Encoding baseEncoding = FF8Encoding.CreateJapanese();
        Byte[] source = [4, 0, 0, 0, 3, 48, 28, 34, 243, 0];

        Byte[] written = MsdFile.Read(source, encoding).Write();

        Assert.Equal([4, 0, 0, 0, 3, 48, 95, 243, 0], written);
        AssertMatchesSourceExceptCanonicalFieldCharacters(source, written, baseEncoding, fieldCharacters);
    }

    [Fact]
    public void RoundTripComparison_NormalizesAFieldCharacterPresentInTheBaseCodepage()
    {
        DefaultJapaneseFieldCharacters provider = new();
        FieldCharacterSet fieldCharacters = provider.Get("bcmin22a");
        FF8Encoding encoding = FF8Encoding.CreateJapanese(provider, "bcmin22a");
        FF8Encoding baseEncoding = FF8Encoding.CreateJapanese();
        Byte[] source = [4, 0, 0, 0, 28, 33];

        Byte[] written = MsdFile.Read(source, encoding).Write();

        Assert.Equal([4, 0, 0, 0, 95], written);
        AssertMatchesSourceExceptCanonicalFieldCharacters(source, written, baseEncoding, fieldCharacters);

        Byte[] uniqueFieldSource = [4, 0, 0, 0, 28, 32];
        Byte[] uniqueFieldWritten = MsdFile.Read(uniqueFieldSource, encoding).Write();
        Assert.Equal(uniqueFieldSource, uniqueFieldWritten);
        AssertMatchesSourceExceptCanonicalFieldCharacters(
            uniqueFieldSource,
            uniqueFieldWritten,
            baseEncoding,
            fieldCharacters);
    }

    [Fact]
    public void AllSupportedFiles_ReadEveryByteAndRoundTrip()
    {
        String? rootPath = Environment.GetEnvironmentVariable(RootEnvironmentVariable);
        if (!Directory.Exists(rootPath))
            throw SkipException.ForSkip($"MSD corpus is unavailable. Set {RootEnvironmentVariable} to the extracted main.zzz directory.");

        String mapListPath = Path.Combine(rootPath, MapListRelativePath);
        HashSet<String> supportedFields = File.ReadLines(mapListPath)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        DefaultJapaneseFieldCharacters fieldCharacters = new();
        FF8Encoding europeanEncoding = FF8Encoding.CreateEuropean();
        FF8Encoding japaneseBaseEncoding = FF8Encoding.CreateJapanese();
        Int32 processedCount = 0;
        List<MsdCorpusFailure> failures = [];

        foreach (MsdCorpusFile corpusFile in EnumerateCorpus(rootPath, supportedFields))
        {
            processedCount++;

            try
            {
                FieldCharacterSet? japaneseFieldCharacters = corpusFile.IsJapanese
                    ? fieldCharacters.TryGet(corpusFile.FieldName)
                    : null;
                FF8Encoding encoding = corpusFile.IsJapanese
                    ? japaneseFieldCharacters is null
                        ? FF8Encoding.CreateJapanese()
                        : FF8Encoding.CreateJapanese(fieldCharacters, corpusFile.FieldName)
                    : europeanEncoding;
                Byte[] content = File.ReadAllBytes(corpusFile.Path);
                MsdFile oldFile = MsdFile.Read(content, encoding);
                MsdFileTests.AssertEveryByteWasRead(content, oldFile, encoding);

                Byte[] newFile = oldFile.Write();
                AssertMatchesSourceExceptCanonicalFieldCharacters(
                    content,
                    newFile,
                    japaneseBaseEncoding,
                    japaneseFieldCharacters);

                MsdFile rewrittenFile = MsdFile.Read(newFile, encoding);
                Assert.Equal(oldFile.Texts, rewrittenFile.Texts);
                Assert.Equal(newFile, rewrittenFile.Write());
            }
            catch (Exception exception)
            {
                failures.Add(new MsdCorpusFailure(corpusFile.Path, exception));
            }
        }

        if (failures.Count > 0)
        {
            String failedFiles = String.Join(
                Environment.NewLine,
                failures.Select((failure, index) =>
                    $"{index + 1}. {failure.Path} ({GetErrorSummary(failure.Exception)})"));

            Assert.Fail(
                $"{failures.Count} of {processedCount} MSD files failed validation:{Environment.NewLine}{failedFiles}");
        }

        Assert.Equal(ExpectedFileCount, processedCount);
    }

    private static void AssertMatchesSourceExceptCanonicalFieldCharacters(
        Byte[] source,
        Byte[] written,
        FF8Encoding baseEncoding,
        FieldCharacterSet? fieldCharacters)
    {
        if (fieldCharacters is null || source.Length == 0)
        {
            Assert.Equal(source, written);
            return;
        }

        Byte[] expected = NormalizeCanonicalFieldCharacters(source, baseEncoding, fieldCharacters);
        Assert.Equal(expected, written);
    }

    private static Byte[] NormalizeCanonicalFieldCharacters(
        Byte[] content,
        FF8Encoding baseEncoding,
        FieldCharacterSet fieldCharacters)
    {
        Int32 headerSize = BinaryPrimitives.ReadInt32LittleEndian(content);
        Int32 textCount = headerSize / sizeof(Int32);
        Byte[] encodedPlaceholder = baseEncoding.GetBytes(fieldCharacters.PlaceholderCharacter.ToString());
        Byte[][] normalizedTexts = new Byte[textCount][];
        Boolean changed = false;

        for (Int32 index = 0; index < textCount; index++)
        {
            Int32 start = ReadOffset(content, index);
            Int32 end = index + 1 < textCount ? ReadOffset(content, index + 1) : content.Length;
            normalizedTexts[index] = NormalizeText(
                content.AsSpan(start, end - start),
                fieldCharacters,
                baseEncoding,
                encodedPlaceholder,
                out Boolean textChanged);
            changed |= textChanged;
        }

        return changed ? BuildMsd(normalizedTexts) : content;
    }

    private static Byte[] NormalizeText(
        ReadOnlySpan<Byte> source,
        FieldCharacterSet fieldCharacters,
        FF8Encoding baseEncoding,
        ReadOnlySpan<Byte> encodedPlaceholder,
        out Boolean changed)
    {
        List<Byte> result = new(source.Length);
        changed = false;

        for (Int32 position = 0; position < source.Length;)
        {
            Byte first = source[position];
            if (HasParameter(first) && position + 1 < source.Length)
            {
                result.Add(first);
                result.Add(source[position + 1]);
                position += 2;
                continue;
            }

            if (first is >= 0x19 and <= 0x1C && position + 1 < source.Length)
            {
                Byte second = source[position + 1];
                Byte[]? replacement = first == 0x1C && second >= 0x20
                    ? GetCanonicalFieldCharacterBytes(
                        second - 0x20,
                        fieldCharacters,
                        baseEncoding,
                        encodedPlaceholder)
                    : null;

                if (replacement is not null)
                {
                    result.AddRange(replacement);
                    changed = true;
                }
                else
                {
                    result.Add(first);
                    result.Add(second);
                }

                position += 2;
                continue;
            }

            result.Add(first);
            position++;
        }

        return result.ToArray();
    }

    private static Byte[]? GetCanonicalFieldCharacterBytes(
        Int32 fieldIndex,
        FieldCharacterSet fieldCharacters,
        FF8Encoding baseEncoding,
        ReadOnlySpan<Byte> encodedPlaceholder)
    {
        if (fieldIndex >= fieldCharacters.Count)
        {
            return encodedPlaceholder.ToArray();
        }

        String value = fieldCharacters.Characters[fieldIndex].ToString();
        try
        {
            Byte[] encoded = baseEncoding.GetBytes(value);
            return baseEncoding.GetString(encoded) == value ? encoded : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static Boolean HasParameter(Byte value)
    {
        return value switch
        {
            (Byte)TextTagCode.Char => true,
            (Byte)TextTagCode.Var => true,
            (Byte)TextTagCode.Key => true,
            (Byte)TextTagCode.Color => true,
            (Byte)TextTagCode.Pause => true,
            (Byte)TextTagCode.Dialog => true,
            (Byte)TextTagCode.Option => true,
            (Byte)TextTagCode.Name => true,
            (Byte)TextTagCode.Term => true,
            _ => false,
        };
    }

    private static Int32 ReadOffset(Byte[] content, Int32 index)
    {
        return BinaryPrimitives.ReadInt32LittleEndian(
            content.AsSpan(index * sizeof(Int32), sizeof(Int32)));
    }

    private static Byte[] BuildMsd(IReadOnlyList<Byte[]> texts)
    {
        Int32 length = checked(texts.Count * sizeof(Int32) + texts.Sum(text => text.Length));
        Byte[] content = new Byte[length];
        Int32 position = texts.Count * sizeof(Int32);

        for (Int32 index = 0; index < texts.Count; index++)
        {
            BinaryPrimitives.WriteInt32LittleEndian(
                content.AsSpan(index * sizeof(Int32), sizeof(Int32)),
                position);
            texts[index].CopyTo(content, position);
            position += texts[index].Length;
        }

        return content;
    }

    private static IEnumerable<MsdCorpusFile> EnumerateCorpus(
        String rootPath,
        IReadOnlySet<String> supportedFields)
    {
        foreach (String path in Directory.EnumerateFiles(rootPath, "*.msd", SearchOption.AllDirectories))
        {
            String fileName = Path.GetFileNameWithoutExtension(path);
            if (fileName.Equals(CorruptedFileName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Int32 separator = fileName.LastIndexOf('_');
            if (separator <= 0)
            {
                continue;
            }

            String fieldName = fileName[..separator];
            if (fieldName.StartsWith("test", StringComparison.OrdinalIgnoreCase)
                || !supportedFields.Contains(fieldName))
            {
                continue;
            }

            String language = fileName[(separator + 1)..];
            yield return new MsdCorpusFile(
                path,
                fieldName,
                language.Equals("jp", StringComparison.OrdinalIgnoreCase));
        }
    }

    private static String GetErrorSummary(Exception exception)
    {
        Int32 lineEnd = exception.Message.IndexOfAny(['\r', '\n']);
        String message = lineEnd >= 0 ? exception.Message[..lineEnd] : exception.Message;
        return $"{exception.GetType().Name}: {message}";
    }

    private sealed record MsdCorpusFile(String Path, String FieldName, Boolean IsJapanese);

    private sealed record MsdCorpusFailure(String Path, Exception Exception);
}
