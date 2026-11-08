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
using umm.Server;

namespace umm.Plugins.Opds;

internal static class OpdsV1Endpoints
{
    private const string Version = "1.2";

    public static void MapOpdsV1Endpoints(RouteGroupBuilder opdsGroup)
    {
        RouteGroupBuilder v1Group = opdsGroup.MapGroup($"v{Version}");
        v1Group.MapGet("advancedsearch/{queryString?}", GetAdvancedSearchOpdsFeed);
        v1Group.MapGet("opensearch", GetOpenSearchOpdsFeed);
        v1Group.MapGet("opensearch/description", GetOpenSearchDescriptionDocument);
        v1Group.MapGet("recipes/{id}", GetRecipeOpdsFeed);
    }

    private static async Task<IResult> GetAdvancedSearchOpdsFeed(
        string? queryString,
        HttpRequest request,
        IOptionsSnapshot<OpdsOptions> options,
        IMediaCatalog catalog, IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping, TimeProvider timeProvider,
        [FromQuery(Name = "paginated")] bool isPaginated = false,
        [FromQuery(Name = "acquisition")] bool isAcquisition = false,
        [FromQuery] string? history = null,
        CancellationToken cancellationToken = default)
    {
        FrozenSet<MediaFormat> mediaFormats = Navigation.GetMediaFormatsFromQuery(request);
        Dictionary<string, StringValues> searchQuery = QueryHelpers.ParseQuery(queryString);
        (string? prevUrl, MediaFullId? after) = Navigation.GetHistory(history, request);
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
        string? nextUrl = Navigation.GetNextUrl(history, request, entries, pageSize);
        OpdsFeed feed = OpdsFeedHandler.CreateOpdsFeed(Version, request.Path + request.QueryString,
            entries, timeProvider,
            isAcquisition, isPaginated,
            prevUrl, nextUrl);
        OpdsSerializerFeedOptionsV1_2 serializerOptions = new();
        return TypedResults.Stream(
            stream => OpdsSerializerV1_2.WriteFeedAsync(stream, feed, serializerOptions, cancellationToken),
            OpdsSerializerV1_2.FeedMediaType,
            $"opds_v{Version}{OpdsSerializerV1_2.FeedFileExtension}");
    }

    private static async Task<IResult> GetOpenSearchOpdsFeed(
        HttpRequest request,
        IOptionsSnapshot<OpdsOptions> options,
        IMediaCatalog catalog, IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping, TimeProvider timeProvider,
        [FromQuery(Name = "q")] string? searchTerm = null,
        [FromQuery(Name = "paginated")] bool isPaginated = false,
        [FromQuery(Name = "acquisition")] bool isAcquisition = false,
        [FromQuery] string? history = null,
        CancellationToken cancellationToken = default)
    {
        FrozenSet<MediaFormat> mediaFormats = Navigation.GetMediaFormatsFromQuery(request);
        (string? prevUrl, MediaFullId? after) = Navigation.GetHistory(history, request);
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
        string? nextUrl = Navigation.GetNextUrl(history, request, entries, pageSize);
        OpdsFeed feed = OpdsFeedHandler.CreateOpdsFeed(Version, request.Path + request.QueryString,
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
            $"opds_v{Version}{OpdsSerializerV1_2.FeedFileExtension}");
    }

    private static async Task<IResult> GetOpenSearchDescriptionDocument(
        HttpRequest request,
        CancellationToken cancellationToken = default)
    {
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
                $"umm OPDS v{Version} Search", $"umm OPDS v{Version} Open Search Description",
                cancellationToken),
            OpdsSerializerV1_2.OpenSearchDescriptionDocumentMediaType,
            $"opds_v{Version}_opensearch_description{OpdsSerializerV1_2.OpenSearchDescriptionDocumentFileExtension}");
    }

    private static async Task<IResult> GetRecipeOpdsFeed(
        string id,
        HttpRequest request,
        IOptionsSnapshot<OpdsOptions> options, IOpdsRecipeStorage recipeStorage,
        IMediaCatalog catalog, IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping, TimeProvider timeProvider,
        [FromQuery] string? history = null,
        CancellationToken cancellationToken = default)
    {
        OpdsRecipe? recipe = await recipeStorage.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (recipe is null) return TypedResults.NotFound();
        (string? prevUrl, MediaFullId? after) = Navigation.GetHistory(history, request);
        int pageSize = options.Value.PageSize;
        SearchOptions searchOptions = new()
        {
            IncludeParts = recipe.Acquisition,
            Pagination = recipe.Paginated
                ? new()
                {
                    After = after,
                    Count = pageSize,
                }
                : null,
            MediaFormats = [.. recipe.MediaFormats],
        };
        List<MediaEntry> entries = await catalog.EnumerateAsync(recipe.SearchQuery, searchOptions, cancellationToken)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        string? nextUrl = Navigation.GetNextUrl(history, request, entries, pageSize);
        OpdsFeed feed = OpdsFeedHandler.CreateOpdsFeed(Version, request.Path + request.QueryString,
            entries, timeProvider,
            recipe.Acquisition, recipe.Paginated,
            prevUrl, nextUrl);
        OpdsSerializerFeedOptionsV1_2 serializerOptions = new();
        return TypedResults.Stream(
            stream => OpdsSerializerV1_2.WriteFeedAsync(stream, feed, serializerOptions, cancellationToken),
            OpdsSerializerV1_2.FeedMediaType,
            $"opds_v{Version}{OpdsSerializerV1_2.FeedFileExtension}");
    }
}
