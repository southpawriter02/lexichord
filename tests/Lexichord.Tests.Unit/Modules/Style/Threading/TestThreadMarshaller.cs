using Lexichord.Abstractions.Contracts;

namespace Lexichord.Tests.Unit.Modules.Style.Threading;

/// <summary>
/// Test implementation of IThreadMarshaller for unit tests.
/// </summary>
/// <remarks>
/// LOGIC: Executes all actions synchronously on the calling thread.
/// Provides <see cref="SimulateUIThread"/> property to control
/// the behavior of thread assertions for testing purposes.
///
/// Version: v0.2.7a
/// </remarks>
internal class TestThreadMarshaller : IThreadMarshaller
{
    /// <summary>
    /// Controls whether <see cref="IsOnUIThread"/> returns true or false.
    /// </summary>
    public bool SimulateUIThread { get; set; } = true;

    /// <inheritdoc/>
    public bool IsOnUIThread => SimulateUIThread;

    /// <inheritdoc/>
    public Task InvokeOnUIThreadAsync(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        action();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<T> InvokeOnUIThreadAsync<T>(Func<T> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        return Task.FromResult(func());
    }

    /// <inheritdoc/>
    public void PostToUIThread(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        action();
    }

    /// <inheritdoc/>
    public void AssertUIThread(string operation)
    {
        if (!SimulateUIThread)
            throw new InvalidOperationException(
                $"{operation} must run on UI thread");
    }

    /// <inheritdoc/>
    public void AssertBackgroundThread(string operation)
    {
        if (SimulateUIThread)
            throw new InvalidOperationException(
                $"{operation} must run on background thread");
    }
}
