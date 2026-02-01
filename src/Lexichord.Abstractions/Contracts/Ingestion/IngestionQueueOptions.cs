// =============================================================================
// File: IngestionQueueOptions.cs
// Project: Lexichord.Abstractions
// Description: Configuration options for the ingestion queue.
// =============================================================================
// LOGIC: Immutable configuration record for queue behavior.
//   - MaxQueueSize prevents unbounded memory growth.
//   - ThrottleDelayMs rate-limits processing to avoid resource contention.
//   - EnableDuplicateDetection prevents redundant processing.
//   - DuplicateWindowSeconds defines the duplicate detection time window.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Ingestion;

/// <summary>
/// Configuration options for the ingestion queue.
/// </summary>
/// <remarks>
/// <para>
/// These options control the behavior of the <see cref="IIngestionQueue"/>
/// implementation, including capacity limits, throttling, and duplicate detection.
/// </para>
/// <para>
/// <b>Capacity Management:</b> The <see cref="MaxQueueSize"/> setting prevents
/// unbounded memory growth. When the queue is full, enqueue operations will
/// wait until space is available (backpressure behavior).
/// </para>
/// <para>
/// <b>Throttling:</b> The <see cref="ThrottleDelayMs"/> setting introduces a
/// delay between processing items to prevent resource contention during
/// batch operations. Set to 0 for maximum throughput.
/// </para>
/// <para>
/// <b>Duplicate Detection:</b> When enabled, the queue tracks recently enqueued
/// file paths and skips duplicates within the <see cref="DuplicateWindowSeconds"/>
/// window. This prevents redundant processing when multiple change events
/// fire for the same file in quick succession.
/// </para>
/// </remarks>
/// <param name="MaxQueueSize">
/// Maximum number of items the queue can hold. Default: 1000.
/// Must be greater than 0.
/// </param>
/// <param name="ThrottleDelayMs">
/// Delay in milliseconds between processing queue items. Default: 100.
/// Set to 0 for no throttling. Must be non-negative.
/// </param>
/// <param name="EnableDuplicateDetection">
/// Whether to detect and skip duplicate file paths within the time window.
/// Default: true.
/// </param>
/// <param name="DuplicateWindowSeconds">
/// Time window in seconds for duplicate detection. Default: 60.
/// Files enqueued within this window of a previous enqueue are skipped.
/// Must be greater than 0 when duplicate detection is enabled.
/// </param>
/// <param name="MaxConcurrentProcessing">
/// Maximum number of items to process concurrently. Default: 1.
/// Must be greater than 0.
/// </param>
/// <param name="ShutdownTimeoutSeconds">
/// Maximum time in seconds to wait for graceful shutdown. Default: 30.
/// After this timeout, the service will force-stop.
/// </param>
public sealed record IngestionQueueOptions(
    int MaxQueueSize = 1000,
    int ThrottleDelayMs = 100,
    bool EnableDuplicateDetection = true,
    int DuplicateWindowSeconds = 60,
    int MaxConcurrentProcessing = 1,
    int ShutdownTimeoutSeconds = 30)
{
    /// <summary>
    /// Default options instance with standard values.
    /// </summary>
    public static IngestionQueueOptions Default { get; } = new();

    /// <summary>
    /// Options optimized for high-throughput batch processing.
    /// </summary>
    /// <remarks>
    /// Uses no throttling and higher concurrency for faster batch imports.
    /// </remarks>
    public static IngestionQueueOptions HighThroughput { get; } = new(
        MaxQueueSize: 5000,
        ThrottleDelayMs: 0,
        EnableDuplicateDetection: true,
        DuplicateWindowSeconds: 30,
        MaxConcurrentProcessing: 4,
        ShutdownTimeoutSeconds: 60);

    /// <summary>
    /// Options optimized for low-latency interactive use.
    /// </summary>
    /// <remarks>
    /// Uses minimal queue size and fast duplicate detection for responsive UI.
    /// </remarks>
    public static IngestionQueueOptions LowLatency { get; } = new(
        MaxQueueSize: 100,
        ThrottleDelayMs: 50,
        EnableDuplicateDetection: true,
        DuplicateWindowSeconds: 10,
        MaxConcurrentProcessing: 1,
        ShutdownTimeoutSeconds: 10);

    /// <summary>
    /// Validates the options and throws if invalid.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when any option value is outside valid bounds.
    /// </exception>
    public void Validate()
    {
        if (MaxQueueSize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaxQueueSize),
                MaxQueueSize,
                "MaxQueueSize must be greater than 0.");
        }

        if (ThrottleDelayMs < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ThrottleDelayMs),
                ThrottleDelayMs,
                "ThrottleDelayMs cannot be negative.");
        }

        if (EnableDuplicateDetection && DuplicateWindowSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(DuplicateWindowSeconds),
                DuplicateWindowSeconds,
                "DuplicateWindowSeconds must be greater than 0 when duplicate detection is enabled.");
        }

        if (MaxConcurrentProcessing <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaxConcurrentProcessing),
                MaxConcurrentProcessing,
                "MaxConcurrentProcessing must be greater than 0.");
        }

        if (ShutdownTimeoutSeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ShutdownTimeoutSeconds),
                ShutdownTimeoutSeconds,
                "ShutdownTimeoutSeconds must be greater than 0.");
        }
    }
}
