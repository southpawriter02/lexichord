// =============================================================================
// File: CacheStatistics.cs
// Project: Lexichord.Abstractions
// Description: Statistics record for monitoring cache performance metrics.
// Version: v0.5.8c
// =============================================================================
// LOGIC: Provides unified cache metrics for monitoring hit rates, eviction
//   counts, and memory usage across the caching layers.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Statistics about the cache state and performance.
/// </summary>
/// <param name="HitCount">Number of successful cache retrievals.</param>
/// <param name="MissCount">Number of cache misses.</param>
/// <param name="HitRate">Calculated hit rate (0.0 to 1.0).</param>
/// <param name="EntryCount">Current number of entries in the cache.</param>
/// <param name="MaxEntries">Maximum allowed entries before eviction.</param>
/// <param name="EvictionCount">Number of entries evicted due to capacity limits.</param>
/// <param name="InvalidationCount">Number of entries removed via explicit invalidation.</param>
/// <param name="ApproximateSizeBytes">Estimated memory usage in bytes.</param>
/// <remarks>
/// <para>
/// <see cref="CacheStatistics"/> provides real-time metrics for monitoring cache
/// efficiency and diagnosing performance issues. All counters are cumulative since
/// the cache was created or last cleared.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.8c as part of the Multi-Layer Caching System.
/// </para>
/// </remarks>
public record CacheStatistics(
    long HitCount,
    long MissCount,
    double HitRate,
    int EntryCount,
    int MaxEntries,
    int EvictionCount,
    int InvalidationCount,
    long ApproximateSizeBytes)
{
    /// <summary>
    /// Creates an empty statistics record for an uninitialized cache.
    /// </summary>
    /// <param name="maxEntries">Maximum entries the cache supports.</param>
    /// <returns>A <see cref="CacheStatistics"/> with all counters at zero.</returns>
    public static CacheStatistics Empty(int maxEntries = 0) =>
        new(0, 0, 0.0, 0, maxEntries, 0, 0, 0);

    /// <summary>
    /// Formats the approximate size in a human-readable format (e.g., "1.5 MB").
    /// </summary>
    /// <returns>A formatted string representing the cache size.</returns>
    public string FormatSize()
    {
        const long KB = 1024;
        const long MB = KB * 1024;
        const long GB = MB * 1024;

        return ApproximateSizeBytes switch
        {
            >= GB => $"{(double)ApproximateSizeBytes / GB:F2} GB",
            >= MB => $"{(double)ApproximateSizeBytes / MB:F2} MB",
            >= KB => $"{(double)ApproximateSizeBytes / KB:F2} KB",
            _ => $"{ApproximateSizeBytes} B"
        };
    }

    /// <summary>
    /// Gets the cache utilization as a percentage (0-100).
    /// </summary>
    public double UtilizationPercent =>
        MaxEntries > 0 ? (double)EntryCount / MaxEntries * 100.0 : 0.0;
}
