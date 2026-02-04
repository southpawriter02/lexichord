// =============================================================================
// File: ClaimSearchCriteria.cs
// Project: Lexichord.Abstractions
// Description: Search criteria for claim queries.
// =============================================================================
// LOGIC: Defines flexible filter criteria for querying claims with support
//   for pagination, sorting, and full-text search.
//
// v0.5.6h: Claim Repository (Knowledge Graph Claim Persistence)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Claims.Repository;

/// <summary>
/// Search criteria for querying claims.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Provides flexible filtering options for claim queries
/// with support for pagination, sorting, and full-text search.
/// </para>
/// <para>
/// <b>Usage:</b>
/// <code>
/// var criteria = new ClaimSearchCriteria
/// {
///     ProjectId = projectGuid,
///     Predicate = ClaimPredicate.ACCEPTS,
///     MinConfidence = 0.7f,
///     PageSize = 20
/// };
/// </code>
/// </para>
/// </remarks>
public record ClaimSearchCriteria
{
    #region Entity Filters

    /// <summary>
    /// Filter by project identifier.
    /// </summary>
    public Guid? ProjectId { get; init; }

    /// <summary>
    /// Filter by document identifier.
    /// </summary>
    public Guid? DocumentId { get; init; }

    /// <summary>
    /// Filter by subject entity identifier.
    /// </summary>
    public Guid? SubjectEntityId { get; init; }

    /// <summary>
    /// Filter by object entity identifier.
    /// </summary>
    public Guid? ObjectEntityId { get; init; }

    #endregion

    #region Predicate Filters

    /// <summary>
    /// Filter by exact predicate match.
    /// </summary>
    public string? Predicate { get; init; }

    /// <summary>
    /// Filter by any of the specified predicates.
    /// </summary>
    public IReadOnlyList<string>? Predicates { get; init; }

    #endregion

    #region Status Filters

    /// <summary>
    /// Filter by validation status.
    /// </summary>
    public ClaimValidationStatus? ValidationStatus { get; init; }

    /// <summary>
    /// Filter by minimum confidence threshold.
    /// </summary>
    public float? MinConfidence { get; init; }

    /// <summary>
    /// Filter by maximum confidence threshold.
    /// </summary>
    public float? MaxConfidence { get; init; }

    /// <summary>
    /// Filter by review status.
    /// </summary>
    public bool? IsReviewed { get; init; }

    /// <summary>
    /// Filter by active status.
    /// </summary>
    public bool? IsActive { get; init; }

    /// <summary>
    /// Filter by extraction method.
    /// </summary>
    public ClaimExtractionMethod? ExtractionMethod { get; init; }

    #endregion

    #region Text Search

    /// <summary>
    /// Full-text search query on evidence and entity names.
    /// </summary>
    public string? TextQuery { get; init; }

    #endregion

    #region Date Filters

    /// <summary>
    /// Filter for claims extracted after this date.
    /// </summary>
    public DateTimeOffset? ExtractedAfter { get; init; }

    /// <summary>
    /// Filter for claims extracted before this date.
    /// </summary>
    public DateTimeOffset? ExtractedBefore { get; init; }

    #endregion

    #region Sorting

    /// <summary>
    /// Field to sort by.
    /// </summary>
    public ClaimSortField SortBy { get; init; } = ClaimSortField.ExtractedAt;

    /// <summary>
    /// Sort direction.
    /// </summary>
    public SortDirection SortDirection { get; init; } = SortDirection.Descending;

    #endregion

    #region Pagination

    /// <summary>
    /// Page number (0-based).
    /// </summary>
    public int Page { get; init; }

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public int PageSize { get; init; } = 50;

    /// <summary>
    /// Alternative: explicit skip count.
    /// </summary>
    public int? Skip { get; init; }

    /// <summary>
    /// Alternative: explicit take count.
    /// </summary>
    public int? Take { get; init; }

    #endregion

    /// <summary>
    /// Creates an empty criteria (returns all claims).
    /// </summary>
    public static ClaimSearchCriteria Empty => new();
}

/// <summary>
/// Sort fields for claim queries.
/// </summary>
public enum ClaimSortField
{
    /// <summary>Sort by extraction timestamp.</summary>
    ExtractedAt,

    /// <summary>Sort by confidence score.</summary>
    Confidence,

    /// <summary>Sort by predicate type.</summary>
    Predicate,

    /// <summary>Sort by validation status.</summary>
    ValidationStatus
}

/// <summary>
/// Sort direction for queries.
/// </summary>
public enum SortDirection
{
    /// <summary>Sort ascending (A-Z, 0-9, oldest first).</summary>
    Ascending,

    /// <summary>Sort descending (Z-A, 9-0, newest first).</summary>
    Descending
}
