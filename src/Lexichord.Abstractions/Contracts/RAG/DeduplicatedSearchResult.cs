// =============================================================================
// File: DeduplicatedSearchResult.cs
// Project: Lexichord.Abstractions
// Description: Extended search result with deduplication metadata.
// =============================================================================
// VERSION: v0.5.9f (Retrieval Integration)
// LOGIC: Extends ChunkSearchResult with canonical record metadata:
//   - CanonicalRecordId: Links to the canonical record (if chunk is canonical).
//   - VariantCount: Number of merged variants (merge_count from CanonicalRecord).
//   - HasContradictions: Whether unresolved contradictions exist for this chunk.
//   - Provenance: Optional list of provenance records tracing chunk origins.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Represents a search result with deduplication-aware metadata.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9f as part of the Retrieval Integration feature.
/// </para>
/// <para>
/// This record extends the basic <see cref="ChunkSearchResult"/> with additional
/// metadata from the deduplication subsystem. When <see cref="SearchOptions.RespectCanonicals"/>
/// is enabled, search results are filtered to return only canonical chunks (authoritative
/// versions) and standalone chunks (those not part of any canonical grouping).
/// </para>
/// <para>
/// <b>Canonical vs Standalone:</b>
/// </para>
/// <list type="bullet">
///   <item><description>
///     <b>Canonical:</b> A chunk designated as the authoritative version of semantically
///     equivalent content. Has a <see cref="CanonicalRecordId"/> and may have merged variants.
///   </description></item>
///   <item><description>
///     <b>Standalone:</b> A chunk not involved in any deduplication relationship.
///     <see cref="CanonicalRecordId"/> is null and <see cref="VariantCount"/> is 0.
///   </description></item>
/// </list>
/// <para>
/// <b>Performance considerations:</b> Loading <see cref="Provenance"/> requires additional
/// database queries. Use <see cref="SearchOptions.IncludeProvenance"/> only when needed.
/// </para>
/// </remarks>
/// <param name="Chunk">
/// The matched chunk. When <see cref="SearchOptions.RespectCanonicals"/> is true,
/// this is always either a canonical chunk or a standalone chunk (never a variant).
/// </param>
/// <param name="SimilarityScore">
/// The relevance score for this result, typically in the range [0.0, 1.0].
/// Higher values indicate greater semantic similarity to the query.
/// </param>
/// <param name="CanonicalRecordId">
/// The identifier of the canonical record this chunk belongs to, if any.
/// Null for standalone chunks that have not been deduplicated.
/// When non-null, this chunk is the designated canonical version.
/// </param>
/// <param name="VariantCount">
/// The number of variant chunks that have been merged into this canonical.
/// Zero for standalone chunks or canonicals with no merged variants.
/// This value comes from <see cref="CanonicalRecord.MergeCount"/>.
/// </param>
/// <param name="HasContradictions">
/// Whether this chunk has any unresolved contradictions flagged against it.
/// When true, the content may conflict with other chunks and requires review.
/// </param>
/// <param name="Provenance">
/// Optional list of provenance records tracing the chunk's origins.
/// Only populated when <see cref="SearchOptions.IncludeProvenance"/> is true.
/// Includes provenance for both the canonical chunk and any merged variants.
/// </param>
public record DeduplicatedSearchResult(
    Chunk Chunk,
    double SimilarityScore,
    Guid? CanonicalRecordId,
    int VariantCount,
    bool HasContradictions,
    IReadOnlyList<ChunkProvenance>? Provenance)
{
    /// <summary>
    /// Gets a value indicating whether this result meets a high confidence threshold.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="SimilarityScore"/> is 0.8 or higher; otherwise, <c>false</c>.
    /// </value>
    public bool IsHighConfidence => SimilarityScore >= 0.8;

    /// <summary>
    /// Gets a value indicating whether this result meets a minimum relevance threshold.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="SimilarityScore"/> is 0.5 or higher; otherwise, <c>false</c>.
    /// </value>
    public bool MeetsMinimumThreshold => SimilarityScore >= 0.5;

    /// <summary>
    /// Gets a value indicating whether this chunk is a canonical record.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="CanonicalRecordId"/> is not null; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// A canonical chunk is the designated authoritative version of semantically
    /// equivalent content. Standalone chunks return <c>false</c> here.
    /// </remarks>
    public bool IsCanonical => CanonicalRecordId.HasValue;

    /// <summary>
    /// Gets a value indicating whether this chunk is standalone (not deduplicated).
    /// </summary>
    /// <value>
    /// <c>true</c> if this chunk is not part of any canonical grouping; otherwise, <c>false</c>.
    /// </value>
    public bool IsStandalone => !CanonicalRecordId.HasValue;

    /// <summary>
    /// Gets a value indicating whether this canonical has merged variants.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="VariantCount"/> is greater than zero; otherwise, <c>false</c>.
    /// </value>
    public bool HasVariants => VariantCount > 0;

    /// <summary>
    /// Gets a value indicating whether provenance information is available.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="Provenance"/> is not null and contains items; otherwise, <c>false</c>.
    /// </value>
    public bool HasProvenance => Provenance is { Count: > 0 };

    /// <summary>
    /// Creates a DeduplicatedSearchResult from a basic ChunkSearchResult.
    /// </summary>
    /// <param name="result">The source search result.</param>
    /// <returns>A DeduplicatedSearchResult with no deduplication metadata.</returns>
    /// <remarks>
    /// Use this factory method when the deduplication feature is not enabled
    /// (e.g., unlicensed) but a DeduplicatedSearchResult is expected.
    /// </remarks>
    public static DeduplicatedSearchResult FromBasicResult(ChunkSearchResult result)
    {
        return new DeduplicatedSearchResult(
            Chunk: result.Chunk,
            SimilarityScore: result.SimilarityScore,
            CanonicalRecordId: null,
            VariantCount: 0,
            HasContradictions: false,
            Provenance: null);
    }
}
