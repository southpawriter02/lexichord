// ════════════════════════════════════════════════════════════════════════════
// Lexichord — v0.5.8a: Retrieval Quality Tests
// ════════════════════════════════════════════════════════════════════════════
// File: RetrievalMetricsCalculator.cs
// Purpose: Implementation of standard information retrieval quality metrics.
// ════════════════════════════════════════════════════════════════════════════

using Lexichord.Tests.Unit.Modules.RAG.Quality.Models;

namespace Lexichord.Tests.Unit.Modules.RAG.Quality;

/// <summary>
/// Implements standard information retrieval quality metrics.
/// </summary>
/// <remarks>
/// <para>
/// This implementation follows standard IR evaluation methodology as described in
/// academic literature on information retrieval evaluation. All metrics are calculated
/// according to their canonical definitions.
/// </para>
/// <para>
/// Thread-safe: this class is stateless and can be used concurrently.
/// </para>
/// </remarks>
public sealed class RetrievalMetricsCalculator : IRetrievalMetricsCalculator
{
    /// <inheritdoc />
    public double PrecisionAtK(
        IReadOnlyList<Guid> results,
        IReadOnlySet<Guid> relevant,
        int k)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(k, nameof(k));

        // Handle edge cases: empty results or no relevant items
        if (results.Count == 0 || relevant.Count == 0)
        {
            return 0.0;
        }

        // Take only top k results (or all if fewer than k)
        var topK = results.Take(k);

        // Count how many of the top k are relevant
        var relevantInTopK = topK.Count(id => relevant.Contains(id));

        // Precision = relevant in top k / k
        return (double)relevantInTopK / k;
    }

    /// <inheritdoc />
    public double RecallAtK(
        IReadOnlyList<Guid> results,
        IReadOnlySet<Guid> relevant,
        int k)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(k, nameof(k));

        // Handle edge cases: empty results or no relevant items
        if (results.Count == 0 || relevant.Count == 0)
        {
            return 0.0;
        }

        // Take only top k results (or all if fewer than k)
        var topK = results.Take(k);

        // Count how many of the top k are relevant
        var relevantInTopK = topK.Count(id => relevant.Contains(id));

        // Recall = relevant in top k / total relevant
        return (double)relevantInTopK / relevant.Count;
    }

    /// <inheritdoc />
    public double F1AtK(
        IReadOnlyList<Guid> results,
        IReadOnlySet<Guid> relevant,
        int k)
    {
        var precision = PrecisionAtK(results, relevant, k);
        var recall = RecallAtK(results, relevant, k);

        // Handle case where both are zero (avoid division by zero)
        if (precision + recall == 0)
        {
            return 0.0;
        }

        // F1 = harmonic mean of precision and recall
        return 2 * (precision * recall) / (precision + recall);
    }

    /// <inheritdoc />
    public double MeanReciprocalRank(IReadOnlyList<QueryResult> queryResults)
    {
        // Handle empty input
        if (queryResults.Count == 0)
        {
            return 0.0;
        }

        // Calculate reciprocal rank for each query
        var reciprocalRanks = queryResults
            .Select(qr => qr.FirstRelevantRank > 0
                ? 1.0 / qr.FirstRelevantRank
                : 0.0);

        // MRR = average of reciprocal ranks
        return reciprocalRanks.Average();
    }

    /// <inheritdoc />
    public double NdcgAtK(
        IReadOnlyList<Guid> results,
        IReadOnlyDictionary<Guid, int> relevanceScores,
        int k)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(k, nameof(k));

        // Handle edge case: no relevance scores
        if (relevanceScores.Count == 0)
        {
            return 0.0;
        }

        // Calculate DCG for actual ranking
        var dcg = CalculateDcg(results.Take(k), relevanceScores);

        // Calculate IDCG (ideal ranking by relevance score descending)
        var idealRanking = relevanceScores
            .OrderByDescending(kv => kv.Value)
            .Select(kv => kv.Key)
            .Take(k)
            .ToList();
        var idcg = CalculateDcg(idealRanking, relevanceScores);

        // Handle case where IDCG is 0 (no relevant items)
        if (idcg <= 0)
        {
            return 0.0;
        }

        // NDCG = DCG / IDCG
        return dcg / idcg;
    }

    /// <inheritdoc />
    public QualityReport GenerateReport(
        IReadOnlyList<QueryResult> queryResults,
        IReadOnlyList<TestQuery> testQueries)
    {
        if (queryResults.Count == 0)
        {
            return new QualityReport(
                TotalQueries: 0,
                AveragePrecisionAt5: 0.0,
                AveragePrecisionAt10: 0.0,
                AverageRecallAt10: 0.0,
                Mrr: 0.0,
                QueriesWithNoRelevant: 0,
                ByCategory: new Dictionary<string, CategoryMetrics>(),
                ByDifficulty: new Dictionary<string, CategoryMetrics>());
        }

        // Build lookup from query ID to test query metadata
        var queryLookup = testQueries.ToDictionary(q => q.Id, q => q);

        // Calculate aggregate metrics
        var precisionAt5Values = new List<double>();
        var precisionAt10Values = new List<double>();
        var recallAt10Values = new List<double>();
        var queriesWithNoRelevant = 0;

        foreach (var qr in queryResults)
        {
            precisionAt5Values.Add(PrecisionAtK(qr.RetrievedIds, qr.RelevantIds, 5));
            precisionAt10Values.Add(PrecisionAtK(qr.RetrievedIds, qr.RelevantIds, 10));
            recallAt10Values.Add(RecallAtK(qr.RetrievedIds, qr.RelevantIds, 10));

            if (qr.FirstRelevantRank == 0)
            {
                queriesWithNoRelevant++;
            }
        }

        var avgPrecisionAt5 = precisionAt5Values.Average();
        var avgPrecisionAt10 = precisionAt10Values.Average();
        var avgRecallAt10 = recallAt10Values.Average();
        var mrr = MeanReciprocalRank(queryResults);

        // Calculate metrics by category
        var byCategory = CalculateStratifiedMetrics(
            queryResults,
            queryLookup,
            q => q.Category);

        // Calculate metrics by difficulty
        var byDifficulty = CalculateStratifiedMetrics(
            queryResults,
            queryLookup,
            q => q.Difficulty);

        return new QualityReport(
            TotalQueries: queryResults.Count,
            AveragePrecisionAt5: avgPrecisionAt5,
            AveragePrecisionAt10: avgPrecisionAt10,
            AverageRecallAt10: avgRecallAt10,
            Mrr: mrr,
            QueriesWithNoRelevant: queriesWithNoRelevant,
            ByCategory: byCategory,
            ByDifficulty: byDifficulty);
    }

    /// <summary>
    /// Calculates Discounted Cumulative Gain for a ranking.
    /// </summary>
    /// <param name="ranking">IDs in rank order.</param>
    /// <param name="relevanceScores">Mapping of ID to relevance score (0-3).</param>
    /// <returns>DCG value.</returns>
    private static double CalculateDcg(
        IEnumerable<Guid> ranking,
        IReadOnlyDictionary<Guid, int> relevanceScores)
    {
        return ranking
            .Select((id, index) =>
            {
                var relevance = relevanceScores.GetValueOrDefault(id, 0);
                var position = index + 1; // 1-based position
                // DCG formula: relevance / log2(position + 1)
                return relevance / Math.Log2(position + 1);
            })
            .Sum();
    }

    /// <summary>
    /// Calculates metrics stratified by a grouping key (category or difficulty).
    /// </summary>
    private Dictionary<string, CategoryMetrics> CalculateStratifiedMetrics(
        IReadOnlyList<QueryResult> queryResults,
        Dictionary<string, TestQuery> queryLookup,
        Func<TestQuery, string> groupKeySelector)
    {
        var result = new Dictionary<string, CategoryMetrics>();

        // Group query results by the specified key
        var groups = queryResults
            .Where(qr => queryLookup.ContainsKey(qr.QueryId))
            .GroupBy(qr => groupKeySelector(queryLookup[qr.QueryId]));

        foreach (var group in groups)
        {
            var groupResults = group.ToList();
            var p5Values = new List<double>();
            var r10Values = new List<double>();

            foreach (var qr in groupResults)
            {
                p5Values.Add(PrecisionAtK(qr.RetrievedIds, qr.RelevantIds, 5));
                r10Values.Add(RecallAtK(qr.RetrievedIds, qr.RelevantIds, 10));
            }

            result[group.Key] = new CategoryMetrics(
                QueryCount: groupResults.Count,
                PrecisionAt5: p5Values.Average(),
                RecallAt10: r10Values.Average(),
                Mrr: MeanReciprocalRank(groupResults));
        }

        return result;
    }
}
