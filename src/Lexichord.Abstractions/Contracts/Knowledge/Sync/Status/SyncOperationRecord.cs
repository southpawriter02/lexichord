// =============================================================================
// File: SyncOperationRecord.cs
// Project: Lexichord.Abstractions
// Description: Record representing a sync operation for tracking and metrics.
// =============================================================================
// LOGIC: Captures detailed information about each sync operation including
//   timing, affected entities, conflicts, and outcome. Used for metrics
//   calculation and operational visibility.
//
// v0.7.6i: Sync Status Tracker (CKVS Phase 4c)
// Dependencies: SyncDirection, SyncOperationStatus (v0.7.6e)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.Status;

/// <summary>
/// Record of a sync operation for tracking and metrics.
/// </summary>
/// <remarks>
/// <para>
/// Captures comprehensive information about sync operations:
/// </para>
/// <list type="bullet">
///   <item><b>Identity:</b> Operation ID and target document.</item>
///   <item><b>Direction:</b> Doc-to-Graph, Graph-to-Doc, or Bidirectional.</item>
///   <item><b>Timing:</b> Start time, completion time, and duration.</item>
///   <item><b>Impact:</b> Entities, claims, and relationships affected.</item>
///   <item><b>Conflicts:</b> Conflicts detected and resolved.</item>
///   <item><b>Outcome:</b> Status and any error information.</item>
/// </list>
/// <para>
/// <b>Usage:</b>
/// <code>
/// var record = new SyncOperationRecord
/// {
///     OperationId = Guid.NewGuid(),
///     DocumentId = documentId,
///     Direction = SyncDirection.DocumentToGraph,
///     Status = SyncOperationStatus.Success,
///     StartedAt = startTime,
///     CompletedAt = DateTimeOffset.UtcNow,
///     EntitiesAffected = 15
/// };
/// await tracker.RecordSyncOperationAsync(documentId, record);
/// </code>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6i as part of the Sync Status Tracker.
/// </para>
/// </remarks>
public record SyncOperationRecord
{
    /// <summary>
    /// Unique identifier for this operation.
    /// </summary>
    /// <value>A globally unique identifier for the operation.</value>
    /// <remarks>
    /// LOGIC: Primary key for operation lookups. Generated at operation start.
    /// Links to <see cref="SyncStatusHistory.SyncOperationId"/> for cross-reference.
    /// </remarks>
    public required Guid OperationId { get; init; }

    /// <summary>
    /// The document being synced.
    /// </summary>
    /// <value>The document ID that was synced.</value>
    /// <remarks>
    /// LOGIC: Foreign key to the document. Used for filtering operations
    /// by document when computing metrics.
    /// </remarks>
    public required Guid DocumentId { get; init; }

    /// <summary>
    /// Direction of the sync operation.
    /// </summary>
    /// <value>
    /// Whether sync was doc-to-graph, graph-to-doc, or bidirectional.
    /// </value>
    /// <remarks>
    /// LOGIC: Indicates the data flow direction:
    /// - DocumentToGraph: Extracting entities from document to graph
    /// - GraphToDocument: Propagating graph changes to document
    /// - Bidirectional: Full two-way sync
    /// </remarks>
    public required SyncDirection Direction { get; init; }

    /// <summary>
    /// Outcome status of the operation.
    /// </summary>
    /// <value>Whether the operation succeeded, failed, or had conflicts.</value>
    /// <remarks>
    /// LOGIC: Final status of the operation:
    /// - Success: Completed without issues
    /// - SuccessWithConflicts: Completed but conflicts were detected
    /// - PartialSuccess: Some items synced, others failed
    /// - Failed: Operation failed entirely
    /// - NoChanges: No changes were needed
    /// </remarks>
    public required SyncOperationStatus Status { get; init; }

    /// <summary>
    /// When the operation started.
    /// </summary>
    /// <value>UTC timestamp when sync began.</value>
    /// <remarks>
    /// LOGIC: Marks the start of timing measurement.
    /// Used with CompletedAt to calculate Duration.
    /// </remarks>
    public required DateTimeOffset StartedAt { get; init; }

    /// <summary>
    /// When the operation completed.
    /// </summary>
    /// <value>
    /// UTC timestamp when sync finished, or null if still in progress.
    /// </value>
    /// <remarks>
    /// LOGIC: Marks the end of the operation.
    /// Null indicates the operation is still running.
    /// </remarks>
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>
    /// Total duration of the operation.
    /// </summary>
    /// <value>
    /// Time elapsed from start to completion, or null if not completed.
    /// </value>
    /// <remarks>
    /// LOGIC: Calculated as CompletedAt - StartedAt when available.
    /// Used for performance metrics and SLA monitoring.
    /// </remarks>
    public TimeSpan? Duration { get; init; }

    /// <summary>
    /// User who initiated the operation.
    /// </summary>
    /// <value>
    /// The user ID who triggered the sync, or null for system-initiated operations.
    /// </value>
    /// <remarks>
    /// LOGIC: Attribution for audit purposes. Null indicates automatic
    /// operations (background sync, scheduled jobs, webhooks, etc.).
    /// </remarks>
    public Guid? InitiatedBy { get; init; }

    /// <summary>
    /// Number of entities affected by the sync.
    /// </summary>
    /// <value>Count of knowledge entities created, updated, or deleted.</value>
    /// <remarks>
    /// LOGIC: Measures the entity impact of the sync.
    /// Used for metrics like average entities per sync.
    /// </remarks>
    public int EntitiesAffected { get; init; }

    /// <summary>
    /// Number of claims affected by the sync.
    /// </summary>
    /// <value>Count of claims created, updated, or deleted.</value>
    /// <remarks>
    /// LOGIC: Measures the claim impact of the sync.
    /// Used for metrics like average claims per sync.
    /// </remarks>
    public int ClaimsAffected { get; init; }

    /// <summary>
    /// Number of relationships affected by the sync.
    /// </summary>
    /// <value>Count of relationships created, updated, or deleted.</value>
    /// <remarks>
    /// LOGIC: Measures the relationship impact of the sync.
    /// Relationships connect entities in the knowledge graph.
    /// </remarks>
    public int RelationshipsAffected { get; init; }

    /// <summary>
    /// Number of conflicts detected during sync.
    /// </summary>
    /// <value>Count of conflicts found between document and graph.</value>
    /// <remarks>
    /// LOGIC: Indicates sync complexity and data divergence.
    /// High conflict counts may indicate stale documents or rapid graph changes.
    /// </remarks>
    public int ConflictsDetected { get; init; }

    /// <summary>
    /// Number of conflicts resolved during sync.
    /// </summary>
    /// <value>Count of conflicts that were automatically or manually resolved.</value>
    /// <remarks>
    /// LOGIC: ConflictsDetected - ConflictsResolved = remaining unresolved conflicts.
    /// Used to calculate conflict resolution efficiency.
    /// </remarks>
    public int ConflictsResolved { get; init; }

    /// <summary>
    /// Error message if the operation failed.
    /// </summary>
    /// <value>
    /// Human-readable error description, or null if successful.
    /// </value>
    /// <remarks>
    /// LOGIC: Provides debugging information for failed syncs.
    /// Should not contain sensitive information.
    /// </remarks>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Error code if the operation failed.
    /// </summary>
    /// <value>
    /// Machine-readable error code for categorization, or null if successful.
    /// </value>
    /// <remarks>
    /// LOGIC: Enables programmatic error handling and metrics grouping.
    /// Examples: "SYNC-001", "CONFLICT-002", "TIMEOUT-003".
    /// </remarks>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Additional operation metadata.
    /// </summary>
    /// <value>
    /// Key-value pairs with additional context. Defaults to empty dictionary.
    /// </value>
    /// <remarks>
    /// LOGIC: Extensible metadata storage for operation-specific data:
    /// - "extractorUsed": Which entity extractor was used
    /// - "graphVersion": Graph schema version at time of sync
    /// - "retryCount": Number of retry attempts
    /// - "batchId": For batched operations
    /// </remarks>
    public Dictionary<string, object> Metadata { get; init; } = new();
}
