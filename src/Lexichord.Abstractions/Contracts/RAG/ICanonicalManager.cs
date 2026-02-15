// =============================================================================
// File: ICanonicalManager.cs
// Project: Lexichord.Abstractions
// Description: Interface for managing canonical records and chunk variants.
// =============================================================================
// VERSION: v0.5.9c (Canonical Record Management)
// LOGIC: Central service for deduplication operations. Creates canonical records
//   for unique facts, merges variants, handles promotion/detachment, and tracks
//   provenance. All operations are license-gated (Writer Pro tier).
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Manages canonical records, chunk variants, and provenance for deduplication.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9c as part of the Semantic Memory Deduplication feature.
/// </para>
/// <para>
/// This service is the central API for canonical record operations:
/// </para>
/// <list type="table">
///   <listheader>
///     <term>Operation</term>
///     <description>Purpose</description>
///   </listheader>
///   <item>
///     <term><see cref="CreateCanonicalAsync"/></term>
///     <description>Establish a chunk as the authoritative version of a fact.</description>
///   </item>
///   <item>
///     <term><see cref="MergeIntoCanonicalAsync"/></term>
///     <description>Mark a chunk as a variant/duplicate of an existing canonical.</description>
///   </item>
///   <item>
///     <term><see cref="PromoteVariantAsync"/></term>
///     <description>Replace the canonical chunk with a better variant.</description>
///   </item>
///   <item>
///     <term><see cref="DetachVariantAsync"/></term>
///     <description>Remove a variant from its canonical (restore as independent).</description>
///   </item>
/// </list>
/// <para>
/// <b>License Requirement:</b> All operations require the "Writer Pro" tier.
/// Operations throw <see cref="FeatureNotLicensedException"/> if unlicensed.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe. Merge/promote/detach
/// operations use database transactions to ensure atomicity.
/// </para>
/// </remarks>
public interface ICanonicalManager
{
    /// <summary>
    /// Creates a new canonical record for a unique chunk.
    /// </summary>
    /// <param name="chunk">The chunk to establish as canonical.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created <see cref="CanonicalRecord"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="chunk"/> is null.</exception>
    /// <exception cref="FeatureNotLicensedException">Thrown when Writer Pro license is not active.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the chunk already has a canonical record (as canonical or variant).
    /// </exception>
    /// <remarks>
    /// <para>
    /// Publishes <see cref="CanonicalRecordCreatedEvent"/> on success.
    /// </para>
    /// <para>
    /// LOGIC: Checks for existing canonical before creating. Uses database unique
    /// constraint on CanonicalChunkId as a safety net.
    /// </para>
    /// </remarks>
    Task<CanonicalRecord> CreateCanonicalAsync(Chunk chunk, CancellationToken ct = default);

    /// <summary>
    /// Merges a variant chunk into an existing canonical record.
    /// </summary>
    /// <param name="canonicalId">The canonical record to merge into.</param>
    /// <param name="variant">The chunk to merge as a variant.</param>
    /// <param name="type">The relationship type between variant and canonical.</param>
    /// <param name="similarity">The similarity score (0.0-1.0).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="variant"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="canonicalId"/> is empty.</exception>
    /// <exception cref="FeatureNotLicensedException">Thrown when Writer Pro license is not active.</exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when the specified canonical record does not exist.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the variant chunk is already a canonical or variant elsewhere.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Publishes <see cref="ChunkDeduplicatedEvent"/> on success.
    /// </para>
    /// <para>
    /// LOGIC: Atomic operation - inserts variant record and increments MergeCount
    /// in a single transaction.
    /// </para>
    /// </remarks>
    Task MergeIntoCanonicalAsync(
        Guid canonicalId,
        Chunk variant,
        RelationshipType type,
        float similarity,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves the canonical record for a given chunk.
    /// </summary>
    /// <param name="chunkId">The chunk ID to look up.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The <see cref="CanonicalRecord"/> if found (either as canonical or variant);
    /// <c>null</c> if the chunk has no canonical association.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="chunkId"/> is empty.</exception>
    /// <remarks>
    /// LOGIC: Checks both CanonicalRecords.CanonicalChunkId and ChunkVariants.VariantChunkId.
    /// Returns the same canonical record in both cases.
    /// </remarks>
    Task<CanonicalRecord?> GetCanonicalForChunkAsync(Guid chunkId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all variants merged into a canonical record.
    /// </summary>
    /// <param name="canonicalId">The canonical record ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A list of <see cref="ChunkVariant"/> records; empty if no variants exist.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="canonicalId"/> is empty.</exception>
    Task<IReadOnlyList<ChunkVariant>> GetVariantsAsync(Guid canonicalId, CancellationToken ct = default);

    /// <summary>
    /// Promotes a variant chunk to become the new canonical.
    /// </summary>
    /// <param name="canonicalId">The canonical record to update.</param>
    /// <param name="newCanonicalChunkId">The variant chunk ID to promote.</param>
    /// <param name="reason">A description of why the variant is being promoted.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="canonicalId"/> or <paramref name="newCanonicalChunkId"/> is empty.
    /// </exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="reason"/> is null or empty.</exception>
    /// <exception cref="FeatureNotLicensedException">Thrown when Writer Pro license is not active.</exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when the canonical record or variant does not exist.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Publishes <see cref="VariantPromotedEvent"/> on success.
    /// </para>
    /// <para>
    /// LOGIC: Atomic operation - swaps CanonicalChunkId, converts old canonical to variant,
    /// removes promoted chunk from variants table, all in one transaction.
    /// </para>
    /// </remarks>
    Task PromoteVariantAsync(
        Guid canonicalId,
        Guid newCanonicalChunkId,
        string reason,
        CancellationToken ct = default);

    /// <summary>
    /// Detaches a variant from its canonical record.
    /// </summary>
    /// <param name="variantChunkId">The variant chunk ID to detach.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="variantChunkId"/> is empty.</exception>
    /// <exception cref="FeatureNotLicensedException">Thrown when Writer Pro license is not active.</exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when the variant record does not exist.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Publishes <see cref="VariantDetachedEvent"/> on success.
    /// </para>
    /// <para>
    /// LOGIC: Removes the variant record and decrements the canonical's MergeCount.
    /// The chunk remains in the Chunks table as an independent entity.
    /// </para>
    /// </remarks>
    Task DetachVariantAsync(Guid variantChunkId, CancellationToken ct = default);

    /// <summary>
    /// Records provenance information for a chunk.
    /// </summary>
    /// <param name="chunkId">The chunk to record provenance for.</param>
    /// <param name="provenance">The provenance details.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="chunkId"/> is empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="provenance"/> is null.</exception>
    /// <exception cref="FeatureNotLicensedException">Thrown when Writer Pro license is not active.</exception>
    /// <remarks>
    /// LOGIC: Upserts provenance - if a record exists for the chunk, it is updated.
    /// </remarks>
    Task RecordProvenanceAsync(Guid chunkId, ChunkProvenance provenance, CancellationToken ct = default);

    /// <summary>
    /// Retrieves provenance records for a canonical record (canonical + all variants).
    /// </summary>
    /// <param name="canonicalId">The canonical record ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A list of <see cref="ChunkProvenance"/> records for the canonical and its variants;
    /// empty if no provenance exists.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="canonicalId"/> is empty.</exception>
    Task<IReadOnlyList<ChunkProvenance>> GetProvenanceAsync(Guid canonicalId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves provenance records for multiple canonical records in a single batch.
    /// </summary>
    /// <param name="canonicalIds">The list of canonical record IDs to retrieve provenance for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A dictionary mapping canonical IDs to their provenance records.
    /// Canonical IDs with no provenance will be present in the dictionary with an empty list.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="canonicalIds"/> is null.</exception>
    Task<IDictionary<Guid, IReadOnlyList<ChunkProvenance>>> GetProvenanceBatchAsync(IEnumerable<Guid> canonicalIds, CancellationToken ct = default);
}
