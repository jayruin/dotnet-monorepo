using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Epubs;
using FileStorage;
using MediaTypes;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Utils;

namespace EpubProj;

internal sealed class EpubProject : IEpubProject
{
    private static readonly ImmutableArray<string> _contentDocumentMediaTypes = [
        MediaType.Application.Xhtml_Xml,
        MediaType.Text.Html,
        MediaType.Text.Markdown,
        MediaType.Text.Plain,
    ];
    private readonly IDirectory _projectDirectory;
    private readonly ImmutableArray<IEpubProjectNavItem> _navItems;
    private readonly IMediaTypeFileExtensionsMapping _mediaTypeFileExtensionsMapping;
    private readonly IHtmlParser _htmlParser;
    private readonly IMarkupFormatter _markupFormatter;
    private readonly EpubProjectConverter _converter;

    public IEpubProjectMetadata Metadata { get; }
    public IFile? CoverFile { get; }

    public EpubProject(IDirectory projectDirectory, IEpubProjectMetadata metadata, ImmutableArray<IEpubProjectNavItem> navItems, IFile? coverFile,
        IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping,
        IHtmlParser htmlParser, IImplementation domImplementation, IMarkupFormatter markupFormatter)
    {
        _projectDirectory = projectDirectory;
        Metadata = metadata;
        _navItems = navItems;
        CoverFile = coverFile;
        _mediaTypeFileExtensionsMapping = mediaTypeFileExtensionsMapping;
        _htmlParser = htmlParser;
        _markupFormatter = markupFormatter;
        _converter = new(mediaTypeFileExtensionsMapping, htmlParser, domImplementation);
    }

    public Task ExportEpub3Async(Stream stream, IReadOnlyCollection<IFile> globalFiles, CancellationToken cancellationToken = default)
        => ExportEpubAsync(stream, globalFiles, EpubVersion.Epub3, cancellationToken);

    public Task ExportEpub3Async(IDirectory directory, IReadOnlyCollection<IFile> globalFiles, CancellationToken cancellationToken = default)
        => ExportEpubAsync(directory, globalFiles, EpubVersion.Epub3, cancellationToken);

    public Task ExportEpub2Async(Stream stream, IReadOnlyCollection<IFile> globalFiles, CancellationToken cancellationToken = default)
        => ExportEpubAsync(stream, globalFiles, EpubVersion.Epub2, cancellationToken);

    public Task ExportEpub2Async(IDirectory directory, IReadOnlyCollection<IFile> globalFiles, CancellationToken cancellationToken = default)
        => ExportEpubAsync(directory, globalFiles, EpubVersion.Epub2, cancellationToken);

    private static bool IsScriptedXhtml(IDocument document)
        => document.QuerySelector<IHtmlScriptElement>("script") is not null;

    private EpubWriterOptions CreateEpubWriterOptions(EpubVersion epubVersion)
        => new()
        {
            Version = epubVersion,
            Modified = Metadata.Modified,
        };

    private async Task ExportEpubAsync(Stream stream, IReadOnlyCollection<IFile> globalFiles, EpubVersion epubVersion, CancellationToken cancellationToken)
    {
        EpubWriter epubWriter = await EpubWriter.CreateAsync(stream, CreateEpubWriterOptions(epubVersion), _mediaTypeFileExtensionsMapping, cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable configuredEpubWriter = epubWriter.ConfigureAwait(false);
        await WriteEpubAsync(epubWriter, globalFiles, epubVersion, cancellationToken);
    }

    private async Task ExportEpubAsync(IDirectory directory, IReadOnlyCollection<IFile> globalFiles, EpubVersion epubVersion, CancellationToken cancellationToken)
    {
        await directory.EnsureIsEmptyAsync(cancellationToken).ConfigureAwait(false);
        EpubWriter epubWriter = await EpubWriter.CreateAsync(directory, CreateEpubWriterOptions(epubVersion), _mediaTypeFileExtensionsMapping, cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable configuredEpubWriter = epubWriter.ConfigureAwait(false);
        await WriteEpubAsync(epubWriter, globalFiles, epubVersion, cancellationToken);
    }

    private async Task WriteEpubAsync(EpubWriter epubWriter, IReadOnlyCollection<IFile> globalFiles, EpubVersion epubVersion, CancellationToken cancellationToken)
    {
        WriteMetadata(epubWriter);
        await WriteCoverAsync(epubWriter, cancellationToken).ConfigureAwait(false);
        WriteToc(epubWriter);
        await WriteResourcesAsync(epubWriter, globalFiles, epubVersion, cancellationToken).ConfigureAwait(false);
    }

    private void WriteMetadata(EpubWriter epubWriter)
    {
        epubWriter.Title = Metadata.Title;
        epubWriter.Creators = Metadata.Creators
            .Select(ConvertCreator)
            .ToList();
        epubWriter.Description = Metadata.Description;
        epubWriter.Languages = Metadata.Languages;
        epubWriter.Direction = ConvertDirection(Metadata.Direction);
        epubWriter.Date = Metadata.Date.ToDateTimeOffsetNullable();
        epubWriter.Identifier = Metadata.Identifier;
        epubWriter.Series = ConvertSeries(Metadata.Series);

        static EpubCreator ConvertCreator(IEpubProjectCreator creator)
        {
            return new()
            {
                Name = creator.Name,
                Roles = creator.Roles,
            };
        }

        static EpubDirection ConvertDirection(EpubProjectDirection direction)
        {
            return direction switch
            {
                EpubProjectDirection.Ltr => EpubDirection.LeftToRight,
                EpubProjectDirection.Rtl => EpubDirection.RightToLeft,
                _ => EpubDirection.Default,
            };
        }

        static EpubSeries? ConvertSeries(IEpubProjectSeries? series)
        {
            if (series is null) return null;
            return new()
            {
                Name = series.Name,
                Index = series.Index,
            };
        }
    }

    private async Task WriteCoverAsync(EpubWriter epubWriter, CancellationToken cancellationToken)
    {
        if (CoverFile is null) return;
        Stream destinationStream = await epubWriter.CreateRasterCoverAsync(CoverFile.Extension, true, cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable configuredDestinationStream = destinationStream.ConfigureAwait(false);
        Stream sourceStream = await CoverFile.OpenReadAsync(cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable configuredSourceStream = sourceStream.ConfigureAwait(false);
        await sourceStream.CopyToAsync(destinationStream, cancellationToken).ConfigureAwait(false);
    }

    private void WriteToc(EpubWriter epubWriter)
    {
        epubWriter.AddToc(_navItems.Select(ConvertNavItem).ToList(), true);

        EpubNavItem ConvertNavItem(IEpubProjectNavItem navItem)
        {
            return new()
            {
                Text = navItem.Text,
                Reference = _converter.ConvertRelativeAnchorHref(navItem.Href),
                Children = navItem.Children.Select(ConvertNavItem).ToList(),
            };
        }
    }

    private async Task WriteResourcesAsync(EpubWriter epubWriter, IReadOnlyCollection<IFile> globalFiles, EpubVersion epubVersion, CancellationToken cancellationToken)
    {
        Dictionary<string, Dictionary<string, ProjectResource>> resources = await TraverseAsync(cancellationToken).ConfigureAwait(false);
        string xhtmlExtension = _mediaTypeFileExtensionsMapping.GetFileExtension(MediaType.Application.Xhtml_Xml)
            ?? throw new InvalidOperationException("No xhtml extension.");
        foreach (string relativePathWithoutExtension in GetNavItemHrefsDepthFirst(_navItems).Select(GetRelativePathWithoutExtension))
        {
            if (!resources.TryGetValue(relativePathWithoutExtension, out Dictionary<string, ProjectResource>? resourceExtensions)) continue;
            await HandleXhtmlAsync(resourceExtensions, cancellationToken).ConfigureAwait(false);
        }
        foreach ((string relativePathWithoutExtension, Dictionary<string, ProjectResource> resourceExtensions) in resources.OrderBy(kvp => kvp.Key))
        {
            await HandleXhtmlAsync(resourceExtensions, cancellationToken).ConfigureAwait(false);
            foreach (ProjectResource resource in resourceExtensions.Values.Order())
            {
                EpubResource epubResource = new()
                {
                    Href = resource.RelativePath,
                };
                Stream resourceStream = await resource.File.OpenReadAsync(cancellationToken).ConfigureAwait(false);
                await using (resourceStream.ConfigureAwait(false))
                {
                    await epubWriter.AddResourceAsync(resourceStream, epubResource, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        foreach (IFile globalFile in globalFiles)
        {
            EpubResource epubResource = new()
            {
                Href = string.Join('/', EpubProjectConstants.GlobalDirectoryName, globalFile.Name),
            };
            Stream resourceStream = await globalFile.OpenReadAsync(cancellationToken).ConfigureAwait(false);
            await using (resourceStream.ConfigureAwait(false))
            {
                await epubWriter.AddResourceAsync(resourceStream, epubResource, cancellationToken).ConfigureAwait(false);
            }
        }

        static IEnumerable<string> GetNavItemHrefsDepthFirst(IEnumerable<IEpubProjectNavItem> navItems)
        {
            foreach (IEpubProjectNavItem navItem in navItems)
            {
                yield return navItem.Href;
                foreach (string href in GetNavItemHrefsDepthFirst(navItem.Children))
                {
                    yield return href;
                }
            }
        }

        static string GetRelativePathWithoutExtension(string href)
        {
            string relativePath = href.Split('#')[0];
            int index = relativePath.LastIndexOf('.');
            string relativePathWithoutExtension = index < 0
                ? relativePath
                : relativePath[..index];
            return relativePathWithoutExtension;
        }

        async Task HandleXhtmlAsync(Dictionary<string, ProjectResource> resourceExtensions, CancellationToken cancellationToken)
        {
            ProjectResource? contentDocumentResource = null;
            for (int i = _contentDocumentMediaTypes.Length - 1; i >= 0; i--)
            {
                string contentDocumentMediaType = _contentDocumentMediaTypes[i];
                if (!_mediaTypeFileExtensionsMapping.TryGetFileExtensions(contentDocumentMediaType, out ImmutableArray<string> contentDocumentExtensions)) continue;
                for (int j = contentDocumentExtensions.Length - 1; j >= 0; j--)
                {
                    string contentDocumentExtension = contentDocumentExtensions[j];
                    if (resourceExtensions.Remove(contentDocumentExtension, out ProjectResource? resource))
                    {
                        contentDocumentResource = resource;
                    }
                }
            }
            if (contentDocumentResource is null) return;
            string? mediaType = _mediaTypeFileExtensionsMapping.GetMediaType(contentDocumentResource.File.Extension);
            if (mediaType == MediaType.Application.Xhtml_Xml)
            {
                IDocument xhtmlDocument;
                Stream xhtmlStream = await contentDocumentResource.File.OpenReadAsync(cancellationToken).ConfigureAwait(false);
                await using (xhtmlStream.ConfigureAwait(false))
                {
                    xhtmlDocument = await _htmlParser.ParseDocumentAsync(xhtmlStream);
                }
                List<string> manifestProperties = [];
                if (IsScriptedXhtml(xhtmlDocument))
                {
                    manifestProperties.Add("scripted");
                }
                EpubResource epubResource = new()
                {
                    Href = contentDocumentResource.RelativePath,
                    ManifestProperties = manifestProperties,
                };
                xhtmlStream = await contentDocumentResource.File.OpenReadAsync(cancellationToken).ConfigureAwait(false);
                await using (xhtmlStream.ConfigureAwait(false))
                {
                    await epubWriter.AddResourceAsync(xhtmlStream, epubResource, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                IDocument? xhtmlDocument = mediaType switch
                {
                    MediaType.Text.Html => await _converter.CreateXhtmlFromHtmlAsync(contentDocumentResource.File, contentDocumentResource.RelativePathParts, globalFiles, epubVersion, cancellationToken).ConfigureAwait(false),
                    MediaType.Text.Markdown => await _converter.CreateXhtmlFromMarkdownAsync(contentDocumentResource.File, contentDocumentResource.RelativePathParts, globalFiles, epubVersion, cancellationToken).ConfigureAwait(false),
                    MediaType.Text.Plain => await _converter.CreateXhtmlFromPlainTextAsync(contentDocumentResource.File, contentDocumentResource.RelativePathParts, globalFiles, epubVersion, cancellationToken).ConfigureAwait(false),
                    _ => null,
                };
                if (xhtmlDocument is null) return;
                List<string> manifestProperties = [];
                if (IsScriptedXhtml(xhtmlDocument))
                {
                    manifestProperties.Add("scripted");
                }
                EpubResource epubResource = new()
                {
                    Href = string.Join('/', [.. contentDocumentResource.RelativePathParts[..^1], $"{contentDocumentResource.File.Stem}{xhtmlExtension}"]),
                    ManifestProperties = manifestProperties,
                };
                Stream resourceStream = await epubWriter.CreateResourceAsync(epubResource, cancellationToken).ConfigureAwait(false);
                await using ConfiguredAsyncDisposable configuredResourceStream = resourceStream.ConfigureAwait(false);
                await using StreamWriter streamWriter = new(resourceStream, EpubProjectConstants.TextEncoding);
                xhtmlDocument.ToHtml(streamWriter, _markupFormatter);
            }
        }
    }

    private async Task<Dictionary<string, Dictionary<string, ProjectResource>>> TraverseAsync(CancellationToken cancellationToken)
    {
        Dictionary<string, Dictionary<string, ProjectResource>> resources = [];
        await TraverseAsync(resources, _projectDirectory.GetDirectory(EpubProjectConstants.ContentsDirectoryName), [], cancellationToken).ConfigureAwait(false);
        return resources;

        static async Task TraverseAsync(Dictionary<string, Dictionary<string, ProjectResource>> resources,
            IDirectory directory, ImmutableArray<string> relativeDirectoryPath,
            CancellationToken cancellationToken)
        {
            await foreach (IFile file in directory.EnumerateFilesAsync(cancellationToken).ConfigureAwait(false))
            {
                if (file.Name.StartsWith('.') || file.Name.StartsWith('_')) continue;
                ImmutableArray<string> relativePathParts = relativeDirectoryPath.Add(file.Name);
                string relativePath = string.Join('/', relativePathParts);
                ProjectResource resource = new()
                {
                    File = file,
                    RelativePathParts = relativePathParts,
                    RelativePath = relativePath,
                };
                string relativePathWithoutExtension = string.Join('/', [.. relativeDirectoryPath, file.Stem]);
                string extension = file.Extension;
                if (!resources.TryGetValue(relativePathWithoutExtension, out Dictionary<string, ProjectResource>? resourceExtensions))
                {
                    resourceExtensions = [];
                    resources[relativePathWithoutExtension] = resourceExtensions;
                }
                resourceExtensions.Add(extension, resource);
            }
            await foreach (IDirectory subdirectory in directory.EnumerateDirectoriesAsync(cancellationToken).ConfigureAwait(false))
            {
                if (subdirectory.Name.StartsWith('.') || subdirectory.Name.StartsWith('_')) continue;
                await TraverseAsync(resources, subdirectory, relativeDirectoryPath.Add(subdirectory.Name), cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private sealed class ProjectResource
    {
        public required IFile File { get; init; }
        public required ImmutableArray<string> RelativePathParts { get; init; }
        public required string RelativePath { get; init; }
    };
}
