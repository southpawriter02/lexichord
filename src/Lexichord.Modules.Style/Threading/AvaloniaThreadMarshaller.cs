using System.Diagnostics;
using Avalonia.Threading;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Style.Threading;

/// <summary>
/// Avalonia implementation of IThreadMarshaller using Dispatcher.UIThread.
/// </summary>
/// <remarks>
/// LOGIC: This implementation bridges the abstraction layer to Avalonia's
/// actual threading infrastructure. The Dispatcher.UIThread provides
/// access to the main Avalonia event loop.
///
/// Thread Safety:
/// - All methods can be safely called from any thread
/// - Dispatcher.UIThread.InvokeAsync is inherently thread-safe
/// - CheckAccess() is thread-safe
///
/// Version: v0.2.7a
/// </remarks>
public sealed class AvaloniaThreadMarshaller : IThreadMarshaller
{
    private readonly ILogger<AvaloniaThreadMarshaller> _logger;

    public AvaloniaThreadMarshaller(ILogger<AvaloniaThreadMarshaller> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public bool IsOnUIThread => Dispatcher.UIThread.CheckAccess();

    /// <inheritdoc/>
    public async Task InvokeOnUIThreadAsync(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (IsOnUIThread)
        {
            // LOGIC: Already on UI thread, execute directly
            action();
            return;
        }

        _logger.LogDebug(
            "Marshalling action to UI thread from thread {ThreadId}",
            Environment.CurrentManagedThreadId);

        // LOGIC: Dispatch to UI thread and await completion
        await Dispatcher.UIThread.InvokeAsync(action);
    }

    /// <inheritdoc/>
    public async Task<T> InvokeOnUIThreadAsync<T>(Func<T> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        if (IsOnUIThread)
        {
            // LOGIC: Already on UI thread, execute directly
            return func();
        }

        _logger.LogDebug(
            "Marshalling function to UI thread from thread {ThreadId}",
            Environment.CurrentManagedThreadId);

        // LOGIC: Dispatch to UI thread and await result
        return await Dispatcher.UIThread.InvokeAsync(func);
    }

    /// <inheritdoc/>
    public void PostToUIThread(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (IsOnUIThread)
        {
            // LOGIC: Already on UI thread, execute directly
            action();
            return;
        }

        // LOGIC: Fire-and-forget dispatch
        // Wrap in try-catch to log exceptions since they won't propagate
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in posted UI action");
            }
        });
    }

    /// <inheritdoc/>
    [Conditional("DEBUG")]
    public void AssertUIThread(string operation)
    {
        if (!IsOnUIThread)
        {
            var message = $"{operation} must run on UI thread but was called on thread {Environment.CurrentManagedThreadId}";
            _logger.LogWarning("{Message}", message);
            throw new InvalidOperationException(message);
        }
    }

    /// <inheritdoc/>
    [Conditional("DEBUG")]
    public void AssertBackgroundThread(string operation)
    {
        if (IsOnUIThread)
        {
            var message = $"{operation} must run on background thread but was called on UI thread";
            _logger.LogWarning("{Message}", message);
            throw new InvalidOperationException(message);
        }
    }
}
