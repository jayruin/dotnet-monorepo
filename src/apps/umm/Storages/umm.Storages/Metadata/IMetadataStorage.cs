using System.Threading;
using System.Threading.Tasks;

namespace umm.Storages.Metadata;

public interface IMetadataStorage : IMediaStorage
{
    Task<bool> ContainsAsync(string vendorId, string contentId, string key, CancellationToken cancellationToken = default);
    Task SaveAsync<TMetadata>(string vendorId, string contentId, string key, TMetadata metadata, CancellationToken cancellationToken = default)
        where TMetadata : ISerializableMetadata<TMetadata>;
    Task<TMetadata> GetAsync<TMetadata>(string vendorId, string contentId, string key, CancellationToken cancellationToken = default)
        where TMetadata : ISerializableMetadata<TMetadata>;
    Task DeleteAsync(string vendorId, string contentId, string key, CancellationToken cancellationToken = default);
}
