using System.Text;
using Albeoris.Games.FF8.C0mDat.Abstractions;
using Albeoris.Games.FF8.C0mDat.Internal;

namespace Albeoris.Games.FF8.C0mDat;

/// <summary>
/// A parsed Final Fantasy VIII <c>c0m*.dat</c> enemy file. Native offsets are resolved while
/// reading and recalculated from the section models whenever the file is written.
/// </summary>
public sealed class C0mFile
{
    private readonly List<IC0mSection> _sections;

    internal C0mFile(List<IC0mSection> sections, Encoding encoding)
    {
        _sections = sections;
        Encoding = encoding;
    }

    /// <summary>The localization-specific FF8 encoding used by the file.</summary>
    public Encoding Encoding { get; }

    /// <summary>The eleven data sections in their fixed native order.</summary>
    public IReadOnlyList<IC0mSection> Sections => _sections;

    /// <summary>The parsed enemy information and statistics section.</summary>
    public C0mInformationSection Information => GetSection<C0mInformationSection>(C0mSectionKind.Information);

    /// <summary>The parsed AI and battle-text section.</summary>
    public C0mBattleScriptSection BattleScript => GetSection<C0mBattleScriptSection>(C0mSectionKind.BattleScript);

    /// <summary>Parses a complete <c>c0m*.dat</c> file.</summary>
    public static C0mFile Read(ReadOnlySpan<Byte> content, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(encoding);
        return C0mFileReader.Read(content, encoding);
    }

    /// <summary>Serializes the file, recalculating every outer and battle-script offset.</summary>
    public Byte[] Write() => C0mFileWriter.Write(this);

    /// <summary>Returns the section at <paramref name="kind"/> as <typeparamref name="TSection"/>.</summary>
    public TSection GetSection<TSection>(C0mSectionKind kind) where TSection : class, IC0mSection
    {
        IC0mSection section = _sections.FirstOrDefault(candidate => candidate.Kind == kind)
            ?? throw new KeyNotFoundException($"The file has no {kind} section.");
        return section as TSection
            ?? throw new InvalidCastException($"The {kind} section is a {section.GetType().Name}, not a {typeof(TSection).Name}.");
    }
}
