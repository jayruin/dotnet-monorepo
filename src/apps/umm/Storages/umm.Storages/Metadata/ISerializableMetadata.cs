using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace umm.Storages.Metadata;

public interface ISerializableMetadata<TSelf>
    where TSelf : ISerializableMetadata<TSelf>
{
    static abstract Task<TSelf> FromJsonAsync(Stream stream, CancellationToken cancellationToken = default);
    Task ToJsonAsync(Stream stream, CancellationToken cancellationToken = default);
}
