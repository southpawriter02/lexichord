// =============================================================================
// File: IQueryHistoryService.cs
// Project: Lexichord.Abstractions
// Description: Interface for query history tracking and analytics (v0.5.4d).
// =============================================================================
// LOGIC: Defines the contract for tracking search patterns:
//   - RecordAsync: Store executed queries with metadata
//   - GetRecentAsync: Retrieve recent queries for quick-access
//   - GetZeroResultQueriesAsync: Identify content gaps
//   - ClearAsync: Privacy-friendly history clearing
// =============================================================================
// VERSION: v0.5.4d (Query History & Analytics)
// DEPENDENCIES:
//   - QueryAnalysis (v0.5.4a) for intent tracking
//   - ISettingsService (v0.1.6a) for telemetry opt-in preference
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Tracks query history and provides search analytics.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IQueryHistoryService"/> enables:
/// <list type="bullet">
///   <item><description>Recent queries quick-access panel</description></item>
///   <item><description>Zero-result query identification (content gaps)</description></item>
///   <item><description>Search performance tracking</description></item>
///   <item><description>Optional anonymized analytics</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Privacy Guarantees:</b>
/// <list type="bullet">
///   <item><description>Query history stored locally only</description></item>
///   <item><description>Analytics events use SHA256-hashed query text</description></item>
///   <item><description>Telemetry is opt-in via settings</description></item>
///   <item><description>Clear history action available to users</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe as the service
/// may be called concurrently from UI and search execution.
/// </para>
/// <para>
/// <b>License Gate:</b> History tracking and analytics are gated at Writer Pro
/// tier via <c>FeatureFlags.RAG.RelevanceTuner</c>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.4d as part of The Relevance Tuner feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Record a search after execution
/// await _historyService.RecordAsync(new QueryHistoryEntry(
///     Id: Guid.NewGuid(),
///     Query: "token refresh",
///     Intent: QueryIntent.Procedural,
///     ResultCount: 12,
///     TopResultScore: 0.92f,
///     ExecutedAt: DateTime.UtcNow,
///     DurationMs: 145));
///
/// // Get recent queries for UI
/// var recent = await _historyService.GetRecentAsync(limit: 5);
///
/// // Identify content gaps
/// var gaps = await _historyService.GetZeroResultQueriesAsync(
///     since: DateTime.UtcNow.AddDays(-30));
/// </code>
/// </example>
public interface IQueryHistoryService
{
    /// <summary>
    /// Records an executed query with its results.
    /// </summary>
    /// <param name="entry">
    /// The query history entry to record. MUST NOT be null.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancellation token for async operation.
    /// </param>
    /// <remarks>
    /// <para>
    /// Recording triggers:
    /// <list type="bullet">
    ///   <item><description>Persistent storage of query metadata</description></item>
    ///   <item><description>Publishing of <see cref="QueryAnalyticsEvent"/> (if telemetry enabled)</description></item>
    ///   <item><description>Zero-result tracking for content gap analysis</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Deduplication:</b> Identical queries within a short window (60s)
    /// may be deduplicated to avoid history pollution from repeated searches.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="entry"/> is null.
    /// </exception>
    Task RecordAsync(QueryHistoryEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent queries for quick access.
    /// </summary>
    /// <param name="limit">
    /// Maximum entries to return (default: 10).
    /// </param>
    /// <param name="cancellationToken">
    /// Cancellation token for async operation.
    /// </param>
    /// <returns>
    /// Recent queries ordered by execution time descending (newest first).
    /// </returns>
    /// <remarks>
    /// Used to populate the "Recent Searches" section in the search panel.
    /// </remarks>
    Task<IReadOnlyList<QueryHistoryEntry>> GetRecentAsync(
        int limit = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets queries that returned zero results (content gaps).
    /// </summary>
    /// <param name="since">
    /// Start date for analysis (UTC). Queries before this date are excluded.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancellation token for async operation.
    /// </param>
    /// <returns>
    /// Aggregated zero-result queries with occurrence counts, ordered by count descending.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Zero-result queries indicate content that users are searching for but
    /// doesn't exist in the documentation. This helps authors identify:
    /// <list type="bullet">
    ///   <item><description>Missing documentation topics</description></item>
    ///   <item><description>Terminology mismatches (users search with different words)</description></item>
    ///   <item><description>Index coverage gaps</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Aggregation:</b> Similar queries (normalized, case-insensitive) are
    /// grouped and counted together.
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<ZeroResultQuery>> GetZeroResultQueriesAsync(
        DateTime since,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears query history.
    /// </summary>
    /// <param name="olderThan">
    /// Optional: only clear entries older than this date (UTC).
    /// If null, clears all history.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancellation token for async operation.
    /// </param>
    /// <remarks>
    /// <para>
    /// Provides privacy-friendly history management:
    /// <list type="bullet">
    ///   <item><description>Clear all: Removes entire history</description></item>
    ///   <item><description>Clear older than: Retains recent history, removes old entries</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    Task ClearAsync(DateTime? olderThan = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of recorded queries.
    /// </summary>
    /// <param name="cancellationToken">
    /// Cancellation token for async operation.
    /// </param>
    /// <returns>Total number of queries in history.</returns>
    Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default);
}
