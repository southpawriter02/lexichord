// =============================================================================
// File: FilterChipViewModel.cs
// Project: Lexichord.Modules.RAG
// Description: Immutable record for representing an active filter chip in the UI.
// =============================================================================
// LOGIC: FilterChipViewModel is an immutable record that represents a single
//        active filter chip displayed in the search filter panel. Each chip
//        shows the filter criterion and can be dismissed to remove that filter.
//        The Type property determines which filter property to modify on removal.
// =============================================================================
// VERSION: v0.5.5b (Filter UI Component)
// =============================================================================

using Lexichord.Modules.RAG.Enums;

namespace Lexichord.Modules.RAG.ViewModels;

/// <summary>
/// Immutable record for an active filter chip display.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="FilterChipViewModel"/> represents a single active filter criterion
/// displayed as a dismissible chip in the search filter panel. When the user
/// clicks the dismiss button, the chip is removed and the corresponding filter
/// criterion is deselected.
/// </para>
/// <para>
/// <b>Chip Display:</b>
/// <list type="bullet">
///   <item><description>Path chips: "📁 folder-name"</description></item>
///   <item><description>Extension chips: ".md", ".txt"</description></item>
///   <item><description>Date range chips: "📅 Last 7 days"</description></item>
///   <item><description>Tag chips: "🏷️ tag-name" (future)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.5b as part of The Filter System feature.
/// </para>
/// </remarks>
/// <param name="Type">
/// The type of filter this chip represents. Used to determine which filter
/// property to modify when the chip is removed.
/// </param>
/// <param name="DisplayText">
/// The text displayed on the chip. Should include an icon prefix for visual
/// identification (e.g., "📁 docs/", ".md", "📅 Last 7 days").
/// </param>
/// <param name="Value">
/// The underlying value for removal logic. For path chips, this is the full
/// path pattern. For extension chips, this is the extension without dot.
/// For date range chips, this is the DateRangeOption name.
/// </param>
/// <example>
/// <code>
/// // Path filter chip
/// var pathChip = new FilterChipViewModel(
///     FilterChipType.Path,
///     "📁 docs/",
///     "docs/**");
///
/// // Extension filter chip
/// var extChip = new FilterChipViewModel(
///     FilterChipType.Extension,
///     ".md",
///     "md");
///
/// // Date range filter chip
/// var dateChip = new FilterChipViewModel(
///     FilterChipType.DateRange,
///     "📅 Last 7 days",
///     "Last7Days");
/// </code>
/// </example>
public record FilterChipViewModel(
    FilterChipType Type,
    string DisplayText,
    string Value);
