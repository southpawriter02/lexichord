// =============================================================================
// File: ResilientSearchService.cs
// Project: Lexichord.Modules.RAG
// Description: Resilient search service with automatic fallback and circuit breaking.
// =============================================================================
// VERSION: v0.5.8d (Error Resilience)
// =============================================================================
// DEPENDENCIES:
//   - IHybridSearchService (v0.5.1c): Primary search service
//   - IBM25SearchService (v0.5.1b): BM25 fallback
//   - IQueryResultCache (v0.5.8c): Cache fallback
//   - CacheKeyGenerator (v0.5.8c): Cache key generation
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Configuration;
using Lexichord.Modules.RAG.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Lexichord.Modules.RAG.Resilience;

/// <summary>
/// Provides resilient search operations with automatic fallback and circuit breaking.
/// </summary>
/// <remarks>
/// <para>
/// This service decorates the hybrid search service with resilience patterns:
/// <list type="bullet">
///   <item><description>Check cache first for repeated queries</description></item>
///   <item><description>Execute hybrid search through Polly pipeline</description></item>
///   <item><description>Fall back to BM25 on embedding/HTTP failures</description></item>
///   <item><description>Fall back to cache on database failures</description></item>
/// </list>
/// </para>
/// <para>
/// The degradation hierarchy is:
/// <code>
/// Hybrid (Full) → BM25 (KeywordOnly) → Cache (CachedOnly) → Unavailable
/// </code>
/// </para>
/// </remarks>
public sealed class ResilientSearchService : IResilientSearchService
{
    private readonly IHybridSearchService _hybridSearch;
    private readonly IBM25SearchService _bm25Search;
    private readonly IQueryResultCache _queryCache;
    private readonly CacheKeyGenerator _cacheKeyGenerator;
    private readonly ResilienceOptions _options;
    private readonly ILogger<ResilientSearchService> _logger;
    private readonly ResiliencePipeline<SearchResult> _resiliencePipeline;

    private CircuitBreakerState _circuitState = CircuitBreakerState.Closed;
    private bool _embeddingApiAvailable = true;
    private bool _databaseAvailable = true;
    private DateTimeOffset _lastHealthCheck = DateTimeOffset.UtcNow;

    /// <summary>
    /// Initializes a new instance of <see cref="ResilientSearchService"/>.
    /// </summary>
    /// <param name="hybridSearch">The hybrid search service to decorate.</param>
    /// <param name="bm25Search">The BM25 search service for keyword fallback.</param>
    /// <param name="queryCache">The query result cache for cache fallback.</param>
    /// <param name="cacheKeyGenerator">Generator for cache keys.</param>
    /// <param name="options">Resilience configuration options.</param>
    /// <param name="logger">Logger instance.</param>
    public ResilientSearchService(
        IHybridSearchService hybridSearch,
        IBM25SearchService bm25Search,
        IQueryResultCache queryCache,
        CacheKeyGenerator cacheKeyGenerator,
        IOptions<ResilienceOptions> options,
        ILogger<ResilientSearchService> logger)
    {
        _hybridSearch = hybridSearch ?? throw new ArgumentNullException(nameof(hybridSearch));
        _bm25Search = bm25Search ?? throw new ArgumentNullException(nameof(bm25Search));
        _queryCache = queryCache ?? throw new ArgumentNullException(nameof(queryCache));
        _cacheKeyGenerator = cacheKeyGenerator ?? throw new ArgumentNullException(nameof(cacheKeyGenerator));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _resiliencePipeline = ResiliencePipelineBuilder.Build(
            _options,
            _logger,
            OnCircuitStateChanged);

        _logger.LogInformation("ResilientSearchService initialized with resilience policies");
    }

    /// <inheritdoc/>
    public async Task<ResilientSearchResult> SearchAsync(
        string query,
        SearchOptions options,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        ArgumentNullException.ThrowIfNull(options);

        _lastHealthCheck = DateTimeOffset.UtcNow;

        // Check cache first for repeated queries
        var cacheKey = _cacheKeyGenerator.GenerateKey(query, options);
        if (_queryCache.TryGet(cacheKey, out var cachedResult))
        {
            _logger.LogDebug("Cache hit for query: {Query}", query);
            return ResilientSearchResult.Success(cachedResult!, SearchMode.Hybrid, isFromCache: true);
        }

        // Check circuit breaker state - if open, go directly to fallback
        if (_circuitState == CircuitBreakerState.Open)
        {
            _logger.LogWarning("Circuit breaker open, using BM25 fallback for query: {Query}", query);
            return await FallbackToBM25Async(query, options, "Circuit breaker open", ct);
        }

        try
        {
            // Attempt hybrid search with resilience pipeline
            var result = await _resiliencePipeline.ExecuteAsync(
                async token =>
                {
                    var searchResult = await _hybridSearch.SearchAsync(query, options, token);
                    return searchResult;
                },
                ct);

            // Mark services as available
            _embeddingApiAvailable = true;
            _databaseAvailable = true;

            // Cache successful result
            var documentIds = result.Hits.Select(h => h.Document.Id).Distinct().ToList();
            _queryCache.Set(cacheKey, result, documentIds);

            _logger.LogDebug("Hybrid search succeeded for query: {Query}, hits: {HitCount}", query, result.Hits.Count);
            return ResilientSearchResult.Success(result, SearchMode.Hybrid);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogWarning(ex, "Circuit breaker triggered for query: {Query}", query);
            _circuitState = CircuitBreakerState.Open;
            _embeddingApiAvailable = false;
            return await FallbackToBM25Async(query, options, "Embedding service unavailable", ct);
        }
        catch (TimeoutRejectedException ex)
        {
            _logger.LogWarning(ex, "Timeout during hybrid search for query: {Query}", query);
            return await FallbackToBM25Async(query, options, "Search timed out", ct);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "HTTP error during hybrid search for query: {Query}", query);
            _embeddingApiAvailable = false;
            return await FallbackToBM25Async(query, options, "Embedding service unavailable", ct);
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "Database error during hybrid search for query: {Query}", query);
            _databaseAvailable = false;
            return await FallbackToCacheOnlyAsync(query, cacheKey, "Database unavailable");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Unexpected error during hybrid search for query: {Query}", query);
            return await FallbackToBM25Async(query, options, "Search temporarily unavailable", ct);
        }
    }

    /// <inheritdoc/>
    public SearchHealthStatus GetHealthStatus()
    {
        var cacheAvailable = true; // In-memory cache is always available if we reach this point

        var mode = (_embeddingApiAvailable, _databaseAvailable) switch
        {
            (true, true) => DegradedSearchMode.Full,
            (false, true) => DegradedSearchMode.KeywordOnly,
            (_, false) when cacheAvailable => DegradedSearchMode.CachedOnly,
            _ => DegradedSearchMode.Unavailable
        };

        return new SearchHealthStatus(
            EmbeddingApiAvailable: _embeddingApiAvailable,
            DatabaseAvailable: _databaseAvailable,
            CacheAvailable: cacheAvailable,
            CircuitBreakerState: _circuitState,
            LastHealthCheck: _lastHealthCheck,
            CurrentMode: mode);
    }

    /// <inheritdoc/>
    public void ResetCircuitBreaker()
    {
        _logger.LogInformation("Circuit breaker manually reset");
        _circuitState = CircuitBreakerState.Closed;
        _embeddingApiAvailable = true;
        _databaseAvailable = true;
    }

    private async Task<ResilientSearchResult> FallbackToBM25Async(
        string query,
        SearchOptions options,
        string reason,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Falling back to BM25 search for query: {Query}", query);
            var result = await _bm25Search.SearchAsync(query, options, ct);

            // Cache the BM25 result too
            var cacheKey = _cacheKeyGenerator.GenerateKey(query, options);
            var documentIds = result.Hits.Select(h => h.Document.Id).Distinct().ToList();
            _queryCache.Set(cacheKey, result, documentIds);

            var healthStatus = SearchHealthStatus.EmbeddingUnavailable(_circuitState);
            return ResilientSearchResult.KeywordFallback(result, reason, healthStatus);
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex, "Database error during BM25 fallback for query: {Query}", query);
            _databaseAvailable = false;
            var cacheKey = _cacheKeyGenerator.GenerateKey(query, options);
            return await FallbackToCacheOnlyAsync(query, cacheKey, "Database unavailable");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error during BM25 fallback for query: {Query}", query);
            var cacheKey = _cacheKeyGenerator.GenerateKey(query, options);
            return await FallbackToCacheOnlyAsync(query, cacheKey, "Search temporarily unavailable");
        }
    }

    private Task<ResilientSearchResult> FallbackToCacheOnlyAsync(
        string query,
        string cacheKey,
        string reason)
    {
        // Try to get any cached result
        if (_queryCache.TryGet(cacheKey, out var cachedResult))
        {
            _logger.LogInformation("Serving cached result for query: {Query}", query);
            var healthStatus = SearchHealthStatus.DatabaseUnavailable();
            return Task.FromResult(ResilientSearchResult.CachedFallback(cachedResult!, reason, healthStatus));
        }

        // No cache available - return unavailable
        _logger.LogWarning("No cached result available for query: {Query}", query);
        return Task.FromResult(ResilientSearchResult.Unavailable(query, "Search service temporarily unavailable"));
    }

    private void OnCircuitStateChanged(CircuitBreakerState newState)
    {
        _circuitState = newState;
        _logger.LogInformation("Circuit breaker state changed to: {State}", newState);

        if (newState == CircuitBreakerState.Closed)
        {
            _embeddingApiAvailable = true;
        }
    }
}
