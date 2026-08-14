using System.Text;
using Albeoris.Games.FF8.C0mDat.Abstractions;

namespace Albeoris.Games.FF8.C0mDat.Internal;

/// <summary>Serializes every section and rebuilds the outer section directory.</summary>
internal static class C0mFileWriter
{
    public static Byte[] Write(C0mFile file)
    {
        IReadOnlyList<IC0mSection> sections = ValidateAndOrderSections(file.Sections);
        Encoding encoding = file.Encoding;

        Byte[][] bodies = new Byte[sections.Count][];
        Int64 fileSize = C0mFormat.FileHeaderSize;
        for (Int32 index = 0; index < sections.Count; index++)
        {
            bodies[index] = WriteSection(sections[index], encoding);
            fileSize = checked(fileSize + bodies[index].Length);
        }

        C0mByteWriter writer = new();
        writer.WriteUInt32((UInt32)C0mFormat.SectionCount);

        Int64 position = C0mFormat.FileHeaderSize;
        foreach (Byte[] body in bodies)
        {
            writer.WriteUInt32(C0mFormat.ToUInt32(position, "section position"));
            position += body.Length;
        }

        writer.WriteUInt32(C0mFormat.ToUInt32(fileSize, "file size"));
        foreach (Byte[] body in bodies)
        {
            writer.WriteBytes(body);
        }

        return writer.ToArray();
    }

    private static IReadOnlyList<IC0mSection> ValidateAndOrderSections(IReadOnlyList<IC0mSection> sections)
    {
        if (sections.Count != C0mFormat.SectionCount)
        {
            throw new InvalidOperationException(
                $"A c0m file requires {C0mFormat.SectionCount} sections, but the model contains {sections.Count}.");
        }

        IC0mSection[] ordered = sections.OrderBy(section => section.Index).ToArray();
        for (Int32 index = 1; index <= C0mFormat.SectionCount; index++)
        {
            if (ordered[index - 1].Index != index)
            {
                throw new InvalidOperationException($"The model does not contain exactly one section at index {index}.");
            }
        }

        return ordered;
    }

    private static Byte[] WriteSection(IC0mSection section, Encoding encoding)
    {
        return section switch
        {
            C0mOpaqueSection opaque => opaque.Content,
            C0mInformationSection information => C0mInformationCodec.Write(information, encoding),
            C0mBattleScriptSection battleScript => C0mBattleScriptCodec.Write(battleScript, encoding),
            _ => throw new NotSupportedException(
                $"Section {section.Index}: unsupported model type '{section.GetType().Name}'.")
        };
    }
}
