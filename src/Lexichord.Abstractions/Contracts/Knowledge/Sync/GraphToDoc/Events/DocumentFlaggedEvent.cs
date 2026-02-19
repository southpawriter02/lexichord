// =============================================================================
// File: DocumentFlaggedEvent.cs
// Project: Lexichord.Abstractions
// Description: Event published when a document is flagged for review.
// =============================================================================
// LOGIC: MediatR notification for document flag creation, enabling
//   subscribers to send notifications to document owners.
//
// v0.7.6g: Graph-to-Doc Sync (CKVS Phase 4c)
// v0.7.6j: Enhanced to implement ISyncEvent for unified event handling.
// Dependencies: DocumentFlag, ISyncEvent (v0.7.6j)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Sync.Events;

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc.Events;

/// <summary>
/// Event raised when a document is flagged for review.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Notifies subscribers when a document flag is created,
/// enabling notification delivery to document owners. Subscribers can:
/// </para>
/// <list type="bullet">
///   <item>Send email or in-app notifications to document owners.</item>
///   <item>Update real-time UI with new flag indicators.</item>
///   <item>Aggregate flags for batch notification delivery.</item>
///   <item>Log flag creation for audit trails.</item>
///   <item>Update badge counts and dashboard statistics.</item>
/// </list>
/// <para>
/// <b>Publication:</b> Published by <see cref="IDocumentFlagger.FlagDocumentAsync"/>
/// when <see cref="DocumentFlagOptions.SendNotification"/> is true.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6g as part of the Graph-to-Doc Sync module.
/// </para>
/// <para>
/// <b>Updated in:</b> v0.7.6j to implement <see cref="ISyncEvent"/> for
/// unified event publishing via <see cref="ISyncEventPublisher"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class DocumentFlaggedHandler : INotificationHandler&lt;DocumentFlaggedEvent&gt;
/// {
///     private readonly INotificationService _notifications;
///
///     public DocumentFlaggedHandler(INotificationService notifications)
///     {
///         _notifications = notifications;
///     }
///
///     public async Task Handle(DocumentFlaggedEvent notification, CancellationToken ct)
///     {
///         var flag = notification.Flag;
///         await _notifications.SendAsync(
///             $"Document flagged: {flag.Reason}",
///             flag.DocumentId,
///             flag.Priority);
///     }
/// }
/// </code>
/// </example>
public record DocumentFlaggedEvent : ISyncEvent
{
    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Generated at event creation. Used for event deduplication,
    /// tracking across handlers, and history queries.
    /// v0.7.6j: Added for ISyncEvent compliance.
    /// </remarks>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Maps to <see cref="Timestamp"/> for ISyncEvent compliance.
    /// v0.7.6j: Added for ISyncEvent compliance.
    /// </remarks>
    DateTimeOffset ISyncEvent.PublishedAt => Timestamp;

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Returns the flagged document's ID.
    /// v0.7.6j: Added for ISyncEvent compliance.
    /// </remarks>
    Guid ISyncEvent.DocumentId => Flag.DocumentId;

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Extensible metadata for correlation IDs, user context,
    /// and custom handler-specific data.
    /// v0.7.6j: Added for ISyncEvent compliance.
    /// </remarks>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } =
        new Dictionary<string, object>();

    /// <summary>
    /// The flag that was created.
    /// </summary>
    /// <value>The complete flag record with all details.</value>
    /// <remarks>
    /// LOGIC: Contains all information about the flag including
    /// document ID, reason, priority, and triggering entity.
    /// Subscribers can use this to determine notification content
    /// and delivery options.
    /// </remarks>
    public required DocumentFlag Flag { get; init; }

    /// <summary>
    /// Timestamp when the event was raised.
    /// </summary>
    /// <value>UTC timestamp of event creation.</value>
    /// <remarks>
    /// LOGIC: Recorded at event creation for audit trails and
    /// notification timing. Should match or be slightly after
    /// <see cref="DocumentFlag.CreatedAt"/>.
    /// </remarks>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Creates a new <see cref="DocumentFlaggedEvent"/> with the specified flag.
    /// </summary>
    /// <param name="flag">The flag that was created.</param>
    /// <returns>A new <see cref="DocumentFlaggedEvent"/> instance.</returns>
    /// <remarks>
    /// LOGIC: Factory method following the project convention for event creation.
    /// Ensures required properties are set and provides a clean API.
    /// </remarks>
    public static DocumentFlaggedEvent Create(DocumentFlag flag)
        => new()
        {
            Flag = flag
        };
}
