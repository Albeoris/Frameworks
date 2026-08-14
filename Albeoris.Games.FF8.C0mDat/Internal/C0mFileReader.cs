using System.Buffers.Binary;
using System.Text;
using Albeoris.Games.FF8.C0mDat.Abstractions;

namespace Albeoris.Games.FF8.C0mDat.Internal;

/// <summary>Parses the outer section directory and dispatches known section layouts.</summary>
internal static class C0mFileReader
{
    public static C0mFile Read(ReadOnlySpan<Byte> content, Encoding encoding)
    {
        if (content.Length < sizeof(UInt32))
        {
            throw new InvalidDataException("The c0m file is too short to contain a section count.");
        }

        UInt32 sectionCount = BinaryPrimitives.ReadUInt32LittleEndian(content);
        if (sectionCount != C0mFormat.SectionCount)
        {
            throw new InvalidDataException(
                $"The c0m file declares {sectionCount} sections instead of {C0mFormat.SectionCount}.");
        }

        if (content.Length < C0mFormat.FileHeaderSize)
        {
            throw new InvalidDataException(
                $"The c0m file is {content.Length} bytes long; its section header requires {C0mFormat.FileHeaderSize} bytes.");
        }

        Int32[] starts = new Int32[C0mFormat.SectionCount];
        Int32 previous = C0mFormat.FileHeaderSize;
        for (Int32 index = 0; index < starts.Length; index++)
        {
            UInt32 storedStart = BinaryPrimitives.ReadUInt32LittleEndian(
                content.Slice(sizeof(UInt32) + index * sizeof(UInt32), sizeof(UInt32)));
            if (storedStart > Int32.MaxValue)
            {
                throw new InvalidDataException($"Section {index + 1} starts at the unsupported position {storedStart}.");
            }

            starts[index] = (Int32)storedStart;
            if (starts[index] < previous || starts[index] > content.Length)
            {
                throw new InvalidDataException(
                    $"Section {index + 1} starts at {starts[index]}, outside its ordered range {previous}..{content.Length}.");
            }

            previous = starts[index];
        }

        if (starts[0] != C0mFormat.FileHeaderSize)
        {
            throw new InvalidDataException(
                $"Section 1 starts at {starts[0]}; the section header ends at {C0mFormat.FileHeaderSize}.");
        }

        UInt32 storedFileSize = BinaryPrimitives.ReadUInt32LittleEndian(
            content.Slice(sizeof(UInt32) + C0mFormat.SectionCount * sizeof(UInt32), sizeof(UInt32)));
        if (storedFileSize != content.Length)
        {
            throw new InvalidDataException(
                $"The c0m header declares a {storedFileSize}-byte file, but the supplied content is {content.Length} bytes long.");
        }

        List<IC0mSection> sections = new(C0mFormat.SectionCount);
        for (Int32 index = 1; index <= C0mFormat.SectionCount; index++)
        {
            Int32 start = starts[index - 1];
            Int32 end = index < C0mFormat.SectionCount ? starts[index] : content.Length;
            ReadOnlySpan<Byte> body = content[start..end];
            C0mSectionKind kind = C0mFormat.GetSectionKind(index);

            try
            {
                sections.Add(ReadSection(kind, body, encoding));
            }
            catch (InvalidDataException exception)
            {
                throw new InvalidDataException($"Section {index} ({kind}): {exception.Message}", exception);
            }
        }

        return new C0mFile(sections, encoding);
    }

    private static IC0mSection ReadSection(C0mSectionKind kind, ReadOnlySpan<Byte> body, Encoding encoding)
    {
        return kind switch
        {
            C0mSectionKind.Information => C0mInformationCodec.Read(body, encoding),
            C0mSectionKind.BattleScript => C0mBattleScriptCodec.Read(body, encoding),
            _ => new C0mOpaqueSection(kind, body.ToArray())
        };
    }
}
