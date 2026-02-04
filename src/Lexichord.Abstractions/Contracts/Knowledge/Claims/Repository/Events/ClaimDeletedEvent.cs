// =============================================================================
// File: ClaimDeletedEvent.cs
// Project: Lexichord.Abstractions
// Description: Event published when a claim is deleted.
// =============================================================================
// LOGIC: MediatR notification for claim deletion, enabling cleanup
//   of related data and cache invalidation.
//
// v0.5.6h: Claim Repository (Knowledge Graph Claim Persistence)
// =============================================================================

using MediatR;

namespace Lexichord.Abstractions.Contracts.Knowledge.Claims.Repository.Events;

/// <summary>
/// Event raised when a claim is deleted.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Notifies subscribers when a claim is removed,
/// enabling cache invalidation and cleanup of related data.
/// </para>
/// </remarks>
public record ClaimDeletedEvent : INotification
{
    /// <summary>
    /// The identifier of the deleted claim.
    /// </summary>
    public Guid ClaimId { get; init; }

    /// <summary>
    /// The document that contained the deleted claim.
    /// </summary>
    public Guid DocumentId { get; init; }

    /// <summary>
    /// Timestamp when the event was raised.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
