// ════════════════════════════════════════════════════════════════════════════
// Lexichord — v0.5.8a: Retrieval Quality Tests
// ════════════════════════════════════════════════════════════════════════════
// File: IRetrievalMetricsCalculator.cs
// Purpose: Interface for standard information retrieval quality metrics.
// ════════════════════════════════════════════════════════════════════════════

using Lexichord.Tests.Unit.Modules.RAG.Quality.Models;

namespace Lexichord.Tests.Unit.Modules.RAG.Quality;

/// <summary>
/// Calculates standard information retrieval quality metrics.
/// </summary>
/// <remarks>
/// <para>
/// Implements standard IR evaluation metrics as defined in academic literature:
/// <list type="bullet">
///   <item><description>Precision@K: Proportion of retrieved results that are relevant</description></item>
///   <item><description>Recall@K: Proportion of relevant items that were retrieved</description></item>
///   <item><description>F1@K: Harmonic mean of Precision and Recall</description></item>
///   <item><description>MRR: Mean Reciprocal Rank — average of 1/rank of first relevant result</description></item>
///   <item><description>NDCG@K: Normalized Discounted Cumulative Gain for graded relevance</description></item>
/// </list>
/// </para>
/// <para>
/// These metrics are used to evaluate retrieval quality against a gold-standard
/// test corpus with human-annotated relevance judgments.
/// </para>
/// </remarks>
public interface IRetrievalMetricsCalculator
{
    /// <summary>
    /// Calculates Precision@K: the proportion of relevant results in top K.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Precision@K = |Relevant ∩ Retrieved@K| / K
    /// </para>
    /// <para>
    /// This metric answers: "Of the top K results, how many are relevant?"
    /// High precision indicates the system returns mostly relevant results.
    /// </para>
    /// </remarks>
    /// <param name="results">Retrieved result IDs in rank order (best match first).</param>
    /// <param name="relevant">Set of IDs marked as relevant in ground truth.</param>
    /// <param name="k">Number of top results to consider.</param>
    /// <returns>Precision value between 0.0 and 1.0.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If k is not positive.</exception>
    double PrecisionAtK(IReadOnlyList<Guid> results, IReadOnlySet<Guid> relevant, int k);

    /// <summary>
    /// Calculates Recall@K: the proportion of relevant items found in top K.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Recall@K = |Relevant ∩ Retrieved@K| / |Relevant|
    /// </para>
    /// <para>
    /// This metric answers: "Of all relevant items, how many did we find in top K?"
    /// High recall indicates the system finds most relevant documents.
    /// </para>
    /// </remarks>
    /// <param name="results">Retrieved result IDs in rank order.</param>
    /// <param name="relevant">Set of IDs marked as relevant in ground truth.</param>
    /// <param name="k">Number of top results to consider.</param>
    /// <returns>Recall value between 0.0 and 1.0.</returns>
    /// <exception cref="ArgumentOutOfRangeException">If k is not positive.</exception>
    double RecallAtK(IReadOnlyList<Guid> results, IReadOnlySet<Guid> relevant, int k);

    /// <summary>
    /// Calculates F1 Score at K: harmonic mean of Precision@K and Recall@K.
    /// </summary>
    /// <remarks>
    /// <para>
    /// F1@K = 2 × (Precision@K × Recall@K) / (Precision@K + Recall@K)
    /// </para>
    /// <para>
    /// F1 balances precision and recall — useful when both false positives
    /// and false negatives are costly.
    /// </para>
    /// </remarks>
    /// <param name="results">Retrieved result IDs in rank order.</param>
    /// <param name="relevant">Set of IDs marked as relevant in ground truth.</param>
    /// <param name="k">Number of top results to consider.</param>
    /// <returns>F1 value between 0.0 and 1.0.</returns>
    double F1AtK(IReadOnlyList<Guid> results, IReadOnlySet<Guid> relevant, int k);

    /// <summary>
    /// Calculates Mean Reciprocal Rank (MRR) across multiple queries.
    /// </summary>
    /// <remarks>
    /// <para>
    /// MRR = (1/N) × Σ(1/rank_i) where rank_i is the rank of the first relevant result.
    /// </para>
    /// <para>
    /// MRR emphasizes how highly the first relevant result is ranked. A perfect
    /// MRR of 1.0 means the first relevant result is always ranked #1.
    /// </para>
    /// </remarks>
    /// <param name="queryResults">Results for each evaluated query.</param>
    /// <returns>MRR value between 0.0 and 1.0.</returns>
    double MeanReciprocalRank(IReadOnlyList<QueryResult> queryResults);

    /// <summary>
    /// Calculates Normalized Discounted Cumulative Gain at K.
    /// </summary>
    /// <remarks>
    /// <para>
    /// NDCG accounts for graded relevance (not just binary relevant/not relevant)
    /// and discounts relevance by position — highly relevant items should appear
    /// earlier in the ranking.
    /// </para>
    /// <para>
    /// DCG = Σ (relevance_i / log2(position_i + 1))
    /// NDCG = DCG / IDCG (ideal DCG with perfect ranking)
    /// </para>
    /// </remarks>
    /// <param name="results">Retrieved result IDs in rank order.</param>
    /// <param name="relevanceScores">Mapping of chunk ID to relevance score (0-3).</param>
    /// <param name="k">Number of top results to consider.</param>
    /// <returns>NDCG value between 0.0 and 1.0.</returns>
    double NdcgAtK(
        IReadOnlyList<Guid> results,
        IReadOnlyDictionary<Guid, int> relevanceScores,
        int k);

    /// <summary>
    /// Generates a comprehensive quality report for a set of queries.
    /// </summary>
    /// <remarks>
    /// Aggregates metrics across all queries and provides breakdowns by
    /// category and difficulty level for stratified analysis.
    /// </remarks>
    /// <param name="queryResults">Results for each evaluated query.</param>
    /// <param name="testQueries">Original test queries for category/difficulty metadata.</param>
    /// <returns>Aggregate metrics and per-category/difficulty breakdown.</returns>
    QualityReport GenerateReport(
        IReadOnlyList<QueryResult> queryResults,
        IReadOnlyList<TestQuery> testQueries);
}
