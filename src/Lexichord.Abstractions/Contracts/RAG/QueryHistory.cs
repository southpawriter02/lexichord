// =============================================================================
// File: QueryHistory.cs
// Project: Lexichord.Abstractions
// Description: Query history types for the Relevance Tuner feature (v0.5.4d).
// =============================================================================
// LOGIC: Defines records for query history tracking and analytics:
//   - QueryHistoryEntry: A recorded query with execution details
//   - ZeroResultQuery: A query that returned no results (content gap)
// =============================================================================
// VERSION: v0.5.4d (Query History & Analytics)
// DEPENDENCIES:
//   - QueryIntent (v0.5.4a) for intent tracking
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// A recorded query with execution details.
/// </summary>
/// <param name="Id">Unique identifier for this history entry.</param>
/// <param name="Query">The search query text.</param>
/// <param name="Intent">Detected query intent (Factual, Procedural, etc.).</param>
/// <param name="ResultCount">Number of results returned.</param>
/// <param name="TopResultScore">Score of the best result, or null if no results.</param>
/// <param name="ExecutedAt">When the query was executed (UTC).</param>
/// <param name="DurationMs">Execution time in milliseconds.</param>
/// <remarks>
/// <para>
/// <see cref="QueryHistoryEntry"/> enables:
/// <list type="bullet">
///   <item><description>Recent queries quick-access panel</description></item>
///   <item><description>Search performance monitoring</description></item>
///   <item><description>Zero-result query identification</description></item>
///   <item><description>Intent distribution analytics</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Privacy:</b> Query history is stored locally only. The
/// <see cref="QueryAnalyticsEvent"/> uses hashed query text for opt-in telemetry.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.4d as part of The Relevance Tuner feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var entry = new QueryHistoryEntry(
///     Id: Guid.NewGuid(),
///     Query: "how to configure OAuth",
///     Intent: QueryIntent.Procedural,
///     ResultCount: 12,
///     TopResultScore: 0.92f,
///     ExecutedAt: DateTime.UtcNow,
///     DurationMs: 145);
/// </code>
/// </example>
public record QueryHistoryEntry(
    Guid Id,
    string Query,
    QueryIntent Intent,
    int ResultCount,
    float? TopResultScore,
    DateTime ExecutedAt,
    long DurationMs)
{
    /// <summary>
    /// Gets whether this query returned results.
    /// </summary>
    public bool HasResults => ResultCount > 0;

    /// <summary>
    /// Gets whether this is a zero-result query (potential content gap).
    /// </summary>
    public bool IsZeroResult => ResultCount == 0;

    /// <summary>
    /// Gets the duration formatted as a display string.
    /// </summary>
    public string DurationDisplay => DurationMs < 1000
        ? $"{DurationMs}ms"
        : $"{DurationMs / 1000.0:F1}s";

    /// <summary>
    /// Gets a relative time description (e.g., "2 min ago").
    /// </summary>
    public string RelativeTime
    {
        get
        {
            var elapsed = DateTime.UtcNow - ExecutedAt;
            return elapsed.TotalMinutes switch
            {
                < 1 => "just now",
                < 60 => $"{(int)elapsed.TotalMinutes} min ago",
                < 1440 => $"{(int)elapsed.TotalHours} hour{((int)elapsed.TotalHours == 1 ? "" : "s")} ago",
                _ => $"{(int)elapsed.TotalDays} day{((int)elapsed.TotalDays == 1 ? "" : "s")} ago"
            };
        }
    }
}

/// <summary>
/// A query that returned no results, representing a potential content gap.
/// </summary>
/// <param name="Query">The query text.</param>
/// <param name="OccurrenceCount">How many times this was searched.</param>
/// <param name="LastSearchedAt">Most recent search attempt (UTC).</param>
/// <param name="SuggestedContent">AI-suggested content to create (future feature).</param>
/// <remarks>
/// <para>
/// <see cref="ZeroResultQuery"/> helps content authors identify gaps in their
/// documentation by surfacing commonly searched terms that don't produce results.
/// </para>
/// <para>
/// <b>Aggregation Logic:</b>
/// Queries are normalized (lowercase, trimmed) for deduplication.
/// Similar queries (edit distance &lt; 3) may be grouped in future versions.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.4d as part of The Relevance Tuner feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var gaps = await historyService.GetZeroResultQueriesAsync(since: DateTime.UtcNow.AddDays(-30));
/// // Returns queries like:
/// // - "kubernetes deployment" (searched 5 times)
/// // - "graphql schema" (searched 3 times)
/// </code>
/// </example>
public record ZeroResultQuery(
    string Query,
    int OccurrenceCount,
    DateTime LastSearchedAt,
    string? SuggestedContent)
{
    /// <summary>
    /// Gets the priority for addressing this content gap.
    /// </summary>
    /// <remarks>
    /// Higher occurrence counts indicate higher priority gaps.
    /// </remarks>
    public string Priority => OccurrenceCount switch
    {
        >= 10 => "High",
        >= 5 => "Medium",
        _ => "Low"
    };
}
