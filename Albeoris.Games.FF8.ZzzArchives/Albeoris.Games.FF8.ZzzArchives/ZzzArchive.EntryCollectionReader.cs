using Albeoris.Games.Core.NsCapacityCalculator;
using Albeoris.Games.Core.NsCollections;
using Albeoris.Games.Core.NsStreams;

namespace Albeoris.Games.FF8.ZzzArchives;

public sealed partial class ZzzArchive
{
    private sealed class EntryCollectionReader(Stream stream)
    {
        private readonly Byte[] _buffer = new Byte[1024];
        
        public static EntryCollection Read(Stream stream)
        {
            EntryCollectionReader reader = new EntryCollectionReader(stream);
            return reader.ReadEntries();
        }

        private EntryCollection ReadEntries()
        {
            Int64 headerBeginning = stream.Position;
            Int32 entryCount = ReadInt32();
            
            ZzzArchiveEntry[] entries = new ZzzArchiveEntry[entryCount];
            for (Int32 i = 0; i < entryCount; i++)
            {
                Int64 position = stream.Position;
                String relativePath = ReadString();
                Int64 offset = ReadInt64();
                UInt32 size = ReadUInt32();
                entries[i] = new ZzzArchiveEntry(relativePath, offset, size, position);
            }
            
            Int64 headerSize = stream.Position - headerBeginning;
            
            CapacityCalculator capacityCalculator = new(boundariesCount: entryCount + 1);
            OrderedDictionary<String, ZzzArchiveEntry> dictionary = new(capacity:entryCount, PathComparer);
            foreach (ZzzArchiveEntry entry in entries.OrderBy(o => o.Offset))
            {
                dictionary.Add(entry.RelativePath, entry);

                if (entry.Size > 0)
                    capacityCalculator.RegisterBoundary(entry.Offset);
            }

            capacityCalculator.RegisterBoundary(stream.Length);

            EntryCollection collection = new EntryCollection(stream, headerSize, dictionary, capacityCalculator);
            foreach (ZzzArchiveEntry entry in entries)
            {
                ValidateEntry(entry, capacityCalculator);
                entry.AttachToArchive(collection);
            }

            return collection;
        }

        private Int32 ReadInt32() => stream.ReadStruct<Int32>();
        private UInt32 ReadUInt32() => stream.ReadStruct<UInt32>();
        private Int64 ReadInt64() => stream.ReadStruct<Int64>();

        private String ReadString()
        {
            Int32 length = ReadInt32();
            stream.ReadExactly(_buffer, 0, length);
            return PathEncoding.GetString(_buffer, 0, length);
        }
    }
}