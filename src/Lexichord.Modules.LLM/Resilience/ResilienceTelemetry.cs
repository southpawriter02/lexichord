// -----------------------------------------------------------------------
// <copyright file="ResilienceTelemetry.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Lexichord.Modules.LLM.Resilience;

/// <summary>
/// Provides telemetry collection for resilience policy events.
/// </summary>
/// <remarks>
/// <para>
/// This class collects metrics for observability including:
/// </para>
/// <list type="bullet">
///   <item><description>Retry counts by attempt number</description></item>
///   <item><description>Circuit breaker state changes</description></item>
///   <item><description>Timeout occurrences</description></item>
///   <item><description>Bulkhead rejections</description></item>
///   <item><description>Latency histograms</description></item>
/// </list>
/// <para>
/// All operations are thread-safe using interlocked operations.
/// </para>
/// </remarks>
public class ResilienceTelemetry
{
    private long _totalRequests;
    private long _successfulRequests;
    private long _failedRequests;
    private long _totalRetries;
    private long _circuitBreakerOpens;
    private long _circuitBreakerResets;
    private long _timeouts;
    private long _bulkheadRejections;

    /// <summary>
    /// Retry counts by attempt number (1-based).
    /// </summary>
    private readonly ConcurrentDictionary<int, long> _retryCountsByAttempt = new();

    /// <summary>
    /// Latency samples for percentile calculation (limited to last N samples).
    /// </summary>
    private readonly ConcurrentQueue<double> _latencySamples = new();

    /// <summary>
    /// Maximum number of latency samples to keep.
    /// </summary>
    private const int MaxLatencySamples = 1000;

    /// <summary>
    /// Gets the total number of requests processed.
    /// </summary>
    public long TotalRequests => Interlocked.Read(ref _totalRequests);

    /// <summary>
    /// Gets the number of successful requests.
    /// </summary>
    public long SuccessfulRequests => Interlocked.Read(ref _successfulRequests);

    /// <summary>
    /// Gets the number of failed requests (after all retries).
    /// </summary>
    public long FailedRequests => Interlocked.Read(ref _failedRequests);

    /// <summary>
    /// Gets the total number of retry attempts.
    /// </summary>
    public long TotalRetries => Interlocked.Read(ref _totalRetries);

    /// <summary>
    /// Gets the number of times the circuit breaker has opened.
    /// </summary>
    public long CircuitBreakerOpens => Interlocked.Read(ref _circuitBreakerOpens);

    /// <summary>
    /// Gets the number of times the circuit breaker has reset.
    /// </summary>
    public long CircuitBreakerResets => Interlocked.Read(ref _circuitBreakerResets);

    /// <summary>
    /// Gets the number of timeout occurrences.
    /// </summary>
    public long Timeouts => Interlocked.Read(ref _timeouts);

    /// <summary>
    /// Gets the number of bulkhead rejections.
    /// </summary>
    public long BulkheadRejections => Interlocked.Read(ref _bulkheadRejections);

    /// <summary>
    /// Gets the success rate as a percentage (0-100).
    /// </summary>
    public double SuccessRate
    {
        get
        {
            var total = TotalRequests;
            return total > 0 ? (double)SuccessfulRequests / total * 100 : 0;
        }
    }

    /// <summary>
    /// Gets the retry counts by attempt number.
    /// </summary>
    /// <returns>A dictionary mapping attempt number to retry count.</returns>
    public IReadOnlyDictionary<int, long> GetRetryCountsByAttempt()
        => new Dictionary<int, long>(_retryCountsByAttempt);

    /// <summary>
    /// Records a successful request completion.
    /// </summary>
    /// <param name="latencyMs">The request latency in milliseconds.</param>
    public void RecordSuccess(double latencyMs)
    {
        Interlocked.Increment(ref _totalRequests);
        Interlocked.Increment(ref _successfulRequests);
        RecordLatency(latencyMs);
    }

    /// <summary>
    /// Records a failed request (after all retries exhausted).
    /// </summary>
    /// <param name="latencyMs">The total latency including retries in milliseconds.</param>
    public void RecordFailure(double latencyMs)
    {
        Interlocked.Increment(ref _totalRequests);
        Interlocked.Increment(ref _failedRequests);
        RecordLatency(latencyMs);
    }

    /// <summary>
    /// Records a retry attempt.
    /// </summary>
    /// <param name="attemptNumber">The retry attempt number (1-based).</param>
    public void RecordRetry(int attemptNumber)
    {
        Interlocked.Increment(ref _totalRetries);
        _retryCountsByAttempt.AddOrUpdate(attemptNumber, 1, (_, count) => count + 1);
    }

    /// <summary>
    /// Records a circuit breaker open event.
    /// </summary>
    public void RecordCircuitBreakerOpen()
    {
        Interlocked.Increment(ref _circuitBreakerOpens);
    }

    /// <summary>
    /// Records a circuit breaker reset event.
    /// </summary>
    public void RecordCircuitBreakerReset()
    {
        Interlocked.Increment(ref _circuitBreakerResets);
    }

    /// <summary>
    /// Records a timeout occurrence.
    /// </summary>
    public void RecordTimeout()
    {
        Interlocked.Increment(ref _timeouts);
    }

    /// <summary>
    /// Records a bulkhead rejection.
    /// </summary>
    public void RecordBulkheadRejection()
    {
        Interlocked.Increment(ref _bulkheadRejections);
    }

    /// <summary>
    /// Records a resilience event from the pipeline.
    /// </summary>
    /// <param name="evt">The resilience event to record.</param>
    public void RecordEvent(ResilienceEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt, nameof(evt));

        if (evt.IsRetry && evt.AttemptNumber.HasValue)
        {
            RecordRetry(evt.AttemptNumber.Value);
        }
        else if (evt.IsCircuitBreak)
        {
            RecordCircuitBreakerOpen();
        }
        else if (evt.PolicyName == ResilienceEvent.PolicyNames.CircuitBreaker &&
                 evt.EventType == ResilienceEvent.CircuitBreakerEventTypes.Reset)
        {
            RecordCircuitBreakerReset();
        }
        else if (evt.IsTimeout)
        {
            RecordTimeout();
        }
        else if (evt.IsBulkheadRejection)
        {
            RecordBulkheadRejection();
        }
    }

    /// <summary>
    /// Gets the P50 (median) latency in milliseconds.
    /// </summary>
    /// <returns>The P50 latency, or 0 if no samples.</returns>
    public double GetP50Latency() => GetPercentileLatency(0.50);

    /// <summary>
    /// Gets the P90 latency in milliseconds.
    /// </summary>
    /// <returns>The P90 latency, or 0 if no samples.</returns>
    public double GetP90Latency() => GetPercentileLatency(0.90);

    /// <summary>
    /// Gets the P99 latency in milliseconds.
    /// </summary>
    /// <returns>The P99 latency, or 0 if no samples.</returns>
    public double GetP99Latency() => GetPercentileLatency(0.99);

    /// <summary>
    /// Gets a snapshot of all telemetry data.
    /// </summary>
    /// <returns>A telemetry snapshot.</returns>
    public TelemetrySnapshot GetSnapshot()
    {
        return new TelemetrySnapshot(
            TotalRequests: TotalRequests,
            SuccessfulRequests: SuccessfulRequests,
            FailedRequests: FailedRequests,
            TotalRetries: TotalRetries,
            CircuitBreakerOpens: CircuitBreakerOpens,
            CircuitBreakerResets: CircuitBreakerResets,
            Timeouts: Timeouts,
            BulkheadRejections: BulkheadRejections,
            SuccessRate: SuccessRate,
            P50LatencyMs: GetP50Latency(),
            P90LatencyMs: GetP90Latency(),
            P99LatencyMs: GetP99Latency(),
            RetryCountsByAttempt: GetRetryCountsByAttempt());
    }

    /// <summary>
    /// Resets all telemetry counters.
    /// </summary>
    public void Reset()
    {
        Interlocked.Exchange(ref _totalRequests, 0);
        Interlocked.Exchange(ref _successfulRequests, 0);
        Interlocked.Exchange(ref _failedRequests, 0);
        Interlocked.Exchange(ref _totalRetries, 0);
        Interlocked.Exchange(ref _circuitBreakerOpens, 0);
        Interlocked.Exchange(ref _circuitBreakerResets, 0);
        Interlocked.Exchange(ref _timeouts, 0);
        Interlocked.Exchange(ref _bulkheadRejections, 0);
        _retryCountsByAttempt.Clear();
        _latencySamples.Clear();
    }

    /// <summary>
    /// Records a latency sample.
    /// </summary>
    /// <param name="latencyMs">The latency in milliseconds.</param>
    private void RecordLatency(double latencyMs)
    {
        _latencySamples.Enqueue(latencyMs);

        // LOGIC: Keep only the last MaxLatencySamples samples.
        while (_latencySamples.Count > MaxLatencySamples)
        {
            _latencySamples.TryDequeue(out _);
        }
    }

    /// <summary>
    /// Gets the latency at the specified percentile.
    /// </summary>
    /// <param name="percentile">The percentile (0.0 to 1.0).</param>
    /// <returns>The latency at the percentile, or 0 if no samples.</returns>
    private double GetPercentileLatency(double percentile)
    {
        var samples = _latencySamples.ToArray();
        if (samples.Length == 0)
        {
            return 0;
        }

        Array.Sort(samples);
        var index = (int)Math.Ceiling(percentile * samples.Length) - 1;
        return samples[Math.Max(0, Math.Min(index, samples.Length - 1))];
    }
}

/// <summary>
/// A snapshot of telemetry data at a point in time.
/// </summary>
/// <param name="TotalRequests">Total number of requests processed.</param>
/// <param name="SuccessfulRequests">Number of successful requests.</param>
/// <param name="FailedRequests">Number of failed requests.</param>
/// <param name="TotalRetries">Total retry attempts.</param>
/// <param name="CircuitBreakerOpens">Times circuit breaker opened.</param>
/// <param name="CircuitBreakerResets">Times circuit breaker reset.</param>
/// <param name="Timeouts">Timeout occurrences.</param>
/// <param name="BulkheadRejections">Bulkhead rejections.</param>
/// <param name="SuccessRate">Success rate percentage.</param>
/// <param name="P50LatencyMs">P50 latency in milliseconds.</param>
/// <param name="P90LatencyMs">P90 latency in milliseconds.</param>
/// <param name="P99LatencyMs">P99 latency in milliseconds.</param>
/// <param name="RetryCountsByAttempt">Retry counts by attempt number.</param>
public record TelemetrySnapshot(
    long TotalRequests,
    long SuccessfulRequests,
    long FailedRequests,
    long TotalRetries,
    long CircuitBreakerOpens,
    long CircuitBreakerResets,
    long Timeouts,
    long BulkheadRejections,
    double SuccessRate,
    double P50LatencyMs,
    double P90LatencyMs,
    double P99LatencyMs,
    IReadOnlyDictionary<int, long> RetryCountsByAttempt);
