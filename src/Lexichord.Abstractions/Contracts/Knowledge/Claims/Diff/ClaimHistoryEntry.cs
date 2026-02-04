// =============================================================================
// File: ClaimHistoryEntry.cs
// Project: Lexichord.Abstractions
// Description: An entry in the claim change history.
// =============================================================================
// LOGIC: Records actions taken on claims for audit trail purposes.
//   Enables tracking who changed what and when.
//
// v0.5.6i: Claim Diff Service (Knowledge Graph Change Tracking)
// Dependencies: ClaimHistoryAction (v0.5.6i)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Claims.Diff;

/// <summary>
/// An entry in the claim change history.
/// </summary>
/// <remarks>
/// <para>
/// History entries form an audit trail of claim operations:
/// </para>
/// <list type="bullet">
///   <item><b>CRUD:</b> Create, update, delete operations.</item>
///   <item><b>Review:</b> Validation and human review events.</item>
///   <item><b>Batch:</b> Bulk extraction and snapshot events.</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.5.6i as part of the Claim Diff Service.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var history = await diffService.GetHistoryAsync(documentId, limit: 20);
/// foreach (var entry in history)
/// {
///     Console.WriteLine($"[{entry.Timestamp:g}] {entry.Action}: {entry.Description}");
/// }
/// </code>
/// </example>
public record ClaimHistoryEntry
{
    /// <summary>
    /// Unique identifier for this history entry.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Document this history entry relates to.
    /// </summary>
    public required Guid DocumentId { get; init; }

    /// <summary>
    /// Specific claim this entry relates to (if applicable).
    /// </summary>
    /// <value>
    /// The claim ID for single-claim operations, null for bulk operations.
    /// </value>
    public Guid? ClaimId { get; init; }

    /// <summary>
    /// Type of action that was performed.
    /// </summary>
    public required ClaimHistoryAction Action { get; init; }

    /// <summary>
    /// When the action occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Who performed the action (if applicable).
    /// </summary>
    /// <value>
    /// User identifier or "system" for automated operations.
    /// </value>
    public string? Actor { get; init; }

    /// <summary>
    /// Human-readable description of the action.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Number of claims affected (for bulk operations).
    /// </summary>
    /// <value>
    /// Count of affected claims. Defaults to 1 for single-claim operations.
    /// </value>
    public int AffectedCount { get; init; } = 1;

    /// <summary>
    /// Related snapshot ID (for snapshot operations).
    /// </summary>
    public Guid? SnapshotId { get; init; }

    /// <summary>
    /// Additional metadata about the action.
    /// </summary>
    /// <value>
    /// Extra context like extraction method, version info, etc.
    /// </value>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}
