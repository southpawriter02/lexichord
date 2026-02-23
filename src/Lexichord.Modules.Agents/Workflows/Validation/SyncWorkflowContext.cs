// -----------------------------------------------------------------------
// <copyright file="SyncWorkflowContext.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Workflow-level context for sync operations. Named SyncWorkflowContext
//   (not SyncContext) to avoid collision with the existing SyncContext record
//   in Lexichord.Abstractions.Contracts.Knowledge.Sync. Contains document
//   identity, user/workspace context, and sync-specific configuration.
//
// v0.7.7g: Sync Step Type (CKVS Phase 4d)
// Dependencies: ConflictStrategy (v0.7.7g)
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Workflows.Validation;

/// <summary>
/// Workflow-level context for sync operations.
/// </summary>
/// <remarks>
/// <para>
/// Provides the execution environment for <see cref="ISyncWorkflowStep"/>
/// instances. Contains document identity, user/workspace context, and
/// sync-specific configuration. This is distinct from the infrastructure-level
/// <see cref="Abstractions.Contracts.Knowledge.Sync.SyncContext"/> which is
/// used internally by the sync step to delegate to <see cref="ISyncService"/>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7g as part of Sync Step Type (CKVS Phase 4d).
/// </para>
/// </remarks>
public record SyncWorkflowContext
{
    /// <summary>
    /// Gets the workspace identifier.
    /// </summary>
    public required Guid WorkspaceId { get; init; }

    /// <summary>
    /// Gets the identifier of the document being synced.
    /// </summary>
    public required string DocumentId { get; init; }

    /// <summary>
    /// Gets the content of the document being synced.
    /// </summary>
    public required string DocumentContent { get; init; }

    /// <summary>
    /// Gets the optional user identifier who initiated the sync.
    /// </summary>
    public Guid? UserId { get; init; }

    /// <summary>
    /// Gets the optional workflow identifier executing this sync.
    /// </summary>
    public Guid? WorkflowId { get; init; }

    /// <summary>
    /// Gets whether to force a full sync even if no changes are detected.
    /// </summary>
    /// <remarks>
    /// Default is <c>false</c>. When <c>true</c>, bypasses change detection
    /// and re-syncs all entities from the document.
    /// </remarks>
    public bool ForceFull { get; init; } = false;

    /// <summary>
    /// Gets the conflict resolution strategy for this sync.
    /// </summary>
    public ConflictStrategy ConflictStrategy { get; init; } = ConflictStrategy.PreferNewer;

    /// <summary>
    /// Gets optional metadata context for the sync operation.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}
