using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace FileStorage.Zip;

// TODO Async Zip
public sealed class ZipFileStorage : IFileStorage, IDisposable
{
    private readonly ZipArchive _archive;
    private readonly List<ZipArchiveEntry> _createModeEntries = [];

    internal ZipFileStorageOptions Options { get; init; }
    internal IReadOnlyList<ZipArchiveEntry> Entries
        => Options.Mode == ZipArchiveMode.Create
            ? _createModeEntries
            : _archive.Entries;

    public ZipFileStorage(Stream stream, ZipFileStorageOptions? options = null)
    {
        Options = options ?? new();
        _archive = new ZipArchive(stream, Options.Mode);
        if (Options.Mode == ZipArchiveMode.Update && Options.FixedTimestamp is DateTimeOffset fixedTimestamp)
        {
            foreach (ZipArchiveEntry entry in _archive.Entries)
            {
                entry.LastWriteTime = fixedTimestamp;
            }
        }
    }

    public IFile GetFile(params IEnumerable<string> paths)
    {
        return new ZipFile(this, JoinPaths(paths));
    }

    public IDirectory GetDirectory(params IEnumerable<string> paths)
    {
        return new ZipDirectory(this, JoinPaths(paths));
    }

    public void Dispose()
    {
        _archive.Dispose();
    }

    internal static string JoinPaths(IEnumerable<string> paths)
    {
        return string.Join('/', paths.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    internal static IEnumerable<string> SplitFullPath(string fullPath)
    {
        return fullPath.Split('/').Where(s => !string.IsNullOrWhiteSpace(s));
    }

    internal ZipArchiveEntry CreateEntry(string entryName)
    {
        CompressionLevel compression = Options.Compression;
        foreach ((string prefix, CompressionLevel compressionOverride) in Options.CompressionOverrides)
        {
            if (entryName.StartsWith(prefix))
            {
                compression = compressionOverride;
                break;
            }
        }
        ZipArchiveEntry entry = _archive.CreateEntry(entryName, compression);
        if (Options.Mode == ZipArchiveMode.Create)
        {
            _createModeEntries.Add(entry);
        }
        return entry;
    }

    internal ZipArchiveEntry? GetEntry(string entryName)
        => Options.Mode == ZipArchiveMode.Create
            ? _createModeEntries.FirstOrDefault(e => e.FullName == entryName)
            : _archive.GetEntry(entryName);
}
