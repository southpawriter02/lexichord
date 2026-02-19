// =============================================================================
// File: GraphChangeSubscription.cs
// Project: Lexichord.Abstractions
// Description: Subscription for graph change notifications on a document.
// =============================================================================
// LOGIC: Enables documents to subscribe to specific entity changes,
//   providing targeted notifications rather than blanket coverage.
//
// v0.7.6g: Graph-to-Doc Sync (CKVS Phase 4c)
// Dependencies: ChangeType (v0.7.6e)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc;

/// <summary>
/// Subscription for graph change notifications on a document.
/// </summary>
/// <remarks>
/// <para>
/// Allows documents to subscribe to specific graph changes:
/// </para>
/// <list type="bullet">
///   <item><b>EntityIds:</b> Specific entities to monitor.</item>
///   <item><b>ChangeTypes:</b> Types of changes to watch for.</item>
///   <item><b>NotifyUser:</b> Who to notify on changes.</item>
///   <item><b>IsActive:</b> Whether subscription is currently active.</item>
/// </list>
/// <para>
/// <b>Usage:</b>
/// <code>
/// var subscription = new GraphChangeSubscription
/// {
///     DocumentId = documentId,
///     EntityIds = [productEntityId, apiEntityId],
///     ChangeTypes = [ChangeType.EntityUpdated, ChangeType.EntityDeleted],
///     NotifyUser = currentUserId
/// };
/// await provider.SubscribeToGraphChangesAsync(documentId, subscription);
/// </code>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6g as part of the Graph-to-Doc Sync module.
/// </para>
/// </remarks>
public record GraphChangeSubscription
{
    /// <summary>
    /// Document subscribing to changes.
    /// </summary>
    /// <value>The document's GUID.</value>
    /// <remarks>
    /// LOGIC: The document that will be flagged when subscribed
    /// entities change.
    /// </remarks>
    public required Guid DocumentId { get; init; }

    /// <summary>
    /// Entities to monitor for changes.
    /// </summary>
    /// <value>List of entity GUIDs to watch.</value>
    /// <remarks>
    /// LOGIC: When any of these entities change (matching
    /// <see cref="ChangeTypes"/>), the document is flagged.
    /// Empty list means watch all entities (not recommended).
    /// </remarks>
    public IReadOnlyList<Guid> EntityIds { get; init; } = [];

    /// <summary>
    /// Types of changes to monitor.
    /// </summary>
    /// <value>List of change types to watch.</value>
    /// <remarks>
    /// LOGIC: Filters which changes trigger notifications.
    /// Empty list means all change types.
    /// </remarks>
    public IReadOnlyList<ChangeType> ChangeTypes { get; init; } = [];

    /// <summary>
    /// User to notify on changes.
    /// </summary>
    /// <value>User ID to receive notifications.</value>
    /// <remarks>
    /// LOGIC: The user who will receive notification when
    /// matching changes occur. Typically the document owner
    /// or designated maintainer.
    /// </remarks>
    public Guid NotifyUser { get; init; }

    /// <summary>
    /// When the subscription was created.
    /// </summary>
    /// <value>UTC timestamp of subscription creation.</value>
    /// <remarks>
    /// LOGIC: Recorded for audit trails and subscription management.
    /// </remarks>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Whether the subscription is active.
    /// </summary>
    /// <value>True if subscription is active, false if suspended.</value>
    /// <remarks>
    /// LOGIC: Inactive subscriptions are ignored during change
    /// processing. Allows temporary suspension without deletion.
    /// </remarks>
    public bool IsActive { get; init; } = true;
}
