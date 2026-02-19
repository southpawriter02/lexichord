// =============================================================================
// File: FlagStatus.cs
// Project: Lexichord.Abstractions
// Description: Status states for document flags.
// =============================================================================
// LOGIC: Tracks the lifecycle of a document flag from creation through
//   resolution, enabling workflow management and filtering.
//
// v0.7.6g: Graph-to-Doc Sync (CKVS Phase 4c)
// Dependencies: None
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc;

/// <summary>
/// Current status of a document flag.
/// </summary>
/// <remarks>
/// <para>
/// Flags progress through a lifecycle:
/// </para>
/// <list type="number">
///   <item><b>Pending:</b> Newly created, awaiting review.</item>
///   <item><b>Acknowledged:</b> User has seen it but not yet resolved.</item>
///   <item><b>Resolved/Dismissed:</b> Terminal states after user action.</item>
///   <item><b>Escalated:</b> Requires higher-level review.</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.6g as part of the Graph-to-Doc Sync module.
/// </para>
/// </remarks>
public enum FlagStatus
{
    /// <summary>
    /// Flag is pending review.
    /// </summary>
    /// <remarks>
    /// LOGIC: Initial state when flag is created. User has not yet
    /// acknowledged or acted on the flag.
    /// </remarks>
    Pending = 0,

    /// <summary>
    /// Flag has been acknowledged but not yet resolved.
    /// </summary>
    /// <remarks>
    /// LOGIC: User has seen the flag and acknowledged it, but has
    /// not taken final action. Used to track in-progress reviews.
    /// </remarks>
    Acknowledged = 1,

    /// <summary>
    /// Flag has been resolved.
    /// </summary>
    /// <remarks>
    /// LOGIC: Terminal state indicating the flag was addressed.
    /// The <see cref="FlagResolution"/> indicates how it was resolved.
    /// </remarks>
    Resolved = 2,

    /// <summary>
    /// Flag was dismissed without action.
    /// </summary>
    /// <remarks>
    /// LOGIC: Terminal state indicating the flag was intentionally
    /// dismissed. The user determined no action was needed.
    /// </remarks>
    Dismissed = 3,

    /// <summary>
    /// Flag has been escalated for higher-level review.
    /// </summary>
    /// <remarks>
    /// LOGIC: User determined the flag requires attention from
    /// a higher authority (e.g., team lead, content owner).
    /// Remains active until resolved by the escalation target.
    /// </remarks>
    Escalated = 4
}
