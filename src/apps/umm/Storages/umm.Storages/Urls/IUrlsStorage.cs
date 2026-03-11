using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using umm.Library;

namespace umm.Storages.Urls;

public interface IUrlsStorage : IMediaStorage
{
    Task SaveAsync(MediaFullId id, IReadOnlyList<string> urls, CancellationToken cancellationToken = default);
    Task<ImmutableArray<string>> GetAsync(MediaFullId id, CancellationToken cancellationToken = default);
}
