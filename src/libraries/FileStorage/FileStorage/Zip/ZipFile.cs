using System;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace FileStorage.Zip;

internal sealed class ZipFile : IFile
{
    private readonly ZipFileStorage _fileStorage;
    private readonly SyncToAsyncFileAdapter _asyncAdapter;

    public IFileStorage FileStorage => _fileStorage;

    public string FullPath { get; }

    public ImmutableArray<string> PathParts { get; }

    public ZipFile(ZipFileStorage fileStorage, string path)
    {
        _fileStorage = fileStorage;
        try
        {
            FullPath = path;
            PathParts = ZipFileStorage.SplitFullPath(FullPath).ToImmutableArray();
        }
        catch (Exception exception)
        {
            throw new FileStorageException(exception);
        }
        _asyncAdapter = new(this);
    }

    public bool Exists() => _fileStorage.GetEntry(FullPath) is not null;

    public Task<bool> ExistsAsync(CancellationToken cancellationToken = default) => _asyncAdapter.ExistsAsync(cancellationToken);

    public Stream OpenRead() => Open();

    public Task<Stream> OpenReadAsync(CancellationToken cancellationToken = default) => OpenAsync(cancellationToken);

    public Stream OpenWrite()
    {
        if (Exists())
        {
            Delete();
        }
        ZipArchiveEntry entry = _fileStorage.CreateEntry(FullPath);
        if (_fileStorage.Options.FixedTimestamp is DateTimeOffset fixedTimestamp)
        {
            entry.LastWriteTime = fixedTimestamp;
        }
        return Open();
    }

    public async Task<Stream> OpenWriteAsync(CancellationToken cancellationToken = default)
    {
        if (Exists())
        {
            Delete();
        }
        ZipArchiveEntry entry = _fileStorage.CreateEntry(FullPath);
        if (_fileStorage.Options.FixedTimestamp is DateTimeOffset fixedTimestamp)
        {
            entry.LastWriteTime = fixedTimestamp;
        }
        return await OpenAsync(cancellationToken);
    }

    public void Delete()
    {
        try
        {
            _fileStorage.GetEntry(FullPath)?.Delete();
        }
        catch (Exception exception)
        {
            throw new FileStorageException(exception);
        }
    }

    public Task DeleteAsync(CancellationToken cancellationToken = default) => _asyncAdapter.DeleteAsync(cancellationToken);

    private Stream Open()
    {
        Stream? stream;
        try
        {
            stream = _fileStorage.GetEntry(FullPath)?.Open();
        }
        catch (Exception exception)
        {
            throw new FileStorageException(exception);
        }
        return stream ?? throw new FileStorageException();
    }

    private async Task<Stream> OpenAsync(CancellationToken cancellationToken)
    {
        Stream? stream;
        try
        {
            ZipArchiveEntry? entry = _fileStorage.GetEntry(FullPath);
            stream = entry is null ? null : await entry.OpenAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            throw new FileStorageException(exception);
        }
        return stream ?? throw new FileStorageException();
    }
}
