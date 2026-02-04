// =============================================================================
// File: DeduplicationResult.cs
// Project: Lexichord.Abstractions
// Description: Result record for chunk deduplication processing.
// =============================================================================
// VERSION: v0.5.9d (Deduplication Service)
// LOGIC: Encapsulates the outcome of processing a chunk through the deduplication
//   pipeline. Contains the canonical chunk ID, action taken, and processing metrics.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Result of processing a chunk through the deduplication pipeline.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9d as part of the Semantic Memory Deduplication feature.
/// </para>
/// <para>
/// This record provides complete information about what happened to a chunk
/// during deduplication, including:
/// </para>
/// <list type="bullet">
///   <item><description>The canonical chunk ID (new or existing) that represents this fact.</description></item>
///   <item><description>The specific action taken by the deduplication service.</description></item>
///   <item><description>Merge source information if the chunk was merged.</description></item>
///   <item><description>Linked chunk IDs if complementary relationships were established.</description></item>
///   <item><description>Processing duration for performance monitoring.</description></item>
/// </list>
/// </remarks>
/// <param name="CanonicalChunkId">
/// The ID of the canonical chunk (new or existing) that represents this fact.
/// When <see cref="ActionTaken"/> is <see cref="DeduplicationAction.StoredAsNew"/>,
/// this is the newly stored chunk's ID. When merged, this is the existing canonical.
/// </param>
/// <param name="ActionTaken">
/// The deduplication action performed. See <see cref="DeduplicationAction"/> for details.
/// </param>
/// <param name="MergedFromId">
/// If merged, the ID of the chunk that was merged into the canonical.
/// This is the new chunk's original ID before being stored as a variant.
/// Null if not a merge operation.
/// </param>
/// <param name="LinkedChunkIds">
/// IDs of chunks linked but not merged. Populated when
/// <see cref="DeduplicationAction.LinkedToExisting"/> is the action.
/// Null or empty if no linking occurred.
/// </param>
/// <param name="ProcessingDuration">
/// Time spent processing this chunk through the deduplication pipeline.
/// Excludes LLM call latency when rule-based classification is used.
/// Null if duration was not tracked.
/// </param>
public record DeduplicationResult(
    Guid CanonicalChunkId,
    DeduplicationAction ActionTaken,
    Guid? MergedFromId = null,
    IReadOnlyList<Guid>? LinkedChunkIds = null,
    TimeSpan? ProcessingDuration = null)
{
    /// <summary>
    /// Creates a result indicating the chunk was stored as new content.
    /// </summary>
    /// <param name="chunkId">The ID of the newly stored chunk.</param>
    /// <param name="duration">Optional processing duration.</param>
    /// <returns>A <see cref="DeduplicationResult"/> with <see cref="DeduplicationAction.StoredAsNew"/>.</returns>
    public static DeduplicationResult StoredAsNew(Guid chunkId, TimeSpan? duration = null)
        => new(chunkId, DeduplicationAction.StoredAsNew, ProcessingDuration: duration);

    /// <summary>
    /// Creates a result indicating the chunk was merged into an existing canonical.
    /// </summary>
    /// <param name="canonicalId">The existing canonical chunk ID.</param>
    /// <param name="mergedFromId">The new chunk's ID that was merged.</param>
    /// <param name="duration">Optional processing duration.</param>
    /// <returns>A <see cref="DeduplicationResult"/> with <see cref="DeduplicationAction.MergedIntoExisting"/>.</returns>
    public static DeduplicationResult Merged(Guid canonicalId, Guid mergedFromId, TimeSpan? duration = null)
        => new(canonicalId, DeduplicationAction.MergedIntoExisting, MergedFromId: mergedFromId, ProcessingDuration: duration);

    /// <summary>
    /// Creates a result indicating the chunk was queued for manual review.
    /// </summary>
    /// <param name="chunkId">The ID of the queued chunk.</param>
    /// <param name="duration">Optional processing duration.</param>
    /// <returns>A <see cref="DeduplicationResult"/> with <see cref="DeduplicationAction.QueuedForReview"/>.</returns>
    public static DeduplicationResult QueuedForReview(Guid chunkId, TimeSpan? duration = null)
        => new(chunkId, DeduplicationAction.QueuedForReview, ProcessingDuration: duration);
}
