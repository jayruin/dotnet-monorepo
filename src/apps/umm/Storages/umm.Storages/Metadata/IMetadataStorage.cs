using System.Threading;
using System.Threading.Tasks;
using umm.Library;

namespace umm.Storages.Metadata;

public interface IMetadataStorage : IMediaStorage
{
    Task<bool> ContainsAsync(MediaMainId id, string key, CancellationToken cancellationToken = default);
    Task SaveAsync<TMetadata>(MediaMainId id, string key, TMetadata metadata, CancellationToken cancellationToken = default)
        where TMetadata : ISerializableMetadata<TMetadata>;
    Task<TMetadata> GetAsync<TMetadata>(MediaMainId id, string key, CancellationToken cancellationToken = default)
        where TMetadata : ISerializableMetadata<TMetadata>;
    Task DeleteAsync(MediaMainId id, string key, CancellationToken cancellationToken = default);
}
