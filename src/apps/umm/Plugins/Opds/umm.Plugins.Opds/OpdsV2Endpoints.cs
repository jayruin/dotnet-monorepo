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

internal static class OpdsV2Endpoints
{
    private const string Version = "2.0";

    public static void MapOpdsV2Endpoints(RouteGroupBuilder opdsGroup)
    {
        RouteGroupBuilder v2Group = opdsGroup.MapGroup($"v{Version}");
        v2Group.MapGet("advancedsearch/{queryString?}", GetAdvancedSearchOpdsFeed);
        v2Group.MapGet("opensearch", GetSearchOpdsFeed);
        v2Group.MapGet("recipes/{id}", GetRecipeOpdsFeed);
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
        OpdsSerializerFeedOptionsV2_0 serializerOptions = new();
        return TypedResults.Stream(
            stream => OpdsSerializerV2_0.WriteFeedAsync(stream, feed, serializerOptions, cancellationToken),
            OpdsSerializerV2_0.FeedMediaType,
            $"opds_v{Version}{OpdsSerializerV2_0.FeedFileExtension}");
    }

    private static async Task<IResult> GetSearchOpdsFeed(
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
            $"opds_v{Version}{OpdsSerializerV2_0.FeedFileExtension}");
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
        OpdsSerializerFeedOptionsV2_0 serializerOptions = new();
        return TypedResults.Stream(
            stream => OpdsSerializerV2_0.WriteFeedAsync(stream, feed, serializerOptions, cancellationToken),
            OpdsSerializerV2_0.FeedMediaType,
            $"opds_v{Version}{OpdsSerializerV2_0.FeedFileExtension}");
    }
}
