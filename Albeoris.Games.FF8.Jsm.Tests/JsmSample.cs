using System.Reflection;
using Xunit;

namespace Albeoris.Games.FF8.Jsm.Tests;

/// <summary>A small shipped JSM file embedded for deterministic unit tests.</summary>
public sealed class JsmSample
{
    private const String ResourcePrefix = "Albeoris.Games.FF8.Jsm.Tests.Resources.";

    private readonly Lazy<Byte[]> _content;

    private JsmSample(String name, String fileName)
    {
        Name = name;
        FileName = fileName;
        _content = new Lazy<Byte[]>(() => Load(fileName));
    }

    public static JsmSample MessageDialogs { get; } = new("Message dialogs", "test1.jsm");

    public static JsmSample FieldMessage { get; } = new("Field message", "test2.jsm");

    public static IReadOnlyList<JsmSample> Values { get; } = [MessageDialogs, FieldMessage];

    public static TheoryData<JsmSample> All => [MessageDialogs, FieldMessage];

    public String Name { get; }

    public String FileName { get; }

    public Byte[] Content => _content.Value;

    public JsmDocument Read() => Jsm.File.ReadDocument(Content);

    public override String ToString() => Name;

    private static Byte[] Load(String fileName)
    {
        Assembly assembly = typeof(JsmSample).Assembly;
        using Stream stream = assembly.GetManifestResourceStream(ResourcePrefix + fileName)
            ?? throw new InvalidOperationException($"Embedded JSM resource {fileName} was not found.");
        using MemoryStream buffer = new();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
