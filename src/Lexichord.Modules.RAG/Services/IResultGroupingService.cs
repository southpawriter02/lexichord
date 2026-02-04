// =============================================================================
// File: IResultGroupingService.cs
// Project: Lexichord.Modules.RAG
// Description: Interface for grouping search results by source document.
// =============================================================================
// LOGIC: Defines the contract for result grouping functionality.
//   - GroupByDocument: Takes SearchResult and options, returns GroupedSearchResults.
//   - Stateless service pattern — no side effects, pure transformation.
// =============================================================================
// DEPENDENCIES (referenced by version):
//   - v0.4.5a: SearchResult, SearchHit (input data).
//   - v0.5.7b: GroupedSearchResults, ResultGroupingOptions (this version).
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.RAG.Models;

namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// Service for grouping search results by their source document.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IResultGroupingService"/> transforms a flat list of <see cref="SearchHit"/>
/// instances into a hierarchical structure organized by document. This supports the
/// collapsible grouped results UI in the Reference Panel.
/// </para>
/// <para>
/// <b>Grouping Logic:</b>
/// <list type="number">
///   <item><description>Group hits by <see cref="SearchHit.Document"/>.<c>FilePath</c>.</description></item>
///   <item><description>Calculate group metadata (match count, max score).</description></item>
///   <item><description>Limit hits per group to <see cref="ResultGroupingOptions.MaxHitsPerGroup"/>.</description></item>
///   <item><description>Sort groups according to <see cref="ResultGroupingOptions.SortMode"/>.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe for use as singletons.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.7b as part of the Result Grouping feature.
/// </para>
/// </remarks>
public interface IResultGroupingService
{
    /// <summary>
    /// Groups search results by their source document.
    /// </summary>
    /// <param name="result">
    /// The search result containing hits to group. Must not be null.
    /// </param>
    /// <param name="options">
    /// Grouping options controlling sort mode, hit limits, and expansion state.
    /// If null, <see cref="ResultGroupingOptions.Default"/> is used.
    /// </param>
    /// <returns>
    /// A <see cref="GroupedSearchResults"/> containing document groups with their hits.
    /// Returns <see cref="GroupedSearchResults.Empty"/> if the input has no hits.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="result"/> is null.
    /// </exception>
    /// <remarks>
    /// LOGIC: Pure transformation with no side effects. Suitable for repeated calls
    /// with different options (e.g., re-sorting without re-executing the search).
    /// </remarks>
    GroupedSearchResults GroupByDocument(SearchResult result, ResultGroupingOptions? options = null);
}
