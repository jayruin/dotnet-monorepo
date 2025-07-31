using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using umm.Storages.Metadata;

namespace umm.Vendors.Common;

public interface IMetadataOnlyUpdateHandlerStrategy<TMetadata>
    where TMetadata : ISerializableMetadata<TMetadata>, ISearchableMetadata, IUpdatableMetadata<TMetadata>
{
    string VendorId { get; }
    IMetadataStorage MetadataStorage { get; }
    ILogger Logger { get; }
    string MetadataKey { get; }
    IAsyncEnumerable<string> EnumerateLocalContentIdsAsync(IReadOnlyDictionary<string, StringValues> searchQuery, CancellationToken cancellationToken);
    bool ShouldUpdate(TMetadata remoteMetadata, TMetadata localMetadata);
    Task<TMetadata> GetLatestMetadataAsync(string contentId, CancellationToken cancellationToken);
}
