// =============================================================================
// File: IResilientSearchService.cs
// Project: Lexichord.Abstractions
// Description: Interface for resilient search operations with graceful degradation.
// =============================================================================
// VERSION: v0.5.8d (Error Resilience)
// =============================================================================
// DEPENDENCIES:
//   - IHybridSearchService (v0.5.1c): Primary search service to decorate
//   - IBM25SearchService (v0.5.1b): Fallback for keyword-only search
//   - IQueryResultCache (v0.5.8c): Cache for fallback results
// =============================================================================
// LICENSE GATING: Requires WriterPro tier (same as IHybridSearchService)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Provides resilient search operations with automatic fallback and circuit breaking.
/// </summary>
/// <remarks>
/// <para>
/// This interface decorates the hybrid search service with resilience patterns:
/// <list type="bullet">
///   <item><description>Retry with exponential backoff for transient failures</description></item>
///   <item><description>Circuit breaker to prevent cascading failures</description></item>
///   <item><description>Fallback to BM25 keyword search when embedding API is unavailable</description></item>
///   <item><description>Fallback to cached results when database is unavailable</description></item>
/// </list>
/// </para>
/// <para>
/// The degradation hierarchy is:
/// <code>
/// Hybrid (Full) → BM25 (KeywordOnly) → Cache (CachedOnly) → Unavailable
/// </code>
/// </para>
/// <para>
/// <b>License Gating:</b> This service requires the WriterPro tier.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = await resilientSearch.SearchAsync("query", options, ct);
/// if (result.IsDegraded)
/// {
///     logger.LogWarning("Search degraded: {Reason}", result.DegradationReason);
///     // Show degradation indicator in UI
/// }
/// </code>
/// </example>
public interface IResilientSearchService
{
    /// <summary>
    /// Executes a search with automatic fallback on failure.
    /// </summary>
    /// <param name="query">The search query text.</param>
    /// <param name="options">Search configuration options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="ResilientSearchResult"/> containing the search results
    /// and metadata about how the search was executed.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The method attempts operations in this order:
    /// <list type="number">
    ///   <item><description>Check cache for existing results</description></item>
    ///   <item><description>Execute hybrid search with resilience pipeline</description></item>
    ///   <item><description>Fall back to BM25 on embedding/HTTP failures</description></item>
    ///   <item><description>Fall back to cache on database failures</description></item>
    ///   <item><description>Return unavailable if all options exhausted</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    Task<ResilientSearchResult> SearchAsync(
        string query,
        SearchOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the current health status of search dependencies.
    /// </summary>
    /// <returns>A <see cref="SearchHealthStatus"/> snapshot.</returns>
    /// <remarks>
    /// This method returns immediately without performing health checks.
    /// The status reflects the last known state based on recent operations.
    /// </remarks>
    SearchHealthStatus GetHealthStatus();

    /// <summary>
    /// Manually resets the circuit breaker to closed state.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this method when you know the underlying service has recovered
    /// and want to immediately resume normal operations without waiting
    /// for the break duration to expire.
    /// </para>
    /// <para>
    /// <b>Caution:</b> Calling this while the service is still failing
    /// may cause a brief spike in failed requests before the circuit
    /// opens again.
    /// </para>
    /// </remarks>
    void ResetCircuitBreaker();
}
