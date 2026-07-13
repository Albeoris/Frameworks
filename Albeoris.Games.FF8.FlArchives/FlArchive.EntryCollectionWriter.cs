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

            // Record the byte positions before writing so the entry can be updated or removed later.
            entry.ListingPosition = listingStream.Position;
            entry.MetricsPosition = metricsStream.Position;

            using (StreamWriter listingWriter = new StreamWriter(listingStream, PathEncoding, bufferSize: 1024, leaveOpen: true))
                listingWriter.WriteLine(InternalPathPrefix + entry.RelativePath);

            WriteUInt32(entry.Size);
            WriteUInt32(entry.Offset);
            WriteUInt32((UInt32)entry.Compression);
        }

        public void RemoveEntry(FlArchiveEntry entry)
        {
            // Remove from listing: read remaining lines, rewrite from the removed entry's position, truncate.
            Stream listingStream = entryCollection._listingStream;
            listingStream.Seek(entry.ListingPosition, SeekOrigin.Begin);

            Int64 lineEnd;
            IReadOnlyList<String> restLines;
            using (StreamReader reader = new StreamReader(listingStream, PathEncoding, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true))
            {
                _ = reader.ReadLine();
                lineEnd = reader.GetBinaryPosition();
                restLines = reader.ReadAllLines();
            }

            Int64 listingShift = lineEnd - entry.ListingPosition;

            listingStream.Seek(entry.ListingPosition, SeekOrigin.Begin);
            using (StreamWriter writer = new StreamWriter(listingStream, PathEncoding, bufferSize: 1024, leaveOpen: true))
                writer.WriteAllLines(restLines);
            listingStream.SetLength(listingStream.Position);

            // Shift ListingPosition of all entries that followed the removed line.
            foreach (FlArchiveEntry remainingEntry in entryCollection.Entries)
            {
                if (remainingEntry.ListingPosition > entry.ListingPosition)
                    remainingEntry.ListingPosition -= listingShift;
            }

            // Remove from metrics: shift remaining bytes left, then truncate.
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

            // Shift MetricsPosition of all entries that followed the removed record.
            foreach (FlArchiveEntry remainingEntry in entryCollection.Entries)
            {
                if (remainingEntry.MetricsPosition > entry.MetricsPosition)
                    remainingEntry.MetricsPosition -= metricsEntrySize;
            }
        }

        private void WriteUInt32(UInt32 value) => entryCollection._metricsStream.WriteStruct(value);
    }
}