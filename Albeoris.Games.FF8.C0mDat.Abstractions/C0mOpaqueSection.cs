namespace Albeoris.Games.FF8.C0mDat.Abstractions;

/// <summary>
/// A section whose internal layout is not interpreted. Its content is preserved byte-for-byte.
/// </summary>
public sealed class C0mOpaqueSection : C0mSection
{
    private Byte[] _content;

    public C0mOpaqueSection(C0mSectionKind kind, Byte[] content)
        : base(kind)
    {
        if (kind is C0mSectionKind.Information or C0mSectionKind.BattleScript)
        {
            throw new ArgumentException($"Section {kind} has a parsed model and cannot be opaque.", nameof(kind));
        }

        ArgumentNullException.ThrowIfNull(content);
        _content = content;
    }

    /// <summary>The exact section bytes.</summary>
    public Byte[] Content
    {
        get => _content;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _content = value;
        }
    }
}
