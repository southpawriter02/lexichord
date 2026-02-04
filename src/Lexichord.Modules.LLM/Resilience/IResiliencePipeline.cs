// -----------------------------------------------------------------------
// <copyright file="IResiliencePipeline.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.LLM.Resilience;

/// <summary>
/// Defines a resilience pipeline for HTTP operations.
/// </summary>
/// <remarks>
/// <para>
/// The resilience pipeline wraps HTTP operations with multiple layers of protection:
/// </para>
/// <list type="bullet">
///   <item><description><b>Bulkhead:</b> Limits concurrent requests to prevent resource exhaustion</description></item>
///   <item><description><b>Timeout:</b> Cancels requests that exceed the configured duration</description></item>
///   <item><description><b>Circuit Breaker:</b> Fails fast during sustained outages</description></item>
///   <item><description><b>Retry:</b> Automatically retries transient failures with exponential backoff</description></item>
/// </list>
/// <para>
/// The policies are applied in the order: Bulkhead → Timeout → Circuit Breaker → Retry,
/// which means the innermost policy (closest to the actual HTTP call) is Retry.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class MyService
/// {
///     private readonly IResiliencePipeline _pipeline;
///     private readonly HttpClient _client;
///
///     public async Task&lt;string&gt; GetDataAsync(CancellationToken ct)
///     {
///         var response = await _pipeline.ExecuteAsync(
///             async token => await _client.GetAsync("https://api.example.com/data", token),
///             ct);
///
///         return await response.Content.ReadAsStringAsync(ct);
///     }
/// }
/// </code>
/// </example>
public interface IResiliencePipeline
{
    /// <summary>
    /// Executes an HTTP operation through the resilience pipeline.
    /// </summary>
    /// <param name="operation">
    /// The HTTP operation to execute. Receives a <see cref="CancellationToken"/>
    /// that may be cancelled by the timeout policy.
    /// </param>
    /// <param name="ct">
    /// Optional cancellation token that can be used to cancel the operation externally.
    /// This token is linked with the timeout policy's cancellation.
    /// </param>
    /// <returns>The HTTP response from the operation.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="operation"/> is null.
    /// </exception>
    /// <exception cref="Polly.CircuitBreaker.BrokenCircuitException">
    /// Thrown when the circuit breaker is open and rejecting requests.
    /// </exception>
    /// <exception cref="Polly.Bulkhead.BulkheadRejectedException">
    /// Thrown when the bulkhead is at capacity and cannot queue the request.
    /// </exception>
    /// <exception cref="Polly.Timeout.TimeoutRejectedException">
    /// Thrown when the operation exceeds the configured timeout.
    /// </exception>
    /// <exception cref="HttpRequestException">
    /// Thrown when all retry attempts have been exhausted.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The operation will be retried automatically for transient failures including:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>HTTP 5xx server errors</description></item>
    ///   <item><description>HTTP 408 request timeout</description></item>
    ///   <item><description>HTTP 429 rate limiting</description></item>
    ///   <item><description>HTTP 529 overloaded (Anthropic-specific)</description></item>
    ///   <item><description>Network-level failures (HttpRequestException)</description></item>
    /// </list>
    /// </remarks>
    Task<HttpResponseMessage> ExecuteAsync(
        Func<CancellationToken, Task<HttpResponseMessage>> operation,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the current state of the circuit breaker.
    /// </summary>
    /// <value>
    /// The current <see cref="CircuitState"/> of the circuit breaker policy.
    /// </value>
    /// <remarks>
    /// <para>
    /// This property can be used to monitor the health of the underlying service:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="CircuitState.Closed"/>: Service is healthy</description></item>
    ///   <item><description><see cref="CircuitState.Open"/>: Service is experiencing failures</description></item>
    ///   <item><description><see cref="CircuitState.HalfOpen"/>: Testing if service has recovered</description></item>
    ///   <item><description><see cref="CircuitState.Isolated"/>: Manually isolated</description></item>
    /// </list>
    /// </remarks>
    CircuitState CircuitState { get; }

    /// <summary>
    /// Occurs when a policy event happens (retry, circuit break, timeout, etc.).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Subscribe to this event to receive telemetry about policy actions.
    /// Events include:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Retry attempts with delay duration</description></item>
    ///   <item><description>Circuit breaker state changes</description></item>
    ///   <item><description>Timeout occurrences</description></item>
    ///   <item><description>Bulkhead rejections</description></item>
    /// </list>
    /// <para>
    /// <b>Note:</b> Event handlers should be fast and non-blocking to avoid
    /// impacting the request pipeline.
    /// </para>
    /// </remarks>
    event EventHandler<ResilienceEvent>? OnPolicyEvent;
}
