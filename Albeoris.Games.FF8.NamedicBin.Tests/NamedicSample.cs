using System.IO.Compression;
using System.Text;
using Albeoris.Games.Core.NsResources;
using Albeoris.Games.FF8.TextEncoding;
using Xunit;

namespace Albeoris.Games.FF8.NamedicBin.Tests;

/// <summary>A shipped <c>namedic.bin</c> file and the encoding used by its localization.</summary>
public sealed class NamedicSample
{
    private const String ArchiveResourcePath = "/Resources/namedic.samples.zip";

    private static readonly Lazy<IReadOnlyDictionary<String, Byte[]>> Files = new(LoadArchive);

    private NamedicSample(String name, String fileName, Func<Encoding> createEncoding)
    {
        Name = name;
        FileName = fileName;
        CreateEncoding = createEncoding;
    }

    public static NamedicSample European { get; } = new("European", "namedic_en.bin", FF8Encoding.CreateEuropean);

    public static NamedicSample Japanese { get; } = new("Japanese", "namedic_jp.bin", FF8Encoding.CreateJapanese);

    public static TheoryData<NamedicSample> All => [European, Japanese];

    public String Name { get; }

    public String FileName { get; }

    public Byte[] Content => Files.Value[FileName];

    private Func<Encoding> CreateEncoding { get; }

    public Encoding NewEncoding() => CreateEncoding();

    public override String ToString() => Name;

    private static IReadOnlyDictionary<String, Byte[]> LoadArchive()
    {
        using Stream stream = EmbeddedResources.Open(typeof(NamedicSample).Assembly, ArchiveResourcePath);
        using ZipArchive archive = new(stream, ZipArchiveMode.Read);

        Dictionary<String, Byte[]> files = [];
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            using Stream entryStream = entry.Open();
            using MemoryStream buffer = new();
            entryStream.CopyTo(buffer);
            files[entry.Name] = buffer.ToArray();
        }

        return files;
    }
}
