using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace FileStorage.Zip;

// TODO Async Zip
public sealed class ZipFileStorage : IFileStorage, IDisposable
{
    internal ZipFileStorageOptions Options { get; init; }
    internal ZipArchive Archive { get; set; }

    public ZipFileStorage(Stream stream, ZipFileStorageOptions? options = null)
    {
        Options = options ?? new();
        Archive = new ZipArchive(stream, Options.Mode);
        if (Options.Mode == ZipArchiveMode.Update && Options.FixedTimestamp is DateTimeOffset fixedTimestamp)
        {
            foreach (ZipArchiveEntry entry in Archive.Entries)
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
        Archive.Dispose();
    }

    internal static string JoinPaths(IEnumerable<string> paths)
    {
        return string.Join('/', paths.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    internal static IEnumerable<string> SplitFullPath(string fullPath)
    {
        return fullPath.Split('/').Where(s => !string.IsNullOrWhiteSpace(s));
    }
}
