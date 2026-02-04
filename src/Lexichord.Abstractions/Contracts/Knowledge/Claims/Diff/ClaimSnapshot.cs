// =============================================================================
// File: ClaimSnapshot.cs
// Project: Lexichord.Abstractions
// Description: A point-in-time snapshot of claims for a document.
// =============================================================================
// LOGIC: Captures the state of claims at a specific moment for baseline
//   comparisons and historical tracking.
//
// v0.5.6i: Claim Diff Service (Knowledge Graph Change Tracking)
// Dependencies: Claim (v0.5.6e)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Claims.Diff;

/// <summary>
/// A point-in-time snapshot of claims for a document.
/// </summary>
/// <remarks>
/// <para>
/// Snapshots enable baseline comparisons by preserving the claim state at a
/// specific moment. Use cases include:
/// </para>
/// <list type="bullet">
///   <item><b>Release tracking:</b> Snapshot before each document version.</item>
///   <item><b>Review checkpoints:</b> Snapshot after human review.</item>
///   <item><b>Rollback comparison:</b> Compare current state to saved baseline.</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.5.6i as part of the Claim Diff Service.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a baseline snapshot
/// var snapshot = await diffService.CreateSnapshotAsync(documentId, "v1.0-release");
/// 
/// // Later, compare current state to baseline
/// var changes = await diffService.DiffFromBaselineAsync(documentId, snapshot.Id);
/// </code>
/// </example>
public record ClaimSnapshot
{
    /// <summary>
    /// Unique identifier for this snapshot.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Document this snapshot is for.
    /// </summary>
    public required Guid DocumentId { get; init; }

    /// <summary>
    /// Optional human-readable label.
    /// </summary>
    /// <value>
    /// A descriptive label like "v1.0-release" or "pre-migration".
    /// </value>
    public string? Label { get; init; }

    /// <summary>
    /// When the snapshot was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// User who created the snapshot (if applicable).
    /// </summary>
    public string? CreatedBy { get; init; }

    /// <summary>
    /// IDs of claims included in this snapshot.
    /// </summary>
    /// <value>
    /// The claim IDs at the time of snapshot creation.
    /// </value>
    /// <remarks>
    /// LOGIC: Storing IDs instead of full claims reduces storage and allows
    /// versioned claim retrieval if claims track version history.
    /// </remarks>
    public required IReadOnlyList<Guid> ClaimIds { get; init; }

    /// <summary>
    /// Full claims if included in the snapshot.
    /// </summary>
    /// <value>
    /// The complete claim records. May be null if only IDs are stored.
    /// </value>
    /// <remarks>
    /// Optional for storage optimization. Can be populated on retrieval.
    /// </remarks>
    public IReadOnlyList<Claim>? Claims { get; init; }

    /// <summary>
    /// Number of claims in this snapshot.
    /// </summary>
    public int ClaimCount => ClaimIds.Count;

    /// <summary>
    /// Version of the document at snapshot time (if tracked).
    /// </summary>
    public int? DocumentVersion { get; init; }

    /// <summary>
    /// Optional notes about why this snapshot was created.
    /// </summary>
    public string? Notes { get; init; }
}
