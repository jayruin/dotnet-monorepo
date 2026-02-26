using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Opds;

public sealed class OpdsSerializerV2_0
{
    private const string OpdsJsonCatalogMediaType = "application/opds+json";
    private const string RelAcquisition = "http://opds-spec.org/acquisition/open-access";

    public const string FeedMediaType = OpdsJsonCatalogMediaType;
    public const string FeedFileExtension = ".json";

    public static async Task WriteFeedAsync(Stream stream, OpdsFeed feed, OpdsSerializerFeedOptionsV2_0 options, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        JsonNode feedDocument = CreateFeedDocument(feed, options);
        Utf8JsonWriter writer = new(stream, CreateJsonWriterOptions());
        await using ConfiguredAsyncDisposable configuredWriter = writer.ConfigureAwait(false);
        feedDocument.WriteTo(writer);
    }

    private static JsonWriterOptions CreateJsonWriterOptions() => new()
    {
        Indented = true,
        NewLine = "\n",
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static JsonNode CreateFeedDocument(OpdsFeed feed, OpdsSerializerFeedOptionsV2_0 options)
    {
        JsonObject rootNode = new()
        {
            ["metadata"] = new JsonObject()
            {
                ["title"] = feed.Title,
            },
            ["links"] = CreateFeedLinksNode(feed, options.OpenSearchTemplateUrl),
        };

        if (feed.NavigationEntries.Length > 0)
        {
            rootNode["navigation"] = new JsonArray([.. feed.NavigationEntries.Select(CreateNavigationNode)]);
        }

        if (feed.AcquisitionEntries.Length > 0)
        {
            rootNode["publications"] = new JsonArray([.. feed.AcquisitionEntries.Select(e => CreatePublicationNode(e, feed.Modified))]);
        }

        return rootNode;
    }

    private static JsonArray CreateFeedLinksNode(OpdsFeed feed, string? openSearchTemplateUrl)
    {
        JsonArray linksNode = new();
        JsonNode selfNode = new JsonObject()
        {
            ["rel"] = "self",
            ["title"] = "Current Page",
            ["type"] = OpdsJsonCatalogMediaType,
            ["href"] = feed.Self,
        };
        linksNode.Add(selfNode);
        if (!string.IsNullOrWhiteSpace(feed.Prev))
        {
            JsonNode prevNode = new JsonObject()
            {
                ["rel"] = "previous",
                ["title"] = "Previous Page",
                ["type"] = OpdsJsonCatalogMediaType,
                ["href"] = feed.Prev,
            };
            linksNode.Add(prevNode);
        }
        if (!string.IsNullOrWhiteSpace(feed.Next))
        {
            JsonNode nextNode = new JsonObject()
            {
                ["rel"] = "next",
                ["title"] = "Next Page",
                ["type"] = OpdsJsonCatalogMediaType,
                ["href"] = feed.Next,
            };
            linksNode.Add(nextNode);
        }
        if (!string.IsNullOrWhiteSpace(openSearchTemplateUrl))
        {
            JsonNode openSearchTemplateUrlNode = new JsonObject()
            {
                ["rel"] = "search",
                ["title"] = "Search",
                ["type"] = OpdsJsonCatalogMediaType,
                ["href"] = openSearchTemplateUrl,
                ["templated"] = true,
            };
            linksNode.Add(openSearchTemplateUrlNode);
        }
        return linksNode;
    }

    private static JsonObject CreateNavigationNode(OpdsNavigationEntry navigationEntry)
    {
        JsonObject navigationNode = new()
        {
            ["rel"] = "subsection",
            ["href"] = navigationEntry.NavigationLink.Href,
            ["type"] = OpdsJsonCatalogMediaType,
        };
        if (!string.IsNullOrWhiteSpace(navigationEntry.NavigationLink.Title))
        {
            navigationNode["title"] = navigationEntry.NavigationLink.Title;
        }
        return navigationNode;
    }

    private static JsonObject CreatePublicationNode(OpdsAcquisitionEntry acquisitionEntry, string feedModified)
    {
        JsonObject metadataNode = new()
        {
            ["identifier"] = acquisitionEntry.Identifier,
            ["title"] = acquisitionEntry.Title,
            ["modified"] = string.IsNullOrWhiteSpace(acquisitionEntry.Modified) ? feedModified : acquisitionEntry.Modified,
        };
        if (acquisitionEntry.Creators.Length > 0)
        {
            metadataNode["author"] = new JsonArray([.. acquisitionEntry.Creators]);
        }
        if (!string.IsNullOrWhiteSpace(acquisitionEntry.Description))
        {
            metadataNode["description"] = acquisitionEntry.Description;
        }
        if (acquisitionEntry.Tags.Length > 0)
        {
            metadataNode["subject"] = new JsonArray([.. acquisitionEntry.Tags]);
        }

        JsonObject publicationNode = new()
        {
            ["metadata"] = metadataNode,
        };

        JsonArray imagesNode = new();
        foreach (OpdsResourceLink imageLink in acquisitionEntry.ImageLinks)
        {
            JsonNode imageNode = new JsonObject()
            {
                ["href"] = imageLink.Href,
                ["type"] = imageLink.Type,
            };
            if (!string.IsNullOrWhiteSpace(imageLink.Title))
            {
                imageNode["title"] = imageLink.Title;
            }
            imagesNode.Add(imageNode);
        }
        if (imagesNode.Count > 0)
        {
            publicationNode["images"] = imagesNode;
        }

        JsonArray linksNode = new();
        foreach (OpdsResourceLink acquisitionLink in acquisitionEntry.AcquisitionLinks)
        {
            JsonNode linkNode = new JsonObject()
            {
                ["rel"] = RelAcquisition,
                ["href"] = acquisitionLink.Href,
                ["type"] = acquisitionLink.Type,
            };
            if (!string.IsNullOrWhiteSpace(acquisitionLink.Title))
            {
                linkNode["title"] = acquisitionLink.Title;
            }
            linksNode.Add(linkNode);
        }
        if (linksNode.Count > 0)
        {
            publicationNode["links"] = linksNode;
        }

        return publicationNode;
    }
}
