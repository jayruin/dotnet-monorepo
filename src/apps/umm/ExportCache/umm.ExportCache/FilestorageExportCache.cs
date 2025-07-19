using FileStorage;
using MediaTypes;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace umm.ExportCache;

public class FilestorageExportCache : IExportCache
{
    private readonly IMediaTypeFileExtensionsMapping _mediaTypeFileExtensionsMapping;
    private readonly FilestorageExportCacheOptions _options;

    public FilestorageExportCache(IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping, FilestorageExportCacheOptions options)
    {
        _mediaTypeFileExtensionsMapping = mediaTypeFileExtensionsMapping;
        _options = options;
    }

    public Task<bool> CanHandleFileAsync(string vendorId, string contentId, string partId, string mediaType, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_options.HandleFiles && CanHandleMediaType(mediaType));
    }

    public Task<bool> CanHandleDirectoryAsync(string vendorId, string contentId, string partId, string mediaType, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_options.HandleDirectories && CanHandleMediaType(mediaType));
    }

    public async Task<Stream> GetStreamForCachingAsync(string vendorId, string contentId, string partId, string mediaType, CancellationToken cancellationToken = default)
    {
        ThrowIfCannotHandleFiles();
        await _options.RootDirectory.CreateAsync(cancellationToken).ConfigureAwait(false);
        return await GetFile(vendorId, contentId, partId, mediaType).OpenWriteAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IDirectory> GetDirectoryForCachingAsync(string vendorId, string contentId, string partId, string mediaType, CancellationToken cancellationToken = default)
    {
        ThrowIfCannotHandleDirectories();
        await _options.RootDirectory.CreateAsync(cancellationToken).ConfigureAwait(false);
        return GetDirectory(vendorId, contentId, partId, mediaType);
    }

    public async Task ExportAsync(string vendorId, string contentId, string partId, string mediaType, Stream stream, CancellationToken cancellationToken = default)
    {
        ThrowIfCannotHandleFiles();
        Stream sourceStream = await GetFile(vendorId, contentId, partId, mediaType).OpenReadAsync(cancellationToken).ConfigureAwait(false);
        await using (sourceStream.ConfigureAwait(false))
        {
            await sourceStream.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
        }
    }

    public Task ExportAsync(string vendorId, string contentId, string partId, string mediaType, IDirectory directory, CancellationToken cancellationToken = default)
    {
        ThrowIfCannotHandleDirectories();
        return GetDirectory(vendorId, contentId, partId, mediaType).CopyToAsync(directory, cancellationToken);
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        if (await _options.RootDirectory.ExistsAsync(cancellationToken).ConfigureAwait(false))
        {
            await _options.RootDirectory.DeleteAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private void ThrowIfCannotHandleFiles()
    {
        if (!_options.HandleFiles) throw new InvalidOperationException("Cannot handle files");
    }

    private void ThrowIfCannotHandleDirectories()
    {
        if (!_options.HandleDirectories) throw new InvalidOperationException("Cannot handle directories");
    }

    private bool CanHandleMediaType(string mediaType) => _options.MediaTypes.Count == 0 || _options.MediaTypes.Contains(mediaType);

    private IFile GetFile(string vendorId, string contentId, string partId, string mediaType)
        => _options.RootDirectory.GetFile(GetName(vendorId, contentId, partId, mediaType, "file"));

    private IDirectory GetDirectory(string vendorId, string contentId, string partId, string mediaType)
        => _options.RootDirectory.GetDirectory(GetName(vendorId, contentId, partId, mediaType, "directory"));

    private string GetName(string vendorId, string contentId, string partId, string mediaType, string type)
        => $"{vendorId}.{contentId}.{partId}.{type}{_mediaTypeFileExtensionsMapping.GetFileExtension(mediaType)}";
}
