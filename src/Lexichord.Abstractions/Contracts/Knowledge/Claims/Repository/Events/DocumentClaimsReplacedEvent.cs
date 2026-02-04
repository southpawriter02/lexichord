// =============================================================================
// File: DocumentClaimsReplacedEvent.cs
// Project: Lexichord.Abstractions
// Description: Event published when all claims for a document are replaced.
// =============================================================================
// LOGIC: MediatR notification for bulk document claim replacement,
//   tracking the change in claim count.
//
// v0.5.6h: Claim Repository (Knowledge Graph Claim Persistence)
// =============================================================================

using MediatR;

namespace Lexichord.Abstractions.Contracts.Knowledge.Claims.Repository.Events;

/// <summary>
/// Event raised when all claims for a document are replaced.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Notifies subscribers when a document's claims are
/// bulk-replaced, typically during re-extraction.
/// </para>
/// </remarks>
public record DocumentClaimsReplacedEvent : INotification
{
    /// <summary>
    /// The document whose claims were replaced.
    /// </summary>
    public Guid DocumentId { get; init; }

    /// <summary>
    /// Number of claims before replacement.
    /// </summary>
    public int OldClaimCount { get; init; }

    /// <summary>
    /// Number of claims after replacement.
    /// </summary>
    public int NewClaimCount { get; init; }

    /// <summary>
    /// Timestamp when the event was raised.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
