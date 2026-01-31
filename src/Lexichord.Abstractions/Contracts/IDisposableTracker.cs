using System;

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Interface for tracking disposable subscriptions with batch disposal.
/// </summary>
/// <remarks>
/// LOGIC: Implements the Composite Disposable pattern for subscription lifecycle management.
/// ViewModels and services use this to track event subscriptions and ensure proper cleanup
/// when documents are closed, preventing memory leaks during long editing sessions.
/// 
/// Key behaviors:
/// - Thread-safe: Multiple threads can safely add subscriptions
/// - Exception-resilient: Disposal of one subscription doesn't block others
/// - Idempotent: Multiple calls to DisposeAll() are safe
/// 
/// Version: v0.3.7d
/// </remarks>
public interface IDisposableTracker : IDisposable
{
    /// <summary>
    /// Track a single disposable subscription.
    /// </summary>
    /// <param name="disposable">The subscription to track.</param>
    /// <exception cref="ArgumentNullException">Thrown when disposable is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when tracker has been disposed.</exception>
    void Track(IDisposable disposable);

    /// <summary>
    /// Track multiple disposable subscriptions at once.
    /// </summary>
    /// <param name="disposables">The subscriptions to track.</param>
    /// <exception cref="ArgumentNullException">Thrown when disposables array is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when tracker has been disposed.</exception>
    void TrackAll(params IDisposable[] disposables);

    /// <summary>
    /// Dispose all tracked subscriptions and clear the collection.
    /// </summary>
    /// <remarks>
    /// LOGIC: This method is idempotent - calling it multiple times is safe.
    /// If any subscription throws during disposal, the exception is logged
    /// but disposal continues for remaining subscriptions.
    /// </remarks>
    void DisposeAll();

    /// <summary>
    /// Gets the current number of tracked subscriptions.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets whether this tracker has been disposed.
    /// </summary>
    bool IsDisposed { get; }
}
