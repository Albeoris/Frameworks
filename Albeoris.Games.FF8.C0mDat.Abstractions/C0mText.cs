namespace Albeoris.Games.FF8.C0mDat.Abstractions;

/// <summary>A decoded string and, until it is edited, its exact original byte representation.</summary>
/// <remarks>
/// FF8 codepages are not necessarily one-to-one. Retaining the original bytes allows an
/// unedited file to round-trip exactly even when multiple byte sequences decode to the same
/// characters. Assigning <see cref="Value"/> marks the text as edited and discards those bytes.
/// </remarks>
public sealed class C0mText
{
    private String _value;

    public C0mText(String value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _value = value;
    }

    internal C0mText(String value, ReadOnlyMemory<Byte>? encodedValue)
        : this(value)
    {
        EncodedValue = encodedValue;
    }

    /// <summary>The decoded value. Assigning it discards the original encoded bytes.</summary>
    public String Value
    {
        get => _value;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _value = value;
            EncodedValue = null;
        }
    }

    /// <summary>The original encoded bytes, or <see langword="null"/> after an edit.</summary>
    public ReadOnlyMemory<Byte>? EncodedValue { get; private set; }

    public static implicit operator C0mText(String value) => new(value);

    public override String ToString() => _value;
}
