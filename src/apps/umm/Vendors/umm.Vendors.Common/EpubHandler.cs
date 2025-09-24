using Epubs;
using FileStorage;
using Images;
using MediaTypes;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using umm.Library;
using umm.Storages.Metadata;

namespace umm.Vendors.Common;

public sealed class EpubHandler
{
    private const string EpubMetadataOverrideKey = "epub_override";
    private const string EpubDirectoryName = "epub";
    private const string CoverOverrideFileName = "cover";
    private static readonly CompressionLevel Compression = CompressionLevel.SmallestSize;
    private static readonly ImmutableArray<string> CoverOverrideExtensions = [".jpg", ".png", ".webp"];
    private static readonly ImmutableArray<string> CoverMediaTypes = [MediaType.Image.Jpeg, MediaType.Image.Png, MediaType.Image.Webp];

    private readonly IEpubHandlerStrategy _strategy;

    public EpubHandler(IEpubHandlerStrategy strategy)
    {
        _strategy = strategy;
    }

    public async IAsyncEnumerable<MediaExportTarget> EnumerateExportTargetsAsync(string contentId, string partId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!await ContainsEpubAsync(contentId, cancellationToken).ConfigureAwait(false) || partId.Length != 0) yield break;
        yield return new(MediaType.Application.Epub_Zip, true, true);
        if (await ContainsCoverAsync(contentId, cancellationToken).ConfigureAwait(false))
        {
            foreach (string mediaType in CoverMediaTypes)
            {
                yield return new(mediaType, true, false);
            }
        }
        if (await ContainsPrePaginatedEpubAsync(contentId, cancellationToken).ConfigureAwait(false))
        {
            yield return new(MediaType.Application.Vnd.Comicbook_Zip, true, true);
        }
    }

    public async Task ExportAsync(string contentId, string partId, string mediaType, Stream stream, CancellationToken cancellationToken)
    {
        if (partId.Length != 0) throw new InvalidOperationException($"{_strategy.VendorContext.VendorId} - Unsupported PartId {partId}.");
        if (mediaType == MediaType.Application.Epub_Zip)
        {
            await ExportEpubAsync(contentId, stream, cancellationToken).ConfigureAwait(false);
            return;
        }
        if (mediaType == MediaType.Application.Vnd.Comicbook_Zip)
        {
            await ExportCbzAsync(contentId, stream, cancellationToken).ConfigureAwait(false);
            return;
        }
        if (await ContainsCoverAsync(contentId, cancellationToken).ConfigureAwait(false))
        {
            if (CoverMediaTypes.Contains(mediaType))
            {
                await ExportCoverAsync(contentId, mediaType, stream, cancellationToken).ConfigureAwait(false);
                return;
            }
        }
        throw new InvalidOperationException($"{_strategy.VendorContext.VendorId} - Unsupported MediaType {mediaType} for file export.");
    }

    public async Task ExportAsync(string contentId, string partId, string mediaType, IDirectory directory, CancellationToken cancellationToken)
    {
        if (partId.Length != 0) throw new InvalidOperationException($"{_strategy.VendorContext.VendorId} - Unsupported PartId {partId}.");
        await directory.EnsureIsEmptyAsync(cancellationToken).ConfigureAwait(false);
        if (mediaType == MediaType.Application.Epub_Zip)
        {
            await ExportEpubAsync(contentId, directory, cancellationToken).ConfigureAwait(false);
            return;
        }
        if (mediaType == MediaType.Application.Vnd.Comicbook_Zip)
        {
            await ExportCbzAsync(contentId, directory, cancellationToken).ConfigureAwait(false);
            return;
        }
        throw new InvalidOperationException($"{_strategy.VendorContext.VendorId} - Unsupported MediaType {mediaType} for directory export.");
    }

    public async Task<IDirectory> GetEpubDirectoryAsync(string contentId, CancellationToken cancellationToken)
    {
        IDirectory contentDirectory = await _strategy.VendorContext.BlobStorage.GetStorageContainerAsync(_strategy.VendorContext.VendorId, contentId, cancellationToken).ConfigureAwait(false);
        return contentDirectory.GetDirectory(EpubDirectoryName);
    }

    public async Task<IEpubMetadata> GetEpubMetadataAsync(string contentId, CancellationToken cancellationToken)
    {
        EpubContainer container = await GetContainerAsync(contentId, cancellationToken).ConfigureAwait(false);
        int version = await container.GetVersionAsync(cancellationToken).ConfigureAwait(false);
        _strategy.VendorContext.Logger.LogRegeneratingEpubMetadata(_strategy.VendorContext.VendorId, contentId, version);
        IEpubMetadata metadata = await container.GetMetadataAsync(cancellationToken).ConfigureAwait(false);
        await ModifyMetadataAsync(contentId, version, container, metadata, cancellationToken).ConfigureAwait(false);
        return metadata;
    }

    public ISinglePartSearchEntryEnumerationStrategy<EpubMetadataAdapter> GetEnumerationStrategy()
    {
        return new EpubHandlerBasicEnumerationStrategy(_strategy.VendorContext, this);
    }

    public ISinglePartSearchEntryEnumerationStrategy<TMetadata> GetEnumerationStrategy<TMetadata>(string metadataKey)
        where TMetadata : ISearchableMetadata, IUniversalizableMediaMetadata, ISerializableMetadata<TMetadata>
    {
        return new EpubHandlerEnumerationStrategy<TMetadata>(_strategy.VendorContext, this, metadataKey);
    }

    private async Task ModifyMetadataAsync(string contentId, int version, EpubContainer container, IEpubMetadata metadata, CancellationToken cancellationToken)
    {
        _strategy.VendorContext.Logger.LogModifyingEpubMetadata(_strategy.VendorContext.VendorId, contentId, version);
        if (_strategy.ModifyMetadataAsync is not null)
        {
            IDirectory epubDirectory = await GetEpubDirectoryAsync(contentId, cancellationToken).ConfigureAwait(false);
            foreach (MetadataPropertyChange metadataPropertyChange in await _strategy.ModifyMetadataAsync(container, contentId, metadata, cancellationToken).ConfigureAwait(false))
            {
                _strategy.VendorContext.Logger.LogMetadataChanged(_strategy.VendorContext.VendorId, contentId, metadataPropertyChange);
            }
        }
        if (_strategy.AllowEpubMetadataOverrides && await _strategy.VendorContext.MetadataStorage.ContainsAsync(_strategy.VendorContext.VendorId, contentId, EpubMetadataOverrideKey, cancellationToken).ConfigureAwait(false))
        {
            BasicEpubMetadataOverride epubMetadataOverride = await _strategy.VendorContext.MetadataStorage.GetAsync<BasicEpubMetadataOverride>(_strategy.VendorContext.VendorId, contentId, EpubMetadataOverrideKey, cancellationToken).ConfigureAwait(false);
            foreach (MetadataPropertyChange metadataPropertyChange in epubMetadataOverride.WriteTo(metadata))
            {
                _strategy.VendorContext.Logger.LogMetadataChanged(_strategy.VendorContext.VendorId, contentId, metadataPropertyChange);
            }
        }
    }

    private async Task<EpubContainer> GetContainerAsync(string contentId, CancellationToken cancellationToken)
    {
        IDirectory epubDirectory = await GetEpubDirectoryAsync(contentId, cancellationToken).ConfigureAwait(false);
        EpubContainer container = new(epubDirectory);
        return container;
    }

    private async Task<bool> ContainsEpubAsync(string contentId, CancellationToken cancellationToken)
    {
        // TODO LINQ
        bool containsEpub = await _strategy.ContainsEpubAsync(contentId, cancellationToken).ConfigureAwait(false)
            ?? await (await GetEpubDirectoryAsync(contentId, cancellationToken).ConfigureAwait(false)).ExistsAsync(cancellationToken).ConfigureAwait(false);
        if (!containsEpub)
        {
            _strategy.VendorContext.Logger.LogNoEpub(_strategy.VendorContext.VendorId, contentId);
        }
        return containsEpub;
    }

    private async Task<bool> ContainsCoverAsync(string contentId, CancellationToken cancellationToken)
    {
        EpubContainer container = await GetContainerAsync(contentId, cancellationToken).ConfigureAwait(false);
        if (await container.GetCoverAsync(cancellationToken).ConfigureAwait(false) is not null)
        {
            return true;
        }
        if (await GetCoverOverrideAsync(contentId, cancellationToken).ConfigureAwait(false) is not null)
        {
            return true;
        }
        return false;
    }

    private async Task<bool> ContainsPrePaginatedEpubAsync(string contentId, CancellationToken cancellationToken)
    {
        if (!await ContainsEpubAsync(contentId, cancellationToken).ConfigureAwait(false)) return false;
        EpubContainer container = await GetContainerAsync(contentId, cancellationToken).ConfigureAwait(false);
        return await container.IsPrePaginatedAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task ExportEpubAsync(string contentId, Stream stream, CancellationToken cancellationToken)
    {
        _strategy.VendorContext.Logger.LogExportingFile(_strategy.VendorContext.VendorId, contentId, string.Empty, MediaType.Application.Epub_Zip);
        EpubPackager packager = await GetPackagerAsync(contentId, cancellationToken);
        await packager.PackageAsync(stream, Compression, cancellationToken).ConfigureAwait(false);
    }

    private async Task ExportEpubAsync(string contentId, IDirectory directory, CancellationToken cancellationToken)
    {
        _strategy.VendorContext.Logger.LogExportingDirectory(_strategy.VendorContext.VendorId, contentId, string.Empty, MediaType.Application.Epub_Zip);
        EpubPackager packager = await GetPackagerAsync(contentId, cancellationToken);
        await packager.PackageAsync(directory, cancellationToken).ConfigureAwait(false);
    }

    private async Task ExportCbzAsync(string contentId, Stream stream, CancellationToken cancellationToken)
    {
        _strategy.VendorContext.Logger.LogExportingFile(_strategy.VendorContext.VendorId, contentId, string.Empty, MediaType.Application.Vnd.Comicbook_Zip);
        EpubToCbzConverter cbzConverter = await GetCbzConverterAsync(contentId, cancellationToken).ConfigureAwait(false);
        await cbzConverter.WriteAsync(stream, Compression, cancellationToken).ConfigureAwait(false);
    }

    private async Task ExportCbzAsync(string contentId, IDirectory directory, CancellationToken cancellationToken)
    {
        _strategy.VendorContext.Logger.LogExportingDirectory(_strategy.VendorContext.VendorId, contentId, string.Empty, MediaType.Application.Vnd.Comicbook_Zip);
        EpubToCbzConverter cbzConverter = await GetCbzConverterAsync(contentId, cancellationToken).ConfigureAwait(false);
        await cbzConverter.WriteAsync(directory, cancellationToken).ConfigureAwait(false);
    }

    private async Task ExportCoverAsync(string contentId, string mediaType, Stream stream, CancellationToken cancellationToken)
    {
        _strategy.VendorContext.Logger.LogExportingFile(_strategy.VendorContext.VendorId, contentId, string.Empty, mediaType);
        IFile? coverOverrideFile = await GetCoverOverrideAsync(contentId, cancellationToken).ConfigureAwait(false);
        if (coverOverrideFile is not null)
        {
            await ExportCoverOverrideAsync(coverOverrideFile, mediaType, stream, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            EpubContainer container = await GetContainerAsync(contentId, cancellationToken).ConfigureAwait(false);
            EpubCover cover = await container.GetCoverAsync(cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"No cover for {_strategy.VendorContext.VendorId}.{contentId}.");
            Stream coverStream = await cover.OpenReadAsync(cancellationToken).ConfigureAwait(false);
            await using (coverStream.ConfigureAwait(false))
            {
                await ExportImageAsync(coverStream, cover.MediaType, stream, mediaType, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task ExportImageAsync(Stream sourceStream, string sourceMediaType, Stream destinationStream, string destinationMediaType, CancellationToken cancellationToken)
    {
        if (sourceMediaType == destinationMediaType)
        {
            await sourceStream.CopyToAsync(destinationStream, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            IImage image = await _strategy.ImageLoader.LoadImageAsync(sourceStream, cancellationToken).ConfigureAwait(false);
            await image.SaveToAsync(destinationStream, ImageFormatParser.FromMediaType(destinationMediaType), cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ExportCoverOverrideAsync(IFile coverOverrideFile, string coverMediaType, Stream outputStream, CancellationToken cancellationToken)
    {
        string coverOverrideExtension = coverOverrideFile.Extension;
        string coverOverrideMediaType = _strategy.MediaTypeFileExtensionsMapping.GetMediaType(coverOverrideExtension)
            ?? throw new InvalidOperationException($"Could not get media type for cover override extension {coverOverrideExtension}.");
        Stream coverOverrideStream = await coverOverrideFile.OpenReadAsync(cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable configuredCoverOverrideStream = coverOverrideStream.ConfigureAwait(false);
        await ExportImageAsync(coverOverrideStream, coverOverrideMediaType, outputStream, coverMediaType, cancellationToken).ConfigureAwait(false);
    }

    private async Task<EpubPackager> GetPackagerAsync(string contentId, CancellationToken cancellationToken)
    {
        EpubContainer container = await GetContainerAsync(contentId, cancellationToken).ConfigureAwait(false);
        EpubPackager packager = new(container, _strategy.MediaTypeFileExtensionsMapping);

        IFile? coverOverrideFile = await GetCoverOverrideAsync(contentId, cancellationToken).ConfigureAwait(false);
        if (coverOverrideFile is not null)
        {
            string coverOverrideExtension = coverOverrideFile.Extension;
            string newCoverMediaType = MediaType.Image.Jpeg;
            packager.WithCoverHandler(newCoverMediaType, (epubCover, outputStream, cancellationToken) =>
            {
                string coverMediaType = epubCover?.MediaType ?? newCoverMediaType;
                return ExportCoverOverrideAsync(coverOverrideFile, coverMediaType, outputStream, cancellationToken);
            });
        }

        if (_strategy.AllowEpubMetadataOverrides || _strategy.ModifyMetadataAsync is not null)
        {
            int version = await container.GetVersionAsync(cancellationToken).ConfigureAwait(false);
            packager.WithMetadataHandler((metadata, metadataCancellationToken)
                => ModifyMetadataAsync(contentId, version, container, metadata, metadataCancellationToken));
        }

        if (_strategy.HandleXhtml is not null)
        {
            packager.WithXhtmlHandler(_strategy.HandleXhtml);
        }

        IReadOnlyDictionary<string, string?>? fileNameOverrides = await _strategy.GetFileNameOverridesAsync(container, contentId, cancellationToken).ConfigureAwait(false);
        if (fileNameOverrides is not null)
        {
            packager.WithFileNameOverrides(fileNameOverrides);
        }

        return packager;
    }

    private async Task<EpubToCbzConverter> GetCbzConverterAsync(string contentId, CancellationToken cancellationToken)
    {
        EpubContainer container = await GetContainerAsync(contentId, cancellationToken).ConfigureAwait(false);
        EpubToCbzConverter cbzConverter = new(container);
        return cbzConverter;
    }

    private async Task<IFile?> GetCoverOverrideAsync(string contentId, CancellationToken cancellationToken)
    {
        if (!_strategy.AllowCoverOverride) return null;
        IDirectory contentDirectory = await _strategy.VendorContext.BlobStorage.GetStorageContainerAsync(_strategy.VendorContext.VendorId, contentId, cancellationToken).ConfigureAwait(false);
        // TODO LINQ
        IFile? coverOverrideFile = await contentDirectory
                .EnumerateFilesAsync(cancellationToken)
                .FirstOrDefaultAsync(f => f.Stem == $".{CoverOverrideFileName}" && CoverOverrideExtensions.Contains(f.Extension), cancellationToken)
                .ConfigureAwait(false);
        return coverOverrideFile;
    }
}
