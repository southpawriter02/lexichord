// =============================================================================
// File: ResultSortMode.cs
// Project: Lexichord.Modules.RAG
// Description: Enum defining sort modes for grouped search results.
// =============================================================================
// LOGIC: Defines how document groups are ordered in GroupedSearchResults.
//   - ByRelevance: Groups ordered by max score descending (default).
//   - ByDocumentPath: Groups ordered alphabetically by document path.
//   - ByMatchCount: Groups ordered by hit count descending.
// =============================================================================
// DEPENDENCIES (referenced by version):
//   - v0.5.7b: Introduced as part of Result Grouping feature.
// =============================================================================

namespace Lexichord.Modules.RAG.Models;

/// <summary>
/// Defines the sort order for grouped search result document groups.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ResultSortMode"/> controls how <see cref="DocumentResultGroup"/> instances
/// are ordered within <see cref="GroupedSearchResults"/>. The sort mode affects only
/// the order of groups; hits within each group are always ordered by descending score.
/// </para>
/// <para>
/// <b>UI Binding:</b> This enum is exposed to the UI via <see cref="GroupedResultsViewModel"/>
/// for the sort mode dropdown. Use <c>Enum.GetValues&lt;ResultSortMode&gt;()</c> to
/// populate the dropdown options.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.7b as part of the Result Grouping feature.
/// </para>
/// </remarks>
public enum ResultSortMode
{
    /// <summary>
    /// Groups ordered by maximum hit score within each group (descending).
    /// </summary>
    /// <remarks>
    /// LOGIC: Default sort mode. Groups containing the highest-scoring hit appear first.
    /// This prioritizes documents with the most relevant individual matches.
    /// </remarks>
    ByRelevance = 0,

    /// <summary>
    /// Groups ordered alphabetically by document file path (ascending).
    /// </summary>
    /// <remarks>
    /// LOGIC: Provides a predictable, stable ordering based on file system location.
    /// Useful when users want to scan results by document organization.
    /// </remarks>
    ByDocumentPath = 1,

    /// <summary>
    /// Groups ordered by number of hits within each group (descending).
    /// </summary>
    /// <remarks>
    /// LOGIC: Prioritizes documents with the most matches, regardless of individual
    /// match quality. Useful for finding documents with dense topic coverage.
    /// </remarks>
    ByMatchCount = 2
}
