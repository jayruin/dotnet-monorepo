using EpubProj;
using FileStorage;
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
    private readonly IMetadataStorage _metadataStorage;
    private readonly IBlobStorage _blobStorage;
    private readonly ITagsStorage _tagsStorage;
    private readonly ILogger<EpubProjVendor> _logger;
    private readonly IEpubProjectLoader _projectLoader;
    private readonly IMediaTypeFileExtensionsMapping _mediaTypeFileExtensionsMapping;

    public EpubProjVendor(IMetadataStorage metadataStorage, IBlobStorage blobStorage, ITagsStorage tagsStorage,
        IEpubProjectLoader projectLoader, IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping,
        ILogger<EpubProjVendor> logger)
    {
        _metadataStorage = metadataStorage;
        _blobStorage = blobStorage;
        _tagsStorage = tagsStorage;
        _logger = logger;
        _projectLoader = projectLoader;
        _mediaTypeFileExtensionsMapping = mediaTypeFileExtensionsMapping;
    }

    public const string Id = "epubproj";

    private const string EpubVersionKey = "epub";
    private const string Epub2Id = "epub2";

    public string VendorId => Id;

    public async IAsyncEnumerable<SearchableMediaEntry> EnumerateAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach ((string vendorId, string contentId) in _metadataStorage.EnumerateContentAsync(cancellationToken).ConfigureAwait(false))
        {
            if (vendorId != VendorId) continue;

            await foreach (SearchableMediaEntry entry in EnumerateEntriesAsync(contentId, [string.Empty, Epub2Id], cancellationToken).ConfigureAwait(false))
            {
                yield return entry;
            }
        }
    }

    public IAsyncEnumerable<SearchableMediaEntry> EnumerateAsync(string contentId, CancellationToken cancellationToken = default)
        => EnumerateEntriesAsync(contentId, [string.Empty, Epub2Id], cancellationToken);

    public Task<SearchableMediaEntry?> GetEntryAsync(string contentId, string partId, CancellationToken cancellationToken = default)
        => EnumerateEntriesAsync(contentId, [partId], cancellationToken).FirstOrDefaultAsync(cancellationToken).AsTask();

    public async Task ExportAsync(string contentId, string partId, string mediaType, Stream stream, CancellationToken cancellationToken = default)
    {
        MediaMainId id = new(VendorId, contentId);
        if (partId.Length == 0 || partId == Epub2Id)
        {
            IDirectory projectDirectory = await _blobStorage.GetStorageContainerAsync(id, cancellationToken).ConfigureAwait(false);
            IEpubProject project = await _projectLoader.LoadFromDirectoryAsync(projectDirectory, cancellationToken).ConfigureAwait(false);
            if (mediaType == MediaType.Application.Epub_Zip)
            {
                if (partId.Length == 0)
                {
                    _logger.LogExportingFile(VendorId, contentId, partId, mediaType);
                    IReadOnlyCollection<IFile> globalFiles = await _projectLoader.GetImplicitGlobalFilesAsync(projectDirectory, cancellationToken).ConfigureAwait(false);
                    await project.ExportEpub3Async(stream, globalFiles, cancellationToken).ConfigureAwait(false);
                    return;
                }
                if (partId == Epub2Id)
                {
                    _logger.LogExportingFile(VendorId, contentId, partId, mediaType);
                    IReadOnlyCollection<IFile> globalFiles = await _projectLoader.GetImplicitGlobalFilesAsync(projectDirectory, cancellationToken).ConfigureAwait(false);
                    await project.ExportEpub2Async(stream, globalFiles, cancellationToken).ConfigureAwait(false);
                    return;
                }
            }
            if (project.CoverFile is not null)
            {
                string? coverMediaType = _mediaTypeFileExtensionsMapping.GetMediaType(project.CoverFile.Extension);
                if (mediaType == coverMediaType)
                {
                    _logger.LogExportingFile(VendorId, contentId, partId, mediaType);
                    Stream sourceStream = await project.CoverFile.OpenReadAsync(cancellationToken).ConfigureAwait(false);
                    await using (sourceStream.ConfigureAwait(false))
                    {
                        await sourceStream.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
                    }
                    return;
                }
            }
        }
        throw new InvalidOperationException($"{VendorId} - Unsupported MediaType {mediaType} for file export.");
    }

    public async Task ExportAsync(string contentId, string partId, string mediaType, IDirectory directory, CancellationToken cancellationToken = default)
    {
        MediaMainId id = new(VendorId, contentId);
        if (partId.Length == 0 || partId == Epub2Id)
        {
            IDirectory projectDirectory = await _blobStorage.GetStorageContainerAsync(id, cancellationToken).ConfigureAwait(false);
            IEpubProject project = await _projectLoader.LoadFromDirectoryAsync(projectDirectory, cancellationToken).ConfigureAwait(false);
            if (mediaType == MediaType.Application.Epub_Zip)
            {
                if (partId.Length == 0)
                {
                    _logger.LogExportingDirectory(VendorId, contentId, partId, mediaType);
                    IReadOnlyCollection<IFile> globalFiles = await _projectLoader.GetImplicitGlobalFilesAsync(projectDirectory, cancellationToken).ConfigureAwait(false);
                    await project.ExportEpub3Async(directory, globalFiles, cancellationToken).ConfigureAwait(false);
                    return;
                }
                if (partId == Epub2Id)
                {
                    _logger.LogExportingDirectory(VendorId, contentId, partId, mediaType);
                    IReadOnlyCollection<IFile> globalFiles = await _projectLoader.GetImplicitGlobalFilesAsync(projectDirectory, cancellationToken).ConfigureAwait(false);
                    await project.ExportEpub2Async(directory, globalFiles, cancellationToken).ConfigureAwait(false);
                    return;
                }
            }
        }
        throw new InvalidOperationException($"{VendorId} - Unsupported MediaType {mediaType} for directory export.");
    }

    public IAsyncEnumerable<string> UpdateContentAsync(IReadOnlyDictionary<string, StringValues> searchQuery, bool force, CancellationToken cancellationToken = default)
        => AsyncEnumerable.Empty<string>();

    private async IAsyncEnumerable<SearchableMediaEntry> EnumerateEntriesAsync(string contentId, IEnumerable<string> partIds, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        EpubProjMetadataAdapter metadata = await GetMetadataAsync(contentId, cancellationToken).ConfigureAwait(false);
        UniversalMediaMetadata universalMetadata = metadata.Universalize();
        // TODO ToImmutableArrayAsync
        ImmutableArray<MediaExportTarget> exportTargets = [.. await EnumerateExportTargetsAsync(contentId, cancellationToken).ToListAsync(cancellationToken).ConfigureAwait(false)];
        ImmutableSortedSet<string> tags = await GetTagsAsync(contentId, cancellationToken).ConfigureAwait(false);
        ImmutableArray<MetadataSearchField> sharedSearchFields = [
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
        ];

        foreach (string partId in partIds)
        {
            if (partId.Length == 0)
            {
                ImmutableArray<MetadataSearchField> epub3SearchFields = [
                    new()
                    {
                        Aliases = [nameof(MediaFullId.PartId)],
                        Values = [string.Empty],
                        ExactMatch = true,
                    },
                    new()
                    {
                        Aliases = [EpubVersionKey],
                        Values = ["3"],
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
                    MetadataSearchFields = sharedSearchFields.AddRange(epub3SearchFields),
                };
            }
            else if (partId == Epub2Id)
            {
                ImmutableArray<MetadataSearchField> epub2SearchFields = [
                    new()
                    {
                        Aliases = [nameof(MediaFullId.PartId)],
                        Values = [Epub2Id],
                        ExactMatch = true,
                    },
                    new()
                    {
                        Aliases = [EpubVersionKey],
                        Values = ["2"],
                        ExactMatch = true,
                    },
                ];
                yield return new()
                {
                    MediaEntry = new()
                    {
                        Id = new(VendorId, contentId, Epub2Id),
                        Metadata = universalMetadata.With(title: $"{universalMetadata.Title} (epub2)"),
                        ExportTargets = exportTargets,
                        Tags = tags,
                    },
                    MetadataSearchFields = sharedSearchFields.AddRange(epub2SearchFields),
                };
            }
        }
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
        yield return new(MediaType.Application.Epub_Zip, true, true);
        IEpubProject project = await _projectLoader.LoadFromDirectoryAsync(projectDirectory, cancellationToken).ConfigureAwait(false);
        if (project.CoverFile is not null)
        {
            string? coverMediaType = _mediaTypeFileExtensionsMapping.GetMediaType(project.CoverFile.Extension);
            if (coverMediaType is not null)
            {
                yield return new(coverMediaType, true, false);
            }
        }
    }
}
