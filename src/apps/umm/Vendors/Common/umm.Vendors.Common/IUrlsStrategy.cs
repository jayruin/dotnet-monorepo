using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace umm.Vendors.Common;

public interface IUrlsStrategy<TMetadata>
{
    Task<ImmutableArray<string>> GetUrlsAsync(TMetadata metadata, CancellationToken cancellationToken);
}
