using Albeoris.Games.Core.NsStreams;
using Xunit;

namespace Albeoris.Games.Core.Tests.NsStreams;

/// <summary>
/// Unit tests for the <see cref="SegmentStream"/> class.
/// </summary>
public class SegmentStreamTests
{
    [Fact]
    public void ReadWithinSegment_ReturnsCorrectData()
    {
        // Arrange
        Byte[] data = [1, 2, 3, 4, 5];
        using var baseStream = new MemoryStream(data);
        using var segmentStream = new SegmentStream(baseStream, 1, 3);
        Byte[] buffer = new Byte[5];

        // Act
        Int32 readCount = segmentStream.Read(buffer, 0, buffer.Length);

        // Assert
        Assert.Equal(3, readCount);
        Assert.Equal(new Byte[] { 2, 3, 4, 0, 0 }, buffer);
    }

    [Fact]
    public void ReadByte_WithinSegment_ReturnsCorrectValue()
    {
        // Arrange
        Byte[] data = [10, 20, 30];
        using var baseStream = new MemoryStream(data);
        using var segmentStream = new SegmentStream(baseStream, 1, 2);

        // Act & Assert
        Assert.Equal(20, segmentStream.ReadByte());
        Assert.Equal(30, segmentStream.ReadByte());
        Assert.Equal(-1, segmentStream.ReadByte()); // End of segment
    }

    [Fact]
    public void WriteByte_WithinSegment_WritesCorrectly()
    {
        // Arrange
        Byte[] data = [1, 2, 3, 4, 5];
        using var baseStream = new MemoryStream(data);
        using var segmentStream = new SegmentStream(baseStream, 1, 3);

        // Act
        segmentStream.WriteByte(99); // overwrites '2'
        segmentStream.WriteByte(100); // overwrites '3'
        segmentStream.WriteByte(101); // overwrites '4'
        // segment length is now fully used

        // Assert
        var result = baseStream.ToArray();
        // We've modified positions 1..3 in the base stream
        Assert.Equal(new Byte[] { 1, 99, 100, 101, 5 }, result);
    }

    [Fact]
    public void SeekAndRead_AfterSeek_PositionIsCorrect()
    {
        // Arrange
        Byte[] data = [10, 20, 30, 40, 50];
        using var baseStream = new MemoryStream(data);
        using var segmentStream = new SegmentStream(baseStream, 0, 5);

        // Act
        segmentStream.Seek(2, SeekOrigin.Begin);
        Int32 val = segmentStream.ReadByte();

        // Assert
        Assert.Equal(30, val);
        Assert.Equal(3, segmentStream.Position); // We read one byte after seeking to 2
    }

    [Fact]
    public void WriteWithinSegment_WritesDataCorrectly()
    {
        // Arrange
        Byte[] data = [100, 101, 102, 103, 104];
        using var baseStream = new MemoryStream(data);
        using var segmentStream = new SegmentStream(baseStream, 1, 3);

        // Act
        segmentStream.Position = 0; // Just making sure
        segmentStream.Write([200, 201], 0, 2);
        segmentStream.Flush();

        // Assert
        Assert.Equal(2, segmentStream.Position);
        Byte[] result = baseStream.ToArray();
        Assert.Equal(new Byte[] { 100, 200, 201, 103, 104 }, result);
    }

    [Fact]
    public void ReadBeyondSegment_ReturnsZero()
    {
        // Arrange
        Byte[] data = [1, 2, 3, 4, 5];
        using var baseStream = new MemoryStream(data);
        using var segmentStream = new SegmentStream(baseStream, 2, 2);
        Byte[] buffer = new Byte[3];

        // Act
        // Read first chunk
        Int32 count1 = segmentStream.Read(buffer, 0, 3);
        // Attempt to read again (should be at end)
        Int32 count2 = segmentStream.Read(buffer, 0, 3);

        // Assert
        Assert.Equal(2, count1);
        Assert.Equal(0, count2);
    }

    [Fact]
    public async Task ReadAsyncWithinSegment_ReturnsCorrectData()
    {
        // Arrange
        Byte[] data = [1, 2, 3, 4, 5];
        using var baseStream = new MemoryStream(data);
        await using var segmentStream = new SegmentStream(baseStream, 1, 3);
        Byte[] buffer = new Byte[5];

        // Act
        Int32 readCount = await segmentStream.ReadAsync(buffer, 0, buffer.Length, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, readCount);
        Assert.Equal(new Byte[] { 2, 3, 4, 0, 0 }, buffer);
    }

    [Fact]
    public async Task WriteAsyncWithinSegment_WritesDataCorrectly()
    {
        // Arrange
        Byte[] data = [10, 20, 30, 40, 50];
        using var baseStream = new MemoryStream(data);
        await using var segmentStream = new SegmentStream(baseStream, 2, 2);

        // Act
        await segmentStream.WriteAsync([99, 100], 0, 2, TestContext.Current.CancellationToken);
        await segmentStream.FlushAsync(TestContext.Current.CancellationToken);

        // Assert
        Byte[] result = baseStream.ToArray();
        Assert.Equal(new Byte[] { 10, 20, 99, 100, 50 }, result);
    }

    [Fact]
    public void ReadSpan_WithinSegment_ReturnsCorrectData()
    {
        // Arrange
        Byte[] data = [1, 2, 3, 4, 5];
        using var baseStream = new MemoryStream(data);
        using var segmentStream = new SegmentStream(baseStream, 1, 3);
        Span<Byte> buffer = new Byte[5];

        // Act
        Int32 readCount = segmentStream.Read(buffer);

        // Assert
        Assert.Equal(3, readCount);
        Assert.Equal(new Byte[] { 2, 3, 4, 0, 0 }, buffer.ToArray());
    }

    [Fact]
    public void WriteSpan_WithinSegment_WritesDataCorrectly()
    {
        // Arrange
        Byte[] data = [0, 0, 0, 0, 0];
        using var baseStream = new MemoryStream(data);
        using var segmentStream = new SegmentStream(baseStream, 1, 3);

        // Act
        segmentStream.Write([10, 11, 12]);

        // Assert
        Assert.Equal(3, segmentStream.Position);
        var result = baseStream.ToArray();
        Assert.Equal(new Byte[] { 0, 10, 11, 12, 0 }, result);
    }

    [Fact]
    public async Task ReadMemoryAsync_WithinSegment_ReturnsCorrectData()
    {
        // Arrange
        Byte[] data = [1, 2, 3, 4, 5];
        using var baseStream = new MemoryStream(data);
        await using var segmentStream = new SegmentStream(baseStream, 1, 3);
        Memory<Byte> buffer = new Byte[5];

        // Act
        Int32 readCount = await segmentStream.ReadAsync(buffer, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, readCount);
        Assert.Equal(new Byte[] { 2, 3, 4, 0, 0 }, buffer.ToArray());
    }

    [Fact]
    public async Task WriteMemoryAsync_WithinSegment_WritesDataCorrectly()
    {
        // Arrange
        Byte[] data = [0, 0, 0, 0, 0];
        using var baseStream = new MemoryStream(data);
        await using var segmentStream = new SegmentStream(baseStream, 1, 3);
        ReadOnlyMemory<Byte> toWrite = new Byte[] { 10, 11 };

        // Act
        await segmentStream.WriteAsync(toWrite, TestContext.Current.CancellationToken);

        // Assert
        var result = baseStream.ToArray();
        Assert.Equal(new Byte[] { 0, 10, 11, 0, 0 }, result);
        Assert.Equal(2, segmentStream.Position);
    }
}