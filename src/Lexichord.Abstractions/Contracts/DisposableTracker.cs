using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Thread-safe implementation of <see cref="IDisposableTracker"/>.
/// </summary>
/// <remarks>
/// LOGIC: Provides composite disposal for subscription lifecycle management.
/// Uses a lock for thread-safety since subscriptions may be added from different
/// threads (UI thread, background analysis, event handlers, etc.).
/// 
/// Exception handling:
/// - Individual subscription disposal failures are logged but don't halt disposal
/// - All subscriptions are attempted even if some fail
/// - The tracker can still be used after DisposeAll() is called, unlike full disposal
/// 
/// Version: v0.3.7d
/// </remarks>
public sealed class DisposableTracker : IDisposableTracker
{
    private readonly object _lock = new();
    private readonly List<IDisposable> _disposables = new();
    private readonly ILogger<DisposableTracker>? _logger;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="DisposableTracker"/> class.
    /// </summary>
    public DisposableTracker()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DisposableTracker"/> class with logging.
    /// </summary>
    /// <param name="logger">Logger for disposal diagnostics.</param>
    public DisposableTracker(ILogger<DisposableTracker>? logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _disposables.Count;
            }
        }
    }

    /// <inheritdoc />
    public bool IsDisposed => _isDisposed;

    /// <inheritdoc />
    public void Track(IDisposable disposable)
    {
        ArgumentNullException.ThrowIfNull(disposable);

        lock (_lock)
        {
            ThrowIfDisposed();
            _disposables.Add(disposable);
            _logger?.LogTrace("Tracked subscription, count now: {Count}", _disposables.Count);
        }
    }

    /// <inheritdoc />
    public void TrackAll(params IDisposable[] disposables)
    {
        ArgumentNullException.ThrowIfNull(disposables);

        lock (_lock)
        {
            ThrowIfDisposed();
            foreach (var disposable in disposables)
            {
                if (disposable != null)
                {
                    _disposables.Add(disposable);
                }
            }
            _logger?.LogTrace("Tracked {Added} subscriptions, count now: {Count}", 
                disposables.Length, _disposables.Count);
        }
    }

    /// <inheritdoc />
    public void DisposeAll()
    {
        List<IDisposable> toDispose;

        lock (_lock)
        {
            if (_disposables.Count == 0)
            {
                _logger?.LogTrace("DisposeAll called but no subscriptions to dispose");
                return;
            }

            toDispose = new List<IDisposable>(_disposables);
            _disposables.Clear();
        }

        _logger?.LogDebug("Disposing {Count} tracked subscriptions", toDispose.Count);

        var disposed = 0;
        var failed = 0;

        foreach (var disposable in toDispose)
        {
            try
            {
                disposable.Dispose();
                disposed++;
            }
            catch (Exception ex)
            {
                failed++;
                _logger?.LogWarning(ex, 
                    "Exception disposing subscription {Type}, continuing with remaining subscriptions",
                    disposable.GetType().Name);
            }
        }

        _logger?.LogDebug("Disposed {Disposed} subscriptions, {Failed} failed", disposed, failed);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        DisposeAll();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Throws ObjectDisposedException if this tracker has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(DisposableTracker));
        }
    }
}
