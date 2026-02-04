// =============================================================================
// File: SearchOptions.cs
// Project: Lexichord.Abstractions
// Description: Configuration options for semantic search operations, including
//              result limits, score thresholds, document filtering, and caching.
// =============================================================================
// LOGIC: Non-positional record with init-only properties and sensible defaults.
//   - Default static property provides zero-configuration usage.
//   - No Validate() method; validation is deferred to the ISemanticSearchService
//     implementation (v0.4.5b) which enforces constraints at search time.
//   - TopK controls result count (default 10, impl enforces 1..100 range).
//   - MinScore filters low-relevance results (default 0.7, impl enforces 0.0..1.0).
//   - DocumentFilter enables scoped search within a single document.
//   - ExpandAbbreviations triggers IQueryPreprocessor abbreviation expansion.
//   - UseCache enables short-lived query embedding caching (5 min TTL).
//   - v0.5.9f: Added deduplication-aware options:
//       - RespectCanonicals: Filter out variant chunks (default true).
//       - IncludeVariantMetadata: Include merge counts in results.
//       - IncludeArchived: Include archived chunks in results.
//       - IncludeProvenance: Load provenance records for each result.
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Configuration options for semantic search operations.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SearchOptions"/> controls how an <see cref="ISemanticSearchService"/> implementation
/// executes a semantic search query, including how many results to return, the minimum relevance
/// threshold, optional document-level filtering, abbreviation expansion, and embedding caching.
/// </para>
/// <para>
/// <b>Defaults:</b> Use <see cref="Default"/> for a production-ready configuration suitable for
/// most interactive search scenarios. Customize individual properties via the init accessor as needed.
/// </para>
/// <para>
/// <b>Validation:</b> This record does not perform self-validation. The consuming
/// <see cref="ISemanticSearchService"/> implementation is responsible for enforcing
/// constraints (e.g., <see cref="TopK"/> in range [1, 100], <see cref="MinScore"/> in range [0.0, 1.0]).
/// </para>
/// <para>
/// <b>Note:</b> This type is distinct from <c>Lexichord.Abstractions.Contracts.Editor.SearchOptions</c>,
/// which configures text-based find/replace operations. This <see cref="SearchOptions"/> is for
/// vector-based semantic search in the RAG subsystem.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5a as part of the Search Abstractions layer.
/// </para>
/// </remarks>
public record SearchOptions
{
    /// <summary>
    /// Default options with sensible defaults for interactive semantic search.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Configuration:</b>
    /// <list type="bullet">
    ///   <item><see cref="TopK"/> = 10</item>
    ///   <item><see cref="MinScore"/> = 0.7f</item>
    ///   <item><see cref="DocumentFilter"/> = <c>null</c> (search all documents)</item>
    ///   <item><see cref="ExpandAbbreviations"/> = <c>false</c></item>
    ///   <item><see cref="UseCache"/> = <c>true</c></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <value>
    /// A <see cref="SearchOptions"/> instance with production defaults.
    /// </value>
    public static SearchOptions Default { get; } = new();

    /// <summary>
    /// Maximum number of results to return.
    /// </summary>
    /// <remarks>
    /// LOGIC: Controls the upper bound on returned <see cref="SearchResult.Hits"/>.
    /// The implementation enforces that this value is between 1 and 100.
    /// Lower values reduce latency; higher values provide more comprehensive results.
    /// When the actual match count exceeds this limit, <see cref="SearchResult.WasTruncated"/>
    /// will be <c>true</c>.
    /// </remarks>
    /// <value>Default: 10 results.</value>
    public int TopK { get; init; } = 10;

    /// <summary>
    /// Minimum similarity score threshold (0.0 to 1.0).
    /// </summary>
    /// <remarks>
    /// LOGIC: Results with cosine similarity below this threshold are filtered out
    /// before being returned. Higher thresholds yield more precise but fewer results;
    /// lower thresholds cast a wider net but may include loosely related content.
    /// A score of 0.7 typically indicates meaningful semantic similarity for most
    /// embedding models.
    /// </remarks>
    /// <value>Default: 0.7 (medium relevance threshold).</value>
    public float MinScore { get; init; } = 0.7f;

    /// <summary>
    /// Optional filter to limit results to a specific document.
    /// </summary>
    /// <remarks>
    /// LOGIC: When set, the search query is scoped to chunks belonging to the
    /// specified document only. When <c>null</c>, the search spans all indexed
    /// documents in the corpus. This enables "search within document" functionality.
    /// </remarks>
    /// <value>Default: <c>null</c> (search all indexed documents).</value>
    public Guid? DocumentFilter { get; init; }

    /// <summary>
    /// Whether to expand abbreviations in the query text.
    /// </summary>
    /// <remarks>
    /// LOGIC: When enabled, the <c>IQueryPreprocessor</c> (v0.4.5c) expands known
    /// abbreviations before embedding. For example, "API" becomes
    /// "API (Application Programming Interface)". This can improve retrieval quality
    /// when the indexed corpus uses full terms rather than abbreviations.
    /// </remarks>
    /// <value>Default: <c>false</c> (abbreviations are not expanded).</value>
    public bool ExpandAbbreviations { get; init; } = false;

    /// <summary>
    /// Whether to use cached query embeddings.
    /// </summary>
    /// <remarks>
    /// LOGIC: When enabled, the <c>IQueryPreprocessor</c> (v0.4.5c) checks a
    /// short-lived cache (5-minute TTL) for previously computed query embeddings.
    /// This avoids redundant embedding API calls for repeated or similar queries.
    /// Set to <c>false</c> to force re-embedding (useful for testing or when
    /// the embedding model has changed).
    /// </remarks>
    /// <value>Default: <c>true</c> (caching enabled).</value>
    public bool UseCache { get; init; } = true;

    #region Deduplication Options (v0.5.9f)

    /// <summary>
    /// Whether to respect canonical records and filter out variant chunks.
    /// </summary>
    /// <remarks>
    /// <para>
    /// LOGIC: When enabled, the search query returns only canonical chunks (the authoritative
    /// version) and standalone chunks (those not part of any canonical grouping). Variant
    /// chunks that have been merged into a canonical record are excluded from results.
    /// </para>
    /// <para>
    /// This prevents duplicate content from appearing in search results when multiple
    /// semantically equivalent chunks have been deduplicated.
    /// </para>
    /// <para>
    /// <b>License gating:</b> Requires Writer Pro tier. When unlicensed, this property
    /// is ignored and search behaves as if <c>false</c> (pre-dedup behavior).
    /// </para>
    /// <para>
    /// <b>Introduced in:</b> v0.5.9f as part of the Retrieval Integration feature.
    /// </para>
    /// </remarks>
    /// <value>Default: <c>true</c> (filter out variants, return canonicals only).</value>
    public bool RespectCanonicals { get; init; } = true;

    /// <summary>
    /// Whether to include variant metadata in search results.
    /// </summary>
    /// <remarks>
    /// <para>
    /// LOGIC: When enabled, each search result includes the number of variants that have
    /// been merged into the canonical record. This enables UI display of "Merged from N sources"
    /// badges and helps users understand the authority of the result.
    /// </para>
    /// <para>
    /// Has no effect when <see cref="RespectCanonicals"/> is <c>false</c>.
    /// </para>
    /// <para>
    /// <b>Performance impact:</b> Adds a subquery for each result to count variants.
    /// For large result sets, consider disabling if variant counts are not needed.
    /// </para>
    /// <para>
    /// <b>Introduced in:</b> v0.5.9f as part of the Retrieval Integration feature.
    /// </para>
    /// </remarks>
    /// <value>Default: <c>false</c> (variant counts not loaded).</value>
    public bool IncludeVariantMetadata { get; init; } = false;

    /// <summary>
    /// Whether to include archived chunks in search results.
    /// </summary>
    /// <remarks>
    /// <para>
    /// LOGIC: Archived chunks are those that have been superseded or manually archived
    /// but not deleted. By default, these are excluded from search results to prevent
    /// outdated information from surfacing.
    /// </para>
    /// <para>
    /// Enable this option for audit, research, or administrative purposes where
    /// historical content is relevant.
    /// </para>
    /// <para>
    /// <b>Introduced in:</b> v0.5.9f as part of the Retrieval Integration feature.
    /// </para>
    /// </remarks>
    /// <value>Default: <c>false</c> (archived chunks excluded).</value>
    public bool IncludeArchived { get; init; } = false;

    /// <summary>
    /// Whether to include provenance information in search results.
    /// </summary>
    /// <remarks>
    /// <para>
    /// LOGIC: When enabled, each search result includes provenance records that trace
    /// the chunk back to its source document, ingestion timestamp, and any verification
    /// metadata. This enables UI display of "View Sources" functionality.
    /// </para>
    /// <para>
    /// Provenance is loaded for both the canonical chunk and any merged variants,
    /// providing a complete picture of all sources that contributed to the canonical.
    /// </para>
    /// <para>
    /// <b>Performance impact:</b> Adds a database query per result to load provenance.
    /// For large result sets, consider disabling if provenance is not needed.
    /// </para>
    /// <para>
    /// <b>Introduced in:</b> v0.5.9f as part of the Retrieval Integration feature.
    /// </para>
    /// </remarks>
    /// <value>Default: <c>false</c> (provenance not loaded).</value>
    public bool IncludeProvenance { get; init; } = false;

    #endregion
}
