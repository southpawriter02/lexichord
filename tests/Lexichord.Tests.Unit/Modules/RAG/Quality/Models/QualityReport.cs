// ════════════════════════════════════════════════════════════════════════════
// Lexichord — v0.5.8a: Retrieval Quality Tests
// ════════════════════════════════════════════════════════════════════════════
// File: QualityReport.cs
// Purpose: Comprehensive retrieval quality report with aggregate and stratified metrics.
// ════════════════════════════════════════════════════════════════════════════

namespace Lexichord.Tests.Unit.Modules.RAG.Quality.Models;

/// <summary>
/// Comprehensive retrieval quality report with aggregate and stratified metrics.
/// </summary>
/// <remarks>
/// <para>
/// The quality report provides both overall metrics and breakdowns by query category
/// and difficulty level. This enables identification of specific areas where the
/// retrieval system excels or needs improvement.
/// </para>
/// <para>
/// Key metrics:
/// <list type="bullet">
///   <item><description>Precision@K: Proportion of retrieved results that are relevant</description></item>
///   <item><description>Recall@K: Proportion of relevant items that were retrieved</description></item>
///   <item><description>MRR: Mean Reciprocal Rank — average of 1/rank of first relevant result</description></item>
/// </list>
/// </para>
/// </remarks>
/// <param name="TotalQueries">Number of queries evaluated.</param>
/// <param name="AveragePrecisionAt5">Average Precision@5 across all queries.</param>
/// <param name="AveragePrecisionAt10">Average Precision@10 across all queries.</param>
/// <param name="AverageRecallAt10">Average Recall@10 across all queries.</param>
/// <param name="Mrr">Mean Reciprocal Rank across all queries.</param>
/// <param name="QueriesWithNoRelevant">Count of queries where no relevant results were found.</param>
/// <param name="ByCategory">Metrics broken down by query category (factual, conceptual, etc.).</param>
/// <param name="ByDifficulty">Metrics broken down by difficulty level (easy, medium, hard).</param>
public record QualityReport(
    int TotalQueries,
    double AveragePrecisionAt5,
    double AveragePrecisionAt10,
    double AverageRecallAt10,
    double Mrr,
    int QueriesWithNoRelevant,
    IReadOnlyDictionary<string, CategoryMetrics> ByCategory,
    IReadOnlyDictionary<string, CategoryMetrics> ByDifficulty)
{
    /// <summary>
    /// Target threshold for MRR (Mean Reciprocal Rank).
    /// </summary>
    public const double TargetMrr = 0.75;

    /// <summary>
    /// Target threshold for Precision@5.
    /// </summary>
    public const double TargetPrecisionAt5 = 0.80;

    /// <summary>
    /// Minimum acceptable threshold for MRR.
    /// </summary>
    public const double MinimumMrr = 0.70;

    /// <summary>
    /// Minimum acceptable threshold for Precision@5.
    /// </summary>
    public const double MinimumPrecisionAt5 = 0.70;

    /// <summary>
    /// Indicates whether the report meets the target quality thresholds.
    /// </summary>
    public bool MeetsTargetThresholds =>
        Mrr >= TargetMrr && AveragePrecisionAt5 >= TargetPrecisionAt5;

    /// <summary>
    /// Indicates whether the report meets the minimum acceptable thresholds.
    /// </summary>
    public bool MeetsMinimumThresholds =>
        Mrr >= MinimumMrr && AveragePrecisionAt5 >= MinimumPrecisionAt5;

    /// <summary>
    /// Percentage of queries that found at least one relevant result.
    /// </summary>
    public double SuccessRate =>
        TotalQueries > 0 ? (double)(TotalQueries - QueriesWithNoRelevant) / TotalQueries : 0.0;
}

/// <summary>
/// Metrics for a specific category or difficulty level.
/// </summary>
/// <remarks>
/// Enables stratified analysis to identify which query types perform well
/// and which need improvement. Each stratification includes its own
/// precision, recall, and MRR calculations.
/// </remarks>
/// <param name="QueryCount">Number of queries in this category/difficulty.</param>
/// <param name="PrecisionAt5">Average Precision@5 for queries in this group.</param>
/// <param name="RecallAt10">Average Recall@10 for queries in this group.</param>
/// <param name="Mrr">Mean Reciprocal Rank for queries in this group.</param>
public record CategoryMetrics(
    int QueryCount,
    double PrecisionAt5,
    double RecallAt10,
    double Mrr);
