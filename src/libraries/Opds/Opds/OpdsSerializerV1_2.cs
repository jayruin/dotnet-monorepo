using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Opds;

public sealed class OpdsSerializerV1_2
{
    private const string AtomXmlNamespace = "http://www.w3.org/2005/Atom";
    private const string DcXmlNamespapce = "http://purl.org/dc/terms/";
    private const string OpdsXmlCatalogMediaType = "application/atom+xml;profile=opds-catalog";
    private const string OpdsXmlAcquisitionMediaType = "application/atom+xml;profile=opds-catalog;kind=acquisition";
    private const string RelImage = "http://opds-spec.org/image";
    private const string RelAcquisition = "http://opds-spec.org/acquisition";
    private const string OpenSearchDescriptionMediaType = "application/opensearchdescription+xml";
    private const string OpenSearchDescriptionNamespace = "http://a9.com/-/spec/opensearch/1.1/";

    public const string FeedMediaType = OpdsXmlCatalogMediaType;
    public const string FeedFileExtension = ".xml";
    public const string OpenSearchDescriptionDocumentMediaType = FeedMediaType;
    public const string OpenSearchDescriptionDocumentFileExtension = FeedFileExtension;

    public static Task WriteFeedAsync(Stream stream, OpdsFeed feed, OpdsSerializerFeedOptionsV1_2 options, CancellationToken cancellationToken = default)
    {
        XDocument feedDocument = CreateFeedDocument(feed, options);
        return SerializeAsync(stream, feedDocument, cancellationToken);
    }

    public static Task WriteOpenSearchDescriptionDocumentAsync(Stream stream, string templateUrl,
        string? title = null, string? description = null,
        CancellationToken cancellationToken = default)
    {
        XDocument document = new(
            new XElement((XNamespace)OpenSearchDescriptionNamespace + "OpenSearchDescription",
                string.IsNullOrWhiteSpace(title)
                    ? null
                    : new XElement((XNamespace)OpenSearchDescriptionNamespace + "ShortName", title),
                string.IsNullOrWhiteSpace(description)
                    ? null
                    : new XElement((XNamespace)OpenSearchDescriptionNamespace + "Description", description),
                new XElement((XNamespace)OpenSearchDescriptionNamespace + "Url",
                    new XAttribute("type", OpdsXmlAcquisitionMediaType),
                    new XAttribute("template", templateUrl))));
        return SerializeAsync(stream, document, cancellationToken);
    }

    private static async Task SerializeAsync(Stream stream, XDocument document, CancellationToken cancellationToken)
    {
        XmlWriter writer = XmlWriter.Create(stream, CreateXmlWriterSettings());
        await using ConfiguredAsyncDisposable configuredWriter = writer.ConfigureAwait(false);
        await document.SaveAsync(writer, cancellationToken).ConfigureAwait(false);
    }

    private static XmlWriterSettings CreateXmlWriterSettings() => new()
    {
        Async = true,
        Encoding = new UTF8Encoding(),
        Indent = true,
        NamespaceHandling = NamespaceHandling.OmitDuplicates,
        NewLineChars = "\n",
    };

    private static XDocument CreateFeedDocument(OpdsFeed feed, OpdsSerializerFeedOptionsV1_2 options)
    {
        XElement feedElement = new((XNamespace)AtomXmlNamespace + "feed",
            new XElement((XNamespace)AtomXmlNamespace + "id", feed.Self),
            new XElement((XNamespace)AtomXmlNamespace + "title", "umm OPDS v1.2 Feed"),
            new XElement((XNamespace)AtomXmlNamespace + "updated", feed.Modified),
            new XElement((XNamespace)AtomXmlNamespace + "link",
                new XAttribute("rel", "self"),
                new XAttribute("title", "Current Page"),
                new XAttribute("type", OpdsXmlCatalogMediaType),
                new XAttribute("href", feed.Self)),
            string.IsNullOrWhiteSpace(feed.Prev)
                ? null
                : new XElement((XNamespace)AtomXmlNamespace + "link",
                    new XAttribute("rel", "previous"),
                    new XAttribute("title", "Previous Page"),
                    new XAttribute("type", OpdsXmlCatalogMediaType),
                    new XAttribute("href", feed.Prev)),
            string.IsNullOrWhiteSpace(feed.Next)
                ? null
                : new XElement((XNamespace)AtomXmlNamespace + "link",
                    new XAttribute("rel", "next"),
                    new XAttribute("title", "Next Page"),
                    new XAttribute("type", OpdsXmlCatalogMediaType),
                    new XAttribute("href", feed.Next)));
        foreach (OpdsNavigationEntry navigationEntry in feed.NavigationEntries)
        {
            feedElement.Add(CreateNavigationEntryElement(navigationEntry, feed.Modified));
        }
        foreach (OpdsAcquisitionEntry acquisitionEntry in feed.AcquisitionEntries)
        {
            feedElement.Add(CreateAcquisitionEntryElement(acquisitionEntry, feed.Modified));
        }
        if (!string.IsNullOrWhiteSpace(options.OpenSearchDescriptionUrl))
        {
            feedElement.Add(new XElement((XNamespace)AtomXmlNamespace + "link",
                new XAttribute("rel", "search"),
                new XAttribute("type", OpenSearchDescriptionMediaType),
                new XAttribute("href", options.OpenSearchDescriptionUrl)));
        }
        XDocument document = new(feedElement);
        return document;
    }

    private static XElement CreateNavigationEntryElement(OpdsNavigationEntry navigationEntry, string feedModified)
    {
        XElement entryElement = new((XNamespace)AtomXmlNamespace + "entry",
            new XElement((XNamespace)AtomXmlNamespace + "id", navigationEntry.Identifier),
            new XElement((XNamespace)DcXmlNamespapce + "identifier", navigationEntry.Identifier),
            new XElement((XNamespace)AtomXmlNamespace + "title", navigationEntry.Title),
            new XElement((XNamespace)AtomXmlNamespace + "updated",
                string.IsNullOrWhiteSpace(navigationEntry.Modified) ? feedModified : navigationEntry.Modified));
        foreach (OpdsResourceLink imageLink in navigationEntry.ImageLinks)
        {
            entryElement.Add(new XElement((XNamespace)AtomXmlNamespace + "link",
                new XAttribute("rel", RelImage),
                new XAttribute("href", imageLink.Href),
                new XAttribute("type", imageLink.Type),
                string.IsNullOrWhiteSpace(imageLink.Title)
                    ? null
                    : new XAttribute("title", imageLink.Title)));
        }
        entryElement.Add(new XElement((XNamespace)AtomXmlNamespace + "link",
            new XAttribute("rel", "subsection"),
            new XAttribute("type", OpdsXmlAcquisitionMediaType),
            new XAttribute("href", navigationEntry.NavigationLink.Href),
            string.IsNullOrWhiteSpace(navigationEntry.NavigationLink.Title)
                ? null
                : new XAttribute("title", navigationEntry.NavigationLink.Title)));
        return entryElement;
    }

    private static XElement CreateAcquisitionEntryElement(OpdsAcquisitionEntry acquisitionEntry, string feedModified)
    {
        XElement entryElement = new((XNamespace)AtomXmlNamespace + "entry",
            new XElement((XNamespace)AtomXmlNamespace + "id", acquisitionEntry.Identifier),
            new XElement((XNamespace)DcXmlNamespapce + "identifier", acquisitionEntry.Identifier),
            new XElement((XNamespace)AtomXmlNamespace + "title", acquisitionEntry.Title),
            new XElement((XNamespace)AtomXmlNamespace + "updated",
                string.IsNullOrWhiteSpace(acquisitionEntry.Modified) ? feedModified : acquisitionEntry.Modified));
        foreach (string creator in acquisitionEntry.Creators)
        {
            entryElement.Add(new XElement((XNamespace)AtomXmlNamespace + "author",
                new XElement((XNamespace)AtomXmlNamespace + "name", creator)));
        }
        if (!string.IsNullOrWhiteSpace(acquisitionEntry.Description))
        {
            entryElement.Add(new XElement((XNamespace)AtomXmlNamespace + "summary", acquisitionEntry.Description));
        }
        foreach (string tag in acquisitionEntry.Tags)
        {
            entryElement.Add(new XElement((XNamespace)AtomXmlNamespace + "category",
                new XAttribute("term", tag)));
        }
        foreach (OpdsResourceLink imageLink in acquisitionEntry.ImageLinks)
        {
            entryElement.Add(new XElement((XNamespace)AtomXmlNamespace + "link",
                new XAttribute("rel", RelImage),
                new XAttribute("href", imageLink.Href),
                new XAttribute("type", imageLink.Type),
                string.IsNullOrWhiteSpace(imageLink.Title)
                    ? null
                    : new XAttribute("title", imageLink.Title)));
        }
        foreach (OpdsResourceLink acquisitionLink in acquisitionEntry.AcquisitionLinks)
        {
            entryElement.Add(new XElement((XNamespace)AtomXmlNamespace + "link",
                new XAttribute("rel", RelAcquisition),
                new XAttribute("href", acquisitionLink.Href),
                new XAttribute("type", acquisitionLink.Type),
                string.IsNullOrWhiteSpace(acquisitionLink.Title)
                    ? null
                    : new XAttribute("title", acquisitionLink.Title)));
        }
        return entryElement;
    }
}
