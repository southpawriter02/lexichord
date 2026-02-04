// =============================================================================
// File: PendingReview.cs
// Project: Lexichord.Abstractions
// Description: Record representing a chunk pending manual deduplication review.
// =============================================================================
// VERSION: v0.5.9d (Deduplication Service)
// LOGIC: Encapsulates a chunk that could not be automatically deduplicated
//   due to ambiguous classification, along with its candidate matches.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// A chunk pending manual review for deduplication.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9d as part of the Semantic Memory Deduplication feature.
/// </para>
/// <para>
/// Chunks are queued for review when the deduplication pipeline encounters
/// ambiguous classification (confidence below 0.7) and
/// <see cref="DeduplicationOptions.EnableManualReviewQueue"/> is enabled.
/// </para>
/// <para>
/// The admin can view pending reviews via <see cref="IDeduplicationService.GetPendingReviewsAsync"/>
/// and resolve them via <see cref="IDeduplicationService.ProcessManualDecisionAsync"/>.
/// </para>
/// </remarks>
/// <param name="ReviewId">
/// The unique identifier for this review item.
/// Used as the primary key in the pending_reviews table.
/// </param>
/// <param name="NewChunk">
/// The chunk that was queued for review.
/// Contains full chunk data including content for side-by-side comparison.
/// </param>
/// <param name="Candidates">
/// The potential duplicate candidates identified during deduplication.
/// Includes similarity scores and classifications for each candidate.
/// </param>
/// <param name="QueuedAt">
/// When this review was queued.
/// Used for ordering reviews (oldest first) in the admin queue.
/// </param>
/// <param name="AutoClassificationReason">
/// The reason why automatic classification failed.
/// Examples: "Low confidence (0.65)", "Conflicting classifications".
/// Helps the admin understand why manual review is needed.
/// </param>
public record PendingReview(
    Guid ReviewId,
    Chunk NewChunk,
    IReadOnlyList<DuplicateCandidate> Candidates,
    DateTimeOffset QueuedAt,
    string? AutoClassificationReason)
{
    /// <summary>
    /// Gets the number of duplicate candidates.
    /// </summary>
    public int CandidateCount => Candidates.Count;

    /// <summary>
    /// Gets the best candidate (highest similarity score).
    /// </summary>
    /// <value>The candidate with the highest similarity, or null if no candidates.</value>
    public DuplicateCandidate? BestCandidate =>
        Candidates.Count > 0
            ? Candidates.OrderByDescending(c => c.SimilarityScore).First()
            : null;

    /// <summary>
    /// Gets how long this review has been pending.
    /// </summary>
    public TimeSpan Age => DateTimeOffset.UtcNow - QueuedAt;
}
