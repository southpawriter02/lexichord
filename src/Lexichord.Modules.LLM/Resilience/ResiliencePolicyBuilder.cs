// -----------------------------------------------------------------------
// <copyright file="ResiliencePolicyBuilder.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Net;
using Lexichord.Modules.LLM.Logging;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Bulkhead;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace Lexichord.Modules.LLM.Resilience;

/// <summary>
/// Builds individual resilience policies for the LLM HTTP pipeline.
/// </summary>
/// <remarks>
/// <para>
/// This class creates Polly policies for:
/// </para>
/// <list type="bullet">
///   <item><description>Retry with exponential backoff and jitter</description></item>
///   <item><description>Circuit breaker with advanced threshold configuration</description></item>
///   <item><description>Timeout with pessimistic strategy</description></item>
///   <item><description>Bulkhead isolation for concurrent request limiting</description></item>
/// </list>
/// <para>
/// All policies include comprehensive logging via the <see cref="LLMLogEvents"/> structured logging.
/// </para>
/// </remarks>
public class ResiliencePolicyBuilder
{
    private readonly ResilienceOptions _options;
    private readonly ILogger _logger;
    private readonly Action<ResilienceEvent>? _onEvent;

    /// <summary>
    /// Random instance for jitter calculation (thread-safe via Random.Shared).
    /// </summary>
    private static readonly Random Jitter = Random.Shared;

    /// <summary>
    /// HTTP status code for Anthropic's overloaded error.
    /// </summary>
    private const int AnthropicOverloadedStatusCode = 529;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResiliencePolicyBuilder"/> class.
    /// </summary>
    /// <param name="options">The resilience options.</param>
    /// <param name="logger">The logger for policy events.</param>
    /// <param name="onEvent">Optional callback for resilience events.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> or <paramref name="logger"/> is null.
    /// </exception>
    public ResiliencePolicyBuilder(ResilienceOptions options, ILogger logger, Action<ResilienceEvent>? onEvent = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _onEvent = onEvent;
    }

    /// <summary>
    /// Builds the retry policy with exponential backoff and jitter.
    /// </summary>
    /// <returns>An async retry policy for HTTP responses.</returns>
    /// <remarks>
    /// <para>
    /// The policy handles:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>HTTP 5xx server errors</description></item>
    ///   <item><description>HTTP 408 request timeout</description></item>
    ///   <item><description>HTTP 429 rate limiting</description></item>
    ///   <item><description>HTTP 529 overloaded (Anthropic-specific)</description></item>
    ///   <item><description>Network-level failures (HttpRequestException)</description></item>
    /// </list>
    /// <para>
    /// Delay calculation:
    /// </para>
    /// <list type="number">
    ///   <item><description>Check for Retry-After header; use if present</description></item>
    ///   <item><description>Otherwise, use exponential backoff: 2^attempt * baseDelay + jitter</description></item>
    ///   <item><description>Cap at maximum delay</description></item>
    /// </list>
    /// </remarks>
    public IAsyncPolicy<HttpResponseMessage> BuildRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(ShouldRetry)
            .WaitAndRetryAsync(
                retryCount: _options.RetryCount,
                sleepDurationProvider: CalculateDelay,
                onRetryAsync: OnRetryAsync);
    }

    /// <summary>
    /// Builds the circuit breaker policy.
    /// </summary>
    /// <returns>A circuit breaker policy for HTTP responses.</returns>
    /// <remarks>
    /// <para>
    /// The circuit breaker uses the advanced configuration with:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>50% failure threshold within sampling duration</description></item>
    ///   <item><description>30-second sampling duration</description></item>
    ///   <item><description>Minimum throughput from <see cref="ResilienceOptions.CircuitBreakerThreshold"/></description></item>
    ///   <item><description>Break duration from <see cref="ResilienceOptions.CircuitBreakerDurationSeconds"/></description></item>
    /// </list>
    /// </remarks>
    public AsyncCircuitBreakerPolicy<HttpResponseMessage> BuildCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(r => (int)r.StatusCode >= 500)
            .AdvancedCircuitBreakerAsync(
                failureThreshold: 0.5,
                samplingDuration: TimeSpan.FromSeconds(30),
                minimumThroughput: _options.CircuitBreakerThreshold,
                durationOfBreak: _options.CircuitBreakerDuration,
                onBreak: OnCircuitBreak,
                onReset: OnCircuitReset,
                onHalfOpen: OnCircuitHalfOpen);
    }

    /// <summary>
    /// Builds a simple circuit breaker policy (without advanced configuration).
    /// </summary>
    /// <returns>A basic circuit breaker policy for HTTP responses.</returns>
    /// <remarks>
    /// <para>
    /// This is a simpler circuit breaker that opens after a fixed number of
    /// consecutive failures, matching the behavior of the existing provider extensions.
    /// </para>
    /// </remarks>
    public AsyncCircuitBreakerPolicy<HttpResponseMessage> BuildSimpleCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: _options.CircuitBreakerThreshold,
                durationOfBreak: _options.CircuitBreakerDuration,
                onBreak: OnCircuitBreak,
                onReset: OnCircuitReset);
    }

    /// <summary>
    /// Builds the timeout policy.
    /// </summary>
    /// <returns>A timeout policy for HTTP responses.</returns>
    /// <remarks>
    /// <para>
    /// Uses pessimistic timeout strategy which cancels the delegate when the timeout elapses,
    /// even if the underlying HTTP request cannot be cancelled (though modern HttpClient supports cancellation).
    /// </para>
    /// </remarks>
    public IAsyncPolicy<HttpResponseMessage> BuildTimeoutPolicy()
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(
            _options.Timeout,
            TimeoutStrategy.Pessimistic,
            onTimeoutAsync: OnTimeoutAsync);
    }

    /// <summary>
    /// Builds the bulkhead isolation policy.
    /// </summary>
    /// <returns>A bulkhead policy for HTTP responses.</returns>
    /// <remarks>
    /// <para>
    /// The bulkhead limits:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Maximum concurrent requests from <see cref="ResilienceOptions.BulkheadMaxConcurrency"/></description></item>
    ///   <item><description>Maximum queued requests from <see cref="ResilienceOptions.BulkheadMaxQueue"/></description></item>
    /// </list>
    /// <para>
    /// When both are full, new requests are rejected with <see cref="BulkheadRejectedException"/>.
    /// </para>
    /// </remarks>
    public IAsyncPolicy<HttpResponseMessage> BuildBulkheadPolicy()
    {
        return Policy.BulkheadAsync<HttpResponseMessage>(
            maxParallelization: _options.BulkheadMaxConcurrency,
            maxQueuingActions: _options.BulkheadMaxQueue,
            onBulkheadRejectedAsync: OnBulkheadRejectedAsync);
    }

    /// <summary>
    /// Determines whether a response should trigger a retry.
    /// </summary>
    /// <param name="response">The HTTP response to evaluate.</param>
    /// <returns><c>true</c> if the response should trigger a retry; otherwise, <c>false</c>.</returns>
    private bool ShouldRetry(HttpResponseMessage response)
    {
        var statusCode = response.StatusCode;

        // LOGIC: Check for status codes that warrant retry.
        var shouldRetry = statusCode switch
        {
            HttpStatusCode.TooManyRequests => true,              // 429
            HttpStatusCode.ServiceUnavailable => true,           // 503
            HttpStatusCode.GatewayTimeout => true,               // 504
            HttpStatusCode.BadGateway => true,                   // 502
            _ when (int)statusCode == AnthropicOverloadedStatusCode => true,  // 529
            _ => false
        };

        if (shouldRetry)
        {
            LLMLogEvents.ResilienceTransientErrorDetected(_logger, (int)statusCode);
        }

        return shouldRetry;
    }

    /// <summary>
    /// Calculates the delay before a retry attempt.
    /// </summary>
    /// <param name="attempt">The retry attempt number (1-based).</param>
    /// <param name="outcome">The outcome of the previous attempt.</param>
    /// <param name="context">The Polly context.</param>
    /// <returns>The delay to wait before retrying.</returns>
    private TimeSpan CalculateDelay(int attempt, DelegateResult<HttpResponseMessage> outcome, Context context)
    {
        // LOGIC: Check for Retry-After header first.
        var retryAfter = outcome.Result?.Headers.RetryAfter?.Delta;
        if (retryAfter.HasValue)
        {
            LLMLogEvents.ResilienceUsingRetryAfterHeader(_logger, retryAfter.Value.TotalSeconds);
            return retryAfter.Value;
        }

        // LOGIC: Calculate exponential backoff: 2^attempt * baseDelay.
        var exponentialDelay = Math.Pow(2, attempt) * _options.RetryBaseDelaySeconds;
        var exponentialDelayMs = exponentialDelay * 1000;

        // LOGIC: Add jitter (0-1000ms).
        var jitterMs = Jitter.Next(0, 1000);
        var totalDelayMs = exponentialDelayMs + jitterMs;

        // LOGIC: Cap at maximum delay.
        var maxDelayMs = _options.RetryMaxDelaySeconds * 1000;
        if (totalDelayMs > maxDelayMs)
        {
            LLMLogEvents.ResilienceDelayCapped(_logger, totalDelayMs, maxDelayMs);
            totalDelayMs = maxDelayMs;
        }

        LLMLogEvents.ResilienceExponentialBackoff(_logger, attempt, exponentialDelayMs, jitterMs, totalDelayMs);

        return TimeSpan.FromMilliseconds(totalDelayMs);
    }

    /// <summary>
    /// Called when a retry is about to be executed.
    /// </summary>
    private Task OnRetryAsync(
        DelegateResult<HttpResponseMessage> outcome,
        TimeSpan delay,
        int attempt,
        Context context)
    {
        var reason = outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString() ?? "Unknown";

        LLMLogEvents.ResilienceRetryAttempt(_logger, attempt, _options.RetryCount, delay.TotalMilliseconds, reason);

        // LOGIC: Raise resilience event for telemetry.
        _onEvent?.Invoke(ResilienceEvent.CreateRetry(attempt, delay, outcome.Exception));

        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when the circuit breaker opens.
    /// </summary>
    private void OnCircuitBreak(
        DelegateResult<HttpResponseMessage> result,
        CircuitState state,
        TimeSpan breakDuration,
        Context context)
    {
        var reason = result.Exception?.Message ?? result.Result?.StatusCode.ToString() ?? "Unknown";

        LLMLogEvents.ResilienceCircuitBreakerOpened(_logger, breakDuration.TotalSeconds, reason);

        // LOGIC: Raise resilience event for telemetry.
        _onEvent?.Invoke(ResilienceEvent.CreateCircuitBreak(breakDuration, result.Exception));
    }

    /// <summary>
    /// Called when the circuit breaker opens (simple version without CircuitState parameter).
    /// </summary>
    private void OnCircuitBreak(
        DelegateResult<HttpResponseMessage> result,
        TimeSpan breakDuration,
        Context context)
    {
        var reason = result.Exception?.Message ?? result.Result?.StatusCode.ToString() ?? "Unknown";

        LLMLogEvents.ResilienceCircuitBreakerOpened(_logger, breakDuration.TotalSeconds, reason);

        _onEvent?.Invoke(ResilienceEvent.CreateCircuitBreak(breakDuration, result.Exception));
    }

    /// <summary>
    /// Called when the circuit breaker resets.
    /// </summary>
    private void OnCircuitReset(Context context)
    {
        LLMLogEvents.ResilienceCircuitBreakerReset(_logger);

        _onEvent?.Invoke(ResilienceEvent.CreateCircuitReset());
    }

    /// <summary>
    /// Called when the circuit breaker transitions to half-open.
    /// </summary>
    private void OnCircuitHalfOpen()
    {
        LLMLogEvents.ResilienceCircuitBreakerHalfOpen(_logger);

        _onEvent?.Invoke(ResilienceEvent.CreateCircuitHalfOpen());
    }

    /// <summary>
    /// Called when a request times out.
    /// </summary>
    private Task OnTimeoutAsync(Context context, TimeSpan timeout, Task timedOutTask)
    {
        LLMLogEvents.ResilienceRequestTimeout(_logger, timeout.TotalSeconds);

        _onEvent?.Invoke(ResilienceEvent.CreateTimeout(timeout));

        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when the bulkhead rejects a request.
    /// </summary>
    private Task OnBulkheadRejectedAsync(Context context)
    {
        LLMLogEvents.ResilienceBulkheadRejected(_logger);

        _onEvent?.Invoke(ResilienceEvent.CreateBulkheadRejection());

        return Task.CompletedTask;
    }
}
