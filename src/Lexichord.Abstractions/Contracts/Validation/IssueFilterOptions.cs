// -----------------------------------------------------------------------
// <copyright file="IssueFilterOptions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;

namespace Lexichord.Abstractions.Contracts.Validation;

/// <summary>
/// Configuration for filtering, sorting, and paginating unified validation issues.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This record defines all available filter criteria for
/// <see cref="IIssueFilterService.FilterAsync"/>. All criteria are combined
/// with AND logic — an issue must match every active criterion to be included.
/// Empty/null criteria are treated as "no filter" (pass-through).
/// </para>
/// <para>
/// <b>Naming:</b> Named <c>IssueFilterOptions</c> instead of <c>FilterOptions</c>
/// to avoid collision with <see cref="Lexichord.Abstractions.Contracts.FilterOptions"/>
/// (v0.2.5b terminology filtering).
/// </para>
/// <para>
/// <b>Severity Ordering:</b> <see cref="UnifiedSeverity"/> uses lower numeric values
/// for higher severity (Error=0, Warning=1, Info=2, Hint=3). The
/// <see cref="MinimumSeverity"/> and <see cref="MaximumSeverity"/> fields define an
/// inclusive range where <c>MaximumSeverity &lt;= issue.Severity &lt;= MinimumSeverity</c>
/// in numeric terms.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This record is immutable and thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5i as part of the Issue Filters feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Filter to show only errors and warnings from the Style Linter
/// var options = new IssueFilterOptions
/// {
///     Categories = [IssueCategory.Style],
///     MinimumSeverity = UnifiedSeverity.Warning,
///     SortBy = [SortCriteria.Severity, SortCriteria.Location]
/// };
///
/// // Filter auto-fixable issues only
/// var autoFixOptions = new IssueFilterOptions
/// {
///     OnlyAutoFixable = true,
///     SortBy = [SortCriteria.Category, SortCriteria.Location]
/// };
///
/// // Use wildcard patterns for issue codes
/// var codeFilter = new IssueFilterOptions
/// {
///     IssueCodes = ["STYLE_*", "TERM_001"],
///     ExcludeCodes = ["STYLE_099"]
/// };
/// </code>
/// </example>
/// <seealso cref="IIssueFilterService"/>
/// <seealso cref="SortCriteria"/>
/// <seealso cref="UnifiedIssue"/>
public record IssueFilterOptions
{
    /// <summary>
    /// Issue categories to include. Empty list means all categories pass.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> When non-empty, only issues whose <see cref="UnifiedIssue.Category"/>
    /// is in this list are included. An empty list (default) disables category filtering.
    /// </remarks>
    public IReadOnlyList<IssueCategory> Categories { get; init; } = [];

    /// <summary>
    /// Minimum severity level to include (least severe threshold).
    /// Default: <see cref="UnifiedSeverity.Hint"/> (includes all severities).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Since <see cref="UnifiedSeverity"/> uses lower numeric values for
    /// higher severity (Error=0, Hint=3), an issue passes this filter when
    /// <c>(int)issue.Severity &lt;= (int)MinimumSeverity</c>.
    /// </para>
    /// <para>
    /// Examples:
    /// <list type="bullet">
    ///   <item><description>Hint (3): includes Error, Warning, Info, Hint (all)</description></item>
    ///   <item><description>Warning (1): includes Error, Warning only</description></item>
    ///   <item><description>Error (0): includes Error only</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public UnifiedSeverity MinimumSeverity { get; init; } = UnifiedSeverity.Hint;

    /// <summary>
    /// Maximum severity level to include (most severe threshold).
    /// Default: <see cref="UnifiedSeverity.Error"/> (includes all severities).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> An issue passes this filter when
    /// <c>(int)issue.Severity &gt;= (int)MaximumSeverity</c>.
    /// Combined with <see cref="MinimumSeverity"/>, defines an inclusive severity range.
    /// </para>
    /// <para>
    /// Example: MaximumSeverity=Warning, MinimumSeverity=Info filters to Warning and Info only.
    /// </para>
    /// </remarks>
    public UnifiedSeverity MaximumSeverity { get; init; } = UnifiedSeverity.Error;

    /// <summary>
    /// Specific issue codes to include (e.g., "STYLE_001", "AXIOM_*").
    /// Supports wildcard patterns using <c>*</c>. Empty list means all codes pass.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Matches against <see cref="UnifiedIssue.SourceId"/>.
    /// Wildcard <c>*</c> matches any sequence of characters.
    /// An issue passes if its SourceId matches any pattern in this list.
    /// </remarks>
    public IReadOnlyList<string> IssueCodes { get; init; } = [];

    /// <summary>
    /// Specific issue codes to exclude. Supports wildcard patterns using <c>*</c>.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Matches against <see cref="UnifiedIssue.SourceId"/>.
    /// An issue is excluded if its SourceId matches any pattern in this list.
    /// Exclusion is applied after inclusion (<see cref="IssueCodes"/>).
    /// </remarks>
    public IReadOnlyList<string> ExcludeCodes { get; init; } = [];

    /// <summary>
    /// Text to search for in message, source ID, and source type.
    /// Case-insensitive substring match. Null or empty disables text search.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> An issue passes if any of <see cref="UnifiedIssue.Message"/>,
    /// <see cref="UnifiedIssue.SourceId"/>, or <see cref="UnifiedIssue.SourceType"/>
    /// contains this text (case-insensitive).
    /// </remarks>
    public string? SearchText { get; init; }

    /// <summary>
    /// Document location filter (character span).
    /// Null disables location filtering.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> An issue passes if its <see cref="UnifiedIssue.Location"/> is
    /// fully contained within this span (start &gt;= span.Start and end &lt;= span.End).
    /// </remarks>
    public TextSpan? LocationSpan { get; init; }

    /// <summary>
    /// Line number range filter (1-based, inclusive).
    /// Null disables line range filtering.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Line numbers are approximated from character offsets since
    /// <see cref="TextSpan"/> does not store line information directly.
    /// </para>
    /// </remarks>
    public (int StartLine, int EndLine)? LineRange { get; init; }

    /// <summary>
    /// When <c>true</c>, only includes issues that can be automatically fixed.
    /// Default: <c>false</c> (all issues pass).
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> An issue passes when <see cref="UnifiedIssue.CanAutoFix"/> is <c>true</c>.
    /// </remarks>
    public bool OnlyAutoFixable { get; init; }

    /// <summary>
    /// When <c>true</c>, only includes issues that require manual review.
    /// Default: <c>false</c> (all issues pass).
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> An issue passes when <see cref="UnifiedIssue.CanAutoFix"/> is <c>false</c>.
    /// Note: Setting both <see cref="OnlyAutoFixable"/> and <see cref="OnlyManual"/> to
    /// <c>true</c> results in no issues matching (conflicting filters).
    /// </remarks>
    public bool OnlyManual { get; init; }

    /// <summary>
    /// Criteria by which to sort results. Empty list preserves natural order.
    /// The first criterion is primary, second is secondary (tiebreaker), etc.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Applied after filtering. Uses <c>OrderBy</c>/<c>ThenBy</c> chain.
    /// Direction is controlled by <see cref="SortAscending"/>.
    /// </remarks>
    public IReadOnlyList<SortCriteria> SortBy { get; init; } = [];

    /// <summary>
    /// If <c>true</c>, sort ascending; if <c>false</c>, descending.
    /// Default: <c>true</c>.
    /// </summary>
    public bool SortAscending { get; init; } = true;

    /// <summary>
    /// Validator source type names to include (e.g., "StyleLinter", "Validation").
    /// Matches against <see cref="UnifiedIssue.SourceType"/>.
    /// Empty list means all validators pass.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> An issue passes if its <see cref="UnifiedIssue.SourceType"/>
    /// is in this list. Standard values: "StyleLinter", "GrammarLinter", "Validation".
    /// </remarks>
    public IReadOnlyList<string> ValidatorNames { get; init; } = [];

    /// <summary>
    /// Maximum number of results to return. 0 means unlimited (default).
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Applied after filtering and before sorting. Used with
    /// <see cref="Offset"/> for pagination.
    /// </remarks>
    public int Limit { get; init; } = 0;

    /// <summary>
    /// Number of results to skip (for pagination). Default: 0.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Applied after filtering. Skips this many results before
    /// applying <see cref="Limit"/>.
    /// </remarks>
    public int Offset { get; init; } = 0;

    /// <summary>
    /// Gets the default filter options (no filtering, no sorting — returns all issues).
    /// </summary>
    public static IssueFilterOptions Default { get; } = new();
}

/// <summary>
/// Criteria by which unified validation issues can be sorted.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Used in <see cref="IssueFilterOptions.SortBy"/> to specify one or more
/// sort criteria. Multiple criteria are applied in order (first is primary, second is
/// tiebreaker, etc.).
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5i as part of the Issue Filters feature.
/// </para>
/// </remarks>
/// <seealso cref="IssueFilterOptions"/>
/// <seealso cref="IIssueFilterService.SortAsync"/>
public enum SortCriteria
{
    /// <summary>
    /// Sort by severity level (Error first in ascending order).
    /// Uses <see cref="UnifiedIssue.SeverityOrder"/>.
    /// </summary>
    Severity,

    /// <summary>
    /// Sort by document position (top of document first in ascending order).
    /// Uses <see cref="UnifiedIssue.Location"/>.<see cref="TextSpan.Start"/>.
    /// </summary>
    Location,

    /// <summary>
    /// Sort by issue category (Style, Grammar, Knowledge, Structure, Custom).
    /// Uses <see cref="UnifiedIssue.Category"/> enum value.
    /// </summary>
    Category,

    /// <summary>
    /// Sort by message text (alphabetical in ascending order).
    /// Uses <see cref="UnifiedIssue.Message"/>.
    /// </summary>
    Message,

    /// <summary>
    /// Sort by issue source identifier (alphabetical in ascending order).
    /// Uses <see cref="UnifiedIssue.SourceId"/>.
    /// </summary>
    Code,

    /// <summary>
    /// Sort by validator source type name (alphabetical in ascending order).
    /// Uses <see cref="UnifiedIssue.SourceType"/>.
    /// </summary>
    ValidatorName,

    /// <summary>
    /// Sort by auto-fixability (auto-fixable issues first in ascending order).
    /// Uses <see cref="UnifiedIssue.CanAutoFix"/>.
    /// </summary>
    AutoFixable
}
