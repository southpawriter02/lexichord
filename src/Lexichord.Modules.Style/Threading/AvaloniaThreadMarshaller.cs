using Avalonia.Threading;
using Lexichord.Abstractions.Contracts.Threading;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Style.Threading;

/// <summary>
/// Avalonia-specific implementation of IThreadMarshaller.
/// </summary>
/// <remarks>
/// LOGIC: Bridges the IThreadMarshaller abstraction to Avalonia's Dispatcher.
/// This enables the linting system to safely marshal results back to the UI
/// thread for rendering without coupling to Avalonia's specifics.
///
/// Thread Safety:
/// - All methods are thread-safe
/// - IsOnUIThread property is safe to read from any thread
/// - Dispatch operations are queued and executed serially on UI thread
///
/// Performance:
/// - When already on UI thread, operations execute immediately
/// - PostToUIThread is fire-and-forget (no await overhead)
/// - Minimal logging in hot paths to reduce overhead
///
/// Version: v0.2.7a
/// </remarks>
public sealed class AvaloniaThreadMarshaller : IThreadMarshaller
{
    private readonly ILogger<AvaloniaThreadMarshaller> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AvaloniaThreadMarshaller"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostics.</param>
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

        // LOGIC: Dispatch to UI thread and await completion
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

        _logger.LogDebug(
            "Posting action to UI thread from thread {ThreadId}",
            Environment.CurrentManagedThreadId);

        // LOGIC: Fire-and-forget post to UI thread
        // Using discard to acknowledge we're intentionally not awaiting
        _ = Dispatcher.UIThread.InvokeAsync(action, DispatcherPriority.Background);
    }

    /// <inheritdoc/>
    public void AssertUIThread(string operation)
    {
#if DEBUG
        if (!IsOnUIThread)
        {
            var message = $"Operation '{operation}' must be called on the UI thread. " +
                          $"Current thread: {Environment.CurrentManagedThreadId}";

            _logger.LogError(message);
            throw new InvalidOperationException(message);
        }
#endif
    }

    /// <inheritdoc/>
    public void AssertBackgroundThread(string operation)
    {
#if DEBUG
        if (IsOnUIThread)
        {
            var message = $"Operation '{operation}' must NOT be called on the UI thread. " +
                          "Offload CPU-intensive work to a background thread using Task.Run.";

            _logger.LogError(message);
            throw new InvalidOperationException(message);
        }
#endif
    }
}
