// =============================================================================
// File: FilterChipType.cs
// Project: Lexichord.Modules.RAG
// Description: Enum defining the types of filter chips displayed in the search filter panel.
// =============================================================================
// LOGIC: Categorizes filter chips for proper removal handling. When a user
//        clicks the dismiss button on a chip, the type determines which
//        filter criterion to deselect (folder, extension, date range, or tag).
// =============================================================================
// VERSION: v0.5.5b (Filter UI Component)
// =============================================================================

namespace Lexichord.Modules.RAG.Enums;

/// <summary>
/// Defines the types of filter chips that can be displayed in the search filter panel.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="FilterChipType"/> categorizes active filter chips for the search filter panel.
/// Each chip represents an active filter criterion that the user can remove by clicking
/// the dismiss button. The type determines which filter property to modify when removing.
/// </para>
/// <para>
/// <b>Chip Types:</b>
/// <list type="table">
///   <listheader>
///     <term>Type</term>
///     <description>SearchFilter Property</description>
///   </listheader>
///   <item>
///     <term><see cref="Path"/></term>
///     <description><c>SearchFilter.PathPatterns</c></description>
///   </item>
///   <item>
///     <term><see cref="Extension"/></term>
///     <description><c>SearchFilter.FileExtensions</c></description>
///   </item>
///   <item>
///     <term><see cref="DateRange"/></term>
///     <description><c>SearchFilter.ModifiedRange</c></description>
///   </item>
///   <item>
///     <term><see cref="Tag"/></term>
///     <description><c>SearchFilter.Tags</c> (reserved for future use)</description>
///   </item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.5b as part of The Filter System feature.
/// </para>
/// </remarks>
public enum FilterChipType
{
    /// <summary>
    /// Chip representing a selected folder path filter.
    /// </summary>
    /// <remarks>
    /// Display format: "📁 folder-name"
    /// Removing this chip deselects the corresponding folder in the tree.
    /// </remarks>
    Path,

    /// <summary>
    /// Chip representing a selected file extension filter.
    /// </summary>
    /// <remarks>
    /// Display format: ".ext" (e.g., ".md", ".txt")
    /// Removing this chip deselects the corresponding extension toggle.
    /// </remarks>
    Extension,

    /// <summary>
    /// Chip representing an active date range filter.
    /// </summary>
    /// <remarks>
    /// Display format: "📅 Last 7 days" or "📅 Custom range"
    /// Removing this chip resets the date range to <see cref="DateRangeOption.AnyTime"/>.
    /// </remarks>
    DateRange,

    /// <summary>
    /// Chip representing a selected document tag filter.
    /// </summary>
    /// <remarks>
    /// Reserved for future use when document tagging is implemented.
    /// Display format: "🏷️ tag-name"
    /// </remarks>
    Tag
}
