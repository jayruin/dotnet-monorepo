using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FileStorage.Zip;

public sealed class ZipFileStorage : IFileStorage, IDisposable, IAsyncDisposable
{
    private readonly ZipArchive _archive;
    private readonly List<ZipArchiveEntry> _createModeEntries = [];

    internal ZipFileStorageOptions Options { get; init; }
    internal IReadOnlyList<ZipArchiveEntry> Entries
        => Options.Mode == ZipArchiveMode.Create
            ? _createModeEntries
            : _archive.Entries;

    private ZipFileStorage(ZipArchive zipArchive, ZipFileStorageOptions options)
    {
        _archive = zipArchive;
        Options = options;
        if (Options.Mode == ZipArchiveMode.Update && Options.FixedTimestamp is DateTimeOffset fixedTimestamp)
        {
            foreach (ZipArchiveEntry entry in _archive.Entries)
            {
                entry.LastWriteTime = fixedTimestamp;
            }
        }
    }

    public static ZipFileStorage Create(Stream stream, ZipFileStorageOptions? options = null)
    {
        options ??= new();
        ZipArchive zipArchive = new(stream, options.Mode);
        return new ZipFileStorage(zipArchive, options);
    }

    public static async Task<ZipFileStorage> CreateAsync(Stream stream, ZipFileStorageOptions? options = null, CancellationToken cancellationToken = default)
    {
        options ??= new();
        ZipArchive zipArchive = await ZipArchive.CreateAsync(stream, options.Mode, false, null, cancellationToken).ConfigureAwait(false);
        return new ZipFileStorage(zipArchive, options);
    }

    public IFile GetFile(params IEnumerable<string> paths)
    {
        return new ZipFile(this, JoinPaths(paths));
    }

    public IDirectory GetDirectory(params IEnumerable<string> paths)
    {
        return new ZipDirectory(this, JoinPaths(paths));
    }

    public void Dispose() => _archive.Dispose();

    public ValueTask DisposeAsync() => _archive.DisposeAsync();

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
