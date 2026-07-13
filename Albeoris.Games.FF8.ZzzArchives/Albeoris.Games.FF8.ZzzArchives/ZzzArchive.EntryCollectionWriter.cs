using Albeoris.Games.Core.NsStreams;

namespace Albeoris.Games.FF8.ZzzArchives;

public sealed partial class ZzzArchive
{
    private sealed class EntryCollectionWriter(Stream stream, EntryCollection collection)
    {
        private readonly Byte[] _buffer = new Byte[1024];
        
        public static void Write(Stream stream, EntryCollection collection)
        {
            EntryCollectionWriter writer = new EntryCollectionWriter(stream, collection);
            writer.WriteEntries();
        }

        private void WriteEntries()
        {
            IReadOnlyList<ZzzArchiveEntry> entries = collection.Entries;
            WriteInt32(entries.Count);

            foreach (ZzzArchiveEntry entry in entries)
            {
                entry.HeaderPosition = stream.Position;
                WriteString(entry.RelativePath);
                WriteInt64(entry.Offset);
                WriteUInt32(entry.Size);
            }
        }

        private void WriteInt32(Int32 value) => stream.WriteStruct(value);
        private void WriteUInt32(UInt32 value) => stream.WriteStruct(value);
        private void WriteInt64(Int64 value) => stream.WriteStruct(value);

        private void WriteString(String value)
        {
            Int32 length = PathEncoding.GetBytes(value, 0, value.Length, _buffer, 0);
            WriteInt32(length);
            stream.Write(_buffer, 0, length);
        }
    }
}