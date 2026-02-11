// -----------------------------------------------------------------------
// <copyright file="ResilientChatService.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts.LLM;
using MediatR;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace Lexichord.Modules.Agents.Resilience;

/// <summary>
/// Decorator that wraps <see cref="IChatCompletionService"/> with Polly resilience policies.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ResilientChatService"/> provides automatic retry, circuit breaker,
/// and timeout functionality for all chat completion requests. It sits between the
/// agent layer and the underlying LLM provider, transparently handling transient failures.
/// </para>
/// <para>
/// <b>Policy Pipeline (outer to inner):</b>
/// </para>
/// <list type="number">
///   <item><description><b>Retry:</b> Up to 3 attempts with exponential backoff + jitter</description></item>
///   <item><description><b>Circuit Breaker:</b> Opens after 50% failure rate in 5+ requests over 30s</description></item>
///   <item><description><b>Timeout:</b> 30-second per-request timeout</description></item>
/// </list>
/// <para>
/// <b>Error Flow:</b>
/// </para>
/// <code>
/// Request → Retry → CircuitBreaker → Timeout → IChatCompletionService
///     ↓ (on failure)
/// RateLimitException → IRateLimitQueue.EnqueueAsync()
/// BrokenCircuit → AgentException("temporarily unavailable")
/// Timeout → AgentException("timed out")
/// Other → AgentException("unexpected error")
/// </code>
/// <para>
/// <b>Introduced in:</b> v0.6.8d as part of the Error Handling &amp; Recovery layer.
/// </para>
/// </remarks>
/// <seealso cref="IChatCompletionService"/>
/// <seealso cref="IErrorRecoveryService"/>
/// <seealso cref="IRateLimitQueue"/>
public sealed class ResilientChatService : IChatCompletionService
{
    private readonly IChatCompletionService _inner;
    private readonly ResiliencePipeline<ChatResponse> _pipeline;
    private readonly IErrorRecoveryService _recovery;
    private readonly IRateLimitQueue _rateLimitQueue;
    private readonly ILogger<ResilientChatService> _logger;
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResilientChatService"/> class.
    /// </summary>
    /// <param name="inner">The underlying chat completion service to wrap with resilience.</param>
    /// <param name="recovery">The error recovery service for strategy decisions.</param>
    /// <param name="rateLimitQueue">The queue for rate-limited requests.</param>
    /// <param name="logger">Logger for resilience pipeline diagnostics.</param>
    /// <param name="mediator">MediatR mediator for publishing error events.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// LOGIC: The Polly resilience pipeline is built in the constructor and reused
    /// across all requests. The pipeline is immutable and thread-safe after construction.
    /// Policy configuration follows the spec: 3 retries with exponential backoff,
    /// 50% failure threshold for circuit breaking, and 30-second timeout.
    /// </remarks>
    public ResilientChatService(
        IChatCompletionService inner,
        IErrorRecoveryService recovery,
        IRateLimitQueue rateLimitQueue,
        ILogger<ResilientChatService> logger,
        IMediator mediator)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _recovery = recovery ?? throw new ArgumentNullException(nameof(recovery));
        _rateLimitQueue = rateLimitQueue ?? throw new ArgumentNullException(nameof(rateLimitQueue));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

        // LOGIC: Build the Polly v8 resilience pipeline with three strategies:
        // 1. Retry: Handles transient HTTP and provider errors
        // 2. Circuit Breaker: Prevents cascading failures during sustained outages
        // 3. Timeout: Prevents individual requests from hanging indefinitely
        _pipeline = new ResiliencePipelineBuilder<ChatResponse>()
            .AddRetry(new RetryStrategyOptions<ChatResponse>
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder<ChatResponse>()
                    .Handle<HttpRequestException>()
                    .Handle<ProviderUnavailableException>(),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "Retry {Attempt} after {Delay}ms due to {Exception}",
                        args.AttemptNumber,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.Message ?? "unknown");

                    return ValueTask.CompletedTask;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<ChatResponse>
            {
                FailureRatio = 0.5,
                MinimumThroughput = 5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(15),
                ShouldHandle = new PredicateBuilder<ChatResponse>()
                    .Handle<HttpRequestException>()
                    .Handle<ProviderUnavailableException>(),
                OnOpened = args =>
                {
                    _logger.LogError(
                        "Circuit breaker opened. Provider unavailable for {Duration}s. " +
                        "Failure ratio exceeded threshold.",
                        args.BreakDuration.TotalSeconds);

                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    _logger.LogInformation(
                        "Circuit breaker closed. Provider recovered.");

                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = args =>
                {
                    _logger.LogInformation(
                        "Circuit breaker half-opened. Testing provider recovery...");

                    return ValueTask.CompletedTask;
                }
            })
            .AddTimeout(TimeSpan.FromSeconds(30))
            .Build();

        _logger.LogDebug(
            "ResilientChatService initialized wrapping {InnerType} for provider '{Provider}'",
            _inner.GetType().Name,
            _inner.ProviderName);
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Delegates to the inner service's provider name. The resilient decorator
    /// is transparent to provider identification.
    /// </remarks>
    public string ProviderName => _inner.ProviderName;

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// LOGIC: Executes the chat completion through the Polly resilience pipeline with
    /// the following error handling:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>
    ///     <see cref="Abstractions.Contracts.LLM.RateLimitException"/>: Detected from the inner
    ///     service and redirected to <see cref="IRateLimitQueue.EnqueueAsync"/>
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="BrokenCircuitException"/>: Caught when the circuit breaker is open,
    ///     publishing an error event and wrapping in an <see cref="AgentException"/>
    ///   </description></item>
    ///   <item><description>
    ///     <see cref="TimeoutRejectedException"/>: Caught on timeout, publishing an
    ///     error event and wrapping in an <see cref="AgentException"/>
    ///   </description></item>
    ///   <item><description>
    ///     Other exceptions: Caught, logged, and wrapped in an <see cref="AgentException"/>
    ///     with a generic user message
    ///   </description></item>
    /// </list>
    /// </remarks>
    public async Task<ChatResponse> CompleteAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        _logger.LogDebug(
            "Executing request with resilience pipeline for provider '{Provider}'",
            _inner.ProviderName);

        try
        {
            return await _pipeline.ExecuteAsync(
                async token => await _inner.CompleteAsync(request, token),
                cancellationToken);
        }
        catch (Lexichord.Abstractions.Contracts.LLM.RateLimitException rl)
        {
            // LOGIC: Rate-limited requests are redirected to the queue rather than
            // failing immediately. The queue waits for the rate limit window to
            // expire before dispatching the request.
            _logger.LogWarning(
                "Rate limited by {Provider}. Queueing request. ETA: {Wait}s",
                rl.ProviderName ?? _inner.ProviderName,
                rl.RetryAfter?.TotalSeconds ?? 30);

            await PublishErrorAsync(
                new AgentRateLimitException(
                    rl.ProviderName ?? _inner.ProviderName,
                    rl.RetryAfter ?? TimeSpan.FromSeconds(30)),
                wasRecovered: true);

            return await _rateLimitQueue.EnqueueAsync(request, cancellationToken);
        }
        catch (BrokenCircuitException bce)
        {
            // LOGIC: The circuit breaker has opened due to sustained failures.
            // We wrap this in a user-friendly AgentException and publish a
            // telemetry event for monitoring.
            _logger.LogError(
                bce,
                "Circuit breaker is open for provider '{Provider}'. Failing fast.",
                _inner.ProviderName);

            var error = new ProviderUnavailableException(
                _inner.ProviderName,
                EstimatedRecovery: TimeSpan.FromSeconds(15));

            await PublishErrorAsync(error, wasRecovered: false);

            throw new AgentException(
                "The AI service is temporarily unavailable. Please try again in a few seconds.",
                error);
        }
        catch (TimeoutRejectedException tre)
        {
            // LOGIC: The request exceeded the 30-second timeout. This is often
            // caused by slow provider responses or large context payloads.
            _logger.LogWarning(
                tre,
                "Request timed out after 30s for provider '{Provider}'",
                _inner.ProviderName);

            var error = new AgentException("Request timed out. Please try again.")
            {
                TechnicalDetails = $"Timeout after 30s. Provider: {_inner.ProviderName}"
            };

            await PublishErrorAsync(error, wasRecovered: false);
            throw error;
        }
        catch (AgentException)
        {
            // LOGIC: Already an AgentException — rethrow without wrapping.
            throw;
        }
        catch (OperationCanceledException)
        {
            // LOGIC: User-initiated cancellation should not be wrapped.
            throw;
        }
        catch (Exception ex)
        {
            // LOGIC: Catch-all for unexpected errors. Wrap in AgentException to
            // ensure the UI always receives a user-friendly message.
            _logger.LogError(
                ex,
                "Unexpected error during chat completion for provider '{Provider}'",
                _inner.ProviderName);

            var error = new AgentException(
                "An unexpected error occurred. Please try again.",
                ex)
            {
                TechnicalDetails = $"Unexpected {ex.GetType().Name}: {ex.Message}"
            };

            await PublishErrorAsync(error, wasRecovered: false);
            throw error;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Streaming is delegated directly to the inner service without the
    /// resilience pipeline. Polly's pipeline operates on Task&lt;T&gt; return types,
    /// not IAsyncEnumerable&lt;T&gt;. Streaming errors should be handled by the
    /// caller's async enumeration. A future enhancement could wrap the stream
    /// with retry-on-disconnect logic.
    /// </remarks>
    public IAsyncEnumerable<StreamingChatToken> StreamAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Streaming request delegated to inner service '{Provider}' (no resilience pipeline)",
            _inner.ProviderName);

        return _inner.StreamAsync(request, cancellationToken);
    }

    /// <summary>
    /// Publishes an <see cref="AgentErrorEvent"/> via MediatR.
    /// </summary>
    /// <param name="error">The agent exception to publish.</param>
    /// <param name="wasRecovered">Whether the error was automatically recovered.</param>
    /// <remarks>
    /// LOGIC: Error events are published for telemetry and UI notification purposes.
    /// Failures in event publishing are logged but do not prevent the error from
    /// being propagated to the caller.
    /// </remarks>
    private async Task PublishErrorAsync(AgentException error, bool wasRecovered)
    {
        try
        {
            await _mediator.Publish(new AgentErrorEvent(
                ErrorType: error.GetType().Name,
                UserMessage: error.UserMessage,
                TechnicalDetails: error.TechnicalDetails,
                WasRecovered: wasRecovered,
                Timestamp: DateTimeOffset.UtcNow));
        }
        catch (Exception ex)
        {
            // LOGIC: Event publishing failures should never impact the main error flow.
            // Log and continue.
            _logger.LogWarning(
                ex,
                "Failed to publish AgentErrorEvent for {ErrorType}",
                error.GetType().Name);
        }
    }
}
