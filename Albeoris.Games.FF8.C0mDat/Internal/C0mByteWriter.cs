using System.Buffers;
using System.Buffers.Binary;

namespace Albeoris.Games.FF8.C0mDat.Internal;

/// <summary>A little-endian append-only buffer used by the native writers.</summary>
internal sealed class C0mByteWriter
{
    private readonly ArrayBufferWriter<Byte> _buffer = new();

    public Int32 Length => _buffer.WrittenCount;

    public void WriteUInt16(UInt16 value)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(_buffer.GetSpan(sizeof(UInt16)), value);
        _buffer.Advance(sizeof(UInt16));
    }

    public void WriteUInt32(UInt32 value)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(_buffer.GetSpan(sizeof(UInt32)), value);
        _buffer.Advance(sizeof(UInt32));
    }

    public void WriteBytes(ReadOnlySpan<Byte> bytes)
    {
        _buffer.Write(bytes);
    }

    public void WriteZeros(Int32 count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (count == 0)
        {
            return;
        }

        _buffer.GetSpan(count)[..count].Clear();
        _buffer.Advance(count);
    }

    public void PadTo(Int32 length)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(length, Length);
        WriteZeros(length - Length);
    }

    public void PadToFour()
    {
        PadTo(C0mFormat.AlignToFour(Length));
    }

    public Byte[] ToArray() => _buffer.WrittenSpan.ToArray();
}
