using FileStorage;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using umm.Library;

namespace umm.ExportCache;

public sealed class FilestorageExportCache : IExportCache
{
    private readonly FilestorageExportCacheOptions _options;

    public FilestorageExportCache(FilestorageExportCacheOptions options)
    {
        _options = options;
    }

    public Task<bool> HasFileAsync(MediaFullId id, string exportId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return GetFile(id, exportId).ExistsAsync(cancellationToken);
    }

    public Task<bool> HasDirectoryAsync(MediaFullId id, string exportId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return GetDirectory(id, exportId).ExistsAsync(cancellationToken);
    }

    public Task<bool> CanHandleFileAsync(string vendorId, string mediaType, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_options.HandleFiles && CanHandleMediaType(vendorId, mediaType));
    }

    public Task<bool> CanHandleDirectoryAsync(string vendorId, string mediaType, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_options.HandleDirectories && CanHandleMediaType(vendorId, mediaType));
    }

    public async Task<Stream> GetStreamForCachingAsync(MediaFullId id, string exportId, CancellationToken cancellationToken = default)
    {
        ThrowIfCannotHandleFiles();
        await GetVendorDirectory(id.VendorId).CreateAsync(cancellationToken).ConfigureAwait(false);
        return await GetFile(id, exportId).OpenWriteAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IDirectory> GetDirectoryForCachingAsync(MediaFullId id, string exportId, CancellationToken cancellationToken = default)
    {
        ThrowIfCannotHandleDirectories();
        await GetVendorDirectory(id.VendorId).CreateAsync(cancellationToken).ConfigureAwait(false);
        return GetDirectory(id, exportId);
    }

    public async Task ExportAsync(MediaFullId id, string exportId, Stream stream, CancellationToken cancellationToken = default)
    {
        ThrowIfCannotHandleFiles();
        Stream sourceStream = await GetFile(id, exportId).OpenReadAsync(cancellationToken).ConfigureAwait(false);
        await using (sourceStream.ConfigureAwait(false))
        {
            await sourceStream.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
        }
    }

    public Task ExportAsync(MediaFullId id, string exportId, IDirectory directory, CancellationToken cancellationToken = default)
    {
        ThrowIfCannotHandleDirectories();
        return GetDirectory(id, exportId).CopyToAsync(directory, cancellationToken);
    }

    public async Task ClearAsync(string vendorId, CancellationToken cancellationToken = default)
    {
        IDirectory vendorDirectory = GetVendorDirectory(vendorId);
        await vendorDirectory.DeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await _options.RootDirectory.DeleteAsync(cancellationToken).ConfigureAwait(false);
    }

    private void ThrowIfCannotHandleFiles()
    {
        if (!_options.HandleFiles) throw new InvalidOperationException("Cannot handle files");
    }

    private void ThrowIfCannotHandleDirectories()
    {
        if (!_options.HandleDirectories) throw new InvalidOperationException("Cannot handle directories");
    }

    private bool CanHandleMediaType(string vendorId, string mediaType)
        => _options.MediaTypes.Count == 0
            || (_options.VendorOverrides.TryGetValue(vendorId, out FilestorageExportCacheVendorOverrideOptions? vendorOverride)
                && vendorOverride.MediaTypes.Contains(mediaType))
            || _options.MediaTypes.Contains(mediaType);

    private IDirectory GetVendorDirectory(string vendorId)
        => _options.RootDirectory.GetDirectory(vendorId);

    private IFile GetFile(MediaFullId id, string exportId)
        => GetVendorDirectory(id.VendorId).GetFile(GetName(id, exportId, "file"));

    private IDirectory GetDirectory(MediaFullId id, string exportId)
        => GetVendorDirectory(id.VendorId).GetDirectory(GetName(id, exportId, "directory"));

    private static string GetName(MediaFullId id, string exportId, string type)
        => $"{id.ToCombinedString()}.{type}.{exportId}";
}
