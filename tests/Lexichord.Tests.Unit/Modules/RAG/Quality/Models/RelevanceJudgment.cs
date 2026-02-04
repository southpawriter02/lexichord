// ════════════════════════════════════════════════════════════════════════════
// Lexichord — v0.5.8a: Retrieval Quality Tests
// ════════════════════════════════════════════════════════════════════════════
// File: RelevanceJudgment.cs
// Purpose: Gold-standard relevance judgments associating queries with relevant chunks.
// ════════════════════════════════════════════════════════════════════════════

namespace Lexichord.Tests.Unit.Modules.RAG.Quality.Models;

/// <summary>
/// Associates test queries with their relevant document chunks for quality evaluation.
/// </summary>
/// <remarks>
/// <para>
/// Relevance judgments form the "gold standard" ground truth for evaluating retrieval
/// quality. Each judgment maps a query to the set of chunks that human evaluators have
/// deemed relevant, along with their relative importance.
/// </para>
/// <para>
/// The scoring system uses a 0-3 scale:
/// <list type="bullet">
///   <item><description>0: Not relevant</description></item>
///   <item><description>1: Marginally relevant (mentions topic)</description></item>
///   <item><description>2: Moderately relevant (partially answers query)</description></item>
///   <item><description>3: Highly relevant (directly answers query)</description></item>
/// </list>
/// </para>
/// </remarks>
/// <param name="QueryId">Reference to <see cref="TestQuery.Id"/>.</param>
/// <param name="RelevantChunkIds">
/// Ordered list of relevant chunk IDs, sorted by relevance (most relevant first).
/// </param>
/// <param name="PrimaryRelevant">
/// The single most relevant chunk ID (should be first in RelevantChunkIds).
/// </param>
/// <param name="RelevanceScores">
/// Optional mapping of chunk IDs to graded relevance scores (0-3) for NDCG calculation.
/// </param>
public record RelevanceJudgment(
    string QueryId,
    IReadOnlyList<Guid> RelevantChunkIds,
    Guid PrimaryRelevant,
    IReadOnlyDictionary<Guid, int>? RelevanceScores = null)
{
    /// <summary>
    /// Minimum relevance score (not relevant).
    /// </summary>
    public const int MinRelevanceScore = 0;

    /// <summary>
    /// Maximum relevance score (highly relevant).
    /// </summary>
    public const int MaxRelevanceScore = 3;

    /// <summary>
    /// Gets the relevance score for a specific chunk, defaulting to 0 if not found.
    /// </summary>
    /// <param name="chunkId">The chunk ID to look up.</param>
    /// <returns>The relevance score (0-3), or 0 if not in the judgment.</returns>
    public int GetRelevanceScore(Guid chunkId)
    {
        if (RelevanceScores is not null && RelevanceScores.TryGetValue(chunkId, out var score))
        {
            return score;
        }

        // Default: if in relevant list, assign score based on position
        var index = RelevantChunkIds.ToList().IndexOf(chunkId);
        return index switch
        {
            0 => 3, // First position = highly relevant
            1 or 2 => 2, // Positions 2-3 = moderately relevant
            >= 3 => 1, // Later positions = marginally relevant
            _ => 0 // Not found = not relevant
        };
    }

    /// <summary>
    /// Validates that this judgment has consistent data.
    /// </summary>
    /// <returns>True if the judgment passes validation.</returns>
    public bool IsValid() =>
        !string.IsNullOrWhiteSpace(QueryId) &&
        RelevantChunkIds.Count > 0 &&
        RelevantChunkIds.Contains(PrimaryRelevant);
}
