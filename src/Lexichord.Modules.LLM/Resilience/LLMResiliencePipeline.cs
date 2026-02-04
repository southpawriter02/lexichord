// -----------------------------------------------------------------------
// <copyright file="LLMResiliencePipeline.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using Lexichord.Modules.LLM.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;

namespace Lexichord.Modules.LLM.Resilience;

/// <summary>
/// Polly-based resilience pipeline for LLM HTTP operations.
/// </summary>
/// <remarks>
/// <para>
/// This class wraps HTTP operations with multiple layers of protection in the following order
/// (outermost to innermost):
/// </para>
/// <list type="bullet">
///   <item><description><b>Bulkhead:</b> Limits concurrent requests (prevents resource exhaustion)</description></item>
///   <item><description><b>Timeout:</b> Cancels long-running requests (prevents indefinite waits)</description></item>
///   <item><description><b>Circuit Breaker:</b> Fails fast during outages (prevents cascade failures)</description></item>
///   <item><description><b>Retry:</b> Handles transient failures (improves reliability)</description></item>
/// </list>
/// <para>
/// The pipeline is thread-safe and designed to be used as a singleton.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Inject via DI
/// public class MyService(IResiliencePipeline pipeline)
/// {
///     public async Task&lt;string&gt; CallApiAsync(CancellationToken ct)
///     {
///         var response = await pipeline.ExecuteAsync(
///             async token => await _httpClient.GetAsync("https://api.example.com", token),
///             ct);
///
///         return await response.Content.ReadAsStringAsync(ct);
///     }
/// }
/// </code>
/// </example>
public class LLMResiliencePipeline : IResiliencePipeline
{
    private readonly ResilienceOptions _options;
    private readonly ILogger<LLMResiliencePipeline> _logger;
    private readonly IAsyncPolicy<HttpResponseMessage> _policyWrap;
    private readonly AsyncCircuitBreakerPolicy<HttpResponseMessage> _circuitBreaker;

    /// <summary>
    /// Initializes a new instance of the <see cref="LLMResiliencePipeline"/> class.
    /// </summary>
    /// <param name="options">The resilience options.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> or <paramref name="logger"/> is null.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when any option value is invalid.
    /// </exception>
    public LLMResiliencePipeline(
        IOptions<ResilienceOptions> options,
        ILogger<LLMResiliencePipeline> logger)
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));

        _options = options.Value ?? throw new ArgumentNullException(nameof(options), "Options.Value is null");
        _logger = logger;

        // LOGIC: Validate options before constructing policies.
        _options.Validate();

        LLMLogEvents.ResilienceConfigurationLoaded(
            _logger,
            _options.RetryCount,
            _options.TimeoutSeconds,
            _options.CircuitBreakerThreshold,
            _options.BulkheadMaxConcurrency);

        // LOGIC: Build individual policies using the builder.
        var builder = new ResiliencePolicyBuilder(_options, _logger, RaiseEvent);

        var bulkhead = builder.BuildBulkheadPolicy();
        var timeout = builder.BuildTimeoutPolicy();
        _circuitBreaker = builder.BuildCircuitBreakerPolicy();
        var retry = builder.BuildRetryPolicy();

        // LOGIC: Wrap policies in order: Bulkhead → Timeout → Circuit Breaker → Retry.
        // This means:
        // - Bulkhead is checked first (rejects if at capacity)
        // - Timeout applies to the entire operation including retries
        // - Circuit breaker can open based on failures from downstream
        // - Retry is innermost, retrying the actual HTTP call
        _policyWrap = Policy.WrapAsync(bulkhead, timeout, _circuitBreaker, retry);

        LLMLogEvents.ResiliencePolicyWrapConstructed(
            _logger,
            _options.RetryCount,
            _options.CircuitBreakerThreshold,
            _options.TimeoutSeconds,
            _options.BulkheadMaxConcurrency);

        LLMLogEvents.ResiliencePipelineCreated(_logger);
    }

    /// <inheritdoc />
    public CircuitState CircuitState => _circuitBreaker.CircuitState switch
    {
        Polly.CircuitBreaker.CircuitState.Closed => CircuitState.Closed,
        Polly.CircuitBreaker.CircuitState.Open => CircuitState.Open,
        Polly.CircuitBreaker.CircuitState.HalfOpen => CircuitState.HalfOpen,
        Polly.CircuitBreaker.CircuitState.Isolated => CircuitState.Isolated,
        _ => CircuitState.Closed
    };

    /// <inheritdoc />
    public event EventHandler<ResilienceEvent>? OnPolicyEvent;

    /// <inheritdoc />
    public async Task<HttpResponseMessage> ExecuteAsync(
        Func<CancellationToken, Task<HttpResponseMessage>> operation,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(operation, nameof(operation));

        LLMLogEvents.ResiliencePipelineExecuting(_logger);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await _policyWrap.ExecuteAsync(operation, ct);

            stopwatch.Stop();
            LLMLogEvents.ResiliencePipelineExecutionCompleted(_logger, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (BrokenCircuitException ex)
        {
            stopwatch.Stop();
            LLMLogEvents.ResiliencePipelineExecutionFailed(_logger, ex, "Circuit breaker is open");
            throw;
        }
        catch (Polly.Bulkhead.BulkheadRejectedException ex)
        {
            stopwatch.Stop();
            LLMLogEvents.ResiliencePipelineExecutionFailed(_logger, ex, "Bulkhead capacity exceeded");
            throw;
        }
        catch (Polly.Timeout.TimeoutRejectedException ex)
        {
            stopwatch.Stop();
            LLMLogEvents.ResiliencePipelineExecutionFailed(_logger, ex, "Request timed out");
            throw;
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            LLMLogEvents.ResiliencePipelineExecutionFailed(_logger, ex, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            LLMLogEvents.ResiliencePipelineExecutionFailed(_logger, ex, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Raises a resilience event to subscribers.
    /// </summary>
    /// <param name="evt">The event to raise.</param>
    private void RaiseEvent(ResilienceEvent evt)
    {
        LLMLogEvents.ResilienceEventRaised(_logger, evt.PolicyName, evt.EventType);
        OnPolicyEvent?.Invoke(this, evt);
    }
}
