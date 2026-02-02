// =============================================================================
// File: HybridSearchOptions.cs
// Project: Lexichord.Modules.RAG
// Description: Configuration options for hybrid search (RRF fusion weights).
// =============================================================================
// LOGIC: Defines tunable parameters for the Reciprocal Rank Fusion algorithm
//   used by HybridSearchService to merge BM25 and semantic search results.
//
//   Parameters:
//     - SemanticWeight: Relative importance of semantic (vector) search results.
//     - BM25Weight: Relative importance of BM25 (keyword) search results.
//     - RRFConstant: The k constant in the RRF formula; higher values reduce
//       the impact of rank differences between the two result sets.
//
//   Formula: RRF_score(chunk) = Σ (weight_i / (k + rank_i))
//
//   Defaults are calibrated for general-purpose retrieval where semantic
//   similarity is weighted more heavily than exact keyword matches, reflecting
//   the typical use case of writers searching for conceptually related content.
//
//   Dependencies:
//     - v0.5.1c: HybridSearchService (consumer)
// =============================================================================

namespace Lexichord.Modules.RAG.Search;

/// <summary>
/// Configuration options for hybrid search using Reciprocal Rank Fusion (RRF).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="HybridSearchOptions"/> controls the behavior of the
/// <see cref="HybridSearchService"/> when merging ranked result lists from
/// BM25 keyword search and semantic vector search.
/// </para>
/// <para>
/// <b>Reciprocal Rank Fusion Formula:</b>
/// <code>
/// RRF_score(chunk) = Σ (weight_i / (k + rank_i))
/// </code>
/// Where:
/// <list type="bullet">
///   <item><description><c>weight_i</c> is <see cref="SemanticWeight"/> or <see cref="BM25Weight"/>.</description></item>
///   <item><description><c>k</c> is <see cref="RRFConstant"/> (default 60).</description></item>
///   <item><description><c>rank_i</c> is the 1-based position in that ranking (contributes 0 if absent).</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Weight Guidelines:</b>
/// <list type="bullet">
///   <item><description>Default (0.7 semantic, 0.3 BM25): Best for general-purpose retrieval where
///   conceptual similarity matters more than exact keyword matches.</description></item>
///   <item><description>Equal (0.5, 0.5): Balanced retrieval for mixed content.</description></item>
///   <item><description>BM25-heavy (0.3 semantic, 0.7 BM25): Best for technical documentation
///   with exact API names and identifiers.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Injected via:</b> <c>IOptions&lt;HybridSearchOptions&gt;</c> in DI.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.1c as part of the Hybrid Fusion Algorithm.
/// </para>
/// </remarks>
public sealed record HybridSearchOptions
{
    /// <summary>
    /// Weight applied to semantic (vector similarity) search results in the RRF formula.
    /// </summary>
    /// <remarks>
    /// <para>
    /// LOGIC: Controls the relative contribution of semantic search results to the
    /// final fused ranking. A higher weight means semantic results have more influence
    /// on the final ordering.
    /// </para>
    /// <para>
    /// Semantic search excels at finding conceptually similar content, synonyms,
    /// and paraphrased information. It is less effective for exact technical terms.
    /// </para>
    /// </remarks>
    /// <value>
    /// A float between 0.0 and 1.0. Default: 0.7.
    /// </value>
    public float SemanticWeight { get; init; } = 0.7f;

    /// <summary>
    /// Weight applied to BM25 (keyword) search results in the RRF formula.
    /// </summary>
    /// <remarks>
    /// <para>
    /// LOGIC: Controls the relative contribution of BM25 keyword search results to the
    /// final fused ranking. A higher weight means keyword matches have more influence
    /// on the final ordering.
    /// </para>
    /// <para>
    /// BM25 search excels at finding exact keyword matches, technical terms, API names,
    /// and identifiers. It is less effective for conceptual or synonym-based queries.
    /// </para>
    /// </remarks>
    /// <value>
    /// A float between 0.0 and 1.0. Default: 0.3.
    /// </value>
    public float BM25Weight { get; init; } = 0.3f;

    /// <summary>
    /// The constant <c>k</c> in the Reciprocal Rank Fusion formula.
    /// </summary>
    /// <remarks>
    /// <para>
    /// LOGIC: The RRF constant controls how much rank position affects the fused score.
    /// Higher values reduce the impact of rank differences (i.e., rank 1 vs rank 5
    /// matters less). Lower values amplify rank differences.
    /// </para>
    /// <para>
    /// The default value of 60 is the industry-standard constant used in the original
    /// RRF paper (Cormack, Clarke, and Butt, 2009) and widely adopted in production
    /// RAG systems.
    /// </para>
    /// <para>
    /// <b>Example:</b> With k=60, a rank-1 item scores weight/(60+1) = weight/61,
    /// while a rank-5 item scores weight/(60+5) = weight/65 — a modest 6.6% difference.
    /// With k=1, the same positions would differ by 200%.
    /// </para>
    /// </remarks>
    /// <value>
    /// A positive integer. Default: 60.
    /// </value>
    public int RRFConstant { get; init; } = 60;

    /// <summary>
    /// Default configuration for hybrid search.
    /// </summary>
    /// <remarks>
    /// LOGIC: Provides a static default instance for use in DI registration
    /// via <c>Options.Create(HybridSearchOptions.Default)</c>.
    /// </remarks>
    public static HybridSearchOptions Default { get; } = new();
}
