// =============================================================================
// File: SearchMode.cs
// Project: Lexichord.Abstractions
// Description: Defines the search strategy for document retrieval in the
//              Reference Panel, enabling users to select between semantic
//              (vector), keyword (BM25), and hybrid (RRF fusion) modes.
// =============================================================================
// LOGIC: Enum with three values representing distinct search strategies:
//   - Semantic: Vector similarity search using embeddings (v0.4.5a).
//   - Keyword: BM25 keyword search using PostgreSQL full-text (v0.5.1b).
//   - Hybrid: Combined search using Reciprocal Rank Fusion (v0.5.1c).
//
//   License Gating:
//   - Semantic and Keyword modes are available to all tiers (Core+).
//   - Hybrid mode requires WriterPro tier or higher.
//
//   Defaults:
//   - WriterPro+ users default to Hybrid mode.
//   - Core users default to Semantic mode.
//
//   Persistence:
//   - The selected mode is persisted via ISystemSettingsRepository under
//     the key "Search.DefaultMode" for cross-session retention.
// =============================================================================
// DEPENDENCIES (referenced by version):
//   - v0.5.1c: IHybridSearchService (Hybrid mode target)
//   - v0.5.1b: IBM25SearchService (Keyword mode target)
//   - v0.4.5a: ISemanticSearchService (Semantic mode target)
//   - v0.0.4c: ILicenseContext (tier checking for Hybrid gating)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Defines the search strategy for document retrieval in the Reference Panel.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SearchMode"/> determines which search service is used when a user
/// executes a query in the Reference Panel. Each mode has different strengths:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       <see cref="Semantic"/>: Uses vector similarity to find conceptually
///       similar content, even when exact keywords don't match. Best for
///       natural language queries and exploring related topics.
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="Keyword"/>: Uses PostgreSQL full-text search (BM25) for
///       exact keyword matching with stemming. Best for finding specific
///       technical terms, API names, and identifiers.
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="Hybrid"/>: Combines both strategies using Reciprocal Rank
///       Fusion (RRF) for comprehensive results. Requires WriterPro license.
///     </description>
///   </item>
/// </list>
/// <para>
/// <b>License Gating:</b>
/// </para>
/// <list type="table">
///   <listheader>
///     <term>Tier</term>
///     <description>Available Modes</description>
///   </listheader>
///   <item><term>Core</term><description>Semantic, Keyword</description></item>
///   <item><term>WriterPro</term><description>Semantic, Keyword, Hybrid (default)</description></item>
///   <item><term>Teams</term><description>Semantic, Keyword, Hybrid (default)</description></item>
///   <item><term>Enterprise</term><description>Semantic, Keyword, Hybrid (default)</description></item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.5.1d as part of the Search Mode Toggle feature.
/// </para>
/// </remarks>
public enum SearchMode
{
    /// <summary>
    /// Vector similarity search using embeddings.
    /// </summary>
    /// <remarks>
    /// LOGIC: Delegates to <see cref="ISemanticSearchService"/> (v0.4.5a).
    /// Uses pgvector cosine distance to find conceptually similar content.
    /// Available to all license tiers. Default for Core users.
    /// </remarks>
    Semantic = 0,

    /// <summary>
    /// BM25 keyword search using PostgreSQL full-text search.
    /// </summary>
    /// <remarks>
    /// LOGIC: Delegates to <see cref="IBM25SearchService"/> (v0.5.1b).
    /// Uses ts_rank scoring against the ContentTsVector GIN index for
    /// exact keyword matching with English stemming and stop word removal.
    /// Available to all license tiers.
    /// </remarks>
    Keyword = 1,

    /// <summary>
    /// Combined search using Reciprocal Rank Fusion (RRF).
    /// </summary>
    /// <remarks>
    /// LOGIC: Delegates to <see cref="IHybridSearchService"/> (v0.5.1c).
    /// Executes both semantic and BM25 searches in parallel, then merges
    /// results using RRF with configurable weights (default: 0.7 semantic,
    /// 0.3 BM25). Chunks appearing in both result sets are naturally boosted.
    /// <b>Requires WriterPro license tier or higher.</b>
    /// Default for WriterPro+ users.
    /// </remarks>
    Hybrid = 2
}
