using Epubs;
using FileStorage;
using Images;
using ImgProj.Covers;
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
using Utils;

namespace ImgProj.Exporting;

public sealed class Epub3Exporter : IExporter, IDirectoryExporter
{
    private static readonly CompressionLevel Compression = CompressionLevel.SmallestSize;

    private readonly ICoverResolver _coverResolver;
    private readonly IImageLoader _imageLoader;
    private readonly IMediaTypeFileExtensionsMapping _mediaTypeFileExtensionsMapping;

    public ExportFormat ExportFormat { get; } = ExportFormat.Epub3;

    public Epub3Exporter(ICoverResolver coverResolver, IImageLoader imageLoader, IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping)
    {
        _coverResolver = coverResolver;
        _imageLoader = imageLoader;
        _mediaTypeFileExtensionsMapping = mediaTypeFileExtensionsMapping;
    }

    public async Task ExportAsync(IImgProject project, Stream stream, ImmutableArray<int> coordinates, string? version, CancellationToken cancellationToken = default)
    {
        IImgProject subProject = project.GetSubProject(coordinates);
        IMetadataVersion metadata = subProject.MetadataVersions[version ?? subProject.MainVersion];
        DateTimeOffset timestamp = metadata.Timestamp ?? System.DateTimeOffset.MinValue;
        timestamp = timestamp.Clamp(ZipConstants.MinLastWriteTime, ZipConstants.MaxLastWriteTime);
        EpubWriterOptions epubWriterOptions = new()
        {
            Version = EpubVersion.Epub3,
            Modified = timestamp,
            Compression = Compression,
        };
        EpubWriter epubWriter = await EpubWriter.CreateAsync(stream, epubWriterOptions, _mediaTypeFileExtensionsMapping, cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable configuredEpubWriter = epubWriter.ConfigureAwait(false);
        await WriteAsync(epubWriter, project, coordinates, version, cancellationToken).ConfigureAwait(false);
    }

    public async Task ExportAsync(IImgProject project, IDirectory directory, ImmutableArray<int> coordinates, string? version, CancellationToken cancellationToken = default)
    {
        EpubWriterOptions epubWriterOptions = new()
        {
            Version = EpubVersion.Epub3,
        };
        EpubWriter epubWriter = await EpubWriter.CreateAsync(directory, epubWriterOptions, _mediaTypeFileExtensionsMapping, cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable configuredEpubWriter = epubWriter.ConfigureAwait(false);
        await WriteAsync(epubWriter, project, coordinates, version, cancellationToken).ConfigureAwait(false);
    }

    private async Task WriteAsync(EpubWriter epubWriter,
        IImgProject project,
        ImmutableArray<int> coordinates,
        string? version,
        CancellationToken cancellationToken)
    {
        IImgProject subProject = project.GetSubProject(coordinates);
        version ??= subProject.MainVersion;
        IMetadataVersion metadata = subProject.MetadataVersions[version];
        List<IPage> pages = [];
        using (IImage coverImage = await _coverResolver.GetCoverImageAsync(project, coordinates, version, cancellationToken).ConfigureAwait(false))
        {
            string coverMediaType = MediaType.Image.Jpeg;
            string coverExtension = _mediaTypeFileExtensionsMapping.GetFileExtension(coverMediaType)
                ?? throw new InvalidOperationException("Could not get cover extension.");
            Stream epubCoverStream = await epubWriter.CreateRasterCoverAsync(coverExtension, true, cancellationToken).ConfigureAwait(false);
            await using ConfiguredAsyncDisposable configuredEpubCoverStream = epubCoverStream.ConfigureAwait(false);
            await coverImage.SaveToAsync(epubCoverStream, ImageFormat.FromMediaType(coverMediaType), cancellationToken).ConfigureAwait(false);
        }
        EpubNavItem navItem = await TraverseAsync(subProject, coordinates, version, pages, epubWriter, cancellationToken).ConfigureAwait(false);
        epubWriter.Identifier = GetIdentifier(metadata);
        epubWriter.Title = MetadataFlattener.GetTitle(metadata.TitleParts);
        epubWriter.Languages = metadata.Languages;
        epubWriter.Creators = metadata.Creators.Select(c => new EpubCreator
        {
            Name = c.Key,
            Roles = c.Value,
        }).ToList();
        epubWriter.Date = metadata.Timestamp;
        epubWriter.PrePaginated = true;
        if (metadata.ReadingDirection == ReadingDirection.LTR)
        {
            epubWriter.Direction = EpubDirection.LeftToRight;
        }
        if (metadata.ReadingDirection == ReadingDirection.RTL)
        {
            epubWriter.Direction = EpubDirection.RightToLeft;
        }
        epubWriter.AddToc(new List<EpubNavItem>() { navItem, }, false);
    }

    private static string GetIdentifier(IMetadataVersion metadata)
    {
        DateTimeOffset timestamp = metadata.Timestamp ?? DateTimeOffset.MinValue;
        Guid namespaceGuid = Guid.CreateVersion7(timestamp);
        string data = MetadataFlattener.GetTitle(metadata.TitleParts);
        Guid guid = Guid.CreateVersion5(namespaceGuid, data);
        return guid.ToUniformResourceName();
    }

    private async Task<EpubNavItem> TraverseAsync(IImgProject project, ImmutableArray<int> coordinates, string version, ICollection<IPage> pages, EpubWriter epubWriter, CancellationToken cancellationToken)
    {
        string title = project.MetadataVersions[version].TitleParts.Count > 0
            ? project.MetadataVersions[version].TitleParts[^1]
            : $"No Title";
        EpubNavItem navItem = new()
        {
            Text = title,
        };
        List<EpubNavItem> children = [];
        int pageNumber = 1;
        await foreach (IPage page in project.EnumeratePagesAsync(version, false, cancellationToken).ConfigureAwait(false))
        {
            pages.Add(page);
            await SavePageAsync(epubWriter, coordinates, page, pageNumber, cancellationToken).ConfigureAwait(false);
            await SavePageXhtmlAsync(epubWriter, project, coordinates, page, pageNumber, cancellationToken).ConfigureAwait(false);
            pageNumber += 1;
        }
        for (int i = 0; i < project.ChildProjects.Count; i++)
        {
            EpubNavItem childNavItem = await TraverseAsync(project.ChildProjects[i], coordinates.Add(i + 1), version, pages, epubWriter, cancellationToken).ConfigureAwait(false);
            children.Add(childNavItem);
        }
        if (pageNumber > 1)
        {
            navItem.Reference = string.Join('/', coordinates.Select(c => c.ToString()).Append("1.xhtml"));
        }
        else if (children.Count > 0)
        {
            navItem.Reference = children[0].Reference;
        }
        else
        {
            throw new FileStorageException();
        }
        navItem.Children = children;
        return navItem;
    }

    private static async Task SavePageAsync(EpubWriter epubWriter, ImmutableArray<int> coordinates, IPage page, int pageNumber, CancellationToken cancellationToken)
    {
        string imageHref = string.Join('/', coordinates.Select(c => c.ToString()).Append($"{pageNumber}{page.Extension}"));
        EpubResource imageResource = new()
        {
            Href = imageHref,
        };
        Stream pageStream = await page.OpenReadAsync(cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable configuredPageStream = pageStream.ConfigureAwait(false);
        await epubWriter.AddResourceAsync(pageStream, imageResource, cancellationToken).ConfigureAwait(false);
    }

    private async Task SavePageXhtmlAsync(EpubWriter epubWriter, IImgProject project, ImmutableArray<int> coordinates, IPage page, int pageNumber, CancellationToken cancellationToken)
    {
        string xhtmlHref = string.Join('/', coordinates.Select(c => c.ToString()).Append($"{pageNumber}.xhtml"));
        EpubResource xhtmlResource = new()
        {
            Href = xhtmlHref,
        };
        List<string> spineProperties = [];
        foreach (IPageSpread pageSpread in project.MetadataVersions[page.Version].PageSpreads)
        {
            if (pageSpread.Left.Length == 1 && pageSpread.Left[0] == pageNumber)
            {
                spineProperties.Add("page-spread-left");
            }
            if (pageSpread.Right.Length == 1 && pageSpread.Right[0] == pageNumber)
            {
                spineProperties.Add("page-spread-right");
            }
        }
        if (spineProperties.Count > 0)
        {
            xhtmlResource.SpineProperties = spineProperties;
        }

        Stream xhtmlStream = await epubWriter.CreateResourceAsync(xhtmlResource, cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable configuredXhtmlStream = xhtmlStream.ConfigureAwait(false);
        Stream pageStream = await page.OpenReadAsync(cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable configuredPageStream = pageStream.ConfigureAwait(false);
        XDocument pageXhtml = await CreatePageXhtmlAsync(pageStream, $"{pageNumber}{page.Extension}", cancellationToken).ConfigureAwait(false);
        await EpubXml.SaveAsync(pageXhtml, xhtmlStream, cancellationToken).ConfigureAwait(false);
    }

    private async Task<XDocument> CreatePageXhtmlAsync(Stream pageStream, string src, CancellationToken cancellationToken)
    {
        using IImage image = await _imageLoader.LoadImageAsync(pageStream, cancellationToken).ConfigureAwait(false);
        return EpubFxl.CreateSingleImageXhtml(src, image.Width, image.Height);
    }
}
