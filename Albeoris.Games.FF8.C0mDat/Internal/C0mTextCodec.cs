using System.Text;
using Albeoris.Games.FF8.C0mDat.Abstractions;

namespace Albeoris.Games.FF8.C0mDat.Internal;

/// <summary>Preserves untouched text bytes and encodes edited values safely.</summary>
internal static class C0mTextCodec
{
    public static C0mText Read(ReadOnlySpan<Byte> bytes, Encoding encoding)
    {
        return new C0mText(encoding.GetString(bytes), bytes.ToArray());
    }

    public static Byte[] Write(C0mText text, Encoding encoding, String description)
    {
        ArgumentNullException.ThrowIfNull(text);

        Byte[] bytes = text.EncodedValue is ReadOnlyMemory<Byte> encoded
            ? encoded.ToArray()
            : encoding.GetBytes(text.Value);

        if (bytes.Contains((Byte)0))
        {
            throw new InvalidOperationException($"The {description} contains a null byte after encoding.");
        }

        return bytes;
    }
}
