using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    internal sealed class NoopDbTransaction : IDbContextTransaction, IAsyncDisposable
    {
        public Guid TransactionId => Guid.Empty;
        public void Commit() { }
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Rollback() { }
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    internal sealed class TimedDbTransaction : IDbContextTransaction, IAsyncDisposable
    {
        private readonly IDbContextTransaction _inner;
        private readonly ILogger? _logger;
        private readonly Application.Interfaces.IAppMetrics? _metrics;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _watcher;
        private volatile bool _completed;
        private bool _disposed;
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, TimedDbTransaction> _registry = new();
        public DateTime StartTime { get; }
        public bool HasTimedOut { get; private set; }

        public TimedDbTransaction(IDbContextTransaction inner, int timeoutMs, ILogger? logger = null, Application.Interfaces.IAppMetrics? metrics = null)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _logger = logger;
            _metrics = metrics;
            StartTime = DateTime.UtcNow;
            HasTimedOut = false;
            _completed = false;
            _registry.TryAdd(_inner.TransactionId, this);
            _watcher = Task.Run(async () =>
            {
                try
                {
                    var half = timeoutMs / 2;
                    if (half > 0)
                    {
                        try { await Task.Delay(half, _cts.Token); _logger?.LogWarning("Transaction running >50% of allowed time ({Half}ms)", half); } catch (OperationCanceledException) { return; }
                    }

                    await Task.Delay(timeoutMs - (half > 0 ? half : 0), _cts.Token);
                    HasTimedOut = true;
                    try { _logger?.LogError("Transaction exceeded timeout of {Timeout}ms and will be rolled back", timeoutMs); } catch { /* best-effort logging */ }
                    try { _metrics?.Increment("transaction.timeout"); } catch { /* best-effort metrics */ }
                    try
                    {
                        lock (this)
                        {
                            if (!_completed)
                            {
                                _inner.RollbackAsync(CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
                            }
                        }
                    }
                    catch (Exception ex) { _logger?.LogError(ex, "Rollback after transaction timeout failed"); }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            });
        }

        public Guid TransactionId => _inner.TransactionId;

        public static bool TryGet(Guid id, out TimedDbTransaction? t)
        {
            return _registry.TryGetValue(id, out t);
        }

        public void Commit()
        {
            lock (this)
            {
                if (_completed) throw new InvalidOperationException("Transaction already completed");
                _inner.Commit();
                _completed = true;
            }
            _cts.Cancel();
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            lock (this)
            {
                if (_completed) throw new InvalidOperationException("Transaction already completed");
            }
            await _inner.CommitAsync(cancellationToken);
            lock (this) { _completed = true; }
            _cts.Cancel();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try
            {
                _cts.Cancel();
                _watcher.Wait(100);
            }
            catch (AggregateException ae)
            {
                foreach (var ex in ae.InnerExceptions) _logger?.LogWarning(ex, "Exception while waiting for transaction watcher");
            }
            finally
            {
                try { _inner.Dispose(); } catch (Exception ex) { _logger?.LogWarning(ex, "Failed disposing inner transaction"); }
                _registry.TryRemove(_inner.TransactionId, out _);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            try
            {
                _cts.Cancel();
                await _watcher.WaitAsync(TimeSpan.FromMilliseconds(100));
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Exception while awaiting watcher during DisposeAsync");
            }
            try { await _inner.DisposeAsync(); } catch (Exception ex) { _logger?.LogWarning(ex, "Failed disposing inner transaction"); }
            _registry.TryRemove(_inner.TransactionId, out _);
        }

        public void Rollback()
        {
            lock (this)
            {
                if (_completed) throw new InvalidOperationException("Transaction already completed");
                _inner.Rollback();
                _completed = true;
            }
            _cts.Cancel();
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            lock (this)
            {
                if (_completed) throw new InvalidOperationException("Transaction already completed");
            }
            await _inner.RollbackAsync(cancellationToken);
            lock (this) { _completed = true; }
            _cts.Cancel();
        }
    }
}
