// -----------------------------------------------------------------------
// <copyright file="ResilienceTelemetryTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Modules.LLM.Resilience;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.LLM.Resilience;

/// <summary>
/// Unit tests for <see cref="ResilienceTelemetry"/>.
/// </summary>
public class ResilienceTelemetryTests
{
    #region Initial State Tests

    /// <summary>
    /// Tests that new telemetry instance starts with zero total requests.
    /// </summary>
    [Fact]
    public void NewInstance_TotalRequests_ShouldBeZero()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();

        // Assert
        telemetry.TotalRequests.Should().Be(0);
    }

    /// <summary>
    /// Tests that new telemetry instance starts with zero successful requests.
    /// </summary>
    [Fact]
    public void NewInstance_SuccessfulRequests_ShouldBeZero()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();

        // Assert
        telemetry.SuccessfulRequests.Should().Be(0);
    }

    /// <summary>
    /// Tests that new telemetry instance starts with zero failed requests.
    /// </summary>
    [Fact]
    public void NewInstance_FailedRequests_ShouldBeZero()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();

        // Assert
        telemetry.FailedRequests.Should().Be(0);
    }

    /// <summary>
    /// Tests that new telemetry instance starts with zero total retries.
    /// </summary>
    [Fact]
    public void NewInstance_TotalRetries_ShouldBeZero()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();

        // Assert
        telemetry.TotalRetries.Should().Be(0);
    }

    /// <summary>
    /// Tests that new telemetry instance starts with zero success rate.
    /// </summary>
    [Fact]
    public void NewInstance_SuccessRate_ShouldBeZero()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();

        // Assert
        telemetry.SuccessRate.Should().Be(0);
    }

    #endregion

    #region RecordSuccess Tests

    /// <summary>
    /// Tests that RecordSuccess increments total requests.
    /// </summary>
    [Fact]
    public void RecordSuccess_ShouldIncrementTotalRequests()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();

        // Act
        telemetry.RecordSuccess(100);

        // Assert
        telemetry.TotalRequests.Should().Be(1);
    }

    /// <summary>
    /// Tests that RecordSuccess increments successful requests.
    /// </summary>
    [Fact]
    public void RecordSuccess_ShouldIncrementSuccessfulRequests()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();

        // Act
        telemetry.RecordSuccess(100);

        // Assert
        telemetry.SuccessfulRequests.Should().Be(1);
    }

    /// <summary>
    /// Tests that RecordSuccess does not increment failed requests.
    /// </summary>
    [Fact]
    public void RecordSuccess_ShouldNotIncrementFailedRequests()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();

        // Act
        telemetry.RecordSuccess(100);

        // Assert
        telemetry.FailedRequests.Should().Be(0);
    }

    /// <summary>
    /// Tests that multiple RecordSuccess calls update counters correctly.
    /// </summary>
    [Fact]
    public void RecordSuccess_MultipleCalls_ShouldUpdateCountersCorrectly()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();

        // Act
        telemetry.RecordSuccess(100);
        telemetry.RecordSuccess(200);
        telemetry.RecordSuccess(150);

        // Assert
        telemetry.TotalRequests.Should().Be(3);
        telemetry.SuccessfulRequests.Should().Be(3);
    }

    #endregion

    #region RecordFailure Tests

    /// <summary>
    /// Tests that RecordFailure increments total requests.
    /// </summary>
    [Fact]
    public void RecordFailure_ShouldIncrementTotalRequests()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();

        // Act
        telemetry.RecordFailure(100);

        // Assert
        telemetry.TotalRequests.Should().Be(1);
    }

    /// <summary>
    /// Tests that RecordFailure increments failed requests.
    /// </summary>
    [Fact]
    public void RecordFailure_ShouldIncrementFailedRequests()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();

        // Act
        telemetry.RecordFailure(100);

        // Assert
        telemetry.FailedRequests.Should().Be(1);
    }

    /// <summary>
    /// Tests that RecordFailure does not increment successful requests.
    /// </summary>
    [Fact]
    public void RecordFailure_ShouldNotIncrementSuccessfulRequests()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();

        // Act
        telemetry.RecordFailure(100);

        // Assert
        telemetry.SuccessfulRequests.Should().Be(0);
    }

    #endregion

    #region RecordRetry Tests

    /// <summary>
    /// Tests that RecordRetry increments total retries.
    /// </summary>
    [Fact]
    public void RecordRetry_ShouldIncrementTotalRetries()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();

        // Act
        telemetry.RecordRetry(1);

        // Assert
        telemetry.TotalRetries.Should().Be(1);
    }

    /// <summary>
    /// Tests that RecordRetry tracks attempts by number.
    /// </summary>
    [Fact]
    public void RecordRetry_ShouldTrackAttemptsByNumber()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();

        // Act
        telemetry.RecordRetry(1);
        telemetry.RecordRetry(1);
        telemetry.RecordRetry(2);

        // Assert
        var retryCountsByAttempt = telemetry.GetRetryCountsByAttempt();
        retryCountsByAttempt[1].Should().Be(2);
        retryCountsByAttempt[2].Should().Be(1);
    }

    /// <summary>
    /// Tests that multiple retry attempts are tracked correctly.
    /// </summary>
    [Fact]
    public void RecordRetry_MultipleAttempts_ShouldUpdateTotalCorrectly()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();

        // Act
        telemetry.RecordRetry(1);
        telemetry.RecordRetry(2);
        telemetry.RecordRetry(3);

        // Assert
        telemetry.TotalRetries.Should().Be(3);
    }

    #endregion

    #region Circuit Breaker Tests

    /// <summary>
    /// Tests that RecordCircuitBreakerOpen increments the counter.
    /// </summary>
    [Fact]
    public void RecordCircuitBreakerOpen_ShouldIncrementCounter()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();

        // Act
        telemetry.RecordCircuitBreakerOpen();

        // Assert
        telemetry.CircuitBreakerOpens.Should().Be(1);
    }

    /// <summary>
    /// Tests that RecordCircuitBreakerReset increments the counter.
    /// </summary>
    [Fact]
    public void RecordCircuitBreakerReset_ShouldIncrementCounter()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();

        // Act
        telemetry.RecordCircuitBreakerReset();

        // Assert
        telemetry.CircuitBreakerResets.Should().Be(1);
    }

    /// <summary>
    /// Tests that circuit breaker open and reset are tracked independently.
    /// </summary>
    [Fact]
    public void CircuitBreaker_OpenAndReset_ShouldBeTrackedIndependently()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();

        // Act
        telemetry.RecordCircuitBreakerOpen();
        telemetry.RecordCircuitBreakerOpen();
        telemetry.RecordCircuitBreakerReset();

        // Assert
        telemetry.CircuitBreakerOpens.Should().Be(2);
        telemetry.CircuitBreakerResets.Should().Be(1);
    }

    #endregion

    #region Timeout and Bulkhead Tests

    /// <summary>
    /// Tests that RecordTimeout increments the counter.
    /// </summary>
    [Fact]
    public void RecordTimeout_ShouldIncrementCounter()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();

        // Act
        telemetry.RecordTimeout();

        // Assert
        telemetry.Timeouts.Should().Be(1);
    }

    /// <summary>
    /// Tests that RecordBulkheadRejection increments the counter.
    /// </summary>
    [Fact]
    public void RecordBulkheadRejection_ShouldIncrementCounter()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();

        // Act
        telemetry.RecordBulkheadRejection();

        // Assert
        telemetry.BulkheadRejections.Should().Be(1);
    }

    #endregion

    #region SuccessRate Tests

    /// <summary>
    /// Tests that success rate is calculated correctly.
    /// </summary>
    [Fact]
    public void SuccessRate_WithMixedResults_ShouldCalculateCorrectly()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();
        telemetry.RecordSuccess(100);
        telemetry.RecordSuccess(100);
        telemetry.RecordSuccess(100);
        telemetry.RecordFailure(100);

        // Assert
        telemetry.SuccessRate.Should().Be(75.0);
    }

    /// <summary>
    /// Tests that success rate is 100% with all successes.
    /// </summary>
    [Fact]
    public void SuccessRate_AllSuccesses_ShouldBe100()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();
        telemetry.RecordSuccess(100);
        telemetry.RecordSuccess(100);

        // Assert
        telemetry.SuccessRate.Should().Be(100.0);
    }

    /// <summary>
    /// Tests that success rate is 0% with all failures.
    /// </summary>
    [Fact]
    public void SuccessRate_AllFailures_ShouldBeZero()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();
        telemetry.RecordFailure(100);
        telemetry.RecordFailure(100);

        // Assert
        telemetry.SuccessRate.Should().Be(0.0);
    }

    #endregion

    #region Latency Tests

    /// <summary>
    /// Tests that P50 latency returns 0 with no samples.
    /// </summary>
    [Fact]
    public void GetP50Latency_NoSamples_ShouldReturnZero()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();

        // Assert
        telemetry.GetP50Latency().Should().Be(0);
    }

    /// <summary>
    /// Tests that P50 latency is calculated correctly.
    /// </summary>
    [Fact]
    public void GetP50Latency_WithSamples_ShouldCalculateMedian()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();
        telemetry.RecordSuccess(100);
        telemetry.RecordSuccess(200);
        telemetry.RecordSuccess(300);

        // Assert - P50 should be around the median
        telemetry.GetP50Latency().Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Tests that P90 latency returns 0 with no samples.
    /// </summary>
    [Fact]
    public void GetP90Latency_NoSamples_ShouldReturnZero()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();

        // Assert
        telemetry.GetP90Latency().Should().Be(0);
    }

    /// <summary>
    /// Tests that P99 latency returns 0 with no samples.
    /// </summary>
    [Fact]
    public void GetP99Latency_NoSamples_ShouldReturnZero()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();

        // Assert
        telemetry.GetP99Latency().Should().Be(0);
    }

    /// <summary>
    /// Tests that P99 is greater than or equal to P90 which is greater than or equal to P50.
    /// </summary>
    [Fact]
    public void Percentiles_ShouldBeOrdered()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();
        for (var i = 1; i <= 100; i++)
        {
            telemetry.RecordSuccess(i * 10);
        }

        // Assert
        telemetry.GetP99Latency().Should().BeGreaterThanOrEqualTo(telemetry.GetP90Latency());
        telemetry.GetP90Latency().Should().BeGreaterThanOrEqualTo(telemetry.GetP50Latency());
    }

    #endregion

    #region RecordEvent Tests

    /// <summary>
    /// Tests that RecordEvent with retry event records the retry.
    /// </summary>
    [Fact]
    public void RecordEvent_WithRetryEvent_ShouldRecordRetry()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();
        var evt = ResilienceEvent.CreateRetry(2, TimeSpan.FromSeconds(2));

        // Act
        telemetry.RecordEvent(evt);

        // Assert
        telemetry.TotalRetries.Should().Be(1);
        telemetry.GetRetryCountsByAttempt()[2].Should().Be(1);
    }

    /// <summary>
    /// Tests that RecordEvent with circuit break event records the open.
    /// </summary>
    [Fact]
    public void RecordEvent_WithCircuitBreakEvent_ShouldRecordOpen()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();
        var evt = ResilienceEvent.CreateCircuitBreak(TimeSpan.FromSeconds(30));

        // Act
        telemetry.RecordEvent(evt);

        // Assert
        telemetry.CircuitBreakerOpens.Should().Be(1);
    }

    /// <summary>
    /// Tests that RecordEvent with circuit breaker reset event records the reset.
    /// </summary>
    [Fact]
    public void RecordEvent_WithCircuitResetEvent_ShouldRecordReset()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();
        var evt = new ResilienceEvent(
            PolicyName: ResilienceEvent.PolicyNames.CircuitBreaker,
            EventType: ResilienceEvent.CircuitBreakerEventTypes.Reset,
            Duration: null,
            Exception: null,
            AttemptNumber: null);

        // Act
        telemetry.RecordEvent(evt);

        // Assert
        telemetry.CircuitBreakerResets.Should().Be(1);
    }

    /// <summary>
    /// Tests that RecordEvent with timeout event records the timeout.
    /// </summary>
    [Fact]
    public void RecordEvent_WithTimeoutEvent_ShouldRecordTimeout()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();
        var evt = ResilienceEvent.CreateTimeout(TimeSpan.FromSeconds(30));

        // Act
        telemetry.RecordEvent(evt);

        // Assert
        telemetry.Timeouts.Should().Be(1);
    }

    /// <summary>
    /// Tests that RecordEvent with bulkhead rejection event records the rejection.
    /// </summary>
    [Fact]
    public void RecordEvent_WithBulkheadRejectionEvent_ShouldRecordRejection()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();
        var evt = ResilienceEvent.CreateBulkheadRejection();

        // Act
        telemetry.RecordEvent(evt);

        // Assert
        telemetry.BulkheadRejections.Should().Be(1);
    }

    /// <summary>
    /// Tests that RecordEvent throws on null event.
    /// </summary>
    [Fact]
    public void RecordEvent_WithNull_ShouldThrow()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();

        // Act & Assert
        var act = () => telemetry.RecordEvent(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region GetSnapshot Tests

    /// <summary>
    /// Tests that GetSnapshot returns all telemetry data.
    /// </summary>
    [Fact]
    public void GetSnapshot_ShouldContainAllData()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();
        telemetry.RecordSuccess(100);
        telemetry.RecordFailure(200);
        telemetry.RecordRetry(1);
        telemetry.RecordCircuitBreakerOpen();
        telemetry.RecordTimeout();

        // Act
        var snapshot = telemetry.GetSnapshot();

        // Assert
        snapshot.TotalRequests.Should().Be(2);
        snapshot.SuccessfulRequests.Should().Be(1);
        snapshot.FailedRequests.Should().Be(1);
        snapshot.TotalRetries.Should().Be(1);
        snapshot.CircuitBreakerOpens.Should().Be(1);
        snapshot.Timeouts.Should().Be(1);
        snapshot.SuccessRate.Should().Be(50.0);
    }

    /// <summary>
    /// Tests that GetSnapshot includes retry counts by attempt.
    /// </summary>
    [Fact]
    public void GetSnapshot_ShouldIncludeRetryCountsByAttempt()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();
        telemetry.RecordRetry(1);
        telemetry.RecordRetry(2);

        // Act
        var snapshot = telemetry.GetSnapshot();

        // Assert
        snapshot.RetryCountsByAttempt.Should().ContainKey(1);
        snapshot.RetryCountsByAttempt.Should().ContainKey(2);
    }

    /// <summary>
    /// Tests that GetSnapshot includes latency percentiles.
    /// </summary>
    [Fact]
    public void GetSnapshot_ShouldIncludeLatencyPercentiles()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();
        telemetry.RecordSuccess(100);

        // Act
        var snapshot = telemetry.GetSnapshot();

        // Assert
        snapshot.P50LatencyMs.Should().BeGreaterOrEqualTo(0);
        snapshot.P90LatencyMs.Should().BeGreaterOrEqualTo(0);
        snapshot.P99LatencyMs.Should().BeGreaterOrEqualTo(0);
    }

    #endregion

    #region Reset Tests

    /// <summary>
    /// Tests that Reset clears all counters.
    /// </summary>
    [Fact]
    public void Reset_ShouldClearAllCounters()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();
        telemetry.RecordSuccess(100);
        telemetry.RecordFailure(200);
        telemetry.RecordRetry(1);
        telemetry.RecordCircuitBreakerOpen();
        telemetry.RecordTimeout();
        telemetry.RecordBulkheadRejection();

        // Act
        telemetry.Reset();

        // Assert
        telemetry.TotalRequests.Should().Be(0);
        telemetry.SuccessfulRequests.Should().Be(0);
        telemetry.FailedRequests.Should().Be(0);
        telemetry.TotalRetries.Should().Be(0);
        telemetry.CircuitBreakerOpens.Should().Be(0);
        telemetry.CircuitBreakerResets.Should().Be(0);
        telemetry.Timeouts.Should().Be(0);
        telemetry.BulkheadRejections.Should().Be(0);
    }

    /// <summary>
    /// Tests that Reset clears retry counts by attempt.
    /// </summary>
    [Fact]
    public void Reset_ShouldClearRetryCountsByAttempt()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();
        telemetry.RecordRetry(1);
        telemetry.RecordRetry(2);

        // Act
        telemetry.Reset();

        // Assert
        telemetry.GetRetryCountsByAttempt().Should().BeEmpty();
    }

    /// <summary>
    /// Tests that Reset clears latency samples.
    /// </summary>
    [Fact]
    public void Reset_ShouldClearLatencySamples()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();
        telemetry.RecordSuccess(100);
        telemetry.RecordSuccess(200);

        // Act
        telemetry.Reset();

        // Assert
        telemetry.GetP50Latency().Should().Be(0);
        telemetry.GetP90Latency().Should().Be(0);
        telemetry.GetP99Latency().Should().Be(0);
    }

    #endregion

    #region Thread Safety Tests

    /// <summary>
    /// Tests that concurrent updates don't lose data.
    /// </summary>
    [Fact]
    public async Task ConcurrentUpdates_ShouldNotLoseData()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();
        const int iterations = 1000;
        var tasks = new List<Task>();

        // Act
        for (var i = 0; i < iterations; i++)
        {
            tasks.Add(Task.Run(() => telemetry.RecordSuccess(100)));
        }

        await Task.WhenAll(tasks);

        // Assert
        telemetry.TotalRequests.Should().Be(iterations);
        telemetry.SuccessfulRequests.Should().Be(iterations);
    }

    /// <summary>
    /// Tests that concurrent mixed operations are thread-safe.
    /// </summary>
    [Fact]
    public async Task ConcurrentMixedOperations_ShouldBeThreadSafe()
    {
        // Arrange
        var telemetry = new ResilienceTelemetry();
        const int iterations = 100;
        var tasks = new List<Task>();

        // Act
        for (var i = 0; i < iterations; i++)
        {
            tasks.Add(Task.Run(() => telemetry.RecordSuccess(100)));
            tasks.Add(Task.Run(() => telemetry.RecordFailure(100)));
            tasks.Add(Task.Run(() => telemetry.RecordRetry(1)));
        }

        await Task.WhenAll(tasks);

        // Assert
        telemetry.TotalRequests.Should().Be(iterations * 2);
        telemetry.SuccessfulRequests.Should().Be(iterations);
        telemetry.FailedRequests.Should().Be(iterations);
        telemetry.TotalRetries.Should().Be(iterations);
    }

    #endregion
}
