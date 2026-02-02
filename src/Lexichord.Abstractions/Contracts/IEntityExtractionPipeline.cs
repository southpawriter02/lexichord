// =============================================================================
// File: IEntityExtractionPipeline.cs
// Project: Lexichord.Abstractions
// Description: Interface for the composite entity extraction pipeline that
//              coordinates multiple extractors and aggregates results.
// =============================================================================
// LOGIC: Defines the contract for the extraction pipeline that orchestrates
//   multiple IEntityExtractor implementations. The pipeline runs extractors
//   in priority order, handles error isolation, filters by confidence,
//   deduplicates overlapping mentions, and aggregates results into unique
//   entities.
//
// Key operations:
//   - Register: Add extractors to the pipeline.
//   - ExtractAllAsync: Extract from a single text string.
//   - ExtractFromChunksAsync: Extract from multiple document chunks.
//
// v0.4.5g: Entity Abstraction Layer (CKVS Phase 1)
// Dependencies: IEntityExtractor, ExtractionRecords (v0.4.5g),
//               TextChunk (v0.4.3a)
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Composite extractor that runs multiple <see cref="IEntityExtractor"/>
/// instances and aggregates results.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IEntityExtractionPipeline"/> coordinates the execution of
/// registered extractors, combining their results into a unified
/// <see cref="ExtractionResult"/>. It provides error isolation (one extractor
/// failure doesn't affect others), confidence filtering, mention deduplication,
/// and entity aggregation.
/// </para>
/// <para>
/// <b>Execution Order:</b> Extractors run in descending
/// <see cref="IEntityExtractor.Priority"/> order. Higher-priority extractors
/// run first, allowing their results to take precedence during deduplication.
/// </para>
/// <para>
/// <b>Deduplication:</b> When multiple extractors produce overlapping mentions
/// (same text span), the mention with the highest confidence is retained.
/// Non-overlapping mentions from different extractors are all included.
/// </para>
/// <para>
/// <b>Aggregation:</b> After deduplication, mentions are grouped by entity
/// type and normalized value into <see cref="AggregatedEntity"/> records,
/// suitable for creating knowledge graph nodes without duplicates.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5g as part of the Entity Abstraction Layer.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register extractors
/// pipeline.Register(new EndpointExtractor());
/// pipeline.Register(new ParameterExtractor());
/// pipeline.Register(new ConceptExtractor());
///
/// // Extract from text
/// var result = await pipeline.ExtractAllAsync(
///     "GET /users/{userId}?include=orders",
///     new ExtractionContext { DocumentId = docId });
///
/// // Process results
/// foreach (var entity in result.AggregatedEntities)
///     Console.WriteLine($"{entity.EntityType}: {entity.CanonicalValue}");
/// </code>
/// </example>
public interface IEntityExtractionPipeline
{
    /// <summary>
    /// Registers an extractor in the pipeline.
    /// </summary>
    /// <param name="extractor">
    /// The <see cref="IEntityExtractor"/> to register. Must not be <c>null</c>.
    /// </param>
    /// <remarks>
    /// LOGIC: Extractors are stored and executed in descending
    /// <see cref="IEntityExtractor.Priority"/> order. Duplicate registrations
    /// are allowed (the same extractor can be registered multiple times).
    /// </remarks>
    void Register(IEntityExtractor extractor);

    /// <summary>
    /// Gets all registered extractors in priority order (descending).
    /// </summary>
    /// <value>
    /// A read-only list of registered extractors sorted by
    /// <see cref="IEntityExtractor.Priority"/> descending.
    /// </value>
    IReadOnlyList<IEntityExtractor> Extractors { get; }

    /// <summary>
    /// Extracts all entity mentions from a single text string.
    /// </summary>
    /// <param name="text">Text content to analyze.</param>
    /// <param name="context">Extraction context with metadata and configuration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// An <see cref="ExtractionResult"/> containing deduplicated mentions
    /// and aggregated entities. Returns an empty result if no entities are found.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Pipeline execution steps:
    /// <list type="number">
    ///   <item><description>Filter extractors by <see cref="ExtractionContext.TargetEntityTypes"/> (if specified).</description></item>
    ///   <item><description>Run each extractor in priority order with error isolation.</description></item>
    ///   <item><description>Filter mentions by <see cref="ExtractionContext.MinConfidence"/>.</description></item>
    ///   <item><description>Tag mentions with <see cref="EntityMention.ExtractorName"/>.</description></item>
    ///   <item><description>Deduplicate overlapping mentions (higher confidence wins).</description></item>
    ///   <item><description>Aggregate mentions into unique <see cref="AggregatedEntity"/> records.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    Task<ExtractionResult> ExtractAllAsync(
        string text,
        ExtractionContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Extracts entities from a document's chunks with offset adjustment.
    /// </summary>
    /// <param name="chunks">Document chunks to process sequentially.</param>
    /// <param name="documentId">Source document ID for provenance.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// An <see cref="ExtractionResult"/> with mentions adjusted to document-level
    /// offsets and aggregated across all chunks.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Each chunk is processed independently via <see cref="ExtractAllAsync"/>.
    /// Mention offsets are adjusted from chunk-relative to document-relative using
    /// <see cref="TextChunk.StartOffset"/>. Results are re-aggregated across all
    /// chunks to produce a unified entity list.
    /// </para>
    /// </remarks>
    Task<ExtractionResult> ExtractFromChunksAsync(
        IReadOnlyList<TextChunk> chunks,
        Guid documentId,
        CancellationToken ct = default);
}
