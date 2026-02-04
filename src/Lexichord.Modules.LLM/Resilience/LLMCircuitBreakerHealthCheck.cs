// -----------------------------------------------------------------------
// <copyright file="LLMCircuitBreakerHealthCheck.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Modules.LLM.Logging;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.LLM.Resilience;

/// <summary>
/// Health check implementation for the LLM circuit breaker state.
/// </summary>
/// <remarks>
/// <para>
/// This health check monitors the circuit breaker state and reports:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="HealthStatus.Healthy"/>: Circuit is closed (normal operation)</description></item>
///   <item><description><see cref="HealthStatus.Degraded"/>: Circuit is half-open (testing recovery)</description></item>
///   <item><description><see cref="HealthStatus.Unhealthy"/>: Circuit is open or isolated (failing)</description></item>
/// </list>
/// <para>
/// Register this health check to enable monitoring of the LLM provider availability
/// through standard ASP.NET Core health check endpoints.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In Startup.cs or Program.cs
/// services.AddHealthChecks()
///     .AddCheck&lt;LLMCircuitBreakerHealthCheck&gt;("llm-circuit-breaker");
///
/// // Endpoint configuration
/// app.MapHealthChecks("/health");
/// </code>
/// </example>
public class LLMCircuitBreakerHealthCheck : IHealthCheck
{
    private readonly IResiliencePipeline _pipeline;
    private readonly ILogger<LLMCircuitBreakerHealthCheck> _logger;

    /// <summary>
    /// The health check name used for registration.
    /// </summary>
    public const string Name = "llm-circuit-breaker";

    /// <summary>
    /// Initializes a new instance of the <see cref="LLMCircuitBreakerHealthCheck"/> class.
    /// </summary>
    /// <param name="pipeline">The resilience pipeline to monitor.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="pipeline"/> or <paramref name="logger"/> is null.
    /// </exception>
    public LLMCircuitBreakerHealthCheck(
        IResiliencePipeline pipeline,
        ILogger<LLMCircuitBreakerHealthCheck> logger)
    {
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Runs the health check, returning the circuit breaker state.
    /// </summary>
    /// <param name="context">The health check context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A health check result based on the circuit breaker state.</returns>
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var circuitState = _pipeline.CircuitState;

        LLMLogEvents.ResilienceHealthCheckQueried(_logger, circuitState.ToString());

        var result = circuitState switch
        {
            CircuitState.Closed => HealthCheckResult.Healthy(
                description: "Circuit breaker is closed. LLM providers are operating normally.",
                data: new Dictionary<string, object>
                {
                    ["circuit_state"] = circuitState.ToString()
                }),

            CircuitState.HalfOpen => HealthCheckResult.Degraded(
                description: "Circuit breaker is half-open. Testing if LLM provider has recovered.",
                data: new Dictionary<string, object>
                {
                    ["circuit_state"] = circuitState.ToString()
                }),

            CircuitState.Open => HealthCheckResult.Unhealthy(
                description: "Circuit breaker is open. LLM provider is experiencing failures.",
                data: new Dictionary<string, object>
                {
                    ["circuit_state"] = circuitState.ToString()
                }),

            CircuitState.Isolated => HealthCheckResult.Unhealthy(
                description: "Circuit breaker is manually isolated. LLM requests are being rejected.",
                data: new Dictionary<string, object>
                {
                    ["circuit_state"] = circuitState.ToString()
                }),

            _ => HealthCheckResult.Healthy(
                description: "Circuit breaker state is unknown, assuming healthy.",
                data: new Dictionary<string, object>
                {
                    ["circuit_state"] = circuitState.ToString()
                })
        };

        return Task.FromResult(result);
    }
}
