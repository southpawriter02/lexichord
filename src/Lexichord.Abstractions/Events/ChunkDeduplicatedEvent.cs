// =============================================================================
// File: ChunkDeduplicatedEvent.cs
// Project: Lexichord.Abstractions
// Description: MediatR notification published when a chunk is merged as a variant.
// =============================================================================
// VERSION: v0.5.9c (Canonical Record Management)
// LOGIC: Published by ICanonicalManager.MergeIntoCanonicalAsync() when a chunk
//   is identified as a duplicate and merged into an existing canonical record.
//   Enables downstream consumers to:
//   - Track deduplication activity for storage savings metrics
//   - Update UI with chunk status changes
//   - Maintain audit trails of merge decisions
// =============================================================================

using Lexichord.Abstractions.Contracts.RAG;
using MediatR;

namespace Lexichord.Abstractions.Events;

/// <summary>
/// Published when a chunk is merged as a variant into a canonical record.
/// </summary>
/// <remarks>
/// <para>
/// This event is published by <see cref="ICanonicalManager.MergeIntoCanonicalAsync"/>
/// after successfully merging a variant chunk into an existing canonical.
/// </para>
/// <para>
/// <b>Consumers:</b>
/// <list type="bullet">
///   <item><description>Deduplication metrics (storage savings, merge count)</description></item>
///   <item><description>UI updates showing chunk deduplication status</description></item>
///   <item><description>Audit logging with relationship and similarity details</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.9c as part of the Semantic Memory Deduplication feature.
/// </para>
/// </remarks>
/// <param name="CanonicalRecordId">
/// The identifier of the canonical record the variant was merged into.
/// </param>
/// <param name="VariantChunkId">
/// The identifier of the chunk that was merged as a variant.
/// </param>
/// <param name="RelationshipType">
/// The classified relationship between the variant and canonical chunk.
/// </param>
/// <param name="SimilarityScore">
/// The cosine similarity score (0.0-1.0) between variant and canonical embeddings.
/// </param>
public record ChunkDeduplicatedEvent(
    Guid CanonicalRecordId,
    Guid VariantChunkId,
    RelationshipType RelationshipType,
    float SimilarityScore) : INotification;
