using Avalonia.Threading;

namespace Lexichord.Host.Services;

/// <summary>
/// Abstraction for UI thread dispatching.
/// </summary>
/// <remarks>
/// LOGIC: This abstraction allows unit tests to bypass Avalonia's dispatcher
/// without requiring a full UI thread setup. In production, the default
/// implementation uses Dispatcher.UIThread. In tests, a passthrough
/// implementation can be injected.
/// </remarks>
public interface IUiDispatcher
{
    /// <summary>
    /// Invokes a function on the UI thread.
    /// </summary>
    Task<TResult> InvokeAsync<TResult>(Func<TResult> callback);

    /// <summary>
    /// Invokes an action on the UI thread.
    /// </summary>
    Task InvokeAsync(Action callback);
}

/// <summary>
/// Default implementation using Avalonia's Dispatcher.UIThread.
/// </summary>
public sealed class AvaloniaUiDispatcher : IUiDispatcher
{
    /// <inheritdoc />
    public async Task<TResult> InvokeAsync<TResult>(Func<TResult> callback)
    {
        return await Dispatcher.UIThread.InvokeAsync(callback);
    }

    /// <inheritdoc />
    public async Task InvokeAsync(Action callback)
    {
        await Dispatcher.UIThread.InvokeAsync(callback);
    }
}

/// <summary>
/// Passthrough implementation for unit testing.
/// </summary>
/// <remarks>
/// LOGIC: Executes callbacks synchronously without any dispatching,
/// enabling unit tests to run without an Avalonia UI thread.
/// </remarks>
public sealed class SynchronousDispatcher : IUiDispatcher
{
    /// <inheritdoc />
    public Task<TResult> InvokeAsync<TResult>(Func<TResult> callback)
    {
        return Task.FromResult(callback());
    }

    /// <inheritdoc />
    public Task InvokeAsync(Action callback)
    {
        callback();
        return Task.CompletedTask;
    }
}
