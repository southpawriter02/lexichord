namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Results from a linting benchmark.
/// </summary>
/// <param name="AverageScanDuration">Average time per scan.</param>
/// <param name="P95ScanDuration">95th percentile scan time.</param>
/// <param name="P99ScanDuration">99th percentile scan time.</param>
/// <param name="PeakMemoryBytes">Peak memory usage during benchmark.</param>
/// <param name="TotalAllocatedBytes">Total bytes allocated.</param>
/// <param name="FrameDropCount">Number of frame drops observed.</param>
/// <param name="AverageFrameRate">Average frame rate during benchmark.</param>
/// <param name="Iterations">Number of iterations completed.</param>
/// <param name="ContentLength">Length of content tested.</param>
/// <param name="ViolationsFound">Average violations per scan.</param>
/// <param name="MeetsPerformanceTargets">Whether all targets were met.</param>
/// <remarks>
/// LOGIC: Captures comprehensive benchmark results including scan
/// performance, memory usage, and UI responsiveness metrics.
///
/// Version: v0.2.7d
/// </remarks>
public record BenchmarkResult(
    TimeSpan AverageScanDuration,
    TimeSpan P95ScanDuration,
    TimeSpan P99ScanDuration,
    long PeakMemoryBytes,
    long TotalAllocatedBytes,
    int FrameDropCount,
    double AverageFrameRate,
    int Iterations,
    int ContentLength,
    double ViolationsFound,
    bool MeetsPerformanceTargets
)
{
    /// <summary>
    /// Gets memory usage in megabytes.
    /// </summary>
    public double PeakMemoryMb => PeakMemoryBytes / (1024.0 * 1024.0);

    /// <summary>
    /// Gets total allocated memory in megabytes.
    /// </summary>
    public double TotalAllocatedMb => TotalAllocatedBytes / (1024.0 * 1024.0);

    /// <summary>
    /// Gets average scan duration in milliseconds.
    /// </summary>
    public double AverageScanMs => AverageScanDuration.TotalMilliseconds;

    /// <summary>
    /// Creates a formatted report string.
    /// </summary>
    /// <returns>Human-readable benchmark report.</returns>
    public string ToReport() => $"""
        Linting Benchmark Report
        ========================
        Content Length: {ContentLength:N0} characters
        Iterations: {Iterations}

        Scan Performance:
          Average: {AverageScanMs:F2}ms
          P95: {P95ScanDuration.TotalMilliseconds:F2}ms
          P99: {P99ScanDuration.TotalMilliseconds:F2}ms
          Violations/Scan: {ViolationsFound:F1}

        Memory:
          Peak: {PeakMemoryMb:F2}MB
          Total Allocated: {TotalAllocatedMb:F2}MB

        UI Responsiveness:
          Average Frame Rate: {AverageFrameRate:F1}fps
          Frame Drops: {FrameDropCount}

        Status: {(MeetsPerformanceTargets ? "PASS" : "FAIL")}
        """;
}

/// <summary>
/// Results from a typing simulation benchmark.
/// </summary>
/// <param name="CharactersTyped">Number of characters typed during simulation.</param>
/// <param name="TotalDuration">Total time for the simulation.</param>
/// <param name="AverageFrameRate">Average frame rate during typing.</param>
/// <param name="MinFrameRate">Minimum frame rate observed.</param>
/// <param name="FrameDrops">Number of frame drops during typing.</param>
/// <param name="LintingTriggered">Number of times linting was triggered.</param>
/// <param name="LintingCompleted">Number of times linting completed.</param>
/// <param name="AverageInputLatency">Average input-to-display latency.</param>
/// <param name="MeetsResponsivenessTargets">Whether responsiveness targets were met.</param>
/// <remarks>
/// LOGIC: Captures typing simulation results to validate that the UI
/// remains responsive during active editing with background linting.
///
/// Version: v0.2.7d
/// </remarks>
public record TypingBenchmarkResult(
    int CharactersTyped,
    TimeSpan TotalDuration,
    double AverageFrameRate,
    double MinFrameRate,
    int FrameDrops,
    int LintingTriggered,
    int LintingCompleted,
    TimeSpan AverageInputLatency,
    bool MeetsResponsivenessTargets
)
{
    /// <summary>
    /// Gets characters per second achieved.
    /// </summary>
    public double CharactersPerSecond =>
        CharactersTyped / TotalDuration.TotalSeconds;

    /// <summary>
    /// Gets average input latency in milliseconds.
    /// </summary>
    public double AverageInputLatencyMs => AverageInputLatency.TotalMilliseconds;
}

/// <summary>
/// Results from a scrolling benchmark.
/// </summary>
/// <param name="ScrollEvents">Number of scroll events simulated.</param>
/// <param name="TotalDuration">Total time for the simulation.</param>
/// <param name="AverageFrameRate">Average frame rate during scrolling.</param>
/// <param name="FrameDrops">Number of frame drops during scrolling.</param>
/// <param name="ViolationsRendered">Number of violations rendered.</param>
/// <param name="MeetsRenderingTargets">Whether rendering targets were met.</param>
/// <remarks>
/// LOGIC: Captures scrolling simulation results to validate that
/// violation rendering remains performant during rapid scrolling.
///
/// Version: v0.2.7d
/// </remarks>
public record ScrollBenchmarkResult(
    int ScrollEvents,
    TimeSpan TotalDuration,
    double AverageFrameRate,
    int FrameDrops,
    int ViolationsRendered,
    bool MeetsRenderingTargets
);
