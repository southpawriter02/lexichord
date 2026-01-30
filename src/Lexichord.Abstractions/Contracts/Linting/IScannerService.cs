namespace Lexichord.Abstractions.Contracts.Linting;

/// <summary>
/// Pattern matching engine for style rules.
/// </summary>
/// <remarks>
/// LOGIC: IScannerService provides optimized pattern matching with:
/// - LRU caching of compiled regex patterns
/// - ReDoS protection via timeouts and complexity analysis
/// - Batch scanning for multiple rules against single content
///
/// Threading:
/// - All methods are thread-safe
/// - Cache is accessed via ConcurrentDictionary
/// - Statistics are tracked with Interlocked operations
///
/// Version: v0.2.3c
/// </remarks>
public interface IScannerService
{
    /// <summary>
    /// Scans content using a single style rule.
    /// </summary>
    /// <param name="content">The text content to scan.</param>
    /// <param name="rule">The style rule to apply.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Scan result with matches and performance data.</returns>
    /// <remarks>
    /// LOGIC: Single rule scan with caching. Uses compiled regex from
    /// cache if available, otherwise compiles and caches for future use.
    /// </remarks>
    Task<ScannerResult> ScanAsync(
        string content,
        StyleRule rule,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Scans content using multiple style rules in batch.
    /// </summary>
    /// <param name="content">The text content to scan.</param>
    /// <param name="rules">The style rules to apply.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of scan results, one per rule.</returns>
    /// <remarks>
    /// LOGIC: Batch scan optimizes by:
    /// 1. Pre-loading all patterns into cache
    /// 2. Scanning rules in parallel where possible
    /// 3. Aggregating results efficiently
    /// </remarks>
    Task<IReadOnlyList<ScannerResult>> ScanBatchAsync(
        string content,
        IEnumerable<StyleRule> rules,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current scanner statistics.
    /// </summary>
    /// <returns>Statistics about cache usage and performance.</returns>
    /// <remarks>
    /// LOGIC: Returns point-in-time snapshot of:
    /// - Total scans performed
    /// - Cache hits/misses
    /// - Current cache size vs max
    /// </remarks>
    ScannerStatistics GetStatistics();

    /// <summary>
    /// Clears the pattern cache.
    /// </summary>
    /// <remarks>
    /// LOGIC: Used for testing or when style sheets change significantly.
    /// Resets cache and statistics counters.
    /// </remarks>
    void ClearCache();
}
