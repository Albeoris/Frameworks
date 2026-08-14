using System.Text;
using Albeoris.Games.FF8.C0mDat.Abstractions;

namespace Albeoris.Games.FF8.C0mDat.Internal;

/// <summary>Reads and writes the fixed-width name at the start of section 7.</summary>
internal static class C0mInformationCodec
{
    public static C0mInformationSection Read(ReadOnlySpan<Byte> body, Encoding encoding)
    {
        if (body.Length != C0mFormat.InformationSectionSize)
        {
            throw new InvalidDataException(
                $"The information section is {body.Length} bytes long instead of {C0mFormat.InformationSectionSize}.");
        }

        ReadOnlySpan<Byte> nameField = body[..C0mFormat.InformationNameSize];
        Int32 terminator = nameField.IndexOf((Byte)0);
        if (terminator < 0)
        {
            throw new InvalidDataException("The monster name has no null terminator in its fixed-width field.");
        }

        if (nameField[terminator..].ContainsAnyExcept((Byte)0))
        {
            throw new InvalidDataException("The monster name field contains non-zero bytes after its null terminator.");
        }

        return new C0mInformationSection(
            C0mTextCodec.Read(nameField[..terminator], encoding),
            body[C0mFormat.InformationNameSize..].ToArray());
    }

    public static Byte[] Write(C0mInformationSection section, Encoding encoding)
    {
        if (section.StatData.Length != C0mFormat.InformationStatDataSize)
        {
            throw new InvalidOperationException(
                $"The information section requires {C0mFormat.InformationStatDataSize} stat bytes, but the model contains {section.StatData.Length}.");
        }

        Byte[] nameBytes = C0mTextCodec.Write(section.MonsterName, encoding, "monster name");
        if (nameBytes.Length >= C0mFormat.InformationNameSize)
        {
            throw new InvalidOperationException(
                $"The encoded monster name is {nameBytes.Length} bytes long and leaves no room for a null terminator in its {C0mFormat.InformationNameSize}-byte field.");
        }

        Byte[] body = new Byte[checked(C0mFormat.InformationNameSize + section.StatData.Length)];
        nameBytes.CopyTo(body, 0);
        section.StatData.CopyTo(body, C0mFormat.InformationNameSize);
        return body;
    }
}
