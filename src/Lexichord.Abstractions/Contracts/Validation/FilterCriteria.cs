// -----------------------------------------------------------------------
// <copyright file="FilterCriteria.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;

namespace Lexichord.Abstractions.Contracts.Validation;

/// <summary>
/// Base class for composable filter criteria that can be evaluated against a
/// <see cref="UnifiedIssue"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Filter criteria are individual, reusable predicates that can be
/// combined to build complex filter expressions. Each subclass implements
/// <see cref="Matches"/> to evaluate a specific aspect of an issue.
/// </para>
/// <para>
/// <b>Composition:</b> Multiple criteria can be combined with AND logic by requiring
/// all criteria to return <c>true</c> from their <see cref="Matches"/> method.
/// </para>
/// <para>
/// <b>Thread Safety:</b> All filter criteria are immutable records and are thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5i as part of the Issue Filters feature.
/// </para>
/// </remarks>
/// <seealso cref="CategoryFilterCriterion"/>
/// <seealso cref="SeverityFilterCriterion"/>
/// <seealso cref="LocationFilterCriterion"/>
/// <seealso cref="TextSearchFilterCriterion"/>
/// <seealso cref="CodeFilterCriterion"/>
public abstract record FilterCriteria
{
    /// <summary>
    /// Evaluates this criterion against the specified issue.
    /// </summary>
    /// <param name="issue">The issue to evaluate. Must not be null.</param>
    /// <returns>
    /// <c>true</c> if the issue matches this criterion; <c>false</c> otherwise.
    /// </returns>
    public abstract bool Matches(UnifiedIssue issue);
}

/// <summary>
/// Matches issues by their <see cref="IssueCategory"/>.
/// </summary>
/// <param name="Categories">
/// The categories to match. An issue matches if its category is in this collection.
/// </param>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Performs a simple <c>Contains</c> check against the issue's
/// <see cref="UnifiedIssue.Category"/> property.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5i as part of the Issue Filters feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var criterion = new CategoryFilterCriterion(new[] { IssueCategory.Style, IssueCategory.Grammar });
/// bool matches = criterion.Matches(issue); // true if issue.Category is Style or Grammar
/// </code>
/// </example>
public record CategoryFilterCriterion(IEnumerable<IssueCategory> Categories) : FilterCriteria
{
    /// <inheritdoc />
    public override bool Matches(UnifiedIssue issue) =>
        Categories.Contains(issue.Category);
}

/// <summary>
/// Matches issues by their severity level.
/// </summary>
/// <param name="MinimumSeverity">
/// The least severe level to include. Issues must be at least this severe to match.
/// </param>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Since <see cref="UnifiedSeverity"/> uses lower numeric values for
/// higher severity (Error=0, Warning=1, Info=2, Hint=3), an issue matches when
/// <c>(int)issue.Severity &lt;= (int)MinimumSeverity</c>.
/// </para>
/// <para>
/// <b>Spec Adaptation:</b> The design spec uses <c>issue.Severity &gt;= MinimumSeverity</c>
/// which would be incorrect for the inverted enum. This implementation uses the
/// corrected comparison direction.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5i as part of the Issue Filters feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var criterion = new SeverityFilterCriterion(UnifiedSeverity.Warning);
/// criterion.Matches(errorIssue);   // true  (Error=0 &lt;= Warning=1)
/// criterion.Matches(warningIssue); // true  (Warning=1 &lt;= Warning=1)
/// criterion.Matches(infoIssue);    // false (Info=2 > Warning=1)
/// </code>
/// </example>
public record SeverityFilterCriterion(UnifiedSeverity MinimumSeverity) : FilterCriteria
{
    /// <inheritdoc />
    /// <remarks>
    /// <b>LOGIC:</b> Lower numeric severity value = more severe.
    /// An issue matches if it is at least as severe as MinimumSeverity.
    /// </remarks>
    public override bool Matches(UnifiedIssue issue) =>
        (int)issue.Severity <= (int)MinimumSeverity;
}

/// <summary>
/// Matches issues by their document location.
/// </summary>
/// <param name="Location">The <see cref="TextSpan"/> defining the target region.</param>
/// <param name="IncludePartialOverlaps">
/// If <c>true</c>, matches issues that partially overlap with the location.
/// If <c>false</c>, only matches issues fully contained within the location.
/// </param>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Uses <see cref="TextSpan.OverlapsWith"/> for partial overlap detection,
/// or start/end containment checks for strict containment.
/// </para>
/// <para>
/// <b>Spec Adaptation:</b> The design spec treats <c>issue.Location</c> as nullable,
/// but the actual <see cref="UnifiedIssue.Location"/> is a non-nullable
/// <see cref="TextSpan"/>. Null checks are removed.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5i as part of the Issue Filters feature.
/// </para>
/// </remarks>
public record LocationFilterCriterion(TextSpan Location, bool IncludePartialOverlaps) : FilterCriteria
{
    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Two modes of location matching:
    /// <list type="bullet">
    ///   <item><description><b>Partial overlaps:</b> Issue location and target location share
    ///     at least one character position. Uses <see cref="TextSpan.OverlapsWith"/>.</description></item>
    ///   <item><description><b>Strict containment:</b> Issue location is fully contained within
    ///     the target location (issue.Start &gt;= location.Start AND issue.End &lt;= location.End).</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public override bool Matches(UnifiedIssue issue)
    {
        // LOGIC: Use TextSpan.OverlapsWith for partial overlap detection.
        if (IncludePartialOverlaps)
            return issue.Location.OverlapsWith(Location);

        // LOGIC: Strict containment — issue must be fully within the target span.
        return issue.Location.Start >= Location.Start && issue.Location.End <= Location.End;
    }
}

/// <summary>
/// Matches issues by text content across message, source ID, and source type.
/// </summary>
/// <param name="SearchText">
/// The text to search for. Case-insensitive substring match.
/// </param>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> An issue matches if any of its text fields contain the search text:
/// <list type="bullet">
///   <item><description><see cref="UnifiedIssue.Message"/></description></item>
///   <item><description><see cref="UnifiedIssue.SourceId"/></description></item>
///   <item><description><see cref="UnifiedIssue.SourceType"/> (when non-null)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Spec Adaptation:</b> The design spec references <c>issue.Code</c> and
/// <c>issue.ValidatorName</c>. Actual properties are <c>SourceId</c> and
/// <c>SourceType</c>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5i as part of the Issue Filters feature.
/// </para>
/// </remarks>
public record TextSearchFilterCriterion(string SearchText) : FilterCriteria
{
    /// <inheritdoc />
    public override bool Matches(UnifiedIssue issue)
    {
        // LOGIC: Case-insensitive substring match across all text fields.
        return issue.Message.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
               issue.SourceId.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
               (issue.SourceType?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false);
    }
}

/// <summary>
/// Matches issues by source ID code pattern, supporting wildcard patterns.
/// </summary>
/// <param name="CodePatterns">
/// Code patterns to match against <see cref="UnifiedIssue.SourceId"/>.
/// Supports <c>*</c> wildcard matching any sequence of characters (e.g., "STYLE_*").
/// </param>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> An issue matches if its <see cref="UnifiedIssue.SourceId"/> matches
/// any of the provided patterns. Patterns use simple wildcard syntax where <c>*</c>
/// matches zero or more characters. Regex special characters are escaped.
/// </para>
/// <para>
/// <b>Spec Adaptation:</b> The design spec references <c>issue.Code</c>.
/// Actual property is <see cref="UnifiedIssue.SourceId"/>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5i as part of the Issue Filters feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var criterion = new CodeFilterCriterion(new[] { "STYLE_*", "TERM_001" });
/// criterion.Matches(issueWithSourceId_STYLE_001); // true  (matches "STYLE_*")
/// criterion.Matches(issueWithSourceId_TERM_001);  // true  (matches "TERM_001")
/// criterion.Matches(issueWithSourceId_GRAMMAR_01); // false (no match)
/// </code>
/// </example>
public record CodeFilterCriterion(IEnumerable<string> CodePatterns) : FilterCriteria
{
    /// <inheritdoc />
    public override bool Matches(UnifiedIssue issue)
    {
        // LOGIC: Check if the issue's SourceId matches any of the wildcard patterns.
        foreach (var pattern in CodePatterns)
        {
            if (WildcardMatch(issue.SourceId, pattern))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Performs simple wildcard matching where <c>*</c> matches any characters.
    /// </summary>
    /// <param name="text">The text to check.</param>
    /// <param name="pattern">The pattern with optional <c>*</c> wildcards.</param>
    /// <returns><c>true</c> if the text matches the pattern.</returns>
    private static bool WildcardMatch(string text, string pattern)
    {
        // LOGIC: Convert wildcard pattern to regex.
        // Escape all regex special characters, then replace escaped \* with .* for wildcard.
        var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        return Regex.IsMatch(text, regexPattern, RegexOptions.IgnoreCase);
    }
}
