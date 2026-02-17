// -----------------------------------------------------------------------
// <copyright file="SortOptions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.Validation;

/// <summary>
/// Configuration for sorting unified validation issues with multi-level criteria.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Provides a structured way to specify up to three levels of sort criteria.
/// When primary values are equal, the secondary criterion is used as a tiebreaker,
/// then tertiary. This is an alternative to using <see cref="IssueFilterOptions.SortBy"/>
/// which accepts a flat list of criteria.
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
/// var sortOptions = new SortOptions
/// {
///     Primary = SortCriteria.Severity,
///     Secondary = SortCriteria.Location,
///     Tertiary = SortCriteria.Category,
///     Ascending = true
/// };
/// </code>
/// </example>
/// <seealso cref="SortCriteria"/>
/// <seealso cref="IIssueFilterService.SortAsync"/>
/// <seealso cref="IssueFilterOptions"/>
public record SortOptions
{
    /// <summary>
    /// Primary sort criterion. This is the most significant sort key.
    /// </summary>
    public required SortCriteria Primary { get; init; }

    /// <summary>
    /// Secondary sort criterion (tiebreaker when primary values are equal).
    /// Null means no secondary sort is applied.
    /// </summary>
    public SortCriteria? Secondary { get; init; }

    /// <summary>
    /// Tertiary sort criterion (tiebreaker when both primary and secondary are equal).
    /// Null means no tertiary sort is applied.
    /// </summary>
    public SortCriteria? Tertiary { get; init; }

    /// <summary>
    /// If <c>true</c>, sort ascending; if <c>false</c>, descending.
    /// Default: <c>true</c>.
    /// </summary>
    public bool Ascending { get; init; } = true;
}
