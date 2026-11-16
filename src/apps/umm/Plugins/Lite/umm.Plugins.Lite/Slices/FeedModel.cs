using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using umm.Library;

namespace umm.Plugins.Lite.Slices;

public sealed class FeedModel
{
    public required IReadOnlyCollection<MediaEntry> MediaEntries { get; init; }
    public required string CurrentPath { get; init; }
    public required IReadOnlyDictionary<string, StringValues> Query { get; init; }
    public required string? SearchQuery { get; init; }
    public required bool IsAdvanced { get; init; }
    public required bool IsPaginated { get; init; }
    public required bool IncludeParts { get; init; }
    public required string? PrevUrl { get; init; }
    public required string? NextUrl { get; init; }
}
