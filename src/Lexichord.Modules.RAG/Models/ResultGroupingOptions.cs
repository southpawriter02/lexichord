// =============================================================================
// File: ResultGroupingOptions.cs
// Project: Lexichord.Modules.RAG
// Description: Options record for configuring search result grouping behavior.
// =============================================================================
// LOGIC: Immutable record containing grouping configuration.
//   - SortMode: How groups are ordered (default: ByRelevance).
//   - MaxHitsPerGroup: Limit for displayed hits per document (default: 10).
//   - CollapseByDefault: Initial expansion state for groups (default: false).
// =============================================================================
// DEPENDENCIES (referenced by version):
//   - v0.5.7b: ResultSortMode enum (this version).
// =============================================================================

namespace Lexichord.Modules.RAG.Models;

/// <summary>
/// Configuration options for grouping search results by document.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ResultGroupingOptions"/> is an immutable record that configures how
/// <see cref="IResultGroupingService"/> groups and orders search results. All properties
/// have sensible defaults suitable for typical use cases.
/// </para>
/// <para>
/// <b>Default Values:</b>
/// <list type="bullet">
///   <item><description><see cref="SortMode"/>: <see cref="ResultSortMode.ByRelevance"/></description></item>
///   <item><description><see cref="MaxHitsPerGroup"/>: 10</description></item>
///   <item><description><see cref="CollapseByDefault"/>: <c>false</c></description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.7b as part of the Result Grouping feature.
/// </para>
/// </remarks>
/// <param name="SortMode">
/// The sort order for document groups. Default: <see cref="ResultSortMode.ByRelevance"/>.
/// </param>
/// <param name="MaxHitsPerGroup">
/// Maximum number of hits to include in each group. Additional hits are indicated
/// via <see cref="DocumentResultGroup.HasMoreHits"/>. Default: 10.
/// </param>
/// <param name="CollapseByDefault">
/// Initial expansion state for groups. When <c>true</c>, groups start collapsed.
/// Default: <c>false</c> (groups start expanded).
/// </param>
public record ResultGroupingOptions(
    ResultSortMode SortMode = ResultSortMode.ByRelevance,
    int MaxHitsPerGroup = 10,
    bool CollapseByDefault = false)
{
    /// <summary>
    /// Default options instance with standard values.
    /// </summary>
    /// <remarks>
    /// LOGIC: Provides a singleton default instance for common use cases.
    /// SortMode=ByRelevance, MaxHitsPerGroup=10, CollapseByDefault=false.
    /// </remarks>
    public static ResultGroupingOptions Default { get; } = new();
}
