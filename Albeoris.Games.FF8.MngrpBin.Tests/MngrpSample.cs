using System.IO.Compression;
using System.Text;
using Albeoris.Games.Core.NsResources;
using Albeoris.Games.FF8.TextEncoding;
using Xunit;

namespace Albeoris.Games.FF8.MngrpBin.Tests;

/// <summary>
/// One shipped <c>mngrp.bin</c>/<c>mngrphd.bin</c> pair from the embedded sample archive, together
/// with the encoding its localization uses.
/// </summary>
public sealed class MngrpSample
{
    private const String ArchiveResourcePath = "/Resources/mngrp.samples.zip";

    private static readonly Lazy<IReadOnlyDictionary<String, Byte[]>> Files = new(LoadArchive);

    private MngrpSample(String name, String contentFileName, String headerFileName, Func<Encoding> createEncoding)
    {
        Name = name;
        ContentFileName = contentFileName;
        HeaderFileName = headerFileName;
        CreateEncoding = createEncoding;
    }

    /// <summary>The European sample, whose strings use the single-page European codepage.</summary>
    public static MngrpSample European { get; } = new("European", "mngrp_en.bin", "mngrphd_en.bin", FF8Encoding.CreateEuropean);

    /// <summary>The Japanese sample, whose strings use the multi-page Japanese codepage.</summary>
    public static MngrpSample Japanese { get; } = new("Japanese", "mngrp_jp.bin", "mngrphd_jp.bin", FF8Encoding.CreateJapanese);

    /// <summary>Every sample, for use as xUnit theory data.</summary>
    public static TheoryData<MngrpSample> All => [European, Japanese];

    public String Name { get; }

    public String ContentFileName { get; }

    public String HeaderFileName { get; }

    private Func<Encoding> CreateEncoding { get; }

    /// <summary>The bytes of the sample's <c>mngrp.bin</c>.</summary>
    public Byte[] Content => Files.Value[ContentFileName];

    /// <summary>The bytes of the sample's <c>mngrphd.bin</c>.</summary>
    public Byte[] Header => Files.Value[HeaderFileName];

    /// <summary>Creates the encoding this sample's strings are stored with.</summary>
    public Encoding NewEncoding() => CreateEncoding();

    /// <summary>Parses the sample into a fresh archive.</summary>
    public MngrpArchive Read() => MngrpArchive.Read(Content, Header, NewEncoding());

    public override String ToString() => Name;

    private static IReadOnlyDictionary<String, Byte[]> LoadArchive()
    {
        using Stream stream = EmbeddedResources.Open(typeof(MngrpSample).Assembly, ArchiveResourcePath);
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
