using EpubProj;
using FileStorage;
using Images;
using MediaTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using umm.Library;
using umm.Storages.Blob;
using umm.Storages.Metadata;
using umm.Storages.Tags;
using umm.Vendors.Abstractions;
using umm.Vendors.Common;

namespace umm.Vendors.EpubProj;

public sealed class EpubProjVendor : IMediaVendor
{
    private static readonly ImmutableArray<string> CoverMediaTypes = [MediaType.Image.Jpeg, MediaType.Image.Png, MediaType.Image.Webp];

    private readonly IMetadataStorage _metadataStorage;
    private readonly IBlobStorage _blobStorage;
    private readonly ITagsStorage _tagsStorage;
    private readonly ILogger<EpubProjVendor> _logger;
    private readonly IEpubProjectLoader _projectLoader;
    private readonly IMediaTypeFileExtensionsMapping _mediaTypeFileExtensionsMapping;
    private readonly IImageLoader _imageLoader;

    public EpubProjVendor(IMetadataStorage metadataStorage, IBlobStorage blobStorage, ITagsStorage tagsStorage,
        IEpubProjectLoader projectLoader, IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping, IImageLoader imageLoader,
        ILogger<EpubProjVendor> logger)
    {
        _metadataStorage = metadataStorage;
        _blobStorage = blobStorage;
        _tagsStorage = tagsStorage;
        _logger = logger;
        _projectLoader = projectLoader;
        _mediaTypeFileExtensionsMapping = mediaTypeFileExtensionsMapping;
        _imageLoader = imageLoader;
    }

    public const string Id = "epubproj";

    private const string EpubExportId = "epub";
    private const string Epub2ExportId = "epub2";
    private const string JpgExportId = "jpg";
    private const string PngExportId = "png";
    private const string WebpExportId = "webp";

    public string VendorId => Id;

    public async IAsyncEnumerable<SearchableMediaEntry> EnumerateAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach ((string vendorId, string contentId) in _metadataStorage.EnumerateContentAsync(cancellationToken).ConfigureAwait(false))
        {
            if (vendorId != VendorId) continue;

            await foreach (SearchableMediaEntry entry in EnumerateEntriesAsync(contentId, cancellationToken).ConfigureAwait(false))
            {
                yield return entry;
            }
        }
    }

    public IAsyncEnumerable<SearchableMediaEntry> EnumerateAsync(string contentId, CancellationToken cancellationToken = default)
        => EnumerateEntriesAsync(contentId, cancellationToken);

    public Task<SearchableMediaEntry?> GetEntryAsync(string contentId, string partId, CancellationToken cancellationToken = default)
        => EnumerateEntriesAsync(contentId, cancellationToken).FirstOrDefaultAsync(cancellationToken).AsTask();

    public async Task ExportAsync(string contentId, string partId, string exportId, Stream stream, CancellationToken cancellationToken = default)
    {
        if (partId.Length != 0) throw new InvalidOperationException($"{VendorId} - Unsupported PartId {partId}.");
        MediaMainId id = new(VendorId, contentId);
        IDirectory projectDirectory = await _blobStorage.GetStorageContainerAsync(id, cancellationToken).ConfigureAwait(false);
        IEpubProject project = await _projectLoader.LoadFromDirectoryAsync(projectDirectory, cancellationToken).ConfigureAwait(false);
        if (exportId == EpubExportId)
        {
            _logger.LogExportingFile(VendorId, contentId, partId, exportId);
            IReadOnlyCollection<IFile> globalFiles = await _projectLoader.GetImplicitGlobalFilesAsync(projectDirectory, cancellationToken).ConfigureAwait(false);
            await project.ExportEpub3Async(stream, globalFiles, cancellationToken).ConfigureAwait(false);
            return;
        }
        else if (exportId == Epub2ExportId)
        {
            _logger.LogExportingFile(VendorId, contentId, partId, exportId);
            IReadOnlyCollection<IFile> globalFiles = await _projectLoader.GetImplicitGlobalFilesAsync(projectDirectory, cancellationToken).ConfigureAwait(false);
            await project.ExportEpub2Async(stream, globalFiles, cancellationToken).ConfigureAwait(false);
            return;
        }
        string mediaType = GetCoverMediaType(exportId);
        if (CoverMediaTypes.Contains(mediaType) && project.CoverFile is not null)
        {
            _logger.LogExportingFile(VendorId, contentId, partId, exportId);
            string? coverMediaType = _mediaTypeFileExtensionsMapping.GetMediaType(project.CoverFile.Extension);
            Stream sourceStream = await project.CoverFile.OpenReadAsync(cancellationToken).ConfigureAwait(false);
            await using (sourceStream.ConfigureAwait(false))
            {
                if (coverMediaType == mediaType)
                {
                    await sourceStream.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    IImage image = await _imageLoader.LoadImageAsync(sourceStream, cancellationToken).ConfigureAwait(false);
                    await image.SaveToAsync(stream, ImageFormatParser.FromMediaType(mediaType), cancellationToken).ConfigureAwait(false);
                }
            }
            return;
        }
        throw new InvalidOperationException($"{VendorId} - Unsupported ExportId {exportId} for file export.");
    }

    public async Task ExportAsync(string contentId, string partId, string exportId, IDirectory directory, CancellationToken cancellationToken = default)
    {
        if (partId.Length != 0) throw new InvalidOperationException($"{VendorId} - Unsupported PartId {partId}.");
        MediaMainId id = new(VendorId, contentId);
        IDirectory projectDirectory = await _blobStorage.GetStorageContainerAsync(id, cancellationToken).ConfigureAwait(false);
        IEpubProject project = await _projectLoader.LoadFromDirectoryAsync(projectDirectory, cancellationToken).ConfigureAwait(false);
        if (exportId == EpubExportId)
        {
            _logger.LogExportingDirectory(VendorId, contentId, partId, exportId);
            IReadOnlyCollection<IFile> globalFiles = await _projectLoader.GetImplicitGlobalFilesAsync(projectDirectory, cancellationToken).ConfigureAwait(false);
            await project.ExportEpub3Async(directory, globalFiles, cancellationToken).ConfigureAwait(false);
            return;
        }
        else if (exportId == Epub2ExportId)
        {
            _logger.LogExportingDirectory(VendorId, contentId, partId, exportId);
            IReadOnlyCollection<IFile> globalFiles = await _projectLoader.GetImplicitGlobalFilesAsync(projectDirectory, cancellationToken).ConfigureAwait(false);
            await project.ExportEpub2Async(directory, globalFiles, cancellationToken).ConfigureAwait(false);
            return;
        }
        throw new InvalidOperationException($"{VendorId} - Unsupported ExportId {exportId} for directory export.");
    }

    public IAsyncEnumerable<string> UpdateContentAsync(IReadOnlyDictionary<string, StringValues> searchQuery, bool force, CancellationToken cancellationToken = default)
        => AsyncEnumerable.Empty<string>();

    private async IAsyncEnumerable<SearchableMediaEntry> EnumerateEntriesAsync(string contentId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        EpubProjMetadataAdapter metadata = await GetMetadataAsync(contentId, cancellationToken).ConfigureAwait(false);
        UniversalMediaMetadata universalMetadata = metadata.Universalize();
        // TODO ToImmutableArrayAsync
        ImmutableArray<MediaExportTarget> exportTargets = [.. await EnumerateExportTargetsAsync(contentId, cancellationToken).ToListAsync(cancellationToken).ConfigureAwait(false)];
        ImmutableSortedSet<string> tags = await GetTagsAsync(contentId, cancellationToken).ConfigureAwait(false);
        ImmutableArray<MetadataSearchField> searchFields = [
            ..metadata.GetSearchFields(),
            new()
            {
                Aliases = ["tag"],
                Values = [..tags],
                ExactMatch = true,
            },
            new()
            {
                Aliases = [nameof(MediaFullId.VendorId)],
                Values = [VendorId],
                ExactMatch = true,
            },
            new()
            {
                Aliases = [nameof(MediaFullId.ContentId)],
                Values = [contentId],
                ExactMatch = true,
            },
            new()
            {
                Aliases = [nameof(MediaFullId.PartId)],
                Values = [string.Empty],
                ExactMatch = true,
            },
        ];
        yield return new()
        {
            MediaEntry = new()
            {
                Id = new(VendorId, contentId, string.Empty),
                Metadata = universalMetadata,
                ExportTargets = exportTargets,
                Tags = tags,
            },
            MetadataSearchFields = searchFields,
        };
    }

    private Task<ImmutableSortedSet<string>> GetTagsAsync(string contentId, CancellationToken cancellationToken)
        => _tagsStorage.GetAsync(new(VendorId, contentId), cancellationToken);

    private async Task<EpubProjMetadataAdapter> GetMetadataAsync(string contentId, CancellationToken cancellationToken)
    {
        MediaMainId id = new(VendorId, contentId);
        IDirectory projectDirectory = await _blobStorage.GetStorageContainerAsync(id, cancellationToken).ConfigureAwait(false);
        IEpubProject project = await _projectLoader.LoadFromDirectoryAsync(projectDirectory, cancellationToken).ConfigureAwait(false);
        return new EpubProjMetadataAdapter(project.Metadata);
    }

    private async IAsyncEnumerable<MediaExportTarget> EnumerateExportTargetsAsync(string contentId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        MediaMainId id = new(VendorId, contentId);
        IDirectory projectDirectory = await _blobStorage.GetStorageContainerAsync(id, cancellationToken).ConfigureAwait(false);
        if (!await projectDirectory.ExistsAsync(cancellationToken).ConfigureAwait(false)) yield break;
        yield return new()
        {
            ExportId = EpubExportId,
            MediaType = MediaType.Application.Epub_Zip,
            SupportsFile = true,
            SupportsDirectory = true,
            MediaFormats = [MediaFormat.Ebook],
        };
        yield return new()
        {
            ExportId = Epub2ExportId,
            MediaType = MediaType.Application.Epub_Zip,
            SupportsFile = true,
            SupportsDirectory = true,
            MediaFormats = [MediaFormat.Ebook],
        };
        IEpubProject project = await _projectLoader.LoadFromDirectoryAsync(projectDirectory, cancellationToken).ConfigureAwait(false);
        if (project.CoverFile is not null)
        {
            string? coverMediaType = _mediaTypeFileExtensionsMapping.GetMediaType(project.CoverFile.Extension);
            if (coverMediaType is not null && CoverMediaTypes.Contains(coverMediaType))
            {
                foreach (string mediaType in CoverMediaTypes)
                {
                    string exportId = GetCoverExportId(mediaType);
                    if (string.IsNullOrWhiteSpace(exportId)) continue;
                    yield return new()
                    {
                        ExportId = exportId,
                        MediaType = mediaType,
                        SupportsFile = true,
                        SupportsDirectory = false,
                        MediaFormats = [MediaFormat.Artwork],
                    };
                }
            }
        }
    }

    private static string GetCoverMediaType(string exportId)
    {
        return exportId switch
        {
            JpgExportId => MediaType.Image.Jpeg,
            PngExportId => MediaType.Image.Png,
            WebpExportId => MediaType.Image.Webp,
            _ => string.Empty,
        };
    }

    private static string GetCoverExportId(string coverMediaType)
    {
        return coverMediaType switch
        {
            MediaType.Image.Jpeg => JpgExportId,
            MediaType.Image.Png => PngExportId,
            MediaType.Image.Webp => WebpExportId,
            _ => string.Empty,
        };
    }
}
