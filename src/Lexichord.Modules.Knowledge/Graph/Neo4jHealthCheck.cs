// =============================================================================
// File: Neo4jHealthCheck.cs
// Project: Lexichord.Modules.Knowledge
// Description: Health check for Neo4j graph database connectivity.
// =============================================================================
// LOGIC: Integrates Neo4j connectivity status into the application health
//   monitoring system. Uses IGraphConnectionFactory.TestConnectionAsync()
//   to verify that the graph database is reachable and responding.
//
// Health States:
//   - Healthy: Neo4j connection successful
//   - Unhealthy: Neo4j connection failed or timed out
//
// v0.4.5e: Graph Database Integration (CKVS Phase 1)
// Dependencies: IGraphConnectionFactory, Microsoft.Extensions.Diagnostics.HealthChecks
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Lexichord.Modules.Knowledge.Graph;

/// <summary>
/// Health check for Neo4j graph database connectivity.
/// </summary>
/// <remarks>
/// <para>
/// Reports the health status of the Neo4j connection to the application's
/// health monitoring infrastructure. Registered via
/// <c>services.AddHealthChecks().AddCheck&lt;Neo4jHealthCheck&gt;("neo4j")</c>.
/// </para>
/// <para>
/// <b>Note:</b> This health check does NOT require a specific license tier.
/// Health monitoring should work regardless of the user's license level to
/// ensure proper diagnostics.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5e as part of the Knowledge Graph Foundation.
/// </para>
/// </remarks>
public sealed class Neo4jHealthCheck : IHealthCheck
{
    private readonly IGraphConnectionFactory _factory;

    /// <summary>
    /// Initializes a new instance with the graph connection factory.
    /// </summary>
    /// <param name="factory">The factory used to test the Neo4j connection.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="factory"/> is null.
    /// </exception>
    public Neo4jHealthCheck(IGraphConnectionFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <summary>
    /// Checks the health of the Neo4j connection.
    /// </summary>
    /// <param name="context">The health check context.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <see cref="HealthCheckResult.Healthy"/> if Neo4j is reachable;
    /// <see cref="HealthCheckResult.Unhealthy"/> if the connection fails.
    /// </returns>
    /// <remarks>
    /// LOGIC: Delegates to IGraphConnectionFactory.TestConnectionAsync() which
    /// uses the Neo4j driver's built-in connectivity verification. The method
    /// catches all exceptions to ensure the health check never throws — it
    /// returns Unhealthy instead.
    /// </remarks>
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct = default)
    {
        try
        {
            var connected = await _factory.TestConnectionAsync(ct);

            if (connected)
            {
                return HealthCheckResult.Healthy(
                    $"Neo4j connection successful (database: {_factory.DatabaseName})");
            }

            return HealthCheckResult.Unhealthy(
                $"Neo4j connection failed (database: {_factory.DatabaseName})");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                $"Neo4j health check exception: {ex.Message}",
                exception: ex);
        }
    }
}
