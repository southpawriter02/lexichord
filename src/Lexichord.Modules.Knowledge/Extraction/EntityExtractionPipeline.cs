// =============================================================================
// File: EntityExtractionPipeline.cs
// Project: Lexichord.Modules.Knowledge
// Description: Composite extractor that coordinates multiple entity extractors
//              and aggregates results.
// =============================================================================
// LOGIC: Orchestrates multiple IEntityExtractor implementations to extract
//   entity mentions from text. The pipeline provides:
//   - Priority-ordered execution (higher priority runs first).
//   - Error isolation (one extractor failure doesn't affect others).
//   - Confidence filtering (mentions below MinConfidence are excluded).
//   - Overlap deduplication (higher confidence wins for same text span).
//   - Entity aggregation (mentions grouped into unique entities).
//
// Execution flow:
//   1. Filter extractors by TargetEntityTypes (if specified).
//   2. Run each extractor in priority order.
//   3. Filter mentions by MinConfidence.
//   4. Tag mentions with ExtractorName.
//   5. Deduplicate overlapping mentions.
//   6. Aggregate into AggregatedEntity records.
//
// v0.4.5g: Entity Abstraction Layer (CKVS Phase 1)
// Dependencies: IEntityExtractor, IEntityExtractionPipeline, ExtractionRecords (v0.4.5g),
//               TextChunk (v0.4.3a), ILogger<T> (v0.0.3b)
// =============================================================================

using System.Diagnostics;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Extraction;

/// <summary>
/// Coordinates multiple entity extractors and aggregates results.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="EntityExtractionPipeline"/> is the central orchestrator for
/// entity extraction in the Knowledge Graph subsystem. It manages a collection
/// of <see cref="IEntityExtractor"/> implementations, executing them in
/// priority order and combining their results into a unified
/// <see cref="ExtractionResult"/>.
/// </para>
/// <para>
/// <b>Error Isolation:</b> Each extractor runs independently. If one extractor
/// throws an exception, the error is logged as a warning and the remaining
/// extractors continue processing. This ensures partial results are available
/// even when individual extractors fail.
/// </para>
/// <para>
/// <b>Deduplication:</b> When multiple extractors produce mentions that
/// overlap in the source text (same character span), the mention with the
/// highest confidence is retained. Non-overlapping mentions from different
/// extractors are all included.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5g as part of the Entity Abstraction Layer.
/// </para>
/// </remarks>
internal sealed class EntityExtractionPipeline : IEntityExtractionPipeline
{
    private readonly List<IEntityExtractor> _extractors = new();
    private readonly MentionAggregator _aggregator;
    private readonly ILogger<EntityExtractionPipeline> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="EntityExtractionPipeline"/>.
    /// </summary>
    /// <param name="logger">Logger for extraction diagnostics.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="logger"/> is <c>null</c>.</exception>
    public EntityExtractionPipeline(ILogger<EntityExtractionPipeline> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        _aggregator = new MentionAggregator();
    }

    /// <inheritdoc/>
    public IReadOnlyList<IEntityExtractor> Extractors =>
        _extractors.OrderByDescending(e => e.Priority).ToList();

    /// <inheritdoc/>
    public void Register(IEntityExtractor extractor)
    {
        ArgumentNullException.ThrowIfNull(extractor);

        _extractors.Add(extractor);

        _logger.LogDebug(
            "Registered extractor: {Name} for types [{Types}] with priority {Priority}",
            extractor.GetType().Name,
            string.Join(", ", extractor.SupportedTypes),
            extractor.Priority);
    }

    /// <inheritdoc/>
    public async Task<ExtractionResult> ExtractAllAsync(
        string text,
        ExtractionContext context,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var allMentions = new List<EntityMention>();

        // LOGIC: Run extractors in priority order (highest first).
        foreach (var extractor in Extractors)
        {
            ct.ThrowIfCancellationRequested();

            // LOGIC: Skip extractors that don't produce any of the target types.
            // This optimization avoids running extractors when only specific
            // entity types are requested.
            if (context.TargetEntityTypes != null &&
                !extractor.SupportedTypes.Any(t =>
                    context.TargetEntityTypes.Contains(t, StringComparer.OrdinalIgnoreCase)))
            {
                _logger.LogDebug(
                    "Skipping extractor {Name}: no matching target types",
                    extractor.GetType().Name);
                continue;
            }

            try
            {
                var mentions = await extractor.ExtractAsync(text, context, ct);

                // LOGIC: Filter mentions below the confidence threshold and
                // tag each mention with the extractor name for traceability.
                var filtered = mentions
                    .Where(m => m.Confidence >= context.MinConfidence)
                    .Select(m => m with { ExtractorName = extractor.GetType().Name })
                    .ToList();

                allMentions.AddRange(filtered);

                _logger.LogDebug(
                    "Extractor {Name} found {FilteredCount} mentions (filtered from {TotalCount})",
                    extractor.GetType().Name, filtered.Count, mentions.Count);
            }
            catch (OperationCanceledException)
            {
                throw; // Re-throw cancellation — don't swallow it.
            }
            catch (Exception ex)
            {
                // LOGIC: Error isolation — log the failure and continue with
                // the remaining extractors. Partial results are better than
                // no results.
                _logger.LogWarning(ex,
                    "Extractor {Name} failed during extraction",
                    extractor.GetType().Name);
            }
        }

        // LOGIC: Deduplicate overlapping mentions. When two mentions cover
        // the same text span, keep the one with higher confidence.
        var deduplicated = DeduplicateMentions(allMentions);

        // LOGIC: Aggregate mentions into unique entities by grouping
        // mentions with the same type and normalized value.
        var aggregated = _aggregator.Aggregate(deduplicated);

        stopwatch.Stop();

        _logger.LogDebug(
            "Extraction complete: {MentionCount} mentions, {EntityCount} entities in {Duration}ms",
            deduplicated.Count, aggregated.Count, stopwatch.ElapsedMilliseconds);

        return new ExtractionResult
        {
            Mentions = deduplicated,
            AggregatedEntities = aggregated,
            Duration = stopwatch.Elapsed,
            ChunksProcessed = 1,
            MentionCountByType = deduplicated
                .GroupBy(m => m.EntityType)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    /// <inheritdoc/>
    public async Task<ExtractionResult> ExtractFromChunksAsync(
        IReadOnlyList<TextChunk> chunks,
        Guid documentId,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(chunks);

        var stopwatch = Stopwatch.StartNew();
        var allMentions = new List<EntityMention>();

        _logger.LogDebug(
            "Starting chunk extraction for document {DocumentId} ({ChunkCount} chunks)",
            documentId, chunks.Count);

        foreach (var chunk in chunks)
        {
            ct.ThrowIfCancellationRequested();

            // LOGIC: Skip empty/whitespace-only chunks.
            if (!chunk.HasContent)
            {
                continue;
            }

            // LOGIC: Build extraction context from chunk metadata.
            var context = new ExtractionContext
            {
                DocumentId = documentId,
                ChunkId = Guid.NewGuid(),
                Heading = chunk.Metadata?.Heading,
                HeadingLevel = chunk.Metadata?.Level
            };

            var result = await ExtractAllAsync(chunk.Content, context, ct);

            // LOGIC: Adjust mention offsets from chunk-relative to
            // document-relative using the chunk's StartOffset. This ensures
            // all mentions reference positions in the original document.
            var adjusted = result.Mentions
                .Select(m => m with
                {
                    StartOffset = m.StartOffset + chunk.StartOffset,
                    EndOffset = m.EndOffset + chunk.StartOffset,
                    ChunkId = context.ChunkId
                });

            allMentions.AddRange(adjusted);
        }

        // LOGIC: Re-aggregate across all chunks to merge mentions of the
        // same entity that appear in different chunks.
        var aggregated = _aggregator.Aggregate(allMentions);

        stopwatch.Stop();

        _logger.LogInformation(
            "Chunk extraction complete for document {DocumentId}: " +
            "{MentionCount} mentions, {EntityCount} entities from {ChunkCount} chunks in {Duration}ms",
            documentId, allMentions.Count, aggregated.Count, chunks.Count,
            stopwatch.ElapsedMilliseconds);

        return new ExtractionResult
        {
            Mentions = allMentions,
            AggregatedEntities = aggregated,
            Duration = stopwatch.Elapsed,
            ChunksProcessed = chunks.Count,
            MentionCountByType = allMentions
                .GroupBy(m => m.EntityType)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    // =========================================================================
    // Deduplication
    // =========================================================================

    /// <summary>
    /// Deduplicates overlapping mentions, keeping the highest-confidence mention
    /// for each text span.
    /// </summary>
    /// <param name="mentions">All mentions to deduplicate.</param>
    /// <returns>A deduplicated list of non-overlapping mentions.</returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Deduplication algorithm:
    /// <list type="number">
    ///   <item><description>Sort mentions by start offset (ascending), then by confidence (descending).</description></item>
    ///   <item><description>Iterate through sorted mentions.</description></item>
    ///   <item><description>Skip any mention whose start offset falls within the previous mention's span.</description></item>
    ///   <item><description>Keep the first (highest-confidence) mention for each overlapping group.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    private static List<EntityMention> DeduplicateMentions(List<EntityMention> mentions)
    {
        if (mentions.Count <= 1)
        {
            return mentions;
        }

        // LOGIC: Sort by position first, then by confidence descending.
        // This ensures that for overlapping mentions at the same position,
        // the highest-confidence one is kept.
        var sorted = mentions
            .OrderBy(m => m.StartOffset)
            .ThenByDescending(m => m.Confidence)
            .ToList();

        var result = new List<EntityMention>();
        var lastEnd = -1;

        foreach (var mention in sorted)
        {
            // LOGIC: Skip mentions that overlap with the previously accepted
            // mention. The first mention at each position has the highest
            // confidence due to the sort order.
            if (mention.StartOffset < lastEnd)
            {
                continue;
            }

            result.Add(mention);
            lastEnd = mention.EndOffset;
        }

        return result;
    }
}
