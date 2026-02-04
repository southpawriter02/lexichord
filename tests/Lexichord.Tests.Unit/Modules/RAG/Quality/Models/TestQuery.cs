// ════════════════════════════════════════════════════════════════════════════
// Lexichord — v0.5.8a: Retrieval Quality Tests
// ════════════════════════════════════════════════════════════════════════════
// File: TestQuery.cs
// Purpose: Represents a single test query with expected results for quality evaluation.
// ════════════════════════════════════════════════════════════════════════════

namespace Lexichord.Tests.Unit.Modules.RAG.Quality.Models;

/// <summary>
/// Represents a single test query with expected results for quality evaluation.
/// </summary>
/// <remarks>
/// <para>
/// Test queries are curated entries in the quality test corpus, each with metadata
/// describing the nature and difficulty of the query. This enables stratified analysis
/// of retrieval quality across different query types.
/// </para>
/// <para>
/// Categories help identify which types of queries the search system handles well
/// and which need improvement. Difficulty levels enable regression detection at
/// different complexity tiers.
/// </para>
/// </remarks>
/// <param name="Id">Unique query identifier (e.g., "Q001", "Q042").</param>
/// <param name="Query">The natural language query string to execute.</param>
/// <param name="Category">
/// Query category for analysis. Valid values:
/// <list type="bullet">
///   <item><description>factual: Questions with specific factual answers</description></item>
///   <item><description>conceptual: Questions about concepts or principles</description></item>
///   <item><description>procedural: "How to" questions about processes</description></item>
///   <item><description>comparative: Questions comparing two or more items</description></item>
///   <item><description>troubleshoot: Debugging or problem-solving queries</description></item>
/// </list>
/// </param>
/// <param name="Difficulty">
/// Difficulty level for stratified analysis. Valid values:
/// <list type="bullet">
///   <item><description>easy: Exact keyword matches expected</description></item>
///   <item><description>medium: Semantic understanding required</description></item>
///   <item><description>hard: Multi-hop reasoning or sparse keywords</description></item>
/// </list>
/// </param>
/// <param name="Description">Human-readable description of the query intent for corpus documentation.</param>
public record TestQuery(
    string Id,
    string Query,
    string Category,
    string Difficulty,
    string Description)
{
    /// <summary>
    /// Valid query categories for corpus classification.
    /// </summary>
    public static readonly IReadOnlyList<string> ValidCategories =
    [
        "factual",
        "conceptual",
        "procedural",
        "comparative",
        "troubleshoot"
    ];

    /// <summary>
    /// Valid difficulty levels for stratified analysis.
    /// </summary>
    public static readonly IReadOnlyList<string> ValidDifficulties =
    [
        "easy",
        "medium",
        "hard"
    ];

    /// <summary>
    /// Validates that this query has valid category and difficulty values.
    /// </summary>
    /// <returns>True if the query passes validation.</returns>
    public bool IsValid() =>
        !string.IsNullOrWhiteSpace(Id) &&
        !string.IsNullOrWhiteSpace(Query) &&
        ValidCategories.Contains(Category) &&
        ValidDifficulties.Contains(Difficulty);
}
