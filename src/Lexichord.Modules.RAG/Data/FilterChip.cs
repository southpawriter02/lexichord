// =============================================================================
// File: FilterChip.cs
// Project: Lexichord.Modules.RAG
// Description: Immutable record representing an active filter chip in the UI.
// =============================================================================
// LOGIC: FilterChip is an immutable record that represents a single active
//        filter displayed as a dismissible chip below the search bar.
//        Factory methods ensure type-safe creation with proper labels.
// =============================================================================
// VERSION: v0.5.7a (Panel Redesign)
// DEPENDENCIES:
//   - v0.5.5b: FilterChipType enum
//   - v0.5.5b: DateRangeOption enum
// =============================================================================

using Lexichord.Modules.RAG.Enums;

namespace Lexichord.Modules.RAG.Data;

/// <summary>
/// Immutable record representing an active filter chip in the Reference Panel.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="FilterChip"/> represents a single active filter criterion displayed
/// as a dismissible chip in the Reference Panel. Each chip shows a human-readable
/// label and can be removed by the user to deactivate that filter.
/// </para>
/// <para>
/// <b>Factory Methods:</b>
/// <list type="bullet">
///   <item><description><see cref="ForPath"/>: Creates a path pattern chip (e.g., "docs/**").</description></item>
///   <item><description><see cref="ForExtension"/>: Creates a file extension chip (e.g., ".md").</description></item>
///   <item><description><see cref="ForDateRange"/>: Creates a date range chip (e.g., "Last 7 days").</description></item>
///   <item><description><see cref="ForTag"/>: Creates a tag chip (e.g., "important").</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.7a as part of the Panel Redesign feature.
/// </para>
/// </remarks>
/// <param name="Label">
/// Human-readable label displayed on the chip.
/// </param>
/// <param name="Type">
/// The filter type, used for removal logic.
/// </param>
/// <param name="Value">
/// The underlying filter value. Type varies by <see cref="Type"/>:
/// <list type="bullet">
///   <item><description><see cref="FilterChipType.Path"/>: <c>string</c> path pattern.</description></item>
///   <item><description><see cref="FilterChipType.Extension"/>: <c>string</c> extension (without dot).</description></item>
///   <item><description><see cref="FilterChipType.DateRange"/>: <see cref="DateRangeOption"/> value.</description></item>
///   <item><description><see cref="FilterChipType.Tag"/>: <c>string</c> tag name.</description></item>
/// </list>
/// </param>
/// <example>
/// <code>
/// // Create chips using factory methods
/// var pathChip = FilterChip.ForPath("docs/**");
/// var extChip = FilterChip.ForExtension("md");
/// var dateChip = FilterChip.ForDateRange(DateRangeOption.Last7Days);
/// var tagChip = FilterChip.ForTag("important");
/// </code>
/// </example>
public record FilterChip(string Label, FilterChipType Type, object Value)
{
    // =========================================================================
    // Factory Methods
    // =========================================================================

    /// <summary>
    /// Creates a filter chip for a path pattern.
    /// </summary>
    /// <param name="pathPattern">
    /// The path pattern to filter by (e.g., "docs/**", "src/Models/*").
    /// </param>
    /// <returns>
    /// A new <see cref="FilterChip"/> with <see cref="FilterChipType.Path"/> type.
    /// </returns>
    /// <remarks>
    /// LOGIC: The label displays the path pattern as-is. Glob patterns
    /// are supported (e.g., "**" for recursive, "*" for wildcard).
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="pathPattern"/> is null or whitespace.
    /// </exception>
    public static FilterChip ForPath(string pathPattern)
    {
        if (string.IsNullOrWhiteSpace(pathPattern))
            throw new ArgumentException("Path pattern cannot be null or whitespace.", nameof(pathPattern));

        return new FilterChip(
            Label: pathPattern,
            Type: FilterChipType.Path,
            Value: pathPattern);
    }

    /// <summary>
    /// Creates a filter chip for a file extension.
    /// </summary>
    /// <param name="extension">
    /// The file extension without the leading dot (e.g., "md", "txt", "cs").
    /// </param>
    /// <returns>
    /// A new <see cref="FilterChip"/> with <see cref="FilterChipType.Extension"/> type.
    /// </returns>
    /// <remarks>
    /// LOGIC: The label prefixes the extension with a dot for display
    /// (e.g., "md" becomes ".md"). The Value stores the raw extension.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="extension"/> is null or whitespace.
    /// </exception>
    public static FilterChip ForExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            throw new ArgumentException("Extension cannot be null or whitespace.", nameof(extension));

        // LOGIC: Strip leading dot if provided, normalize for storage.
        var normalized = extension.TrimStart('.');

        return new FilterChip(
            Label: $".{normalized}",
            Type: FilterChipType.Extension,
            Value: normalized);
    }

    /// <summary>
    /// Creates a filter chip for a date range.
    /// </summary>
    /// <param name="range">
    /// The date range option to filter by.
    /// </param>
    /// <returns>
    /// A new <see cref="FilterChip"/> with <see cref="FilterChipType.DateRange"/> type.
    /// </returns>
    /// <remarks>
    /// LOGIC: The label is derived from the enum value using a human-readable
    /// format (e.g., "Last7Days" becomes "Last 7 days"). The Value stores
    /// the original <see cref="DateRangeOption"/> enum.
    /// </remarks>
    public static FilterChip ForDateRange(DateRangeOption range)
    {
        // LOGIC: Convert enum to human-readable label.
        var label = range switch
        {
            DateRangeOption.AnyTime => "Any time",
            DateRangeOption.LastDay => "Last day",
            DateRangeOption.Last7Days => "Last 7 days",
            DateRangeOption.Last30Days => "Last 30 days",
            DateRangeOption.Custom => "Custom range",
            _ => range.ToString()
        };

        return new FilterChip(
            Label: label,
            Type: FilterChipType.DateRange,
            Value: range);
    }

    /// <summary>
    /// Creates a filter chip for a document tag.
    /// </summary>
    /// <param name="tag">
    /// The tag name to filter by.
    /// </param>
    /// <returns>
    /// A new <see cref="FilterChip"/> with <see cref="FilterChipType.Tag"/> type.
    /// </returns>
    /// <remarks>
    /// LOGIC: Reserved for future use when document tagging is implemented.
    /// The label displays the tag name as-is.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="tag"/> is null or whitespace.
    /// </exception>
    public static FilterChip ForTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Tag cannot be null or whitespace.", nameof(tag));

        return new FilterChip(
            Label: tag,
            Type: FilterChipType.Tag,
            Value: tag);
    }
}
