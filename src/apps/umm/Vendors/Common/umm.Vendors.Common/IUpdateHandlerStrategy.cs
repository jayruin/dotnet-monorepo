using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using umm.Storages.Metadata;

namespace umm.Vendors.Common;

public interface IUpdateHandlerStrategy<TMetadata>
    where TMetadata : ISerializableMetadata<TMetadata>, ISearchableMetadata, IUpdatableMetadata<TMetadata>
{
    MediaVendorContext VendorContext { get; }
    string MetadataKey { get; }
    IAsyncEnumerable<TMetadata> EnumerateRemoteAsync(CancellationToken cancellationToken);
    Task PerformUpdateAsync(TMetadata remoteMetadata, CancellationToken cancellationToken);
    Task<bool> AttemptPerformUpdateAsync(TMetadata remoteMetadata, TMetadata localMetadata, CancellationToken cancellationToken);
}
