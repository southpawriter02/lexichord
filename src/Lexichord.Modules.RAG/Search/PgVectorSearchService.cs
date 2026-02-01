// =============================================================================
// File: PgVectorSearchService.cs
// Project: Lexichord.Modules.RAG
// Description: Semantic search service using pgvector cosine similarity.
// =============================================================================
// LOGIC: Implements ISemanticSearchService using pgvector's <=> operator for
//   cosine distance-based vector similarity search.
//
//   Search Flow:
//   1. Validate inputs (query non-empty, TopK 1-100, MinScore 0.0-1.0).
//   2. Check license tier via SearchLicenseGuard (WriterPro+ required).
//   3. Preprocess query via IQueryPreprocessor (normalize, expand abbreviations).
//   4. Get query embedding (check cache first, then generate via IEmbeddingService).
//   5. Execute SQL with pgvector <=> operator for cosine similarity search.
//   6. Map Dapper rows to SearchHit objects with document caching.
//   7. Publish SemanticSearchExecutedEvent for telemetry.
//   8. Return SearchResult with ranked hits.
//
//   Score Calculation: Score = 1 - cosine_distance.
//     - 1.0 = identical vectors (perfect match)
//     - 0.0 = orthogonal vectors (no similarity)
//     - Default MinScore threshold: 0.7 (medium relevance)
//
//   Dependencies:
//     - v0.4.5a: ISemanticSearchService, SearchOptions, SearchResult, SearchHit
//     - v0.4.5b: IQueryPreprocessor, SearchLicenseGuard, SearchEvents
//     - v0.4.5d: UsedCachedEmbedding telemetry flag on SemanticSearchExecutedEvent
//     - v0.4.4a: IEmbeddingService for query embedding generation
//     - v0.4.1c: IDocumentRepository for document metadata
//     - v0.0.5b: IDbConnectionFactory for database connections
//     - v0.0.4c: ILicenseContext (via SearchLicenseGuard)
// =============================================================================

using System.Diagnostics;
using System.Text.Json;
using Dapper;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using MediatR;
using Microsoft.Extensions.Logging;
using DapperCommandDefinition = Dapper.CommandDefinition;

namespace Lexichord.Modules.RAG.Search;

/// <summary>
/// Semantic search service using pgvector cosine similarity.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="PgVectorSearchService"/> implements <see cref="ISemanticSearchService"/>
/// by converting the user's natural language query into an embedding vector and executing
/// a cosine similarity search against the indexed chunks stored in PostgreSQL with pgvector.
/// </para>
/// <para>
/// <b>Architecture:</b> This service orchestrates the search pipeline:
/// </para>
/// <list type="number">
///   <item><description>Input validation (query text, search options).</description></item>
///   <item><description>License validation via <see cref="SearchLicenseGuard"/>.</description></item>
///   <item><description>Query preprocessing via <see cref="IQueryPreprocessor"/>.</description></item>
///   <item><description>Embedding generation/retrieval via <see cref="IEmbeddingService"/>.</description></item>
///   <item><description>SQL execution using pgvector's <c>&lt;=&gt;</c> cosine distance operator.</description></item>
///   <item><description>Result mapping with document metadata caching.</description></item>
///   <item><description>Telemetry event publishing via <see cref="IMediator"/>.</description></item>
/// </list>
/// <para>
/// <b>SQL Query Pattern:</b>
/// </para>
/// <code>
/// SELECT *, 1 - (embedding &lt;=&gt; @query_embedding::vector) AS score
/// FROM chunks
/// WHERE score &gt;= @min_score
/// ORDER BY embedding &lt;=&gt; @query_embedding::vector ASC
/// LIMIT @top_k
/// </code>
/// <para>
/// <b>License Requirement:</b> Semantic search requires
/// <see cref="LicenseTier.WriterPro"/> or higher. Core and Writer tier users
/// receive a <see cref="FeatureNotLicensedException"/>.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This class is safe for concurrent use when registered
/// as a scoped service (one instance per request scope).
/// </para>
/// <para>
/// <b>Introduced:</b> v0.4.5b.
/// </para>
/// </remarks>
public sealed class PgVectorSearchService : ISemanticSearchService
{
    private readonly IDbConnectionFactory _dbFactory;
    private readonly IEmbeddingService _embedder;
    private readonly IQueryPreprocessor _preprocessor;
    private readonly IDocumentRepository _docRepo;
    private readonly SearchLicenseGuard _licenseGuard;
    private readonly IMediator _mediator;
    private readonly ILogger<PgVectorSearchService> _logger;

    /// <summary>
    /// Creates a new <see cref="PgVectorSearchService"/> instance.
    /// </summary>
    /// <param name="dbFactory">Factory for database connections to PostgreSQL with pgvector.</param>
    /// <param name="embedder">Embedding service for generating query vectors.</param>
    /// <param name="preprocessor">Query preprocessor for normalization and caching.</param>
    /// <param name="docRepo">Document repository for retrieving source document metadata.</param>
    /// <param name="licenseGuard">License guard for WriterPro tier validation.</param>
    /// <param name="mediator">MediatR mediator for publishing search events.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    public PgVectorSearchService(
        IDbConnectionFactory dbFactory,
        IEmbeddingService embedder,
        IQueryPreprocessor preprocessor,
        IDocumentRepository docRepo,
        SearchLicenseGuard licenseGuard,
        IMediator mediator,
        ILogger<PgVectorSearchService> logger)
    {
        _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
        _embedder = embedder ?? throw new ArgumentNullException(nameof(embedder));
        _preprocessor = preprocessor ?? throw new ArgumentNullException(nameof(preprocessor));
        _docRepo = docRepo ?? throw new ArgumentNullException(nameof(docRepo));
        _licenseGuard = licenseGuard ?? throw new ArgumentNullException(nameof(licenseGuard));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// LOGIC: Orchestrates the full search pipeline from query validation through
    /// result assembly. The method is structured as a linear pipeline with early
    /// exits for validation failures and license denials.
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
        _licenseGuard.EnsureSearchAuthorized();

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Step 3: Preprocess query (normalize whitespace, optionally expand abbreviations).
            var processedQuery = _preprocessor.Process(query, options);

            // Step 4: Get query embedding (cache check, then generate if needed).
            // LOGIC: v0.4.5d — destructure tuple to capture cache hit status for telemetry.
            var (queryEmbedding, usedCachedEmbedding) = await GetQueryEmbeddingAsync(processedQuery, options, ct);

            // Step 5: Execute vector similarity search against pgvector.
            var hits = await ExecuteVectorSearchAsync(queryEmbedding, options, ct);

            stopwatch.Stop();

            // Step 6: Publish telemetry event.
            // LOGIC: v0.4.5d — include UsedCachedEmbedding for cache efficiency tracking.
            await _mediator.Publish(new SemanticSearchExecutedEvent
            {
                Query = query,
                ResultCount = hits.Count,
                Duration = stopwatch.Elapsed,
                Timestamp = DateTimeOffset.UtcNow,
                UsedCachedEmbedding = usedCachedEmbedding
            }, ct);

            _logger.LogInformation(
                "Search completed: {HitCount} hits for '{Query}' in {Duration}ms",
                hits.Count, query, stopwatch.ElapsedMilliseconds);

            // Step 7: Assemble and return the result.
            return new SearchResult
            {
                Hits = hits,
                Duration = stopwatch.Elapsed,
                QueryEmbedding = queryEmbedding,
                Query = query,
                WasTruncated = hits.Count >= options.TopK
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
            _logger.LogError(ex, "Search failed for query: '{Query}'", query);
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
    /// init-only properties. These bounds prevent SQL abuse (unbounded LIMIT)
    /// and nonsensical score thresholds.
    /// </remarks>
    private static void ValidateOptions(SearchOptions options)
    {
        if (options.TopK is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(options), "TopK must be between 1 and 100");

        if (options.MinScore is < 0.0f or > 1.0f)
            throw new ArgumentOutOfRangeException(nameof(options), "MinScore must be between 0.0 and 1.0");
    }

    /// <summary>
    /// Gets the query embedding, checking the cache first if enabled.
    /// </summary>
    /// <param name="query">The preprocessed query text.</param>
    /// <param name="options">Search options controlling cache behavior.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A tuple containing the embedding vector and a flag indicating whether
    /// the embedding was served from cache (<c>true</c>) or freshly generated (<c>false</c>).
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Cache lookup flow:
    /// </para>
    /// <list type="number">
    ///   <item><description>If <see cref="SearchOptions.UseCache"/> is true, check the preprocessor cache.</description></item>
    ///   <item><description>On cache hit, return the cached embedding with <c>usedCache: true</c> (skip API call).</description></item>
    ///   <item><description>On cache miss, generate a new embedding via <see cref="IEmbeddingService"/>.</description></item>
    ///   <item><description>If caching is enabled, store the new embedding in the cache.</description></item>
    /// </list>
    /// <para>
    /// <b>v0.4.5d:</b> Return type changed from <c>float[]</c> to tuple to expose cache hit
    /// status for <see cref="SemanticSearchExecutedEvent.UsedCachedEmbedding"/> telemetry.
    /// </para>
    /// </remarks>
    private async Task<(float[] Embedding, bool UsedCache)> GetQueryEmbeddingAsync(
        string query,
        SearchOptions options,
        CancellationToken ct)
    {
        // LOGIC: Check cache first if caching is enabled.
        if (options.UseCache)
        {
            var cached = _preprocessor.GetCachedEmbedding(query);
            if (cached != null)
            {
                _logger.LogDebug("Using cached embedding for query: '{Query}'", query);
                return (cached, true);
            }
        }

        // LOGIC: Generate new embedding via the embedding service.
        var embedding = await _embedder.EmbedAsync(query, ct);

        // LOGIC: Cache the embedding for reuse if caching is enabled.
        if (options.UseCache)
        {
            _preprocessor.CacheEmbedding(query, embedding);
        }

        return (embedding, false);
    }

    /// <summary>
    /// Executes the vector similarity search against PostgreSQL with pgvector.
    /// </summary>
    /// <param name="queryEmbedding">The query embedding vector.</param>
    /// <param name="options">Search options (TopK, MinScore, DocumentFilter).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of <see cref="SearchHit"/> objects ranked by descending similarity.</returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Executes a Dapper query against the chunks table using pgvector's
    /// <c>&lt;=&gt;</c> cosine distance operator. Results are filtered by minimum
    /// score threshold and optionally by document ID.
    /// </para>
    /// <para>
    /// <b>Document Caching:</b> A <see cref="Dictionary{TKey,TValue}"/> caches document
    /// lookups during result mapping to avoid redundant database queries when multiple
    /// chunks belong to the same document.
    /// </para>
    /// <para>
    /// <b>Orphaned Chunks:</b> If a document is not found for a chunk (e.g., concurrent
    /// deletion), the chunk is skipped with a warning log.
    /// </para>
    /// </remarks>
    private async Task<IReadOnlyList<SearchHit>> ExecuteVectorSearchAsync(
        float[] queryEmbedding,
        SearchOptions options,
        CancellationToken ct)
    {
        await using var connection = await _dbFactory.CreateConnectionAsync(ct);

        var sql = BuildSearchQuery(options);

        _logger.LogDebug(
            "Executing vector search: TopK={TopK}, MinScore={MinScore}, Filter={Filter}",
            options.TopK, options.MinScore, options.DocumentFilter);

        // LOGIC: Build parameters using anonymous types (project convention).
        // The document filter parameter is conditionally included in the SQL WHERE clause.
        object parameters;
        if (options.DocumentFilter.HasValue)
        {
            parameters = new
            {
                query_embedding = queryEmbedding,
                min_score = options.MinScore,
                top_k = options.TopK,
                document_filter = options.DocumentFilter.Value
            };
        }
        else
        {
            parameters = new
            {
                query_embedding = queryEmbedding,
                min_score = options.MinScore,
                top_k = options.TopK
            };
        }

        var command = new DapperCommandDefinition(sql, parameters, cancellationToken: ct);
        var rows = await connection.QueryAsync<ChunkSearchRow>(command);

        // LOGIC: Map Dapper rows to SearchHit objects with document caching.
        // Dictionary avoids redundant GetByIdAsync calls when multiple chunks
        // belong to the same document.
        var hits = new List<SearchHit>();
        var documentCache = new Dictionary<Guid, Document>();

        foreach (var row in rows)
        {
            // LOGIC: Cache document lookups to avoid N+1 queries.
            if (!documentCache.TryGetValue(row.DocumentId, out var document))
            {
                document = await _docRepo.GetByIdAsync(row.DocumentId, ct);
                if (document == null)
                {
                    // LOGIC: Orphaned chunk — document may have been deleted concurrently.
                    _logger.LogWarning(
                        "Document {DocumentId} not found for chunk {ChunkId}. Skipping orphaned chunk.",
                        row.DocumentId, row.Id);
                    continue;
                }
                documentCache[row.DocumentId] = document;
            }

            // LOGIC: Construct ChunkMetadata from Heading/HeadingLevel columns.
            // Fall back to ChunkIndex-only metadata if heading columns are null.
            var metadata = new ChunkMetadata(
                row.ChunkIndex,
                row.Heading,
                row.HeadingLevel ?? 0);

            hits.Add(new SearchHit
            {
                Chunk = new TextChunk(
                    row.Content,
                    row.StartOffset,
                    row.EndOffset,
                    metadata),
                Document = document,
                Score = row.Score
            });
        }

        if (hits.Count == 0)
        {
            _logger.LogWarning("Search returned no results");
        }

        return hits;
    }

    /// <summary>
    /// Builds the SQL query for vector similarity search with optional document filtering.
    /// </summary>
    /// <param name="options">Search options controlling the query shape.</param>
    /// <returns>The parameterized SQL query string.</returns>
    /// <remarks>
    /// <para>
    /// LOGIC: The SQL uses pgvector's <c>&lt;=&gt;</c> operator for cosine distance.
    /// The score is computed as <c>1 - cosine_distance</c> so that higher values
    /// indicate greater similarity (1.0 = identical, 0.0 = orthogonal).
    /// </para>
    /// <para>
    /// <b>Index Usage:</b> The <c>ORDER BY c."Embedding" &lt;=&gt; @query_embedding::vector</c>
    /// clause leverages the HNSW index on the Embedding column for efficient approximate
    /// nearest neighbor search.
    /// </para>
    /// <para>
    /// <b>Column Aliasing:</b> Uses double-quoted aliases (e.g., <c>AS "Id"</c>) to match
    /// the <see cref="ChunkSearchRow"/> property names for Dapper mapping, following the
    /// established convention in <c>ChunkRepository</c>.
    /// </para>
    /// </remarks>
    private static string BuildSearchQuery(SearchOptions options)
    {
        // LOGIC: Conditionally include document filter clause.
        var filterClause = options.DocumentFilter.HasValue
            ? @"AND c.""DocumentId"" = @document_filter"
            : "";

        // LOGIC: pgvector cosine distance: <=> operator.
        // Score = 1 - distance (higher is more similar).
        // Filter by minimum score threshold and optional document ID.
        // Order by distance ascending (most similar first).
        return $@"
            SELECT
                c.""Id"" AS ""Id"",
                c.""DocumentId"" AS ""DocumentId"",
                c.""Content"" AS ""Content"",
                c.""ChunkIndex"" AS ""ChunkIndex"",
                c.""Metadata"" AS ""Metadata"",
                c.""Heading"" AS ""Heading"",
                c.""HeadingLevel"" AS ""HeadingLevel"",
                COALESCE(c.""StartOffset"", 0) AS ""StartOffset"",
                COALESCE(c.""EndOffset"", 0) AS ""EndOffset"",
                1 - (c.""Embedding"" <=> @query_embedding::vector) AS ""Score""
            FROM ""Chunks"" c
            WHERE c.""Embedding"" IS NOT NULL
              AND 1 - (c.""Embedding"" <=> @query_embedding::vector) >= @min_score
              {filterClause}
            ORDER BY c.""Embedding"" <=> @query_embedding::vector ASC
            LIMIT @top_k";
    }
}
