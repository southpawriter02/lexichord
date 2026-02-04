// =============================================================================
// File: IContradictionService.cs
// Project: Lexichord.Abstractions
// Description: Interface for managing detected contradictions.
// =============================================================================
// VERSION: v0.5.9e (Contradiction Detection & Resolution)
// LOGIC: Defines the contract for contradiction lifecycle management:
//   - FlagAsync: Record a new contradiction from deduplication pipeline.
//   - GetByIdAsync: Retrieve a contradiction by ID.
//   - GetPendingAsync: List pending contradictions for admin review.
//   - ResolveAsync: Apply a resolution decision.
//   - DismissAsync: Mark as false positive.
//   - AutoResolveAsync: System-initiated resolution (document changes).
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Service interface for managing detected contradictions between chunks.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9e as part of the Semantic Memory Deduplication feature.
/// </para>
/// <para>
/// This service manages the full lifecycle of contradictions:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="FlagAsync"/>: Records new contradictions from the deduplication pipeline.</description></item>
///   <item><description><see cref="GetPendingAsync"/>: Retrieves contradictions awaiting review.</description></item>
///   <item><description><see cref="ResolveAsync"/>: Applies admin resolution decisions.</description></item>
///   <item><description><see cref="DismissAsync"/>: Marks false positives as dismissed.</description></item>
/// </list>
/// <para>
/// <b>Integration Points:</b>
/// </para>
/// <list type="bullet">
///   <item><description><see cref="IDeduplicationService"/>: Calls <see cref="FlagAsync"/> when contradictions are detected.</description></item>
///   <item><description><see cref="ICanonicalManager"/>: Resolution may trigger canonical record updates.</description></item>
///   <item><description>Admin UI: Provides review queue and resolution workflow.</description></item>
/// </list>
/// <para>
/// <b>Events:</b>
/// </para>
/// <list type="bullet">
///   <item><description><c>ContradictionDetectedEvent</c>: Published when a new contradiction is flagged.</description></item>
///   <item><description><c>ContradictionResolvedEvent</c>: Published when a contradiction is resolved.</description></item>
/// </list>
/// <para>
/// <b>License:</b> Requires Writer Pro tier for full functionality.
/// Unlicensed users can view contradictions but cannot resolve them.
/// </para>
/// </remarks>
public interface IContradictionService
{
    /// <summary>
    /// Flags a new contradiction detected by the deduplication pipeline.
    /// </summary>
    /// <param name="chunkAId">The first conflicting chunk ID.</param>
    /// <param name="chunkBId">The second conflicting chunk ID.</param>
    /// <param name="similarityScore">Similarity score between the chunks.</param>
    /// <param name="confidence">Classification confidence that they are contradictory.</param>
    /// <param name="reason">Optional explanation of the contradiction.</param>
    /// <param name="projectId">Optional project scope for multi-tenant scenarios.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created <see cref="Contradiction"/> record with database-assigned ID.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="chunkAId"/> equals <paramref name="chunkBId"/>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> This method:
    /// </para>
    /// <list type="number">
    ///   <item><description>Checks for existing contradiction between the same chunk pair.</description></item>
    ///   <item><description>Creates a new record if none exists (or updates severity if re-detected).</description></item>
    ///   <item><description>Publishes <c>ContradictionDetectedEvent</c> for downstream consumers.</description></item>
    /// </list>
    /// <para>
    /// Duplicate detection is symmetric: (A, B) and (B, A) are treated as the same pair.
    /// </para>
    /// </remarks>
    Task<Contradiction> FlagAsync(
        Guid chunkAId,
        Guid chunkBId,
        float similarityScore,
        float confidence,
        string? reason = null,
        Guid? projectId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves a contradiction by its ID.
    /// </summary>
    /// <param name="contradictionId">The contradiction ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The <see cref="Contradiction"/> record, or <c>null</c> if not found.</returns>
    Task<Contradiction?> GetByIdAsync(Guid contradictionId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves contradictions involving a specific chunk.
    /// </summary>
    /// <param name="chunkId">The chunk ID to search for.</param>
    /// <param name="includeResolved">Whether to include resolved contradictions. Default: false.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of <see cref="Contradiction"/> records involving the chunk.</returns>
    /// <remarks>
    /// Searches both <see cref="Contradiction.ChunkAId"/> and <see cref="Contradiction.ChunkBId"/>.
    /// </remarks>
    Task<IReadOnlyList<Contradiction>> GetByChunkIdAsync(
        Guid chunkId,
        bool includeResolved = false,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves all pending contradictions awaiting admin review.
    /// </summary>
    /// <param name="projectId">Optional project filter. Null returns all projects.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// List of pending <see cref="Contradiction"/> records ordered by detection date (oldest first).
    /// </returns>
    /// <remarks>
    /// Returns only contradictions with <see cref="ContradictionStatus.Pending"/> or
    /// <see cref="ContradictionStatus.UnderReview"/> status.
    /// </remarks>
    Task<IReadOnlyList<Contradiction>> GetPendingAsync(
        Guid? projectId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets statistics about contradictions in the system.
    /// </summary>
    /// <param name="projectId">Optional project filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="ContradictionStatistics"/> summary.</returns>
    Task<ContradictionStatistics> GetStatisticsAsync(
        Guid? projectId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Begins review of a contradiction, transitioning it to UnderReview status.
    /// </summary>
    /// <param name="contradictionId">The contradiction ID.</param>
    /// <param name="reviewerId">The admin identity beginning the review.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated <see cref="Contradiction"/> in UnderReview status.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the contradiction ID is not found.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the contradiction is not in <see cref="ContradictionStatus.Pending"/> status
    /// or is already being reviewed by another admin.
    /// </exception>
    Task<Contradiction> BeginReviewAsync(
        Guid contradictionId,
        string reviewerId,
        CancellationToken ct = default);

    /// <summary>
    /// Resolves a contradiction by applying a resolution decision.
    /// </summary>
    /// <param name="contradictionId">The contradiction ID to resolve.</param>
    /// <param name="resolution">The resolution decision to apply.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated <see cref="Contradiction"/> in Resolved status.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the contradiction ID is not found.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when:
    /// <list type="bullet">
    ///   <item><description>The contradiction is already resolved.</description></item>
    ///   <item><description>The resolution type is CreateSynthesis but no synthesized content provided.</description></item>
    /// </list>
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Resolution processing based on type:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="ContradictionResolutionType.KeepOlder"/>: Archives newer chunk as variant.</description></item>
    ///   <item><description><see cref="ContradictionResolutionType.KeepNewer"/>: Archives older chunk as variant.</description></item>
    ///   <item><description><see cref="ContradictionResolutionType.KeepBoth"/>: Clears contradiction flag, links chunks.</description></item>
    ///   <item><description><see cref="ContradictionResolutionType.CreateSynthesis"/>: Creates new canonical from synthesis.</description></item>
    ///   <item><description><see cref="ContradictionResolutionType.DeleteBoth"/>: Deletes both chunks and canonicals.</description></item>
    /// </list>
    /// <para>
    /// Publishes <c>ContradictionResolvedEvent</c> upon successful resolution.
    /// </para>
    /// </remarks>
    Task<Contradiction> ResolveAsync(
        Guid contradictionId,
        ContradictionResolution resolution,
        CancellationToken ct = default);

    /// <summary>
    /// Dismisses a contradiction as a false positive.
    /// </summary>
    /// <param name="contradictionId">The contradiction ID to dismiss.</param>
    /// <param name="reason">Explanation for why this is a false positive.</param>
    /// <param name="dismissedBy">The admin identity dismissing the contradiction.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated <see cref="Contradiction"/> in Dismissed status.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the contradiction ID is not found.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the contradiction is already in a terminal state.
    /// </exception>
    /// <remarks>
    /// Dismissal is appropriate when the relationship classifier incorrectly
    /// identified a contradiction, or when the conflicting information is
    /// intentionally different (e.g., different contexts, time periods).
    /// </remarks>
    Task<Contradiction> DismissAsync(
        Guid contradictionId,
        string reason,
        string dismissedBy,
        CancellationToken ct = default);

    /// <summary>
    /// Automatically resolves a contradiction due to external changes.
    /// </summary>
    /// <param name="contradictionId">The contradiction ID to auto-resolve.</param>
    /// <param name="reason">System-generated reason for auto-resolution.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated <see cref="Contradiction"/> in AutoResolved status.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the contradiction ID is not found.</exception>
    /// <remarks>
    /// Called by the system when:
    /// <list type="bullet">
    ///   <item><description>One of the conflicting chunks is deleted.</description></item>
    ///   <item><description>One of the source documents is removed from the index.</description></item>
    ///   <item><description>One of the conflicting chunks is updated (re-indexed).</description></item>
    /// </list>
    /// </remarks>
    Task<Contradiction> AutoResolveAsync(
        Guid contradictionId,
        string reason,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a contradiction record permanently.
    /// </summary>
    /// <param name="contradictionId">The contradiction ID to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the record was deleted, false if not found.</returns>
    /// <remarks>
    /// <b>Warning:</b> This permanently removes the contradiction record and audit trail.
    /// Use <see cref="DismissAsync"/> or <see cref="ResolveAsync"/> for normal workflows.
    /// </remarks>
    Task<bool> DeleteAsync(Guid contradictionId, CancellationToken ct = default);
}

/// <summary>
/// Statistics summary for contradictions in the system.
/// </summary>
/// <remarks>
/// <b>Introduced in:</b> v0.5.9e as part of the Semantic Memory Deduplication feature.
/// </remarks>
/// <param name="TotalCount">Total number of contradiction records.</param>
/// <param name="PendingCount">Number of contradictions pending review.</param>
/// <param name="UnderReviewCount">Number of contradictions currently under review.</param>
/// <param name="ResolvedCount">Number of resolved contradictions.</param>
/// <param name="DismissedCount">Number of dismissed contradictions.</param>
/// <param name="AutoResolvedCount">Number of auto-resolved contradictions.</param>
/// <param name="HighConfidenceCount">Number of pending contradictions with high confidence.</param>
/// <param name="OldestPendingAge">Age of the oldest pending contradiction, if any.</param>
public record ContradictionStatistics(
    int TotalCount,
    int PendingCount,
    int UnderReviewCount,
    int ResolvedCount,
    int DismissedCount,
    int AutoResolvedCount,
    int HighConfidenceCount,
    TimeSpan? OldestPendingAge);
