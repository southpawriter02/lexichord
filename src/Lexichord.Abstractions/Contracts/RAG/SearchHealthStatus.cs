// =============================================================================
// File: SearchHealthStatus.cs
// Project: Lexichord.Abstractions
// Description: Health status snapshot of search service dependencies.
// =============================================================================
// VERSION: v0.5.8d (Error Resilience)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Provides a snapshot of the health status of search service dependencies.
/// </summary>
/// <remarks>
/// <para>
/// This record is used by <see cref="IResilientSearchService"/> to report the
/// current operational state of the search subsystem, including the availability
/// of external services and the circuit breaker state.
/// </para>
/// <para>
/// The <see cref="CurrentMode"/> property reflects the effective search mode
/// based on the availability of dependencies:
/// <list type="bullet">
///   <item><description>All available → <see cref="DegradedSearchMode.Full"/></description></item>
///   <item><description>Embedding unavailable → <see cref="DegradedSearchMode.KeywordOnly"/></description></item>
///   <item><description>Database unavailable → <see cref="DegradedSearchMode.CachedOnly"/></description></item>
///   <item><description>All unavailable → <see cref="DegradedSearchMode.Unavailable"/></description></item>
/// </list>
/// </para>
/// </remarks>
/// <param name="EmbeddingApiAvailable">Whether the embedding API is reachable and operational.</param>
/// <param name="DatabaseAvailable">Whether the PostgreSQL database is reachable.</param>
/// <param name="CacheAvailable">Whether the query result cache is operational.</param>
/// <param name="CircuitBreakerState">Current state of the circuit breaker.</param>
/// <param name="LastHealthCheck">Timestamp of the last health check.</param>
/// <param name="CurrentMode">The effective degraded search mode based on availability.</param>
public record SearchHealthStatus(
    bool EmbeddingApiAvailable,
    bool DatabaseAvailable,
    bool CacheAvailable,
    CircuitBreakerState CircuitBreakerState,
    DateTimeOffset LastHealthCheck,
    DegradedSearchMode CurrentMode)
{
    /// <summary>
    /// Creates a healthy status indicating all services are operational.
    /// </summary>
    /// <returns>A <see cref="SearchHealthStatus"/> in <see cref="DegradedSearchMode.Full"/> mode.</returns>
    public static SearchHealthStatus Healthy() => new(
        EmbeddingApiAvailable: true,
        DatabaseAvailable: true,
        CacheAvailable: true,
        CircuitBreakerState: CircuitBreakerState.Closed,
        LastHealthCheck: DateTimeOffset.UtcNow,
        CurrentMode: DegradedSearchMode.Full);

    /// <summary>
    /// Creates a status indicating the embedding API is unavailable.
    /// </summary>
    /// <param name="circuitState">The current circuit breaker state.</param>
    /// <returns>A <see cref="SearchHealthStatus"/> in <see cref="DegradedSearchMode.KeywordOnly"/> mode.</returns>
    public static SearchHealthStatus EmbeddingUnavailable(CircuitBreakerState circuitState) => new(
        EmbeddingApiAvailable: false,
        DatabaseAvailable: true,
        CacheAvailable: true,
        CircuitBreakerState: circuitState,
        LastHealthCheck: DateTimeOffset.UtcNow,
        CurrentMode: DegradedSearchMode.KeywordOnly);

    /// <summary>
    /// Creates a status indicating the database is unavailable.
    /// </summary>
    /// <returns>A <see cref="SearchHealthStatus"/> in <see cref="DegradedSearchMode.CachedOnly"/> mode.</returns>
    public static SearchHealthStatus DatabaseUnavailable() => new(
        EmbeddingApiAvailable: false,
        DatabaseAvailable: false,
        CacheAvailable: true,
        CircuitBreakerState: CircuitBreakerState.Open,
        LastHealthCheck: DateTimeOffset.UtcNow,
        CurrentMode: DegradedSearchMode.CachedOnly);

    /// <summary>
    /// Creates a status indicating all services are unavailable.
    /// </summary>
    /// <returns>A <see cref="SearchHealthStatus"/> in <see cref="DegradedSearchMode.Unavailable"/> mode.</returns>
    public static SearchHealthStatus AllUnavailable() => new(
        EmbeddingApiAvailable: false,
        DatabaseAvailable: false,
        CacheAvailable: false,
        CircuitBreakerState: CircuitBreakerState.Open,
        LastHealthCheck: DateTimeOffset.UtcNow,
        CurrentMode: DegradedSearchMode.Unavailable);

    /// <summary>
    /// Gets whether the search service is currently operating in a degraded mode.
    /// </summary>
    public bool IsDegraded => CurrentMode != DegradedSearchMode.Full;
}
