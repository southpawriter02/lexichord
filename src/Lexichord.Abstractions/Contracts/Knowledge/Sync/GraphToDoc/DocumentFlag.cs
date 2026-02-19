// =============================================================================
// File: DocumentFlag.cs
// Project: Lexichord.Abstractions
// Description: Represents a flag on a document requiring review.
// =============================================================================
// LOGIC: Tracks document review requirements caused by graph changes,
//   enabling workflow management, notification, and audit trails.
//
// v0.7.6g: Graph-to-Doc Sync (CKVS Phase 4c)
// Dependencies: FlagReason, FlagPriority, FlagStatus, FlagResolution
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc;

/// <summary>
/// A flag indicating a document needs review due to graph changes.
/// </summary>
/// <remarks>
/// <para>
/// Document flags are created when knowledge graph changes may affect
/// document content. They track:
/// </para>
/// <list type="bullet">
///   <item><b>Cause:</b> The entity and reason that triggered the flag.</item>
///   <item><b>Priority:</b> Urgency level for review.</item>
///   <item><b>Status:</b> Current workflow state.</item>
///   <item><b>Resolution:</b> How the flag was addressed.</item>
///   <item><b>Notification:</b> Whether the document owner was notified.</item>
/// </list>
/// <para>
/// <b>Usage:</b>
/// <code>
/// var pendingFlags = await provider.GetPendingFlagsAsync(documentId);
/// foreach (var flag in pendingFlags.OrderByDescending(f => f.Priority))
/// {
///     Console.WriteLine($"[{flag.Priority}] {flag.Reason}: {flag.Description}");
///     Console.WriteLine($"  Created: {flag.CreatedAt}");
/// }
/// </code>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6g as part of the Graph-to-Doc Sync module.
/// </para>
/// </remarks>
public record DocumentFlag
{
    /// <summary>
    /// Unique identifier for this flag.
    /// </summary>
    /// <value>The flag's GUID.</value>
    /// <remarks>
    /// LOGIC: Primary key for flag operations. Used for resolution,
    /// updates, and audit tracking.
    /// </remarks>
    public required Guid FlagId { get; init; }

    /// <summary>
    /// Document this flag applies to.
    /// </summary>
    /// <value>The flagged document's GUID.</value>
    /// <remarks>
    /// LOGIC: Foreign key to document. A document may have multiple
    /// pending flags from different graph changes.
    /// </remarks>
    public required Guid DocumentId { get; init; }

    /// <summary>
    /// Entity whose change triggered this flag.
    /// </summary>
    /// <value>The triggering entity's GUID.</value>
    /// <remarks>
    /// LOGIC: Links flag to the specific entity change. Used for
    /// displaying change details and navigating to the graph.
    /// </remarks>
    public required Guid TriggeringEntityId { get; init; }

    /// <summary>
    /// Reason this document was flagged.
    /// </summary>
    /// <value>The category of change that caused the flag.</value>
    /// <remarks>
    /// LOGIC: Determines default priority and suggested actions.
    /// Also used for filtering and categorizing flags in UI.
    /// </remarks>
    public required FlagReason Reason { get; init; }

    /// <summary>
    /// Human-readable description of the flag.
    /// </summary>
    /// <value>Explanation of why the flag was created.</value>
    /// <remarks>
    /// LOGIC: Provides context beyond the reason enum. May include
    /// specific entity names, old/new values, etc.
    /// Example: "Entity 'ProductX' was renamed to 'ProductY'".
    /// </remarks>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Priority level of this flag.
    /// </summary>
    /// <value>How urgently the flag should be addressed.</value>
    /// <remarks>
    /// LOGIC: Typically derived from <see cref="Reason"/> via
    /// <see cref="GraphToDocSyncOptions.ReasonPriorities"/> but can
    /// be overridden. Used for sorting and notification timing.
    /// </remarks>
    public FlagPriority Priority { get; init; } = FlagPriority.Medium;

    /// <summary>
    /// Current status of this flag.
    /// </summary>
    /// <value>Workflow state of the flag.</value>
    /// <remarks>
    /// LOGIC: Tracks flag lifecycle from creation to resolution.
    /// Used for filtering pending/resolved flags.
    /// </remarks>
    public FlagStatus Status { get; init; } = FlagStatus.Pending;

    /// <summary>
    /// When this flag was created.
    /// </summary>
    /// <value>UTC timestamp of flag creation.</value>
    /// <remarks>
    /// LOGIC: Recorded at creation for audit trails and SLA tracking.
    /// </remarks>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// When this flag was resolved.
    /// </summary>
    /// <value>UTC timestamp of resolution, or null if unresolved.</value>
    /// <remarks>
    /// LOGIC: Set when flag transitions to Resolved or Dismissed status.
    /// Null while Pending or Acknowledged.
    /// </remarks>
    public DateTimeOffset? ResolvedAt { get; init; }

    /// <summary>
    /// User who resolved this flag.
    /// </summary>
    /// <value>User ID of resolver, or null if unresolved.</value>
    /// <remarks>
    /// LOGIC: Audit trail for resolution. Enables tracking which
    /// team members address specific types of flags.
    /// </remarks>
    public Guid? ResolvedBy { get; init; }

    /// <summary>
    /// How this flag was resolved.
    /// </summary>
    /// <value>Resolution type, or null if unresolved.</value>
    /// <remarks>
    /// LOGIC: Records the action taken. Can be analyzed to improve
    /// future suggestions based on common resolution patterns.
    /// </remarks>
    public FlagResolution? Resolution { get; init; }

    /// <summary>
    /// Whether a notification was sent for this flag.
    /// </summary>
    /// <value>True if notification sent, false otherwise.</value>
    /// <remarks>
    /// LOGIC: Tracks notification state for deduplication and
    /// ensuring users are informed of high-priority flags.
    /// </remarks>
    public bool NotificationSent { get; init; }

    /// <summary>
    /// When notification was sent.
    /// </summary>
    /// <value>UTC timestamp of notification, or null if not sent.</value>
    /// <remarks>
    /// LOGIC: Recorded when notification is dispatched. Used for
    /// deduplication within <see cref="GraphToDocSyncOptions.DeduplicationWindow"/>.
    /// </remarks>
    public DateTimeOffset? NotificationSentAt { get; init; }
}
