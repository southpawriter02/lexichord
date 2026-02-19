// =============================================================================
// File: ConflictResolvedEvent.cs
// Project: Lexichord.Abstractions
// Description: MediatR notification for conflict resolution completion.
// =============================================================================
// LOGIC: ConflictResolvedEvent is published when a conflict has been
//   resolved. It provides details about the conflict, the resolution
//   strategy used, and the result for audit and notification purposes.
//
// v0.7.6h: Conflict Resolver (CKVS Phase 4c)
// Dependencies: SyncConflict, ConflictResolutionStrategy (v0.7.6e),
//               ConflictResolutionResult (v0.7.6h)
// =============================================================================

using MediatR;

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict.Events;

/// <summary>
/// MediatR notification published when a conflict has been resolved.
/// </summary>
/// <remarks>
/// <para>
/// Published after successful conflict resolution:
/// </para>
/// <list type="bullet">
///   <item><b>Conflict:</b> The conflict that was resolved.</item>
///   <item><b>Result:</b> The detailed resolution result.</item>
///   <item><b>Timestamp:</b> When resolution completed.</item>
///   <item><b>InitiatedBy:</b> User who initiated (for manual resolution).</item>
/// </list>
/// <para>
/// <b>Usage:</b>
/// <code>
/// public class ConflictResolvedHandler : INotificationHandler&lt;ConflictResolvedEvent&gt;
/// {
///     public Task Handle(ConflictResolvedEvent notification, CancellationToken ct)
///     {
///         Console.WriteLine($"Conflict resolved: {notification.Conflict.ConflictTarget}");
///         Console.WriteLine($"Strategy: {notification.Result.Strategy}");
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6h as part of the Conflict Resolver module.
/// </para>
/// </remarks>
public record ConflictResolvedEvent : INotification
{
    /// <summary>
    /// The conflict that was resolved.
    /// </summary>
    /// <value>The original <see cref="SyncConflict"/> that was resolved.</value>
    /// <remarks>
    /// LOGIC: Provides context about what was in conflict.
    /// </remarks>
    public required SyncConflict Conflict { get; init; }

    /// <summary>
    /// The resolution result.
    /// </summary>
    /// <value>
    /// The <see cref="ConflictResolutionResult"/> with full resolution details.
    /// </value>
    /// <remarks>
    /// LOGIC: Contains strategy, success status, resolved value, and metadata.
    /// </remarks>
    public required ConflictResolutionResult Result { get; init; }

    /// <summary>
    /// Timestamp when the resolution completed.
    /// </summary>
    /// <value>UTC timestamp of resolution completion.</value>
    /// <remarks>
    /// LOGIC: Defaults to current time when event is created.
    /// </remarks>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// User who initiated the resolution.
    /// </summary>
    /// <value>
    /// User ID for manual resolutions.
    /// Null for automatic resolutions.
    /// </value>
    /// <remarks>
    /// LOGIC: Provides accountability for manual interventions.
    /// </remarks>
    public Guid? InitiatedBy { get; init; }

    /// <summary>
    /// Creates a ConflictResolvedEvent instance.
    /// </summary>
    /// <param name="conflict">The resolved conflict.</param>
    /// <param name="result">The resolution result.</param>
    /// <param name="initiatedBy">Optional user who initiated resolution.</param>
    /// <returns>A new ConflictResolvedEvent instance.</returns>
    public static ConflictResolvedEvent Create(
        SyncConflict conflict,
        ConflictResolutionResult result,
        Guid? initiatedBy = null)
    {
        return new ConflictResolvedEvent
        {
            Conflict = conflict,
            Result = result,
            InitiatedBy = initiatedBy
        };
    }
}
