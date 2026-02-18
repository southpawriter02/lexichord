// =============================================================================
// File: SyncContext.cs
// Project: Lexichord.Abstractions
// Description: Context record for sync operations containing configuration.
// =============================================================================
// LOGIC: Every sync operation receives a context that configures behavior:
//   who initiated it, what document, conflict resolution preferences,
//   whether to publish events, and timeout settings.
//
// v0.7.6e: Sync Service Core (CKVS Phase 4c)
// Dependencies: ConflictResolutionStrategy (v0.7.6e), Document (v0.4.1c)
// =============================================================================

using Lexichord.Abstractions.Contracts.RAG;

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync;

/// <summary>
/// Context for synchronization operations between documents and knowledge graph.
/// </summary>
/// <remarks>
/// <para>
/// Provides configuration for a sync operation:
/// </para>
/// <list type="bullet">
///   <item><b>Identity:</b> User and workspace initiating the sync.</item>
///   <item><b>Target:</b> The document being synchronized.</item>
///   <item><b>Conflict Handling:</b> Auto-resolve preference and default strategy.</item>
///   <item><b>Events:</b> Whether to publish completion events.</item>
///   <item><b>Timeout:</b> Maximum duration for the operation.</item>
/// </list>
/// <para>
/// <b>Usage:</b>
/// <code>
/// var context = new SyncContext
/// {
///     UserId = currentUser.Id,
///     Document = document,
///     WorkspaceId = workspace.Id,
///     AutoResolveConflicts = true,
///     DefaultConflictStrategy = ConflictResolutionStrategy.Merge,
///     Timeout = TimeSpan.FromMinutes(2)
/// };
/// var result = await syncService.SyncDocumentToGraphAsync(document, context);
/// </code>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6e as part of the Sync Service Core.
/// </para>
/// </remarks>
public record SyncContext
{
    /// <summary>
    /// ID of the user initiating the sync.
    /// </summary>
    /// <value>The unique identifier of the current user.</value>
    /// <remarks>
    /// LOGIC: Used for audit logging and permission checks.
    /// The user must have write access to the workspace.
    /// </remarks>
    public required Guid UserId { get; init; }

    /// <summary>
    /// The document being synchronized.
    /// </summary>
    /// <value>The document record to sync with the graph.</value>
    /// <remarks>
    /// LOGIC: Contains the document ID, content hash, and metadata
    /// needed for extraction and conflict detection.
    /// </remarks>
    public required Document Document { get; init; }

    /// <summary>
    /// ID of the workspace (optional).
    /// </summary>
    /// <value>
    /// The workspace containing the document. Null for personal documents.
    /// </value>
    /// <remarks>
    /// LOGIC: Used to scope graph operations to the workspace.
    /// Entities are stored per-workspace in Teams tier.
    /// </remarks>
    public Guid? WorkspaceId { get; init; }

    /// <summary>
    /// Whether to automatically resolve low-severity conflicts.
    /// </summary>
    /// <value>
    /// True to auto-resolve conflicts with <see cref="ConflictSeverity.Low"/>.
    /// Defaults to true.
    /// </value>
    /// <remarks>
    /// LOGIC: When enabled, minor conflicts (e.g., whitespace, formatting)
    /// are resolved using <see cref="DefaultConflictStrategy"/> without
    /// user intervention.
    /// </remarks>
    public bool AutoResolveConflicts { get; init; } = true;

    /// <summary>
    /// Default strategy for conflict resolution.
    /// </summary>
    /// <value>
    /// The strategy to use when auto-resolving or as the default in the UI.
    /// Defaults to <see cref="ConflictResolutionStrategy.Merge"/>.
    /// </value>
    /// <remarks>
    /// LOGIC: Applied automatically for low-severity conflicts when
    /// <see cref="AutoResolveConflicts"/> is true. Also used as the
    /// pre-selected option in the conflict resolution UI.
    /// </remarks>
    public ConflictResolutionStrategy DefaultConflictStrategy { get; init; } = ConflictResolutionStrategy.Merge;

    /// <summary>
    /// Whether to publish sync completion events.
    /// </summary>
    /// <value>
    /// True to publish <see cref="SyncCompletedEvent"/> via MediatR.
    /// Defaults to true.
    /// </value>
    /// <remarks>
    /// LOGIC: Events notify other parts of the system (UI, background jobs)
    /// about sync completion. Disable for batch operations where
    /// individual events are not needed.
    /// </remarks>
    public bool PublishEvents { get; init; } = true;

    /// <summary>
    /// Timeout for the sync operation.
    /// </summary>
    /// <value>
    /// Maximum duration before the operation is cancelled.
    /// Defaults to 5 minutes.
    /// </value>
    /// <remarks>
    /// LOGIC: Protects against runaway operations. Large documents with
    /// many entities may need longer timeouts. The sync service
    /// monitors this and throws TimeoutException if exceeded.
    /// </remarks>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(5);
}
