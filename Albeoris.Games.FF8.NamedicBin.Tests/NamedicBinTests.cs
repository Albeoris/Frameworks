using System.Buffers.Binary;
using Xunit;

namespace Albeoris.Games.FF8.NamedicBin.Tests;

public class NamedicBinTests
{
    [Theory]
    [MemberData(nameof(NamedicSample.All), MemberType = typeof(NamedicSample))]
    public void ReadStrings_ConsumesContiguousStringsAndReachesEndOfFile(NamedicSample sample)
    {
        Byte[] content = sample.Content;
        UInt16 count = BinaryPrimitives.ReadUInt16LittleEndian(content);
        Int32 position = sizeof(UInt16) + count * sizeof(UInt16);

        for (Int32 index = 0; index < count; index++)
        {
            Int32 offsetPosition = sizeof(UInt16) + index * sizeof(UInt16);
            UInt16 offset = BinaryPrimitives.ReadUInt16LittleEndian(content.AsSpan(offsetPosition, sizeof(UInt16)));
            Assert.Equal(position, offset);

            Int32 terminatorOffset = content.AsSpan(position).IndexOf((Byte)0);
            Assert.True(terminatorOffset >= 0, $"String {index} is not null-terminated.");
            position += terminatorOffset + 1;
        }

        String[] values = NamedicBin.ReadStrings(content, sample.NewEncoding());

        Assert.Equal(count, values.Length);
        Assert.Equal(content.Length, position);
    }

    [Theory]
    [MemberData(nameof(NamedicSample.All), MemberType = typeof(NamedicSample))]
    public void WriteStrings_ReproducesSourceFileByteForByte(NamedicSample sample)
    {
        String[] values = NamedicBin.ReadStrings(sample.Content, sample.NewEncoding());

        Byte[] written = NamedicBin.WriteStrings(values, sample.NewEncoding());

        Assert.Equal(sample.Content, written);
    }
}
