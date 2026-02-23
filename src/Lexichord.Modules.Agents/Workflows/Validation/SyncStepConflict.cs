// -----------------------------------------------------------------------
// <copyright file="SyncStepConflict.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Records representing sync conflicts detected during a workflow
//   sync step and their resolution details. Named SyncStepConflict (not
//   SyncConflict) to avoid collision with the existing SyncConflict record
//   in Lexichord.Abstractions.Contracts.Knowledge.Sync.
//
// v0.7.7g: Sync Step Type (CKVS Phase 4d)
// Dependencies: ConflictStrategy (v0.7.7g)
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Workflows.Validation;

/// <summary>
/// A conflict detected during a sync workflow step.
/// </summary>
/// <remarks>
/// <para>
/// Captures details about a conflict between the document and knowledge
/// graph state. Each conflict identifies the conflicting entity, the
/// divergent property, both values, modification timestamps, and any
/// resolution that was applied.
/// </para>
/// <para>
/// Unresolved conflicts are collected in
/// <see cref="SyncStepResult.UnresolvedConflicts"/> for user review.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7g as part of Sync Step Type (CKVS Phase 4d).
/// </para>
/// </remarks>
public record SyncStepConflict
{
    /// <summary>
    /// Gets the identifier of the conflicting entity.
    /// </summary>
    public required Guid EntityId { get; init; }

    /// <summary>
    /// Gets the type of the conflicting entity (e.g., "KnowledgeEntity", "Claim").
    /// </summary>
    public required string EntityType { get; init; }

    /// <summary>
    /// Gets the name of the property that conflicts.
    /// </summary>
    public required string Property { get; init; }

    /// <summary>
    /// Gets the document's version of the conflicting value.
    /// </summary>
    public required object DocumentValue { get; init; }

    /// <summary>
    /// Gets the graph's version of the conflicting value.
    /// </summary>
    public required object GraphValue { get; init; }

    /// <summary>
    /// Gets when the document was last modified.
    /// </summary>
    public DateTime DocumentModifiedTime { get; init; }

    /// <summary>
    /// Gets when the graph entity was last modified.
    /// </summary>
    public DateTime GraphModifiedTime { get; init; }

    /// <summary>
    /// Gets the resolution applied to this conflict (if any).
    /// </summary>
    /// <remarks>
    /// Null when the conflict is unresolved and requires manual intervention.
    /// </remarks>
    public SyncStepConflictResolution? Resolution { get; init; }

    /// <summary>
    /// Gets optional notes about the conflict.
    /// </summary>
    public string? Notes { get; init; }
}

/// <summary>
/// Resolution details for a <see cref="SyncStepConflict"/>.
/// </summary>
/// <remarks>
/// <para>
/// Records which strategy was used to resolve a conflict, the chosen
/// value, and when the resolution was applied.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7g as part of Sync Step Type (CKVS Phase 4d).
/// </para>
/// </remarks>
public record SyncStepConflictResolution
{
    /// <summary>
    /// Gets the strategy used to resolve the conflict.
    /// </summary>
    public ConflictStrategy Strategy { get; init; }

    /// <summary>
    /// Gets the value that was chosen as the resolution.
    /// </summary>
    public required object ResolvedValue { get; init; }

    /// <summary>
    /// Gets the timestamp when the resolution was applied.
    /// </summary>
    public DateTime ResolvedAt { get; init; } = DateTime.UtcNow;
}
