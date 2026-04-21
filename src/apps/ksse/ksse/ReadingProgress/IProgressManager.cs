using System.Threading;
using System.Threading.Tasks;

namespace ksse.ReadingProgress;

internal interface IProgressManager
{
    Task<ProgressDocument?> GetAsync(string user, string hash, CancellationToken cancellationToken = default);
    Task PutAsync(ProgressDocument progress, CancellationToken cancellationToken = default);
}
