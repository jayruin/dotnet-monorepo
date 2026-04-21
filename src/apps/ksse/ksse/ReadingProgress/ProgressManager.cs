using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace ksse.ReadingProgress;

internal sealed class ProgressManager : IProgressManager
{
    private readonly ProgressDbContext _dbContext;

    public ProgressManager(ProgressDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ProgressDocument?> GetAsync(string user, string hash, CancellationToken cancellationToken = default)
    {
        return _dbContext.ProgressDocuments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.User == user && p.Hash == hash, cancellationToken);
    }

    public async Task PutAsync(ProgressDocument progress, CancellationToken cancellationToken = default)
    {
        ProgressDocument? existingProgressDocument = await GetAsync(progress.User, progress.Hash, cancellationToken).ConfigureAwait(false);
        if (existingProgressDocument is null)
        {
            await _dbContext.AddAsync(progress, cancellationToken);
        }
        else
        {
            //_dbContext.Entry(existingProgressDocument).CurrentValues.SetValues(progress);
            _dbContext.Update(progress);
        }
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
