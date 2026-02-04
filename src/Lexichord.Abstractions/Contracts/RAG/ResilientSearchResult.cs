// =============================================================================
// File: ResilientSearchResult.cs
// Project: Lexichord.Abstractions
// Description: Search result wrapper with resilience metadata.
// =============================================================================
// VERSION: v0.5.8d (Error Resilience)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Wraps a <see cref="SearchResult"/> with additional resilience metadata.
/// </summary>
/// <remarks>
/// <para>
/// This record is returned by <see cref="IResilientSearchService.SearchAsync"/>
/// to provide consumers with information about how the search was executed
/// and whether any fallback mechanisms were activated.
/// </para>
/// <para>
/// The <see cref="ActualMode"/> indicates how the search was actually performed,
/// which may differ from the requested mode if degradation occurred:
/// <list type="bullet">
///   <item><description><see cref="SearchMode.Hybrid"/>: Full semantic + keyword search</description></item>
///   <item><description><see cref="SearchMode.Keyword"/>: BM25 keyword-only fallback</description></item>
/// </list>
/// </para>
/// </remarks>
/// <param name="Result">The search result containing matched documents.</param>
/// <param name="ActualMode">The search mode that was actually used.</param>
/// <param name="IsDegraded">Whether the search operated in a degraded mode.</param>
/// <param name="DegradationReason">Human-readable explanation if degraded, null otherwise.</param>
/// <param name="IsFromCache">Whether the result was served from cache.</param>
/// <param name="HealthStatus">Current health status of search dependencies.</param>
public record ResilientSearchResult(
    SearchResult Result,
    SearchMode ActualMode,
    bool IsDegraded,
    string? DegradationReason,
    bool IsFromCache,
    SearchHealthStatus HealthStatus)
{
    /// <summary>
    /// Creates a successful non-degraded result.
    /// </summary>
    /// <param name="result">The search result.</param>
    /// <param name="mode">The search mode used.</param>
    /// <param name="isFromCache">Whether the result came from cache.</param>
    /// <returns>A successful <see cref="ResilientSearchResult"/>.</returns>
    public static ResilientSearchResult Success(SearchResult result, SearchMode mode, bool isFromCache = false) => new(
        Result: result,
        ActualMode: mode,
        IsDegraded: false,
        DegradationReason: null,
        IsFromCache: isFromCache,
        HealthStatus: SearchHealthStatus.Healthy());

    /// <summary>
    /// Creates a degraded result with BM25 fallback.
    /// </summary>
    /// <param name="result">The BM25 search result.</param>
    /// <param name="reason">The reason for degradation.</param>
    /// <param name="healthStatus">The current health status.</param>
    /// <returns>A degraded <see cref="ResilientSearchResult"/>.</returns>
    public static ResilientSearchResult KeywordFallback(
        SearchResult result,
        string reason,
        SearchHealthStatus healthStatus) => new(
        Result: result,
        ActualMode: SearchMode.Keyword,
        IsDegraded: true,
        DegradationReason: reason,
        IsFromCache: false,
        HealthStatus: healthStatus);

    /// <summary>
    /// Creates a cached-only result.
    /// </summary>
    /// <param name="result">The cached search result.</param>
    /// <param name="reason">The reason for serving from cache.</param>
    /// <param name="healthStatus">The current health status.</param>
    /// <returns>A cached <see cref="ResilientSearchResult"/>.</returns>
    public static ResilientSearchResult CachedFallback(
        SearchResult result,
        string reason,
        SearchHealthStatus healthStatus) => new(
        Result: result,
        ActualMode: SearchMode.Hybrid, // Original mode preserved
        IsDegraded: true,
        DegradationReason: reason,
        IsFromCache: true,
        HealthStatus: healthStatus);

    /// <summary>
    /// Creates an unavailable result with empty hits.
    /// </summary>
    /// <param name="query">The original query.</param>
    /// <param name="reason">The reason for unavailability.</param>
    /// <returns>An unavailable <see cref="ResilientSearchResult"/>.</returns>
    public static ResilientSearchResult Unavailable(string query, string reason) => new(
        Result: new SearchResult
        {
            Hits = [],
            Query = query,
            WasTruncated = false,
            Duration = TimeSpan.Zero
        },
        ActualMode: SearchMode.Keyword,
        IsDegraded: true,
        DegradationReason: reason,
        IsFromCache: false,
        HealthStatus: SearchHealthStatus.AllUnavailable());
}
