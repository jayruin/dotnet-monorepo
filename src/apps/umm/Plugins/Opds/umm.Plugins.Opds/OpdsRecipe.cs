using Microsoft.Extensions.Primitives;
using System.Collections.Immutable;
using umm.Library;

namespace umm.Plugins.Opds;

internal sealed class OpdsRecipe
{
    public required string Id { get; init; }
    public required bool Paginated { get; init; }
    public required bool Acquisition { get; init; }
    public SearchTerm? SearchTerm { get; init; }
    public ImmutableSortedDictionary<string, StringValues>? SearchQuery { get; init; }
}
