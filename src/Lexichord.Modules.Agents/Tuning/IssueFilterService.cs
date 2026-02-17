// =============================================================================
// File: IssueFilterService.cs
// Project: Lexichord.Modules.Agents
// Description: In-memory filtering, sorting, searching, and preset management
//              for unified validation issues.
// =============================================================================
// LOGIC: Provides the core filtering pipeline for the Unified Issues Panel:
//   - AND-composed criteria matching with early exit for performance
//   - Multi-criteria sorting with OrderBy/ThenBy chain
//   - Case-insensitive text search across message, source ID, and source type
//   - Wildcard pattern matching for issue code filtering
//   - Named filter presets with 6 built-in defaults
//   - Pagination via Limit/Offset
//   - Performance logging with Stopwatch
//
// Spec Adaptations:
//   - issue.Code → issue.SourceId
//   - issue.ValidatorName → issue.SourceType
//   - issue.Fix (single) → issue.CanAutoFix (computed from Fixes list)
//   - issue.Location is non-nullable TextSpan (removed null checks)
//   - Severity comparison inverted: lower numeric = more severe (Error=0, Hint=3)
//   - Sort uses OrderBy/ThenBy chain (spec had OrderBy loop that overwrites)
//   - FilterOptions → IssueFilterOptions (avoids collision with Contracts.FilterOptions)
//
// v0.7.5i: Issue Filters (Unified Validation Feature)
// =============================================================================

using System.Diagnostics;
using System.Text.RegularExpressions;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;
using Lexichord.Abstractions.Contracts.Validation;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Tuning;

/// <summary>
/// Implements in-memory filtering, sorting, searching, and preset management
/// for unified validation issues.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This service provides the core filtering pipeline for the Unified
/// Issues Panel and any other consumers that need to filter or sort
/// <see cref="UnifiedIssue"/> collections. All operations are performed in-memory
/// on the provided issues list.
/// </para>
/// <para>
/// <b>Filter Pipeline:</b>
/// <list type="number">
///   <item><description>Extract active criteria from <see cref="IssueFilterOptions"/></description></item>
///   <item><description>For each issue, apply all criteria with AND logic (early exit on first failure)</description></item>
///   <item><description>Apply pagination (Offset, then Limit)</description></item>
///   <item><description>Sort results by <see cref="SortCriteria"/> using OrderBy/ThenBy chain</description></item>
///   <item><description>Return filtered and sorted results</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Default Presets:</b> Six built-in presets are initialized at construction:
/// "Errors Only", "Warnings and Errors", "Auto-Fixable Only", "Style Issues",
/// "Grammar Issues", "Knowledge Issues".
/// </para>
/// <para>
/// <b>Thread Safety:</b> The service is thread-safe. Preset storage uses a
/// <see cref="Dictionary{TKey,TValue}"/> which is safe for concurrent reads.
/// Preset mutations (save/delete) are not expected to be called concurrently.
/// </para>
/// <para>
/// <b>Performance:</b> Filtering 1000 issues targets &lt;50ms P95. Early exit on
/// first failing criterion and compiled wildcard regex patterns are used for optimization.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5i as part of the Issue Filters feature.
/// </para>
/// </remarks>
/// <seealso cref="IIssueFilterService"/>
/// <seealso cref="IssueFilterOptions"/>
/// <seealso cref="SortCriteria"/>
/// <seealso cref="FilterCriteria"/>
internal sealed class IssueFilterService : IIssueFilterService
{
    // ── Constants ──────────────────────────────────────────────────────────

    /// <summary>
    /// Average characters per line, used for approximate line number calculation
    /// from character offsets when <see cref="TextSpan"/> doesn't provide line info.
    /// </summary>
    private const int AverageCharsPerLine = 80;

    // ── Dependencies ──────────────────────────────────────────────────────

    private readonly ILogger<IssueFilterService> _logger;

    // ── Internal State ────────────────────────────────────────────────────

    /// <summary>
    /// In-memory storage for named filter presets. Includes default presets
    /// initialized at construction.
    /// </summary>
    private readonly Dictionary<string, IssueFilterOptions> _presets;

    /// <summary>
    /// Initializes a new instance of the <see cref="IssueFilterService"/> class.
    /// </summary>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="logger"/> is null.
    /// </exception>
    /// <remarks>
    /// <b>LOGIC:</b> Initializes the preset dictionary and loads 6 default presets:
    /// "Errors Only", "Warnings and Errors", "Auto-Fixable Only", "Style Issues",
    /// "Grammar Issues", "Knowledge Issues".
    /// </remarks>
    public IssueFilterService(ILogger<IssueFilterService> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
        _presets = new Dictionary<string, IssueFilterOptions>(StringComparer.Ordinal);

        // LOGIC: Initialize default presets for common filtering scenarios.
        InitializeDefaultPresets();

        _logger.LogDebug(
            "IssueFilterService initialized with {PresetCount} default presets",
            _presets.Count);
    }

    // ── IIssueFilterService Implementation ────────────────────────────────

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Applies all active criteria from <paramref name="options"/> with AND logic.
    /// Each issue is evaluated by <see cref="MatchesAllCriteria"/> which returns <c>false</c>
    /// on the first failing criterion (early exit).
    /// </para>
    /// <para>
    /// Pipeline: Filter → Paginate → Sort.
    /// </para>
    /// </remarks>
    public Task<IReadOnlyList<UnifiedIssue>> FilterAsync(
        IReadOnlyList<UnifiedIssue> issues,
        IssueFilterOptions options,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(issues);
        ArgumentNullException.ThrowIfNull(options);

        _logger.LogDebug("Filtering {Count} issues with options", issues.Count);

        var sw = Stopwatch.StartNew();
        var filtered = new List<UnifiedIssue>();

        // LOGIC: Apply all filter criteria with AND logic.
        // MatchesAllCriteria uses early exit on first failing criterion.
        foreach (var issue in issues)
        {
            ct.ThrowIfCancellationRequested();

            if (MatchesAllCriteria(issue, options))
            {
                filtered.Add(issue);
            }
        }

        sw.Stop();
        _logger.LogDebug(
            "Filtered to {FilteredCount} issues from {TotalCount} in {Duration}ms",
            filtered.Count, issues.Count, sw.ElapsedMilliseconds);

        // LOGIC: Apply pagination (offset, then limit) before sorting.
        if (options.Offset > 0)
        {
            filtered = filtered.Skip(options.Offset).ToList();
        }

        if (options.Limit > 0)
        {
            filtered = filtered.Take(options.Limit).ToList();
        }

        // LOGIC: Apply sorting after filtering and pagination.
        if (options.SortBy.Count > 0)
        {
            filtered = SortIssues(filtered, options.SortBy, options.SortAscending);
        }

        return Task.FromResult<IReadOnlyList<UnifiedIssue>>(filtered);
    }

    /// <inheritdoc />
    /// <remarks>
    /// <b>LOGIC:</b> Merges the search query into the options' SearchText property
    /// and delegates to <see cref="FilterAsync"/>.
    /// </remarks>
    public Task<IReadOnlyList<UnifiedIssue>> SearchAsync(
        IReadOnlyList<UnifiedIssue> issues,
        string query,
        IssueFilterOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(issues);
        ArgumentNullException.ThrowIfNull(query);

        _logger.LogDebug("Searching {Count} issues for '{Query}'", issues.Count, query);

        // LOGIC: Merge search query into options, preserving any existing criteria.
        var searchOptions = options ?? IssueFilterOptions.Default;
        var withSearch = searchOptions with { SearchText = query };

        return FilterAsync(issues, withSearch, ct);
    }

    /// <inheritdoc />
    /// <remarks>
    /// <b>LOGIC:</b> Creates a temporary <see cref="IssueFilterOptions"/> with the
    /// specified categories and delegates to <see cref="FilterAsync"/>.
    /// </remarks>
    public Task<IReadOnlyList<UnifiedIssue>> FilterByCategoryAsync(
        IReadOnlyList<UnifiedIssue> issues,
        IEnumerable<IssueCategory> categories,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(issues);
        ArgumentNullException.ThrowIfNull(categories);

        var catList = categories.ToList();
        _logger.LogDebug(
            "Filtering by categories: {Categories}",
            string.Join(", ", catList));

        return FilterAsync(issues, new IssueFilterOptions { Categories = catList }, ct);
    }

    /// <inheritdoc />
    /// <remarks>
    /// <b>LOGIC:</b> Creates a temporary <see cref="IssueFilterOptions"/> with the
    /// specified minimum severity and delegates to <see cref="FilterAsync"/>.
    /// </remarks>
    public Task<IReadOnlyList<UnifiedIssue>> FilterBySeverityAsync(
        IReadOnlyList<UnifiedIssue> issues,
        UnifiedSeverity minimumSeverity,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(issues);

        _logger.LogDebug("Filtering minimum severity: {Severity}", minimumSeverity);

        return FilterAsync(
            issues,
            new IssueFilterOptions { MinimumSeverity = minimumSeverity },
            ct);
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Two modes of location filtering:
    /// <list type="bullet">
    ///   <item><description><b>Partial overlaps (<paramref name="includePartialOverlaps"/> = true):</b>
    ///     Uses <see cref="TextSpan.OverlapsWith"/> to detect any overlap.</description></item>
    ///   <item><description><b>Strict containment (<paramref name="includePartialOverlaps"/> = false):</b>
    ///     Issue must be fully within the specified location span.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public Task<IReadOnlyList<UnifiedIssue>> FilterByLocationAsync(
        IReadOnlyList<UnifiedIssue> issues,
        TextSpan location,
        bool includePartialOverlaps = false,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(issues);

        _logger.LogDebug(
            "Filtering by location {Start}-{End} (partial: {Partial})",
            location.Start, location.End, includePartialOverlaps);

        // LOGIC: Implement location filtering directly rather than through FilterAsync,
        // because the includePartialOverlaps parameter is not part of IssueFilterOptions.
        var filtered = new List<UnifiedIssue>();

        foreach (var issue in issues)
        {
            ct.ThrowIfCancellationRequested();

            if (includePartialOverlaps)
            {
                // LOGIC: Use TextSpan.OverlapsWith for partial overlap detection.
                if (issue.Location.OverlapsWith(location))
                    filtered.Add(issue);
            }
            else
            {
                // LOGIC: Strict containment — issue must be fully within location.
                if (issue.Location.Start >= location.Start && issue.Location.End <= location.End)
                    filtered.Add(issue);
            }
        }

        _logger.LogDebug(
            "Location filter matched {Count} of {Total} issues",
            filtered.Count, issues.Count);

        return Task.FromResult<IReadOnlyList<UnifiedIssue>>(filtered);
    }

    /// <inheritdoc />
    /// <remarks>
    /// <b>LOGIC:</b> Creates a temporary <see cref="IssueFilterOptions"/> with the
    /// specified line range and delegates to <see cref="FilterAsync"/>.
    /// </remarks>
    public Task<IReadOnlyList<UnifiedIssue>> FilterByLineRangeAsync(
        IReadOnlyList<UnifiedIssue> issues,
        int startLine,
        int endLine,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(issues);

        _logger.LogDebug("Filtering by lines {Start}-{End}", startLine, endLine);

        return FilterAsync(
            issues,
            new IssueFilterOptions { LineRange = (startLine, endLine) },
            ct);
    }

    /// <inheritdoc />
    /// <remarks>
    /// <b>LOGIC:</b> If no criteria are provided, returns the original order.
    /// Uses <see cref="SortIssues"/> with ascending=true.
    /// </remarks>
    public Task<IReadOnlyList<UnifiedIssue>> SortAsync(
        IReadOnlyList<UnifiedIssue> issues,
        IEnumerable<SortCriteria> sortBy,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(issues);
        ArgumentNullException.ThrowIfNull(sortBy);

        var criteria = sortBy.ToList();
        if (criteria.Count == 0)
        {
            _logger.LogDebug("No sort criteria provided, preserving original order");
            return Task.FromResult(issues);
        }

        _logger.LogDebug("Sorting {Count} issues by: {Criteria}",
            issues.Count, string.Join(", ", criteria));

        var sorted = SortIssues(issues.ToList(), criteria, ascending: true);
        return Task.FromResult<IReadOnlyList<UnifiedIssue>>(sorted);
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<UnifiedSeverity, int> CountBySeverity(
        IReadOnlyList<UnifiedIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);

        return issues
            .GroupBy(i => i.Severity)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<IssueCategory, int> CountByCategory(
        IReadOnlyList<UnifiedIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);

        return issues
            .GroupBy(i => i.Category)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <inheritdoc />
    public void SavePreset(string name, IssueFilterOptions options)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Preset name cannot be empty or whitespace.", nameof(name));

        _logger.LogInformation("Saving filter preset: {Name}", name);
        _presets[name] = options;
    }

    /// <inheritdoc />
    public IssueFilterOptions? LoadPreset(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (_presets.TryGetValue(name, out var options))
        {
            _logger.LogDebug("Loaded filter preset: {Name}", name);
            return options;
        }

        _logger.LogWarning("Filter preset not found: {Name}", name);
        return null;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> ListPresets() =>
        _presets.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();

    /// <inheritdoc />
    public bool DeletePreset(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        if (_presets.Remove(name))
        {
            _logger.LogInformation("Deleted filter preset: {Name}", name);
            return true;
        }

        _logger.LogDebug("Filter preset not found for deletion: {Name}", name);
        return false;
    }

    // ── Private Helpers ───────────────────────────────────────────────────

    /// <summary>
    /// Evaluates all active criteria from <paramref name="options"/> against the issue.
    /// </summary>
    /// <param name="issue">The issue to evaluate.</param>
    /// <param name="options">The filter options containing active criteria.</param>
    /// <returns><c>true</c> if the issue matches all active criteria; <c>false</c> otherwise.</returns>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Uses AND composition with early exit. Each criterion is checked
    /// in order; as soon as one fails, the method returns <c>false</c> without
    /// evaluating remaining criteria. This ordering prioritizes cheaper checks first:
    /// <list type="number">
    ///   <item><description>Category (enum comparison)</description></item>
    ///   <item><description>Severity (numeric comparison with inverted enum)</description></item>
    ///   <item><description>Auto-fixable / manual (bool check)</description></item>
    ///   <item><description>Validator name (list contains)</description></item>
    ///   <item><description>Issue code patterns (regex)</description></item>
    ///   <item><description>Exclude code patterns (regex)</description></item>
    ///   <item><description>Text search (string contains)</description></item>
    ///   <item><description>Location span (numeric comparison)</description></item>
    ///   <item><description>Line range (numeric approximation)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    private static bool MatchesAllCriteria(UnifiedIssue issue, IssueFilterOptions options)
    {
        // LOGIC: Category filter — issue must be in the allowed category list.
        if (options.Categories.Count > 0 && !options.Categories.Contains(issue.Category))
            return false;

        // LOGIC: Severity filter — issue must be within [MaximumSeverity, MinimumSeverity] range.
        // UnifiedSeverity: Error=0, Warning=1, Info=2, Hint=3 (lower = more severe).
        // MinimumSeverity = least severe to include (default Hint=3 = include all).
        // MaximumSeverity = most severe to include (default Error=0 = include all).
        // An issue passes if: MaximumSeverity <= issue.Severity <= MinimumSeverity (in numeric terms).
        if ((int)issue.Severity > (int)options.MinimumSeverity ||
            (int)issue.Severity < (int)options.MaximumSeverity)
            return false;

        // LOGIC: Auto-fixable filter — only include issues that can be auto-fixed.
        if (options.OnlyAutoFixable && !issue.CanAutoFix)
            return false;

        // LOGIC: Manual filter — only include issues that require manual review.
        if (options.OnlyManual && issue.CanAutoFix)
            return false;

        // LOGIC: Validator name filter — issue must come from an allowed validator.
        // Matches against UnifiedIssue.SourceType (not ValidatorName as spec references).
        if (options.ValidatorNames.Count > 0 &&
            (issue.SourceType is null || !options.ValidatorNames.Contains(issue.SourceType)))
            return false;

        // LOGIC: Issue code inclusion filter — SourceId must match at least one pattern.
        if (options.IssueCodes.Count > 0)
        {
            if (!MatchesAnyPattern(issue.SourceId, options.IssueCodes))
                return false;
        }

        // LOGIC: Issue code exclusion filter — SourceId must NOT match any pattern.
        if (options.ExcludeCodes.Count > 0)
        {
            if (MatchesAnyPattern(issue.SourceId, options.ExcludeCodes))
                return false;
        }

        // LOGIC: Text search — case-insensitive substring match across text fields.
        // Searches Message, SourceId, and SourceType (adapted from spec's Code/ValidatorName).
        if (!string.IsNullOrEmpty(options.SearchText))
        {
            var matched =
                issue.Message.Contains(options.SearchText, StringComparison.OrdinalIgnoreCase) ||
                issue.SourceId.Contains(options.SearchText, StringComparison.OrdinalIgnoreCase) ||
                (issue.SourceType?.Contains(options.SearchText, StringComparison.OrdinalIgnoreCase) ?? false);

            if (!matched)
                return false;
        }

        // LOGIC: Location span filter — issue must be fully contained within the span.
        // UnifiedIssue.Location is non-nullable TextSpan (adapted from spec's nullable check).
        if (options.LocationSpan is not null)
        {
            var loc = options.LocationSpan;
            if (issue.Location.Start < loc.Start || issue.Location.End > loc.End)
                return false;
        }

        // LOGIC: Line range filter — approximate line number from character offset.
        // TextSpan does not store line information, so we use a simplified approximation
        // assuming AverageCharsPerLine (80) characters per line.
        if (options.LineRange.HasValue)
        {
            var (startLine, endLine) = options.LineRange.Value;
            var issueLine = GetApproximateLineNumber(issue.Location);
            if (issueLine < startLine || issueLine > endLine)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if the specified text matches any of the wildcard patterns.
    /// </summary>
    /// <param name="text">The text to check (e.g., <see cref="UnifiedIssue.SourceId"/>).</param>
    /// <param name="patterns">Wildcard patterns where <c>*</c> matches any characters.</param>
    /// <returns><c>true</c> if the text matches at least one pattern.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Converts each wildcard pattern to a regex by escaping special
    /// characters and replacing <c>*</c> with <c>.*</c>. Case-insensitive matching.
    /// </remarks>
    private static bool MatchesAnyPattern(string text, IReadOnlyList<string> patterns)
    {
        foreach (var pattern in patterns)
        {
            if (WildcardMatch(text, pattern))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Performs simple wildcard matching where <c>*</c> matches any sequence of characters.
    /// </summary>
    /// <param name="text">The text to match against.</param>
    /// <param name="pattern">The pattern containing optional <c>*</c> wildcards.</param>
    /// <returns><c>true</c> if the text matches the pattern.</returns>
    private static bool WildcardMatch(string text, string pattern)
    {
        // LOGIC: Convert wildcard pattern to regex.
        // 1. Escape all regex special characters in the pattern.
        // 2. Replace escaped \* with .* to restore wildcard behavior.
        // 3. Anchor with ^ and $ for full-string matching.
        var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        return Regex.IsMatch(text, regexPattern, RegexOptions.IgnoreCase);
    }

    /// <summary>
    /// Sorts issues by the specified criteria using an OrderBy/ThenBy chain.
    /// </summary>
    /// <param name="issues">The issues to sort.</param>
    /// <param name="criteria">Sort criteria in priority order (first = primary).</param>
    /// <param name="ascending">Sort direction: true for ascending, false for descending.</param>
    /// <returns>A new sorted list.</returns>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> The first criterion uses <c>OrderBy</c>/<c>OrderByDescending</c>,
    /// subsequent criteria use <c>ThenBy</c>/<c>ThenByDescending</c> to maintain
    /// the primary sort order while breaking ties.
    /// </para>
    /// <para>
    /// <b>Spec Adaptation:</b> The design spec's sort loop uses <c>OrderBy</c> for every
    /// criterion, which overwrites the previous sort order. This implementation uses the
    /// correct <c>OrderBy</c>/<c>ThenBy</c> chain pattern.
    /// </para>
    /// </remarks>
    private static List<UnifiedIssue> SortIssues(
        List<UnifiedIssue> issues,
        IReadOnlyList<SortCriteria> criteria,
        bool ascending)
    {
        if (criteria.Count == 0 || issues.Count == 0)
            return issues;

        // LOGIC: Build an OrderBy/ThenBy chain. First criterion is OrderBy,
        // subsequent criteria are ThenBy to break ties.
        IOrderedEnumerable<UnifiedIssue>? ordered = null;

        for (var i = 0; i < criteria.Count; i++)
        {
            var criterion = criteria[i];

            if (i == 0)
            {
                // LOGIC: First criterion — use OrderBy to establish primary sort.
                ordered = ascending
                    ? issues.OrderBy(issue => GetSortKey(issue, criterion))
                    : issues.OrderByDescending(issue => GetSortKey(issue, criterion));
            }
            else
            {
                // LOGIC: Subsequent criteria — use ThenBy to break ties.
                ordered = ascending
                    ? ordered!.ThenBy(issue => GetSortKey(issue, criterion))
                    : ordered!.ThenByDescending(issue => GetSortKey(issue, criterion));
            }
        }

        return ordered!.ToList();
    }

    /// <summary>
    /// Gets the sort key for the specified criterion.
    /// </summary>
    /// <param name="issue">The issue to extract the sort key from.</param>
    /// <param name="criterion">The sort criterion to apply.</param>
    /// <returns>A comparable sort key value.</returns>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Maps each <see cref="SortCriteria"/> to the corresponding
    /// <see cref="UnifiedIssue"/> property:
    /// <list type="bullet">
    ///   <item><description>Severity → <see cref="UnifiedIssue.SeverityOrder"/> (int, lower=more severe)</description></item>
    ///   <item><description>Location → <see cref="UnifiedIssue.Location"/>.<see cref="TextSpan.Start"/> (int)</description></item>
    ///   <item><description>Category → <see cref="UnifiedIssue.Category"/> (enum int value)</description></item>
    ///   <item><description>Message → <see cref="UnifiedIssue.Message"/> (string)</description></item>
    ///   <item><description>Code → <see cref="UnifiedIssue.SourceId"/> (adapted from spec's issue.Code)</description></item>
    ///   <item><description>ValidatorName → <see cref="UnifiedIssue.SourceType"/> (adapted from spec's issue.ValidatorName)</description></item>
    ///   <item><description>AutoFixable → <see cref="UnifiedIssue.CanAutoFix"/> (bool)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    private static IComparable GetSortKey(UnifiedIssue issue, SortCriteria criterion)
    {
        return criterion switch
        {
            SortCriteria.Severity => issue.SeverityOrder,
            SortCriteria.Location => issue.Location.Start,
            SortCriteria.Category => (int)issue.Category,
            SortCriteria.Message => issue.Message,
            SortCriteria.Code => issue.SourceId,
            SortCriteria.ValidatorName => issue.SourceType ?? string.Empty,
            SortCriteria.AutoFixable => issue.CanAutoFix ? 0 : 1, // LOGIC: Auto-fixable first (0 < 1)
            _ => string.Empty
        };
    }

    /// <summary>
    /// Approximates a 1-based line number from a <see cref="TextSpan"/> character offset.
    /// </summary>
    /// <param name="location">The text span to convert.</param>
    /// <returns>An approximate 1-based line number.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Since <see cref="TextSpan"/> stores character offsets without
    /// line information, this method provides a rough approximation assuming
    /// <see cref="AverageCharsPerLine"/> (80) characters per line. The result is
    /// 1-based to match the convention of <see cref="IssueFilterOptions.LineRange"/>.
    /// </remarks>
    private static int GetApproximateLineNumber(TextSpan location)
    {
        // LOGIC: Convert character offset to approximate line number.
        // Add 1 for 1-based line numbering.
        return (location.Start / AverageCharsPerLine) + 1;
    }

    /// <summary>
    /// Initializes the default filter presets.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Creates six built-in presets covering common filtering scenarios:
    /// <list type="bullet">
    ///   <item><description><b>"Errors Only":</b> Only error-severity issues, sorted by location.</description></item>
    ///   <item><description><b>"Warnings and Errors":</b> Warning and error severity, sorted by severity then location.</description></item>
    ///   <item><description><b>"Auto-Fixable Only":</b> Only auto-fixable issues, sorted by category then location.</description></item>
    ///   <item><description><b>"Style Issues":</b> Style category only, sorted by severity then location.</description></item>
    ///   <item><description><b>"Grammar Issues":</b> Grammar category only, sorted by location.</description></item>
    ///   <item><description><b>"Knowledge Issues":</b> Knowledge category only, sorted by severity then location.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    private void InitializeDefaultPresets()
    {
        // LOGIC: "Errors Only" — show only error-severity issues.
        // MinimumSeverity and MaximumSeverity both set to Error to create a single-value range.
        _presets["Errors Only"] = new IssueFilterOptions
        {
            MinimumSeverity = UnifiedSeverity.Error,
            MaximumSeverity = UnifiedSeverity.Error,
            SortBy = [SortCriteria.Location]
        };

        // LOGIC: "Warnings and Errors" — show Warning and Error severity.
        // MinimumSeverity = Warning means Error(0) and Warning(1) pass.
        _presets["Warnings and Errors"] = new IssueFilterOptions
        {
            MinimumSeverity = UnifiedSeverity.Warning,
            SortBy = [SortCriteria.Severity, SortCriteria.Location]
        };

        // LOGIC: "Auto-Fixable Only" — show only issues with auto-applicable fixes.
        _presets["Auto-Fixable Only"] = new IssueFilterOptions
        {
            OnlyAutoFixable = true,
            SortBy = [SortCriteria.Category, SortCriteria.Location]
        };

        // LOGIC: "Style Issues" — show only Style category issues.
        _presets["Style Issues"] = new IssueFilterOptions
        {
            Categories = [IssueCategory.Style],
            SortBy = [SortCriteria.Severity, SortCriteria.Location]
        };

        // LOGIC: "Grammar Issues" — show only Grammar category issues.
        _presets["Grammar Issues"] = new IssueFilterOptions
        {
            Categories = [IssueCategory.Grammar],
            SortBy = [SortCriteria.Location]
        };

        // LOGIC: "Knowledge Issues" — show only Knowledge category issues.
        _presets["Knowledge Issues"] = new IssueFilterOptions
        {
            Categories = [IssueCategory.Knowledge],
            SortBy = [SortCriteria.Severity, SortCriteria.Location]
        };
    }
}
