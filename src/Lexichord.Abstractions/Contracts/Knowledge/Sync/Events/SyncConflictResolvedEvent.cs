// =============================================================================
// File: SyncConflictResolvedEvent.cs
// Project: Lexichord.Abstractions
// Description: Event published when a sync conflict is resolved.
// =============================================================================
// LOGIC: Published when a conflict is resolved, either automatically or manually.
//   Handlers can update UI state, log resolution for audit, or trigger
//   subsequent sync operations.
//
// v0.7.6j: Sync Event Publisher (CKVS Phase 4c)
// Dependencies: ISyncEvent (v0.7.6j), SyncConflict (v0.7.6e),
//               ConflictResolutionStrategy (v0.7.6e)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.Events;

/// <summary>
/// Event published when a sync conflict is resolved.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Notifies subscribers when a conflict has been resolved,
/// providing details about the resolution strategy and outcome.
/// </para>
/// <para>
/// <b>Common Use Cases:</b>
/// </para>
/// <list type="bullet">
///   <item>Update conflict indicators in UI.</item>
///   <item>Log resolution for audit trail.</item>
///   <item>Trigger re-sync after resolution.</item>
///   <item>Notify user of automatic resolution.</item>
/// </list>
/// <para>
/// <b>Publishing:</b> Published by conflict resolution workflows when a
/// conflict is successfully resolved.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6j as part of the Sync Event Publisher.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class ResolutionLogger : INotificationHandler&lt;SyncConflictResolvedEvent&gt;
/// {
///     public Task Handle(SyncConflictResolvedEvent notification, CancellationToken ct)
///     {
///         Console.WriteLine($"Conflict resolved for document {notification.DocumentId}");
///         Console.WriteLine($"  Strategy: {notification.Strategy}");
///         Console.WriteLine($"  Resolved by: {notification.ResolvedBy ?? "System"}");
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
public record SyncConflictResolvedEvent : ISyncEvent
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
    /// LOGIC: The document where the conflict was resolved.
    /// </remarks>
    public required Guid DocumentId { get; init; }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Extensible metadata for correlation and context.
    /// </remarks>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } =
        new Dictionary<string, object>();

    /// <summary>
    /// The conflict that was resolved.
    /// </summary>
    /// <value>The original conflict record.</value>
    /// <remarks>
    /// LOGIC: Contains the conflict details before resolution.
    /// </remarks>
    public required SyncConflict Conflict { get; init; }

    /// <summary>
    /// Resolution strategy applied.
    /// </summary>
    /// <value>The strategy used to resolve the conflict.</value>
    /// <remarks>
    /// LOGIC: Indicates how the conflict was resolved:
    /// UseDocument, UseGraph, Manual, Merge, etc.
    /// </remarks>
    public required ConflictResolutionStrategy Strategy { get; init; }

    /// <summary>
    /// The resolved value.
    /// </summary>
    /// <value>The final value after resolution, or null.</value>
    /// <remarks>
    /// LOGIC: The value that was chosen or merged as the resolution.
    /// Null for DiscardDocument/DiscardGraph strategies.
    /// </remarks>
    public object? ResolvedValue { get; init; }

    /// <summary>
    /// User who resolved the conflict.
    /// </summary>
    /// <value>User ID of resolver, or null for automatic resolution.</value>
    /// <remarks>
    /// LOGIC: Null indicates system/automatic resolution.
    /// Non-null indicates manual user action.
    /// </remarks>
    public Guid? ResolvedBy { get; init; }

    /// <summary>
    /// Creates a new <see cref="SyncConflictResolvedEvent"/> with the specified parameters.
    /// </summary>
    /// <param name="documentId">The document where the conflict was resolved.</param>
    /// <param name="conflict">The resolved conflict.</param>
    /// <param name="strategy">The resolution strategy applied.</param>
    /// <param name="resolvedValue">The resulting value.</param>
    /// <param name="resolvedBy">User who resolved, or null for automatic.</param>
    /// <returns>A new <see cref="SyncConflictResolvedEvent"/> instance.</returns>
    /// <remarks>
    /// LOGIC: Factory method for consistent event creation.
    /// </remarks>
    public static SyncConflictResolvedEvent Create(
        Guid documentId,
        SyncConflict conflict,
        ConflictResolutionStrategy strategy,
        object? resolvedValue = null,
        Guid? resolvedBy = null) => new()
    {
        DocumentId = documentId,
        Conflict = conflict,
        Strategy = strategy,
        ResolvedValue = resolvedValue,
        ResolvedBy = resolvedBy
    };
}
