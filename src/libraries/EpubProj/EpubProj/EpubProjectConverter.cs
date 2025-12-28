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
using System.Threading;
using System.Threading.Tasks;
using Utils;

namespace EpubProj;

internal sealed class EpubProjectConverter
{
    private static readonly FrozenSet<string> _convertibleMediaTypes = FrozenSet.Create(MediaType.Text.Html, MediaType.Text.Markdown, MediaType.Text.Plain);
    private readonly IMediaTypeFileExtensionsMapping _mediaTypeFileExtensionsMapping;
    private readonly IHtmlParser _htmlParser;
    private readonly IImplementation _domImplementation;

    public EpubProjectConverter(IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping, IHtmlParser htmlParser, IImplementation domImplementation)
    {
        _mediaTypeFileExtensionsMapping = mediaTypeFileExtensionsMapping;
        _htmlParser = htmlParser;
        _domImplementation = domImplementation;
    }

    public async Task<IDocument> CreateXhtmlFromHtmlAsync(IFile htmlFile, ImmutableArray<string> relativePathParts,
        IReadOnlyCollection<IFile> globalFiles, EpubVersion epubVersion,
        CancellationToken cancellationToken)
    {
        IDocument htmlDocument;
        Stream htmlStream = await htmlFile.OpenReadAsync(cancellationToken).ConfigureAwait(false);
        await using (htmlStream.ConfigureAwait(false))
        {
            htmlDocument = await _htmlParser.ParseDocumentAsync(htmlStream, cancellationToken).ConfigureAwait(false);
        }

        return CreateXhtmlDocumentFromHtmlDocument(htmlDocument, relativePathParts, globalFiles, epubVersion);
    }

    public async Task<IDocument> CreateXhtmlFromMarkdownAsync(IFile markdownFile, ImmutableArray<string> relativePathParts,
        IReadOnlyCollection<IFile> globalFiles, EpubVersion epubVersion,
        CancellationToken cancellationToken)
    {
        byte[] markdownBytes;
        Stream markdownStream = await markdownFile.OpenReadAsync(cancellationToken).ConfigureAwait(false);
        await using (markdownStream.ConfigureAwait(false))
        {
            markdownBytes = await markdownStream.ToByteArrayAsync(cancellationToken).ConfigureAwait(false);
        }
        string markdownString = EpubProjectConstants.TextEncoding.GetString(markdownBytes);
        string htmlString = Markdown.ToHtml(markdownString);

        IDocument htmlDocument = await _htmlParser.ParseDocumentAsync(htmlString, cancellationToken).ConfigureAwait(false);

        return CreateXhtmlDocumentFromHtmlDocument(htmlDocument, relativePathParts, globalFiles, epubVersion);
    }

    public async Task<IDocument> CreateXhtmlFromPlainTextAsync(IFile textFile, ImmutableArray<string> relativePathParts,
        IReadOnlyCollection<IFile> globalFiles, EpubVersion epubVersion,
        CancellationToken cancellationToken)
    {
        IDocument? htmlDocument = null;
        Stream textStream = await textFile.OpenReadAsync(cancellationToken).ConfigureAwait(false);
        await using (textStream.ConfigureAwait(false))
        {
            using StreamReader streamReader = new(textStream, EpubProjectConstants.TextEncoding);
            string? line;
            while ((line = await streamReader.ReadLineAsync(cancellationToken).ConfigureAwait(false)) is not null)
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

    public string ConvertRelativeAnchorHref(string href)
    {
        string xhtmlExtension = _mediaTypeFileExtensionsMapping.GetFileExtension(MediaType.Application.Xhtml_Xml)
            ?? throw new InvalidOperationException("No xhtml extension.");
        string trimmedXhtmlExtension = xhtmlExtension.Trim('.');
        string[] hrefParts = href.Split('#');
        string hrefPath = hrefParts[0];
        if (string.IsNullOrWhiteSpace(hrefPath)) return href;
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

    private static IHtmlHeadingElement? GetHighestHeadingElement(IDocument document)
        => Enumerable.Range(1, 6)
            .Select(i => $"h{i}")
            .Select(name => document.QuerySelector<IHtmlHeadingElement>(name))
            .FirstOrDefault(e => e is not null);

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

        xhtmlDocument.DocumentElement.SetAttribute(EpubXmlNamespaces.Xmlns, "xmlns", "http://www.w3.org/1999/xhtml");

        if (epubVersion == EpubVersion.Epub3)
        {
            xhtmlDocument.DocumentElement.SetAttribute(EpubXmlNamespaces.Xmlns, "xmlns:epub", EpubXmlNamespaces.Ops);
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
            string pathToGlobalFile = string.Join('/', [.. Enumerable.Repeat("..", relativePathParts.Length - 1), EpubProjectConstants.GlobalDirectoryName, globalFile.Name]);
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
}
