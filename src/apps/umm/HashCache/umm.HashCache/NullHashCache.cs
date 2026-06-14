using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using umm.Library;

namespace umm.HashCache;

public sealed class NullHashCache : IHashCache
{
    public Task<bool> CanHandleAsync(string mediaType, CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task<ImmutableSortedDictionary<string, string>> GetHashesAsync(MediaFullId id, string exportId, CancellationToken cancellationToken = default)
        => Task.FromResult(ImmutableSortedDictionary<string, string>.Empty);

    public Task SetHashesAsync(MediaFullId id, string exportId, ImmutableSortedDictionary<string, string> hashes, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task DeleteAsync(MediaMainId id, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task ClearAsync(string vendorId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task ResetAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
