// =============================================================================
// File: ConflictSeverity.cs
// Project: Lexichord.Abstractions
// Description: Defines severity levels for synchronization conflicts.
// =============================================================================
// LOGIC: Not all conflicts are equal. Some can be auto-resolved with
//   sensible defaults, while others require explicit user intervention.
//   Severity helps prioritize conflict resolution.
//
// v0.7.6e: Sync Service Core (CKVS Phase 4c)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync;

/// <summary>
/// Severity level of a synchronization conflict.
/// </summary>
/// <remarks>
/// <para>
/// Indicates how urgently a conflict needs attention:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="Low"/>: Can be auto-resolved with defaults.</description></item>
///   <item><description><see cref="Medium"/>: Should be reviewed but has sensible default.</description></item>
///   <item><description><see cref="High"/>: Requires manual intervention.</description></item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.6e as part of the Sync Service Core.
/// </para>
/// </remarks>
public enum ConflictSeverity
{
    /// <summary>
    /// Conflict can be safely auto-resolved with defaults.
    /// </summary>
    /// <remarks>
    /// LOGIC: Minor discrepancy that has an obvious resolution. For example,
    /// whitespace differences or formatting changes. Auto-resolved when
    /// <see cref="SyncContext.AutoResolveConflicts"/> is true.
    /// </remarks>
    Low = 0,

    /// <summary>
    /// Conflict has a default resolution but should be reviewed.
    /// </summary>
    /// <remarks>
    /// LOGIC: Meaningful difference that can be auto-resolved but the user
    /// should verify the result. For example, minor value changes or
    /// property updates. Shows as a warning in the UI.
    /// </remarks>
    Medium = 1,

    /// <summary>
    /// Conflict requires explicit manual intervention.
    /// </summary>
    /// <remarks>
    /// LOGIC: Significant conflict that cannot be safely auto-resolved.
    /// For example, deletion vs. modification, or contradictory values.
    /// Blocks sync until user chooses a resolution strategy.
    /// </remarks>
    High = 2
}
