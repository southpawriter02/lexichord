// =============================================================================
// File: IDeduplicationService.cs
// Project: Lexichord.Abstractions
// Description: Interface for the main deduplication orchestrator service.
// =============================================================================
// VERSION: v0.5.9d (Deduplication Service)
// LOGIC: Orchestrates the full deduplication pipeline during chunk ingestion.
//   Coordinates similarity detection (v0.5.9a), relationship classification (v0.5.9b),
//   and canonical management (v0.5.9c) to process incoming chunks.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Orchestrates the full deduplication pipeline during chunk ingestion.
/// Coordinates similarity detection, relationship classification, and canonical management.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9d as part of the Semantic Memory Deduplication feature.
/// </para>
/// <para>
/// This service is the main entry point for deduplication operations. It integrates:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="ISimilarityDetector"/> (v0.5.9a): Finds similar chunks via pgvector.</description></item>
///   <item><description><see cref="IRelationshipClassifier"/> (v0.5.9b): Classifies semantic relationships.</description></item>
///   <item><description><see cref="ICanonicalManager"/> (v0.5.9c): Manages canonical records and variants.</description></item>
/// </list>
/// <para>
/// <b>Processing Flow:</b>
/// </para>
/// <list type="number">
///   <item><description>Check license (Writer Pro required).</description></item>
///   <item><description>Find similar chunks using <see cref="ISimilarityDetector"/>.</description></item>
///   <item><description>Classify relationships using <see cref="IRelationshipClassifier"/>.</description></item>
///   <item><description>Route by classification type (merge, link, flag, queue, or store as new).</description></item>
/// </list>
/// <para>
/// <b>License Requirement:</b> Writer Pro tier required. When unlicensed,
/// <see cref="ProcessChunkAsync"/> returns <see cref="DeduplicationAction.StoredAsNew"/>
/// without performing any deduplication checks.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe for concurrent chunk processing.
/// </para>
/// </remarks>
public interface IDeduplicationService
{
    /// <summary>
    /// Processes a new chunk through the deduplication pipeline.
    /// </summary>
    /// <param name="newChunk">
    /// The chunk being ingested. Must have a non-null <see cref="Chunk.Embedding"/>.
    /// </param>
    /// <param name="options">
    /// Deduplication options. Defaults to <see cref="DeduplicationOptions.Default"/> if null.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// Result indicating what action was taken. See <see cref="DeduplicationResult"/> for details.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="newChunk"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="newChunk"/> has a <c>null</c> embedding.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Main processing pipeline:
    /// </para>
    /// <list type="number">
    ///   <item><description>License check → bypass if unlicensed (return StoredAsNew).</description></item>
    ///   <item><description>Find similar chunks via ISimilarityDetector.</description></item>
    ///   <item><description>If no matches → create canonical, return StoredAsNew.</description></item>
    ///   <item><description>Classify relationships for each match.</description></item>
    ///   <item><description>Route based on best classification:
    ///     <list type="bullet">
    ///       <item><description>Equivalent + high confidence → auto-merge.</description></item>
    ///       <item><description>Complementary → link to existing.</description></item>
    ///       <item><description>Contradictory → flag (if enabled).</description></item>
    ///       <item><description>Low confidence → queue for review (if enabled).</description></item>
    ///     </list>
    ///   </description></item>
    /// </list>
    /// <para>
    /// <b>Performance:</b> Target latency &lt; 100ms excluding LLM calls.
    /// </para>
    /// </remarks>
    Task<DeduplicationResult> ProcessChunkAsync(
        Chunk newChunk,
        DeduplicationOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Finds potential duplicate candidates for a chunk without taking action.
    /// Useful for preview/dry-run scenarios.
    /// </summary>
    /// <param name="chunk">
    /// The chunk to analyze. Must have a non-null <see cref="Chunk.Embedding"/>.
    /// </param>
    /// <param name="similarityThreshold">
    /// Minimum similarity to consider. Default: 0.85.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// List of duplicate candidates with classification. Empty if no matches found.
    /// Ordered by descending similarity score.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="chunk"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="chunk"/> has a <c>null</c> embedding.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method performs read-only analysis without modifying the index.
    /// Use for:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Previewing deduplication decisions before committing.</description></item>
    ///   <item><description>Building UI for manual deduplication review.</description></item>
    ///   <item><description>Auditing existing content for duplicates.</description></item>
    /// </list>
    /// <para>
    /// <b>License:</b> Requires Writer Pro tier. Returns empty list if unlicensed.
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<DuplicateCandidate>> FindDuplicatesAsync(
        Chunk chunk,
        float similarityThreshold = 0.85f,
        CancellationToken ct = default);

    /// <summary>
    /// Processes a manual merge decision from the review queue.
    /// </summary>
    /// <param name="decision">
    /// The merge decision. See <see cref="ManualMergeDecision"/> for options.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="decision"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when the review ID does not exist or has already been processed.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the decision type requires a target canonical ID that is missing
    /// (e.g., <see cref="ManualDecisionType.Merge"/> without <see cref="ManualMergeDecision.TargetCanonicalId"/>).
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>Authorization:</b> Requires Admin role. Audit trail is logged with reviewer identity.
    /// </para>
    /// <para>
    /// <b>LOGIC:</b> Based on decision type:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="ManualDecisionType.Merge"/>: Calls <see cref="ICanonicalManager.MergeIntoCanonicalAsync"/>.</description></item>
    ///   <item><description><see cref="ManualDecisionType.KeepSeparate"/>: Calls <see cref="ICanonicalManager.CreateCanonicalAsync"/>.</description></item>
    ///   <item><description><see cref="ManualDecisionType.Link"/>: Establishes relationship link.</description></item>
    ///   <item><description><see cref="ManualDecisionType.FlagContradiction"/>: Routes to contradiction workflow.</description></item>
    ///   <item><description><see cref="ManualDecisionType.Delete"/>: Removes the chunk from index.</description></item>
    /// </list>
    /// <para>
    /// The review record is marked as processed with decision details and reviewer.
    /// </para>
    /// </remarks>
    Task ProcessManualDecisionAsync(
        ManualMergeDecision decision,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves chunks pending manual review.
    /// </summary>
    /// <param name="projectId">
    /// Optional project filter. When null, returns reviews across all projects.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// List of chunks awaiting manual deduplication decision.
    /// Ordered by <see cref="PendingReview.QueuedAt"/> ascending (oldest first).
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>Authorization:</b> Requires Admin role.
    /// </para>
    /// <para>
    /// Returns only unprocessed reviews (where reviewed_at is null).
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<PendingReview>> GetPendingReviewsAsync(
        Guid? projectId = null,
        CancellationToken ct = default);
}
