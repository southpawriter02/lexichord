// =============================================================================
// File: FlagPriority.cs
// Project: Lexichord.Abstractions
// Description: Priority levels for document flags.
// =============================================================================
// LOGIC: Categorizes flag urgency to enable appropriate workflow routing,
//   notification timing, and UI presentation.
//
// v0.7.6g: Graph-to-Doc Sync (CKVS Phase 4c)
// Dependencies: None
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc;

/// <summary>
/// Priority level for a document flag.
/// </summary>
/// <remarks>
/// <para>
/// Determines how urgently a flag should be addressed:
/// </para>
/// <list type="bullet">
///   <item><b>Low:</b> Informational, review at convenience.</item>
///   <item><b>Medium:</b> Should be reviewed soon.</item>
///   <item><b>High:</b> Needs prompt attention.</item>
///   <item><b>Critical:</b> Requires immediate action.</item>
/// </list>
/// <para>
/// Priority is typically derived from <see cref="FlagReason"/> but can be
/// overridden via <see cref="GraphToDocSyncOptions.ReasonPriorities"/>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6g as part of the Graph-to-Doc Sync module.
/// </para>
/// </remarks>
public enum FlagPriority
{
    /// <summary>
    /// Informational flag that can be reviewed at the user's convenience.
    /// </summary>
    /// <remarks>
    /// LOGIC: Low-impact changes that may enhance content but are not
    /// urgent. Notifications may be batched or delayed.
    /// </remarks>
    Low = 0,

    /// <summary>
    /// Flag that should be reviewed in a reasonable timeframe.
    /// </summary>
    /// <remarks>
    /// LOGIC: Default priority for most flags. Changes that are
    /// important but not time-sensitive.
    /// </remarks>
    Medium = 1,

    /// <summary>
    /// Flag that needs prompt attention.
    /// </summary>
    /// <remarks>
    /// LOGIC: Significant changes that may affect content accuracy.
    /// Notifications are sent promptly.
    /// </remarks>
    High = 2,

    /// <summary>
    /// Flag requiring immediate action.
    /// </summary>
    /// <remarks>
    /// LOGIC: Critical issues like entity deletion or severe conflicts.
    /// Content may be incorrect or broken. Immediate notification sent.
    /// </remarks>
    Critical = 3
}
