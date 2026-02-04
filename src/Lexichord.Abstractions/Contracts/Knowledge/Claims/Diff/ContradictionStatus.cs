// =============================================================================
// File: ContradictionStatus.cs
// Project: Lexichord.Abstractions
// Description: Status of a detected contradiction.
// =============================================================================
// LOGIC: Tracks the resolution status of contradictions for review workflows.
//
// v0.5.6i: Claim Diff Service (Knowledge Graph Change Tracking)
// Dependencies: None (pure enum)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Claims.Diff;

/// <summary>
/// Status of a detected contradiction.
/// </summary>
/// <remarks>
/// <para>
/// Contradictions move through a resolution workflow:
/// </para>
/// <list type="bullet">
///   <item><b>Open:</b> Newly detected, awaiting review.</item>
///   <item><b>UnderReview:</b> Being investigated by a user.</item>
///   <item><b>Resolved:</b> User has reconciled the claims.</item>
///   <item><b>Ignored:</b> User marked as intentional/acceptable.</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.5.6i as part of the Claim Diff Service.
/// </para>
/// </remarks>
public enum ContradictionStatus
{
    /// <summary>
    /// Contradiction is newly detected and awaiting review.
    /// </summary>
    Open = 0,

    /// <summary>
    /// Contradiction is being reviewed by a user.
    /// </summary>
    UnderReview = 1,

    /// <summary>
    /// Contradiction has been resolved (claims reconciled).
    /// </summary>
    Resolved = 2,

    /// <summary>
    /// Contradiction has been intentionally ignored.
    /// </summary>
    /// <remarks>
    /// Used when the contradiction is acceptable (e.g., version-specific differences).
    /// </remarks>
    Ignored = 3
}
