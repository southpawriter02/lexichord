// =============================================================================
// File: IDeduplicationMetricsService.cs
// Project: Lexichord.Abstractions
// Description: Interface for recording and querying deduplication metrics.
// =============================================================================
// VERSION: v0.5.9h (Hardening & Metrics)
// LOGIC: Defines the contract for the deduplication metrics service which tracks
//   operational metrics and provides dashboard data for observability.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Service for recording and querying deduplication metrics.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9h as part of the Hardening &amp; Metrics phase.
/// </para>
/// <para>
/// This service provides two categories of functionality:
/// </para>
/// <list type="bullet">
///   <item><description>
///     <b>Recording:</b> Thread-safe methods for recording metrics from deduplication
///     operations. Called by <see cref="ISimilarityDetector"/>, <see cref="IRelationshipClassifier"/>,
///     <see cref="IDeduplicationService"/>, and related services.
///   </description></item>
///   <item><description>
///     <b>Querying:</b> Async methods for retrieving aggregated metrics for dashboard
///     display and alerting.
///   </description></item>
/// </list>
/// <para>
/// <b>License Requirements:</b>
/// </para>
/// <list type="bullet">
///   <item><description>Recording methods: Always enabled (no license check).</description></item>
///   <item><description>Dashboard data: Writer Pro tier required for full data.</description></item>
///   <item><description>Health status: Available to all tiers (basic operational info).</description></item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> All recording methods must be thread-safe as they are called
/// concurrently from multiple chunk processing operations.
/// </para>
/// </remarks>
public interface IDeduplicationMetricsService
{
    #region Recording Methods

    /// <summary>
    /// Records a chunk processing event.
    /// </summary>
    /// <param name="action">The action taken for this chunk.</param>
    /// <param name="processingTime">The total processing time for this chunk.</param>
    /// <remarks>
    /// Thread-safe. Called by <see cref="IDeduplicationService"/> after each chunk is processed.
    /// </remarks>
    void RecordChunkProcessed(DeduplicationAction action, TimeSpan processingTime);

    /// <summary>
    /// Records a similarity query operation.
    /// </summary>
    /// <param name="duration">The query execution time.</param>
    /// <param name="matchCount">The number of similar chunks found.</param>
    /// <remarks>
    /// Thread-safe. Called by <see cref="ISimilarityDetector"/> after each query.
    /// </remarks>
    void RecordSimilarityQuery(TimeSpan duration, int matchCount);

    /// <summary>
    /// Records a relationship classification operation.
    /// </summary>
    /// <param name="method">The classification method used (RuleBased, LlmBased, Cached).</param>
    /// <param name="result">The classification result type.</param>
    /// <param name="duration">The classification execution time.</param>
    /// <remarks>
    /// Thread-safe. Called by <see cref="IRelationshipClassifier"/> after each classification.
    /// </remarks>
    void RecordClassification(ClassificationMethod method, RelationshipType result, TimeSpan duration);

    /// <summary>
    /// Records a contradiction detection event.
    /// </summary>
    /// <param name="severity">The severity of the detected contradiction.</param>
    /// <remarks>
    /// Thread-safe. Called by <see cref="IContradictionService"/> when a contradiction is flagged.
    /// </remarks>
    void RecordContradictionDetected(ContradictionSeverity severity);

    /// <summary>
    /// Records a batch job completion event.
    /// </summary>
    /// <param name="result">The batch job result.</param>
    /// <remarks>
    /// Thread-safe. Called by <see cref="IBatchDeduplicationJob"/> upon job completion.
    /// </remarks>
    void RecordBatchJobCompleted(BatchDeduplicationResult result);

    #endregion

    #region Query Methods

    /// <summary>
    /// Gets aggregated dashboard data for the deduplication system.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Aggregated metrics for dashboard display.</returns>
    /// <remarks>
    /// <para>
    /// <b>License:</b> Full data requires Writer Pro tier. Core tier receives
    /// <see cref="DeduplicationDashboardData.Empty"/>.
    /// </para>
    /// <para>
    /// Data is computed from current gauge values and accumulated counters.
    /// Some values may be cached for performance (TTL: 30 seconds).
    /// </para>
    /// </remarks>
    Task<DeduplicationDashboardData> GetDashboardDataAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets trend data for the specified time period.
    /// </summary>
    /// <param name="period">The time period to retrieve trends for.</param>
    /// <param name="interval">
    /// The aggregation interval. Defaults to automatic selection based on period.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of trend data points ordered by timestamp.</returns>
    /// <remarks>
    /// <para>
    /// <b>License:</b> Requires Writer Pro tier. Returns empty list if unlicensed.
    /// </para>
    /// <para>
    /// <b>Automatic Interval Selection:</b>
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Period ≤ 24 hours: Hourly intervals</description></item>
    ///   <item><description>Period ≤ 7 days: Daily intervals</description></item>
    ///   <item><description>Period &gt; 7 days: Weekly intervals</description></item>
    /// </list>
    /// </remarks>
    Task<IReadOnlyList<DeduplicationTrend>> GetTrendsAsync(
        TimeSpan period,
        TimeSpan? interval = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the current health status of the deduplication system.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Health status with performance metrics and warnings.</returns>
    /// <remarks>
    /// <para>
    /// <b>License:</b> Available to all tiers (basic operational info).
    /// </para>
    /// <para>
    /// Health is determined by comparing current P99 latencies against targets
    /// and checking for warning conditions.
    /// </para>
    /// </remarks>
    Task<DeduplicationHealthStatus> GetHealthStatusAsync(CancellationToken ct = default);

    #endregion
}
