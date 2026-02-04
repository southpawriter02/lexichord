// =============================================================================
// File: ClaimCreatedEvent.cs
// Project: Lexichord.Abstractions
// Description: Event published when a claim is created.
// =============================================================================
// LOGIC: MediatR notification for claim creation, enabling subscribers
//   to react to new claims (e.g., update indexes, trigger validation).
//
// v0.5.6h: Claim Repository (Knowledge Graph Claim Persistence)
// =============================================================================

using MediatR;

namespace Lexichord.Abstractions.Contracts.Knowledge.Claims.Repository.Events;

/// <summary>
/// Event raised when a claim is created.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Notifies subscribers when a new claim is persisted,
/// enabling cache invalidation, indexing, or downstream processing.
/// </para>
/// </remarks>
public record ClaimCreatedEvent : INotification
{
    /// <summary>
    /// The newly created claim.
    /// </summary>
    public required Claim Claim { get; init; }

    /// <summary>
    /// Timestamp when the event was raised.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
