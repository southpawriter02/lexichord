// =============================================================================
// File: ManualMergeDecision.cs
// Project: Lexichord.Abstractions
// Description: Record representing a manual decision for a queued review item.
// =============================================================================
// VERSION: v0.5.9d (Deduplication Service)
// LOGIC: Encapsulates an admin's decision for a pending review item, including
//   the decision type, target canonical (for merge/link), and notes.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// A manual decision for a queued review item.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9d as part of the Semantic Memory Deduplication feature.
/// </para>
/// <para>
/// This record is passed to <see cref="IDeduplicationService.ProcessManualDecisionAsync"/>
/// to resolve a pending review. Based on the <see cref="Decision"/> type:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="ManualDecisionType.Merge"/>: Requires <see cref="TargetCanonicalId"/>.</description></item>
///   <item><description><see cref="ManualDecisionType.Link"/>: Requires <see cref="TargetCanonicalId"/>.</description></item>
///   <item><description><see cref="ManualDecisionType.KeepSeparate"/>: <see cref="TargetCanonicalId"/> is ignored.</description></item>
///   <item><description><see cref="ManualDecisionType.FlagContradiction"/>: May include <see cref="TargetCanonicalId"/> for context.</description></item>
///   <item><description><see cref="ManualDecisionType.Delete"/>: All other fields are optional.</description></item>
/// </list>
/// </remarks>
/// <param name="ReviewId">
/// The unique identifier of the pending review being resolved.
/// Retrieved from <see cref="PendingReview.ReviewId"/>.
/// </param>
/// <param name="Decision">
/// The type of decision being made. See <see cref="ManualDecisionType"/> for options.
/// </param>
/// <param name="TargetCanonicalId">
/// The canonical record to merge/link into, if applicable.
/// Required for <see cref="ManualDecisionType.Merge"/> and <see cref="ManualDecisionType.Link"/>.
/// Null for <see cref="ManualDecisionType.KeepSeparate"/> and <see cref="ManualDecisionType.Delete"/>.
/// </param>
/// <param name="Notes">
/// Optional notes explaining the decision.
/// Stored in the review record for audit purposes.
/// </param>
public record ManualMergeDecision(
    Guid ReviewId,
    ManualDecisionType Decision,
    Guid? TargetCanonicalId,
    string? Notes)
{
    /// <summary>
    /// Creates a merge decision targeting a specific canonical.
    /// </summary>
    /// <param name="reviewId">The review being resolved.</param>
    /// <param name="targetCanonicalId">The canonical to merge into.</param>
    /// <param name="notes">Optional explanation.</param>
    /// <returns>A merge decision.</returns>
    public static ManualMergeDecision CreateMerge(Guid reviewId, Guid targetCanonicalId, string? notes = null)
        => new(reviewId, ManualDecisionType.Merge, targetCanonicalId, notes);

    /// <summary>
    /// Creates a keep-separate decision.
    /// </summary>
    /// <param name="reviewId">The review being resolved.</param>
    /// <param name="notes">Optional explanation.</param>
    /// <returns>A keep-separate decision.</returns>
    public static ManualMergeDecision KeepSeparate(Guid reviewId, string? notes = null)
        => new(reviewId, ManualDecisionType.KeepSeparate, null, notes);

    /// <summary>
    /// Creates a delete decision.
    /// </summary>
    /// <param name="reviewId">The review being resolved.</param>
    /// <param name="notes">Optional explanation.</param>
    /// <returns>A delete decision.</returns>
    public static ManualMergeDecision CreateDelete(Guid reviewId, string? notes = null)
        => new(reviewId, ManualDecisionType.Delete, null, notes);
}
