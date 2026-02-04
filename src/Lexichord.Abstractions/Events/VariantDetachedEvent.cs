// =============================================================================
// File: VariantDetachedEvent.cs
// Project: Lexichord.Abstractions
// Description: MediatR notification published when a variant is detached.
// =============================================================================
// VERSION: v0.5.9c (Canonical Record Management)
// LOGIC: Published by ICanonicalManager.DetachVariantAsync() when a variant
//   chunk is removed from its canonical record. Enables downstream consumers to:
//   - Track detachment activity for deduplication metrics
//   - Update UI with chunk independence status
//   - Maintain audit trails of detachment decisions
// =============================================================================

using MediatR;

namespace Lexichord.Abstractions.Events;

/// <summary>
/// Published when a variant chunk is detached from its canonical record.
/// </summary>
/// <remarks>
/// <para>
/// This event is published by <see cref="ICanonicalManager.DetachVariantAsync"/>
/// after successfully detaching a variant from its canonical record. The chunk
/// remains in the Chunks table as an independent entity.
/// </para>
/// <para>
/// <b>Consumers:</b>
/// <list type="bullet">
///   <item><description>Deduplication metrics (adjust merge counts)</description></item>
///   <item><description>UI updates showing chunk restored as independent</description></item>
///   <item><description>Audit logging for detachment decisions</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.9c as part of the Semantic Memory Deduplication feature.
/// </para>
/// </remarks>
/// <param name="CanonicalRecordId">
/// The identifier of the canonical record the variant was detached from.
/// </param>
/// <param name="DetachedChunkId">
/// The identifier of the chunk that was detached (now independent).
/// </param>
public record VariantDetachedEvent(
    Guid CanonicalRecordId,
    Guid DetachedChunkId) : INotification;
