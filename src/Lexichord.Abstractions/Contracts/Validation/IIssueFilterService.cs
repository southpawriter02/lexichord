// -----------------------------------------------------------------------
// <copyright file="IIssueFilterService.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;

namespace Lexichord.Abstractions.Contracts.Validation;

/// <summary>
/// Provides filtering, sorting, and searching of unified validation issues.
/// Supports multiple filter criteria with AND-composition and efficient in-memory execution.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This service operates entirely in-memory on the provided issues list.
/// Filters are composable — all active criteria are combined with AND logic. Sorting
/// supports multiple criteria with primary/secondary/tertiary ordering.
/// </para>
/// <para>
/// <b>Filter Presets:</b> Common filter configurations can be saved as named presets
/// for quick reuse. Six default presets are provided: "Errors Only", "Warnings and Errors",
/// "Auto-Fixable Only", "Style Issues", "Grammar Issues", "Knowledge Issues".
/// </para>
/// <para>
/// <b>Performance:</b> Filtering is optimized for &lt;50ms on 1000 issues with early-exit
/// on failing criteria. Wildcard pattern matching uses compiled regex for efficiency.
/// </para>
/// <para>
/// <b>Thread Safety:</b> All methods are thread-safe. The service is registered as a singleton.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5i as part of the Issue Filters feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var issues = validationResult.Issues;
///
/// // Filter by category and severity
/// var filtered = await filterService.FilterAsync(
///     issues,
///     new IssueFilterOptions
///     {
///         Categories = [IssueCategory.Style],
///         MinimumSeverity = UnifiedSeverity.Warning,
///         SortBy = [SortCriteria.Severity, SortCriteria.Location]
///     });
///
/// // Search for specific issues
/// var searchResults = await filterService.SearchAsync(issues, "OAuth");
///
/// // Use a preset
/// var preset = filterService.LoadPreset("Errors Only");
/// if (preset is not null)
///     filtered = await filterService.FilterAsync(issues, preset);
/// </code>
/// </example>
/// <seealso cref="IssueFilterOptions"/>
/// <seealso cref="SortCriteria"/>
/// <seealso cref="UnifiedIssue"/>
/// <seealso cref="FilterCriteria"/>
public interface IIssueFilterService
{
    /// <summary>
    /// Filters issues based on the provided criteria.
    /// </summary>
    /// <param name="issues">The issues to filter. Must not be null.</param>
    /// <param name="options">Filter options specifying criteria and sort order. Must not be null.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>A filtered and sorted list of issues matching all criteria.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="issues"/> or <paramref name="options"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> All active criteria in <paramref name="options"/> are combined with AND logic.
    /// An issue must match <em>all</em> active criteria to be included in the result.
    /// Pagination (Limit/Offset) is applied after filtering but before sorting.
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<UnifiedIssue>> FilterAsync(
        IReadOnlyList<UnifiedIssue> issues,
        IssueFilterOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Searches issues by text content across message, source ID, and source type.
    /// </summary>
    /// <param name="issues">The issues to search. Must not be null.</param>
    /// <param name="query">
    /// Search query string. Case-insensitive substring match against
    /// <see cref="UnifiedIssue.Message"/>, <see cref="UnifiedIssue.SourceId"/>,
    /// and <see cref="UnifiedIssue.SourceType"/>.
    /// </param>
    /// <param name="options">
    /// Optional additional filter options to combine with the search. When provided,
    /// the search text is merged into these options.
    /// </param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>Issues matching the search query and any additional filter criteria.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="issues"/> or <paramref name="query"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Delegates to <see cref="FilterAsync"/> with the query merged into
    /// the <see cref="IssueFilterOptions.SearchText"/> property.
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<UnifiedIssue>> SearchAsync(
        IReadOnlyList<UnifiedIssue> issues,
        string query,
        IssueFilterOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Filters issues by category.
    /// </summary>
    /// <param name="issues">The issues to filter. Must not be null.</param>
    /// <param name="categories">
    /// Categories to include. An issue is included if its
    /// <see cref="UnifiedIssue.Category"/> is in this collection.
    /// </param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>Issues matching any of the specified categories.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="issues"/> or <paramref name="categories"/> is null.
    /// </exception>
    Task<IReadOnlyList<UnifiedIssue>> FilterByCategoryAsync(
        IReadOnlyList<UnifiedIssue> issues,
        IEnumerable<IssueCategory> categories,
        CancellationToken ct = default);

    /// <summary>
    /// Filters issues by severity level.
    /// </summary>
    /// <param name="issues">The issues to filter. Must not be null.</param>
    /// <param name="minimumSeverity">
    /// Minimum severity to include. Issues must be at least this severe to pass.
    /// Since <see cref="UnifiedSeverity"/> uses lower values for higher severity
    /// (Error=0, Hint=3), an issue passes when its numeric value is less than or
    /// equal to <paramref name="minimumSeverity"/>.
    /// </param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>Issues with severity at least as severe as <paramref name="minimumSeverity"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="issues"/> is null.
    /// </exception>
    Task<IReadOnlyList<UnifiedIssue>> FilterBySeverityAsync(
        IReadOnlyList<UnifiedIssue> issues,
        UnifiedSeverity minimumSeverity,
        CancellationToken ct = default);

    /// <summary>
    /// Filters issues by document location.
    /// </summary>
    /// <param name="issues">The issues to filter. Must not be null.</param>
    /// <param name="location">
    /// The <see cref="TextSpan"/> defining the document region to filter by.
    /// </param>
    /// <param name="includePartialOverlaps">
    /// If <c>true</c>, includes issues that partially overlap with <paramref name="location"/>.
    /// If <c>false</c> (default), only includes issues fully contained within <paramref name="location"/>.
    /// </param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>Issues within or overlapping with the specified location.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="issues"/> is null.
    /// </exception>
    Task<IReadOnlyList<UnifiedIssue>> FilterByLocationAsync(
        IReadOnlyList<UnifiedIssue> issues,
        TextSpan location,
        bool includePartialOverlaps = false,
        CancellationToken ct = default);

    /// <summary>
    /// Filters issues by line number range.
    /// </summary>
    /// <param name="issues">The issues to filter. Must not be null.</param>
    /// <param name="startLine">Start line number (1-based, inclusive).</param>
    /// <param name="endLine">End line number (1-based, inclusive).</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>Issues within the specified line range.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="issues"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Line numbers are approximated from character offsets since
    /// <see cref="TextSpan"/> does not store line information. The approximation
    /// assumes an average of 80 characters per line.
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<UnifiedIssue>> FilterByLineRangeAsync(
        IReadOnlyList<UnifiedIssue> issues,
        int startLine,
        int endLine,
        CancellationToken ct = default);

    /// <summary>
    /// Sorts issues by the specified criteria.
    /// </summary>
    /// <param name="issues">The issues to sort. Must not be null.</param>
    /// <param name="sortBy">
    /// Sort criteria in priority order. The first criterion is primary,
    /// second is secondary (tiebreaker), etc.
    /// </param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>Issues sorted by the specified criteria in ascending order.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="issues"/> or <paramref name="sortBy"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Multiple sort criteria are applied using the standard
    /// <c>OrderBy</c>/<c>ThenBy</c> chain pattern. If no criteria are provided,
    /// the original order is preserved.
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<UnifiedIssue>> SortAsync(
        IReadOnlyList<UnifiedIssue> issues,
        IEnumerable<SortCriteria> sortBy,
        CancellationToken ct = default);

    /// <summary>
    /// Counts issues by severity level.
    /// </summary>
    /// <param name="issues">The issues to analyze. Must not be null.</param>
    /// <returns>
    /// A dictionary mapping each <see cref="UnifiedSeverity"/> to its issue count.
    /// Only severities with at least one issue are included.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="issues"/> is null.
    /// </exception>
    IReadOnlyDictionary<UnifiedSeverity, int> CountBySeverity(
        IReadOnlyList<UnifiedIssue> issues);

    /// <summary>
    /// Counts issues by category.
    /// </summary>
    /// <param name="issues">The issues to analyze. Must not be null.</param>
    /// <returns>
    /// A dictionary mapping each <see cref="IssueCategory"/> to its issue count.
    /// Only categories with at least one issue are included.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="issues"/> is null.
    /// </exception>
    IReadOnlyDictionary<IssueCategory, int> CountByCategory(
        IReadOnlyList<UnifiedIssue> issues);

    /// <summary>
    /// Saves a named filter preset for later reuse.
    /// </summary>
    /// <param name="name">
    /// Preset name (e.g., "Errors Only", "My Review Filter").
    /// If a preset with this name already exists, it is overwritten.
    /// </param>
    /// <param name="options">Filter options to save as the preset.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name"/> or <paramref name="options"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> is empty or whitespace.
    /// </exception>
    void SavePreset(string name, IssueFilterOptions options);

    /// <summary>
    /// Loads a previously saved filter preset.
    /// </summary>
    /// <param name="name">The preset name to load.</param>
    /// <returns>
    /// The <see cref="IssueFilterOptions"/> for the preset, or <c>null</c> if no
    /// preset with the given name exists.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name"/> is null.
    /// </exception>
    IssueFilterOptions? LoadPreset(string name);

    /// <summary>
    /// Lists the names of all saved filter presets, including default presets.
    /// </summary>
    /// <returns>An ordered list of preset names.</returns>
    IReadOnlyList<string> ListPresets();

    /// <summary>
    /// Deletes a previously saved filter preset.
    /// </summary>
    /// <param name="name">The preset name to delete.</param>
    /// <returns>
    /// <c>true</c> if the preset was found and deleted; <c>false</c> if no preset
    /// with the given name exists.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="name"/> is null.
    /// </exception>
    bool DeletePreset(string name);
}
