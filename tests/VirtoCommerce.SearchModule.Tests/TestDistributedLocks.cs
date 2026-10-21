#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VirtoCommerce.Platform.Core.DistributedLock;

namespace VirtoCommerce.SearchModule.Tests;

// A no-op lock handle for tests. Disposing it releases nothing; HandleLostToken never fires.
internal sealed class FakeLockHandle(string resource) : IDistributedLockHandle
{
    public string Resource { get; } = resource;
    public CancellationToken HandleLostToken => CancellationToken.None;
    public void Dispose() { }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

// Always grants the lock: the guarded action runs. Mirrors a free resource.
internal sealed class AcquiringDistributedLock : IDistributedLock
{
    public Task<IDistributedLockHandle> AcquireAsync(string resource, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        => Task.FromResult<IDistributedLockHandle>(new FakeLockHandle(resource));

    public Task<IDistributedLockHandle?> TryAcquireAsync(string resource, TimeSpan timeout = default, CancellationToken cancellationToken = default)
        => Task.FromResult<IDistributedLockHandle?>(new FakeLockHandle(resource));
}

// The resource is held by another holder: TryAcquire yields null, so TryExecuteAsync returns false and the
// guarded work is skipped without throwing - exactly the contended-lock path under test.
internal sealed class HeldDistributedLock : IDistributedLock
{
    public Task<IDistributedLockHandle> AcquireAsync(string resource, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("The test lock is held; blocking Acquire is not used by these tests.");

    public Task<IDistributedLockHandle?> TryAcquireAsync(string resource, TimeSpan timeout = default, CancellationToken cancellationToken = default)
        => Task.FromResult<IDistributedLockHandle?>(null);
}

// Records log entries so tests can assert that a skip/failure was made VISIBLE in the logs.
internal sealed class CapturingLogger<T> : ILogger<T>
{
    public readonly List<(LogLevel Level, string Message, Exception? Exception)> Entries = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => Entries.Add((logLevel, formatter(state, exception), exception));

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
