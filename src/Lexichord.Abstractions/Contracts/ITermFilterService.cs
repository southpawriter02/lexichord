using Lexichord.Abstractions.Entities;

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Service contract for filtering style terminology.
/// </summary>
/// <remarks>
/// LOGIC: Provides pure filtering logic for StyleTerm collections.
/// Implemented by modules to support grid filtering in the Lexicon view.
///
/// Design decisions:
/// - Stateless service - all state passed via FilterCriteria
/// - Multiple filter conditions applied as AND logic
/// - Case-insensitive matching for text and category/severity filters
///
/// Version: v0.2.5b
/// </remarks>
public interface ITermFilterService
{
    /// <summary>
    /// Filters a collection of style terms based on the specified criteria.
    /// </summary>
    /// <param name="terms">The terms to filter.</param>
    /// <param name="criteria">The filter criteria to apply.</param>
    /// <returns>Terms matching all specified criteria.</returns>
    /// <remarks>
    /// LOGIC: All filter conditions are applied as AND logic.
    /// Empty or null criteria values are treated as "no filter" for that field.
    /// </remarks>
    IEnumerable<StyleTerm> Filter(IEnumerable<StyleTerm> terms, FilterCriteria criteria);
}

/// <summary>
/// Encapsulates all filter parameters for term filtering.
/// </summary>
/// <remarks>
/// LOGIC: Immutable record containing all filter state.
/// Used to pass complete filter configuration to ITermFilterService.
///
/// Default values represent "no filter" (show all matching terms):
/// - Empty SearchText: no text filter
/// - ShowInactive = true: show all terms regardless of active status
/// - Null CategoryFilter/SeverityFilter: show all categories/severities
///
/// Version: v0.2.5b
/// </remarks>
public sealed record FilterCriteria
{
    /// <summary>
    /// Gets the text to search for in term, replacement, and notes fields.
    /// </summary>
    /// <remarks>
    /// LOGIC: Case-insensitive substring match.
    /// Empty or whitespace means no text filter.
    /// </remarks>
    public string SearchText { get; init; } = string.Empty;

    /// <summary>
    /// Gets whether inactive terms should be included.
    /// </summary>
    /// <remarks>
    /// LOGIC: When false (default), only active terms are shown.
    /// When true, all terms are shown regardless of IsActive status.
    /// </remarks>
    public bool ShowInactive { get; init; } = false;

    /// <summary>
    /// Gets the category to filter by, or null for all categories.
    /// </summary>
    /// <remarks>
    /// LOGIC: Case-insensitive exact match.
    /// Null or empty means show all categories.
    /// </remarks>
    public string? CategoryFilter { get; init; }

    /// <summary>
    /// Gets the severity to filter by, or null for all severities.
    /// </summary>
    /// <remarks>
    /// LOGIC: Case-insensitive exact match.
    /// Null or empty means show all severities.
    /// </remarks>
    public string? SeverityFilter { get; init; }
}
