namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Interface for monitoring linting performance metrics.
/// </summary>
/// <remarks>
/// LOGIC: IPerformanceMonitor tracks scan durations, frame drops,
/// and memory usage across all linting operations. It provides
/// adaptive debounce recommendations based on observed performance.
///
/// Thread Safety:
/// - All methods are thread-safe
/// - Uses Interlocked operations for counters
/// - Returns immutable snapshots
///
/// Version: v0.2.7d
/// </remarks>
public interface IPerformanceMonitor
{
    /// <summary>
    /// Starts timing an operation.
    /// </summary>
    /// <param name="operationName">Name of the operation for logging.</param>
    /// <returns>Disposable that records duration when disposed.</returns>
    /// <remarks>
    /// LOGIC: Use with 'using' statement to automatically record
    /// operation duration when the scope ends.
    ///
    /// Example:
    /// <code>
    /// using (_monitor.StartOperation("scan"))
    /// {
    ///     await ScanAsync(content);
    /// }
    /// </code>
    /// </remarks>
    IDisposable StartOperation(string operationName);

    /// <summary>
    /// Records a completed operation's duration.
    /// </summary>
    /// <param name="operationName">Name of the operation.</param>
    /// <param name="duration">Duration of the operation.</param>
    /// <remarks>
    /// LOGIC: Use this when you have the duration available directly
    /// rather than using StartOperation/Dispose pattern.
    /// </remarks>
    void RecordOperation(string operationName, TimeSpan duration);

    /// <summary>
    /// Reports dropped frames to the monitor.
    /// </summary>
    /// <param name="droppedFrames">Number of frames dropped.</param>
    /// <remarks>
    /// LOGIC: Call when frame drops are detected by the UI layer.
    /// Frame drops affect debounce recommendations.
    /// </remarks>
    void ReportFrameDrop(int droppedFrames);

    /// <summary>
    /// Gets a snapshot of current performance metrics.
    /// </summary>
    /// <returns>Current metrics snapshot.</returns>
    PerformanceMetrics GetMetrics();

    /// <summary>
    /// Resets all collected metrics.
    /// </summary>
    /// <remarks>
    /// LOGIC: Clears all samples, counters, and tracked values.
    /// Use when starting a new benchmarking session.
    /// </remarks>
    void Reset();

    /// <summary>
    /// Gets whether performance is currently degraded.
    /// </summary>
    /// <value>True if average scan time exceeds threshold or frame drops are frequent.</value>
    /// <remarks>
    /// LOGIC: Used to trigger adaptive behaviors like increased debounce
    /// or reduced scanning scope.
    /// </remarks>
    bool IsPerformanceDegraded { get; }

    /// <summary>
    /// Gets the recommended debounce interval based on current performance.
    /// </summary>
    /// <value>Recommended debounce interval (200ms–1000ms).</value>
    /// <remarks>
    /// LOGIC: Returns higher debounce intervals when scans are slow
    /// to prevent UI lag. Returns minimum interval when performance is good.
    /// </remarks>
    TimeSpan RecommendedDebounceInterval { get; }
}
