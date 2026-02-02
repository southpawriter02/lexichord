// =============================================================================
// File: EntityChangeRecord.cs
// Project: Lexichord.Abstractions
// Description: Audit trail record for entity changes.
// =============================================================================
// LOGIC: Defines an immutable record for tracking entity modifications over time.
//   Each change is recorded with before/after state snapshots for auditability.
//
// v0.4.7g: Entity CRUD Operations
// Dependencies: None
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge;

/// <summary>
/// Audit trail record for an entity change operation.
/// </summary>
/// <remarks>
/// <para>
/// Captures a snapshot of entity state before and after modification,
/// enabling change history review and potential rollback operations.
/// </para>
/// <para>
/// <b>Storage:</b> Change records are persisted to PostgreSQL (not Neo4j)
/// to maintain separation between operational graph data and audit history.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7g as part of Entity CRUD Operations.
/// </para>
/// </remarks>
public sealed record EntityChangeRecord
{
    /// <summary>
    /// Gets the unique identifier for this change record.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets the ID of the entity that was modified.
    /// </summary>
    public required Guid EntityId { get; init; }

    /// <summary>
    /// Gets the type of operation performed (Created, Updated, Merged, Deleted).
    /// </summary>
    public required string Operation { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the change occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the JSON-serialized entity state before the change, or null for creation.
    /// </summary>
    public string? PreviousState { get; init; }

    /// <summary>
    /// Gets the JSON-serialized entity state after the change, or null for deletion.
    /// </summary>
    public string? NewState { get; init; }

    /// <summary>
    /// Gets the optional reason provided for the change.
    /// </summary>
    public string? ChangeReason { get; init; }
}
