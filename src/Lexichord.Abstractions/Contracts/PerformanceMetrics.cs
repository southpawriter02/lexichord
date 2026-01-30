namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Snapshot of performance metrics from the performance monitor.
/// </summary>
/// <param name="AverageScanDurationMs">Average scan duration in milliseconds.</param>
/// <param name="MaxScanDurationMs">Maximum scan duration observed.</param>
/// <param name="P95ScanDurationMs">95th percentile scan duration.</param>
/// <param name="FrameDropCount">Total frame drops observed.</param>
/// <param name="MemoryUsageMb">Current memory usage in megabytes.</param>
/// <param name="ScansCompleted">Number of scans completed.</param>
/// <param name="ScansCancelled">Number of scans cancelled.</param>
/// <remarks>
/// LOGIC: Immutable snapshot of performance metrics at a point in time.
/// Used for adaptive debounce tuning and performance monitoring.
///
/// Thread Safety:
/// - This is an immutable record, safe to share across threads
///
/// Version: v0.2.7d
/// </remarks>
public record PerformanceMetrics(
    double AverageScanDurationMs,
    double MaxScanDurationMs,
    double P95ScanDurationMs,
    int FrameDropCount,
    double MemoryUsageMb,
    int ScansCompleted,
    int ScansCancelled
)
{
    /// <summary>
    /// Gets whether any scans have been completed.
    /// </summary>
    public bool HasData => ScansCompleted > 0;

    /// <summary>
    /// Gets the cancellation rate as a percentage.
    /// </summary>
    public double CancellationRate =>
        ScansCompleted + ScansCancelled > 0
            ? (double)ScansCancelled / (ScansCompleted + ScansCancelled) * 100
            : 0;
}
