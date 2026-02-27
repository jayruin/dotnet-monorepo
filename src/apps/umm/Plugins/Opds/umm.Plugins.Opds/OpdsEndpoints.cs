using MediaTypes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Opds;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using umm.Catalog;
using umm.Library;
using Utils;

namespace umm.Plugins.Opds;

internal static class OpdsEndpoints
{
    public static void MapOpdsEndpoints(IEndpointRouteBuilder builder)
    {
        RouteGroupBuilder v1_2Group = builder.MapGroup("opds/v1.2");
        v1_2Group.MapGet("advancedsearch/{queryString?}", GetAdvancedSearchOpdsFeedV1_2);
        v1_2Group.MapGet("opensearch", GetOpenSearchOpdsFeedV1_2);
        v1_2Group.MapGet("opensearch/description", GetOpenSearchDescriptionDocumentV1_2);

        RouteGroupBuilder v2_0Group = builder.MapGroup("opds/v2.0");
        v2_0Group.MapGet("advancedsearch/{queryString?}", GetAdvancedSearchOpdsFeedV2_0);
        v2_0Group.MapGet("opensearch", GetOpenSearchOpdsFeedV2_0);
    }

    private static async Task<IResult> GetAdvancedSearchOpdsFeedV1_2(
        string? queryString,
        HttpRequest request,
        IOptionsSnapshot<OpdsOptions> options,
        IMediaCatalog catalog, IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping, TimeProvider timeProvider,
        [FromQuery(Name = "paginated")] bool isPaginated = false,
        [FromQuery(Name = "acquisition")] bool isAcquisition = false,
        [FromQuery] string? history = null,
        CancellationToken cancellationToken = default)
    {
        string opdsVersion = "1.2";
        FrozenSet<MediaFormat> mediaFormats = GetMediaFormatsFromQuery(request);
        Dictionary<string, StringValues> searchQuery = QueryHelpers.ParseQuery(queryString);
        (string? prevUrl, MediaFullId? after) = GetHistory(history, request);
        int pageSize = options.Value.PageSize;
        SearchOptions searchOptions = new()
        {
            IncludeParts = isAcquisition,
            Pagination = isPaginated
                ? new()
                {
                    After = after,
                    Count = pageSize,
                }
                : null,
            MediaFormats = mediaFormats,
        };
        List<MediaEntry> entries = await catalog.EnumerateAsync(searchQuery, searchOptions, cancellationToken)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        string? nextUrl = GetNextUrl(history, request, entries, pageSize);
        OpdsFeed feed = CreateOpdsFeed(opdsVersion, request.Path + request.QueryString,
            entries, timeProvider,
            isAcquisition, isPaginated,
            prevUrl, nextUrl);
        OpdsSerializerFeedOptionsV1_2 serializerOptions = new();
        return TypedResults.Stream(
            stream => OpdsSerializerV1_2.WriteFeedAsync(stream, feed, serializerOptions, cancellationToken),
            OpdsSerializerV1_2.FeedMediaType,
            $"opds_v{opdsVersion}{OpdsSerializerV1_2.FeedFileExtension}");
    }

    private static async Task<IResult> GetOpenSearchOpdsFeedV1_2(
        HttpRequest request,
        IOptionsSnapshot<OpdsOptions> options,
        IMediaCatalog catalog, IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping, TimeProvider timeProvider,
        [FromQuery(Name = "q")] string? searchTerm = null,
        [FromQuery(Name = "paginated")] bool isPaginated = false,
        [FromQuery(Name = "acquisition")] bool isAcquisition = false,
        [FromQuery] string? history = null,
        CancellationToken cancellationToken = default)
    {
        string opdsVersion = "1.2";
        FrozenSet<MediaFormat> mediaFormats = GetMediaFormatsFromQuery(request);
        (string? prevUrl, MediaFullId? after) = GetHistory(history, request);
        int pageSize = options.Value.PageSize;
        SearchOptions searchOptions = new()
        {
            IncludeParts = isAcquisition,
            Pagination = isPaginated
                ? new()
                {
                    After = after,
                    Count = pageSize,
                }
                : null,
            MediaFormats = mediaFormats,
        };
        List<MediaEntry> entries = await catalog.EnumerateAsync(searchTerm ?? string.Empty, searchOptions, cancellationToken)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        string? nextUrl = GetNextUrl(history, request, entries, pageSize);
        OpdsFeed feed = CreateOpdsFeed(opdsVersion, request.Path + request.QueryString,
            entries, timeProvider,
            isAcquisition, isPaginated,
            prevUrl, nextUrl);
        string openSearchDescriptionUrl = QueryHelpers.AddQueryString(
            request.Path.Add("/description"),
            new Dictionary<string, StringValues>()
            {
                { "paginated", isPaginated.ToString() },
                { "acquisition", isAcquisition.ToString() },
                { "format", request.Query.TryGetValue("format", out StringValues formats) ? formats : StringValues.Empty }
            });
        OpdsSerializerFeedOptionsV1_2 serializerOptions = new()
        {
            OpenSearchDescriptionUrl = openSearchDescriptionUrl,
        };
        return TypedResults.Stream(
            stream => OpdsSerializerV1_2.WriteFeedAsync(stream, feed, serializerOptions, cancellationToken),
            OpdsSerializerV1_2.FeedMediaType,
            $"opds_v{opdsVersion}{OpdsSerializerV1_2.FeedFileExtension}");
    }

    private static async Task<IResult> GetOpenSearchDescriptionDocumentV1_2(
        HttpRequest request,
        CancellationToken cancellationToken = default)
    {
        string opdsVersion = "1.2";
        string openSearchEndpoint = string.Join('/', request.Path.ToString().Split('/')[..^1]);
        Dictionary<string, StringValues> query = QueryHelpers.ParseQuery(request.QueryString.Value);
        string templateUrl = string.Concat(
            QueryHelpers.AddQueryString(openSearchEndpoint, query),
            query.Count > 0 ? "&" : "?",
            "q={searchTerms}"
        );
        return TypedResults.Stream(
            stream => OpdsSerializerV1_2.WriteOpenSearchDescriptionDocumentAsync(
                stream, templateUrl,
                $"umm OPDS v{opdsVersion} Search", $"umm OPDS v{opdsVersion} Open Search Description",
                cancellationToken),
            OpdsSerializerV1_2.OpenSearchDescriptionDocumentMediaType,
            $"opds_v{opdsVersion}_opensearch_description{OpdsSerializerV1_2.OpenSearchDescriptionDocumentFileExtension}");
    }

    private static async Task<IResult> GetAdvancedSearchOpdsFeedV2_0(
        string? queryString,
        HttpRequest request,
        IOptionsSnapshot<OpdsOptions> options,
        IMediaCatalog catalog, IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping, TimeProvider timeProvider,
        [FromQuery(Name = "paginated")] bool isPaginated = false,
        [FromQuery(Name = "acquisition")] bool isAcquisition = false,
        [FromQuery] string? history = null,
        CancellationToken cancellationToken = default)
    {
        string opdsVersion = "2.0";
        FrozenSet<MediaFormat> mediaFormats = GetMediaFormatsFromQuery(request);
        Dictionary<string, StringValues> searchQuery = QueryHelpers.ParseQuery(queryString);
        (string? prevUrl, MediaFullId? after) = GetHistory(history, request);
        int pageSize = options.Value.PageSize;
        SearchOptions searchOptions = new()
        {
            IncludeParts = isAcquisition,
            Pagination = isPaginated
                ? new()
                {
                    After = after,
                    Count = pageSize,
                }
                : null,
            MediaFormats = mediaFormats,
        };
        List<MediaEntry> entries = await catalog.EnumerateAsync(searchQuery, searchOptions, cancellationToken)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        string? nextUrl = GetNextUrl(history, request, entries, pageSize);
        OpdsFeed feed = CreateOpdsFeed(opdsVersion, request.Path + request.QueryString,
            entries, timeProvider,
            isAcquisition, isPaginated,
            prevUrl, nextUrl);
        OpdsSerializerFeedOptionsV2_0 serializerOptions = new();
        return TypedResults.Stream(
            stream => OpdsSerializerV2_0.WriteFeedAsync(stream, feed, serializerOptions, cancellationToken),
            OpdsSerializerV2_0.FeedMediaType,
            $"opds_v{opdsVersion}{OpdsSerializerV2_0.FeedFileExtension}");
    }

    private static async Task<IResult> GetOpenSearchOpdsFeedV2_0(
        HttpRequest request,
        IOptionsSnapshot<OpdsOptions> options,
        IMediaCatalog catalog, IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping, TimeProvider timeProvider,
        [FromQuery(Name = "q")] string? searchTerm = null,
        [FromQuery(Name = "paginated")] bool isPaginated = false,
        [FromQuery(Name = "acquisition")] bool isAcquisition = false,
        [FromQuery] string? history = null,
        CancellationToken cancellationToken = default)
    {
        string opdsVersion = "2.0";
        FrozenSet<MediaFormat> mediaFormats = GetMediaFormatsFromQuery(request);
        (string? prevUrl, MediaFullId? after) = GetHistory(history, request);
        int pageSize = options.Value.PageSize;
        SearchOptions searchOptions = new()
        {
            IncludeParts = isAcquisition,
            Pagination = isPaginated
                ? new()
                {
                    After = after,
                    Count = pageSize,
                }
                : null,
            MediaFormats = mediaFormats,
        };
        List<MediaEntry> entries = await catalog.EnumerateAsync(searchTerm ?? string.Empty, searchOptions, cancellationToken)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        string? nextUrl = GetNextUrl(history, request, entries, pageSize);
        OpdsFeed feed = CreateOpdsFeed(opdsVersion, request.Path + request.QueryString,
            entries, timeProvider,
            isAcquisition, isPaginated,
            prevUrl, nextUrl);
        string openSearchEndpoint = request.Path;
        Dictionary<string, StringValues> query = QueryHelpers.ParseQuery(request.QueryString.Value);
        query.Remove("q");
        string templateUrl = string.Concat(
            QueryHelpers.AddQueryString(openSearchEndpoint, query),
            query.Count > 0 ? "&" : "?",
            "q={query}"
        );
        OpdsSerializerFeedOptionsV2_0 serializerOptions = new()
        {
            OpenSearchTemplateUrl = templateUrl,
        };
        return TypedResults.Stream(
            stream => OpdsSerializerV2_0.WriteFeedAsync(stream, feed, serializerOptions, cancellationToken),
            OpdsSerializerV2_0.FeedMediaType,
            $"opds_v{opdsVersion}{OpdsSerializerV2_0.FeedFileExtension}");
    }

    private static FrozenSet<MediaFormat> GetMediaFormatsFromQuery(HttpRequest request)
    {
        return request.Query.TryGetValue("format", out StringValues formats)
            ? formats
                .Select<string, MediaFormat?>(s =>
                    Enum.TryParse(s, true, out MediaFormat mediaFormat)
                        ? mediaFormat
                        : null)
                .OfType<MediaFormat>()
                .ToFrozenSet()
            : [];
    }

    private static (string? PrevUrl, MediaFullId? After) GetHistory(string? history, HttpRequest request)
    {
        string[] historyParts = history?.Split(',') ?? [];
        MediaFullId? after = historyParts.Length > 0
            ? MediaFullId.FromCombinedString(historyParts[^1])
            : null;
        string? prevHistory = historyParts.Length > 1
            ? string.Join(',', historyParts[..^1])
            : null;
        Dictionary<string, StringValues> query = QueryHelpers.ParseQuery(request.QueryString.Value);
        if (string.IsNullOrWhiteSpace(history))
        {
            query.Remove("history");
        }
        else
        {
            query["history"] = prevHistory;
        }
        string? prevUrl = historyParts.Length > 0
            ? QueryHelpers.AddQueryString(request.Path, query)
            : null;
        return (prevUrl, after);
    }

    private static string? GetNextUrl(string? history, HttpRequest request,
        List<MediaEntry> entries, int pageSize)
    {
        string[] historyParts = history?.Split(',') ?? [];
        string? nextHistory = entries.Count > 0 && entries.Count == pageSize
            ? string.Join(',', [.. historyParts, entries[^1].Id.ToCombinedString()])
            : null;
        Dictionary<string, StringValues> query = QueryHelpers.ParseQuery(request.QueryString.Value);
        query["history"] = nextHistory;
        string? nextUrl = !string.IsNullOrWhiteSpace(nextHistory)
            ? QueryHelpers.AddQueryString(request.Path, query)
            : null;
        return nextUrl;
    }

    private static OpdsFeed CreateOpdsFeed(string opdsVersion, string selfUrl, IEnumerable<MediaEntry> entries,
       TimeProvider timeProvider,
       bool isAcquisitionFeed, bool isPaginatedFeed,
       string? prevUrl, string? nextUrl)
    {
        string modified = timeProvider.GetUtcNow().UtcDateTime.ToString(DateTimeFormatting.Iso8601);
        OpdsFeed feed = new()
        {
            Title = "umm OPDS Feed",
            Modified = modified,
            Self = selfUrl,
            Prev = prevUrl,
            Next = nextUrl,
            NavigationEntries = isAcquisitionFeed
                ? []
                : [..entries.Select(mediaEntry => new OpdsNavigationEntry()
                    {
                        Identifier = $"urn:umm:{mediaEntry.Id.ToCombinedString()}",
                        Title = mediaEntry.Metadata.Title,
                        NavigationLink = new()
                        {
                            Href = $"/opds/v{opdsVersion}/advancedsearch/{nameof(MediaFullId.VendorId)}={mediaEntry.Id.VendorId}&{nameof(MediaFullId.ContentId)}={mediaEntry.Id.ContentId}/?acquisition=true&paginated={isPaginatedFeed}",
                            Title = mediaEntry.Metadata.Title,
                        },
                        Modified = modified,
                        ImageLinks = [..GetResourceLinks(mediaEntry, true)],
                    })
                ],
            AcquisitionEntries = isAcquisitionFeed
                ? [..entries.Select(mediaEntry => new OpdsAcquisitionEntry()
                    {
                        Identifier = $"urn:umm:{mediaEntry.Id.ToCombinedString()}",
                        Title = mediaEntry.Metadata.Title,
                        Modified = modified,
                        Creators = mediaEntry.Metadata.Creators,
                        Description = mediaEntry.Metadata.Description,
                        Tags = [..mediaEntry.Tags],
                        ImageLinks = [..GetResourceLinks(mediaEntry, true)],
                        AcquisitionLinks = [..GetResourceLinks(mediaEntry, false)],
                    })
                ]
                : [],
        };
        return feed;
    }

    private static IEnumerable<OpdsResourceLink> GetResourceLinks(MediaEntry mediaEntry, bool isImage)
    {
        return mediaEntry.ExportTargets
            .Where(exportTarget => isImage == exportTarget.MediaFormats.Contains(MediaFormat.Artwork))
            .Select(exportTarget => new OpdsResourceLink()
            {
                Href = GetDownloadUrl(mediaEntry, exportTarget),
                Type = exportTarget.MediaType,
                Title = exportTarget.ExportId,
            });
    }

    private static string GetDownloadUrl(MediaEntry mediaEntry, MediaExportTarget exportTarget)
    {
        string downloadPath = string.Join('/', new[]
            {
                exportTarget.ExportId,
                mediaEntry.Id.VendorId,
                mediaEntry.Id.ContentId,
                mediaEntry.Id.PartId,
            }.Where(s => s.Length > 0));
        return $"/download/{downloadPath}";
    }
}
