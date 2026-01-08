using System;
using System.Threading;
using System.Threading.Tasks;

namespace Utils;

public static class SemaphoreSlimExtensions
{
    extension(SemaphoreSlim semaphoreSlim)
    {
        public async Task<IDisposable> EnterScopeAsync(CancellationToken cancellationToken = default)
        {
            await semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);
            return new SemaphoreSlimScope(semaphoreSlim);
        }

        public IDisposable EnterScope()
        {
            semaphoreSlim.Wait();
            return new SemaphoreSlimScope(semaphoreSlim);
        }
    }

    private sealed class SemaphoreSlimScope : IDisposable
    {
        private readonly SemaphoreSlim _semaphoreSlim;
        private bool _disposed;

        public SemaphoreSlimScope(SemaphoreSlim semaphoreSlim)
        {
            _semaphoreSlim = semaphoreSlim;
        }

        void IDisposable.Dispose()
        {
            if (_disposed) return;
            _semaphoreSlim.Release();
            _disposed = true;
        }
    }
}
