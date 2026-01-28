using Npgsql;

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Factory for creating database connections with pooling support.
/// </summary>
/// <remarks>
/// LOGIC: This interface abstracts the connection creation process, enabling:
/// - Mocking for unit tests
/// - Connection pooling via NpgsqlDataSource
/// - Resilience policies (retry, circuit breaker)
///
/// Implementations should:
/// - Return connections from a pool where possible
/// - Apply resilience policies for transient failures
/// - Log connection events for observability
/// </remarks>
public interface IDbConnectionFactory : IDisposable
{
    /// <summary>
    /// Creates and opens a database connection from the pool.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An open database connection.</returns>
    /// <remarks>
    /// LOGIC: The returned connection is pooled. Always dispose it when done
    /// to return it to the pool. Using statements are recommended:
    /// <code>
    /// await using var connection = await factory.CreateConnectionAsync();
    /// // Use connection
    /// // Connection returns to pool when disposed
    /// </code>
    /// </remarks>
    /// <exception cref="Polly.CircuitBreaker.BrokenCircuitException">
    /// Thrown when the circuit breaker is open due to repeated failures.
    /// </exception>
    Task<NpgsqlConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the underlying data source for advanced scenarios.
    /// </summary>
    /// <remarks>
    /// LOGIC: Direct access to NpgsqlDataSource enables:
    /// - Bulk operations (COPY)
    /// - Notifications (LISTEN/NOTIFY)
    /// - Connection-level settings
    ///
    /// Use with caution; prefer CreateConnectionAsync for most scenarios.
    /// </remarks>
    NpgsqlDataSource DataSource { get; }

    /// <summary>
    /// Gets a value indicating whether the connection factory is healthy.
    /// </summary>
    /// <remarks>
    /// LOGIC: Returns false when circuit breaker is open.
    /// Use for health checks and readiness probes.
    /// </remarks>
    bool IsHealthy { get; }

    /// <summary>
    /// Attempts to verify connectivity to the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if connection successful, false otherwise.</returns>
    Task<bool> CanConnectAsync(CancellationToken cancellationToken = default);
}
