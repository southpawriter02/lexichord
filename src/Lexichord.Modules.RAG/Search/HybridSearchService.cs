// =============================================================================
// File: HybridSearchService.cs
// Project: Lexichord.Modules.RAG
// Description: Hybrid search service combining BM25 and semantic search via RRF.
// =============================================================================
// LOGIC: Implements IHybridSearchService by executing BM25 and semantic searches
//   in parallel and merging their results using Reciprocal Rank Fusion (RRF).
//
//   Search Pipeline:
//   1. Validate inputs (query non-empty, TopK 1-100, MinScore 0.0-1.0).
//   2. Check license tier via SearchLicenseGuard (WriterPro+ required).
//   3. Preprocess query via IQueryPreprocessor (normalize whitespace).
//   4. Execute BM25 and semantic searches in parallel (Task.WhenAll).
//      - Both sub-searches use expanded TopK (2× requested) for better fusion.
//   5. Apply Reciprocal Rank Fusion to merge ranked result lists.
//      - Chunks appearing in both lists get combined RRF scores.
//      - Score: RRF_score = Σ (weight_i / (k + rank_i))
//   6. Trim to requested TopK and assemble SearchResult.
//   7. Publish HybridSearchExecutedEvent for telemetry.
//
//   Chunk Identity:
//     Chunks are identified by the composite key (Document.Id, Chunk.Metadata.Index)
//     since TextChunk does not carry a database-level ID. This composite key
//     uniquely identifies a chunk within the indexed corpus.
//
//   Dependencies:
//     - v0.5.1b: IBM25SearchService (keyword search)
//     - v0.4.5a: ISemanticSearchService (vector search)
//     - v0.4.5b: SearchLicenseGuard (license validation)
//     - v0.4.5c: IQueryPreprocessor (query normalization)
//     - v0.5.1c: HybridSearchOptions, HybridSearchExecutedEvent
//     - v0.4.5a: SearchResult, SearchHit, SearchOptions
//     - v0.0.7a: IMediator (event publishing)
// =============================================================================

using System.Diagnostics;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lexichord.Modules.RAG.Search;

/// <summary>
/// Hybrid search service that combines BM25 keyword search and semantic vector
/// search using Reciprocal Rank Fusion (RRF).
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="HybridSearchService"/> implements <see cref="IHybridSearchService"/>
/// by executing both <see cref="ISemanticSearchService"/> and <see cref="IBM25SearchService"/>
/// in parallel, then merging their ranked result lists using the Reciprocal Rank Fusion
/// algorithm. This approach captures both exact keyword matches (BM25) and conceptual
/// similarity (semantic) in a single ranked result set.
/// </para>
/// <para>
/// <b>Architecture:</b> This service orchestrates the hybrid search pipeline:
/// </para>
/// <list type="number">
///   <item><description>Input validation (query text, search options).</description></item>
///   <item><description>License validation via <see cref="SearchLicenseGuard"/>.</description></item>
///   <item><description>Query preprocessing via <see cref="IQueryPreprocessor"/>.</description></item>
///   <item><description>Parallel execution of BM25 and semantic searches.</description></item>
///   <item><description>Reciprocal Rank Fusion to merge ranked results.</description></item>
///   <item><description>Telemetry event publishing via <see cref="IMediator"/>.</description></item>
/// </list>
/// <para>
/// <b>RRF Formula:</b>
/// <code>
/// RRF_score(chunk) = Σ (weight_i / (k + rank_i))
/// </code>
/// Where <c>weight_i</c> is <see cref="HybridSearchOptions.SemanticWeight"/> or
/// <see cref="HybridSearchOptions.BM25Weight"/>, <c>k</c> is
/// <see cref="HybridSearchOptions.RRFConstant"/>, and <c>rank_i</c> is the 1-based
/// position in that ranking (contributes 0 if the chunk is absent from that ranking).
/// </para>
/// <para>
/// <b>Chunk Identity:</b> Chunks are identified across result sets using a composite
/// key of (<see cref="Document.Id"/>, <see cref="ChunkMetadata.Index"/>), which
/// uniquely identifies each chunk in the indexed corpus.
/// </para>
/// <para>
/// <b>License Requirement:</b> Hybrid search requires
/// <see cref="LicenseTier.WriterPro"/> or higher. Core and Writer tier users
/// receive a <see cref="FeatureNotLicensedException"/>.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This class is safe for concurrent use when registered
/// as a scoped service (one instance per request scope).
/// </para>
/// <para>
/// <b>Introduced:</b> v0.5.1c.
/// </para>
/// </remarks>
public sealed class HybridSearchService : IHybridSearchService
{
    private readonly ISemanticSearchService _semanticSearch;
    private readonly IBM25SearchService _bm25Search;
    private readonly IQueryPreprocessor _preprocessor;
    private readonly SearchLicenseGuard _licenseGuard;
    private readonly IOptions<HybridSearchOptions> _options;
    private readonly IMediator _mediator;
    private readonly ILogger<HybridSearchService> _logger;

    /// <summary>
    /// Creates a new <see cref="HybridSearchService"/> instance.
    /// </summary>
    /// <param name="semanticSearch">Semantic (vector) search service for conceptual similarity matching.</param>
    /// <param name="bm25Search">BM25 (keyword) search service for full-text keyword matching.</param>
    /// <param name="preprocessor">Query preprocessor for normalization.</param>
    /// <param name="licenseGuard">License guard for WriterPro tier validation.</param>
    /// <param name="options">Configuration options for RRF weights and constants.</param>
    /// <param name="mediator">MediatR mediator for publishing search events.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    public HybridSearchService(
        ISemanticSearchService semanticSearch,
        IBM25SearchService bm25Search,
        IQueryPreprocessor preprocessor,
        SearchLicenseGuard licenseGuard,
        IOptions<HybridSearchOptions> options,
        IMediator mediator,
        ILogger<HybridSearchService> logger)
    {
        _semanticSearch = semanticSearch ?? throw new ArgumentNullException(nameof(semanticSearch));
        _bm25Search = bm25Search ?? throw new ArgumentNullException(nameof(bm25Search));
        _preprocessor = preprocessor ?? throw new ArgumentNullException(nameof(preprocessor));
        _licenseGuard = licenseGuard ?? throw new ArgumentNullException(nameof(licenseGuard));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// LOGIC: Orchestrates the full hybrid search pipeline from query validation through
    /// RRF fusion and result assembly. The method executes both sub-searches in parallel
    /// to minimize total latency.
    /// </para>
    /// <para>
    /// <b>Expanded TopK:</b> Both sub-searches use <c>options.TopK * 2</c> to retrieve
    /// a larger candidate pool for fusion. This improves recall by ensuring chunks that
    /// rank well in one system but not the other still participate in the fusion.
    /// </para>
    /// <para>
    /// <b>Error Handling:</b>
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="ArgumentException"/>: Invalid query text.</description></item>
    ///   <item><description><see cref="ArgumentOutOfRangeException"/>: Invalid TopK or MinScore.</description></item>
    ///   <item><description><see cref="FeatureNotLicensedException"/>: Insufficient license tier (re-thrown).</description></item>
    ///   <item><description>Other exceptions: Logged and re-thrown for caller handling.</description></item>
    /// </list>
    /// </remarks>
    public async Task<SearchResult> SearchAsync(
        string query,
        SearchOptions options,
        CancellationToken ct = default)
    {
        // Step 1: Validate inputs.
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ValidateOptions(options);

        // Step 2: Check license tier (throws FeatureNotLicensedException if unauthorized).
        // LOGIC: Fail fast before spawning parallel tasks. Both sub-services also
        // check internally, but this avoids wasted work on unauthorized requests.
        _licenseGuard.EnsureSearchAuthorized();

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Step 3: Preprocess query (normalize whitespace).
            // LOGIC: Normalize once and pass to both sub-searches for consistency.
            var processedQuery = _preprocessor.Process(query, options);

            _logger.LogDebug(
                "Hybrid search starting: Query='{Query}', TopK={TopK}, MinScore={MinScore}",
                processedQuery, options.TopK, options.MinScore);

            // Step 4: Execute both searches in parallel with expanded TopK.
            // LOGIC: Retrieve 2× the requested results from each sub-search to provide
            // a larger candidate pool for fusion. Chunks that rank highly in one system
            // but lower in another still participate in the RRF merge.
            var expandedTopK = Math.Min(options.TopK * 2, 100);
            var expandedOptions = options with { TopK = expandedTopK };

            _logger.LogDebug(
                "Executing parallel sub-searches: ExpandedTopK={ExpandedTopK} (original={OriginalTopK})",
                expandedTopK, options.TopK);

            var semanticTask = _semanticSearch.SearchAsync(processedQuery, expandedOptions, ct);
            var bm25Task = _bm25Search.SearchAsync(processedQuery, expandedOptions, ct);

            await Task.WhenAll(semanticTask, bm25Task);

            var semanticResult = semanticTask.Result;
            var bm25Result = bm25Task.Result;

            _logger.LogDebug(
                "Sub-searches completed: SemanticHits={SemanticHits}, BM25Hits={BM25Hits}",
                semanticResult.Hits.Count, bm25Result.Hits.Count);

            // Step 5: Apply Reciprocal Rank Fusion.
            var opts = _options.Value;
            var fusedHits = ApplyRRF(semanticResult.Hits, bm25Result.Hits, opts);

            // Step 6: Trim to requested TopK.
            var finalHits = fusedHits.Take(options.TopK).ToList();

            stopwatch.Stop();

            _logger.LogDebug(
                "RRF fusion applied: k={K}, weights={{semantic:{SemanticWeight}, bm25:{BM25Weight}}}, " +
                "unique chunks={UniqueChunks}, final={FinalCount}",
                opts.RRFConstant, opts.SemanticWeight, opts.BM25Weight,
                fusedHits.Count, finalHits.Count);

            _logger.LogInformation(
                "Hybrid search completed: semantic={SemanticHits}, bm25={BM25Hits}, " +
                "fused={FusedCount} in {ElapsedMs}ms",
                semanticResult.Hits.Count, bm25Result.Hits.Count,
                finalHits.Count, stopwatch.ElapsedMilliseconds);

            // Step 7: Publish telemetry event.
            await _mediator.Publish(new HybridSearchExecutedEvent
            {
                Query = query,
                SemanticHitCount = semanticResult.Hits.Count,
                BM25HitCount = bm25Result.Hits.Count,
                FusedResultCount = finalHits.Count,
                Duration = stopwatch.Elapsed,
                SemanticWeight = opts.SemanticWeight,
                BM25Weight = opts.BM25Weight,
                RRFConstant = opts.RRFConstant
            }, ct);

            // Step 8: Assemble and return the result.
            return new SearchResult
            {
                Hits = finalHits,
                Duration = stopwatch.Elapsed,
                QueryEmbedding = semanticResult.QueryEmbedding,
                Query = query,
                WasTruncated = finalHits.Count >= options.TopK
            };
        }
        catch (FeatureNotLicensedException)
        {
            // LOGIC: Re-throw license exceptions without wrapping.
            // These are expected control flow for unlicensed users.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hybrid search failed for query: '{Query}'", query);
            throw;
        }
    }

    /// <summary>
    /// Validates that the search options are within acceptable ranges.
    /// </summary>
    /// <param name="options">The search options to validate.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <see cref="SearchOptions.TopK"/> is not between 1 and 100,
    /// or when <see cref="SearchOptions.MinScore"/> is not between 0.0 and 1.0.
    /// </exception>
    /// <remarks>
    /// LOGIC: Validates runtime constraints that are not enforced by the record's
    /// init-only properties. These bounds prevent SQL abuse (unbounded LIMIT via
    /// sub-searches) and nonsensical score thresholds.
    /// </remarks>
    private static void ValidateOptions(SearchOptions options)
    {
        if (options.TopK is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(options), "TopK must be between 1 and 100");

        if (options.MinScore is < 0.0f or > 1.0f)
            throw new ArgumentOutOfRangeException(nameof(options), "MinScore must be between 0.0 and 1.0");
    }

    /// <summary>
    /// Applies Reciprocal Rank Fusion to merge two ranked result lists.
    /// </summary>
    /// <param name="semanticHits">Ranked hits from semantic (vector) search.</param>
    /// <param name="bm25Hits">Ranked hits from BM25 (keyword) search.</param>
    /// <param name="opts">RRF configuration (weights and k constant).</param>
    /// <returns>
    /// A list of <see cref="SearchHit"/> instances sorted by descending RRF score.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Reciprocal Rank Fusion assigns scores based on rank position rather than
    /// raw scores, making it robust when combining heterogeneous ranking systems
    /// (cosine similarity from semantic search vs. ts_rank from BM25).
    /// </para>
    /// <para>
    /// <b>Algorithm:</b>
    /// <list type="number">
    ///   <item><description>For each chunk in the semantic results, compute
    ///   <c>SemanticWeight / (k + rank)</c> where rank is 1-based.</description></item>
    ///   <item><description>For each chunk in the BM25 results, compute
    ///   <c>BM25Weight / (k + rank)</c> and add to existing score if present.</description></item>
    ///   <item><description>Sort all chunks by descending fused RRF score.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Chunk Identity:</b> Uses composite key (<see cref="Document.Id"/>,
    /// <see cref="ChunkMetadata.Index"/>) to identify the same chunk across result sets.
    /// </para>
    /// <para>
    /// <b>Overlap Handling:</b> Chunks appearing in both result sets receive RRF
    /// contributions from both rankings, naturally boosting items that are relevant
    /// to both keyword and conceptual queries.
    /// </para>
    /// </remarks>
    private List<SearchHit> ApplyRRF(
        IReadOnlyList<SearchHit> semanticHits,
        IReadOnlyList<SearchHit> bm25Hits,
        HybridSearchOptions opts)
    {
        var k = opts.RRFConstant;

        // LOGIC: Dictionary keyed by (DocumentId, ChunkIndex) to identify unique chunks.
        // Value tuple stores the accumulated RRF score and the SearchHit instance.
        var scores = new Dictionary<(Guid DocId, int ChunkIndex), (float Score, SearchHit Hit)>();

        // LOGIC: Score from semantic ranking (1-based rank).
        // Each hit contributes SemanticWeight / (k + rank) to its RRF score.
        for (int i = 0; i < semanticHits.Count; i++)
        {
            var hit = semanticHits[i];
            var chunkKey = (hit.Document.Id, hit.Chunk.Metadata.Index);
            var rrfScore = opts.SemanticWeight / (k + i + 1); // i+1 for 1-based rank
            scores[chunkKey] = (rrfScore, hit);
        }

        // LOGIC: Score from BM25 ranking (1-based rank).
        // Each hit contributes BM25Weight / (k + rank) to its RRF score.
        // If the chunk already exists from semantic results, add the BM25 contribution.
        for (int i = 0; i < bm25Hits.Count; i++)
        {
            var hit = bm25Hits[i];
            var chunkKey = (hit.Document.Id, hit.Chunk.Metadata.Index);
            var rrfScore = opts.BM25Weight / (k + i + 1); // i+1 for 1-based rank

            if (scores.TryGetValue(chunkKey, out var existing))
            {
                // LOGIC: Chunk appears in both result sets — add BM25 contribution.
                // This naturally boosts chunks that are both keyword-relevant and
                // semantically similar.
                scores[chunkKey] = (existing.Score + rrfScore, existing.Hit);
            }
            else
            {
                // LOGIC: Chunk only appears in BM25 results.
                // Use the BM25 SearchHit directly since it already contains
                // full Document and TextChunk data.
                scores[chunkKey] = (rrfScore, hit);
            }
        }

        // LOGIC: Sort by descending RRF score and return as list.
        // The highest-scoring chunks (appearing in both lists and/or ranked highly)
        // will be at the top of the fused result set.
        return scores
            .OrderByDescending(kvp => kvp.Value.Score)
            .Select(kvp => kvp.Value.Hit)
            .ToList();
    }
}
