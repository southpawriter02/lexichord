// -----------------------------------------------------------------------
// <copyright file="ResilienceOptions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.LLM.Resilience;

/// <summary>
/// Configuration options for the LLM resilience pipeline.
/// </summary>
/// <remarks>
/// <para>
/// This record defines all configurable parameters for resilience policies including:
/// </para>
/// <list type="bullet">
///   <item><description>Retry policy with exponential backoff</description></item>
///   <item><description>Circuit breaker for fail-fast during sustained outages</description></item>
///   <item><description>Timeout policy for per-request time limits</description></item>
///   <item><description>Bulkhead policy for concurrent request limiting</description></item>
/// </list>
/// <para>
/// <b>Configuration Section:</b> <c>LLM:Resilience</c>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Configuration in appsettings.json
/// {
///   "LLM": {
///     "Resilience": {
///       "RetryCount": 3,
///       "RetryBaseDelaySeconds": 1.0,
///       "RetryMaxDelaySeconds": 30.0,
///       "CircuitBreakerThreshold": 5,
///       "CircuitBreakerDurationSeconds": 30,
///       "TimeoutSeconds": 30,
///       "BulkheadMaxConcurrency": 10,
///       "BulkheadMaxQueue": 100
///     }
///   }
/// }
/// </code>
/// </example>
/// <param name="RetryCount">
/// Number of retry attempts after initial failure. Must be non-negative.
/// Default: 3.
/// </param>
/// <param name="RetryBaseDelaySeconds">
/// Base delay for exponential backoff calculation (2^attempt * base).
/// Must be greater than 0. Default: 1.0 seconds.
/// </param>
/// <param name="RetryMaxDelaySeconds">
/// Maximum delay cap for retries to prevent unbounded waits.
/// Must be greater than 0. Default: 30.0 seconds.
/// </param>
/// <param name="CircuitBreakerThreshold">
/// Number of failures within the sampling duration before the circuit opens.
/// Must be at least 1. Default: 5.
/// </param>
/// <param name="CircuitBreakerDurationSeconds">
/// Duration the circuit stays open before transitioning to half-open.
/// Must be at least 1. Default: 30 seconds.
/// </param>
/// <param name="TimeoutSeconds">
/// Per-request timeout duration. Must be at least 1. Default: 30 seconds.
/// </param>
/// <param name="BulkheadMaxConcurrency">
/// Maximum number of concurrent requests allowed through the bulkhead.
/// Must be at least 1. Default: 10.
/// </param>
/// <param name="BulkheadMaxQueue">
/// Maximum number of requests that can wait in the bulkhead queue.
/// Must be non-negative. Default: 100.
/// </param>
public record ResilienceOptions(
    int RetryCount = 3,
    double RetryBaseDelaySeconds = 1.0,
    double RetryMaxDelaySeconds = 30.0,
    int CircuitBreakerThreshold = 5,
    int CircuitBreakerDurationSeconds = 30,
    int TimeoutSeconds = 30,
    int BulkheadMaxConcurrency = 10,
    int BulkheadMaxQueue = 100)
{
    /// <summary>
    /// The configuration section name for resilience options.
    /// </summary>
    /// <value><c>"LLM:Resilience"</c></value>
    public const string SectionName = "LLM:Resilience";

    /// <summary>
    /// Default resilience options with conservative settings suitable for most scenarios.
    /// </summary>
    public static ResilienceOptions Default { get; } = new();

    /// <summary>
    /// Aggressive resilience options with more retries and longer timeouts.
    /// </summary>
    /// <remarks>
    /// Suitable for batch processing or background operations where latency is less critical.
    /// </remarks>
    public static ResilienceOptions Aggressive { get; } = new(
        RetryCount: 5,
        RetryBaseDelaySeconds: 2.0,
        RetryMaxDelaySeconds: 60.0,
        CircuitBreakerThreshold: 10,
        CircuitBreakerDurationSeconds: 60,
        TimeoutSeconds: 60,
        BulkheadMaxConcurrency: 20,
        BulkheadMaxQueue: 200);

    /// <summary>
    /// Minimal resilience options with fast failure for real-time interactions.
    /// </summary>
    /// <remarks>
    /// Suitable for user-facing operations where responsiveness is critical.
    /// </remarks>
    public static ResilienceOptions Minimal { get; } = new(
        RetryCount: 1,
        RetryBaseDelaySeconds: 0.5,
        RetryMaxDelaySeconds: 5.0,
        CircuitBreakerThreshold: 3,
        CircuitBreakerDurationSeconds: 15,
        TimeoutSeconds: 10,
        BulkheadMaxConcurrency: 5,
        BulkheadMaxQueue: 25);

    /// <summary>
    /// Validates the options and throws <see cref="ArgumentOutOfRangeException"/> if any value is invalid.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when any configuration value is outside its valid range.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Validation rules:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><see cref="RetryCount"/> must be ≥ 0</description></item>
    ///   <item><description><see cref="RetryBaseDelaySeconds"/> must be > 0</description></item>
    ///   <item><description><see cref="RetryMaxDelaySeconds"/> must be > 0</description></item>
    ///   <item><description><see cref="CircuitBreakerThreshold"/> must be ≥ 1</description></item>
    ///   <item><description><see cref="CircuitBreakerDurationSeconds"/> must be ≥ 1</description></item>
    ///   <item><description><see cref="TimeoutSeconds"/> must be ≥ 1</description></item>
    ///   <item><description><see cref="BulkheadMaxConcurrency"/> must be ≥ 1</description></item>
    ///   <item><description><see cref="BulkheadMaxQueue"/> must be ≥ 0</description></item>
    /// </list>
    /// </remarks>
    public void Validate()
    {
        if (RetryCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(RetryCount),
                RetryCount,
                "RetryCount must be non-negative.");
        }

        if (RetryBaseDelaySeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(RetryBaseDelaySeconds),
                RetryBaseDelaySeconds,
                "RetryBaseDelaySeconds must be greater than zero.");
        }

        if (RetryMaxDelaySeconds <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(RetryMaxDelaySeconds),
                RetryMaxDelaySeconds,
                "RetryMaxDelaySeconds must be greater than zero.");
        }

        if (CircuitBreakerThreshold < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(CircuitBreakerThreshold),
                CircuitBreakerThreshold,
                "CircuitBreakerThreshold must be at least 1.");
        }

        if (CircuitBreakerDurationSeconds < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(CircuitBreakerDurationSeconds),
                CircuitBreakerDurationSeconds,
                "CircuitBreakerDurationSeconds must be at least 1.");
        }

        if (TimeoutSeconds < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(TimeoutSeconds),
                TimeoutSeconds,
                "TimeoutSeconds must be at least 1.");
        }

        if (BulkheadMaxConcurrency < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(BulkheadMaxConcurrency),
                BulkheadMaxConcurrency,
                "BulkheadMaxConcurrency must be at least 1.");
        }

        if (BulkheadMaxQueue < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(BulkheadMaxQueue),
                BulkheadMaxQueue,
                "BulkheadMaxQueue must be non-negative.");
        }
    }

    /// <summary>
    /// Attempts to validate the options and returns a list of validation errors.
    /// </summary>
    /// <returns>
    /// A list of validation error messages. Empty if validation passes.
    /// </returns>
    /// <remarks>
    /// Use this method when you want to collect all validation errors rather than
    /// failing on the first error.
    /// </remarks>
    public IReadOnlyList<string> GetValidationErrors()
    {
        var errors = new List<string>();

        if (RetryCount < 0)
        {
            errors.Add($"RetryCount must be non-negative (was {RetryCount}).");
        }

        if (RetryBaseDelaySeconds <= 0)
        {
            errors.Add($"RetryBaseDelaySeconds must be greater than zero (was {RetryBaseDelaySeconds}).");
        }

        if (RetryMaxDelaySeconds <= 0)
        {
            errors.Add($"RetryMaxDelaySeconds must be greater than zero (was {RetryMaxDelaySeconds}).");
        }

        if (CircuitBreakerThreshold < 1)
        {
            errors.Add($"CircuitBreakerThreshold must be at least 1 (was {CircuitBreakerThreshold}).");
        }

        if (CircuitBreakerDurationSeconds < 1)
        {
            errors.Add($"CircuitBreakerDurationSeconds must be at least 1 (was {CircuitBreakerDurationSeconds}).");
        }

        if (TimeoutSeconds < 1)
        {
            errors.Add($"TimeoutSeconds must be at least 1 (was {TimeoutSeconds}).");
        }

        if (BulkheadMaxConcurrency < 1)
        {
            errors.Add($"BulkheadMaxConcurrency must be at least 1 (was {BulkheadMaxConcurrency}).");
        }

        if (BulkheadMaxQueue < 0)
        {
            errors.Add($"BulkheadMaxQueue must be non-negative (was {BulkheadMaxQueue}).");
        }

        return errors;
    }

    /// <summary>
    /// Returns whether the options are valid according to all validation rules.
    /// </summary>
    /// <value>
    /// <c>true</c> if all options are within valid ranges; otherwise, <c>false</c>.
    /// </value>
    public bool IsValid => GetValidationErrors().Count == 0;

    /// <summary>
    /// Gets the retry base delay as a <see cref="TimeSpan"/>.
    /// </summary>
    public TimeSpan RetryBaseDelay => TimeSpan.FromSeconds(RetryBaseDelaySeconds);

    /// <summary>
    /// Gets the retry maximum delay as a <see cref="TimeSpan"/>.
    /// </summary>
    public TimeSpan RetryMaxDelay => TimeSpan.FromSeconds(RetryMaxDelaySeconds);

    /// <summary>
    /// Gets the circuit breaker duration as a <see cref="TimeSpan"/>.
    /// </summary>
    public TimeSpan CircuitBreakerDuration => TimeSpan.FromSeconds(CircuitBreakerDurationSeconds);

    /// <summary>
    /// Gets the timeout as a <see cref="TimeSpan"/>.
    /// </summary>
    public TimeSpan Timeout => TimeSpan.FromSeconds(TimeoutSeconds);
}
