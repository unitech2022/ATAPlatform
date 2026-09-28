using System.Collections.Concurrent;

namespace ATA.Infrastructure.Locking;

/// <summary>Named lock for background jobs (<c>lock:job:{name}</c>). In-memory for the single-node deployment; Redis in F21.</summary>
public interface IDistributedLock
{
    /// <summary>Returns a handle when the lock was acquired, or <c>null</c> when another holder has it.</summary>
    Task<IAsyncDisposable?> TryAcquireAsync(string key, TimeSpan expiry, CancellationToken ct);
}

public sealed class InMemoryDistributedLock : IDistributedLock
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.Ordinal);

    public async Task<IAsyncDisposable?> TryAcquireAsync(string key, TimeSpan expiry, CancellationToken ct)
    {
        var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        return await semaphore.WaitAsync(TimeSpan.Zero, ct) ? new Releaser(semaphore) : null;
    }

    private sealed class Releaser(SemaphoreSlim semaphore) : IAsyncDisposable
    {
        private int _released;

        public ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
            {
                semaphore.Release();
            }

            return ValueTask.CompletedTask;
        }
    }
}
