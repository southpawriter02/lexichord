// -----------------------------------------------------------------------
// <copyright file="ResilienceEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.LLM.Resilience;

/// <summary>
/// Represents a telemetry event from resilience policies.
/// </summary>
/// <remarks>
/// <para>
/// This record captures information about policy actions such as retries,
/// circuit breaker state changes, timeouts, and bulkhead rejections.
/// It is used for observability and metrics collection.
/// </para>
/// </remarks>
/// <param name="PolicyName">
/// The name of the policy that generated the event (e.g., "Retry", "CircuitBreaker", "Timeout", "Bulkhead").
/// </param>
/// <param name="EventType">
/// The type of event that occurred (e.g., "Retry", "Break", "Reset", "HalfOpen", "Timeout", "Rejected").
/// </param>
/// <param name="Duration">
/// The duration associated with the event, if applicable (e.g., retry delay, timeout duration).
/// </param>
/// <param name="Exception">
/// The exception that triggered the event, if applicable.
/// </param>
/// <param name="AttemptNumber">
/// The retry attempt number, if applicable. Starts at 1 for the first retry.
/// </param>
public record ResilienceEvent(
    string PolicyName,
    string EventType,
    TimeSpan? Duration,
    Exception? Exception,
    int? AttemptNumber)
{
    /// <summary>
    /// Event types for retry policy events.
    /// </summary>
    public static class RetryEventTypes
    {
        /// <summary>
        /// A retry attempt is being made.
        /// </summary>
        public const string Retry = "Retry";

        /// <summary>
        /// A retry delay is calculated using exponential backoff.
        /// </summary>
        public const string Backoff = "Backoff";

        /// <summary>
        /// A retry delay is using the Retry-After header value.
        /// </summary>
        public const string RetryAfter = "RetryAfter";
    }

    /// <summary>
    /// Event types for circuit breaker policy events.
    /// </summary>
    public static class CircuitBreakerEventTypes
    {
        /// <summary>
        /// The circuit has opened (tripped) due to failures.
        /// </summary>
        public const string Break = "Break";

        /// <summary>
        /// The circuit has reset to closed state after successful recovery.
        /// </summary>
        public const string Reset = "Reset";

        /// <summary>
        /// The circuit has transitioned to half-open state for testing.
        /// </summary>
        public const string HalfOpen = "HalfOpen";

        /// <summary>
        /// The circuit has been manually isolated.
        /// </summary>
        public const string Isolated = "Isolated";
    }

    /// <summary>
    /// Event types for timeout policy events.
    /// </summary>
    public static class TimeoutEventTypes
    {
        /// <summary>
        /// A request has timed out.
        /// </summary>
        public const string Timeout = "Timeout";
    }

    /// <summary>
    /// Event types for bulkhead policy events.
    /// </summary>
    public static class BulkheadEventTypes
    {
        /// <summary>
        /// A request was rejected because the bulkhead is at capacity.
        /// </summary>
        public const string Rejected = "Rejected";
    }

    /// <summary>
    /// Policy names used in resilience events.
    /// </summary>
    public static class PolicyNames
    {
        /// <summary>
        /// The retry policy name.
        /// </summary>
        public const string Retry = "Retry";

        /// <summary>
        /// The circuit breaker policy name.
        /// </summary>
        public const string CircuitBreaker = "CircuitBreaker";

        /// <summary>
        /// The timeout policy name.
        /// </summary>
        public const string Timeout = "Timeout";

        /// <summary>
        /// The bulkhead policy name.
        /// </summary>
        public const string Bulkhead = "Bulkhead";
    }

    /// <summary>
    /// Creates a retry event.
    /// </summary>
    /// <param name="attemptNumber">The retry attempt number (1-based).</param>
    /// <param name="delay">The delay before the retry.</param>
    /// <param name="exception">The exception that triggered the retry, if any.</param>
    /// <returns>A new resilience event for a retry.</returns>
    public static ResilienceEvent CreateRetry(int attemptNumber, TimeSpan delay, Exception? exception = null)
        => new(PolicyNames.Retry, RetryEventTypes.Retry, delay, exception, attemptNumber);

    /// <summary>
    /// Creates a circuit breaker break event.
    /// </summary>
    /// <param name="breakDuration">The duration the circuit will remain open.</param>
    /// <param name="exception">The exception that triggered the break, if any.</param>
    /// <returns>A new resilience event for a circuit break.</returns>
    public static ResilienceEvent CreateCircuitBreak(TimeSpan breakDuration, Exception? exception = null)
        => new(PolicyNames.CircuitBreaker, CircuitBreakerEventTypes.Break, breakDuration, exception, null);

    /// <summary>
    /// Creates a circuit breaker reset event.
    /// </summary>
    /// <returns>A new resilience event for a circuit reset.</returns>
    public static ResilienceEvent CreateCircuitReset()
        => new(PolicyNames.CircuitBreaker, CircuitBreakerEventTypes.Reset, null, null, null);

    /// <summary>
    /// Creates a circuit breaker half-open event.
    /// </summary>
    /// <returns>A new resilience event for a circuit half-open transition.</returns>
    public static ResilienceEvent CreateCircuitHalfOpen()
        => new(PolicyNames.CircuitBreaker, CircuitBreakerEventTypes.HalfOpen, null, null, null);

    /// <summary>
    /// Creates a timeout event.
    /// </summary>
    /// <param name="timeoutDuration">The timeout duration that elapsed.</param>
    /// <returns>A new resilience event for a timeout.</returns>
    public static ResilienceEvent CreateTimeout(TimeSpan timeoutDuration)
        => new(PolicyNames.Timeout, TimeoutEventTypes.Timeout, timeoutDuration, null, null);

    /// <summary>
    /// Creates a bulkhead rejection event.
    /// </summary>
    /// <returns>A new resilience event for a bulkhead rejection.</returns>
    public static ResilienceEvent CreateBulkheadRejection()
        => new(PolicyNames.Bulkhead, BulkheadEventTypes.Rejected, null, null, null);

    /// <summary>
    /// Gets whether this event represents a retry operation.
    /// </summary>
    public bool IsRetry => PolicyName == PolicyNames.Retry && EventType == RetryEventTypes.Retry;

    /// <summary>
    /// Gets whether this event represents a circuit breaker opening.
    /// </summary>
    public bool IsCircuitBreak => PolicyName == PolicyNames.CircuitBreaker && EventType == CircuitBreakerEventTypes.Break;

    /// <summary>
    /// Gets whether this event represents a timeout.
    /// </summary>
    public bool IsTimeout => PolicyName == PolicyNames.Timeout && EventType == TimeoutEventTypes.Timeout;

    /// <summary>
    /// Gets whether this event represents a bulkhead rejection.
    /// </summary>
    public bool IsBulkheadRejection => PolicyName == PolicyNames.Bulkhead && EventType == BulkheadEventTypes.Rejected;

    /// <summary>
    /// Gets the timestamp when this event was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
