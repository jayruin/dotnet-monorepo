using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace umm.Storages;

public interface IMediaStorage
{
    bool Supports(string vendorId);
    Task<bool> ContainsAsync(string vendorId, string contentId, CancellationToken cancellationToken = default);
    IAsyncEnumerable<(string VendorId, string ContentId)> EnumerateContentAsync(CancellationToken cancellationToken = default);
}
