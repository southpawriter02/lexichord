// =============================================================================
// File: DegradedSearchMode.cs
// Project: Lexichord.Abstractions
// Description: Defines the operating mode when search services are degraded.
// =============================================================================
// VERSION: v0.5.8d (Error Resilience)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Defines the current operating mode of the resilient search service
/// when one or more dependencies are unavailable.
/// </summary>
/// <remarks>
/// <para>
/// The modes form a degradation hierarchy from full functionality to unavailable:
/// <list type="bullet">
///   <item><description><see cref="Full"/>: All services operational</description></item>
///   <item><description><see cref="KeywordOnly"/>: Embedding API unavailable, BM25 fallback</description></item>
///   <item><description><see cref="CachedOnly"/>: Database unavailable, serving cached results</description></item>
///   <item><description><see cref="Unavailable"/>: All services down</description></item>
/// </list>
/// </para>
/// <para>
/// LICENSE GATING: This enum is available to all license tiers but the
/// <see cref="IResilientSearchService"/> that uses it requires WriterPro.
/// </para>
/// </remarks>
public enum DegradedSearchMode
{
    /// <summary>
    /// All search services are fully operational.
    /// Hybrid search (semantic + keyword) is available.
    /// </summary>
    Full = 0,

    /// <summary>
    /// The embedding API is unavailable. Search falls back to BM25 keyword-only mode.
    /// Semantic similarity is not available but keyword matching continues to work.
    /// </summary>
    KeywordOnly = 1,

    /// <summary>
    /// The database is unavailable. Search falls back to cached results only.
    /// Only previously cached query results can be returned.
    /// </summary>
    CachedOnly = 2,

    /// <summary>
    /// All search services are unavailable.
    /// No search results can be provided.
    /// </summary>
    Unavailable = 3
}
