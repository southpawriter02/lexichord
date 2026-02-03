// =============================================================================
// File: IQueryExpander.cs
// Project: Lexichord.Abstractions
// Description: Interface for query expansion in the Relevance Tuner feature.
// =============================================================================
// LOGIC: Defines the contract for expanding queries with synonyms and related
//   terms to improve search recall. Expansion sources include:
//   - Terminology database (user-defined synonyms)
//   - Algorithmic (stemming, morphological variants)
//   - Content-derived (learned from indexed documents)
// =============================================================================
// VERSION: v0.5.4b (Query Expansion)
// DEPENDENCIES:
//   - QueryAnalysis (v0.5.4a) for keywords to expand
//   - ITerminologyRepository (v0.2.2b) for synonym lookup
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Expands queries with synonyms and related terms.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IQueryExpander"/> takes a <see cref="QueryAnalysis"/> and enriches
/// it with synonyms from multiple sources, producing an <see cref="ExpandedQuery"/>
/// that can find documents using different terminology for the same concepts.
/// </para>
/// <para>
/// <b>Expansion Sources:</b>
/// <list type="table">
///   <listheader>
///     <term>Source</term>
///     <description>Description</description>
///   </listheader>
///   <item>
///     <term>Terminology Database</term>
///     <description>User/admin-defined synonyms (auth → authentication)</description>
///   </item>
///   <item>
///     <term>Algorithmic</term>
///     <description>Stemming/morphology (implementing → implement)</description>
///   </item>
///   <item>
///     <term>Content-Derived</term>
///     <description>Learned co-occurrences (future feature)</description>
///   </item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe as the expander
/// may be called concurrently from multiple search requests.
/// </para>
/// <para>
/// <b>License Gate:</b> Query expansion is gated at Writer Pro tier via
/// <c>FeatureFlags.RAG.RelevanceTuner</c>. Core tier receives
/// <see cref="ExpandedQuery.NoExpansion"/> passthrough.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.4b as part of The Relevance Tuner feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public async Task&lt;SearchResult&gt; SearchAsync(string query)
/// {
///     var analysis = _analyzer.Analyze(query);
///
///     // Expand query (license-gated)
///     var expanded = await _expander.ExpandAsync(analysis);
///
///     _logger.LogInfo("Query expanded: {Original} → {Total} terms",
///         analysis.KeywordCount, expanded.TotalTermCount);
///
///     // Search with expanded keywords
///     return await _searchService.SearchAsync(expanded.ExpandedKeywords);
/// }
/// </code>
/// </example>
public interface IQueryExpander
{
    /// <summary>
    /// Expands a query analysis with synonyms and related terms.
    /// </summary>
    /// <param name="analysis">
    /// The analyzed query to expand. MUST NOT be null.
    /// </param>
    /// <param name="options">
    /// Expansion configuration options. If null, uses <see cref="ExpansionOptions.Default"/>.
    /// </param>
    /// <param name="cancellationToken">
    /// Cancellation token for async operation.
    /// </param>
    /// <returns>
    /// Expanded query with original and additional terms.
    /// Returns <see cref="ExpandedQuery.NoExpansion"/> if:
    /// <list type="bullet">
    ///   <item><description>User lacks Writer Pro license</description></item>
    ///   <item><description>No synonyms found for any keywords</description></item>
    ///   <item><description>Analysis has no keywords</description></item>
    /// </list>
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="analysis"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>Processing Steps:</b>
    /// <list type="number">
    ///   <item><description>Validate input and check license</description></item>
    ///   <item><description>For each keyword, lookup synonyms in terminology database</description></item>
    ///   <item><description>Optionally generate algorithmic variants (stemming)</description></item>
    ///   <item><description>Filter synonyms by weight threshold</description></item>
    ///   <item><description>Limit synonyms per term</description></item>
    ///   <item><description>Combine into expanded keyword list</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Caching:</b> Frequently expanded terms are cached for performance.
    /// Cache is invalidated when terminology database changes.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Expand with default options
    /// var expanded = await expander.ExpandAsync(analysis);
    ///
    /// // Expand with custom options (more aggressive)
    /// var options = new ExpansionOptions(MaxSynonymsPerTerm: 5, MinSynonymWeight: 0.2f);
    /// var expanded = await expander.ExpandAsync(analysis, options);
    /// </code>
    /// </example>
    Task<ExpandedQuery> ExpandAsync(
        QueryAnalysis analysis,
        ExpansionOptions? options = null,
        CancellationToken cancellationToken = default);
}
