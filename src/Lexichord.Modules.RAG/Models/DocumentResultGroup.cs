// =============================================================================
// File: DocumentResultGroup.cs
// Project: Lexichord.Modules.RAG
// Description: Record representing a single document's grouped search results.
// =============================================================================
// LOGIC: Immutable record containing grouped hits from a single document.
//   - DocumentPath: Full path to the source document.
//   - DocumentTitle: Display title (file name without extension by default).
//   - MatchCount: Total number of hits in this document (may exceed Hits.Count).
//   - MaxScore: Highest score among all hits in this document.
//   - Hits: Limited list of SearchHit instances (up to MaxHitsPerGroup).
//   - IsExpanded: Current UI expansion state.
//   - Computed properties for FileName, HasMoreHits, HiddenHitCount.
// =============================================================================
// DEPENDENCIES (referenced by version):
//   - v0.4.5a: SearchHit (hit data).
// =============================================================================

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Modules.RAG.Models;

/// <summary>
/// Represents a group of search results from a single source document.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="DocumentResultGroup"/> aggregates all <see cref="SearchHit"/> instances
/// from a single document, providing summary metadata (match count, max score) and
/// a limited subset of hits for display. This supports the collapsible group UI
/// in the Reference Panel.
/// </para>
/// <para>
/// <b>Hit Limiting:</b> The <see cref="Hits"/> list is capped at
/// <see cref="ResultGroupingOptions.MaxHitsPerGroup"/>. When more hits exist,
/// <see cref="HasMoreHits"/> returns <c>true</c> and <see cref="HiddenHitCount"/>
/// indicates how many additional hits are hidden.
/// </para>
/// <para>
/// <b>Expansion State:</b> The <see cref="IsExpanded"/> property tracks the current
/// UI state. This is a transient property managed by <see cref="GroupedResultsViewModel"/>
/// and is not persisted across sessions.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.7b as part of the Result Grouping feature.
/// </para>
/// </remarks>
/// <param name="DocumentPath">
/// Full file system path to the source document. Used for navigation and display.
/// </param>
/// <param name="DocumentTitle">
/// Display title for the document, typically the file name without extension.
/// </param>
/// <param name="MatchCount">
/// Total number of hits from this document, including those not shown in <see cref="Hits"/>.
/// </param>
/// <param name="MaxScore">
/// Highest similarity score among all hits from this document. Used for relevance sorting.
/// </param>
/// <param name="Hits">
/// Limited list of <see cref="SearchHit"/> instances from this document,
/// ordered by descending score. Count is capped at <see cref="ResultGroupingOptions.MaxHitsPerGroup"/>.
/// </param>
/// <param name="IsExpanded">
/// Current expansion state for the group in the UI. When <c>true</c>, hits are visible.
/// </param>
public record DocumentResultGroup(
    string DocumentPath,
    string DocumentTitle,
    int MatchCount,
    float MaxScore,
    IReadOnlyList<SearchHit> Hits,
    bool IsExpanded)
{
    /// <summary>
    /// Gets the file name portion of the document path.
    /// </summary>
    /// <value>
    /// The file name including extension, e.g., "document.md".
    /// </value>
    /// <remarks>
    /// LOGIC: Extracts the file name from <see cref="DocumentPath"/> for compact display
    /// in contexts where the full path is too long.
    /// </remarks>
    public string FileName => Path.GetFileName(DocumentPath);

    /// <summary>
    /// Gets whether additional hits exist beyond those in <see cref="Hits"/>.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="MatchCount"/> exceeds <see cref="Hits"/> count;
    /// otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// LOGIC: When <c>true</c>, the UI should display a "X more" indicator.
    /// </remarks>
    public bool HasMoreHits => MatchCount > Hits.Count;

    /// <summary>
    /// Gets the number of hits not included in <see cref="Hits"/>.
    /// </summary>
    /// <value>
    /// The difference between <see cref="MatchCount"/> and <see cref="Hits"/> count.
    /// Zero if all hits are shown.
    /// </value>
    /// <remarks>
    /// LOGIC: Used by the UI to display the hidden hit count, e.g., "+3 more".
    /// </remarks>
    public int HiddenHitCount => MatchCount - Hits.Count;
}
