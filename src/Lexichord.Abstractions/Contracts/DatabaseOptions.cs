namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Configuration options for database connectivity.
/// </summary>
/// <remarks>
/// LOGIC: These options are loaded from appsettings.json and environment variables.
/// Connection strings should NEVER be committed to source control.
///
/// Configuration precedence (highest to lowest):
/// 1. Environment variables (LEXICHORD_DATABASE__CONNECTIONSTRING)
/// 2. appsettings.{Environment}.json
/// 3. appsettings.json
///
/// Pooling defaults are optimized for desktop applications with moderate concurrency.
/// Server applications may need larger pool sizes.
/// </remarks>
/// <example>
/// appsettings.json:
/// {
///   "Database": {
///     "ConnectionString": "Host=localhost;Database=lexichord;...",
///     "MaxPoolSize": 100,
///     "MinPoolSize": 10
///   }
/// }
/// </example>
public sealed record DatabaseOptions
{
    /// <summary>
    /// Configuration section name for binding.
    /// </summary>
    public const string SectionName = "Database";

    /// <summary>
    /// The PostgreSQL connection string.
    /// </summary>
    /// <example>Host=localhost;Port=5432;Database=lexichord;Username=lexichord;Password=secret</example>
    public required string ConnectionString { get; init; }

    /// <summary>
    /// Maximum number of connections in the pool.
    /// </summary>
    /// <remarks>
    /// LOGIC: Default 100 matches Npgsql default. For desktop apps, 20-50 is usually sufficient.
    /// Monitor pool exhaustion in production and adjust accordingly.
    /// </remarks>
    public int MaxPoolSize { get; init; } = 100;

    /// <summary>
    /// Minimum number of connections to maintain in the pool.
    /// </summary>
    /// <remarks>
    /// LOGIC: Keeping warm connections reduces latency for first operations after idle.
    /// Set to 0 for minimal resource usage, higher for better responsiveness.
    /// </remarks>
    public int MinPoolSize { get; init; } = 10;

    /// <summary>
    /// Maximum lifetime of a connection in seconds before recycling.
    /// </summary>
    /// <remarks>
    /// LOGIC: Recycling connections helps with load balancing when using pgBouncer
    /// or cloud database proxies. Default 300s (5 minutes).
    /// </remarks>
    public int ConnectionLifetimeSeconds { get; init; } = 300;

    /// <summary>
    /// Connection timeout in seconds.
    /// </summary>
    /// <remarks>
    /// LOGIC: Time to wait when acquiring a connection from pool or opening new one.
    /// Shorter timeout fails fast; longer timeout handles temporary network issues.
    /// </remarks>
    public int ConnectionTimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// Command timeout in seconds for query execution.
    /// </summary>
    /// <remarks>
    /// LOGIC: Maximum time a query can run before being cancelled.
    /// Set based on expected query complexity. Default 30s handles most operations.
    /// Long-running reports may need higher values.
    /// </remarks>
    public int CommandTimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// Enable multiplexing for improved throughput.
    /// </summary>
    /// <remarks>
    /// LOGIC: Multiplexing allows multiple commands on a single physical connection.
    /// This dramatically reduces connection overhead for async workloads.
    /// Disable if experiencing compatibility issues with older PostgreSQL or proxies.
    /// </remarks>
    public bool EnableMultiplexing { get; init; } = true;

    /// <summary>
    /// Retry policy settings.
    /// </summary>
    public RetryOptions Retry { get; init; } = new();

    /// <summary>
    /// Circuit breaker policy settings.
    /// </summary>
    public CircuitBreakerOptions CircuitBreaker { get; init; } = new();
}

/// <summary>
/// Retry policy configuration.
/// </summary>
public sealed record RetryOptions
{
    /// <summary>
    /// Maximum number of retry attempts.
    /// </summary>
    public int MaxRetryAttempts { get; init; } = 4;

    /// <summary>
    /// Base delay between retries in milliseconds.
    /// </summary>
    /// <remarks>
    /// LOGIC: With exponential backoff, delays are: 1s, 2s, 4s, 8s.
    /// Jitter adds randomness to prevent thundering herd.
    /// </remarks>
    public int BaseDelayMs { get; init; } = 1000;

    /// <summary>
    /// Maximum delay between retries in milliseconds.
    /// </summary>
    public int MaxDelayMs { get; init; } = 30000;

    /// <summary>
    /// Enable jitter to randomize retry delays.
    /// </summary>
    public bool UseJitter { get; init; } = true;
}

/// <summary>
/// Circuit breaker policy configuration.
/// </summary>
public sealed record CircuitBreakerOptions
{
    /// <summary>
    /// Failure ratio threshold to open the circuit (0.0 to 1.0).
    /// </summary>
    /// <remarks>
    /// LOGIC: 0.5 means circuit opens if 50% of requests fail within the sampling window.
    /// Lower values are more sensitive, higher values are more tolerant.
    /// </remarks>
    public double FailureRatio { get; init; } = 0.5;

    /// <summary>
    /// Time window in seconds for sampling failures.
    /// </summary>
    public int SamplingDurationSeconds { get; init; } = 30;

    /// <summary>
    /// Minimum number of requests before the circuit can open.
    /// </summary>
    /// <remarks>
    /// LOGIC: Prevents circuit from opening due to a single failure during low traffic.
    /// </remarks>
    public int MinimumThroughput { get; init; } = 5;

    /// <summary>
    /// Duration in seconds the circuit stays open before testing.
    /// </summary>
    public int BreakDurationSeconds { get; init; } = 30;
}
