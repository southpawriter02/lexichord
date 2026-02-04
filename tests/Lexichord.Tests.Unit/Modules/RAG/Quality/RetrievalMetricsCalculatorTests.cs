// ════════════════════════════════════════════════════════════════════════════
// Lexichord — v0.5.8a: Retrieval Quality Tests
// ════════════════════════════════════════════════════════════════════════════
// File: RetrievalMetricsCalculatorTests.cs
// Purpose: Comprehensive unit tests for retrieval quality metrics calculator.
// ════════════════════════════════════════════════════════════════════════════

using Lexichord.Tests.Unit.Modules.RAG.Quality;
using Lexichord.Tests.Unit.Modules.RAG.Quality.Models;

namespace Lexichord.Tests.Unit.Modules.RAG.Quality;

/// <summary>
/// Unit tests for <see cref="RetrievalMetricsCalculator"/>.
/// </summary>
/// <remarks>
/// Tests verify correct calculation of standard IR metrics:
/// <list type="bullet">
///   <item><description>Precision@K: proportion of relevant in top K</description></item>
///   <item><description>Recall@K: proportion of relevant items found</description></item>
///   <item><description>F1@K: harmonic mean of precision and recall</description></item>
///   <item><description>MRR: mean reciprocal rank</description></item>
///   <item><description>NDCG@K: normalized discounted cumulative gain</description></item>
/// </list>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.8a")]
public class RetrievalMetricsCalculatorTests
{
    private readonly IRetrievalMetricsCalculator _calculator = new RetrievalMetricsCalculator();

    #region Precision@K Tests

    /// <summary>
    /// Verifies Precision@K = 1.0 when all top K results are relevant.
    /// </summary>
    [Fact]
    public void PrecisionAtK_AllRelevant_ReturnsOne()
    {
        // Arrange: 5 retrieved, all 5 are relevant
        var results = CreateGuids(5);
        var relevant = results.ToHashSet();

        // Act
        var precision = _calculator.PrecisionAtK(results, relevant, 5);

        // Assert
        precision.Should().Be(1.0);
    }

    /// <summary>
    /// Verifies Precision@K = 0.0 when no top K results are relevant.
    /// </summary>
    [Fact]
    public void PrecisionAtK_NoneRelevant_ReturnsZero()
    {
        // Arrange: 5 retrieved, none are relevant
        var results = CreateGuids(5);
        var relevant = CreateGuids(3).ToHashSet(); // Different GUIDs

        // Act
        var precision = _calculator.PrecisionAtK(results, relevant, 5);

        // Assert
        precision.Should().Be(0.0);
    }

    /// <summary>
    /// Verifies Precision@K = 0.6 when 3 of 5 top results are relevant.
    /// </summary>
    [Fact]
    public void PrecisionAtK_PartialRelevant_ReturnsCorrectRatio()
    {
        // Arrange: 5 retrieved, 3 are relevant
        var results = CreateGuids(5);
        var relevant = new HashSet<Guid> { results[0], results[2], results[4] };

        // Act
        var precision = _calculator.PrecisionAtK(results, relevant, 5);

        // Assert
        precision.Should().BeApproximately(0.6, 0.001);
    }

    /// <summary>
    /// Verifies Precision@K handles K larger than result count.
    /// </summary>
    [Fact]
    public void PrecisionAtK_KLargerThanResults_DividesbyK()
    {
        // Arrange: 3 retrieved, 2 relevant, K = 10
        var results = CreateGuids(3);
        var relevant = new HashSet<Guid> { results[0], results[1] };

        // Act
        var precision = _calculator.PrecisionAtK(results, relevant, 10);

        // Assert: 2 relevant / 10 = 0.2
        precision.Should().BeApproximately(0.2, 0.001);
    }

    /// <summary>
    /// Verifies Precision@K throws for K = 0.
    /// </summary>
    [Fact]
    public void PrecisionAtK_ZeroK_ThrowsArgumentOutOfRange()
    {
        // Arrange
        var results = CreateGuids(5);
        var relevant = results.ToHashSet();

        // Act & Assert
        var act = () => _calculator.PrecisionAtK(results, relevant, 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Verifies Precision@K returns 0 for empty results.
    /// </summary>
    [Fact]
    public void PrecisionAtK_EmptyResults_ReturnsZero()
    {
        // Arrange
        var results = new List<Guid>();
        var relevant = CreateGuids(3).ToHashSet();

        // Act
        var precision = _calculator.PrecisionAtK(results, relevant, 5);

        // Assert
        precision.Should().Be(0.0);
    }

    #endregion

    #region Recall@K Tests

    /// <summary>
    /// Verifies Recall@K = 1.0 when all relevant items are in top K.
    /// </summary>
    [Fact]
    public void RecallAtK_AllRelevantFound_ReturnsOne()
    {
        // Arrange: 5 retrieved, 3 are relevant, all 3 in top 5
        var results = CreateGuids(5);
        var relevant = new HashSet<Guid> { results[0], results[1], results[2] };

        // Act
        var recall = _calculator.RecallAtK(results, relevant, 5);

        // Assert
        recall.Should().Be(1.0);
    }

    /// <summary>
    /// Verifies Recall@K handles partial retrieval correctly.
    /// </summary>
    [Fact]
    public void RecallAtK_PartialFound_ReturnsCorrectRatio()
    {
        // Arrange: 5 retrieved, 4 total relevant, 2 found in top 5
        var results = CreateGuids(5);
        var notInResults = CreateGuids(2);
        var relevant = new HashSet<Guid> { results[0], results[2], notInResults[0], notInResults[1] };

        // Act
        var recall = _calculator.RecallAtK(results, relevant, 5);

        // Assert: 2 found / 4 total = 0.5
        recall.Should().BeApproximately(0.5, 0.001);
    }

    /// <summary>
    /// Verifies Recall@K = 0 when no relevant items found.
    /// </summary>
    [Fact]
    public void RecallAtK_NoneFound_ReturnsZero()
    {
        // Arrange
        var results = CreateGuids(5);
        var relevant = CreateGuids(3).ToHashSet(); // Different GUIDs

        // Act
        var recall = _calculator.RecallAtK(results, relevant, 5);

        // Assert
        recall.Should().Be(0.0);
    }

    /// <summary>
    /// Verifies Recall@K returns 0 for empty relevant set.
    /// </summary>
    [Fact]
    public void RecallAtK_EmptyRelevantSet_ReturnsZero()
    {
        // Arrange
        var results = CreateGuids(5);
        var relevant = new HashSet<Guid>();

        // Act
        var recall = _calculator.RecallAtK(results, relevant, 5);

        // Assert
        recall.Should().Be(0.0);
    }

    #endregion

    #region F1@K Tests

    /// <summary>
    /// Verifies F1@K = 1.0 when precision and recall are both 1.0.
    /// </summary>
    [Fact]
    public void F1AtK_PerfectPrecisionAndRecall_ReturnsOne()
    {
        // Arrange: 5 retrieved = 5 relevant
        var results = CreateGuids(5);
        var relevant = results.ToHashSet();

        // Act
        var f1 = _calculator.F1AtK(results, relevant, 5);

        // Assert
        f1.Should().Be(1.0);
    }

    /// <summary>
    /// Verifies F1@K = 0 when precision and recall are both 0.
    /// </summary>
    [Fact]
    public void F1AtK_ZeroPrecisionAndRecall_ReturnsZero()
    {
        // Arrange
        var results = CreateGuids(5);
        var relevant = CreateGuids(3).ToHashSet();

        // Act
        var f1 = _calculator.F1AtK(results, relevant, 5);

        // Assert
        f1.Should().Be(0.0);
    }

    /// <summary>
    /// Verifies F1@K correctly calculates harmonic mean.
    /// </summary>
    [Fact]
    public void F1AtK_BalancedPrecisionRecall_ReturnsHarmonicMean()
    {
        // Arrange: P@5 = 2/5 = 0.4, R@5 = 2/4 = 0.5
        var results = CreateGuids(5);
        var notInResults = CreateGuids(2);
        var relevant = new HashSet<Guid> { results[0], results[1], notInResults[0], notInResults[1] };

        // Act
        var f1 = _calculator.F1AtK(results, relevant, 5);

        // Assert: F1 = 2 * 0.4 * 0.5 / (0.4 + 0.5) = 0.444...
        f1.Should().BeApproximately(0.444, 0.01);
    }

    #endregion

    #region MRR Tests

    /// <summary>
    /// Verifies MRR = 1.0 when first result is always relevant.
    /// </summary>
    [Fact]
    public void MeanReciprocalRank_FirstAlwaysRelevant_ReturnsOne()
    {
        // Arrange: 3 queries, all have first result relevant
        var queryResults = new List<QueryResult>
        {
            CreateQueryResult("Q1", firstRelevantRank: 1),
            CreateQueryResult("Q2", firstRelevantRank: 1),
            CreateQueryResult("Q3", firstRelevantRank: 1)
        };

        // Act
        var mrr = _calculator.MeanReciprocalRank(queryResults);

        // Assert
        mrr.Should().Be(1.0);
    }

    /// <summary>
    /// Verifies MRR calculation for varying first relevant ranks.
    /// </summary>
    [Fact]
    public void MeanReciprocalRank_VaryingRanks_ReturnsCorrectAverage()
    {
        // Arrange: ranks 1, 2, 4 → RR = 1, 0.5, 0.25 → MRR = 0.583...
        var queryResults = new List<QueryResult>
        {
            CreateQueryResult("Q1", firstRelevantRank: 1),
            CreateQueryResult("Q2", firstRelevantRank: 2),
            CreateQueryResult("Q3", firstRelevantRank: 4)
        };

        // Act
        var mrr = _calculator.MeanReciprocalRank(queryResults);

        // Assert: (1 + 0.5 + 0.25) / 3 = 0.583...
        mrr.Should().BeApproximately(0.583, 0.01);
    }

    /// <summary>
    /// Verifies MRR = 0 when no queries find relevant results.
    /// </summary>
    [Fact]
    public void MeanReciprocalRank_NoRelevantFound_ReturnsZero()
    {
        // Arrange: no relevant results found (rank = 0)
        var queryResults = new List<QueryResult>
        {
            CreateQueryResult("Q1", firstRelevantRank: 0),
            CreateQueryResult("Q2", firstRelevantRank: 0)
        };

        // Act
        var mrr = _calculator.MeanReciprocalRank(queryResults);

        // Assert
        mrr.Should().Be(0.0);
    }

    /// <summary>
    /// Verifies MRR for empty query list returns 0.
    /// </summary>
    [Fact]
    public void MeanReciprocalRank_EmptyList_ReturnsZero()
    {
        // Arrange
        var queryResults = new List<QueryResult>();

        // Act
        var mrr = _calculator.MeanReciprocalRank(queryResults);

        // Assert
        mrr.Should().Be(0.0);
    }

    #endregion

    #region NDCG@K Tests

    /// <summary>
    /// Verifies NDCG@K = 1.0 for perfect ranking (descending by relevance).
    /// </summary>
    [Fact]
    public void NdcgAtK_PerfectRanking_ReturnsOne()
    {
        // Arrange: items ranked perfectly by relevance score
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();
        var results = new List<Guid> { id1, id2, id3 };
        var relevanceScores = new Dictionary<Guid, int>
        {
            { id1, 3 },
            { id2, 2 },
            { id3, 1 }
        };

        // Act
        var ndcg = _calculator.NdcgAtK(results, relevanceScores, 3);

        // Assert
        ndcg.Should().Be(1.0);
    }

    /// <summary>
    /// Verifies NDCG@K &lt; 1.0 for imperfect ranking.
    /// </summary>
    [Fact]
    public void NdcgAtK_ImperfectRanking_ReturnsLessThanOne()
    {
        // Arrange: worst item ranked first
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();
        var results = new List<Guid> { id3, id2, id1 }; // Reversed
        var relevanceScores = new Dictionary<Guid, int>
        {
            { id1, 3 },
            { id2, 2 },
            { id3, 1 }
        };

        // Act
        var ndcg = _calculator.NdcgAtK(results, relevanceScores, 3);

        // Assert
        ndcg.Should().BeLessThan(1.0);
        ndcg.Should().BeGreaterThan(0.0);
    }

    /// <summary>
    /// Verifies NDCG@K = 0 for empty relevance scores.
    /// </summary>
    [Fact]
    public void NdcgAtK_EmptyRelevance_ReturnsZero()
    {
        // Arrange
        var results = CreateGuids(3);
        var relevanceScores = new Dictionary<Guid, int>();

        // Act
        var ndcg = _calculator.NdcgAtK(results, relevanceScores, 3);

        // Assert
        ndcg.Should().Be(0.0);
    }

    /// <summary>
    /// Verifies NDCG@K throws for K = 0.
    /// </summary>
    [Fact]
    public void NdcgAtK_ZeroK_ThrowsArgumentOutOfRange()
    {
        // Arrange
        var results = CreateGuids(3);
        var relevanceScores = new Dictionary<Guid, int>();

        // Act & Assert
        var act = () => _calculator.NdcgAtK(results, relevanceScores, 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region GenerateReport Tests

    /// <summary>
    /// Verifies GenerateReport produces correct aggregate metrics.
    /// </summary>
    [Fact]
    public void GenerateReport_MultipleQueries_CalculatesAggregates()
    {
        // Arrange
        var testQueries = new List<TestQuery>
        {
            new("Q1", "test query 1", "factual", "easy", "desc"),
            new("Q2", "test query 2", "conceptual", "medium", "desc")
        };

        var queryResults = new List<QueryResult>
        {
            CreateQueryResultWithData("Q1"),
            CreateQueryResultWithData("Q2")
        };

        // Act
        var report = _calculator.GenerateReport(queryResults, testQueries);

        // Assert
        report.TotalQueries.Should().Be(2);
        report.ByCategory.Should().ContainKey("factual");
        report.ByCategory.Should().ContainKey("conceptual");
        report.ByDifficulty.Should().ContainKey("easy");
        report.ByDifficulty.Should().ContainKey("medium");
    }

    /// <summary>
    /// Verifies GenerateReport handles empty input.
    /// </summary>
    [Fact]
    public void GenerateReport_EmptyInput_ReturnsEmptyReport()
    {
        // Arrange
        var queryResults = new List<QueryResult>();
        var testQueries = new List<TestQuery>();

        // Act
        var report = _calculator.GenerateReport(queryResults, testQueries);

        // Assert
        report.TotalQueries.Should().Be(0);
        report.Mrr.Should().Be(0.0);
        report.ByCategory.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies GenerateReport correctly identifies queries with no relevant results.
    /// </summary>
    [Fact]
    public void GenerateReport_SomeNoRelevant_CountsCorrectly()
    {
        // Arrange
        var testQueries = new List<TestQuery>
        {
            new("Q1", "q1", "factual", "easy", "d"),
            new("Q2", "q2", "factual", "easy", "d")
        };

        var queryResults = new List<QueryResult>
        {
            CreateQueryResult("Q1", firstRelevantRank: 1),
            CreateQueryResult("Q2", firstRelevantRank: 0) // No relevant found
        };

        // Act
        var report = _calculator.GenerateReport(queryResults, testQueries);

        // Assert
        report.QueriesWithNoRelevant.Should().Be(1);
    }

    #endregion

    #region Helper Methods

    private static List<Guid> CreateGuids(int count) =>
        Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToList();

    private static QueryResult CreateQueryResult(string queryId, int firstRelevantRank)
    {
        var retrieved = CreateGuids(10);
        var relevant = new HashSet<Guid> { retrieved[0] };

        return new QueryResult(
            queryId,
            "test query",
            retrieved,
            relevant,
            firstRelevantRank,
            TimeSpan.FromMilliseconds(50));
    }

    private static QueryResult CreateQueryResultWithData(string queryId)
    {
        var retrieved = CreateGuids(10);
        var relevant = new HashSet<Guid> { retrieved[0], retrieved[2], retrieved[4] };

        return QueryResult.Create(
            queryId,
            "test query",
            retrieved,
            relevant,
            TimeSpan.FromMilliseconds(50));
    }

    #endregion
}
