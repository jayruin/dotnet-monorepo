using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using umm.Library;

namespace umm.HashCache;

public interface IHashCache
{
    Task<bool> CanHandleAsync(string mediaType, CancellationToken cancellationToken = default);
    Task<ImmutableSortedDictionary<string, string>> GetHashesAsync(MediaFullId id, string exportId, CancellationToken cancellationToken = default);
    Task SetHashesAsync(MediaFullId id, string exportId, ImmutableSortedDictionary<string, string> hashes, CancellationToken cancellationToken = default);
    Task ClearAsync(string vendorId, CancellationToken cancellationToken = default);
    Task ResetAsync(CancellationToken cancellationToken = default);
}
