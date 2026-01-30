namespace Lexichord.Abstractions.Contracts.Linting;

/// <summary>
/// Configuration options for the linting orchestrator.
/// </summary>
/// <remarks>
/// LOGIC: These settings control the reactive pipeline behavior:
/// - Debouncing prevents excessive re-scans during rapid edits
/// - Concurrency limits prevent resource exhaustion
/// - Timeout prevents runaway regex patterns
///
/// Version: v0.2.3a
/// </remarks>
public sealed record LintingOptions
{
    /// <summary>
    /// Debounce delay in milliseconds between content changes and scan trigger.
    /// </summary>
    /// <remarks>
    /// LOGIC: User typically pauses briefly between edits. 300ms balances
    /// responsiveness with avoiding unnecessary scans mid-typing.
    /// Range: 100-2000ms
    /// </remarks>
    public int DebounceMilliseconds { get; init; } = 300;

    /// <summary>
    /// Maximum number of concurrent document scans.
    /// </summary>
    /// <remarks>
    /// LOGIC: Limits CPU pressure when many documents are open.
    /// Each scan may use multiple threads internally for rule matching.
    /// Range: 1-8
    /// </remarks>
    public int MaxConcurrentScans { get; init; } = 2;

    /// <summary>
    /// Whether to emit partial results during progressive scanning.
    /// </summary>
    /// <remarks>
    /// LOGIC: When true, violations are published as they're found.
    /// When false, waits for complete scan before publishing.
    /// Default is false for simpler UI updates.
    /// </remarks>
    public bool EnableProgressiveResults { get; init; } = false;

    /// <summary>
    /// Timeout in milliseconds for individual regex pattern matching.
    /// </summary>
    /// <remarks>
    /// LOGIC: Prevents catastrophic backtracking from hanging scans.
    /// Applied per-pattern match, not per-document.
    /// Range: 100-5000ms
    /// </remarks>
    public int RegexTimeoutMilliseconds { get; init; } = 1000;

    /// <summary>
    /// Whether linting is globally enabled.
    /// </summary>
    /// <remarks>
    /// LOGIC: Master switch for disabling all background linting.
    /// Useful for performance-sensitive situations or user preference.
    /// </remarks>
    public bool Enabled { get; init; } = true;
}
