using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
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

    public IAsyncEnumerable<ProgressDocument> GetAllAsync(string user)
    {
        return _dbContext.ProgressDocuments
            .AsNoTracking()
            .Where(p => p.User == user)
            .AsAsyncEnumerable();
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
            _dbContext.Update(progress);
        }
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task DeleteAsync(string user, string hash, CancellationToken cancellationToken = default)
    {
        _dbContext.RemoveRange(_dbContext.ProgressDocuments.Where(p => p.User == user && p.Hash == hash));
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task DeleteAllAsync(string user, CancellationToken cancellationToken = default)
    {
        _dbContext.RemoveRange(_dbContext.ProgressDocuments.Where(p => p.User == user));
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
