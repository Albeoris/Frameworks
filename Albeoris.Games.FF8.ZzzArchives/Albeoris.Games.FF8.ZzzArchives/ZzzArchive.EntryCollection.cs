using Albeoris.Games.Core.NsCapacityCalculator;
using Albeoris.Games.Core.NsCollections;
using Albeoris.Games.Core.NsStreams;
using Albeoris.Games.FF8.ZzzArchives.Abstractions;

namespace Albeoris.Games.FF8.ZzzArchives;

public sealed partial class ZzzArchive
{
    private sealed class EntryCollection
    {
        private readonly Stream _archiveStream;
        private readonly OrderedDictionary<String, ZzzArchiveEntry> _entries;
        private readonly CapacityCalculator _capacityCalculator;
        
        private Int64 _headerSize;
        private Boolean _hasChanges; 

        private ZzzArchiveEntry? _openedEntry;

        public EntryCollection(Stream archiveStream, Int64 headerSize, OrderedDictionary<String, ZzzArchiveEntry> entries, CapacityCalculator capacityCalculator)
        {
            _archiveStream = archiveStream;
            _headerSize = headerSize;
            _entries = entries;
            _capacityCalculator = capacityCalculator;
        }
        
        public static EntryCollection CreateEmpty(Stream archiveStream)
        {
            CapacityCalculator calculator = new CapacityCalculator();
            calculator.RegisterBoundary(archiveStream.Length);
            return new EntryCollection(archiveStream, headerSize: 4, new OrderedDictionary<String, ZzzArchiveEntry>(PathComparer), calculator);
        }

        public IReadOnlyList<ZzzArchiveEntry> Entries => _entries.Values;
        
        public IZzzArchiveEntry AddEntry(String relativePath)
        {
            ArgumentNullException.ThrowIfNull(relativePath);
            
            if (relativePath.Contains('\\'))
                throw new ArgumentException($"Relative path [{relativePath}] contains wrong directory separator. Use '/' instead.");

            ZzzArchiveEntry entry = new ZzzArchiveEntry(relativePath, offset: 0, size: 0, headerPosition: 0);
            if (!_entries.TryAdd(relativePath, entry))
                throw new InvalidOperationException($"The file {relativePath} is already exists inside the archive.");

            entry.AttachToArchive(this);
            _headerSize += entry.CalculateHeaderSize();
            _hasChanges = true;
            return entry;
        }

        public void RemoveEntry(String relativePath)
        {
            ArgumentNullException.ThrowIfNull(relativePath);
        
            if (!_entries.TryRemove(relativePath, out ZzzArchiveEntry? entry))
                throw new InvalidOperationException($"The file {relativePath} does not exist inside the archive.");

            entry.DetachFromArchive();
            _headerSize -= entry.CalculateHeaderSize();
            _hasChanges = true;
            UnregisterEntryContentPosition(entry);
        }
        
        public void Flush()
        {
            if (!_hasChanges)
                return;
            
            if (!_capacityCalculator.TryGetCapacity(0, out Int64 headerCapacity) || headerCapacity < _headerSize)
            {
                List<ZzzArchiveEntry> entriesToMove = new();
                foreach (ZzzArchiveEntry entry in _entries.Values)
                {
                    if (entry.Size == 0)
                        continue;

                    entriesToMove.Add(entry);

                    headerCapacity = entry.Offset;
                    if (headerCapacity >= _headerSize + HeaderPadding)
                        break;
                }

                // End of file
                Int64 delta = _headerSize + HeaderPadding - headerCapacity;
                if (delta > 0)
                    IncreaseArchiveSize(delta);

                Byte[] buffer = new Byte[MovingBufferSize];

                foreach (ZzzArchiveEntry entry in entriesToMove)
                    MoveContentToEndOfArchive(entry, buffer);
            }
            
            headerCapacity = _capacityCalculator.GetCapacity(offset: 0);
            if (headerCapacity < _headerSize)
                throw new InvalidOperationException($"Failed to free space for the header size of {_headerSize}.");

            _archiveStream.Position = 0;

            SortEntriesByOffset();
            EntryCollectionWriter.Write(_archiveStream, this);
            
            _hasChanges = false;
        }

        private void SortEntriesByOffset()
        {
            if (IsSortedByOffsets())
                return;

            ZzzArchiveEntry[] ordered = _entries.Values.OrderBy(e => e.Offset).ToArray();
            _entries.Clear();

            foreach (ZzzArchiveEntry entry in ordered)
                _entries.Add(entry.RelativePath, entry);
        }

        private Boolean IsSortedByOffsets()
        {
            Int64 lastOffset = Int64.MinValue;
            foreach (var entry in _entries.Values)
            {
                if (entry.Size == 0)
                    continue;

                if (entry.Offset < lastOffset)
                    return false;

                lastOffset = entry.Offset;
            }

            return true;
        }

        private void MoveContentToEndOfArchive(ZzzArchiveEntry entry, Byte[] buffer)
        {
            Int64 fromOffset = entry.Offset;
            Int64 toOffset = _archiveStream.Length;
            Int64 leftSize = entry.Size;
            
            UnregisterEntryContentPosition(entry);

            if (entry.Size != 0)
                IncreaseArchiveSize(entry.Size);

            entry.Offset = toOffset;
            RegisterEntryContentPosition(entry);

            while (leftSize > 0)
            {
                Int32 chunkSize = checked((Int32)Math.Min(leftSize, buffer.Length));

                _archiveStream.Position = fromOffset;
                _archiveStream.ReadExactly(buffer, 0, chunkSize);
                
                _archiveStream.Position = toOffset;
                _archiveStream.Write(buffer, 0, chunkSize);
                
                leftSize -= chunkSize;
                fromOffset += chunkSize;
                toOffset += chunkSize;
            }
        }

        internal Stream OpenForRead(ZzzArchiveEntry entry)
        {
            return OpenEntry(entry);
        }

        internal Stream OpenForWrite(ZzzArchiveEntry entry, UInt32 desiredSize)
        {
            Flush();
            
            Int64 capacity = _capacityCalculator.GetCapacity(entry.Offset);
            
            if (capacity < desiredSize || entry.Offset == 0)
            {
                UnregisterEntryContentPosition(entry);
                entry.Offset = _archiveStream.Length;
                entry.Size = desiredSize;

                if (entry.Size != 0)
                    IncreaseArchiveSize(desiredSize);

                RegisterEntryContentPosition(entry);
                _archiveStream.Position = entry.CalculateHeaderOffsetPosition();
                _archiveStream.WriteStruct(entry.Offset);
                _archiveStream.WriteStruct(entry.Size);
            }
            else if (entry.Size != desiredSize)
            {
                entry.Size = desiredSize;
                _archiveStream.Position = entry.CalculateHeaderSizePosition();
                _archiveStream.WriteStruct(entry.Size);
            }

            return OpenEntry(entry);
        }

        private Stream OpenEntry(ZzzArchiveEntry entry)
        {
            ZzzArchiveEntry? previousValue = Interlocked.CompareExchange(ref _openedEntry, entry, null);
            if (previousValue is not null)
                throw new InvalidOperationException($"The archive is already in use by another stream for entry [{previousValue.RelativePath}].");

            ValidateEntry(entry, _capacityCalculator);
            
            SegmentStream segment = new SegmentStream(_archiveStream, entry.Offset, entry.Size);
            DisposableStream callback = new DisposableStream(segment);
            callback.AfterDispose += AfterDispose;
            return callback;

            void AfterDispose(Stream stream, Boolean managedDisposing) => CloseEntry(entry);
        }

        private void CloseEntry(ZzzArchiveEntry entry)
        {
            ZzzArchiveEntry? previousValue = Interlocked.CompareExchange(ref _openedEntry, null, entry);
            if (ReferenceEquals(previousValue, entry))
                return;

            if (ReferenceEquals(previousValue, null))
                throw new InvalidOperationException("There is no opened entry.");

            throw new InvalidOperationException($"Entry {entry.RelativePath} is not opened.");
        }

        private void RegisterEntryContentPosition(IZzzArchiveEntry entry)
        {
            if (entry.Size != 0)
                _capacityCalculator.RegisterBoundary(entry.Offset);
        }

        private void UnregisterEntryContentPosition(IZzzArchiveEntry entry)
        {
            if (entry.Size != 0)
                _capacityCalculator.UnregisterBoundary(entry.Offset);
        }

        private void IncreaseArchiveSize(Int64 desiredSize)
        {
            _capacityCalculator.UnregisterBoundary(_archiveStream.Length);
            _archiveStream.SetLength(_archiveStream.Length + desiredSize);
            _capacityCalculator.RegisterBoundary(_archiveStream.Length);
        }
    }
}