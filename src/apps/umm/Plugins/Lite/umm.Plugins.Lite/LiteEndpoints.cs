using MediaTypes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using umm.Catalog;
using umm.Library;
using umm.Plugins.Lite.Slices;
using umm.Server;

namespace umm.Plugins.Lite;

public static class LiteEndpoints
{
    public static void MapLiteEndpoints(IEndpointRouteBuilder builder)
    {
        RouteGroupBuilder liteGroup = builder.MapGroup("lite");
        liteGroup.MapGet("feed", GetFeed);
        liteGroup.MapGet("entry/{id?}", GetEntry);
    }

    private static async Task<IResult> GetFeed(
        HttpRequest request,
        IOptionsSnapshot<LiteOptions> options,
        IMediaCatalog catalog, IMediaTypeFileExtensionsMapping mediaTypeFileExtensionsMapping, TimeProvider timeProvider,
        [FromQuery(Name = "search")] string? search = null,
        [FromQuery(Name = "advanced")] bool isAdvanced = false,
        [FromQuery(Name = "paginated")] bool isPaginated = false,
        [FromQuery(Name = "parts")] bool includeParts = false,
        [FromQuery] string? history = null,
        CancellationToken cancellationToken = default)
    {
        FrozenSet<MediaFormat> mediaFormats = Navigation.GetMediaFormatsFromQuery(request);
        (string? prevUrl, MediaFullId? after) = Navigation.GetHistory(history, request);
        int pageSize = options.Value.PageSize;
        SearchOptions searchOptions = new()
        {
            IncludeParts = includeParts,
            Pagination = isPaginated
                ? new()
                {
                    After = after,
                    Count = pageSize,
                }
                : null,
            MediaFormats = mediaFormats,
        };
        IAsyncEnumerable<MediaEntry> entriesStream;
        if (isAdvanced)
        {
            Dictionary<string, StringValues> searchQuery = QueryHelpers.ParseQuery(search);
            entriesStream = catalog.EnumerateAsync(searchQuery, searchOptions, cancellationToken);
        }
        else
        {
            string searchTerm = search ?? string.Empty;
            entriesStream = catalog.EnumerateAsync(searchTerm, searchOptions, cancellationToken);
        }
        List<MediaEntry> entries = await entriesStream
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        string? nextUrl = Navigation.GetNextUrl(history, request, entries, pageSize);
        FeedModel model = new()
        {
            MediaEntries = entries,
            CurrentPath = request.Path,
            Query = QueryHelpers.ParseQuery(request.QueryString.ToString()),
            SearchQuery = search,
            IsAdvanced = isAdvanced,
            IsPaginated = isPaginated,
            IncludeParts = includeParts,
            MediaFormats = mediaFormats,
            PrevUrl = prevUrl,
            NextUrl = nextUrl,
        };
        return Results.Extensions.RazorSlice<Feed, FeedModel>(model);
    }

    private static async Task<IResult> GetEntry(
        IMediaCatalog catalog,
        string? id = null,
        CancellationToken cancellationToken = default)
    {
        MediaFullId? mediaFullId = MediaFullId.FromCombinedString(id ?? string.Empty);
        if (mediaFullId is null) return TypedResults.NotFound();
        MediaEntry? mediaEntry = await catalog.GetMediaEntryAsync(mediaFullId, cancellationToken).ConfigureAwait(false);
        if (mediaEntry is null) return TypedResults.NotFound();
        return Results.Extensions.RazorSlice<Entry, MediaEntry>(mediaEntry);
    }
}
