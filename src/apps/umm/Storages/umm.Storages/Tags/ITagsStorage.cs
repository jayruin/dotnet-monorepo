using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using umm.Library;

namespace umm.Storages.Tags;

public interface ITagsStorage : IMediaStorage
{
    Task SaveAsync(MediaMainId id, IReadOnlySet<string> tags, CancellationToken cancellationToken = default);
    Task<ImmutableSortedSet<string>> GetAsync(MediaMainId id, CancellationToken cancellationToken = default);
}
