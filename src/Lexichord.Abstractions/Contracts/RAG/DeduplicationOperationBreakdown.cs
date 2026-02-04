// =============================================================================
// File: DeduplicationOperationBreakdown.cs
// Project: Lexichord.Abstractions
// Description: Record for deduplication action distribution metrics.
// =============================================================================
// VERSION: v0.5.9h (Hardening & Metrics)
// LOGIC: Provides a breakdown of deduplication actions taken, enabling
//   pie chart visualization in the metrics dashboard.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Breakdown of deduplication operations by action type.
/// </summary>
/// <param name="StoredAsNew">Count of chunks stored as new canonical records.</param>
/// <param name="MergedIntoExisting">Count of chunks merged into existing canonical records.</param>
/// <param name="LinkedToExisting">Count of chunks linked to existing records as complementary.</param>
/// <param name="FlaggedAsContradiction">Count of chunks flagged as contradictions.</param>
/// <param name="QueuedForReview">Count of chunks queued for manual review.</param>
/// <param name="Errors">Count of processing errors encountered.</param>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9h as part of the Hardening &amp; Metrics phase.
/// </para>
/// <para>
/// This record is designed for pie chart or bar chart visualization in the
/// metrics dashboard, showing the distribution of deduplication outcomes.
/// </para>
/// <para>
/// <b>Usage:</b> Included in <see cref="DeduplicationDashboardData"/> to provide
/// action distribution for a given time period.
/// </para>
/// <para>
/// <b>Calculation:</b> Counts are accumulated from the start of the time period
/// until now. The total of all counts equals the total chunks processed.
/// </para>
/// </remarks>
public record DeduplicationOperationBreakdown(
    int StoredAsNew,
    int MergedIntoExisting,
    int LinkedToExisting,
    int FlaggedAsContradiction,
    int QueuedForReview,
    int Errors)
{
    /// <summary>
    /// Gets the total count of all operations.
    /// </summary>
    public int Total => StoredAsNew + MergedIntoExisting + LinkedToExisting +
                        FlaggedAsContradiction + QueuedForReview + Errors;

    /// <summary>
    /// Gets an empty breakdown with all counts at zero.
    /// </summary>
    public static DeduplicationOperationBreakdown Empty { get; } = new(0, 0, 0, 0, 0, 0);
}
