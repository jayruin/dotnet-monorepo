using System.Collections.Frozen;

namespace umm.HashCache;

public sealed class HashCacheOptions
{
    public required FrozenSet<string> MediaTypes { get; init; }
}
