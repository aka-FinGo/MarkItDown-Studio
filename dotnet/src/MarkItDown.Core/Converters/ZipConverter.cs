using System.IO.Compression;

namespace MarkItDown.Core.Converters;

public class ZipConverter
{
    public static IEnumerable<(string EntryName, byte[] Data)> ExtractEntries(byte[] zipBytes)
    {
        using var stream = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name) || entry.FullName.StartsWith("__MACOSX/") || entry.FullName.StartsWith("."))
                continue;

            using var entryStream = entry.Open();
            using var ms = new MemoryStream();
            entryStream.CopyTo(ms);
            yield return (entry.FullName, ms.ToArray());
        }
    }
}
