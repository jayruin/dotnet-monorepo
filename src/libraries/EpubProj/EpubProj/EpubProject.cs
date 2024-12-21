using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Epubs;
using FileStorage;
using Markdig;
using MediaTypes;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utils;

namespace EpubProj;

internal sealed class EpubProject : IEpubProject
{
    private const string ContentsDirectoryName = "contents";
    private const string GlobalDirectoryName = "_global";
    private static readonly ImmutableArray<string> _contentDocumentMediaTypes = [
        MediaType.Application.Xhtml_Xml,
        MediaType.Text.Html,
        MediaType.Text.Markdown,
        MediaType.Text.Plain,
    ];
    private static readonly FrozenSet<string> _convertibleMediaTypes = FrozenSet.Create(MediaType.Text.Html, MediaType.Text.Markdown, MediaType.Text.Plain);
    private static readonly Encoding _encoding = new UTF8Encoding();
    private readonly IDirectory _projectDirectory;
    private readonly ImmutableArray<IEpubProjectNavItem> _navItems;
    private readonly IMediaTypeFileExtensionsMapping _mediaTypeFileExtensionsMapping;
    private readonly IHtmlParser _htmlParser;
    private readonly IImplementation _domImplementation;
    private readonly IMarkupFormatter _markupFormatter;

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
        _domImplementation = domImplementation;
        _markupFormatter = markupFormatter;
    }

    public Task ExportEpub3Async(Stream stream, IReadOnlyCollection<IFile> globalFiles)
        => ExportEpubAsync(stream, globalFiles, EpubVersion.Epub3);

    public Task ExportEpub2Async(Stream stream, IReadOnlyCollection<IFile> globalFiles)
        => ExportEpubAsync(stream, globalFiles, EpubVersion.Epub2);

    private static IHtmlHeadingElement? GetHighestHeadingElement(IDocument document)
        => Enumerable.Range(1, 6)
            .Select(i => $"h{i}")
            .Select(name => document.QuerySelector<IHtmlHeadingElement>(name))
            .FirstOrDefault(e => e is not null);

    private static bool IsScriptedXhtml(IDocument document)
        => document.QuerySelector<IHtmlScriptElement>("script") is not null;

    private async Task ExportEpubAsync(Stream stream, IReadOnlyCollection<IFile> globalFiles, EpubVersion epubVersion)
    {
        await using EpubWriter epubWriter = await EpubWriter.CreateAsync(stream, epubVersion, _mediaTypeFileExtensionsMapping);
        WriteMetadata(epubWriter);
        await WriteCoverAsync(epubWriter);
        WriteToc(epubWriter);
        await WriteResourcesAsync(epubWriter, globalFiles, epubVersion);
    }

    private void WriteMetadata(EpubWriter epubWriter)
    {
        epubWriter.Title = Metadata.Title;
        epubWriter.Creators = Metadata.Creators
            .Select(ConvertCreator)
            .ToList();
        epubWriter.Languages = Metadata.Languages;
        epubWriter.Direction = ConvertDirection(Metadata.Direction);
        epubWriter.Date = Metadata.Date.ToDateTimeOffsetNullable();
        epubWriter.Identifier = Metadata.Identifier;
        epubWriter.Modified = Metadata.Modified;
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

    private async Task WriteCoverAsync(EpubWriter epubWriter)
    {
        if (CoverFile is null) return;
        await using Stream destinationStream = epubWriter.CreateRasterCover(CoverFile.Extension, true);
        await using Stream sourceStream = await CoverFile.OpenReadAsync();
        await sourceStream.CopyToAsync(destinationStream);
    }

    private string ConvertRelativeAnchorHref(string href)
    {
        string xhtmlExtension = _mediaTypeFileExtensionsMapping.GetFileExtension(MediaType.Application.Xhtml_Xml)
            ?? throw new InvalidOperationException("No xhtml extension.");
        string trimmedXhtmlExtension = xhtmlExtension.Trim('.');
        string[] hrefParts = href.Split('#');
        string hrefPath = hrefParts[0];
        List<string> hrefPathParts = [.. hrefPath.Split('.')];
        string? mediaType = _mediaTypeFileExtensionsMapping.GetMediaType($".{hrefPathParts[^1]}");
        if (mediaType != null && _convertibleMediaTypes.Contains(mediaType))
        {
            hrefPathParts[^1] = trimmedXhtmlExtension;
        }
        else if (hrefPathParts[^1] != trimmedXhtmlExtension)
        {
            hrefPathParts.Add(trimmedXhtmlExtension);
        }
        hrefParts[0] = string.Join('.', hrefPathParts);
        return string.Join('#', hrefParts);
    }

    private void WriteToc(EpubWriter epubWriter)
    {
        epubWriter.AddToc(_navItems.Select(ConvertNavItem).ToList(), true);

        EpubNavItem ConvertNavItem(IEpubProjectNavItem navItem)
        {
            return new()
            {
                Text = navItem.Text,
                Reference = ConvertRelativeAnchorHref(navItem.Href),
                Children = navItem.Children.Select(ConvertNavItem).ToList(),
            };
        }
    }

    private async Task WriteResourcesAsync(EpubWriter epubWriter, IReadOnlyCollection<IFile> globalFiles, EpubVersion epubVersion)
    {
        Dictionary<string, Dictionary<string, ProjectResource>> resources = await TraverseAsync();
        string xhtmlExtension = _mediaTypeFileExtensionsMapping.GetFileExtension(MediaType.Application.Xhtml_Xml)
            ?? throw new InvalidOperationException("No xhtml extension.");
        foreach (string relativePathWithoutExtension in GetNavItemHrefsDepthFirst(_navItems).Select(GetRelativePathWithoutExtension))
        {
            if (!resources.TryGetValue(relativePathWithoutExtension, out Dictionary<string, ProjectResource>? resourceExtensions)) continue;
            await HandleXhtmlAsync(resourceExtensions);
        }
        foreach ((string relativePathWithoutExtension, Dictionary<string, ProjectResource> resourceExtensions) in resources.OrderBy(kvp => kvp.Key))
        {
            await HandleXhtmlAsync(resourceExtensions);
            foreach (ProjectResource resource in resourceExtensions.Values.Order())
            {
                EpubResource epubResource = new()
                {
                    Href = resource.RelativePath,
                };
                await using Stream resourceStream = await resource.File.OpenReadAsync();
                await epubWriter.AddResourceAsync(resourceStream, epubResource);
            }
        }
        foreach (IFile globalFile in globalFiles)
        {
            EpubResource epubResource = new()
            {
                Href = string.Join('/', GlobalDirectoryName, globalFile.Name),
            };
            await using Stream resourceStream = await globalFile.OpenReadAsync();
            await epubWriter.AddResourceAsync(resourceStream, epubResource);
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

        async Task HandleXhtmlAsync(Dictionary<string, ProjectResource> resourceExtensions)
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
                await using (Stream xhtmlStream = await contentDocumentResource.File.OpenReadAsync())
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
                await using (Stream xhtmlStream = await contentDocumentResource.File.OpenReadAsync())
                {
                    await epubWriter.AddResourceAsync(xhtmlStream, epubResource);
                }
            }
            else
            {
                IDocument? xhtmlDocument = mediaType switch
                {
                    MediaType.Text.Html => await CreateXhtmlFromHtmlAsync(contentDocumentResource.File, contentDocumentResource.RelativePathParts, globalFiles, epubVersion),
                    MediaType.Text.Markdown => await CreateXhtmlFromMarkdownAsync(contentDocumentResource.File, contentDocumentResource.RelativePathParts, globalFiles, epubVersion),
                    MediaType.Text.Plain => await CreateXhtmlFromPlainTextAsync(contentDocumentResource.File, contentDocumentResource.RelativePathParts, globalFiles, epubVersion),
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
                await using Stream resourceStream = epubWriter.CreateResource(epubResource);
                await using StreamWriter streamWriter = new(resourceStream, _encoding);
                xhtmlDocument.ToHtml(streamWriter, _markupFormatter);
            }
        }
    }

    private IDocument CreateTemplateXhtmlDocument(string title, ImmutableArray<string> relativePathParts,
        IReadOnlyCollection<IFile> globalFiles, EpubVersion epubVersion)
    {
        IDocument xhtmlDocument = _domImplementation.CreateHtmlDocument(title);

        IDocumentType doctype = epubVersion switch
        {
            EpubVersion.Epub2 => _domImplementation.CreateDocumentType("html", "-//W3C//DTD XHTML 1.1//EN", "http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd"),
            EpubVersion.Epub3 => _domImplementation.CreateDocumentType("html", "", ""),
            _ => throw new InvalidOperationException("Invalid epub version."),
        };
        xhtmlDocument.Doctype.ReplaceWith(doctype);

        IProcessingInstruction xmlHeader = xhtmlDocument.CreateProcessingInstruction("xml", "version=\"1.0\" encoding=\"UTF-8\"");
        xhtmlDocument.InsertBefore(xmlHeader, xhtmlDocument.FirstChild);

        xhtmlDocument.DocumentElement.SetAttribute("http://www.w3.org/2000/xmlns/", "xmlns", "http://www.w3.org/1999/xhtml");

        if (epubVersion == EpubVersion.Epub3)
        {
            xhtmlDocument.DocumentElement.SetAttribute("http://www.w3.org/2000/xmlns/", "xmlns:epub", "http://www.idpf.org/2007/ops");
        }

        IHtmlMetaElement meta = (IHtmlMetaElement)xhtmlDocument.CreateElement("meta");
        if (epubVersion == EpubVersion.Epub3)
        {
            meta.Charset = "utf-8";
        }
        else if (epubVersion == EpubVersion.Epub2)
        {
            meta.HttpEquivalent = "content-type";
            meta.Content = "application/xhtml+xml; charset=utf-8";
        }
        xhtmlDocument.Head?.AppendChild(meta);

        foreach (IFile globalFile in globalFiles)
        {
            string pathToGlobalFile = string.Join('/', [.. Enumerable.Repeat("..", relativePathParts.Length - 1), GlobalDirectoryName, globalFile.Name]);
            string? mediaType = _mediaTypeFileExtensionsMapping.GetMediaType(globalFile.Extension);
            if (mediaType == MediaType.Text.Css)
            {
                IHtmlLinkElement link = (IHtmlLinkElement)xhtmlDocument.CreateElement("link");
                link.Href = pathToGlobalFile;
                link.Type = mediaType;
                link.Relation = "stylesheet";
                xhtmlDocument.Head?.Append(link);
            }
            else if (mediaType == MediaType.Text.Javascript && epubVersion == EpubVersion.Epub3)
            {
                IHtmlScriptElement script = (IHtmlScriptElement)xhtmlDocument.CreateElement("script");
                script.Source = pathToGlobalFile;
                script.Type = globalFile.Extension == ".mjs"
                    ? "module"
                    : mediaType;
                xhtmlDocument.Head?.Append(script);
            }
        }

        return xhtmlDocument;
    }

    private IDocument CreateXhtmlDocumentFromHtmlDocument(IDocument htmlDocument, ImmutableArray<string> relativePathParts,
        IReadOnlyCollection<IFile> globalFiles, EpubVersion epubVersion)
    {
        string title = htmlDocument.Title
            ?? GetHighestHeadingElement(htmlDocument)?.TextContent
            ?? Path.GetFileNameWithoutExtension(relativePathParts[^1]);
        IDocument xhtmlDocument = CreateTemplateXhtmlDocument(title, relativePathParts, globalFiles, epubVersion);

        if (htmlDocument.Head is not null)
        {
            xhtmlDocument.Head?.Append(htmlDocument.Head.QuerySelectorAll<IHtmlLinkElement>("link[rel=\"stylesheet\"]").Select(n => n.Clone(true)).ToArray());
            xhtmlDocument.Head?.Append(htmlDocument.Head.QuerySelectorAll<IHtmlScriptElement>("script").Select(n => n.Clone(true)).ToArray());
        }

        if (htmlDocument.Body is not null)
        {
            xhtmlDocument.Body?.Append(htmlDocument.Body.Children.Select(n => n.Clone(true)).ToArray());
        }

        ConvertRelativeAnchorHrefs(xhtmlDocument);

        return xhtmlDocument;

        void ConvertRelativeAnchorHrefs(IDocument xhtmlDocument)
        {
            foreach (IHtmlAnchorElement a in xhtmlDocument.QuerySelectorAll<IHtmlAnchorElement>("a"))
            {
                string? href = a.GetAttribute("href");
                if (string.IsNullOrWhiteSpace(href)) continue;
                if (Uri.IsWellFormedUriString(href, UriKind.Absolute)) continue;
                a.SetAttribute("href", ConvertRelativeAnchorHref(href));
            }
        }
    }

    private async Task<IDocument> CreateXhtmlFromHtmlAsync(IFile htmlFile, ImmutableArray<string> relativePathParts,
        IReadOnlyCollection<IFile> globalFiles, EpubVersion epubVersion)
    {
        IDocument htmlDocument;
        await using (Stream htmlStream = await htmlFile.OpenReadAsync())
        {
            htmlDocument = await _htmlParser.ParseDocumentAsync(htmlStream);
        }

        return CreateXhtmlDocumentFromHtmlDocument(htmlDocument, relativePathParts, globalFiles, epubVersion);
    }

    private async Task<IDocument> CreateXhtmlFromMarkdownAsync(IFile markdownFile, ImmutableArray<string> relativePathParts,
        IReadOnlyCollection<IFile> globalFiles, EpubVersion epubVersion)
    {
        byte[] markdownBytes;
        await using (Stream markdownStream = await markdownFile.OpenReadAsync())
        {
            markdownBytes = await markdownStream.ToByteArrayAsync();
        }
        string markdownString = _encoding.GetString(markdownBytes);
        string htmlString = Markdown.ToHtml(markdownString);

        IDocument htmlDocument = await _htmlParser.ParseDocumentAsync(htmlString);

        return CreateXhtmlDocumentFromHtmlDocument(htmlDocument, relativePathParts, globalFiles, epubVersion);
    }

    private async Task<IDocument> CreateXhtmlFromPlainTextAsync(IFile textFile, ImmutableArray<string> relativePathParts,
        IReadOnlyCollection<IFile> globalFiles, EpubVersion epubVersion)
    {
        IDocument? htmlDocument = null;
        await using (Stream textStream = await textFile.OpenReadAsync())
        {
            using StreamReader streamReader = new(textStream, _encoding);
            string? line;
            while ((line = await streamReader.ReadLineAsync()) is not null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                string text = line.Trim();
                if (htmlDocument is null)
                {
                    htmlDocument = _domImplementation.CreateHtmlDocument(text);
                    IElement heading = htmlDocument.CreateElement("h1");
                    heading.TextContent = text;
                    htmlDocument.Body?.Append(heading);
                }
                else
                {
                    IElement paragraph = htmlDocument.CreateElement("p");
                    paragraph.TextContent = text;
                    htmlDocument.Body?.Append(paragraph);
                }
            }
        }
        if (htmlDocument is null)
        {
            string text = textFile.Stem.Trim();
            htmlDocument = _domImplementation.CreateHtmlDocument(text);
            IElement heading = htmlDocument.CreateElement("h1");
            heading.TextContent = text;
            htmlDocument.Body?.Append(heading);
        }
        return CreateXhtmlDocumentFromHtmlDocument(htmlDocument, relativePathParts, globalFiles, epubVersion);
    }

    private async Task<Dictionary<string, Dictionary<string, ProjectResource>>> TraverseAsync()
    {
        Dictionary<string, Dictionary<string, ProjectResource>> resources = [];
        await TraverseAsync(resources, _projectDirectory.GetDirectory(ContentsDirectoryName), []);
        return resources;

        static async Task TraverseAsync(Dictionary<string, Dictionary<string, ProjectResource>> resources,
            IDirectory directory, ImmutableArray<string> relativeDirectoryPath)
        {
            await foreach (IFile file in directory.EnumerateFilesAsync())
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
            await foreach (IDirectory subdirectory in directory.EnumerateDirectoriesAsync())
            {
                if (subdirectory.Name.StartsWith('.') || subdirectory.Name.StartsWith('_')) continue;
                await TraverseAsync(resources, subdirectory, relativeDirectoryPath.Add(subdirectory.Name));
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
