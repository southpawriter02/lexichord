using System.Collections.Concurrent;
using System.Diagnostics;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Style.Services;

/// <summary>
/// Monitors linting performance metrics.
/// </summary>
/// <remarks>
/// LOGIC: PerformanceMonitor tracks scan durations, frame drops,
/// and memory usage across all linting operations. Uses lock-free
/// concurrent collections for thread-safe metrics collection.
///
/// Metrics are used for:
/// - Adaptive debounce adjustment
/// - Performance regression detection
/// - User-facing performance indicators
///
/// Thread Safety:
/// - All methods are thread-safe
/// - Uses Interlocked operations for counters
/// - ConcurrentQueue for duration samples
///
/// Version: v0.2.7d
/// </remarks>
public sealed class PerformanceMonitor : IPerformanceMonitor, IDisposable
{
    private readonly ConcurrentQueue<double> _scanDurations = new();
    private readonly ILogger<PerformanceMonitor> _logger;
    private readonly int _maxSamples;

    private long _scansCompleted;
    private long _scansCancelled;
    private int _frameDropCount;
    private long _peakMemoryBytes;
    private bool _disposed;

    // LOGIC: Thresholds for performance degradation detection
    private const double DegradedScanThresholdMs = 500;
    private const int DegradedFrameDropThreshold = 5;

    // LOGIC: Baseline debounce intervals
    private static readonly TimeSpan MinDebounce = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan MaxDebounce = TimeSpan.FromMilliseconds(1000);

    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceMonitor"/> class.
    /// </summary>
    /// <param name="logger">Logger for performance events.</param>
    /// <param name="maxSamples">Maximum number of samples to retain for averaging.</param>
    public PerformanceMonitor(ILogger<PerformanceMonitor> logger, int maxSamples = 100)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _maxSamples = maxSamples;

        _logger.LogDebug("PerformanceMonitor initialized with maxSamples={MaxSamples}", maxSamples);
    }

    /// <inheritdoc/>
    public IDisposable StartOperation(string operationName)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return new OperationTimer(this, operationName);
    }

    /// <inheritdoc/>
    public void RecordOperation(string operationName, TimeSpan duration)
    {
        if (_disposed) return;

        // LOGIC: Record scan duration
        _scanDurations.Enqueue(duration.TotalMilliseconds);

        // LOGIC: Trim old samples to maintain bounded memory
        while (_scanDurations.Count > _maxSamples)
        {
            _scanDurations.TryDequeue(out _);
        }

        // LOGIC: Update memory tracking
        var currentMemory = GC.GetTotalMemory(false);
        InterlockedMax(ref _peakMemoryBytes, currentMemory);

        // LOGIC: Increment completed count
        Interlocked.Increment(ref _scansCompleted);

        _logger.LogDebug(
            "Operation {OperationName} completed in {Duration}ms",
            operationName,
            duration.TotalMilliseconds);
    }

    /// <inheritdoc/>
    public void ReportFrameDrop(int droppedFrames)
    {
        if (_disposed) return;

        var newCount = Interlocked.Add(ref _frameDropCount, droppedFrames);

        _logger.LogWarning(
            "Frame drops detected: {Count} in last interval (total: {Total})",
            droppedFrames,
            newCount);
    }

    /// <inheritdoc/>
    public PerformanceMetrics GetMetrics()
    {
        var durations = _scanDurations.ToArray();

        if (durations.Length == 0)
        {
            return new PerformanceMetrics(
                AverageScanDurationMs: 0,
                MaxScanDurationMs: 0,
                P95ScanDurationMs: 0,
                FrameDropCount: _frameDropCount,
                MemoryUsageMb: GC.GetTotalMemory(false) / (1024.0 * 1024.0),
                ScansCompleted: (int)Interlocked.Read(ref _scansCompleted),
                ScansCancelled: (int)Interlocked.Read(ref _scansCancelled)
            );
        }

        // LOGIC: Calculate statistics
        Array.Sort(durations);
        var average = durations.Average();
        var max = durations.Max();
        var p95Index = (int)(durations.Length * 0.95);
        var p95 = durations[Math.Min(p95Index, durations.Length - 1)];

        return new PerformanceMetrics(
            AverageScanDurationMs: average,
            MaxScanDurationMs: max,
            P95ScanDurationMs: p95,
            FrameDropCount: _frameDropCount,
            MemoryUsageMb: GC.GetTotalMemory(false) / (1024.0 * 1024.0),
            ScansCompleted: (int)Interlocked.Read(ref _scansCompleted),
            ScansCancelled: (int)Interlocked.Read(ref _scansCancelled)
        );
    }

    /// <inheritdoc/>
    public void Reset()
    {
        while (_scanDurations.TryDequeue(out _)) { }
        Interlocked.Exchange(ref _scansCompleted, 0);
        Interlocked.Exchange(ref _scansCancelled, 0);
        Interlocked.Exchange(ref _frameDropCount, 0);
        Interlocked.Exchange(ref _peakMemoryBytes, 0);

        _logger.LogDebug("PerformanceMonitor metrics reset");
    }

    /// <inheritdoc/>
    public bool IsPerformanceDegraded
    {
        get
        {
            var metrics = GetMetrics();
            var isDegraded = metrics.AverageScanDurationMs > DegradedScanThresholdMs
                || metrics.FrameDropCount > DegradedFrameDropThreshold;

            if (isDegraded)
            {
                _logger.LogInformation(
                    "Performance degradation detected: avg scan {Avg}ms, frame drops {Drops}",
                    metrics.AverageScanDurationMs,
                    metrics.FrameDropCount);
            }

            return isDegraded;
        }
    }

    /// <inheritdoc/>
    public TimeSpan RecommendedDebounceInterval
    {
        get
        {
            var metrics = GetMetrics();

            // LOGIC: Scale debounce based on scan performance
            if (metrics.AverageScanDurationMs < 100)
            {
                return MinDebounce;
            }

            if (metrics.AverageScanDurationMs > 500)
            {
                _logger.LogDebug(
                    "Recommended debounce adjusted to {Debounce}ms due to slow scans",
                    MaxDebounce.TotalMilliseconds);
                return MaxDebounce;
            }

            // LOGIC: Linear interpolation between min and max
            var factor = (metrics.AverageScanDurationMs - 100) / 400;
            var debounceMs = MinDebounce.TotalMilliseconds +
                (MaxDebounce.TotalMilliseconds - MinDebounce.TotalMilliseconds) * factor;

            return TimeSpan.FromMilliseconds(debounceMs);
        }
    }

    /// <summary>
    /// Records a cancelled scan operation.
    /// </summary>
    /// <remarks>
    /// LOGIC: Tracks cancellation separately from completion for
    /// calculating cancellation rates and detecting issues.
    /// </remarks>
    public void RecordCancellation()
    {
        if (_disposed) return;
        Interlocked.Increment(ref _scansCancelled);
    }

    /// <summary>
    /// Gets the peak memory usage in bytes.
    /// </summary>
    public long PeakMemoryBytes => Interlocked.Read(ref _peakMemoryBytes);

    /// <summary>
    /// Atomically updates a location to the maximum of its current value and a new value.
    /// </summary>
    private static void InterlockedMax(ref long location, long value)
    {
        long current;
        do
        {
            current = Interlocked.Read(ref location);
            if (value <= current) return;
        } while (Interlocked.CompareExchange(ref location, value, current) != current);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _logger.LogDebug("PerformanceMonitor disposed");
    }

    /// <summary>
    /// Timer for measuring operation duration.
    /// </summary>
    /// <remarks>
    /// LOGIC: Implements IDisposable to automatically record duration
    /// when the using scope ends.
    /// </remarks>
    private sealed class OperationTimer : IDisposable
    {
        private readonly PerformanceMonitor _monitor;
        private readonly string _operationName;
        private readonly Stopwatch _stopwatch;
        private bool _disposed;

        public OperationTimer(PerformanceMonitor monitor, string operationName)
        {
            _monitor = monitor;
            _operationName = operationName;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _stopwatch.Stop();
            _monitor.RecordOperation(_operationName, _stopwatch.Elapsed);
        }
    }
}
