// =============================================================================
// File: VariantPromotedEvent.cs
// Project: Lexichord.Abstractions
// Description: MediatR notification published when a variant is promoted.
// =============================================================================
// VERSION: v0.5.9c (Canonical Record Management)
// LOGIC: Published by ICanonicalManager.PromoteVariantAsync() when a variant
//   chunk replaces the current canonical. Enables downstream consumers to:
//   - Track promotion activity for knowledge base evolution
//   - Update cached references to canonical chunks
//   - Maintain audit trails of promotion decisions with reasons
// =============================================================================

using MediatR;

namespace Lexichord.Abstractions.Events;

/// <summary>
/// Published when a variant chunk is promoted to become the new canonical.
/// </summary>
/// <remarks>
/// <para>
/// This event is published by <see cref="ICanonicalManager.PromoteVariantAsync"/>
/// after successfully promoting a variant to replace the current canonical chunk.
/// </para>
/// <para>
/// <b>Consumers:</b>
/// <list type="bullet">
///   <item><description>Cache invalidation for canonical chunk references</description></item>
///   <item><description>UI updates showing new canonical status</description></item>
///   <item><description>Audit logging with promotion reason</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.9c as part of the Semantic Memory Deduplication feature.
/// </para>
/// </remarks>
/// <param name="CanonicalRecordId">
/// The identifier of the canonical record that was updated.
/// </param>
/// <param name="OldCanonicalChunkId">
/// The identifier of the chunk that was previously canonical (now a variant).
/// </param>
/// <param name="NewCanonicalChunkId">
/// The identifier of the chunk that is now canonical (was a variant).
/// </param>
/// <param name="Reason">
/// The reason provided for the promotion decision.
/// </param>
public record VariantPromotedEvent(
    Guid CanonicalRecordId,
    Guid OldCanonicalChunkId,
    Guid NewCanonicalChunkId,
    string Reason) : INotification;
