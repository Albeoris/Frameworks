using System.Text;
using Albeoris.Games.Core.NsCapacityCalculator;
using Albeoris.Games.Core.NsCollections;
using Albeoris.Games.Core.NsStreams;
using Albeoris.Games.FF8.FlArchives.Abstractions;

namespace Albeoris.Games.FF8.FlArchives;

public sealed partial class FlArchive
{
    private sealed class EntryCollectionReader(Stream listingStream, Stream metricsStream, Stream contentStream)
    {
        public static EntryCollection Read(Stream listingStream, Stream metricsStream, Stream contentStream)
        {
            ArgumentNullException.ThrowIfNull(listingStream);
            ArgumentNullException.ThrowIfNull(metricsStream);
            ArgumentNullException.ThrowIfNull(contentStream);
            
            EntryCollectionReader reader = new EntryCollectionReader(listingStream, metricsStream, contentStream);
            return reader.ReadEntries();
        }

        private EntryCollection ReadEntries()
        {
            StreamReader listingReader = new StreamReader(listingStream, PathEncoding);
            
            Int64 metricsSize = metricsStream.Length - metricsStream.Position;
            Int64 recordSize = sizeof(Int32) * 3;
            if (metricsSize % recordSize != 0)
                throw new FormatException($"Invalid metrics size. Expected metrics size to be a multiple of record size ({recordSize})");
            Int32 recordCount = checked((Int32)(metricsSize / recordSize));
            
            FlArchiveEntry[] entries = new FlArchiveEntry[recordCount];
            for (Int32 i = 0; i < recordCount && !listingReader.EndOfStream; i++)
            {
                Int64 listingPosition = listingReader.GetBinaryPosition();
                String? line = listingReader.ReadLine();
                if (String.IsNullOrWhiteSpace(line))
                    throw new FormatException($"Unexpected end of file.");
                
                if (!line.StartsWith(InternalPathPrefix, StringComparison.OrdinalIgnoreCase))
                    throw new FormatException($"Invalid internal path: {line}. Expected prefix: {InternalPathPrefix}");

                Int64 metricPosition = metricsStream.Position;
                String relativePath = line.Substring(InternalPathPrefix.Length);
                UInt32 uncompressedContentSize = ReadUInt32();
                UInt32 contentOffset = ReadUInt32();
                FlCompressionMethod compression = (FlCompressionMethod)ReadInt32();

                FlArchiveEntry entry = new FlArchiveEntry(relativePath, contentOffset, uncompressedContentSize, compression, metricPosition, listingPosition);
                entries[i] = entry;
            }

            CapacityCalculator capacityCalculator = new(boundariesCount: recordCount + 1);
            OrderedDictionary<String, FlArchiveEntry> dictionary = new(capacity: recordCount, PathComparer);
            foreach (FlArchiveEntry entry in entries.OrderBy(o => o.Offset))
            {
                dictionary.Add(entry.RelativePath, entry);

                if (entry.Size > 0)
                    capacityCalculator.RegisterBoundary(entry.Offset);
            }

            capacityCalculator.RegisterBoundary(contentStream.Length);

            EntryCollection collection = new EntryCollection(listingStream, metricsStream, contentStream, dictionary, capacityCalculator);
            foreach (FlArchiveEntry entry in entries)
            {
                ValidateEntry(entry, capacityCalculator);
                entry.AttachToArchive(collection);
            }

            return collection;
        }

        private Int32 ReadInt32() => metricsStream.ReadStruct<Int32>();
        private UInt32 ReadUInt32() => metricsStream.ReadStruct<UInt32>();
    }
}