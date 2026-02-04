// =============================================================================
// File: ClaimQueryResult.cs
// Project: Lexichord.Abstractions
// Description: Paginated result container for claim queries.
// =============================================================================
// LOGIC: Wraps query results with pagination metadata including total count,
//   current page, and navigation helpers.
//
// v0.5.6h: Claim Repository (Knowledge Graph Claim Persistence)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Claims.Repository;

/// <summary>
/// Paginated result container for claim queries.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Provides query results with pagination metadata
/// for efficient browsing of large claim datasets.
/// </para>
/// </remarks>
public record ClaimQueryResult
{
    /// <summary>
    /// Claims in the current page.
    /// </summary>
    public required IReadOnlyList<Claim> Claims { get; init; }

    /// <summary>
    /// Total count of claims matching the criteria.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Current page number (0-based).
    /// </summary>
    public int Page { get; init; }

    /// <summary>
    /// Page size used for this query.
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Total number of pages available.
    /// </summary>
    public int TotalPages => PageSize > 0
        ? (int)Math.Ceiling((double)TotalCount / PageSize)
        : 0;

    /// <summary>
    /// Whether there are more pages after this one.
    /// </summary>
    public bool HasMore => Page < TotalPages - 1;

    /// <summary>
    /// Whether there are pages before this one.
    /// </summary>
    public bool HasPrevious => Page > 0;

    /// <summary>
    /// Creates an empty result with no claims.
    /// </summary>
    public static ClaimQueryResult Empty => new()
    {
        Claims = Array.Empty<Claim>(),
        TotalCount = 0,
        Page = 0,
        PageSize = 50
    };
}

/// <summary>
/// Result of bulk upsert operation.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Tracks the outcome of bulk upsert operations,
/// counting created, updated, and unchanged claims.
/// </para>
/// </remarks>
public record BulkUpsertResult
{
    /// <summary>
    /// Number of new claims created.
    /// </summary>
    public int CreatedCount { get; init; }

    /// <summary>
    /// Number of existing claims updated.
    /// </summary>
    public int UpdatedCount { get; init; }

    /// <summary>
    /// Number of claims that were unchanged.
    /// </summary>
    public int UnchangedCount { get; init; }

    /// <summary>
    /// Claims that failed to upsert with error details.
    /// </summary>
    public IReadOnlyList<ClaimUpsertError>? Errors { get; init; }

    /// <summary>
    /// Total number of claims processed.
    /// </summary>
    public int TotalProcessed => CreatedCount + UpdatedCount + UnchangedCount;

    /// <summary>
    /// Whether all claims were processed successfully.
    /// </summary>
    public bool AllSucceeded => Errors == null || Errors.Count == 0;

    /// <summary>
    /// Creates an empty result (zero processed).
    /// </summary>
    public static BulkUpsertResult Empty => new();
}

/// <summary>
/// Error details for a failed claim upsert.
/// </summary>
public record ClaimUpsertError
{
    /// <summary>
    /// The claim identifier that failed.
    /// </summary>
    public Guid ClaimId { get; init; }

    /// <summary>
    /// Error message describing the failure.
    /// </summary>
    public string Error { get; init; } = "";
}
