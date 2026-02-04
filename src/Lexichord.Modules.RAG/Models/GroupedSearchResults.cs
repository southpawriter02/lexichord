// =============================================================================
// File: GroupedSearchResults.cs
// Project: Lexichord.Modules.RAG
// Description: Container record for document-grouped search results.
// =============================================================================
// LOGIC: Immutable record containing grouped results with summary metadata.
//   - Groups: List of DocumentResultGroup instances.
//   - TotalHits: Sum of all match counts across groups.
//   - TotalDocuments: Number of groups (distinct documents).
//   - Query: Original search query for display.
//   - SearchDuration: Time taken for the search operation.
//   - AllHits: Computed iterator for flat access to all hits in relevance order.
// =============================================================================
// DEPENDENCIES (referenced by version):
//   - v0.4.5a: SearchHit (hit data).
//   - v0.5.7b: DocumentResultGroup (this version).
// =============================================================================

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Modules.RAG.Models;

/// <summary>
/// Container for search results grouped by source document.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="GroupedSearchResults"/> is the output of <see cref="IResultGroupingService.GroupByDocument"/>,
/// providing a hierarchical view of search results organized by document. Each document
/// is represented by a <see cref="DocumentResultGroup"/> containing its hits and metadata.
/// </para>
/// <para>
/// <b>Flat Access:</b> Use <see cref="AllHits"/> to iterate through all hits in
/// relevance order (descending score), flattening the group structure.
/// </para>
/// <para>
/// <b>Summary Metadata:</b> <see cref="TotalHits"/> and <see cref="TotalDocuments"/>
/// provide aggregate statistics for status bar display.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.7b as part of the Result Grouping feature.
/// </para>
/// </remarks>
/// <param name="Groups">
/// Ordered list of document groups. Order depends on <see cref="ResultGroupingOptions.SortMode"/>.
/// </param>
/// <param name="TotalHits">
/// Total number of hits across all documents before grouping.
/// </param>
/// <param name="TotalDocuments">
/// Number of distinct source documents containing hits.
/// </param>
/// <param name="Query">
/// Original search query text. Preserved for display and telemetry.
/// </param>
/// <param name="SearchDuration">
/// Duration of the underlying search operation. Preserved from <see cref="SearchResult.Duration"/>.
/// </param>
public record GroupedSearchResults(
    IReadOnlyList<DocumentResultGroup> Groups,
    int TotalHits,
    int TotalDocuments,
    string? Query = null,
    TimeSpan SearchDuration = default)
{
    /// <summary>
    /// Iterates through all hits from all groups in relevance order.
    /// </summary>
    /// <value>
    /// An enumerable of <see cref="SearchHit"/> instances ordered by descending score.
    /// </value>
    /// <remarks>
    /// LOGIC: Flattens the group structure for scenarios where the hierarchical
    /// view is not needed. Hits are re-sorted by score to ensure consistent
    /// relevance ordering regardless of group sort mode.
    /// </remarks>
    public IEnumerable<SearchHit> AllHits =>
        Groups.SelectMany(g => g.Hits).OrderByDescending(h => h.Score);

    /// <summary>
    /// Gets whether any results were found.
    /// </summary>
    /// <value>
    /// <c>true</c> if at least one group exists; otherwise, <c>false</c>.
    /// </value>
    public bool HasResults => Groups.Count > 0;

    /// <summary>
    /// Creates an empty grouped result set with no groups.
    /// </summary>
    /// <param name="query">Optional query text to preserve.</param>
    /// <returns>
    /// A <see cref="GroupedSearchResults"/> with an empty <see cref="Groups"/> list.
    /// </returns>
    /// <remarks>
    /// LOGIC: Factory method for early-return scenarios (empty query, no results).
    /// Avoids null instances in the API.
    /// </remarks>
    public static GroupedSearchResults Empty(string? query = null) => new(
        Groups: Array.Empty<DocumentResultGroup>(),
        TotalHits: 0,
        TotalDocuments: 0,
        Query: query);
}
