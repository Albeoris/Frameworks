using Albeoris.Games.Core.NsStreams;
using Albeoris.Games.FF8.FlArchives.Abstractions;

namespace Albeoris.Games.FF8.FlArchives;

public sealed partial class FlArchive
{
    private sealed class EntryCollectionWriter(EntryCollection entryCollection)
    {
        public void AppendEntry(FlArchiveEntry entry)
        {
            Stream listingStream = entryCollection._listingStream;
            Stream metricsStream = entryCollection._metricsStream;
            
            listingStream.Seek(0, SeekOrigin.End);
            metricsStream.Seek(0, SeekOrigin.End);
            
            StreamWriter listingWriter = new StreamWriter(listingStream, PathEncoding);
            listingWriter.WriteLine(InternalPathPrefix + entry.RelativePath);

            WriteUInt32(entry.Size);
            WriteUInt32(entry.Offset);
            WriteUInt32((UInt32)entry.Compression);
        }

        public void RemoveEntry(FlArchiveEntry entry)
        {
            // Remove from listing
            Stream listingStream = entryCollection._listingStream;

            listingStream.Seek(entry.ListingPosition, SeekOrigin.Begin);
            StreamReader reader = new StreamReader(listingStream, PathEncoding);
            _ = reader.ReadLine(); // skip current line
            IReadOnlyList<String> restLines = reader.ReadAllLines();
            
            listingStream.Seek(entry.ListingPosition, SeekOrigin.Begin);
            StreamWriter writer = new StreamWriter(listingStream, PathEncoding);
            writer.WriteAllLines(restLines);
            
            // Remove from metrics
            const Int32 metricsEntrySize = sizeof(UInt32) * 3;

            Stream metricsStream = entryCollection._metricsStream;
            Int64 readPosition = entry.MetricsPosition + metricsEntrySize;
            Int64 writePosition = entry.MetricsPosition;

            metricsStream.SetPosition(readPosition);

            Byte[] buffer = new Byte[64 * 1024];
            Int32 bytesRead;
            while ((bytesRead = metricsStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                metricsStream.Seek(writePosition, SeekOrigin.Begin);
                metricsStream.Write(buffer, 0, bytesRead);
                writePosition += bytesRead;
                readPosition += bytesRead;
                metricsStream.Seek(readPosition, SeekOrigin.Begin);
            }

            metricsStream.SetLength(metricsStream.Length - metricsEntrySize);
        }

        private void WriteUInt32(UInt32 value) => entryCollection._metricsStream.WriteStruct(value);
    }
}