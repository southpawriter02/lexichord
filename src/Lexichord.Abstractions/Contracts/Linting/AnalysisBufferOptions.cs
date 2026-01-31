namespace Lexichord.Abstractions.Contracts.Linting;

/// <summary>
/// Configuration options for the analysis request buffer.
/// </summary>
/// <remarks>
/// LOGIC: Controls how the buffer debounces and manages analysis requests.
/// These settings affect typing responsiveness and system resource usage:
/// - Lower idle periods = faster feedback, higher CPU usage
/// - Higher idle periods = smoother typing, delayed feedback
///
/// Default values follow LCS-DES-037a specification recommendations.
///
/// Version: v0.3.7a
/// </remarks>
public sealed class AnalysisBufferOptions
{
    /// <summary>
    /// Default idle period in milliseconds before processing a request.
    /// </summary>
    /// <remarks>
    /// LOGIC: 300ms provides a good balance between responsiveness and
    /// avoiding excessive analysis during rapid typing. This is the
    /// "quiet period" after the last keystroke before analysis begins.
    /// </remarks>
    public const int DefaultIdlePeriodMilliseconds = 300;

    /// <summary>
    /// Default maximum number of documents that can have pending requests.
    /// </summary>
    /// <remarks>
    /// LOGIC: 100 documents covers typical workloads while preventing
    /// unbounded memory growth from pathological scenarios.
    /// </remarks>
    public const int DefaultMaxBufferedDocuments = 100;

    /// <summary>
    /// Gets or sets the idle period in milliseconds before a buffered
    /// request is emitted for processing.
    /// </summary>
    /// <remarks>
    /// LOGIC: After receiving a request, the buffer waits this long
    /// for additional requests. If new requests arrive, the timer resets.
    /// Only after this idle period elapses is the latest request processed.
    /// Must be greater than 0.
    /// </remarks>
    public int IdlePeriodMilliseconds { get; set; } = DefaultIdlePeriodMilliseconds;

    /// <summary>
    /// Gets or sets the maximum number of documents that can have
    /// pending requests in the buffer simultaneously.
    /// </summary>
    /// <remarks>
    /// LOGIC: Prevents unbounded memory usage if many documents are
    /// being edited concurrently. When limit is reached, oldest pending
    /// requests are dropped. Must be greater than 0.
    /// </remarks>
    public int MaxBufferedDocuments { get; set; } = DefaultMaxBufferedDocuments;

    /// <summary>
    /// Gets or sets whether the buffer is enabled.
    /// </summary>
    /// <remarks>
    /// LOGIC: When disabled, requests pass through immediately without
    /// debouncing. Useful for testing or when instant feedback is needed.
    /// </remarks>
    public bool Enabled { get; set; } = true;
}
