using Epubs;
using FileStorage;
using FileStorage.Zip;
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
using System.Xml.Linq;
using umm.Library;
using umm.Storages.Urls;
using umm.Vendors.Common;
using Utils;

namespace umm.Vendors.ComicBookArchive;

internal sealed class ContentHandler
{
    private const string MetadataKey = "";
    private const string PartIdSeparator = "-";
    private const string ContentsDirectoryName = "contents";
    private const string EpubExportId = "epub";
    private const string CbzExportId = "cbz";
    private const string CoverExportId = "cover";

    private static readonly CompressionLevel Compression = CompressionLevel.SmallestSize;

    private readonly MediaVendorContext _vendorContext;
    private readonly IUrlsStorage _urlsStorage;
    private readonly IImageLoader _imageLoader;
    private readonly IMediaTypeFileExtensionsMapping _mediaTypeFileExtensionsMapping;

    public ContentHandler(MediaVendorContext vendorContext, IUrlsStorage urlsStorage,
        IImageLoader imageLoader, IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping)
    {
        _vendorContext = vendorContext;
        _urlsStorage = urlsStorage;
        _imageLoader = imageLoader;
        _mediaTypeFileExtensionsMapping = mediaTypeFileExtensionsMapping;
    }

    public IAsyncEnumerable<SearchableMediaEntry> EnumerateAsync(CancellationToken cancellationToken)
        => _vendorContext.MetadataStorage.EnumerateContentAsync(cancellationToken)
            .Where(id => id.VendorId == _vendorContext.VendorId)
            .SelectMany(id => EnumerateEntriesAsync(id.ContentId, cancellationToken));

    public IAsyncEnumerable<SearchableMediaEntry> EnumerateAsync(string contentId, CancellationToken cancellationToken)
        => EnumerateEntriesAsync(contentId, cancellationToken);

    public Task<SearchableMediaEntry?> GetEntryAsync(string contentId, string partId, CancellationToken cancellationToken)
        => EnumerateEntriesAsync(contentId, cancellationToken).FirstOrDefaultAsync(e => e.MediaEntry.Id.PartId == partId, cancellationToken).AsTask();

    public async Task<EpubMetadataOverrideMetadataAdapter> GetMetadataAsync(string contentId, CancellationToken cancellationToken)
        => new(
            contentId,
            await _vendorContext.MetadataStorage.GetAsync<BasicEpubMetadataOverride>(
                new(_vendorContext.VendorId, contentId),
                MetadataKey, cancellationToken).ConfigureAwait(false));

    public Task<ImmutableSortedSet<string>> GetTagsAsync(string contentId, CancellationToken cancellationToken)
        => _vendorContext.TagsStorage.GetAsync(new(_vendorContext.VendorId, contentId), cancellationToken);

    public Task<ImmutableArray<string>> GetUrlsAsync(string contentId, CancellationToken cancellationToken)
        => _urlsStorage.GetAsync(new(_vendorContext.VendorId, contentId, string.Empty), cancellationToken);

    public async Task ExportAsync(string contentId, string partId, string exportId, Stream stream, CancellationToken cancellationToken)
    {
        if (exportId == EpubExportId)
        {
            _vendorContext.Logger.LogExportingFile(_vendorContext.VendorId, contentId, string.Empty, exportId);
            await ExportEpubAsync(contentId, partId, stream, cancellationToken).ConfigureAwait(false);
            return;
        }
        if (exportId == CbzExportId)
        {
            _vendorContext.Logger.LogExportingFile(_vendorContext.VendorId, contentId, partId, exportId);
            await ExportCbzAsync(contentId, partId, stream, cancellationToken).ConfigureAwait(false);
            return;
        }
        if (exportId == CoverExportId)
        {
            _vendorContext.Logger.LogExportingFile(_vendorContext.VendorId, contentId, partId, exportId);
            await ExportCoverAsync(contentId, partId, stream, cancellationToken).ConfigureAwait(false);
            return;
        }
        throw new InvalidOperationException($"{_vendorContext.VendorId} - Unsupported ExportId {exportId} for file export.");
    }

    public async Task ExportAsync(string contentId, string partId, string exportId, IDirectory directory, CancellationToken cancellationToken)
    {
        if (exportId == EpubExportId)
        {
            _vendorContext.Logger.LogExportingDirectory(_vendorContext.VendorId, contentId, string.Empty, exportId);
            await ExportEpubAsync(contentId, partId, directory, cancellationToken).ConfigureAwait(false);
            return;
        }
        if (exportId == CbzExportId)
        {
            _vendorContext.Logger.LogExportingDirectory(_vendorContext.VendorId, contentId, string.Empty, exportId);
            await ExportCbzAsync(contentId, partId, directory, cancellationToken).ConfigureAwait(false);
            return;
        }
        throw new InvalidOperationException($"{_vendorContext.VendorId} - Unsupported ExportId {exportId} for directory export.");
    }

    private async IAsyncEnumerable<SearchableMediaEntry> EnumerateEntriesAsync(string contentId, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        EpubMetadataOverrideMetadataAdapter metadata = await GetMetadataAsync(contentId, cancellationToken).ConfigureAwait(false);
        string title = !string.IsNullOrWhiteSpace(metadata.EpubMetadataOverride.Title)
            ? metadata.EpubMetadataOverride.Title
            : new MediaMainId(_vendorContext.VendorId, contentId).ToCombinedString();
        ComicBookArchiveStorageNode storageNode = await GetStorageNodeAsync(contentId, title, cancellationToken).ConfigureAwait(false);
        UniversalMediaMetadata universalMetadata = metadata.Universalize();
        ImmutableSortedSet<string> tags = await GetTagsAsync(contentId, cancellationToken).ConfigureAwait(false);
        ImmutableArray<string> urls = await GetUrlsAsync(contentId, cancellationToken).ConfigureAwait(false);
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
                Values = [_vendorContext.VendorId],
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
            new()
            {
                Aliases = ["depth"],
                Values = ["0"],
                ExactMatch = true,
            },
        ];
        yield return new()
        {
            MediaEntry = new()
            {
                Id = new(_vendorContext.VendorId, contentId, string.Empty),
                Metadata = universalMetadata,
                ExportTargets = GetExportTargets(storageNode),
                Tags = tags,
                Urls = urls,
            },
            MetadataSearchFields = searchFields,
        };
        Stack<ImmutableArray<int>> coordinatesStack = [];
        for (int coordinate = storageNode.ChildNodes.Length; coordinate > 0; coordinate--)
        {
            coordinatesStack.Push([coordinate]);
        }
        while (coordinatesStack.Count > 0)
        {
            ImmutableArray<int> coordinates = coordinatesStack.Pop();
            string partId = string.Join(PartIdSeparator, coordinates);
            ComicBookArchiveStorageNode resolvedStorageNode = storageNode.Resolve(coordinates)
                ?? throw new InvalidOperationException($"Could not resolve coordinates for contentId {contentId}.");
            string resolvedFullTitle = storageNode.ResolveFullTitle(coordinates);
            ImmutableArray<MetadataSearchField>.Builder searchFieldsBuilder = searchFields.ToBuilder();
            (int partIdSearchFieldIndex, MetadataSearchField partIdSearchField) = searchFieldsBuilder
                .Index()
                .First(t => t.Item.Aliases.Any(a => a == nameof(MediaFullId.PartId)));
            searchFieldsBuilder[partIdSearchFieldIndex] = new()
            {
                Aliases = partIdSearchField.Aliases,
                Values = [partId],
                ExactMatch = partIdSearchField.ExactMatch,
            };
            (int titleSearchFieldIndex, MetadataSearchField titleSearchField) = searchFieldsBuilder
                .Index()
                .First(t => t.Item.Aliases.Any(a => a.Equals("title", StringComparison.OrdinalIgnoreCase)));
            searchFieldsBuilder[titleSearchFieldIndex] = new()
            {
                Aliases = titleSearchField.Aliases,
                Values = [resolvedFullTitle],
                ExactMatch = titleSearchField.ExactMatch,
            };
            (int depthSearchFieldIndex, MetadataSearchField depthSearchField) = searchFieldsBuilder
                .Index()
                .First(t => t.Item.Aliases.Any(a => a.Equals("depth", StringComparison.OrdinalIgnoreCase)));
            searchFieldsBuilder[depthSearchFieldIndex] = new()
            {
                Aliases = depthSearchField.Aliases,
                Values = [coordinates.Length.ToString() ?? string.Empty],
                ExactMatch = depthSearchField.ExactMatch,
            };
            yield return new()
            {
                MediaEntry = new()
                {
                    Id = new(_vendorContext.VendorId, contentId, partId),
                    Metadata = universalMetadata.With(title: resolvedFullTitle),
                    ExportTargets = GetExportTargets(resolvedStorageNode),
                    Tags = tags,
                    Urls = urls,
                },
                MetadataSearchFields = searchFieldsBuilder.ToImmutable(),
            };
            for (int coordinate = resolvedStorageNode.ChildNodes.Length; coordinate > 0; coordinate--)
            {
                coordinatesStack.Push(coordinates.Add(coordinate));
            }
        }
    }

    private ImmutableArray<MediaExportTarget> GetExportTargets(ComicBookArchiveStorageNode storageNode)
    {
        ImmutableArray<IFile> pageFiles = storageNode.GetAllPageFiles();
        if (pageFiles.Length == 0)
        {
            return [];
        }
        string coverMediaType = _mediaTypeFileExtensionsMapping.GetMediaType(pageFiles[0].Extension, MediaType.Application.OctetStream);
        return [
            new()
            {
                ExportId = EpubExportId,
                MediaType = MediaType.Application.Epub_Zip,
                SupportsFile = true,
                SupportsDirectory = true,
                MediaFormats = [MediaFormat.Comic, MediaFormat.Ebook],
            },
            new()
            {
                ExportId = CbzExportId,
                MediaType = MediaType.Application.Vnd.Comicbook_Zip,
                SupportsFile = true,
                SupportsDirectory = true,
                MediaFormats = [MediaFormat.Comic, MediaFormat.Ebook],
            },
            new()
            {
                ExportId = CoverExportId,
                MediaType = coverMediaType,
                SupportsFile = true,
                SupportsDirectory = false,
                MediaFormats = [MediaFormat.Artwork],
            },
        ];
    }

    private static bool TryParsePartId(string partId, out ImmutableArray<int> coordinates)
    {
        coordinates = default;
        if (partId.Length == 0)
        {
            coordinates = [];
            return true;
        }
        ImmutableArray<int>.Builder builder = ImmutableArray.CreateBuilder<int>();
        foreach (string splitPartIdPart in partId.Split(PartIdSeparator))
        {
            if (string.IsNullOrWhiteSpace(splitPartIdPart)
                || !splitPartIdPart.All(char.IsAsciiDigit)
                || splitPartIdPart[0] == '0')
            {
                return false;
            }
            builder.Add(int.Parse(splitPartIdPart));
        }
        coordinates = builder.ToImmutable();
        return true;
    }

    private async Task<ComicBookArchiveStorageNode> GetStorageNodeAsync(string contentId, string title, CancellationToken cancellationToken)
    {
        IDirectory storageContainer = await _vendorContext.BlobStorage.GetStorageContainerAsync(new(_vendorContext.VendorId, contentId), cancellationToken).ConfigureAwait(false);
        IDirectory contentsDirectory = storageContainer.GetDirectory(ContentsDirectoryName);
        return await ComicBookArchiveStorageNode.FromDirectoryAsync(contentsDirectory, title, cancellationToken).ConfigureAwait(false);
    }

    private async Task ExportEpubAsync(string contentId, string partId, Stream stream, CancellationToken cancellationToken)
    {
        if (!TryParsePartId(partId, out ImmutableArray<int> coordinates))
        {
            throw new InvalidOperationException($"Invalid partId {partId} for contentId {contentId}.");
        }
        EpubMetadataOverrideMetadataAdapter metadata = await GetMetadataAsync(contentId, cancellationToken).ConfigureAwait(false);
        string title = !string.IsNullOrWhiteSpace(metadata.EpubMetadataOverride.Title)
            ? metadata.EpubMetadataOverride.Title
            : new MediaFullId(_vendorContext.VendorId, contentId, partId).ToCombinedString();
        ComicBookArchiveStorageNode storageNode = await GetStorageNodeAsync(contentId, title, cancellationToken).ConfigureAwait(false);
        ComicBookArchiveStorageNode resolvedStorageNode = storageNode.Resolve(coordinates)
            ?? throw new InvalidOperationException($"Could not resolve partId {partId} coordinates for contentId {contentId}.");
        string fullTitle = storageNode.ResolveFullTitle(coordinates);
        EpubWriterOptions epubWriterOptions = new()
        {
            Version = EpubVersion.Epub3,
            Compression = Compression,
            Modified = metadata.Timestamp,
        };
        EpubWriter epubWriter = await EpubWriter.CreateAsync(stream, epubWriterOptions, _mediaTypeFileExtensionsMapping, cancellationToken).ConfigureAwait(false);
        await using (epubWriter.ConfigureAwait(false))
        {
            await WriteEpubAsync(epubWriter, metadata, resolvedStorageNode, partId, fullTitle, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ExportEpubAsync(string contentId, string partId, IDirectory directory, CancellationToken cancellationToken)
    {
        if (!TryParsePartId(partId, out ImmutableArray<int> coordinates))
        {
            throw new InvalidOperationException($"Invalid partId {partId} for contentId {contentId}.");
        }
        EpubMetadataOverrideMetadataAdapter metadata = await GetMetadataAsync(contentId, cancellationToken).ConfigureAwait(false);
        string title = !string.IsNullOrWhiteSpace(metadata.EpubMetadataOverride.Title)
            ? metadata.EpubMetadataOverride.Title
            : new MediaFullId(_vendorContext.VendorId, contentId, partId).ToCombinedString();
        ComicBookArchiveStorageNode storageNode = await GetStorageNodeAsync(contentId, title, cancellationToken).ConfigureAwait(false);
        ComicBookArchiveStorageNode resolvedStorageNode = storageNode.Resolve(coordinates)
            ?? throw new InvalidOperationException($"Could not resolve partId {partId} coordinates for contentId {contentId}.");
        string fullTitle = storageNode.ResolveFullTitle(coordinates);
        EpubWriterOptions epubWriterOptions = new()
        {
            Version = EpubVersion.Epub3,
            Compression = Compression,
            Modified = metadata.Timestamp,
        };
        EpubWriter epubWriter = await EpubWriter.CreateAsync(directory, epubWriterOptions, _mediaTypeFileExtensionsMapping, cancellationToken).ConfigureAwait(false);
        await using (epubWriter.ConfigureAwait(false))
        {
            await WriteEpubAsync(epubWriter, metadata, resolvedStorageNode, partId, fullTitle, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task WriteEpubAsync(EpubWriter epubWriter,
        EpubMetadataOverrideMetadataAdapter metadata, ComicBookArchiveStorageNode storageNode,
        string partId,
        string fullTitle,
        CancellationToken cancellationToken)
    {
        WriteEpubMetadata(epubWriter, metadata, partId, fullTitle);
        await WriteEpubCoverAsync(epubWriter, storageNode, cancellationToken).ConfigureAwait(false);
        EpubNavItem navItem = await WriteEpubPagesAsync(epubWriter, storageNode, [], [], cancellationToken).ConfigureAwait(false);
        epubWriter.AddToc(new List<EpubNavItem>() { navItem, }, false);
    }

    private void WriteEpubMetadata(EpubWriter epubWriter, EpubMetadataOverrideMetadataAdapter metadata, string partId, string fullTitle)
    {
        string title = !string.IsNullOrWhiteSpace(metadata.EpubMetadataOverride.Title)
            ? fullTitle
            : new MediaFullId(_vendorContext.VendorId, metadata.ContentId, partId).ToCombinedString();
        epubWriter.Title = title;
        if (metadata.EpubMetadataOverride.Creators.Length > 0)
        {
            epubWriter.Creators = metadata.EpubMetadataOverride.Creators;
        }
        if (!string.IsNullOrWhiteSpace(metadata.EpubMetadataOverride.Description))
        {
            epubWriter.Description = metadata.EpubMetadataOverride.Description;
        }
        if (metadata.EpubMetadataOverride.Series is not null)
        {
            epubWriter.Series = metadata.EpubMetadataOverride.Series;
        }
        DateTimeOffset? date = metadata.EpubMetadataOverride.Date.ToDateTimeOffsetNullable();
        if (date is DateTimeOffset validDate)
        {
            epubWriter.Date = validDate;
        }
        if (metadata.EpubMetadataOverride.Direction is EpubDirection direction)
        {
            epubWriter.Direction = direction;
        }
        epubWriter.PrePaginated = true;
    }

    private static async Task WriteEpubCoverAsync(EpubWriter epubWriter, ComicBookArchiveStorageNode storageNode, CancellationToken cancellationToken)
    {
        ImmutableArray<IFile> pageFiles = storageNode.GetAllPageFiles();
        IFile coverFile = pageFiles[0];
        Stream destinationCoverStream = await epubWriter.CreateRasterCoverAsync(coverFile.Extension, false, cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable configuredDestinationCoverStream = destinationCoverStream.ConfigureAwait(false);
        Stream sourceCoverStream = await coverFile.OpenReadAsync(cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable configuredSourceCoverStream = sourceCoverStream.ConfigureAwait(false);
        await sourceCoverStream.CopyToAsync(destinationCoverStream, cancellationToken).ConfigureAwait(false);
    }

    private async Task<EpubNavItem> WriteEpubPagesAsync(EpubWriter epubWriter, ComicBookArchiveStorageNode storageNode,
        ImmutableArray<int> coordinates, ImmutableArray<string> titles,
        CancellationToken cancellationToken)
    {
        for (int i = 0; i < storageNode.PageFiles.Length; i++)
        {
            IFile pageFile = storageNode.PageFiles[i];
            await WriteEpubPageAsync(epubWriter, pageFile, coordinates, i + 1, cancellationToken).ConfigureAwait(false);
        }
        List<EpubNavItem> childNavItems = [];
        ImmutableArray<string> currentTitles = titles.Add(storageNode.Title);
        for (int i = 0; i < storageNode.ChildNodes.Length; i++)
        {
            ComicBookArchiveStorageNode childNode = storageNode.ChildNodes[i];
            EpubNavItem childNavItem = await WriteEpubPagesAsync(epubWriter, childNode, coordinates.Add(i + 1), currentTitles, cancellationToken).ConfigureAwait(false);
            childNavItems.Add(childNavItem);
        }
        EpubNavItem navItem = new()
        {
            Text = string.Join(' ', currentTitles),
            Children = childNavItems,
        };
        if (storageNode.PageFiles.Length > 0)
        {
            navItem.Reference = string.Join('/', coordinates.Select(c => c.ToString()).Append("1.xhtml"));
        }
        else if (childNavItems.Count > 0)
        {
            navItem.Reference = childNavItems[0].Reference;
        }
        else
        {
            throw new InvalidOperationException($"Could not get reference for {navItem.Text}");
        }
        return navItem;
    }

    private async Task WriteEpubPageAsync(EpubWriter epubWriter, IFile pageFile,
        ImmutableArray<int> coordinates, int pageNumber,
        CancellationToken cancellationToken)
    {
        string imagePageName = $"{pageNumber}{pageFile.Extension}";
        string imageHref = string.Join('/', coordinates.Select(c => c.ToString()).Append(imagePageName));
        EpubResource imageResource = new()
        {
            Href = imageHref,
        };
        Stream pageStream = await pageFile.OpenReadAsync(cancellationToken).ConfigureAwait(false);
        await using (pageStream.ConfigureAwait(false))
        {
            await epubWriter.AddResourceAsync(pageStream, imageResource, cancellationToken).ConfigureAwait(false);
        }
        int width;
        int height;
        pageStream = await pageFile.OpenReadAsync(cancellationToken).ConfigureAwait(false);
        await using (pageStream.ConfigureAwait(false))
        {
            using IImage image = await _imageLoader.LoadImageAsync(pageStream, cancellationToken).ConfigureAwait(false);
            width = image.Width;
            height = image.Height;
        }
        XDocument pageXhtml = EpubFxl.CreateSingleImageXhtml(imagePageName, width, height);
        string xhtmlHref = string.Join('/', coordinates.Select(c => c.ToString()).Append($"{pageNumber}.xhtml"));
        EpubResource xhtmlResource = new()
        {
            Href = xhtmlHref,
        };
        Stream xhtmlStream = await epubWriter.CreateResourceAsync(xhtmlResource, cancellationToken).ConfigureAwait(false);
        await using (xhtmlStream.ConfigureAwait(false))
        {
            await EpubXml.SaveAsync(pageXhtml, xhtmlStream, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ExportCoverAsync(string contentId, string partId, Stream stream, CancellationToken cancellationToken)
    {
        if (!TryParsePartId(partId, out ImmutableArray<int> coordinates))
        {
            throw new InvalidOperationException($"Invalid partId {partId} for contentId {contentId}.");
        }
        EpubMetadataOverrideMetadataAdapter metadata = await GetMetadataAsync(contentId, cancellationToken).ConfigureAwait(false);
        string title = !string.IsNullOrWhiteSpace(metadata.EpubMetadataOverride.Title)
            ? metadata.EpubMetadataOverride.Title
            : new MediaFullId(_vendorContext.VendorId, contentId, partId).ToCombinedString();
        ComicBookArchiveStorageNode storageNode = await GetStorageNodeAsync(contentId, title, cancellationToken).ConfigureAwait(false);
        ComicBookArchiveStorageNode resolvedStorageNode = storageNode.Resolve(coordinates)
            ?? throw new InvalidOperationException($"Could not resolve partId {partId} coordinates for contentId {contentId}.");
        IFile coverFile = resolvedStorageNode.GetAllPageFiles()[0];
        Stream coverFileStream = await coverFile.OpenReadAsync(cancellationToken).ConfigureAwait(false);
        await using (coverFileStream.ConfigureAwait(false))
        {
            await coverFileStream.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task ExportCbzAsync(string contentId, string partId, Stream stream, CancellationToken cancellationToken)
    {
        if (!TryParsePartId(partId, out ImmutableArray<int> coordinates))
        {
            throw new InvalidOperationException($"Invalid partId {partId} for contentId {contentId}.");
        }
        EpubMetadataOverrideMetadataAdapter metadata = await GetMetadataAsync(contentId, cancellationToken).ConfigureAwait(false);
        string title = !string.IsNullOrWhiteSpace(metadata.EpubMetadataOverride.Title)
            ? metadata.EpubMetadataOverride.Title
            : new MediaFullId(_vendorContext.VendorId, contentId, partId).ToCombinedString();
        ComicBookArchiveStorageNode storageNode = await GetStorageNodeAsync(contentId, title, cancellationToken).ConfigureAwait(false);
        ComicBookArchiveStorageNode resolvedStorageNode = storageNode.Resolve(coordinates)
            ?? throw new InvalidOperationException($"Could not resolve partId {partId} coordinates for contentId {contentId}.");
        DateTimeOffset timestamp = metadata.Timestamp;
        await WriteCbzAsync(resolvedStorageNode, timestamp, stream, cancellationToken).ConfigureAwait(false);
    }

    private async Task ExportCbzAsync(string contentId, string partId, IDirectory directory, CancellationToken cancellationToken)
    {
        if (!TryParsePartId(partId, out ImmutableArray<int> coordinates))
        {
            throw new InvalidOperationException($"Invalid partId {partId} for contentId {contentId}.");
        }
        EpubMetadataOverrideMetadataAdapter metadata = await GetMetadataAsync(contentId, cancellationToken).ConfigureAwait(false);
        string title = !string.IsNullOrWhiteSpace(metadata.EpubMetadataOverride.Title)
            ? metadata.EpubMetadataOverride.Title
            : new MediaFullId(_vendorContext.VendorId, contentId, partId).ToCombinedString();
        ComicBookArchiveStorageNode storageNode = await GetStorageNodeAsync(contentId, title, cancellationToken).ConfigureAwait(false);
        ComicBookArchiveStorageNode resolvedStorageNode = storageNode.Resolve(coordinates)
            ?? throw new InvalidOperationException($"Could not resolve partId {partId} coordinates for contentId {contentId}.");
        await WriteCbzAsync(resolvedStorageNode, directory, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteCbzAsync(ComicBookArchiveStorageNode storageNode, DateTimeOffset timestamp, Stream outputStream, CancellationToken cancellationToken)
    {
        ZipFileStorageOptions options = new()
        {
            Mode = ZipArchiveMode.Create,
            FixedTimestamp = timestamp,
            Compression = Compression,
        };
        ZipFileStorage fileStorage = await ZipFileStorage.CreateAsync(outputStream, options, cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable configuredZipFileStorage = fileStorage.ConfigureAwait(false);
        IDirectory directory = fileStorage.GetDirectory();
        await WriteCbzAsync(storageNode, directory, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteCbzAsync(ComicBookArchiveStorageNode storageNode, IDirectory outputDirectory, CancellationToken cancellationToken)
    {
        ImmutableArray<IFile> imageFiles = storageNode.GetAllPageFiles();

        await outputDirectory.EnsureIsEmptyAsync(cancellationToken).ConfigureAwait(false);

        for (int i = 0; i < imageFiles.Length; i++)
        {
            IFile imageFile = imageFiles[i];
            IFile outputFile = outputDirectory.GetFile($"{(i + 1).ToPaddedString(imageFiles.Length)}{imageFile.Extension}");
            await imageFile.CopyToAsync(outputFile, cancellationToken).ConfigureAwait(false);
        }
    }
}
