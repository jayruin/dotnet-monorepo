using System.Collections.Immutable;
using System.Linq;

namespace umm.Library;

public sealed class MetadataSearchField
{
    public required ImmutableArray<string> Aliases
    {
        get;
        init
        {
            field = [.. value.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct()];
        }
    }
    public required ImmutableArray<string> Values
    {
        get;
        init
        {
            field = [.. value.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct()];
        }
    }
    public required bool ExactMatch { get; init; }
}
