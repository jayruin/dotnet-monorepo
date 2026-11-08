using Microsoft.Extensions.Primitives;
using System.Collections.Immutable;
using umm.Library;

namespace umm.Plugins.Opds;

internal sealed class OpdsRecipe
{
    public required string Id { get; init; }
    public required ImmutableSortedDictionary<string, StringValues> SearchQuery { get; init; }
    public required ImmutableSortedSet<MediaFormat> MediaFormats { get; init; }
    public required bool Paginated { get; init; }
    public required bool Acquisition { get; init; }
}
