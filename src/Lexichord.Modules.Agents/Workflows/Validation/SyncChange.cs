// -----------------------------------------------------------------------
// <copyright file="SyncChange.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Records representing individual changes made during synchronization
//   and the type of change. Tracked in SyncStepResult.Changes for audit
//   trail and UI display purposes.
//
// v0.7.7g: Sync Step Type (CKVS Phase 4d)
// Dependencies: SyncDirection (v0.7.7g)
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts.Knowledge.Sync;

namespace Lexichord.Modules.Agents.Workflows.Validation;

/// <summary>
/// A single change made during synchronization.
/// </summary>
/// <remarks>
/// <para>
/// Tracks individual entity or property modifications performed during
/// a sync operation. Each change records what was modified, the before
/// and after values, the direction of the change, and a timestamp.
/// </para>
/// <para>
/// Changes are collected in <see cref="SyncStepResult.Changes"/> for:
/// </para>
/// <list type="bullet">
///   <item><description>Audit trail logging and compliance.</description></item>
///   <item><description>UI display of sync outcomes.</description></item>
///   <item><description>Undo support for reversible sync operations.</description></item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.7g as part of Sync Step Type (CKVS Phase 4d).
/// </para>
/// </remarks>
public record SyncChange
{
    /// <summary>
    /// Gets the type of change that was made.
    /// </summary>
    /// <remarks>
    /// Indicates whether the entity was created, updated, deleted, merged, or linked.
    /// </remarks>
    public SyncChangeType ChangeType { get; init; }

    /// <summary>
    /// Gets the identifier of the affected entity.
    /// </summary>
    public Guid EntityId { get; init; }

    /// <summary>
    /// Gets the type of the affected entity (e.g., "KnowledgeEntity", "Claim").
    /// </summary>
    public string? EntityType { get; init; }

    /// <summary>
    /// Gets the name of the property that changed (if applicable).
    /// </summary>
    /// <remarks>
    /// Null for entity-level changes (create, delete). Populated for
    /// property-level updates (e.g., "Description", "Confidence").
    /// </remarks>
    public string? Property { get; init; }

    /// <summary>
    /// Gets the value before the change.
    /// </summary>
    /// <remarks>
    /// Null for <see cref="SyncChangeType.Created"/> changes.
    /// </remarks>
    public object? PreviousValue { get; init; }

    /// <summary>
    /// Gets the value after the change.
    /// </summary>
    /// <remarks>
    /// Null for <see cref="SyncChangeType.Deleted"/> changes.
    /// </remarks>
    public object? NewValue { get; init; }

    /// <summary>
    /// Gets the direction of the change.
    /// </summary>
    public SyncDirection Direction { get; init; }

    /// <summary>
    /// Gets the timestamp when the change was applied.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Type of change made during synchronization.
/// </summary>
/// <remarks>
/// <para>
/// Categorizes the nature of a <see cref="SyncChange"/> for filtering,
/// grouping, and display in the sync results UI.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7g as part of Sync Step Type (CKVS Phase 4d).
/// </para>
/// </remarks>
public enum SyncChangeType
{
    /// <summary>A new entity or claim was created in the target.</summary>
    Created,

    /// <summary>An existing entity or claim was updated.</summary>
    Updated,

    /// <summary>An entity or claim was removed from the target.</summary>
    Deleted,

    /// <summary>Two conflicting versions were merged.</summary>
    Merged,

    /// <summary>A new relationship was linked between entities.</summary>
    Linked
}
