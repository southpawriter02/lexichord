// =============================================================================
// File: ContradictionStatus.cs
// Project: Lexichord.Abstractions
// Description: Enumeration of contradiction lifecycle states.
// =============================================================================
// VERSION: v0.5.9e (Contradiction Detection & Resolution)
// LOGIC: Defines the lifecycle states a contradiction can be in, from initial
//   detection through resolution or dismissal. Status transitions are tracked
//   for audit purposes.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Defines the lifecycle states for a detected contradiction.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9e as part of the Semantic Memory Deduplication feature.
/// </para>
/// <para>
/// Contradictions progress through a typical workflow:
/// </para>
/// <list type="number">
///   <item><description><see cref="Pending"/> → Initial state upon detection.</description></item>
///   <item><description><see cref="UnderReview"/> → Admin has opened the contradiction for review.</description></item>
///   <item><description><see cref="Resolved"/> → Resolution decision has been applied.</description></item>
/// </list>
/// <para>
/// Alternative terminal states:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="Dismissed"/> → False positive, no action needed.</description></item>
///   <item><description><see cref="AutoResolved"/> → System resolved due to document update/deletion.</description></item>
/// </list>
/// </remarks>
public enum ContradictionStatus
{
    /// <summary>
    /// Contradiction is pending review.
    /// </summary>
    /// <remarks>
    /// Initial state when a contradiction is first detected. The contradiction
    /// remains in the queue until an admin opens it for review.
    /// </remarks>
    Pending = 0,

    /// <summary>
    /// Contradiction is currently under admin review.
    /// </summary>
    /// <remarks>
    /// An admin has opened this contradiction and is evaluating the conflicting
    /// content. This state prevents multiple admins from working on the same item.
    /// </remarks>
    UnderReview = 1,

    /// <summary>
    /// Contradiction has been resolved by an admin.
    /// </summary>
    /// <remarks>
    /// A resolution decision has been applied. The <see cref="ContradictionResolution"/>
    /// record contains details about the action taken (keep older, keep newer,
    /// keep both, create synthesis).
    /// </remarks>
    Resolved = 2,

    /// <summary>
    /// Contradiction was dismissed as a false positive.
    /// </summary>
    /// <remarks>
    /// The admin determined that the detected contradiction was a false positive
    /// (e.g., different contexts, intentional differences). No action is needed
    /// and the contradiction is removed from the queue.
    /// </remarks>
    Dismissed = 3,

    /// <summary>
    /// Contradiction was automatically resolved by the system.
    /// </summary>
    /// <remarks>
    /// The system automatically resolved this contradiction, typically because
    /// one of the conflicting documents was updated or deleted, making the
    /// contradiction no longer applicable.
    /// </remarks>
    AutoResolved = 4
}
