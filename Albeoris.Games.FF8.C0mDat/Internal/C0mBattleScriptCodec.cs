using System.Buffers.Binary;
using System.Text;
using Albeoris.Games.FF8.C0mDat.Abstractions;

namespace Albeoris.Games.FF8.C0mDat.Internal;

/// <summary>
/// Resolves the nested AI and text offsets in section 8 into logical scripts and strings, and
/// rebuilds every offset when those values are serialized.
/// </summary>
internal static class C0mBattleScriptCodec
{
    public static C0mBattleScriptSection Read(ReadOnlySpan<Byte> body, Encoding encoding)
    {
        if (body.Length < C0mFormat.BattleHeaderSize)
        {
            throw new InvalidDataException(
                $"The battle-script section is {body.Length} bytes long; its header requires {C0mFormat.BattleHeaderSize} bytes.");
        }

        UInt32 subsectionCount = ReadUInt32(body, 0);
        if (subsectionCount != C0mFormat.BattleSubsectionCount)
        {
            throw new InvalidDataException(
                $"The battle-script section declares {subsectionCount} subsections instead of {C0mFormat.BattleSubsectionCount}.");
        }

        Int32 aiStart = ReadPosition(body, sizeof(UInt32), "AI subsection");
        Int32 textOffsetsStart = ReadPosition(body, 2 * sizeof(UInt32), "text-offset subsection");
        Int32 textStart = ReadPosition(body, 3 * sizeof(UInt32), "text subsection");

        if (aiStart != C0mFormat.BattleHeaderSize)
        {
            throw new InvalidDataException(
                $"The AI subsection starts at {aiStart}; the native layout requires {C0mFormat.BattleHeaderSize}.");
        }

        if (textOffsetsStart < aiStart + C0mFormat.AiOffsetTableSize || textStart < textOffsetsStart || textStart > body.Length)
        {
            throw new InvalidDataException(
                $"The battle subsections are out of order or out of bounds: AI {aiStart}, text offsets {textOffsetsStart}, text {textStart}, length {body.Length}.");
        }

        C0mBattleScriptSection section = new();
        ReadAiScripts(body[aiStart..textOffsetsStart], section.AiScripts);
        ReadTexts(body[textOffsetsStart..textStart], body[textStart..], encoding, section.Texts);
        return section;
    }

    public static Byte[] Write(C0mBattleScriptSection section, Encoding encoding)
    {
        IReadOnlyList<Byte[]> scripts = section.AiScripts.InFileOrder;
        Int32 aiLength = C0mFormat.AiOffsetTableSize;
        for (Int32 index = 0; index < scripts.Count; index++)
        {
            Byte[] script = scripts[index];
            ValidateAiScript(script, index);
            aiLength = checked(aiLength + script.Length);
        }

        Byte[][] encodedTexts = new Byte[section.Texts.Count][];
        Int32 textDataLength = 0;
        for (Int32 index = 0; index < section.Texts.Count; index++)
        {
            encodedTexts[index] = C0mTextCodec.Write(section.Texts[index], encoding, $"battle text {index}");
            textDataLength = checked(textDataLength + encodedTexts[index].Length + 1);
        }

        Int32 textOffsetsStart = checked(C0mFormat.BattleHeaderSize + aiLength);
        Int32 textOffsetTableLength = C0mFormat.AlignToFour(checked(encodedTexts.Length * sizeof(UInt16)));
        Int32 textStart = checked(textOffsetsStart + textOffsetTableLength);

        C0mByteWriter writer = new();
        writer.WriteUInt32(C0mFormat.BattleSubsectionCount);
        writer.WriteUInt32((UInt32)C0mFormat.BattleHeaderSize);
        writer.WriteUInt32(C0mFormat.ToUInt32(textOffsetsStart, "text-offset subsection position"));
        writer.WriteUInt32(C0mFormat.ToUInt32(textStart, "text subsection position"));

        Int32 scriptPosition = C0mFormat.AiOffsetTableSize;
        foreach (Byte[] script in scripts)
        {
            writer.WriteUInt32(C0mFormat.ToUInt32(scriptPosition, "AI script position"));
            scriptPosition = checked(scriptPosition + script.Length);
        }

        foreach (Byte[] script in scripts)
        {
            writer.WriteBytes(script);
        }

        Int32 textPosition = 0;
        foreach (Byte[] text in encodedTexts)
        {
            writer.WriteUInt16(C0mFormat.ToUInt16(textPosition, "battle text position"));
            textPosition = checked(textPosition + text.Length + 1);
        }

        writer.PadTo(textStart);
        foreach (Byte[] text in encodedTexts)
        {
            writer.WriteBytes(text);
            writer.WriteZeros(1);
        }

        writer.PadTo(checked(textStart + C0mFormat.AlignToFour(textDataLength)));
        return writer.ToArray();
    }

    private static void ReadAiScripts(ReadOnlySpan<Byte> aiBody, C0mAiScripts scripts)
    {
        if (aiBody.Length < C0mFormat.AiOffsetTableSize)
        {
            throw new InvalidDataException(
                $"The AI subsection is {aiBody.Length} bytes long; its offset table requires {C0mFormat.AiOffsetTableSize} bytes.");
        }

        Int32[] starts = new Int32[C0mFormat.AiScriptCount];
        for (Int32 index = 0; index < starts.Length; index++)
        {
            starts[index] = ReadPosition(aiBody, index * sizeof(UInt32), $"AI script {index}");
            Int32 minimum = index == 0 ? C0mFormat.AiOffsetTableSize : starts[index - 1];
            if (starts[index] < minimum || starts[index] > aiBody.Length)
            {
                throw new InvalidDataException(
                    $"AI script {index} starts at {starts[index]}, outside its ordered range {minimum}..{aiBody.Length}.");
            }
        }

        if (starts[0] != C0mFormat.AiOffsetTableSize)
        {
            throw new InvalidDataException(
                $"The first AI script starts at {starts[0]}; the native layout requires {C0mFormat.AiOffsetTableSize}.");
        }

        Byte[][] values = new Byte[starts.Length][];
        for (Int32 index = 0; index < starts.Length; index++)
        {
            Int32 end = index + 1 < starts.Length ? starts[index + 1] : aiBody.Length;
            values[index] = aiBody[starts[index]..end].ToArray();
        }

        scripts.Initialization = values[0];
        scripts.EnemyTurn = values[1];
        scripts.CounterAttack = values[2];
        scripts.Death = values[3];
        scripts.BeforeDyingOrHit = values[4];
    }

    private static void ValidateAiScript(Byte[] script, Int32 index)
    {
        if (script.Length == 0 || script.Length % 4 != 0 || script[^1] != 0)
        {
            throw new InvalidOperationException(
                $"AI script {index} must be non-empty, end with a stop opcode and occupy a multiple of four bytes.");
        }
    }

    private static void ReadTexts(
        ReadOnlySpan<Byte> offsetTable,
        ReadOnlySpan<Byte> textData,
        Encoding encoding,
        List<C0mText> texts)
    {
        if (offsetTable.Length % 4 != 0)
        {
            throw new InvalidDataException($"The {offsetTable.Length}-byte battle text-offset table is not four-byte aligned.");
        }

        List<Int32> starts = [];
        for (Int32 position = 0; position < offsetTable.Length; position += sizeof(UInt16))
        {
            UInt16 start = BinaryPrimitives.ReadUInt16LittleEndian(offsetTable[position..]);
            if (position == 0)
            {
                if (start != 0)
                {
                    throw new InvalidDataException($"The first battle text starts at {start} instead of zero.");
                }

                starts.Add(0);
                continue;
            }

            if (start == 0)
            {
                if (offsetTable[position..].ContainsAnyExcept((Byte)0))
                {
                    throw new InvalidDataException("The battle text-offset padding contains non-zero bytes.");
                }

                break;
            }

            if (start <= starts[^1] || start >= textData.Length)
            {
                throw new InvalidDataException(
                    $"Battle text {starts.Count} starts at {start}, outside its ordered range {starts[^1] + 1}..{textData.Length - 1}.");
            }

            starts.Add(start);
        }

        if (starts.Count == 0)
        {
            if (!textData.IsEmpty)
            {
                throw new InvalidDataException("The battle text data is present without a text-offset table.");
            }

            return;
        }

        Int32 contentEnd = 0;
        for (Int32 index = 0; index < starts.Count; index++)
        {
            Int32 start = starts[index];
            Int32 terminatorDistance = textData[start..].IndexOf((Byte)0);
            if (terminatorDistance < 0)
            {
                throw new InvalidDataException($"Battle text {index} is not null-terminated.");
            }

            Int32 end = start + terminatorDistance;
            if (index + 1 < starts.Count && starts[index + 1] != end + 1)
            {
                throw new InvalidDataException(
                    $"Battle text {index + 1} starts at {starts[index + 1]}, but contiguous text data requires {end + 1}.");
            }

            texts.Add(C0mTextCodec.Read(textData[start..end], encoding));
            contentEnd = end + 1;
        }

        ReadOnlySpan<Byte> padding = textData[contentEnd..];
        if (padding.Length > 3 || padding.ContainsAnyExcept((Byte)0))
        {
            throw new InvalidDataException(
                $"The battle text data ends with {padding.Length} padding bytes; expected at most three zero bytes.");
        }
    }

    private static Int32 ReadPosition(ReadOnlySpan<Byte> body, Int32 position, String description)
    {
        UInt32 value = ReadUInt32(body, position);
        if (value > Int32.MaxValue)
        {
            throw new InvalidDataException($"The {description} position {value} is too large.");
        }

        return (Int32)value;
    }

    private static UInt32 ReadUInt32(ReadOnlySpan<Byte> body, Int32 position)
    {
        return BinaryPrimitives.ReadUInt32LittleEndian(body.Slice(position, sizeof(UInt32)));
    }
}
