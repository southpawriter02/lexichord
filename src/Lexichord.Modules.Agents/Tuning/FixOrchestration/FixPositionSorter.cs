// =============================================================================
// File: FixPositionSorter.cs
// Project: Lexichord.Modules.Agents
// Description: Sorts fixes by text position (bottom-to-top) to prevent offset drift.
// =============================================================================
// LOGIC: When applying multiple text modifications to a document, changes at later
//   positions must be applied first to prevent earlier changes from invalidating
//   the offsets of later changes. This class sorts issues by position descending
//   (highest Start first) to ensure correct offset tracking.
//
// v0.7.5h: Combined Fix Workflow (Unified Validation Feature)
// =============================================================================

using Lexichord.Abstractions.Contracts.Validation;

namespace Lexichord.Modules.Agents.Tuning.FixOrchestration;

/// <summary>
/// Sorts fixes by text position (bottom-to-top) to prevent offset drift.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> When applying multiple text modifications to a document, changes at
/// later positions in the text must be applied first. If a fix at position 10 adds
/// 5 characters, then a fix at position 20 would shift to position 25. By applying
/// the fix at position 20 first, we avoid this offset drift problem.
/// </para>
/// <para>
/// <b>Sort Order:</b>
/// <list type="number">
///   <item><description>Primary: <see cref="Editor.TextSpan.Start"/> descending (highest first)</description></item>
///   <item><description>Secondary: <see cref="Editor.TextSpan.End"/> descending (longest span first)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Filtering:</b> Only includes issues where:
/// <list type="bullet">
///   <item><description><see cref="UnifiedIssue.BestFix"/> is not null</description></item>
///   <item><description><see cref="UnifiedFix.CanAutoApply"/> is true</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> All methods are static and thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5h as part of the Combined Fix Workflow feature.
/// </para>
/// </remarks>
internal static class FixPositionSorter
{
    /// <summary>
    /// Sorts issues by position descending (highest position first) and filters
    /// to only include auto-fixable issues.
    /// </summary>
    /// <param name="issues">The issues to sort and filter.</param>
    /// <returns>
    /// A read-only list of issues sorted bottom-to-top, containing only issues
    /// with a non-null <see cref="UnifiedIssue.BestFix"/> that has
    /// <see cref="UnifiedFix.CanAutoApply"/> set to true.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="issues"/> is null.
    /// </exception>
    /// <remarks>
    /// <b>LOGIC:</b> The sort ensures that when fixes are applied in order,
    /// each fix's <see cref="Editor.TextSpan"/> remains valid because no
    /// earlier modifications have shifted text positions.
    /// Performance: O(n log n) for n issues.
    /// </remarks>
    public static IReadOnlyList<UnifiedIssue> SortBottomToTop(
        IEnumerable<UnifiedIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);

        return issues
            // LOGIC: Filter to only auto-fixable issues with valid fixes.
            .Where(i => i.BestFix is not null && i.BestFix.CanAutoApply)
            // LOGIC: Sort by Start descending (bottom of document first).
            .OrderByDescending(i => i.Location.Start)
            // LOGIC: For equal Start positions, sort by End descending (longer spans first).
            .ThenByDescending(i => i.Location.End)
            .ToList();
    }

    /// <summary>
    /// Sorts issues by position descending without filtering.
    /// </summary>
    /// <param name="issues">The issues to sort.</param>
    /// <returns>
    /// A read-only list of all provided issues sorted bottom-to-top.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="issues"/> is null.
    /// </exception>
    /// <remarks>
    /// <b>LOGIC:</b> Used when the caller has already filtered issues and
    /// only needs the position sorting. For example, after filtering by
    /// category or severity.
    /// </remarks>
    public static IReadOnlyList<UnifiedIssue> SortBottomToTopUnfiltered(
        IEnumerable<UnifiedIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);

        return issues
            .OrderByDescending(i => i.Location.Start)
            .ThenByDescending(i => i.Location.End)
            .ToList();
    }
}
