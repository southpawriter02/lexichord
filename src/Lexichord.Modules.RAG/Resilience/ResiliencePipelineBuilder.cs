// =============================================================================
// File: ResiliencePipelineBuilder.cs
// Project: Lexichord.Modules.RAG
// Description: Factory for building Polly resilience pipelines.
// =============================================================================
// VERSION: v0.5.8d (Error Resilience)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace Lexichord.Modules.RAG.Resilience;

/// <summary>
/// Factory for building Polly resilience pipelines for search operations.
/// </summary>
/// <remarks>
/// <para>
/// This builder creates a pipeline with three strategies in order:
/// <list type="number">
///   <item><description>Timeout: Limits individual operation duration</description></item>
///   <item><description>Retry: Exponential backoff with jitter for transient failures</description></item>
///   <item><description>Circuit Breaker: Fails fast when service is unhealthy</description></item>
/// </list>
/// </para>
/// <para>
/// The pipeline handles these transient exceptions:
/// <list type="bullet">
///   <item><description><see cref="HttpRequestException"/>: Network/HTTP failures</description></item>
///   <item><description><see cref="TimeoutRejectedException"/>: Operation timeout</description></item>
///   <item><description><see cref="TaskCanceledException"/>: Request cancellations</description></item>
/// </list>
/// </para>
/// </remarks>
public static class ResiliencePipelineBuilder
{
    /// <summary>
    /// Builds a resilience pipeline for search operations.
    /// </summary>
    /// <param name="options">Resilience configuration options.</param>
    /// <param name="logger">Logger for resilience events.</param>
    /// <param name="onCircuitStateChanged">Optional callback for circuit state changes.</param>
    /// <returns>A configured <see cref="ResiliencePipeline{SearchResult}"/>.</returns>
    public static ResiliencePipeline<SearchResult> Build(
        ResilienceOptions options,
        ILogger logger,
        Action<CircuitBreakerState>? onCircuitStateChanged = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        var pipeline = new ResiliencePipelineBuilder<SearchResult>()
            .AddTimeout(CreateTimeoutStrategy(options, logger))
            .AddRetry(CreateRetryStrategy(options, logger))
            .AddCircuitBreaker(CreateCircuitBreakerStrategy(options, logger, onCircuitStateChanged))
            .Build();

        logger.LogInformation(
            "Resilience pipeline built: Timeout={Timeout}s, Retry={MaxAttempts}, CircuitBreaker={Threshold}/{Duration}s",
            options.TimeoutPerOperation.TotalSeconds,
            options.RetryMaxAttempts,
            options.CircuitBreakerFailureThreshold,
            options.CircuitBreakerBreakDuration.TotalSeconds);

        return pipeline;
    }

    private static TimeoutStrategyOptions CreateTimeoutStrategy(
        ResilienceOptions options,
        ILogger logger)
    {
        return new TimeoutStrategyOptions
        {
            Timeout = options.TimeoutPerOperation,
            OnTimeout = args =>
            {
                logger.LogWarning(
                    "Operation timed out after {Timeout}s",
                    args.Timeout.TotalSeconds);
                return ValueTask.CompletedTask;
            }
        };
    }

    private static RetryStrategyOptions<SearchResult> CreateRetryStrategy(
        ResilienceOptions options,
        ILogger logger)
    {
        return new RetryStrategyOptions<SearchResult>
        {
            MaxRetryAttempts = options.RetryMaxAttempts,
            Delay = options.RetryInitialDelay,
            MaxDelay = options.RetryMaxDelay,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            ShouldHandle = new PredicateBuilder<SearchResult>()
                .Handle<HttpRequestException>()
                .Handle<TimeoutRejectedException>()
                .Handle<TaskCanceledException>(ex => !ex.CancellationToken.IsCancellationRequested),
            OnRetry = args =>
            {
                logger.LogWarning(
                    args.Outcome.Exception,
                    "Retry attempt {Attempt}/{MaxAttempts} after {Delay}ms",
                    args.AttemptNumber,
                    options.RetryMaxAttempts,
                    args.RetryDelay.TotalMilliseconds);
                return ValueTask.CompletedTask;
            }
        };
    }

    private static CircuitBreakerStrategyOptions<SearchResult> CreateCircuitBreakerStrategy(
        ResilienceOptions options,
        ILogger logger,
        Action<CircuitBreakerState>? onStateChanged)
    {
        return new CircuitBreakerStrategyOptions<SearchResult>
        {
            FailureRatio = 0.5, // 50% failure rate
            MinimumThroughput = options.CircuitBreakerMinimumThroughput,
            SamplingDuration = options.CircuitBreakerSamplingDuration,
            BreakDuration = options.CircuitBreakerBreakDuration,
            ShouldHandle = new PredicateBuilder<SearchResult>()
                .Handle<HttpRequestException>()
                .Handle<TimeoutRejectedException>()
                .Handle<TaskCanceledException>(ex => !ex.CancellationToken.IsCancellationRequested),
            OnOpened = args =>
            {
                logger.LogError(
                    args.Outcome.Exception,
                    "Circuit breaker OPENED for {Duration}s due to failures",
                    args.BreakDuration.TotalSeconds);
                onStateChanged?.Invoke(CircuitBreakerState.Open);
                return ValueTask.CompletedTask;
            },
            OnClosed = args =>
            {
                logger.LogInformation("Circuit breaker CLOSED - normal operation resumed");
                onStateChanged?.Invoke(CircuitBreakerState.Closed);
                return ValueTask.CompletedTask;
            },
            OnHalfOpened = args =>
            {
                logger.LogInformation("Circuit breaker HALF-OPEN - testing recovery");
                onStateChanged?.Invoke(CircuitBreakerState.HalfOpen);
                return ValueTask.CompletedTask;
            }
        };
    }

    /// <summary>
    /// Determines if an exception is transient and should trigger resilience policies.
    /// </summary>
    /// <param name="ex">The exception to check.</param>
    /// <returns>True if the exception is transient, false otherwise.</returns>
    public static bool IsTransientException(Exception ex)
    {
        return ex is HttpRequestException
            or TimeoutRejectedException
            or TaskCanceledException { CancellationToken.IsCancellationRequested: false };
    }
}
