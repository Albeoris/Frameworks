using Albeoris.Games.Core.NsCapacityCalculator;
using Albeoris.Games.Core.NsCollections;
using Albeoris.Games.Core.NSCompression.LZ4;
using Albeoris.Games.Core.NSCompression.LZS;
using Albeoris.Games.Core.NsStreams;
using Albeoris.Games.FF8.FlArchives.Abstractions;

namespace Albeoris.Games.FF8.FlArchives;

public sealed partial class FlArchive
{
    private sealed class EntryCollection
    {
        internal readonly Stream _listingStream;
        internal readonly Stream _metricsStream;
        internal readonly Stream _contentStream;
        
        private readonly OrderedDictionary<String, FlArchiveEntry> _entries;
        private readonly CapacityCalculator _capacityCalculator;

        public EntryCollection(Stream listingStream, Stream metricsStream, Stream contentStream, OrderedDictionary<String, FlArchiveEntry> entries, CapacityCalculator capacityCalculator)
        {
            ArgumentNullException.ThrowIfNull(listingStream);
            ArgumentNullException.ThrowIfNull(metricsStream);
            ArgumentNullException.ThrowIfNull(contentStream);
            ArgumentNullException.ThrowIfNull(entries);
            ArgumentNullException.ThrowIfNull(capacityCalculator);
            
            _listingStream = listingStream;
            _metricsStream = metricsStream;
            _contentStream = contentStream;
            _entries = entries;
            _capacityCalculator = capacityCalculator;
        }
        
        public static EntryCollection CreateEmpty(Stream listingStream, Stream metricsStream, Stream contentStream)
        {
            CapacityCalculator calculator = new CapacityCalculator();
            calculator.RegisterBoundary(contentStream.Length);
            return new EntryCollection(listingStream, metricsStream, contentStream, new OrderedDictionary<String, FlArchiveEntry>(PathComparer), calculator);
        }
        
        public IReadOnlyList<FlArchiveEntry> Entries => _entries.Values;
        
        private FlArchiveEntry? _openedEntry;
        private Boolean _hasChanges;

        public Stream OpenForRead(FlArchiveEntry entry)
        {
            return DecompressEntry(entry);
        }

        public Stream OpenForWrite(FlArchiveEntry entry, UInt32 desiredSize)
        {
            throw new NotImplementedException();
        }
        
        private void OpenEntry(FlArchiveEntry entry)
        {
            FlArchiveEntry? previousValue = Interlocked.CompareExchange(ref _openedEntry, entry, null);
            if (previousValue is not null)
                throw new InvalidOperationException($"The archive is already in use by another stream for entry [{previousValue.RelativePath}].");

            _contentStream.Position = entry.Offset;
        }

        private Stream ProtectEntry(FlArchiveEntry entry, Stream contentStream)
        {
            DisposableStream callback = new DisposableStream(contentStream);
            callback.AfterDispose += AfterDispose;
            return callback;
            
            void AfterDispose(Stream stream, Boolean managedDisposing) => CloseEntry(entry);
        }

        private Stream DecompressEntry(FlArchiveEntry entry)
        {
            OpenEntry(entry);
            switch (entry.Compression)
            {
                case FlCompressionMethod.None:
                {
                    Int64 capacity = _capacityCalculator.GetCapacity(_contentStream.Position);
                    if (capacity < entry.Size)
                        throw new InvalidDataException($"Not enough capacity available: {capacity}. Desired: {entry.Size}");
                    
                    SegmentStream segment = new SegmentStream(_contentStream, _contentStream.Position, entry.Size);
                    return ProtectEntry(entry, segment);
                }
                case FlCompressionMethod.LZS:
                {
                    UInt32 compressedSize = _contentStream.ReadStruct<UInt32>();
                    Int64 capacity = _capacityCalculator.GetCapacity(_contentStream.Position);
                    if (capacity < compressedSize)
                        throw new InvalidDataException($"Not enough capacity available: {capacity}. Desired: {compressedSize}");
                    
                    SegmentStream segment = new SegmentStream(_contentStream, _contentStream.Position, compressedSize);
                    LZSDecompressionStream decompression = new LZSDecompressionStream(segment, compressedSize, entry.Size, leaveOpen: false);
                    return ProtectEntry(entry, decompression);
                }
                case FlCompressionMethod.LZ4:
                {
                    UInt32 compressedSize = _contentStream.ReadStruct<UInt32>();
                    if (compressedSize < 8)
                        throw new InvalidDataException($"Invalid compressed size: {compressedSize}. Minimum size is 8.");
                    compressedSize -= 8;
                    
                    UInt32 magic = _contentStream.ReadStruct<UInt32>(); // LZ4
                    if (magic != 0x5F4C5A34)
                        throw new InvalidDataException($"The magic number is incorrect: {magic}. Expected: 0x5F4C5A34");
                    
                    UInt32 uncompressedSize = _contentStream.ReadStruct<UInt32>();
                    if (uncompressedSize != entry.Size)
                        throw new InvalidDataException($"The uncompressed size is incorrect: {uncompressedSize}. Expected: {entry.Size}");

                    Int64 capacity = _capacityCalculator.GetCapacity(_contentStream.Position);
                    if (capacity < compressedSize)
                        throw new InvalidDataException($"Not enough capacity available: {capacity}. Desired: {compressedSize}");
                    
                    SegmentStream segment = new SegmentStream(_contentStream, _contentStream.Position, compressedSize);
                    LZ4DecompressionStream decompression = new LZ4DecompressionStream(segment, entry.Size, leaveOpen: false);
                    return ProtectEntry(entry, decompression);
                }
                default:
                {
                    throw new NotSupportedException(entry.Compression.ToString());
                }
            }
        }

        private void CloseEntry(FlArchiveEntry entry)
        {
            FlArchiveEntry? previousValue = Interlocked.CompareExchange(ref _openedEntry, null, entry);
            if (ReferenceEquals(previousValue, entry))
                return;

            if (ReferenceEquals(previousValue, null))
                throw new InvalidOperationException("There is no opened entry.");

            throw new InvalidOperationException($"Entry {entry.RelativePath} is not opened.");
        }

        private void RegisterEntryContentPosition(IFlArchiveEntry entry)
        {
            if (entry.Size != 0)
                _capacityCalculator.RegisterBoundary(entry.Offset);
        }

        private void UnregisterEntryContentPosition(IFlArchiveEntry entry)
        {
            if (entry.Size != 0)
                _capacityCalculator.UnregisterBoundary(entry.Offset);
        }
        
        public IFlArchiveEntry AddEntry(String relativePath)
        {
            ArgumentNullException.ThrowIfNull(relativePath);
            
            if (relativePath.Contains('\\'))
                throw new ArgumentException($"Relative path [{relativePath}] contains wrong directory separator. Use '/' instead.");

            FlArchiveEntry entry = new FlArchiveEntry(relativePath, offset: 0, size: 0, FlCompressionMethod.None, metricsPosition: 0, listingPosition: 0);
            if (!_entries.TryAdd(relativePath, entry))
                throw new InvalidOperationException($"The file {relativePath} is already exists inside the archive.");

            entry.AttachToArchive(this);

            EntryCollectionWriter writer = new EntryCollectionWriter(this);
            writer.AppendEntry(entry);
            
            return entry;
        }

        public void RemoveEntry(String relativePath)
        {
            ArgumentNullException.ThrowIfNull(relativePath);
        
            if (!_entries.TryRemove(relativePath, out FlArchiveEntry? entry))
                throw new InvalidOperationException($"The file {relativePath} does not exist inside the archive.");

            entry.DetachFromArchive();
            
            EntryCollectionWriter writer = new EntryCollectionWriter(this);
            writer.RemoveEntry(entry);
            
            UnregisterEntryContentPosition(entry);
        }
        
        public void Flush()
        {
            if (!_hasChanges)
                return;

            throw new NotImplementedException();

            // if (!_capacityCalculator.TryGetCapacity(0, out Int64 headerCapacity) || headerCapacity < _headerSize)
            // {
            //     List<ZzzArchiveEntry> entriesToMove = new();
            //     foreach (ZzzArchiveEntry entry in _entries.Values)
            //     {
            //         if (entry.Size == 0)
            //             continue;
            //
            //         entriesToMove.Add(entry);
            //
            //         headerCapacity = entry.Offset;
            //         if (headerCapacity >= _headerSize + HeaderPadding)
            //             break;
            //     }
            //
            //     // End of file
            //     Int64 delta = _headerSize + HeaderPadding - headerCapacity;
            //     if (delta > 0)
            //     {
            //         _capacityCalculator.UnregisterBoundary(_archiveStream.Length);
            //         _archiveStream.SetLength(_archiveStream.Length + delta);
            //         _capacityCalculator.RegisterBoundary(_archiveStream.Length);
            //     }
            //
            //     Byte[] buffer = new Byte[MovingBufferSize];
            //
            //     foreach (ZzzArchiveEntry entry in entriesToMove)
            //         MoveContentToEndOfArchive(entry, buffer);
            // }
            //
            // headerCapacity = _capacityCalculator.GetCapacity(offset: 0);
            // if (headerCapacity < _headerSize)
            //     throw new InvalidOperationException($"Failed to free space for the header size of {_headerSize}.");
            //
            // _archiveStream.Position = 0;
            //
            // SortEntriesByOffset();
            // EntryCollectionWriter.Write(_archiveStream, this);
            //
            // _hasChanges = false;
        }
    }
}

public sealed class LZSStream
    {
        private readonly Stream _input;
        private readonly Stream _output;
        private readonly CircularBuffer<Byte> _circularBuffer;

        public event EventHandler<Int32> ReverseProgress;

        public LZSStream(Stream input, Stream output)
        {
            if (input == null)
                throw new ArgumentNullException("input");
            if (output == null)
                throw new ArgumentNullException("output");

            if (!input.CanRead)
                throw new ArgumentException("Входной поток не поддерживает чтения.", "input");
            if (!output.CanWrite)
                throw new ArgumentException("Выходной поток не поддерживает записи.", "input");

            _input = input;
            _output = output;
            _circularBuffer = new CircularBuffer<Byte>(4096);
        }

        public void Decompress(Int32 unpackedLength)
        {
            Byte bits = 0, bitsCount = 0;

            while (unpackedLength != 0)
            {
                Int32 b = _input.ReadByte();
                if (b == -1)
                    throw new Exception("Непредвиденный конец входного потока.");

                

                Byte current = (Byte)b;

                if (bitsCount == 0)
                {
                    bits = current;
                    bitsCount = 8;
                    continue;
                }

                if ((bits & 1) != 0)
                {
                    _output.WriteByte(current);
                    _circularBuffer.Write(current);
                    unpackedLength--;
                }
                else
                {
                    Int16 offset = current;

                    b = _input.ReadByte();
                    if (b == -1)
                        throw new Exception("Непредвиденный конец входного потока.");

                    current = (Byte)b;

                    offset += (Int16)((current & 0xF0) << 4);
                    Int16 length = (Int16)((current & 0xF) + 3);

                    for (Int32 i = offset + 18; --length >= 0; i++)
                    {
                        i &= 0xFFF;
                        current = _circularBuffer.GetByOffset(i);
                        _output.WriteByte(current);
                        _circularBuffer.Write(current);
                        unpackedLength--;
                    }
                }

                bits >>= 1;
                bitsCount--;
            }
        }
    }
    
public sealed class CircularBuffer<T>
{
    private readonly T[] _buff;
    private Int32 _index;

    public Int32 Length
    {
        get { return _buff.Length; }
    }

    public Int64 Index
    {
        get { return _index; }
    }

    public CircularBuffer(Int32 length)
    {
        if (length < 1)
            throw new Exception("Длина циклического буфера не может быть меньше 1.");

        _buff = new T[length];
    }

    public void Write(T value)
    {
        _buff[_index] = value;
        _index = (_index + 1) % _buff.Length;
    }

    public void Write(Byte[] value, Int32 index, Int32 length)
    {
        index += (length / _buff.Length) * _buff.Length;
        length %= _buff.Length;

        Int32 last = Math.Min(length, (_buff.Length - _index));
        Array.Copy(value, index, _buff, _index, last);

        Int32 first = length - last;
        if (first != 0)
            Array.Copy(value, index + last, _buff, 0, first);

        _index = (index + length) % _buff.Length;
    }

    public T GetByOffset(Int32 offset)
    {
        return _buff[offset];
    }
}

public class LZSS
    {
        #region Fields

        private const Int32 EOF = -1;
        private const Int32 F = 18;
        private const Int32 N = 4096;
        private const Int32 THRESHOLD = 2;

        #endregion Fields

        #region Methods

        public static Byte[] DecompressAllNew(Byte[] data, Int32 uncompressedSize, Boolean skip = false)
        {
            if (uncompressedSize < 0) throw new ArgumentOutOfRangeException(nameof(uncompressedSize)); // if 0 ignore checks.
            //Memory.Log.WriteLine($"{nameof(LZSS)}::{nameof(DecompressAllNew)} :: decompressing data");
            Byte[] outFileArray;
            using (var infile = new MemoryStream(!skip ? data : data.Skip(4).ToArray()))
            {
                Decode(infile, out outFileArray);
            }
            if (uncompressedSize > 0 && outFileArray.Length != uncompressedSize)
                throw new InvalidDataException($"{nameof(LZSS)}::{nameof(DecompressAllNew)} Expected size ({uncompressedSize}) != ({outFileArray.Length})");
            return outFileArray;
        }

        //Code borrowed from Java's implementation of LZSS by antiquechrono
        private static void Decode(Stream infile, out Byte[] outFileArray)
        {
            var outfile = new List<Byte>();

            var textBuf = new Int32[N + F - 1];    // ring buffer of size N, with extra F-1 bytes to facilitate string comparison

            var r = N - F; var flags = 0;
            for (; ; )
            {
                Int32 c;
                if (((flags >>= 1) & 256) == 0)
                {
                    if ((c = infile.ReadByte()) == EOF) break;
                    flags = c | 0xff00;     // uses higher byte cleverly
                }                           // to Count eight
                if ((flags & 1) == 1)
                {
                    if ((c = infile.ReadByte()) == EOF) break;
                    outfile.Add((Byte)c);
                    textBuf[r++] = c;
                    r &= (N - 1);
                }
                else
                {
                    Int32 i;
                    if ((i = infile.ReadByte()) == EOF) break;
                    Int32 j;
                    if ((j = infile.ReadByte()) == EOF) break;
                    i |= ((j & 0xf0) << 4); j = (j & 0x0f) + THRESHOLD;
                    Int32 k;
                    for (k = 0; k <= j; k++)
                    {
                        c = textBuf[(i + k) & (N - 1)];
                        outfile.Add((Byte)c);
                        textBuf[r++] = c;
                        r &= (N - 1);
                    }
                }
            }
            outFileArray = outfile.ToArray();
        }

        #endregion Methods
    }