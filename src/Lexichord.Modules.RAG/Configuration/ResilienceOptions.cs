// =============================================================================
// File: ResilienceOptions.cs
// Project: Lexichord.Modules.RAG
// Description: Configuration options for resilience policies.
// =============================================================================
// VERSION: v0.5.8d (Error Resilience)
// =============================================================================

namespace Lexichord.Modules.RAG.Configuration;

/// <summary>
/// Configuration options for the resilient search service's Polly policies.
/// </summary>
/// <remarks>
/// <para>
/// These options configure three resilience strategies:
/// <list type="bullet">
///   <item><description>Timeout: Maximum time for a single operation</description></item>
///   <item><description>Retry: Exponential backoff with jitter for transient failures</description></item>
///   <item><description>Circuit Breaker: Fail-fast when service is unhealthy</description></item>
/// </list>
/// </para>
/// <para>
/// Default values are tuned for typical embedding API latencies (100-500ms)
/// with reasonable tolerance for network variability.
/// </para>
/// </remarks>
/// <example>
/// Configure in appsettings.json:
/// <code>
/// {
///   "Resilience": {
///     "RetryMaxAttempts": 3,
///     "TimeoutPerOperationMs": 5000,
///     "CircuitBreakerFailureThreshold": 5
///   }
/// }
/// </code>
/// </example>
public class ResilienceOptions
{
    /// <summary>
    /// Gets or sets the maximum number of retry attempts for transient failures.
    /// </summary>
    /// <value>Default: 3</value>
    public int RetryMaxAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the initial delay before the first retry.
    /// Subsequent retries use exponential backoff from this base.
    /// </summary>
    /// <value>Default: 200 milliseconds</value>
    public TimeSpan RetryInitialDelay { get; set; } = TimeSpan.FromMilliseconds(200);

    /// <summary>
    /// Gets or sets the maximum delay between retry attempts.
    /// </summary>
    /// <value>Default: 2 seconds</value>
    public TimeSpan RetryMaxDelay { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Gets or sets the timeout for individual operations.
    /// </summary>
    /// <value>Default: 5 seconds</value>
    public TimeSpan TimeoutPerOperation { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the number of consecutive failures required to trip the circuit breaker.
    /// </summary>
    /// <value>Default: 5</value>
    public int CircuitBreakerFailureThreshold { get; set; } = 5;

    /// <summary>
    /// Gets or sets the minimum throughput (requests) required in the sampling window
    /// before the circuit breaker can trip.
    /// </summary>
    /// <value>Default: 10</value>
    public int CircuitBreakerMinimumThroughput { get; set; } = 10;

    /// <summary>
    /// Gets or sets the duration the circuit stays open before transitioning to half-open.
    /// </summary>
    /// <value>Default: 30 seconds</value>
    public TimeSpan CircuitBreakerBreakDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the sampling window duration for the circuit breaker.
    /// </summary>
    /// <value>Default: 60 seconds</value>
    public TimeSpan CircuitBreakerSamplingDuration { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets or sets whether resilience policies are enabled.
    /// When disabled, operations bypass all resilience logic.
    /// </summary>
    /// <value>Default: true</value>
    public bool Enabled { get; set; } = true;
}
