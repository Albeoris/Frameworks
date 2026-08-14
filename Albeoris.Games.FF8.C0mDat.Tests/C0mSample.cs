using System.IO.Compression;
using System.Text;
using Albeoris.Games.Core.NsResources;
using Albeoris.Games.FF8.TextEncoding;
using Xunit;

namespace Albeoris.Games.FF8.C0mDat.Tests;

/// <summary>A shipped enemy file and the FF8 encoding used by its localization.</summary>
public sealed class C0mSample
{
    private const String ArchiveResourcePath = "/Resources/c0m.samples.zip";

    private static readonly Lazy<IReadOnlyDictionary<String, Byte[]>> Files = new(LoadArchive);

    private C0mSample(String name, String fileName, Func<Encoding> createEncoding)
    {
        Name = name;
        FileName = fileName;
        CreateEncoding = createEncoding;
    }

    public static C0mSample European { get; } = new("European", "c0m019_en.dat", FF8Encoding.CreateEuropean);

    public static C0mSample Japanese { get; } = new("Japanese", "c0m019_jp.dat", FF8Encoding.CreateJapanese);

    public static TheoryData<C0mSample> All => [European, Japanese];

    public static TheoryData<C0mSample, Int32> AllBattleTexts
    {
        get
        {
            TheoryData<C0mSample, Int32> data = [];
            foreach (C0mSample sample in new[] { European, Japanese })
            {
                data.Add(sample, 0);
                data.Add(sample, 1);
            }

            return data;
        }
    }

    public String Name { get; }

    public String FileName { get; }

    public Byte[] Content => Files.Value[FileName];

    private Func<Encoding> CreateEncoding { get; }

    public Encoding NewEncoding() => CreateEncoding();

    public C0mFile Read() => C0mFile.Read(Content, NewEncoding());

    public override String ToString() => Name;

    private static IReadOnlyDictionary<String, Byte[]> LoadArchive()
    {
        using Stream stream = EmbeddedResources.Open(typeof(C0mSample).Assembly, ArchiveResourcePath);
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
