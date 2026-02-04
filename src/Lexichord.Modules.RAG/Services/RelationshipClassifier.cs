// =============================================================================
// File: RelationshipClassifier.cs
// Project: Lexichord.Modules.RAG
// Description: Implementation of IRelationshipClassifier for chunk relationship classification.
// =============================================================================
// VERSION: v0.5.9b (Relationship Classification)
// LOGIC: Classifies semantic relationships using a hybrid approach:
//   - Rule-based fast-path for high-confidence matches (similarity >= 0.95).
//   - LLM-based classification for ambiguous cases when enabled.
//   - Caching layer to minimize redundant LLM calls.
//   - License gating: Writer Pro required.
// =============================================================================

using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// Implementation of <see cref="IRelationshipClassifier"/> for classifying
/// semantic relationships between similar chunks.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9b as part of the Semantic Memory Deduplication feature.
/// </para>
/// <para>
/// This service uses a hybrid classification strategy:
/// </para>
/// <list type="bullet">
///   <item><description>
///     <b>Fast-path:</b> Rule-based classification for similarity >= 0.95.
///     Uses content analysis and metadata to determine relationships quickly.
///   </description></item>
///   <item><description>
///     <b>LLM classification:</b> For ambiguous cases (0.80 <= similarity < 0.95)
///     when <see cref="ClassificationOptions.EnableLlmClassification"/> is true
///     and an LLM service is available.
///   </description></item>
///   <item><description>
///     <b>Caching:</b> Results are cached using sorted chunk IDs as keys
///     to ensure cache hits regardless of pair ordering.
///   </description></item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> This service is thread-safe. The memory cache handles
/// concurrent access, and all state is managed through injected dependencies.
/// </para>
/// </remarks>
public sealed class RelationshipClassifier : IRelationshipClassifier
{
    private readonly IMemoryCache _cache;
    private readonly ILicenseContext _licenseContext;
    private readonly ClassificationOptions _defaultOptions;
    private readonly ILogger<RelationshipClassifier> _logger;

    // LOGIC: Cache key prefix to avoid collisions with other cached data.
    private const string CacheKeyPrefix = "RelClassify:";

    /// <summary>
    /// Initializes a new instance of the <see cref="RelationshipClassifier"/> class.
    /// </summary>
    /// <param name="cache">Memory cache for classification results.</param>
    /// <param name="licenseContext">License context for feature gating.</param>
    /// <param name="options">Default classification options.</param>
    /// <param name="logger">Logger for structured diagnostics.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any required parameter is <c>null</c>.
    /// </exception>
    public RelationshipClassifier(
        IMemoryCache cache,
        ILicenseContext licenseContext,
        IOptions<ClassificationOptions> options,
        ILogger<RelationshipClassifier> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _defaultOptions = (options ?? throw new ArgumentNullException(nameof(options))).Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug(
            "RelationshipClassifier initialized with RuleBasedThreshold={Threshold}, " +
            "LlmEnabled={LlmEnabled}, CacheDuration={CacheDuration}",
            _defaultOptions.RuleBasedThreshold,
            _defaultOptions.EnableLlmClassification,
            _defaultOptions.CacheDuration);
    }

    /// <inheritdoc/>
    public async Task<RelationshipClassification> ClassifyAsync(
        Chunk chunkA,
        Chunk chunkB,
        float similarityScore,
        ClassificationOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(chunkA);
        ArgumentNullException.ThrowIfNull(chunkB);

        var effectiveOptions = options ?? _defaultOptions;

        // LOGIC: License check - Writer Pro required for classification.
        if (!_licenseContext.IsFeatureEnabled(FeatureCodes.SemanticDeduplication))
        {
            _logger.LogDebug(
                "Classification skipped - license insufficient for ChunkA={ChunkAId}, ChunkB={ChunkBId}",
                chunkA.Id,
                chunkB.Id);

            return await Task.FromResult(new RelationshipClassification(
                Type: RelationshipType.Unknown,
                Confidence: 0f,
                Explanation: "License tier does not include relationship classification.",
                Method: ClassificationMethod.RuleBased));
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // LOGIC: Check cache first if enabled.
            if (effectiveOptions.EnableCaching)
            {
                var cacheKey = GenerateCacheKey(chunkA.Id, chunkB.Id);
                if (_cache.TryGetValue<RelationshipClassification>(cacheKey, out var cachedResult) && cachedResult is not null)
                {
                    _logger.LogDebug(
                        "Cache hit for classification: ChunkA={ChunkAId}, ChunkB={ChunkBId}, Type={Type}",
                        chunkA.Id,
                        chunkB.Id,
                        cachedResult.Type);

                    return cachedResult with { Method = ClassificationMethod.Cached };
                }
            }

            // LOGIC: Classify the relationship.
            var result = ClassifyInternal(chunkA, chunkB, similarityScore, effectiveOptions);

            // LOGIC: Cache the result if caching is enabled.
            if (effectiveOptions.EnableCaching)
            {
                var cacheKey = GenerateCacheKey(chunkA.Id, chunkB.Id);
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = effectiveOptions.CacheDuration
                };
                _cache.Set(cacheKey, result, cacheOptions);
            }

            stopwatch.Stop();

            // LOGIC: Record metrics for observability (v0.5.9h).
            Metrics.DeduplicationMetrics.RecordClassification(
                result.Method,
                stopwatch.Elapsed.TotalMilliseconds);

            _logger.LogInformation(
                "Classified relationship: ChunkA={ChunkAId}, ChunkB={ChunkBId}, " +
                "Type={Type}, Confidence={Confidence:F2}, Method={Method}, Duration={DurationMs}ms",
                chunkA.Id,
                chunkB.Id,
                result.Type,
                result.Confidence,
                result.Method,
                stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stopwatch.Stop();

            _logger.LogError(
                ex,
                "Error classifying relationship: ChunkA={ChunkAId}, ChunkB={ChunkBId}, Duration={DurationMs}ms",
                chunkA.Id,
                chunkB.Id,
                stopwatch.ElapsedMilliseconds);

            // LOGIC: Return Unknown on error rather than throwing.
            return new RelationshipClassification(
                Type: RelationshipType.Unknown,
                Confidence: 0f,
                Explanation: "Classification failed due to an internal error.",
                Method: ClassificationMethod.RuleBased);
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RelationshipClassification>> ClassifyBatchAsync(
        IReadOnlyList<ChunkPair> pairs,
        ClassificationOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(pairs);

        if (pairs.Count == 0)
        {
            _logger.LogDebug("ClassifyBatchAsync called with empty pairs collection");
            return Array.Empty<RelationshipClassification>();
        }

        var effectiveOptions = options ?? _defaultOptions;

        _logger.LogInformation(
            "Starting batch classification for {Count} pairs",
            pairs.Count);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var results = new List<RelationshipClassification>(pairs.Count);

        foreach (var pair in pairs)
        {
            ct.ThrowIfCancellationRequested();

            var result = await ClassifyAsync(
                pair.ChunkA,
                pair.ChunkB,
                pair.SimilarityScore,
                effectiveOptions,
                ct);

            results.Add(result);
        }

        stopwatch.Stop();

        _logger.LogInformation(
            "Batch classification complete: {Count} pairs in {DurationMs}ms",
            pairs.Count,
            stopwatch.ElapsedMilliseconds);

        return results;
    }

    /// <summary>
    /// Generates a cache key from two chunk IDs, ensuring consistent ordering.
    /// </summary>
    /// <param name="idA">First chunk ID.</param>
    /// <param name="idB">Second chunk ID.</param>
    /// <returns>A cache key string.</returns>
    private static string GenerateCacheKey(Guid idA, Guid idB)
    {
        // LOGIC: Sort IDs to ensure same key regardless of pair order.
        var minId = idA.CompareTo(idB) <= 0 ? idA : idB;
        var maxId = idA.CompareTo(idB) <= 0 ? idB : idA;
        return $"{CacheKeyPrefix}{minId}:{maxId}";
    }

    /// <summary>
    /// Performs the internal classification logic.
    /// </summary>
    private RelationshipClassification ClassifyInternal(
        Chunk chunkA,
        Chunk chunkB,
        float similarityScore,
        ClassificationOptions options)
    {
        // LOGIC: Below minimum threshold = Distinct.
        if (similarityScore < options.MinimumSimilarityThreshold)
        {
            return new RelationshipClassification(
                Type: RelationshipType.Distinct,
                Confidence: 0.95f,
                Explanation: options.IncludeExplanation
                    ? $"Similarity score {similarityScore:F2} is below minimum threshold {options.MinimumSimilarityThreshold:F2}."
                    : null,
                Method: ClassificationMethod.RuleBased);
        }

        // LOGIC: Perfect match = Equivalent with full confidence.
        if (similarityScore >= 0.999f)
        {
            return new RelationshipClassification(
                Type: RelationshipType.Equivalent,
                Confidence: 1.0f,
                Explanation: options.IncludeExplanation
                    ? "Chunks have identical or near-identical embeddings."
                    : null,
                Method: ClassificationMethod.RuleBased);
        }

        // LOGIC: High similarity fast-path (rule-based).
        if (similarityScore >= options.RuleBasedThreshold)
        {
            return ClassifyRuleBased(chunkA, chunkB, similarityScore, options);
        }

        // LOGIC: Ambiguous range - would use LLM if available.
        // For v0.5.9b MVP, fall back to rule-based with lower confidence.
        if (options.EnableLlmClassification)
        {
            _logger.LogDebug(
                "LLM classification requested but not yet implemented; " +
                "falling back to rule-based for ChunkA={ChunkAId}, ChunkB={ChunkBId}",
                chunkA.Id,
                chunkB.Id);
        }

        // LOGIC: Fallback to rule-based with reduced confidence.
        return ClassifyRuleBasedAmbiguous(chunkA, chunkB, similarityScore, options);
    }

    /// <summary>
    /// Rule-based classification for high-similarity pairs (>= threshold).
    /// </summary>
    private RelationshipClassification ClassifyRuleBased(
        Chunk chunkA,
        Chunk chunkB,
        float similarityScore,
        ClassificationOptions options)
    {
        // LOGIC: Same document - check for complementary relationship.
        if (chunkA.DocumentId == chunkB.DocumentId)
        {
            // Adjacent chunks are complementary.
            if (Math.Abs(chunkA.ChunkIndex - chunkB.ChunkIndex) <= 1)
            {
                return new RelationshipClassification(
                    Type: RelationshipType.Complementary,
                    Confidence: 0.85f,
                    Explanation: options.IncludeExplanation
                        ? "Chunks are adjacent in the same document."
                        : null,
                    Method: ClassificationMethod.RuleBased);
            }
        }

        // LOGIC: Check for subset relationship based on content length.
        var lengthRatio = (float)Math.Min(chunkA.ContentLength, chunkB.ContentLength)
                        / Math.Max(chunkA.ContentLength, chunkB.ContentLength);

        if (lengthRatio < 0.5f && similarityScore >= 0.90f)
        {
            // Significant length difference with high similarity suggests subset.
            return new RelationshipClassification(
                Type: RelationshipType.Subset,
                Confidence: 0.80f,
                Explanation: options.IncludeExplanation
                    ? $"One chunk is significantly shorter (ratio: {lengthRatio:F2}) with high similarity."
                    : null,
                Method: ClassificationMethod.RuleBased);
        }

        // LOGIC: Default to Equivalent for high-similarity pairs.
        return new RelationshipClassification(
            Type: RelationshipType.Equivalent,
            Confidence: Math.Min(0.95f, similarityScore),
            Explanation: options.IncludeExplanation
                ? $"High similarity ({similarityScore:F2}) indicates semantic equivalence."
                : null,
            Method: ClassificationMethod.RuleBased);
    }

    /// <summary>
    /// Rule-based classification for ambiguous similarity range (below threshold).
    /// </summary>
    private RelationshipClassification ClassifyRuleBasedAmbiguous(
        Chunk chunkA,
        Chunk chunkB,
        float similarityScore,
        ClassificationOptions options)
    {
        // LOGIC: Same document - likely complementary.
        if (chunkA.DocumentId == chunkB.DocumentId)
        {
            return new RelationshipClassification(
                Type: RelationshipType.Complementary,
                Confidence: 0.70f,
                Explanation: options.IncludeExplanation
                    ? "Moderately similar chunks from the same document."
                    : null,
                Method: ClassificationMethod.RuleBased);
        }

        // LOGIC: Different documents with moderate similarity - uncertain.
        // Default to Complementary with lower confidence.
        return new RelationshipClassification(
            Type: RelationshipType.Complementary,
            Confidence: 0.60f,
            Explanation: options.IncludeExplanation
                ? $"Moderate similarity ({similarityScore:F2}) suggests related but not equivalent content."
                : null,
            Method: ClassificationMethod.RuleBased);
    }
}
