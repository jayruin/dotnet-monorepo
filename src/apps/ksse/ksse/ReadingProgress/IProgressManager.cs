using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ksse.ReadingProgress;

internal interface IProgressManager
{
    Task<ProgressDocument?> GetAsync(string user, string hash, CancellationToken cancellationToken = default);
    IAsyncEnumerable<ProgressDocument> GetAllAsync(string user);
    Task PutAsync(ProgressDocument progress, CancellationToken cancellationToken = default);
    Task DeleteAsync(string user, string hash, CancellationToken cancellationToken = default);
    Task DeleteAllAsync(string user, CancellationToken cancellationToken = default);
}
