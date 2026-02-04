// =============================================================================
// File: CanonicalRecordCreatedEvent.cs
// Project: Lexichord.Abstractions
// Description: MediatR notification published when a canonical record is created.
// =============================================================================
// VERSION: v0.5.9c (Canonical Record Management)
// LOGIC: Published by ICanonicalManager.CreateCanonicalAsync() when a new
//   canonical record is established. Enables downstream consumers to:
//   - Track canonical record creation for analytics
//   - Update UI with deduplication status
//   - Log canonical creation for audit trails
// =============================================================================

using MediatR;

namespace Lexichord.Abstractions.Events;

/// <summary>
/// Published when a new canonical record is created for a unique chunk.
/// </summary>
/// <remarks>
/// <para>
/// This event is published by <see cref="ICanonicalManager.CreateCanonicalAsync"/>
/// after successfully establishing a chunk as the canonical representation of a fact.
/// </para>
/// <para>
/// <b>Consumers:</b>
/// <list type="bullet">
///   <item><description>Deduplication analytics and metrics tracking</description></item>
///   <item><description>UI updates for canonical record status</description></item>
///   <item><description>Audit logging for knowledge base changes</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.9c as part of the Semantic Memory Deduplication feature.
/// </para>
/// </remarks>
/// <param name="CanonicalRecordId">
/// The unique identifier of the newly created canonical record.
/// </param>
/// <param name="ChunkId">
/// The identifier of the chunk that was established as canonical.
/// </param>
/// <param name="CreatedAt">
/// UTC timestamp of when the canonical record was created.
/// </param>
public record CanonicalRecordCreatedEvent(
    Guid CanonicalRecordId,
    Guid ChunkId,
    DateTimeOffset CreatedAt) : INotification;
