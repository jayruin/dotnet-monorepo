using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using umm.Library;

namespace umm.Storages;

public interface IMediaStorage
{
    bool Supports(string vendorId);
    Task<bool> ContainsAsync(MediaMainId id, CancellationToken cancellationToken = default);
    IAsyncEnumerable<MediaMainId> EnumerateContentAsync(CancellationToken cancellationToken = default);
}
