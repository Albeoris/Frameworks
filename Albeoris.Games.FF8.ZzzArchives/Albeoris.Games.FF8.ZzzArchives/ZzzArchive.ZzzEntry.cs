using System.Diagnostics.CodeAnalysis;
using Albeoris.Games.FF8.ZzzArchives.Abstractions;

namespace Albeoris.Games.FF8.ZzzArchives;

public sealed partial class ZzzArchive
{
    private sealed class ZzzArchiveEntry : IZzzArchiveEntry
    {
        private EntryCollection? _collection;
        
        public String RelativePath { get; }
        public Int64 Offset { get; internal set; }
        public UInt32 Size { get; internal set; }
        public Int64 HeaderPosition { get; internal set; }

        public ZzzArchiveEntry(String relativePath, Int64 offset, UInt32 size, Int64 headerPosition)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(relativePath, nameof(relativePath));
            ArgumentOutOfRangeException.ThrowIfNegative(offset, nameof(offset));
            ArgumentOutOfRangeException.ThrowIfNegative(size, nameof(size));

            RelativePath = relativePath;
            Offset = offset;
            Size = size;

            HeaderPosition = headerPosition;
        }

        public Stream OpenForRead()
        {
            ThrowIfDetachedFromArchive();
            
            return _collection.OpenForRead(this);
        }

        public Stream OpenForWrite(UInt32 desiredSize)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(desiredSize, nameof(desiredSize));
            ThrowIfDetachedFromArchive();

            return _collection.OpenForWrite(this, desiredSize);
        }

        internal Int32 CalculateHeaderSize()
        {
            return sizeof(Int32) + PathEncoding.GetByteCount(RelativePath) + sizeof(Int64) + sizeof(UInt32);
        }

        internal Int64 CalculateHeaderOffsetPosition()
        {
            return HeaderPosition + sizeof(Int32) + PathEncoding.GetByteCount(RelativePath);
        }

        internal Int64 CalculateHeaderSizePosition()
        {
            return HeaderPosition + sizeof(Int32) + PathEncoding.GetByteCount(RelativePath) + sizeof(Int64);
        }

        internal void AttachToArchive(EntryCollection entries)
        {
            ThrowIfAttachedFromArchive();
            
            _collection = entries;
        }

        internal void DetachFromArchive()
        {
            ThrowIfDetachedFromArchive();
            
            _collection = null;
        }
        
        private void ThrowIfAttachedFromArchive()
        {
            if (_collection is not null)
                throw new InvalidOperationException($"The entry {RelativePath} has been added to an archive.");
        }

        [MemberNotNull(nameof(_collection))]
        private void ThrowIfDetachedFromArchive()
        {
            if (_collection is null)
                throw new InvalidOperationException($"The entry {RelativePath} has been removed from an archive.");
        }
    }
}