// =============================================================================
// File: DeduplicationDashboardData.cs
// Project: Lexichord.Abstractions
// Description: Record for deduplication metrics dashboard overview.
// =============================================================================
// VERSION: v0.5.9h (Hardening & Metrics)
// LOGIC: Aggregates key deduplication metrics for dashboard display,
//   providing a snapshot of system status and efficiency.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Aggregated metrics for the deduplication dashboard.
/// </summary>
/// <param name="DeduplicationRate">Percentage of chunks deduplicated (0.0-100.0).</param>
/// <param name="StorageSavedBytes">Estimated storage savings in bytes from deduplication.</param>
/// <param name="TotalCanonicalRecords">Total number of canonical records in the system.</param>
/// <param name="TotalVariants">Total number of merged variants across all canonicals.</param>
/// <param name="PendingReviews">Number of chunks awaiting manual review.</param>
/// <param name="PendingContradictions">Number of unresolved contradictions.</param>
/// <param name="ChunksProcessedToday">Number of chunks processed today.</param>
/// <param name="AverageProcessingTimeMs">Average processing time per chunk in milliseconds.</param>
/// <param name="ActionBreakdown">Breakdown of actions by type.</param>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9h as part of the Hardening &amp; Metrics phase.
/// </para>
/// <para>
/// <b>License:</b> Full dashboard data requires Writer Pro tier. Core tier users
/// receive only basic health status.
/// </para>
/// <para>
/// <b>Calculation Notes:</b>
/// </para>
/// <list type="bullet">
///   <item><description>
///     <b>DeduplicationRate</b>: Calculated as 
///     <c>((Merged + Linked) / TotalProcessed) * 100</c>.
///   </description></item>
///   <item><description>
///     <b>StorageSavedBytes</b>: Estimated based on average chunk size multiplied
///     by number of deduplicated chunks.
///   </description></item>
///   <item><description>
///     <b>ChunksProcessedToday</b>: Resets at midnight UTC.
///   </description></item>
/// </list>
/// </remarks>
public record DeduplicationDashboardData(
    double DeduplicationRate,
    long StorageSavedBytes,
    int TotalCanonicalRecords,
    int TotalVariants,
    int PendingReviews,
    int PendingContradictions,
    int ChunksProcessedToday,
    double AverageProcessingTimeMs,
    DeduplicationOperationBreakdown ActionBreakdown)
{
    /// <summary>
    /// Gets empty dashboard data with all metrics at zero.
    /// </summary>
    public static DeduplicationDashboardData Empty { get; } = new(
        0.0,
        0L,
        0,
        0,
        0,
        0,
        0,
        0.0,
        DeduplicationOperationBreakdown.Empty);

    /// <summary>
    /// Gets the storage saved in a human-readable format.
    /// </summary>
    /// <returns>Formatted string like "1.5 MB" or "256 KB".</returns>
    public string GetFormattedStorageSaved()
    {
        return StorageSavedBytes switch
        {
            >= 1_073_741_824 => $"{StorageSavedBytes / 1_073_741_824.0:F1} GB",
            >= 1_048_576 => $"{StorageSavedBytes / 1_048_576.0:F1} MB",
            >= 1_024 => $"{StorageSavedBytes / 1_024.0:F1} KB",
            _ => $"{StorageSavedBytes} B"
        };
    }
}
