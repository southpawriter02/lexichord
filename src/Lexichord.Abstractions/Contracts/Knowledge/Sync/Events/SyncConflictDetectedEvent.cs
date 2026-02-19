// =============================================================================
// File: SyncConflictDetectedEvent.cs
// Project: Lexichord.Abstractions
// Description: Event published when sync conflicts are detected.
// =============================================================================
// LOGIC: Published during document-to-graph or graph-to-document sync when
//   conflicts are detected between document content and graph state. Handlers
//   can notify users, trigger conflict resolution workflows, or log for audit.
//
// v0.7.6j: Sync Event Publisher (CKVS Phase 4c)
// Dependencies: ISyncEvent (v0.7.6j), SyncConflict (v0.7.6e),
//               ConflictResolutionStrategy (v0.7.6e)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.Events;

/// <summary>
/// Event published when sync conflicts are detected.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Notifies subscribers when conflicts are detected during
/// synchronization between a document and the knowledge graph.
/// </para>
/// <para>
/// <b>Common Use Cases:</b>
/// </para>
/// <list type="bullet">
///   <item>Display conflict notification to the user.</item>
///   <item>Trigger conflict resolution workflow.</item>
///   <item>Update UI conflict indicators.</item>
///   <item>Log conflicts for audit trail.</item>
/// </list>
/// <para>
/// <b>Publishing:</b> Published by sync components when conflicts are detected.
/// May be published multiple times for the same document if new conflicts arise.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6j as part of the Sync Event Publisher.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class ConflictAlertHandler : INotificationHandler&lt;SyncConflictDetectedEvent&gt;
/// {
///     public async Task Handle(SyncConflictDetectedEvent notification, CancellationToken ct)
///     {
///         Console.WriteLine($"{notification.ConflictCount} conflicts detected for document {notification.DocumentId}");
///         foreach (var conflict in notification.Conflicts)
///         {
///             Console.WriteLine($"  - {conflict.Type}: {conflict.ConflictTarget}");
///         }
///     }
/// }
/// </code>
/// </example>
public record SyncConflictDetectedEvent : ISyncEvent
{
    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Generated at event creation. Used for deduplication and tracking.
    /// </remarks>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Recorded at event creation for ordering and audit.
    /// </remarks>
    public DateTimeOffset PublishedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: The document where conflicts were detected.
    /// </remarks>
    public required Guid DocumentId { get; init; }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Extensible metadata for correlation and context.
    /// </remarks>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } =
        new Dictionary<string, object>();

    /// <summary>
    /// The conflicts that were detected.
    /// </summary>
    /// <value>List of detected sync conflicts.</value>
    /// <remarks>
    /// LOGIC: Contains detailed information about each conflict including
    /// type, severity, document value, and graph value.
    /// </remarks>
    public required IReadOnlyList<SyncConflict> Conflicts { get; init; }

    /// <summary>
    /// Total number of conflicts detected.
    /// </summary>
    /// <value>Count of conflicts.</value>
    /// <remarks>
    /// LOGIC: Convenience property for quick conflict count access.
    /// Equivalent to <c>Conflicts.Count</c>.
    /// </remarks>
    public int ConflictCount { get; init; }

    /// <summary>
    /// Suggested resolution strategy.
    /// </summary>
    /// <value>The recommended strategy for resolving these conflicts, or null.</value>
    /// <remarks>
    /// LOGIC: System-suggested strategy based on conflict analysis.
    /// Null when conflicts require manual review or mixed strategies.
    /// </remarks>
    public ConflictResolutionStrategy? SuggestedStrategy { get; init; }

    /// <summary>
    /// Creates a new <see cref="SyncConflictDetectedEvent"/> with the specified parameters.
    /// </summary>
    /// <param name="documentId">The document where conflicts were detected.</param>
    /// <param name="conflicts">The detected conflicts.</param>
    /// <param name="suggestedStrategy">Optional suggested resolution strategy.</param>
    /// <returns>A new <see cref="SyncConflictDetectedEvent"/> instance.</returns>
    /// <remarks>
    /// LOGIC: Factory method for consistent event creation.
    /// Sets ConflictCount automatically from conflicts list.
    /// </remarks>
    public static SyncConflictDetectedEvent Create(
        Guid documentId,
        IReadOnlyList<SyncConflict> conflicts,
        ConflictResolutionStrategy? suggestedStrategy = null) => new()
    {
        DocumentId = documentId,
        Conflicts = conflicts,
        ConflictCount = conflicts.Count,
        SuggestedStrategy = suggestedStrategy
    };
}
