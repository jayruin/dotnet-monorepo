using System.Collections.Frozen;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace umm.HashCache;

public interface IMultiHashProvider
{
    FrozenSet<string> SupportedHashFunctionNames { get; }
    ImmutableSortedDictionary<string, string> ComputeHashes(Stream stream);
    Task<ImmutableSortedDictionary<string, string>> ComputeHashesAsync(Stream stream, CancellationToken cancellationToken = default);
}
