using System.Diagnostics;

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Marshalls operations between background and UI threads safely.
/// </summary>
/// <remarks>
/// LOGIC: IThreadMarshaller provides a testable abstraction over
/// platform-specific thread dispatching. The production implementation
/// uses Avalonia's Dispatcher.UIThread; tests use a synchronous mock.
///
/// Key Responsibilities:
/// - Invoke actions on UI thread from any thread
/// - Assert correct thread context in debug builds
/// - Provide fire-and-forget posting for non-critical updates
///
/// Thread Safety:
/// - All methods are thread-safe and can be called from any thread
/// - Multiple concurrent calls are serialized on the UI thread
///
/// Version: v0.2.7a
/// </remarks>
public interface IThreadMarshaller
{
    /// <summary>
    /// Invokes an action on the UI thread and awaits completion.
    /// </summary>
    /// <param name="action">The action to invoke on the UI thread.</param>
    /// <returns>Task that completes when action finishes on UI thread.</returns>
    /// <remarks>
    /// LOGIC: If already on UI thread, executes synchronously and returns
    /// completed task. Otherwise, dispatches to UI thread and awaits.
    ///
    /// Use this when:
    /// - You need to wait for the UI update to complete
    /// - You need to coordinate UI updates with subsequent operations
    /// </remarks>
    Task InvokeOnUIThreadAsync(Action action);

    /// <summary>
    /// Invokes a function on the UI thread and returns the result.
    /// </summary>
    /// <typeparam name="T">The return type of the function.</typeparam>
    /// <param name="func">The function to invoke on the UI thread.</param>
    /// <returns>Task with the function's return value.</returns>
    /// <remarks>
    /// LOGIC: Same as InvokeOnUIThreadAsync(Action) but captures return value.
    /// </remarks>
    Task<T> InvokeOnUIThreadAsync<T>(Func<T> func);

    /// <summary>
    /// Posts an action to the UI thread without waiting for completion.
    /// </summary>
    /// <param name="action">The action to post.</param>
    /// <remarks>
    /// LOGIC: Fire-and-forget dispatch. Use when:
    /// - Caller doesn't need to wait for completion
    /// - UI update is not time-critical
    /// - You want to avoid blocking the background thread
    ///
    /// Warning: Exceptions in the action are not propagated to caller.
    /// Ensure actions have proper error handling.
    /// </remarks>
    void PostToUIThread(Action action);

    /// <summary>
    /// Gets whether the current thread is the UI thread.
    /// </summary>
    /// <remarks>
    /// LOGIC: Returns true if called from the Avalonia UI thread.
    /// Useful for conditional logic based on thread context.
    ///
    /// Note: Prefer using AssertUIThread/AssertBackgroundThread for
    /// debugging rather than branching on this property.
    /// </remarks>
    bool IsOnUIThread { get; }

    /// <summary>
    /// Asserts that the current code is running on the UI thread.
    /// </summary>
    /// <param name="operation">Operation name for the error message.</param>
    /// <remarks>
    /// LOGIC: Debug-only assertion that throws InvalidOperationException
    /// if not on UI thread. Compiled out in release builds.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown in DEBUG builds if not on UI thread.
    /// </exception>
    [Conditional("DEBUG")]
    void AssertUIThread(string operation);

    /// <summary>
    /// Asserts that the current code is NOT on the UI thread.
    /// </summary>
    /// <param name="operation">Operation name for the error message.</param>
    /// <remarks>
    /// LOGIC: Debug-only assertion that throws InvalidOperationException
    /// if ON UI thread. Compiled out in release builds.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown in DEBUG builds if on UI thread.
    /// </exception>
    [Conditional("DEBUG")]
    void AssertBackgroundThread(string operation);
}
