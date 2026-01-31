using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Abstract base class for ViewModels that manage subscription lifecycles.
/// </summary>
/// <remarks>
/// LOGIC: Provides automatic subscription disposal to prevent memory leaks.
/// ViewModels inherit from this class and use Track() to register subscriptions.
/// When Dispose() is called, all tracked subscriptions are automatically cleaned up.
/// 
/// Usage pattern:
/// 1. Inherit from DisposableViewModel
/// 2. Use Track() for each subscription (events, Rx observables, etc.)
/// 3. Subscribe to DocumentClosedEvent to trigger Dispose() on document close
/// 4. Override OnDisposed() for additional cleanup if needed
/// 
/// Example:
/// <code>
/// public class MyViewModel : DisposableViewModel
/// {
///     public MyViewModel(IMediator mediator, string documentId)
///     {
///         Track(mediator.CreateStream&lt;DocumentChangedEvent&gt;()
///             .Where(e => e.DocumentId == documentId)
///             .Subscribe(OnDocumentChanged));
///             
///         // Self-dispose when document closes
///         Track(mediator.CreateStream&lt;DocumentClosedEvent&gt;()
///             .Where(e => e.DocumentId == documentId)
///             .Take(1)
///             .Subscribe(_ => Dispose()));
///     }
/// }
/// </code>
/// 
/// Version: v0.3.7d
/// </remarks>
public abstract class DisposableViewModel : ObservableObject, IDisposable
{
    private readonly IDisposableTracker _tracker = new DisposableTracker();
    private bool _disposed;

    /// <summary>
    /// Track a subscription for automatic disposal.
    /// </summary>
    /// <param name="subscription">The subscription to track.</param>
    /// <exception cref="ObjectDisposedException">Thrown when ViewModel has been disposed.</exception>
    protected void Track(IDisposable subscription)
    {
        ThrowIfDisposed();
        _tracker.Track(subscription);
    }

    /// <summary>
    /// Track multiple subscriptions for automatic disposal.
    /// </summary>
    /// <param name="subscriptions">The subscriptions to track.</param>
    /// <exception cref="ObjectDisposedException">Thrown when ViewModel has been disposed.</exception>
    protected void TrackAll(params IDisposable[] subscriptions)
    {
        ThrowIfDisposed();
        _tracker.TrackAll(subscriptions);
    }

    /// <summary>
    /// Gets the current number of tracked subscriptions.
    /// </summary>
    /// <remarks>
    /// LOGIC: Useful for testing and debugging to verify subscriptions are being tracked.
    /// </remarks>
    protected int SubscriptionCount => _tracker.Count;

    /// <summary>
    /// Gets whether this ViewModel has been disposed.
    /// </summary>
    public bool IsDisposed => _disposed;

    /// <summary>
    /// Disposes all tracked subscriptions and marks the ViewModel as disposed.
    /// </summary>
    /// <remarks>
    /// LOGIC: This method is idempotent - calling it multiple times is safe.
    /// After disposal, any attempt to track new subscriptions will throw ObjectDisposedException.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _tracker.DisposeAll();
        OnDisposed();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Called after all subscriptions have been disposed.
    /// </summary>
    /// <remarks>
    /// LOGIC: Override this method to perform additional cleanup such as:
    /// - Clearing cached data
    /// - Releasing unmanaged resources
    /// - Logging disposal for diagnostics
    /// </remarks>
    protected virtual void OnDisposed()
    {
    }

    /// <summary>
    /// Throws ObjectDisposedException if this ViewModel has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }
    }
}
