// =============================================================================
// File: SearchFilter.cs
// Project: Lexichord.Abstractions
// Description: Filter model records for the Filter System feature (v0.5.5a).
// =============================================================================
// LOGIC: Defines immutable records for search filtering:
//   - DateRange: Temporal bounds for date-based filtering with factory methods
//   - SearchFilter: Primary filter criteria container with computed properties
//   - FilterPreset: Saved filter configuration with identity and management
//   All records are positional with optional parameters for flexibility.
//   Factory methods provide common use cases with sensible defaults.
// =============================================================================
// VERSION: v0.5.5a (Filter Model)
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Defines a date range for temporal filtering.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="DateRange"/> specifies temporal bounds for filtering documents
/// by modification date. Either bound can be null to indicate an open-ended range.
/// Both bounds being null represents "any time" (no temporal restriction).
/// </para>
/// <para>
/// <b>Range Interpretation:</b>
/// <list type="bullet">
///   <item><description>Start only: Documents modified on or after the start date.</description></item>
///   <item><description>End only: Documents modified on or before the end date.</description></item>
///   <item><description>Both: Documents modified within the inclusive range.</description></item>
///   <item><description>Neither: No temporal restriction.</description></item>
/// </list>
/// </para>
/// <para>
/// Use the factory methods <see cref="LastDays"/>, <see cref="LastHours"/>,
/// <see cref="Today"/>, and <see cref="ForMonth"/> for common date ranges.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.5a as part of The Filter System feature.
/// </para>
/// </remarks>
/// <param name="Start">Inclusive start date (null = no lower bound).</param>
/// <param name="End">Inclusive end date (null = no upper bound).</param>
/// <example>
/// <code>
/// // Last 7 days
/// var recent = DateRange.LastDays(7);
///
/// // Specific month
/// var january = DateRange.ForMonth(2026, 1);
///
/// // Custom range
/// var custom = new DateRange(
///     new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
///     new DateTime(2026, 1, 31, 23, 59, 59, DateTimeKind.Utc));
///
/// // Validate before use
/// if (!custom.IsValid)
///     throw new ArgumentException("Invalid date range");
/// </code>
/// </example>
public record DateRange(DateTime? Start, DateTime? End)
{
    /// <summary>
    /// Gets whether this is an open-ended range (at least one bound is null).
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="Start"/> or <see cref="End"/> is null; otherwise, <c>false</c>.
    /// </value>
    public bool IsOpenEnded => Start is null || End is null;

    /// <summary>
    /// Gets whether this range has any bounds specified.
    /// </summary>
    /// <value>
    /// <c>true</c> if at least one bound is specified; <c>false</c> if both are null.
    /// </value>
    public bool HasBounds => Start is not null || End is not null;

    /// <summary>
    /// Validates that Start is not after End when both are provided.
    /// </summary>
    /// <value>
    /// <c>true</c> if the range is valid (Start &lt;= End, or either is null);
    /// <c>false</c> if Start is after End.
    /// </value>
    /// <remarks>
    /// Open-ended ranges are always considered valid.
    /// </remarks>
    public bool IsValid => Start is null || End is null || Start <= End;

    /// <summary>
    /// Creates a "last N days" date range.
    /// </summary>
    /// <param name="days">Number of days to look back. Must be non-negative.</param>
    /// <returns>A <see cref="DateRange"/> from N days ago until now (no upper bound).</returns>
    /// <remarks>
    /// The start date is calculated from <see cref="DateTime.UtcNow"/>.
    /// The range is open-ended on the upper bound to include all recent documents.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Documents modified in the last week
    /// var lastWeek = DateRange.LastDays(7);
    ///
    /// // Documents modified in the last month
    /// var lastMonth = DateRange.LastDays(30);
    /// </code>
    /// </example>
    public static DateRange LastDays(int days) =>
        new(DateTime.UtcNow.AddDays(-days), null);

    /// <summary>
    /// Creates a "last N hours" date range.
    /// </summary>
    /// <param name="hours">Number of hours to look back. Must be non-negative.</param>
    /// <returns>A <see cref="DateRange"/> from N hours ago until now (no upper bound).</returns>
    /// <remarks>
    /// Useful for finding very recently modified documents.
    /// The start time is calculated from <see cref="DateTime.UtcNow"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Documents modified in the last 24 hours
    /// var lastDay = DateRange.LastHours(24);
    ///
    /// // Documents modified in the last hour
    /// var lastHour = DateRange.LastHours(1);
    /// </code>
    /// </example>
    public static DateRange LastHours(int hours) =>
        new(DateTime.UtcNow.AddHours(-hours), null);

    /// <summary>
    /// Creates a date range for today only.
    /// </summary>
    /// <returns>A <see cref="DateRange"/> covering the current UTC day from midnight to end of day.</returns>
    /// <remarks>
    /// The range spans from the start of today (00:00:00) to just before midnight (23:59:59.9999999).
    /// Uses UTC to ensure consistent behavior across time zones.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Documents modified today
    /// var today = DateRange.Today();
    /// </code>
    /// </example>
    public static DateRange Today()
    {
        var today = DateTime.UtcNow.Date;
        return new(today, today.AddDays(1).AddTicks(-1));
    }

    /// <summary>
    /// Creates a date range for a specific month.
    /// </summary>
    /// <param name="year">The year (e.g., 2026).</param>
    /// <param name="month">The month (1-12).</param>
    /// <returns>A <see cref="DateRange"/> covering the entire specified month.</returns>
    /// <remarks>
    /// The range spans from the first day of the month (00:00:00) to just before
    /// the first day of the next month (23:59:59.9999999 on the last day).
    /// </remarks>
    /// <example>
    /// <code>
    /// // Documents modified in January 2026
    /// var january = DateRange.ForMonth(2026, 1);
    ///
    /// // Documents modified in December 2025
    /// var december = DateRange.ForMonth(2025, 12);
    /// </code>
    /// </example>
    public static DateRange ForMonth(int year, int month)
    {
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1).AddTicks(-1);
        return new(start, end);
    }
}

/// <summary>
/// Defines search filtering criteria to narrow results to specific documents or metadata.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="SearchFilter"/> specifies criteria for narrowing search results.
/// All properties are optional; null values indicate no filtering for that criterion.
/// Multiple criteria are combined with AND logic (all must match).
/// </para>
/// <para>
/// <b>Filter Criteria:</b>
/// <list type="bullet">
///   <item><description><see cref="PathPatterns"/>: Glob patterns for path matching (e.g., "docs/**/*.md").</description></item>
///   <item><description><see cref="FileExtensions"/>: File extensions without dots (e.g., "md", "txt").</description></item>
///   <item><description><see cref="ModifiedRange"/>: Temporal bounds for modification date.</description></item>
///   <item><description><see cref="Tags"/>: Document tags (reserved for future use).</description></item>
///   <item><description><see cref="HasHeadings"/>: Filter to chunks with heading context.</description></item>
/// </list>
/// </para>
/// <para>
/// Use the factory methods <see cref="ForPath"/>, <see cref="ForExtensions"/>, and
/// <see cref="RecentlyModified"/> for common filter scenarios. The <see cref="Empty"/>
/// property returns a filter with no criteria (matches all documents).
/// </para>
/// <para>
/// This record is immutable; use the <c>with</c> expression to create modified copies.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.5a as part of The Filter System feature.
/// </para>
/// </remarks>
/// <param name="PathPatterns">
/// Glob patterns for path matching (e.g., "docs/**/*.md").
/// Supports standard glob syntax: ** (any path), * (any characters), ? (single character).
/// Null indicates no path filtering.
/// </param>
/// <param name="FileExtensions">
/// File extensions to include, without leading dot (e.g., "md", "txt").
/// Case-insensitive matching is applied at query time.
/// Null indicates no extension filtering.
/// </param>
/// <param name="ModifiedRange">
/// Date range for file modification time filtering.
/// Null indicates no temporal restriction.
/// </param>
/// <param name="Tags">
/// Document tags to filter by. Reserved for future tagging feature.
/// Currently unused; null is expected.
/// </param>
/// <param name="HasHeadings">
/// When true, only include chunks that have heading context.
/// When false or null, no heading restriction is applied.
/// </param>
/// <example>
/// <code>
/// // Empty filter (matches all)
/// var all = SearchFilter.Empty;
///
/// // Single path pattern
/// var docs = SearchFilter.ForPath("docs/**");
///
/// // Multiple extensions
/// var textFiles = SearchFilter.ForExtensions("md", "txt", "rst");
///
/// // Recently modified
/// var recent = SearchFilter.RecentlyModified(7);
///
/// // Combined filter using 'with'
/// var combined = SearchFilter.ForPath("docs/**") with
/// {
///     FileExtensions = new[] { "md" },
///     ModifiedRange = DateRange.LastDays(30)
/// };
/// </code>
/// </example>
public record SearchFilter(
    IReadOnlyList<string>? PathPatterns = null,
    IReadOnlyList<string>? FileExtensions = null,
    DateRange? ModifiedRange = null,
    IReadOnlyList<string>? Tags = null,
    bool? HasHeadings = null)
{
    /// <summary>
    /// Gets whether any filter criteria are applied.
    /// </summary>
    /// <value>
    /// <c>true</c> if at least one filter criterion is specified;
    /// <c>false</c> if all criteria are null or empty.
    /// </value>
    /// <remarks>
    /// An empty filter (HasCriteria = false) returns all results without filtering.
    /// </remarks>
    public bool HasCriteria =>
        PathPatterns?.Count > 0 ||
        FileExtensions?.Count > 0 ||
        ModifiedRange is not null ||
        Tags?.Count > 0 ||
        HasHeadings == true;  // Only true is considered a filter; false/null means no restriction

    /// <summary>
    /// Gets the total number of distinct criteria applied.
    /// </summary>
    /// <value>
    /// Count of non-null/non-empty criteria (0-5).
    /// </value>
    /// <remarks>
    /// Useful for displaying filter complexity to users or for analytics.
    /// </remarks>
    public int CriteriaCount
    {
        get
        {
            var count = 0;
            if (PathPatterns?.Count > 0) count++;
            if (FileExtensions?.Count > 0) count++;
            if (ModifiedRange is not null) count++;
            if (Tags?.Count > 0) count++;
            if (HasHeadings == true) count++;
            return count;
        }
    }

    /// <summary>
    /// Returns an empty filter with no criteria.
    /// </summary>
    /// <value>
    /// A <see cref="SearchFilter"/> instance with all properties set to null.
    /// </value>
    /// <remarks>
    /// Use this as a starting point for building filters or when no filtering is needed.
    /// </remarks>
    public static SearchFilter Empty => new();

    /// <summary>
    /// Creates a filter for a single path pattern.
    /// </summary>
    /// <param name="pattern">The glob pattern (e.g., "docs/**", "src/**/*.cs").</param>
    /// <returns>A <see cref="SearchFilter"/> with the path pattern applied.</returns>
    /// <remarks>
    /// <para>
    /// Supports standard glob syntax:
    /// <list type="bullet">
    ///   <item><description><c>**</c> matches any path (including subdirectories).</description></item>
    ///   <item><description><c>*</c> matches any characters within a path segment.</description></item>
    ///   <item><description><c>?</c> matches a single character.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Match all files in docs folder recursively
    /// var docs = SearchFilter.ForPath("docs/**");
    ///
    /// // Match markdown files in any folder
    /// var markdown = SearchFilter.ForPath("**/*.md");
    /// </code>
    /// </example>
    public static SearchFilter ForPath(string pattern) =>
        new(PathPatterns: new[] { pattern });

    /// <summary>
    /// Creates a filter for specific file extensions.
    /// </summary>
    /// <param name="extensions">Extensions without dots (e.g., "md", "txt", "json").</param>
    /// <returns>A <see cref="SearchFilter"/> with the extensions applied.</returns>
    /// <remarks>
    /// Extensions are matched case-insensitively at query time.
    /// Do not include the leading dot.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Match markdown and text files
    /// var textFiles = SearchFilter.ForExtensions("md", "txt");
    ///
    /// // Match configuration files
    /// var configs = SearchFilter.ForExtensions("json", "yaml", "yml");
    /// </code>
    /// </example>
    public static SearchFilter ForExtensions(params string[] extensions) =>
        new(FileExtensions: extensions);

    /// <summary>
    /// Creates a filter for recently modified documents.
    /// </summary>
    /// <param name="days">Number of days in the past to include.</param>
    /// <returns>A <see cref="SearchFilter"/> with the date range applied.</returns>
    /// <remarks>
    /// Uses <see cref="DateRange.LastDays"/> to create an open-ended range
    /// starting from N days ago.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Documents modified in the last week
    /// var lastWeek = SearchFilter.RecentlyModified(7);
    ///
    /// // Documents modified in the last month
    /// var lastMonth = SearchFilter.RecentlyModified(30);
    /// </code>
    /// </example>
    public static SearchFilter RecentlyModified(int days) =>
        new(ModifiedRange: DateRange.LastDays(days));
}

/// <summary>
/// A saved filter configuration that can be reused.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="FilterPreset"/> stores a named <see cref="SearchFilter"/> configuration
/// that can be saved and quickly applied later. Presets enable users to define
/// common filter combinations once and reuse them efficiently.
/// </para>
/// <para>
/// Presets are persisted as JSON in user settings:
/// <list type="bullet">
///   <item><description>Windows: <c>%APPDATA%/Lexichord/settings/filter-presets.json</c></description></item>
///   <item><description>macOS: <c>~/Library/Application Support/Lexichord/settings/filter-presets.json</c></description></item>
///   <item><description>Linux: <c>~/.config/Lexichord/settings/filter-presets.json</c></description></item>
/// </list>
/// </para>
/// <para>
/// <b>License Gating:</b> Saved presets require WriterPro tier or higher.
/// Team-shared presets (<see cref="IsShared"/> = true) require Teams tier or higher.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.5a as part of The Filter System feature.
/// </para>
/// </remarks>
/// <param name="Id">Unique identifier for the preset.</param>
/// <param name="Name">User-defined display name.</param>
/// <param name="Filter">The filter criteria to apply when this preset is selected.</param>
/// <param name="CreatedAt">When the preset was created (UTC).</param>
/// <param name="IsShared">
/// Whether this preset is shared with team members.
/// Only applicable for Teams tier or higher. Defaults to false.
/// </param>
/// <example>
/// <code>
/// // Create a new preset
/// var preset = FilterPreset.Create("API Documentation",
///     SearchFilter.ForPath("docs/api/**") with { FileExtensions = new[] { "md" } });
///
/// // Rename a preset
/// var renamed = preset.Rename("API Docs");
///
/// // Update the filter criteria
/// var updated = preset.UpdateFilter(preset.Filter with { ModifiedRange = DateRange.LastDays(90) });
/// </code>
/// </example>
public record FilterPreset(
    Guid Id,
    string Name,
    SearchFilter Filter,
    DateTime CreatedAt,
    bool IsShared = false)
{
    /// <summary>
    /// Creates a new preset with a generated ID and current timestamp.
    /// </summary>
    /// <param name="name">The preset name.</param>
    /// <param name="filter">The filter criteria.</param>
    /// <returns>A new <see cref="FilterPreset"/> instance with generated ID and CreatedAt.</returns>
    /// <remarks>
    /// The ID is generated using <see cref="Guid.NewGuid"/>.
    /// The CreatedAt timestamp uses <see cref="DateTime.UtcNow"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// var preset = FilterPreset.Create("Release Notes",
    ///     SearchFilter.ForPath("docs/releases/**"));
    ///
    /// Console.WriteLine($"Created preset {preset.Id} at {preset.CreatedAt}");
    /// </code>
    /// </example>
    public static FilterPreset Create(string name, SearchFilter filter) =>
        new(Guid.NewGuid(), name, filter, DateTime.UtcNow);

    /// <summary>
    /// Creates a copy of this preset with a new name.
    /// </summary>
    /// <param name="newName">The new preset name.</param>
    /// <returns>A new <see cref="FilterPreset"/> with the updated name.</returns>
    /// <remarks>
    /// All other properties (Id, Filter, CreatedAt, IsShared) are preserved.
    /// </remarks>
    /// <example>
    /// <code>
    /// var original = FilterPreset.Create("Docs", SearchFilter.ForPath("docs/**"));
    /// var renamed = original.Rename("Documentation");
    ///
    /// // original.Name == "Docs"
    /// // renamed.Name == "Documentation"
    /// // original.Id == renamed.Id (same ID)
    /// </code>
    /// </example>
    public FilterPreset Rename(string newName) =>
        this with { Name = newName };

    /// <summary>
    /// Creates a copy of this preset with updated filter criteria.
    /// </summary>
    /// <param name="newFilter">The updated filter criteria.</param>
    /// <returns>A new <see cref="FilterPreset"/> with the updated filter.</returns>
    /// <remarks>
    /// The Id, Name, CreatedAt, and IsShared properties are preserved.
    /// Use this to modify the filter while keeping the preset identity.
    /// </remarks>
    /// <example>
    /// <code>
    /// var preset = FilterPreset.Create("Recent Docs", SearchFilter.RecentlyModified(7));
    ///
    /// // Update to include path filter
    /// var updated = preset.UpdateFilter(preset.Filter with
    /// {
    ///     PathPatterns = new[] { "docs/**" }
    /// });
    /// </code>
    /// </example>
    public FilterPreset UpdateFilter(SearchFilter newFilter) =>
        this with { Filter = newFilter };
}
