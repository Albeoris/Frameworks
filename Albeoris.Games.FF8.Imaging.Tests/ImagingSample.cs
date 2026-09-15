using System.Reflection;

namespace Albeoris.Games.FF8.Imaging.Tests;

/// <summary>A real FF8 imaging file embedded for deterministic tests.</summary>
public sealed class ImagingSample
{
    private const String ResourcePrefix = "Albeoris.Games.FF8.Imaging.Tests.Resources.";
    private readonly Lazy<Byte[]> _content;

    private ImagingSample(String name, String fileName)
    {
        Name = name;
        FileName = fileName;
        _content = new Lazy<Byte[]>(() => Load(fileName));
    }

    public static ImagingSample MultiImageTim { get; } = new("Multi-image magic texture", "mag277.tim");
    public static ImagingSample JapaneseSystemFontTdw { get; } = new("Japanese system font", "sysfnt.tdw");
    public static ImagingSample MultiPaletteTex { get; } = new("Multi-palette menu font", "font8_odd.tex");
    public static ImagingSample ComplexFieldMim { get; } = new("BVTR_2 field atlas", "bvtr_2.mim");
    public static ImagingSample ComplexFieldMap { get; } = new("BVTR_2 field layout", "bvtr_2.map");

    public String Name { get; }
    public String FileName { get; }
    public Byte[] Content => _content.Value;

    public override String ToString() => Name;

    private static Byte[] Load(String fileName)
    {
        Assembly assembly = typeof(ImagingSample).Assembly;
        using Stream stream = assembly.GetManifestResourceStream(ResourcePrefix + fileName) ?? throw new InvalidOperationException($"Embedded imaging resource {fileName} was not found.");
        using MemoryStream buffer = new();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}
