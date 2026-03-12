using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using umm.Library;

namespace umm.Server;

public static class Navigation
{
    public static (string? PrevUrl, MediaFullId? After) GetHistory(string? history, HttpRequest request, string? historyKey = null)
    {
        if (string.IsNullOrWhiteSpace(historyKey))
        {
            historyKey = nameof(history);
        }
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
            query.Remove(historyKey);
        }
        else
        {
            query[historyKey] = prevHistory;
        }
        string? prevUrl = historyParts.Length > 0
            ? QueryHelpers.AddQueryString(request.Path, query)
            : null;
        return (prevUrl, after);
    }

    public static string? GetNextUrl(string? history, HttpRequest request,
        IReadOnlyList<MediaEntry> entries, int pageSize)
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

    public static FrozenSet<MediaFormat> GetMediaFormatsFromQuery(HttpRequest request)
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
}
