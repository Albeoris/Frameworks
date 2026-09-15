using Albeoris.Games.Core.NsStreams;
using Xunit;

namespace Albeoris.Games.Core.Tests.NsStreams;

public sealed class HalfByteStreamTests
{
    [Fact]
    public void Read_ExpandsLowNibbleBeforeHighNibble()
    {
        using MemoryStream packed = new([0x21, 0xF3]);
        using HalfByteStream stream = new(packed);
        Byte[] result = new Byte[4];

        stream.ReadExactly(result);

        Assert.Equal([1, 2, 3, 15], result);
        Assert.Equal(4, stream.Position);
    }

    [Fact]
    public void Write_PacksLowNibbleBeforeHighNibbleAndCanLeaveBaseStreamOpen()
    {
        using MemoryStream packed = new();
        using (HalfByteStream stream = new(packed, leaveOpen: true))
            stream.Write([1, 2, 3, 15]);

        Assert.Equal([0x21, 0xF3], packed.ToArray());
    }

    [Fact]
    public void Read_SupportsOddSizedCalls()
    {
        using MemoryStream packed = new([0x21, 0x43]);
        using HalfByteStream stream = new(packed);
        Byte[] first = new Byte[3];
        Byte[] second = new Byte[1];

        stream.ReadExactly(first);
        stream.ReadExactly(second);

        Assert.Equal([1, 2, 3], first);
        Assert.Equal([4], second);
    }
}
