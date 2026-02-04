// =============================================================================
// File: ClaimUpdatedEvent.cs
// Project: Lexichord.Abstractions
// Description: Event published when a claim is updated.
// =============================================================================
// LOGIC: MediatR notification for claim updates, including both the new
//   and previous claim state for comparison.
//
// v0.5.6h: Claim Repository (Knowledge Graph Claim Persistence)
// =============================================================================

using MediatR;

namespace Lexichord.Abstractions.Contracts.Knowledge.Claims.Repository.Events;

/// <summary>
/// Event raised when a claim is updated.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Notifies subscribers when a claim is modified,
/// providing both the new and previous state for change detection.
/// </para>
/// </remarks>
public record ClaimUpdatedEvent : INotification
{
    /// <summary>
    /// The updated claim with new values.
    /// </summary>
    public required Claim Claim { get; init; }

    /// <summary>
    /// The previous claim state before the update.
    /// </summary>
    public required Claim PreviousClaim { get; init; }

    /// <summary>
    /// Timestamp when the event was raised.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
