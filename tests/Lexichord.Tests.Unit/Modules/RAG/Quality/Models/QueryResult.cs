// ════════════════════════════════════════════════════════════════════════════
// Lexichord — v0.5.8a: Retrieval Quality Tests
// ════════════════════════════════════════════════════════════════════════════
// File: QueryResult.cs
// Purpose: Container for evaluated query results with retrieval and relevance data.
// ════════════════════════════════════════════════════════════════════════════

namespace Lexichord.Tests.Unit.Modules.RAG.Quality.Models;

/// <summary>
/// Result container for a single evaluated query in the quality test suite.
/// </summary>
/// <remarks>
/// <para>
/// Captures the outcome of executing a test query against the search system,
/// including the retrieved results, ground truth relevance, and performance timing.
/// This data is used to calculate quality metrics like Precision@K, Recall@K, and MRR.
/// </para>
/// <para>
/// The <see cref="FirstRelevantRank"/> field is critical for MRR calculation:
/// it records the 1-based rank position of the first relevant result, or 0 if
/// no relevant results were found in the retrieved set.
/// </para>
/// </remarks>
/// <param name="QueryId">Reference to <see cref="TestQuery.Id"/>.</param>
/// <param name="Query">The query string that was executed.</param>
/// <param name="RetrievedIds">
/// IDs of chunks returned by search, in rank order (best match first).
/// </param>
/// <param name="RelevantIds">
/// IDs of chunks marked as relevant in ground truth (from <see cref="RelevanceJudgment"/>).
/// </param>
/// <param name="FirstRelevantRank">
/// 1-based rank of the first relevant result in the retrieved list.
/// Set to 0 if no relevant results were found.
/// </param>
/// <param name="Duration">Time taken to execute the search query.</param>
public record QueryResult(
    string QueryId,
    string Query,
    IReadOnlyList<Guid> RetrievedIds,
    IReadOnlySet<Guid> RelevantIds,
    int FirstRelevantRank,
    TimeSpan Duration)
{
    /// <summary>
    /// Indicates whether any relevant results were found in the retrieved set.
    /// </summary>
    public bool HasRelevantResult => FirstRelevantRank > 0;

    /// <summary>
    /// The reciprocal rank for this query (1/rank of first relevant, or 0 if none).
    /// </summary>
    public double ReciprocalRank => FirstRelevantRank > 0 ? 1.0 / FirstRelevantRank : 0.0;

    /// <summary>
    /// Creates a QueryResult by finding the first relevant rank automatically.
    /// </summary>
    /// <param name="queryId">The test query ID.</param>
    /// <param name="query">The query string.</param>
    /// <param name="retrievedIds">Retrieved chunk IDs in rank order.</param>
    /// <param name="relevantIds">Ground truth relevant chunk IDs.</param>
    /// <param name="duration">Search execution duration.</param>
    /// <returns>A new QueryResult with calculated FirstRelevantRank.</returns>
    public static QueryResult Create(
        string queryId,
        string query,
        IReadOnlyList<Guid> retrievedIds,
        IReadOnlySet<Guid> relevantIds,
        TimeSpan duration)
    {
        var firstRelevantRank = 0;
        for (var i = 0; i < retrievedIds.Count; i++)
        {
            if (relevantIds.Contains(retrievedIds[i]))
            {
                firstRelevantRank = i + 1; // 1-based rank
                break;
            }
        }

        return new QueryResult(queryId, query, retrievedIds, relevantIds, firstRelevantRank, duration);
    }
}
