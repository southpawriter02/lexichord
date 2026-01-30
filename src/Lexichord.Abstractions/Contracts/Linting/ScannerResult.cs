namespace Lexichord.Abstractions.Contracts.Linting;

/// <summary>
/// Represents a single pattern match in scanned content.
/// </summary>
/// <param name="StartOffset">Character offset where the match starts (0-indexed).</param>
/// <param name="Length">Length of the matched text.</param>
/// <remarks>
/// LOGIC: Lightweight struct for efficient storage of match positions.
/// Use with content.Substring(StartOffset, Length) to extract matched text.
///
/// Version: v0.2.3c
/// </remarks>
public readonly record struct PatternMatchSpan(int StartOffset, int Length)
{
    /// <summary>
    /// Gets the end offset (exclusive).
    /// </summary>
    public int EndOffset => StartOffset + Length;
}

/// <summary>
/// Result of scanning content with a single style rule.
/// </summary>
/// <param name="RuleId">The ID of the rule that was applied.</param>
/// <param name="Matches">List of match positions found.</param>
/// <param name="ScanDuration">Time taken to perform the scan.</param>
/// <param name="WasCacheHit">Whether the compiled pattern was from cache.</param>
/// <remarks>
/// LOGIC: Immutable record capturing both results and performance metrics.
/// ScanDuration and WasCacheHit enable performance monitoring and tuning.
///
/// Version: v0.2.3c
/// </remarks>
public sealed record ScannerResult(
    string RuleId,
    IReadOnlyList<PatternMatchSpan> Matches,
    TimeSpan ScanDuration,
    bool WasCacheHit)
{
    /// <summary>
    /// Gets the number of matches found.
    /// </summary>
    public int MatchCount => Matches.Count;

    /// <summary>
    /// Gets whether any matches were found.
    /// </summary>
    public bool HasMatches => Matches.Count > 0;

    /// <summary>
    /// Creates an empty result for disabled rules or empty content.
    /// </summary>
    public static ScannerResult Empty(string ruleId) => new(
        ruleId,
        Array.Empty<PatternMatchSpan>(),
        TimeSpan.Zero,
        WasCacheHit: false);

    /// <summary>
    /// Creates a timeout result when ReDoS protection triggered.
    /// </summary>
    public static ScannerResult Timeout(string ruleId, TimeSpan duration) => new(
        ruleId,
        Array.Empty<PatternMatchSpan>(),
        duration,
        WasCacheHit: false);
}

/// <summary>
/// Statistics about scanner cache usage and performance.
/// </summary>
/// <param name="TotalScans">Total number of scan operations performed.</param>
/// <param name="CacheHits">Number of scans using cached compiled patterns.</param>
/// <param name="CacheMisses">Number of scans requiring pattern compilation.</param>
/// <param name="CurrentCacheSize">Current number of patterns in cache.</param>
/// <param name="MaxCacheSize">Maximum cache capacity.</param>
/// <remarks>
/// LOGIC: Provides insight into cache efficiency for monitoring.
/// High CacheHits/TotalScans ratio indicates good cache utilization.
///
/// Version: v0.2.3c
/// </remarks>
public sealed record ScannerStatistics(
    long TotalScans,
    long CacheHits,
    long CacheMisses,
    int CurrentCacheSize,
    int MaxCacheSize)
{
    /// <summary>
    /// Gets the cache hit ratio (0.0 to 1.0).
    /// </summary>
    public double HitRatio => TotalScans > 0
        ? (double)CacheHits / TotalScans
        : 0.0;

    /// <summary>
    /// Empty statistics for initial state.
    /// </summary>
    public static ScannerStatistics Empty(int maxCacheSize) => new(
        TotalScans: 0,
        CacheHits: 0,
        CacheMisses: 0,
        CurrentCacheSize: 0,
        MaxCacheSize: maxCacheSize);
}
