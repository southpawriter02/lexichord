namespace Lexichord.Abstractions.Contracts.Threading;

/// <summary>
/// Provides thread marshalling operations for cross-thread communication.
/// </summary>
/// <remarks>
/// LOGIC: IThreadMarshaller abstracts Avalonia's Dispatcher to enable:
/// - Testable code that doesn't depend on UI framework
/// - Explicit thread boundary crossing for CPU-bound work
/// - Debug-time assertions for thread safety validation
///
/// Data Flow:
/// 1. Background thread completes CPU-bound work (e.g., regex scanning)
/// 2. Thread marshaller dispatches results to UI thread
/// 3. UI components update safely on the main thread
///
/// Usage Patterns:
/// - Use InvokeOnUIThreadAsync when you need to wait for completion
/// - Use PostToUIThread for fire-and-forget UI updates
/// - Use Assert methods in DEBUG builds to catch thread violations early
///
/// Version: v0.2.7a
/// </remarks>
public interface IThreadMarshaller
{
    /// <summary>
    /// Gets whether the current thread is the UI thread.
    /// </summary>
    /// <remarks>
    /// LOGIC: Used to optimize code paths - if already on UI thread,
    /// no marshalling is needed.
    /// </remarks>
    bool IsOnUIThread { get; }

    /// <summary>
    /// Invokes an action on the UI thread and awaits completion.
    /// </summary>
    /// <param name="action">The action to invoke on the UI thread.</param>
    /// <returns>Task that completes when action finishes on UI thread.</returns>
    /// <remarks>
    /// LOGIC: Use when the caller needs to wait for the UI operation
    /// to complete before proceeding. The action executes synchronously
    /// on the UI thread, but the await returns on the original thread.
    ///
    /// If already on the UI thread, executes immediately without dispatch.
    /// </remarks>
    Task InvokeOnUIThreadAsync(Action action);

    /// <summary>
    /// Invokes a function on the UI thread and returns the result.
    /// </summary>
    /// <typeparam name="T">The return type of the function.</typeparam>
    /// <param name="func">The function to invoke on the UI thread.</param>
    /// <returns>Task with the function's return value.</returns>
    /// <remarks>
    /// LOGIC: Use when you need to get a value from UI-bound state
    /// (e.g., reading a control property) from a background thread.
    ///
    /// If already on the UI thread, executes immediately without dispatch.
    /// </remarks>
    Task<T> InvokeOnUIThreadAsync<T>(Func<T> func);

    /// <summary>
    /// Posts an action to the UI thread without waiting for completion.
    /// </summary>
    /// <param name="action">The action to post.</param>
    /// <remarks>
    /// LOGIC: Use for fire-and-forget UI updates where the background
    /// thread doesn't need to wait. Slightly more efficient than
    /// InvokeOnUIThreadAsync when you don't need the result.
    ///
    /// WARNING: Exceptions thrown by the action will be caught by
    /// the UI thread's unhandled exception handler.
    /// </remarks>
    void PostToUIThread(Action action);

    /// <summary>
    /// Asserts that the current code is running on the UI thread.
    /// </summary>
    /// <param name="operation">Operation name for the error message.</param>
    /// <remarks>
    /// LOGIC: Called at the start of methods that MUST run on the UI
    /// thread (e.g., updating ObservableCollection). Implementations
    /// should apply [Conditional("DEBUG")] to enable stripping in release.
    ///
    /// In RELEASE builds, calls should be optimized out via [Conditional].
    /// </remarks>
    void AssertUIThread(string operation);

    /// <summary>
    /// Asserts that the current code is NOT on the UI thread.
    /// </summary>
    /// <param name="operation">Operation name for the error message.</param>
    /// <remarks>
    /// LOGIC: Called at the start of CPU-intensive methods that should
    /// NOT run on the UI thread (e.g., regex scanning). Helps catch
    /// cases where background work accidentally runs on the UI thread.
    ///
    /// In RELEASE builds, calls should be optimized out via [Conditional].
    /// </remarks>
    void AssertBackgroundThread(string operation);
}
