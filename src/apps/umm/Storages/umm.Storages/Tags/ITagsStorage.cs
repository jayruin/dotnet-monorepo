using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace umm.Storages.Tags;

public interface ITagsStorage : IMediaStorage
{
    Task SaveAsync(string vendorId, string contentId, IReadOnlySet<string> tags, CancellationToken cancellationToken = default);
    Task<ImmutableSortedSet<string>> GetAsync(string vendorId, string contentId, CancellationToken cancellationToken = default);
}
