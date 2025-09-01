namespace Solhigson.Utilities;

/// <summary>
/// Async-friendly mutual exclusion.
/// Guarantees balanced Wait/Release via disposable scopes.
/// </summary>
public sealed class AsyncLock(int maxConcurrency)
{
    private readonly SemaphoreSlim _sem = new(maxConcurrency, maxConcurrency);

    public AsyncLock() : this(1) { }

    /// <summary>Await a slot and get a scope that releases automatically.</summary>
    public async ValueTask<Releaser> AcquireAsync(CancellationToken ct = default)
    {
        await _sem.WaitAsync(ct).ConfigureAwait(false);
        return new Releaser(_sem);
    }

    /// <summary>Try to acquire within a timeout; returns (acquired, scope).</summary>
    public async ValueTask<(bool Acquired, Releaser Scope)> TryAcquireAsync(TimeSpan timeout, CancellationToken ct = default)
    {
        if (await _sem.WaitAsync(timeout, ct).ConfigureAwait(false))
            return (true, new Releaser(_sem));
        return (false, default);
    }

    /// <summary>Synchronous acquire (for sync code paths).</summary>
    public Releaser Acquire()
    {
        _sem.Wait(); // no CT here; add overload if you need it
        return new Releaser(_sem);
    }

    /// <summary>Convenience helpers to run delegates under the lock.</summary>
    public async Task WithLockAsync(Func<CancellationToken, Task> action, CancellationToken ct = default)
    {
        await _sem.WaitAsync(ct).ConfigureAwait(false);
        try { await action(ct).ConfigureAwait(false); }
        finally { _sem.Release(); }
    }

    public async Task<T> WithLockAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct = default)
    {
        await _sem.WaitAsync(ct).ConfigureAwait(false);
        try { return await action(ct).ConfigureAwait(false); }
        finally { _sem.Release(); }
    }

    /// <summary>
    /// Disposable scope that releases exactly once.
    /// Value type to avoid extra allocation; safe to default.
    /// </summary>
    public readonly struct Releaser : IDisposable, IAsyncDisposable
    {
        private readonly SemaphoreSlim? _s;
        internal Releaser(SemaphoreSlim s) => _s = s;

        public void Dispose()
        {
            // release only if we actually acquired
            _s?.Release();
        }

        public ValueTask DisposeAsync()
        {
            _s?.Release();
            return ValueTask.CompletedTask;
        }
    }
}