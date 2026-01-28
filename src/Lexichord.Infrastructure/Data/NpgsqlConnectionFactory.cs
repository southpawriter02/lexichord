using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Lexichord.Abstractions.Contracts;

namespace Lexichord.Infrastructure.Data;

/// <summary>
/// Factory for creating PostgreSQL database connections with pooling and resilience.
/// </summary>
/// <remarks>
/// LOGIC: This factory wraps NpgsqlDataSource which manages connection pooling internally.
///
/// Architecture:
/// 1. NpgsqlDataSource provides connection pooling (configurable min/max)
/// 2. Polly RetryPolicy handles transient failures with exponential backoff
/// 3. Polly CircuitBreaker prevents cascade failures during outages
///
/// Connection flow:
/// CreateConnectionAsync() -> ResiliencePipeline -> NpgsqlDataSource.OpenConnectionAsync()
///
/// Transient errors that trigger retry:
/// - Network connectivity issues (08xxx error codes)
/// - Server shutdown (57Pxx error codes)
/// - Deadlock/serialization failures (40xxx error codes)
/// </remarks>
public sealed class NpgsqlConnectionFactory : IDbConnectionFactory
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<NpgsqlConnectionFactory> _logger;
    private readonly ResiliencePipeline<NpgsqlConnection> _resiliencePipeline;
    private readonly CircuitBreakerStateProvider _circuitBreakerState;

    /// <summary>
    /// Initializes a new instance of the NpgsqlConnectionFactory.
    /// </summary>
    /// <param name="options">Database configuration options.</param>
    /// <param name="logger">Logger instance.</param>
    public NpgsqlConnectionFactory(
        IOptions<DatabaseOptions> options,
        ILogger<NpgsqlConnectionFactory> logger)
    {
        _logger = logger;

        var dbOptions = options.Value;

        // LOGIC: Build connection string with pool settings
        var connectionBuilder = new NpgsqlConnectionStringBuilder(dbOptions.ConnectionString)
        {
            MaxPoolSize = dbOptions.MaxPoolSize,
            MinPoolSize = dbOptions.MinPoolSize,
            ConnectionIdleLifetime = dbOptions.ConnectionLifetimeSeconds,
            Timeout = dbOptions.ConnectionTimeoutSeconds,
            CommandTimeout = dbOptions.CommandTimeoutSeconds,
            Multiplexing = dbOptions.EnableMultiplexing,
            // Enable keepalive for long-lived connections
            KeepAlive = 60,
            // Include error detail in exceptions (development only)
            IncludeErrorDetail = true
        };

        // LOGIC: Build data source with the configured connection string
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionBuilder.ConnectionString);

        // Optional: Configure logging at the Npgsql level
        dataSourceBuilder.UseLoggerFactory(
            LoggerFactory.Create(builder => builder.AddProvider(
                new NpgsqlLoggingProvider(logger))));

        _dataSource = dataSourceBuilder.Build();

        // LOGIC: Create circuit breaker state provider for health checks
        _circuitBreakerState = new CircuitBreakerStateProvider();

        // LOGIC: Build resilience pipeline with retry and circuit breaker
        _resiliencePipeline = new ResiliencePipelineBuilder<NpgsqlConnection>()
            // Retry policy for transient failures
            .AddRetry(new RetryStrategyOptions<NpgsqlConnection>
            {
                ShouldHandle = new PredicateBuilder<NpgsqlConnection>()
                    .Handle<NpgsqlException>(IsTransientError)
                    .Handle<TimeoutException>()
                    .Handle<OperationCanceledException>(ex => ex.CancellationToken != default),
                MaxRetryAttempts = dbOptions.Retry.MaxRetryAttempts,
                Delay = TimeSpan.FromMilliseconds(dbOptions.Retry.BaseDelayMs),
                MaxDelay = TimeSpan.FromMilliseconds(dbOptions.Retry.MaxDelayMs),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = dbOptions.Retry.UseJitter,
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        args.Outcome.Exception,
                        "Database connection retry {AttemptNumber}/{MaxRetries} after {Delay}ms. " +
                        "Exception: {ExceptionType}",
                        args.AttemptNumber,
                        dbOptions.Retry.MaxRetryAttempts,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.GetType().Name ?? "None");
                    return ValueTask.CompletedTask;
                }
            })
            // Circuit breaker for catastrophic failures
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<NpgsqlConnection>
            {
                ShouldHandle = new PredicateBuilder<NpgsqlConnection>()
                    .Handle<NpgsqlException>(IsTransientError)
                    .Handle<TimeoutException>(),
                FailureRatio = dbOptions.CircuitBreaker.FailureRatio,
                SamplingDuration = TimeSpan.FromSeconds(dbOptions.CircuitBreaker.SamplingDurationSeconds),
                MinimumThroughput = dbOptions.CircuitBreaker.MinimumThroughput,
                BreakDuration = TimeSpan.FromSeconds(dbOptions.CircuitBreaker.BreakDurationSeconds),
                StateProvider = _circuitBreakerState,
                OnOpened = args =>
                {
                    _logger.LogError(
                        args.Outcome.Exception,
                        "Circuit breaker OPENED for database connections. " +
                        "Break duration: {BreakDuration}s. " +
                        "Requests will fail fast until circuit closes.",
                        args.BreakDuration.TotalSeconds);
                    return ValueTask.CompletedTask;
                },
                OnClosed = args =>
                {
                    _logger.LogInformation(
                        "Circuit breaker CLOSED. Database connections restored. " +
                        "Normal operation resumed.");
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = args =>
                {
                    _logger.LogInformation(
                        "Circuit breaker HALF-OPEN. Testing database connection...");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();

        _logger.LogInformation(
            "Database connection factory initialized. " +
            "Pool: {MinPool}-{MaxPool}, " +
            "Multiplexing: {Multiplexing}, " +
            "Retry: {MaxRetries} attempts, " +
            "CircuitBreaker: {FailureRatio:P0} threshold",
            dbOptions.MinPoolSize,
            dbOptions.MaxPoolSize,
            dbOptions.EnableMultiplexing,
            dbOptions.Retry.MaxRetryAttempts,
            dbOptions.CircuitBreaker.FailureRatio);
    }

    /// <inheritdoc/>
    public NpgsqlDataSource DataSource => _dataSource;

    /// <inheritdoc/>
    public bool IsHealthy => _circuitBreakerState.CircuitState != CircuitState.Open;

    /// <inheritdoc/>
    public async Task<NpgsqlConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var connection = await _resiliencePipeline.ExecuteAsync(
                async ct =>
                {
                    var conn = await _dataSource.OpenConnectionAsync(ct);
                    return conn;
                },
                cancellationToken);

            var elapsed = DateTime.UtcNow - startTime;

            _logger.LogDebug(
                "Database connection acquired in {ElapsedMs}ms. Pool statistics: {PoolStats}",
                elapsed.TotalMilliseconds,
                GetPoolStatistics());

            return connection;
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(
                "Database connection failed: Circuit breaker is open. " +
                "Retry after {RetryAfter}",
                ex.RetryAfter);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await CreateConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database connectivity check failed");
            return false;
        }
    }

    /// <summary>
    /// Determines if an NpgsqlException represents a transient error that should be retried.
    /// </summary>
    /// <param name="ex">The exception to evaluate.</param>
    /// <returns>True if the error is transient; otherwise, false.</returns>
    /// <remarks>
    /// LOGIC: PostgreSQL error codes are defined in the SQL standard and Postgres extensions.
    /// Class 08 = Connection exceptions
    /// Class 40 = Transaction rollback (deadlock, serialization)
    /// Class 57 = Operator intervention (shutdown, crash recovery)
    ///
    /// Reference: https://www.postgresql.org/docs/current/errcodes-appendix.html
    /// </remarks>
    internal static bool IsTransientError(NpgsqlException ex)
    {
        // Check Npgsql's built-in transient detection first
        if (ex.IsTransient)
            return true;

        // Check specific SQL state codes for transient conditions
        return ex.SqlState switch
        {
            // Class 08 - Connection Exception
            "08000" => true,  // connection_exception
            "08003" => true,  // connection_does_not_exist
            "08006" => true,  // connection_failure
            "08001" => true,  // sqlclient_unable_to_establish_sqlconnection
            "08004" => true,  // sqlserver_rejected_establishment_of_sqlconnection
            "08007" => true,  // transaction_resolution_unknown
            "08P01" => true,  // protocol_violation

            // Class 40 - Transaction Rollback
            "40001" => true,  // serialization_failure
            "40002" => true,  // transaction_integrity_constraint_violation
            "40003" => true,  // statement_completion_unknown
            "40P01" => true,  // deadlock_detected

            // Class 53 - Insufficient Resources
            "53000" => true,  // insufficient_resources
            "53100" => true,  // disk_full
            "53200" => true,  // out_of_memory
            "53300" => true,  // too_many_connections

            // Class 57 - Operator Intervention
            "57000" => true,  // operator_intervention
            "57014" => true,  // query_canceled
            "57P01" => true,  // admin_shutdown
            "57P02" => true,  // crash_shutdown
            "57P03" => true,  // cannot_connect_now
            "57P04" => true,  // database_dropped

            // Class 58 - System Error
            "58000" => true,  // system_error
            "58030" => true,  // io_error

            // Not a transient error
            _ => false
        };
    }

    /// <summary>
    /// Gets pool statistics for logging.
    /// </summary>
    /// <remarks>
    /// Note: NpgsqlDataSource.Statistics is internal in Npgsql 9.x.
    /// For now, we return a placeholder. In production, consider using
    /// NpgsqlDataSource.GetStatistics() if it becomes available.
    /// </remarks>
    private static string GetPoolStatistics()
    {
        // Statistics API is internal in Npgsql 9.x
        return "(pool stats unavailable in Npgsql 9.x)";
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _dataSource.Dispose();
        _logger.LogDebug("Database connection factory disposed");
    }
}

/// <summary>
/// Custom Npgsql logging provider for routing logs to Microsoft.Extensions.Logging.
/// </summary>
internal sealed class NpgsqlLoggingProvider(ILogger logger) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new NpgsqlLoggerAdapter(logger, categoryName);
    }

    public void Dispose() { }

    private sealed class NpgsqlLoggerAdapter : ILogger
    {
        private readonly ILogger _innerLogger;

        public NpgsqlLoggerAdapter(ILogger logger, string categoryName)
        {
            _innerLogger = logger;
            // categoryName could be used for filtering but we route all logs through the parent
            _ = categoryName; // Suppress unused warning
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            => _innerLogger.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => _innerLogger.IsEnabled(logLevel);

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            _innerLogger.Log(logLevel, eventId, state, exception, formatter);
        }
    }
}
