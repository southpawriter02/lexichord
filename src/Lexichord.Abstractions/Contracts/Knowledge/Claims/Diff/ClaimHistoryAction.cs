// =============================================================================
// File: ClaimHistoryAction.cs
// Project: Lexichord.Abstractions
// Description: Actions tracked in claim history.
// =============================================================================
// LOGIC: Enumerates the types of actions that are recorded in claim history
//   for audit trail purposes.
//
// v0.5.6i: Claim Diff Service (Knowledge Graph Change Tracking)
// Dependencies: None (pure enum)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Claims.Diff;

/// <summary>
/// Actions tracked in claim history.
/// </summary>
/// <remarks>
/// <para>
/// History entries track operations performed on claims:
/// </para>
/// <list type="bullet">
///   <item><b>CRUD:</b> Created, Updated, Deleted — basic operations.</item>
///   <item><b>Review:</b> Validated, Reviewed — human verification.</item>
///   <item><b>Bulk:</b> BulkExtraction, SnapshotCreated — batch operations.</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.5.6i as part of the Claim Diff Service.
/// </para>
/// </remarks>
public enum ClaimHistoryAction
{
    /// <summary>
    /// Claim was created (initial extraction).
    /// </summary>
    Created = 0,

    /// <summary>
    /// Claim was updated (re-extraction or manual edit).
    /// </summary>
    Updated = 1,

    /// <summary>
    /// Claim was deleted (soft delete).
    /// </summary>
    Deleted = 2,

    /// <summary>
    /// Claim was validated against axioms.
    /// </summary>
    Validated = 3,

    /// <summary>
    /// Claim was reviewed by a human.
    /// </summary>
    Reviewed = 4,

    /// <summary>
    /// Multiple claims were extracted in a batch operation.
    /// </summary>
    BulkExtraction = 5,

    /// <summary>
    /// A baseline snapshot was created.
    /// </summary>
    SnapshotCreated = 6
}
