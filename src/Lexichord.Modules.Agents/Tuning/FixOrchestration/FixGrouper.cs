// =============================================================================
// File: FixGrouper.cs
// Project: Lexichord.Modules.Agents
// Description: Groups fixes by category for ordered application.
// =============================================================================
// LOGIC: Groups issues by IssueCategory and returns them in the application
//   order: Knowledge first, Grammar second, Style last. Within each group,
//   maintains the existing sort order (typically bottom-to-top).
//
// v0.7.5h: Combined Fix Workflow (Unified Validation Feature)
// =============================================================================

using Lexichord.Abstractions.Contracts.Validation;

namespace Lexichord.Modules.Agents.Tuning.FixOrchestration;

/// <summary>
/// Groups fixes by category for ordered application.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Category ordering ensures that fixes are applied in the correct
/// dependency order:
/// <list type="number">
///   <item><description><see cref="IssueCategory.Knowledge"/>: Applied first — factual corrections
///     that may change content meaning</description></item>
///   <item><description><see cref="IssueCategory.Structure"/>: Applied second — structural changes
///     that may affect text layout</description></item>
///   <item><description><see cref="IssueCategory.Grammar"/>: Applied third — grammar/spelling
///     corrections that depend on stable content</description></item>
///   <item><description><see cref="IssueCategory.Style"/>: Applied last — style adjustments
///     that depend on stable grammar</description></item>
///   <item><description><see cref="IssueCategory.Custom"/>: Applied last — user-defined rules</description></item>
/// </list>
/// </para>
/// <para>
/// Within each category group, issues maintain their existing sort order
/// (typically bottom-to-top from <see cref="FixPositionSorter"/>).
/// </para>
/// <para>
/// <b>Thread Safety:</b> All methods are static and thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5h as part of the Combined Fix Workflow feature.
/// </para>
/// </remarks>
internal static class FixGrouper
{
    /// <summary>
    /// Category application order. Lower values are applied first.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Knowledge fixes are applied first because they correct factual
    /// content. Grammar fixes depend on stable content. Style fixes depend on stable
    /// grammar. This ordering minimizes cascading invalidations.
    /// </remarks>
    private static readonly IReadOnlyDictionary<IssueCategory, int> CategoryOrder =
        new Dictionary<IssueCategory, int>
        {
            [IssueCategory.Knowledge] = 0,
            [IssueCategory.Structure] = 1,
            [IssueCategory.Grammar] = 2,
            [IssueCategory.Style] = 3,
            [IssueCategory.Custom] = 4
        };

    /// <summary>
    /// Groups issues by category in the defined application order.
    /// </summary>
    /// <param name="issues">The issues to group (should already be position-sorted).</param>
    /// <returns>
    /// A list of category groups ordered for application. Each group contains
    /// issues of the same category, maintaining their original sort order.
    /// Empty categories are excluded.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="issues"/> is null.
    /// </exception>
    /// <remarks>
    /// <b>LOGIC:</b> The returned list should be iterated in order. For each group,
    /// apply fixes in order (typically bottom-to-top within each category).
    /// </remarks>
    public static IReadOnlyList<CategoryGroup> GroupByCategory(
        IReadOnlyList<UnifiedIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);

        return issues
            .GroupBy(i => i.Category)
            .OrderBy(g => GetCategoryOrder(g.Key))
            .Select(g => new CategoryGroup(g.Key, g.ToList()))
            .ToList();
    }

    /// <summary>
    /// Flattens category groups back into a single ordered list.
    /// </summary>
    /// <param name="groups">The category groups to flatten.</param>
    /// <returns>
    /// A flat list of issues in category application order, with bottom-to-top
    /// ordering within each category.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="groups"/> is null.
    /// </exception>
    /// <remarks>
    /// <b>LOGIC:</b> Used when the caller needs a single ordered list for
    /// sequential application rather than iterating category groups.
    /// </remarks>
    public static IReadOnlyList<UnifiedIssue> FlattenGroups(
        IReadOnlyList<CategoryGroup> groups)
    {
        ArgumentNullException.ThrowIfNull(groups);

        return groups
            .SelectMany(g => g.Issues)
            .ToList();
    }

    /// <summary>
    /// Gets the application order for a category.
    /// </summary>
    /// <param name="category">The issue category.</param>
    /// <returns>Numeric order (lower = applied first).</returns>
    private static int GetCategoryOrder(IssueCategory category) =>
        CategoryOrder.TryGetValue(category, out var order) ? order : int.MaxValue;
}

/// <summary>
/// Represents a group of issues in the same category.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Used by <see cref="FixGrouper.GroupByCategory"/> to return
/// issues organized by category. The orchestrator iterates groups in order
/// and applies fixes within each group.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This record is immutable and thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5h as part of the Combined Fix Workflow feature.
/// </para>
/// </remarks>
/// <param name="Category">The issue category for this group.</param>
/// <param name="Issues">The issues in this group, in application order.</param>
internal record CategoryGroup(
    IssueCategory Category,
    IReadOnlyList<UnifiedIssue> Issues)
{
    /// <summary>
    /// Gets the number of issues in this group.
    /// </summary>
    public int Count => Issues.Count;
}
