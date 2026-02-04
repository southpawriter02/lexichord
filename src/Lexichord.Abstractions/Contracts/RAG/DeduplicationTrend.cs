// =============================================================================
// File: DeduplicationTrend.cs
// Project: Lexichord.Abstractions
// Description: Record for time series deduplication metrics.
// =============================================================================
// VERSION: v0.5.9h (Hardening & Metrics)
// LOGIC: Provides a single data point in a time series for trend visualization
//   in the metrics dashboard.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// A single data point in the deduplication metrics time series.
/// </summary>
/// <param name="Timestamp">The timestamp for this data point.</param>
/// <param name="ChunksProcessed">Number of chunks processed in this interval.</param>
/// <param name="MergedCount">Number of chunks merged in this interval.</param>
/// <param name="ContradictionsDetected">Number of contradictions detected in this interval.</param>
/// <param name="DeduplicationRate">Deduplication rate (0.0-1.0) for this interval.</param>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9h as part of the Hardening &amp; Metrics phase.
/// </para>
/// <para>
/// Time series data is aggregated by the specified interval (e.g., hourly, daily)
/// and used to visualize trends in deduplication activity over time.
/// </para>
/// <para>
/// <b>Deduplication Rate:</b> Calculated as:
/// <c>(MergedCount + LinkedCount) / ChunksProcessed</c>
/// where LinkedCount is the number of chunks linked to existing records.
/// </para>
/// <para>
/// <b>Usage:</b> Returned by <see cref="IDeduplicationMetricsService.GetTrendsAsync"/>
/// for line chart or area chart visualization.
/// </para>
/// </remarks>
public record DeduplicationTrend(
    DateTimeOffset Timestamp,
    int ChunksProcessed,
    int MergedCount,
    int ContradictionsDetected,
    double DeduplicationRate)
{
    /// <summary>
    /// Gets an empty trend data point for the specified timestamp.
    /// </summary>
    /// <param name="timestamp">The timestamp for the empty data point.</param>
    /// <returns>A trend record with all counts at zero.</returns>
    public static DeduplicationTrend Empty(DateTimeOffset timestamp) =>
        new(timestamp, 0, 0, 0, 0.0);
}
