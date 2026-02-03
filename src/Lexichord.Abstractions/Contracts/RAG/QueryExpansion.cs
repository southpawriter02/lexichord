// =============================================================================
// File: QueryExpansion.cs
// Project: Lexichord.Abstractions
// Description: Query expansion types for the Relevance Tuner feature (v0.5.4b).
// =============================================================================
// LOGIC: Defines records and enums for query expansion including:
//   - ExpandedQuery: Query with synonyms and related terms added
//   - Synonym: A related term with weight and source
//   - SynonymSource: Origin of a synonym (database, algorithmic, content)
//   - ExpansionOptions: Configuration for expansion behavior
// =============================================================================
// VERSION: v0.5.4b (Query Expansion)
// DEPENDENCIES:
//   - QueryAnalysis (v0.5.4a) for keywords to expand
//   - ITerminologyRepository (v0.2.2b) for synonym lookup
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Source of a synonym or related term.
/// </summary>
/// <remarks>
/// <para>
/// Knowing the source of a synonym helps with:
/// <list type="bullet">
///   <item><description>Weight calibration (database terms may be more reliable)</description></item>
///   <item><description>UI display (show source to users)</description></item>
///   <item><description>Debugging expansion behavior</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.4b as part of The Relevance Tuner feature.
/// </para>
/// </remarks>
public enum SynonymSource
{
    /// <summary>
    /// From user-defined terminology database.
    /// </summary>
    /// <remarks>
    /// Highest confidence source; explicitly defined by users/admins.
    /// </remarks>
    TerminologyDatabase,

    /// <summary>
    /// Algorithmically derived (stemming, morphology).
    /// </summary>
    /// <remarks>
    /// Medium confidence; based on linguistic rules (e.g., "implementing" → "implement").
    /// </remarks>
    Algorithmic,

    /// <summary>
    /// From usage patterns in indexed content.
    /// </summary>
    /// <remarks>
    /// Variable confidence; learned from co-occurrence in documents.
    /// </remarks>
    ContentDerived
}

/// <summary>
/// A synonym or related term with relevance weight.
/// </summary>
/// <param name="Term">The synonym text.</param>
/// <param name="Weight">Relevance weight from 0.0 (weak relation) to 1.0 (strong relation).</param>
/// <param name="Source">Source of the synonym (database, algorithmic, content-derived).</param>
/// <remarks>
/// <para>
/// Synonyms are added to search queries to improve recall by matching
/// documents that use different terminology for the same concept.
/// </para>
/// <para>
/// The weight affects result ranking: lower-weight synonyms produce
/// matches with reduced relevance scores.
/// </para>
/// <para>
/// <b>Weight Guidelines:</b>
/// <list type="table">
///   <listheader>
///     <term>Weight Range</term>
///     <description>Meaning</description>
///   </listheader>
///   <item>
///     <term>0.8-1.0</term>
///     <description>Near-exact synonym (auth → authentication)</description>
///   </item>
///   <item>
///     <term>0.5-0.8</term>
///     <description>Related term (auth → login, credentials)</description>
///   </item>
///   <item>
///     <term>0.3-0.5</term>
///     <description>Loosely related (auth → security, access)</description>
///   </item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.4b as part of The Relevance Tuner feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Expanding "auth"
/// var synonyms = new[]
/// {
///     new Synonym("authentication", 0.95f, SynonymSource.TerminologyDatabase),
///     new Synonym("authorization", 0.85f, SynonymSource.TerminologyDatabase),
///     new Synonym("login", 0.7f, SynonymSource.TerminologyDatabase)
/// };
/// </code>
/// </example>
public record Synonym(string Term, float Weight, SynonymSource Source);

/// <summary>
/// Configuration options for query expansion.
/// </summary>
/// <param name="MaxSynonymsPerTerm">Maximum synonyms to add per keyword (default: 3).</param>
/// <param name="MinSynonymWeight">Minimum weight threshold for inclusion (default: 0.3).</param>
/// <param name="IncludeAlgorithmic">Whether to include stemming/morphological variants (default: true).</param>
/// <remarks>
/// <para>
/// Expansion options allow fine-tuning the tradeoff between recall (more synonyms)
/// and precision (fewer, higher-quality synonyms).
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.4b as part of The Relevance Tuner feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Conservative expansion: fewer synonyms, higher quality
/// var options = new ExpansionOptions(MaxSynonymsPerTerm: 2, MinSynonymWeight: 0.5f);
///
/// // Aggressive expansion: more synonyms, broader recall
/// var options = new ExpansionOptions(MaxSynonymsPerTerm: 5, MinSynonymWeight: 0.2f);
/// </code>
/// </example>
public record ExpansionOptions(
    int MaxSynonymsPerTerm = 3,
    float MinSynonymWeight = 0.3f,
    bool IncludeAlgorithmic = true)
{
    /// <summary>
    /// Default expansion options optimized for balanced recall/precision.
    /// </summary>
    public static ExpansionOptions Default { get; } = new();
}

/// <summary>
/// Query with expanded terms for broader search coverage.
/// </summary>
/// <param name="Original">The original query analysis.</param>
/// <param name="Expansions">Map of original term to expanded synonyms.</param>
/// <param name="ExpandedKeywords">All keywords including expansions (for search execution).</param>
/// <param name="TotalTermCount">Total number of search terms after expansion.</param>
/// <remarks>
/// <para>
/// <see cref="ExpandedQuery"/> contains both the original analysis and the
/// expanded terms, allowing the search pipeline to:
/// <list type="bullet">
///   <item><description>Execute search with expanded keywords</description></item>
///   <item><description>Display expansion info to users</description></item>
///   <item><description>Apply different weights to original vs. expanded terms</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.4b as part of The Relevance Tuner feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Original query: "api auth"
/// // Keywords: ["api", "auth"]
/// // Expanded:
/// //   "auth" → ["authentication", "authorization", "login"]
/// // ExpandedKeywords: ["api", "auth", "authentication", "authorization", "login"]
/// </code>
/// </example>
public record ExpandedQuery(
    QueryAnalysis Original,
    IReadOnlyDictionary<string, IReadOnlyList<Synonym>> Expansions,
    IReadOnlyList<string> ExpandedKeywords,
    int TotalTermCount)
{
    /// <summary>
    /// Gets whether any terms were expanded.
    /// </summary>
    public bool WasExpanded => Expansions.Count > 0;

    /// <summary>
    /// Gets the number of terms that were expanded.
    /// </summary>
    public int ExpandedTermCount => Expansions.Count;

    /// <summary>
    /// Gets the count of synonyms added across all expansions.
    /// </summary>
    public int SynonymCount => Expansions.Values.Sum(s => s.Count);

    /// <summary>
    /// Creates a non-expanded query (passthrough when expansion is disabled).
    /// </summary>
    /// <param name="analysis">The original query analysis.</param>
    /// <returns>An expanded query with no expansions.</returns>
    public static ExpandedQuery NoExpansion(QueryAnalysis analysis) =>
        new(
            Original: analysis,
            Expansions: new Dictionary<string, IReadOnlyList<Synonym>>(),
            ExpandedKeywords: analysis.Keywords,
            TotalTermCount: analysis.Keywords.Count);
}
