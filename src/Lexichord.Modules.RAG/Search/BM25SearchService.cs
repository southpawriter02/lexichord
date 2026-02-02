// =============================================================================
// File: BM25SearchService.cs
// Project: Lexichord.Modules.RAG
// Description: BM25 keyword search service using PostgreSQL full-text search.
// =============================================================================
// LOGIC: Implements IBM25SearchService using PostgreSQL's ts_rank() function for
//   BM25-style keyword ranking against the ContentTsVector column.
//
//   Search Flow:
//   1. Validate inputs (query non-empty, TopK 1-100, MinScore 0.0-1.0).
//   2. Check license tier via SearchLicenseGuard (WriterPro+ required).
//   3. Preprocess query via IQueryPreprocessor (normalize whitespace).
//   4. Execute SQL with plainto_tsquery and ts_rank for BM25-style scoring.
//   5. Map Dapper rows to SearchHit objects with document caching.
//   6. Publish BM25SearchExecutedEvent for telemetry.
//   7. Return SearchResult with ranked hits.
//
//   Score Range: ts_rank returns values typically between 0.0 and 1.0, though
//   values can exceed 1.0 for very high term frequency. The default MinScore
//   of 0.0 accepts all matches; callers can raise this for stricter filtering.
//
//   Dependencies:
//     - v0.5.1a: ContentTsVector column and GIN index on Chunks table
//     - v0.5.1b: IBM25SearchService, BM25SearchExecutedEvent
//     - v0.4.5b: IQueryPreprocessor, SearchLicenseGuard
//     - v0.4.5a: SearchOptions, SearchResult, SearchHit
//     - v0.4.1c: IDocumentRepository for document metadata
//     - v0.0.5b: IDbConnectionFactory for database connections
//     - v0.0.4c: ILicenseContext (via SearchLicenseGuard)
// =============================================================================

using System.Diagnostics;
using Dapper;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using MediatR;
using Microsoft.Extensions.Logging;
using DapperCommandDefinition = Dapper.CommandDefinition;

namespace Lexichord.Modules.RAG.Search;

/// <summary>
/// BM25 keyword search service using PostgreSQL full-text search.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="BM25SearchService"/> implements <see cref="IBM25SearchService"/>
/// by converting the user's keyword query into a tsquery and executing a full-text
/// search against the ContentTsVector column using PostgreSQL's ts_rank function.
/// </para>
/// <para>
/// <b>Architecture:</b> This service orchestrates the search pipeline:
/// </para>
/// <list type="number">
///   <item><description>Input validation (query text, search options).</description></item>
///   <item><description>License validation via <see cref="SearchLicenseGuard"/>.</description></item>
///   <item><description>Query preprocessing via <see cref="IQueryPreprocessor"/>.</description></item>
///   <item><description>SQL execution using plainto_tsquery and ts_rank.</description></item>
///   <item><description>Result mapping with document metadata caching.</description></item>
///   <item><description>Telemetry event publishing via <see cref="IMediator"/>.</description></item>
/// </list>
/// <para>
/// <b>SQL Query Pattern:</b>
/// </para>
/// <code>
/// SELECT *, ts_rank(c."ContentTsVector", plainto_tsquery('english', @query)) AS score
/// FROM "Chunks" c
/// WHERE c."ContentTsVector" @@ plainto_tsquery('english', @query)
///   AND ts_rank(...) &gt;= @min_score
/// ORDER BY score DESC
/// LIMIT @top_k
/// </code>
/// <para>
/// <b>License Requirement:</b> BM25 search requires
/// <see cref="LicenseTier.WriterPro"/> or higher. Core and Writer tier users
/// receive a <see cref="FeatureNotLicensedException"/>.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This class is safe for concurrent use when registered
/// as a scoped service (one instance per request scope).
/// </para>
/// <para>
/// <b>Introduced:</b> v0.5.1b.
/// </para>
/// </remarks>
public sealed class BM25SearchService : IBM25SearchService
{
    private readonly IDbConnectionFactory _dbFactory;
    private readonly IQueryPreprocessor _preprocessor;
    private readonly IDocumentRepository _docRepo;
    private readonly SearchLicenseGuard _licenseGuard;
    private readonly IMediator _mediator;
    private readonly ILogger<BM25SearchService> _logger;

    /// <summary>
    /// Creates a new <see cref="BM25SearchService"/> instance.
    /// </summary>
    /// <param name="dbFactory">Factory for database connections to PostgreSQL.</param>
    /// <param name="preprocessor">Query preprocessor for normalization.</param>
    /// <param name="docRepo">Document repository for retrieving source document metadata.</param>
    /// <param name="licenseGuard">License guard for WriterPro tier validation.</param>
    /// <param name="mediator">MediatR mediator for publishing search events.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    public BM25SearchService(
        IDbConnectionFactory dbFactory,
        IQueryPreprocessor preprocessor,
        IDocumentRepository docRepo,
        SearchLicenseGuard licenseGuard,
        IMediator mediator,
        ILogger<BM25SearchService> logger)
    {
        _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
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
            // Step 3: Preprocess query (normalize whitespace).
            // LOGIC: BM25 search does not use abbreviation expansion or embedding cache,
            // but we still normalize whitespace for consistency.
            var processedQuery = _preprocessor.Process(query, options);

            _logger.LogDebug(
                "Executing BM25 search: Query='{Query}', TopK={TopK}, MinScore={MinScore}",
                processedQuery, options.TopK, options.MinScore);

            // Step 4: Execute BM25 full-text search against PostgreSQL.
            var hits = await ExecuteBM25SearchAsync(processedQuery, options, ct);

            stopwatch.Stop();

            // Step 5: Publish telemetry event.
            await _mediator.Publish(new BM25SearchExecutedEvent
            {
                Query = query,
                ResultCount = hits.Count,
                Duration = stopwatch.Elapsed,
                Timestamp = DateTimeOffset.UtcNow
            }, ct);

            _logger.LogInformation(
                "BM25 search completed: {HitCount} hits for '{Query}' in {Duration}ms",
                hits.Count, query, stopwatch.ElapsedMilliseconds);

            // Step 6: Assemble and return the result.
            return new SearchResult
            {
                Hits = hits,
                Duration = stopwatch.Elapsed,
                QueryEmbedding = null, // BM25 doesn't use embeddings
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
            _logger.LogError(ex, "BM25 search failed for query: '{Query}'", query);
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
    /// Executes the BM25 full-text search against PostgreSQL.
    /// </summary>
    /// <param name="query">The preprocessed query text.</param>
    /// <param name="options">Search options (TopK, MinScore, DocumentFilter).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of <see cref="SearchHit"/> objects ranked by descending ts_rank score.</returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Executes a Dapper query against the chunks table using PostgreSQL's
    /// full-text search operators. Results are filtered by minimum score threshold
    /// and optionally by document ID.
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
    private async Task<IReadOnlyList<SearchHit>> ExecuteBM25SearchAsync(
        string query,
        SearchOptions options,
        CancellationToken ct)
    {
        await using var connection = await _dbFactory.CreateConnectionAsync(ct);

        var sql = BuildSearchQuery(options);

        _logger.LogDebug(
            "Executing BM25 SQL: TopK={TopK}, MinScore={MinScore}, Filter={Filter}",
            options.TopK, options.MinScore, options.DocumentFilter);

        // LOGIC: Build parameters using anonymous types (project convention).
        // The document filter parameter is conditionally included in the SQL WHERE clause.
        object parameters;
        if (options.DocumentFilter.HasValue)
        {
            parameters = new
            {
                query,
                min_score = options.MinScore,
                top_k = options.TopK,
                document_filter = options.DocumentFilter.Value
            };
        }
        else
        {
            parameters = new
            {
                query,
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
            _logger.LogWarning("BM25 search returned no results for query: '{Query}'", query);
        }

        return hits;
    }

    /// <summary>
    /// Builds the SQL query for BM25 full-text search with optional document filtering.
    /// </summary>
    /// <param name="options">Search options controlling the query shape.</param>
    /// <returns>The parameterized SQL query string.</returns>
    /// <remarks>
    /// <para>
    /// LOGIC: The SQL uses PostgreSQL's full-text search operators:
    /// - <c>plainto_tsquery('english', @query)</c>: Converts plain text to tsquery.
    /// - <c>@@</c>: Full-text match operator.
    /// - <c>ts_rank()</c>: BM25-style relevance ranking.
    /// </para>
    /// <para>
    /// <b>Index Usage:</b> The <c>WHERE c."ContentTsVector" @@ plainto_tsquery(...)</c>
    /// clause leverages the GIN index on ContentTsVector for efficient full-text matching.
    /// </para>
    /// <para>
    /// <b>Column Aliasing:</b> Uses double-quoted aliases (e.g., <c>AS "Id"</c>) to match
    /// the <see cref="ChunkSearchRow"/> property names for Dapper mapping, following the
    /// established convention in <c>PgVectorSearchService</c>.
    /// </para>
    /// </remarks>
    private static string BuildSearchQuery(SearchOptions options)
    {
        // LOGIC: Conditionally include document filter clause.
        var filterClause = options.DocumentFilter.HasValue
            ? @"AND c.""DocumentId"" = @document_filter"
            : "";

        // LOGIC: PostgreSQL full-text search with ts_rank scoring.
        // plainto_tsquery converts plain text to tsquery (handles stopwords, stemming).
        // ts_rank returns relevance score based on term frequency and document length.
        // Filter by minimum score threshold and optional document ID.
        // Order by score descending (most relevant first).
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
                ts_rank(c.""ContentTsVector"", plainto_tsquery('english', @query)) AS ""Score""
            FROM ""Chunks"" c
            WHERE c.""ContentTsVector"" @@ plainto_tsquery('english', @query)
              AND ts_rank(c.""ContentTsVector"", plainto_tsquery('english', @query)) >= @min_score
              {filterClause}
            ORDER BY ts_rank(c.""ContentTsVector"", plainto_tsquery('english', @query)) DESC
            LIMIT @top_k";
    }
}
