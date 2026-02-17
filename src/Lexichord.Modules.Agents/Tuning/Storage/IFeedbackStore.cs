// -----------------------------------------------------------------------
// <copyright file="IFeedbackStore.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Agents;

namespace Lexichord.Modules.Agents.Tuning.Storage;

/// <summary>
/// Internal interface for SQLite-based feedback persistence.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Abstracts the SQLite storage implementation to enable testing
/// with mock stores. The store manages two primary tables:
/// <list type="bullet">
///   <item><description><c>feedback</c> — Raw feedback records from user decisions</description></item>
///   <item><description><c>pattern_cache</c> — Computed aggregations of feedback patterns</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Visibility:</b> Internal to <c>Lexichord.Modules.Agents</c>. Accessible by
/// test projects via <c>InternalsVisibleTo</c>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5d as part of the Learning Loop feature.
/// </para>
/// </remarks>
/// <seealso cref="SqliteFeedbackStore"/>
/// <seealso cref="FeedbackRecord"/>
/// <seealso cref="PatternCacheRecord"/>
internal interface IFeedbackStore
{
    /// <summary>
    /// Initializes the database schema (creates tables if not exists).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous initialization.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Must be called once during application startup before any
    /// other store operations. Creates the <c>feedback</c> and <c>pattern_cache</c>
    /// tables with appropriate indexes.
    /// </remarks>
    Task InitializeAsync(CancellationToken ct = default);

    /// <summary>
    /// Stores a feedback record in the database.
    /// </summary>
    /// <param name="record">The feedback record to store.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StoreFeedbackAsync(FeedbackRecord record, CancellationToken ct = default);

    /// <summary>
    /// Gets feedback records for a specific rule, ordered by timestamp descending.
    /// </summary>
    /// <param name="ruleId">The rule ID to filter by.</param>
    /// <param name="limit">Maximum number of records to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Feedback records for the rule.</returns>
    Task<IReadOnlyList<FeedbackRecord>> GetFeedbackByRuleAsync(
        string ruleId,
        int limit = 1000,
        CancellationToken ct = default);

    /// <summary>
    /// Gets accepted pattern cache entries for a rule.
    /// </summary>
    /// <param name="ruleId">The rule ID to filter by.</param>
    /// <param name="minFrequency">Minimum occurrence count to include.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Accepted pattern cache records.</returns>
    Task<IReadOnlyList<PatternCacheRecord>> GetAcceptedPatternsAsync(
        string ruleId,
        int minFrequency = 2,
        CancellationToken ct = default);

    /// <summary>
    /// Gets rejected pattern cache entries for a rule.
    /// </summary>
    /// <param name="ruleId">The rule ID to filter by.</param>
    /// <param name="minFrequency">Minimum occurrence count to include.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Rejected pattern cache records.</returns>
    Task<IReadOnlyList<PatternCacheRecord>> GetRejectedPatternsAsync(
        string ruleId,
        int minFrequency = 2,
        CancellationToken ct = default);

    /// <summary>
    /// Gets aggregated statistics from the feedback store.
    /// </summary>
    /// <param name="filter">Optional filter criteria.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Aggregated statistics.</returns>
    Task<LearningStatistics> GetStatisticsAsync(
        LearningStatisticsFilter? filter = null,
        CancellationToken ct = default);

    /// <summary>
    /// Updates the pattern cache for a specific rule by re-aggregating feedback data.
    /// </summary>
    /// <param name="ruleId">The rule ID to update patterns for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Clears existing pattern cache entries for the rule and
    /// re-computes them from the feedback table. Groups feedback by
    /// (OriginalText, SuggestedText) and calculates acceptance/rejection counts.
    /// </remarks>
    Task UpdatePatternCacheAsync(string ruleId, CancellationToken ct = default);

    /// <summary>
    /// Clears learning data based on the provided options.
    /// </summary>
    /// <param name="options">Clear options specifying what to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ClearDataAsync(ClearLearningDataOptions options, CancellationToken ct = default);

    /// <summary>
    /// Gets the total count of feedback records, optionally filtered by date range.
    /// </summary>
    /// <param name="period">Optional date range filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The count of matching feedback records.</returns>
    Task<int> GetFeedbackCountAsync(DateRange? period = null, CancellationToken ct = default);

    /// <summary>
    /// Exports pattern data for team sharing.
    /// </summary>
    /// <param name="options">Export options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of exported patterns.</returns>
    Task<IReadOnlyList<ExportedPattern>> ExportPatternsAsync(
        LearningExportOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes feedback records older than the specified cutoff date.
    /// </summary>
    /// <param name="cutoff">Records older than this date are deleted.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The number of records deleted.</returns>
    Task<int> DeleteOlderThanAsync(DateTime cutoff, CancellationToken ct = default);

    /// <summary>
    /// Deletes the oldest N feedback records.
    /// </summary>
    /// <param name="count">Number of oldest records to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The number of records actually deleted.</returns>
    Task<int> DeleteOldestAsync(int count, CancellationToken ct = default);
}
