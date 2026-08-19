using System.Buffers.Binary;
using Albeoris.Games.FF8.TextEncoding;
using Xunit;

namespace Albeoris.Games.FF8.Msd.Tests;

public sealed class MsdFileTests
{
    [Fact]
    public void ReadWrite_ReproducesEveryByteAndConsumesEveryTextSpan()
    {
        FF8Encoding encoding = FF8Encoding.CreateEuropean();
        MsdFile source = new(encoding);
        source.Texts.Add("First{End}");
        source.Texts.Add(String.Empty);
        source.Texts.Add("Second{End}{End}");
        Byte[] content = source.Write();

        MsdFile parsed = MsdFile.Read(content, encoding);
        Byte[] written = parsed.Write();

        Assert.Equal(content, written);
        AssertEveryByteWasRead(content, parsed, encoding);
    }

    [Fact]
    public void ReadWrite_SupportsAnEmptyFile()
    {
        FF8Encoding encoding = FF8Encoding.CreateEuropean();

        MsdFile parsed = MsdFile.Read([], encoding);

        Assert.Empty(parsed.Texts);
        Assert.Empty(parsed.Write());
    }

    [Fact]
    public void Read_SupportsRepeatedOffsetsAndTextsWithoutEndTags()
    {
        FF8Encoding encoding = FF8Encoding.CreateEuropean();
        Byte[] finalText = encoding.GetBytes("Text without an end tag");
        Byte[] content = new Byte[3 * sizeof(Int32) + finalText.Length];
        BinaryPrimitives.WriteInt32LittleEndian(content, 3 * sizeof(Int32));
        BinaryPrimitives.WriteInt32LittleEndian(content.AsSpan(sizeof(Int32)), 3 * sizeof(Int32));
        BinaryPrimitives.WriteInt32LittleEndian(content.AsSpan(2 * sizeof(Int32)), 3 * sizeof(Int32));
        finalText.CopyTo(content, 3 * sizeof(Int32));

        MsdFile parsed = MsdFile.Read(content, encoding);

        Assert.Equal([String.Empty, String.Empty, "Text without an end tag"], parsed.Texts);
        Assert.Equal(content, parsed.Write());
        AssertEveryByteWasRead(content, parsed, encoding);
    }

    [Fact]
    public void Write_RecalculatesFollowingOffsetsAfterAnEdit()
    {
        FF8Encoding encoding = FF8Encoding.CreateEuropean();
        MsdFile file = new(encoding);
        file.Texts.Add("A");
        file.Texts.Add("B");
        Byte[] original = file.Write();
        Int32 originalSecondOffset = BinaryPrimitives.ReadInt32LittleEndian(original.AsSpan(sizeof(Int32)));

        file.Texts[0] = "A longer first text";
        Byte[] edited = file.Write();
        MsdFile reparsed = MsdFile.Read(edited, encoding);

        Int32 editedSecondOffset = BinaryPrimitives.ReadInt32LittleEndian(edited.AsSpan(sizeof(Int32)));
        Assert.True(editedSecondOffset > originalSecondOffset);
        Assert.Equal(file.Texts, reparsed.Texts);
    }

    [Theory]
    [InlineData(new Byte[] { 1, 0, 0, 0 })]
    [InlineData(new Byte[] { 8, 0, 0, 0 })]
    [InlineData(new Byte[] { 8, 0, 0, 0, 3, 0, 0, 0 })]
    public void Read_RejectsMalformedOffsetTables(Byte[] content)
    {
        Assert.Throws<InvalidDataException>(
            () => MsdFile.Read(content, FF8Encoding.CreateEuropean()));
    }

    internal static void AssertEveryByteWasRead(Byte[] content, MsdFile file, FF8Encoding encoding)
    {
        if (content.Length == 0)
        {
            Assert.Empty(file.Texts);
            return;
        }

        Int32 headerSize = checked(file.Texts.Count * sizeof(Int32));
        Assert.Equal(headerSize, BinaryPrimitives.ReadInt32LittleEndian(content));

        Int32 coveredUntil = headerSize;
        for (Int32 index = 0; index < file.Texts.Count; index++)
        {
            Int32 start = BinaryPrimitives.ReadInt32LittleEndian(
                content.AsSpan(index * sizeof(Int32), sizeof(Int32)));
            Int32 end = index + 1 < file.Texts.Count
                ? BinaryPrimitives.ReadInt32LittleEndian(
                    content.AsSpan((index + 1) * sizeof(Int32), sizeof(Int32)))
                : content.Length;

            Assert.Equal(coveredUntil, start);
            Assert.Equal(encoding.GetString(content.AsSpan(start, end - start)), file.Texts[index]);
            coveredUntil = end;
        }

        Assert.Equal(content.Length, coveredUntil);
    }
}
