// =============================================================================
// File: GraphToDocSyncResult.cs
// Project: Lexichord.Abstractions
// Description: Result record for graph-to-document sync operations.
// =============================================================================
// LOGIC: Comprehensive result containing operation status, affected documents,
//   created flags, notification counts, timing, and error information.
//
// v0.7.6g: Graph-to-Doc Sync (CKVS Phase 4c)
// Dependencies: SyncOperationStatus (v0.7.6e), GraphChange (v0.7.6e),
//               AffectedDocument, DocumentFlag
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc;

/// <summary>
/// Result of a graph-to-document synchronization operation.
/// </summary>
/// <remarks>
/// <para>
/// Contains comprehensive information about the sync outcome:
/// </para>
/// <list type="bullet">
///   <item><b>Status:</b> Overall operation result (see <see cref="SyncOperationStatus"/>).</item>
///   <item><b>AffectedDocuments:</b> Documents impacted by the graph change.</item>
///   <item><b>FlagsCreated:</b> Document flags created for review.</item>
///   <item><b>Notifications:</b> Count of documents notified.</item>
///   <item><b>TriggeringChange:</b> The graph change that initiated sync.</item>
///   <item><b>Timing:</b> Operation duration.</item>
/// </list>
/// <para>
/// <b>Usage:</b>
/// <code>
/// var result = await provider.OnGraphChangeAsync(change, options);
/// if (result.Status == SyncOperationStatus.Success)
/// {
///     Console.WriteLine($"Found {result.AffectedDocuments.Count} affected documents");
///     Console.WriteLine($"Created {result.FlagsCreated.Count} flags");
///     Console.WriteLine($"Notified {result.TotalDocumentsNotified} document owners");
///     Console.WriteLine($"Completed in {result.Duration.TotalMilliseconds}ms");
/// }
/// </code>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6g as part of the Graph-to-Doc Sync module.
/// </para>
/// </remarks>
public record GraphToDocSyncResult
{
    /// <summary>
    /// Status of the sync operation.
    /// </summary>
    /// <value>The overall outcome of the synchronization.</value>
    /// <remarks>
    /// LOGIC: Determines how the caller should handle the result:
    /// - Success: Affected documents identified and flagged.
    /// - NoChanges: No documents reference the changed entity.
    /// - Failed: Operation encountered an error.
    /// </remarks>
    public required SyncOperationStatus Status { get; init; }

    /// <summary>
    /// Documents affected by the graph change.
    /// </summary>
    /// <value>
    /// A read-only list of documents that reference the changed entity.
    /// Empty if no documents are affected.
    /// </value>
    /// <remarks>
    /// LOGIC: Populated by <see cref="IAffectedDocumentDetector"/>.
    /// May be truncated to <see cref="GraphToDocSyncOptions.MaxDocumentsPerChange"/>.
    /// </remarks>
    public IReadOnlyList<AffectedDocument> AffectedDocuments { get; init; } = [];

    /// <summary>
    /// Flags created for affected documents.
    /// </summary>
    /// <value>
    /// A read-only list of flags created during this sync operation.
    /// Empty if <see cref="GraphToDocSyncOptions.AutoFlagDocuments"/> is false.
    /// </value>
    /// <remarks>
    /// LOGIC: One flag per affected document. Flags include reason,
    /// priority, and optional suggested actions.
    /// </remarks>
    public IReadOnlyList<DocumentFlag> FlagsCreated { get; init; } = [];

    /// <summary>
    /// Count of documents that were notified.
    /// </summary>
    /// <value>Number of notification sent.</value>
    /// <remarks>
    /// LOGIC: May be less than flags created if notifications are
    /// disabled or deduplicated within the
    /// <see cref="GraphToDocSyncOptions.DeduplicationWindow"/>.
    /// </remarks>
    public int TotalDocumentsNotified { get; init; }

    /// <summary>
    /// The graph change that triggered this sync.
    /// </summary>
    /// <value>The change event that initiated the operation.</value>
    /// <remarks>
    /// LOGIC: Links the result to its cause. Used for logging,
    /// audit trails, and understanding the context of flags.
    /// </remarks>
    public required GraphChange TriggeringChange { get; init; }

    /// <summary>
    /// Duration of the sync operation.
    /// </summary>
    /// <value>Elapsed time from start to completion of the sync.</value>
    /// <remarks>
    /// LOGIC: Measured using Stopwatch for accuracy. Used for performance
    /// monitoring, logging, and timeout enforcement.
    /// </remarks>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Error message if operation failed.
    /// </summary>
    /// <value>Error details when Status is Failed, null otherwise.</value>
    /// <remarks>
    /// LOGIC: Provides details about failure cause. Should be
    /// human-readable for logging and UI display.
    /// </remarks>
    public string? ErrorMessage { get; init; }
}
