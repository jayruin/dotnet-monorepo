using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FileStorage.Zip;

internal sealed class ZipDirectory : IDirectory
{
    private readonly ZipFileStorage _fileStorage;
    private readonly SyncToAsyncDirectoryAdapter _asyncAdapter;

    private readonly string _archivePath;

    public IFileStorage FileStorage => _fileStorage;

    public string FullPath { get; }

    public ImmutableArray<string> PathParts { get; }

    public ZipDirectory(ZipFileStorage fileStorage, string path)
    {
        _fileStorage = fileStorage;
        try
        {
            FullPath = path;
            PathParts = ZipFileStorage.SplitFullPath(FullPath).ToImmutableArray();
            _archivePath = FullPath + '/';
        }
        catch (Exception exception)
        {
            throw new FileStorageException(exception);
        }
        _asyncAdapter = new(this);
    }

    public bool Exists() => _archivePath == "/"
        || _fileStorage.Entries
            .FirstOrDefault(e => e.FullName.StartsWith(_archivePath)) is not null;

    public Task<bool> ExistsAsync(CancellationToken cancellationToken = default) => _asyncAdapter.ExistsAsync(cancellationToken);

    public IEnumerable<IFile> EnumerateFiles() => EnumerateEntryPaths(false)
        .Where(p => !p.EndsWith('/'))
        .Select(p => new ZipFile(_fileStorage, p));

    public IAsyncEnumerable<IFile> EnumerateFilesAsync(CancellationToken cancellationToken = default) =>
        _asyncAdapter.EnumerateFilesAsync(cancellationToken);

    public IEnumerable<IDirectory> EnumerateDirectories() => EnumerateEntryPaths(false)
        .Where(p => p.EndsWith('/'))
        .Select(p => p[..^1])
        .Select(p => new ZipDirectory(_fileStorage, p));

    public IAsyncEnumerable<IDirectory> EnumerateDirectoriesAsync(CancellationToken cancellationToken = default)
        => _asyncAdapter.EnumerateDirectoriesAsync(cancellationToken);

    public void Create()
    {
        if (!Exists())
        {
            ZipArchiveEntry entry = _fileStorage.CreateEntry(_archivePath);
            if (_fileStorage.Options.FixedTimestamp is DateTimeOffset fixedTimestamp)
            {
                entry.LastWriteTime = fixedTimestamp;
            }
        }
    }

    public Task CreateAsync(CancellationToken cancellationToken = default)
        => _asyncAdapter.CreateAsync(cancellationToken);

    public void Delete()
    {
        if (_archivePath == "/") return;
        _fileStorage.GetEntry(_archivePath)?.Delete();
        foreach (string entryPath in EnumerateEntryPaths(true).ToList())
        {
            _fileStorage.GetEntry(entryPath)?.Delete();
        }
    }

    public Task DeleteAsync(CancellationToken cancellationToken = default) => _asyncAdapter.DeleteAsync(cancellationToken);

    private IEnumerable<string> EnumerateEntryPaths(bool recurse)
    {
        HashSet<string> result = [];
        foreach (ZipArchiveEntry entry in _fileStorage.Entries)
        {
            if (_archivePath != "/" && !entry.FullName.StartsWith(_archivePath) || entry.FullName == _archivePath) continue;
            if (recurse)
            {
                result.Add(entry.FullName);
            }
            else if (_archivePath == "/")
            {
                string truncatedPath = entry.FullName.Split('/')[0];
                if (entry.FullName.Contains('/'))
                {
                    truncatedPath += '/';
                }
                result.Add(truncatedPath);
            }
            else if (entry.FullName.StartsWith(_archivePath))
            {
                string relativePath = entry.FullName[_archivePath.Length..];
                string truncatedPath = relativePath.Split('/')[0];
                if (relativePath.Contains('/'))
                {
                    truncatedPath += '/';
                }
                result.Add(_archivePath + truncatedPath);
            }
        }
        return result;
    }
}
