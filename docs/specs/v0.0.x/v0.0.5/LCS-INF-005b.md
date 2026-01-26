# LCS-INF-005b: Database Connector

## 1. Metadata & Categorization

| Field                | Value                                | Description                                  |
| :------------------- | :----------------------------------- | :------------------------------------------- |
| **Feature ID**       | `INF-005b`                           | Infrastructure - Database Connector          |
| **Feature Name**     | Database Connector                   | Npgsql with Connection Pooling & Polly       |
| **Target Version**   | `v0.0.5b`                            | Second sub-part of v0.0.5                    |
| **Module Scope**     | `Lexichord.Infrastructure`           | Data access infrastructure                   |
| **Swimlane**         | `Infrastructure`                     | The Podium (Platform)                        |
| **License Tier**     | `Core`                               | Foundation (Required for all tiers)          |
| **Author**           | System Architect                     |                                              |
| **Status**           | **Draft**                            | Pending implementation                       |
| **Last Updated**     | 2026-01-26                           |                                              |

---

## 2. Executive Summary

### 2.1 The Requirement

Lexichord requires a **resilient database connection layer** that:

- Manages connection pooling for efficient resource usage.
- Handles transient failures with automatic retry.
- Implements circuit breaker pattern to prevent cascade failures.
- Provides observable connection metrics for diagnostics.

Without this foundation:

- Each database operation creates a new connection (expensive).
- Network glitches crash the entire application.
- Database overload cascades to application failure.
- Connection issues are impossible to diagnose.

### 2.2 The Proposed Solution

We **SHALL** implement Npgsql with Polly resilience using:

1. **NpgsqlDataSource** — Modern connection pooling with multiplexing.
2. **DatabaseOptions** — Strongly-typed configuration for connection settings.
3. **Polly Retry Policy** — Exponential backoff for transient failures.
4. **Polly Circuit Breaker** — Fail-fast protection during outages.

---

## 3. Implementation Tasks

### Task 1.1: Install NuGet Packages

**File:** `src/Lexichord.Infrastructure/Lexichord.Infrastructure.csproj`

```xml
<ItemGroup>
  <!-- Database Connectivity -->
  <PackageReference Include="Npgsql" Version="9.0.2" />

  <!-- Resilience -->
  <PackageReference Include="Polly" Version="8.5.0" />
  <PackageReference Include="Polly.Extensions" Version="8.5.0" />

  <!-- Configuration & DI -->
  <PackageReference Include="Microsoft.Extensions.Options" Version="9.0.0" />
  <PackageReference Include="Microsoft.Extensions.Options.ConfigurationExtensions" Version="9.0.0" />
  <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="9.0.0" />
  <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="9.0.0" />
</ItemGroup>
```

**Rationale:**

- `Npgsql` 9.x provides the modern `NpgsqlDataSource` API with built-in pooling.
- `Polly` 8.x offers the new `ResiliencePipeline` API for composable policies.
- Configuration extensions enable strongly-typed options binding.

---

### Task 1.2: Define Database Configuration Options

**File:** `src/Lexichord.Abstractions/Contracts/DatabaseOptions.cs`

```csharp
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
```

---

### Task 1.3: Define Connection Factory Interface

**File:** `src/Lexichord.Abstractions/Contracts/IDbConnectionFactory.cs`

```csharp
using System.Data;
using Npgsql;

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Factory for creating database connections with pooling support.
/// </summary>
/// <remarks>
/// LOGIC: This interface abstracts the connection creation process, enabling:
/// - Mocking for unit tests
/// - Connection pooling via NpgsqlDataSource
/// - Resilience policies (retry, circuit breaker)
///
/// Implementations should:
/// - Return connections from a pool where possible
/// - Apply resilience policies for transient failures
/// - Log connection events for observability
/// </remarks>
public interface IDbConnectionFactory : IDisposable
{
    /// <summary>
    /// Creates and opens a database connection from the pool.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An open database connection.</returns>
    /// <remarks>
    /// LOGIC: The returned connection is pooled. Always dispose it when done
    /// to return it to the pool. Using statements are recommended:
    /// <code>
    /// await using var connection = await factory.CreateConnectionAsync();
    /// // Use connection
    /// // Connection returns to pool when disposed
    /// </code>
    /// </remarks>
    /// <exception cref="Polly.CircuitBreaker.BrokenCircuitException">
    /// Thrown when the circuit breaker is open due to repeated failures.
    /// </exception>
    Task<NpgsqlConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the underlying data source for advanced scenarios.
    /// </summary>
    /// <remarks>
    /// LOGIC: Direct access to NpgsqlDataSource enables:
    /// - Bulk operations (COPY)
    /// - Notifications (LISTEN/NOTIFY)
    /// - Connection-level settings
    ///
    /// Use with caution; prefer CreateConnectionAsync for most scenarios.
    /// </remarks>
    NpgsqlDataSource DataSource { get; }

    /// <summary>
    /// Gets a value indicating whether the connection factory is healthy.
    /// </summary>
    /// <remarks>
    /// LOGIC: Returns false when circuit breaker is open.
    /// Use for health checks and readiness probes.
    /// </remarks>
    bool IsHealthy { get; }

    /// <summary>
    /// Attempts to verify connectivity to the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if connection successful, false otherwise.</returns>
    Task<bool> CanConnectAsync(CancellationToken cancellationToken = default);
}
```

---

### Task 1.4: Implement NpgsqlConnectionFactory

**File:** `src/Lexichord.Infrastructure/Data/NpgsqlConnectionFactory.cs`

```csharp
using System.Data;
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
    private static bool IsTransientError(NpgsqlException ex)
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
    private string GetPoolStatistics()
    {
        try
        {
            var stats = _dataSource.Statistics;
            return $"Idle={stats.Idle}, Busy={stats.Busy}, Total={stats.Total}";
        }
        catch
        {
            return "unavailable";
        }
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

    private sealed class NpgsqlLoggerAdapter(ILogger logger, string categoryName) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            => logger.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => logger.IsEnabled(logLevel);

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            logger.Log(logLevel, eventId, state, exception, formatter);
        }
    }
}
```

---

### Task 1.5: Register Services in DI Container

**File:** `src/Lexichord.Infrastructure/InfrastructureServices.cs`

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Lexichord.Abstractions.Contracts;
using Lexichord.Infrastructure.Data;

namespace Lexichord.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure services.
/// </summary>
public static class InfrastructureServices
{
    /// <summary>
    /// Adds database connectivity services to the DI container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDatabaseServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // LOGIC: Bind configuration options from appsettings.json
        services.Configure<DatabaseOptions>(
            configuration.GetSection(DatabaseOptions.SectionName));

        // LOGIC: Register connection factory as singleton
        // The factory maintains the connection pool which should live for app lifetime
        services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>();

        return services;
    }
}
```

**Usage in Host:**

```csharp
// In Program.cs or App.axaml.cs
services.AddDatabaseServices(configuration);
```

---

## 4. Decision Tree: Connection Acquisition

```text
START: "I need a database connection"
|
+-- CreateConnectionAsync() called
|   |
|   +-- Is circuit breaker OPEN?
|   |   +-- YES -> Throw BrokenCircuitException immediately
|   |   |         (Fail fast, no connection attempt)
|   |   |
|   |   +-- NO -> Continue to connection attempt
|   |
|   +-- Request connection from NpgsqlDataSource pool
|   |   |
|   |   +-- Pool has idle connection?
|   |   |   +-- YES -> Return existing connection (fast path)
|   |   |   |
|   |   |   +-- NO -> Pool at MaxPoolSize?
|   |   |       +-- YES -> Wait for connection to return to pool
|   |   |       |         (up to ConnectionTimeoutSeconds)
|   |   |       |
|   |   |       +-- NO -> Open new physical connection to database
|   |   |
|   |   +-- Connection attempt failed?
|   |       +-- Is error transient? (network, timeout, deadlock)
|   |       |   +-- YES -> Retry with exponential backoff
|   |       |   |         (1s, 2s, 4s, 8s)
|   |       |   |   |
|   |       |   |   +-- Max retries exceeded?
|   |       |   |       +-- Record failure in circuit breaker
|   |       |   |       +-- Throw original exception
|   |       |   |
|   |       |   +-- NO -> Throw immediately (non-transient error)
|   |       |             (e.g., authentication failure, invalid query)
|   |
|   +-- Connection returned successfully
|       +-- Log pool statistics
|       +-- Return connection to caller
|
+-- Caller uses connection (await using recommended)
|
+-- Connection disposed -> Returns to pool for reuse
```

---

## 5. Unit Testing Requirements

### 5.1 Test: Database Options Validation

```csharp
[TestFixture]
[Category("Unit")]
public class DatabaseOptionsTests
{
    [Test]
    public void Defaults_AreReasonable()
    {
        // Arrange & Act
        var options = new DatabaseOptions
        {
            ConnectionString = "Host=localhost;Database=test"
        };

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(options.MaxPoolSize, Is.EqualTo(100));
            Assert.That(options.MinPoolSize, Is.EqualTo(10));
            Assert.That(options.ConnectionTimeoutSeconds, Is.EqualTo(30));
            Assert.That(options.CommandTimeoutSeconds, Is.EqualTo(30));
            Assert.That(options.EnableMultiplexing, Is.True);
            Assert.That(options.Retry.MaxRetryAttempts, Is.EqualTo(4));
            Assert.That(options.CircuitBreaker.FailureRatio, Is.EqualTo(0.5));
        });
    }

    [Test]
    public void SectionName_IsCorrect()
    {
        Assert.That(DatabaseOptions.SectionName, Is.EqualTo("Database"));
    }
}
```

### 5.2 Test: Connection Factory Initialization

```csharp
[TestFixture]
[Category("Unit")]
public class NpgsqlConnectionFactoryTests
{
    private Mock<ILogger<NpgsqlConnectionFactory>> _mockLogger = null!;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<NpgsqlConnectionFactory>>();
    }

    [Test]
    public void Constructor_LogsInitialization()
    {
        // Arrange
        var options = Options.Create(new DatabaseOptions
        {
            ConnectionString = "Host=localhost;Database=test;Username=test;Password=test",
            MaxPoolSize = 50,
            MinPoolSize = 5,
            EnableMultiplexing = false
        });

        // Act
        using var sut = new NpgsqlConnectionFactory(options, _mockLogger.Object);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains("Database connection factory initialized") &&
                    v.ToString()!.Contains("5-50")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public void DataSource_IsNotNull()
    {
        // Arrange
        var options = Options.Create(new DatabaseOptions
        {
            ConnectionString = "Host=localhost;Database=test;Username=test;Password=test"
        });

        // Act
        using var sut = new NpgsqlConnectionFactory(options, _mockLogger.Object);

        // Assert
        Assert.That(sut.DataSource, Is.Not.Null);
    }

    [Test]
    public void IsHealthy_InitiallyTrue()
    {
        // Arrange
        var options = Options.Create(new DatabaseOptions
        {
            ConnectionString = "Host=localhost;Database=test;Username=test;Password=test"
        });

        // Act
        using var sut = new NpgsqlConnectionFactory(options, _mockLogger.Object);

        // Assert
        Assert.That(sut.IsHealthy, Is.True);
    }

    [Test]
    public void Dispose_LogsDisposal()
    {
        // Arrange
        var options = Options.Create(new DatabaseOptions
        {
            ConnectionString = "Host=localhost;Database=test;Username=test;Password=test"
        });
        var sut = new NpgsqlConnectionFactory(options, _mockLogger.Object);

        // Act
        sut.Dispose();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("disposed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
```

### 5.3 Test: Transient Error Detection

```csharp
[TestFixture]
[Category("Unit")]
public class TransientErrorDetectionTests
{
    [TestCase("08000", true, Description = "connection_exception")]
    [TestCase("08006", true, Description = "connection_failure")]
    [TestCase("40001", true, Description = "serialization_failure")]
    [TestCase("40P01", true, Description = "deadlock_detected")]
    [TestCase("57P01", true, Description = "admin_shutdown")]
    [TestCase("57P03", true, Description = "cannot_connect_now")]
    [TestCase("53300", true, Description = "too_many_connections")]
    [TestCase("23505", false, Description = "unique_violation")]
    [TestCase("23503", false, Description = "foreign_key_violation")]
    [TestCase("42P01", false, Description = "undefined_table")]
    [TestCase("28P01", false, Description = "invalid_password")]
    public void IsTransientError_ClassifiesCorrectly(string sqlState, bool expectedTransient)
    {
        // Arrange
        var exception = CreateNpgsqlExceptionWithState(sqlState);

        // Act
        var result = IsTransientError(exception);

        // Assert
        Assert.That(result, Is.EqualTo(expectedTransient),
            $"SqlState {sqlState} should be {(expectedTransient ? "transient" : "non-transient")}");
    }

    // Helper to create NpgsqlException with specific SqlState
    // Note: This requires reflection or a test double since NpgsqlException is not easily constructible
    private static NpgsqlException CreateNpgsqlExceptionWithState(string sqlState)
    {
        // Implementation depends on Npgsql version
        // May need to use mock or derived test class
        throw new NotImplementedException("Implement based on test infrastructure");
    }

    private static bool IsTransientError(NpgsqlException ex)
    {
        // Copy of the method from NpgsqlConnectionFactory for testing
        if (ex.IsTransient) return true;

        return ex.SqlState switch
        {
            "08000" or "08003" or "08006" or "08001" or "08004" or "08007" or "08P01" => true,
            "40001" or "40002" or "40003" or "40P01" => true,
            "53000" or "53100" or "53200" or "53300" => true,
            "57000" or "57014" or "57P01" or "57P02" or "57P03" or "57P04" => true,
            "58000" or "58030" => true,
            _ => false
        };
    }
}
```

### 5.4 Integration Test: Connection Lifecycle

```csharp
[TestFixture]
[Category("Integration")]
[Explicit("Requires running PostgreSQL")]
public class ConnectionFactoryIntegrationTests
{
    private NpgsqlConnectionFactory _sut = null!;
    private Mock<ILogger<NpgsqlConnectionFactory>> _mockLogger = null!;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<NpgsqlConnectionFactory>>();

        var options = Options.Create(new DatabaseOptions
        {
            ConnectionString = GetTestConnectionString(),
            MaxPoolSize = 5,
            MinPoolSize = 1,
            EnableMultiplexing = false // Easier to test without multiplexing
        });

        _sut = new NpgsqlConnectionFactory(options, _mockLogger.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _sut.Dispose();
    }

    [Test]
    public async Task CreateConnectionAsync_ReturnsOpenConnection()
    {
        // Act
        await using var connection = await _sut.CreateConnectionAsync();

        // Assert
        Assert.That(connection.State, Is.EqualTo(ConnectionState.Open));
    }

    [Test]
    public async Task CreateConnectionAsync_CanExecuteQuery()
    {
        // Arrange
        await using var connection = await _sut.CreateConnectionAsync();

        // Act
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 AS result";
        var result = await command.ExecuteScalarAsync();

        // Assert
        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public async Task CreateConnectionAsync_PoolsConnections()
    {
        // Arrange - Get first connection and note its process ID
        int firstPid;
        await using (var conn1 = await _sut.CreateConnectionAsync())
        {
            firstPid = conn1.ProcessID;
        }

        // Act - Get second connection (should reuse from pool)
        await using var conn2 = await _sut.CreateConnectionAsync();
        var secondPid = conn2.ProcessID;

        // Assert - Same backend process means connection was reused
        Assert.That(secondPid, Is.EqualTo(firstPid),
            "Connection should be reused from pool");
    }

    [Test]
    public async Task CanConnectAsync_ReturnsTrueWhenDatabaseAvailable()
    {
        // Act
        var result = await _sut.CanConnectAsync();

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task CreateConnectionAsync_MultipleConcurrentConnections()
    {
        // Arrange
        const int connectionCount = 5;
        var tasks = new List<Task<NpgsqlConnection>>();

        // Act
        for (var i = 0; i < connectionCount; i++)
        {
            tasks.Add(_sut.CreateConnectionAsync());
        }

        var connections = await Task.WhenAll(tasks);

        // Assert
        Assert.That(connections.All(c => c.State == ConnectionState.Open), Is.True);

        // Cleanup
        foreach (var conn in connections)
        {
            await conn.DisposeAsync();
        }
    }

    private static string GetTestConnectionString()
    {
        return Environment.GetEnvironmentVariable("LEXICHORD_TEST_DB")
            ?? "Host=localhost;Port=5432;Database=lexichord_test;Username=lexichord;Password=lexichord_dev";
    }
}
```

---

## 6. Observability & Logging

### 6.1 Log Events

| Level       | Context                  | Message Template                                                                 |
| :---------- | :----------------------- | :------------------------------------------------------------------------------- |
| Information | NpgsqlConnectionFactory  | `Database connection factory initialized. Pool: {MinPool}-{MaxPool}, Multiplexing: {Multiplexing}, Retry: {MaxRetries} attempts, CircuitBreaker: {FailureRatio:P0} threshold` |
| Debug       | NpgsqlConnectionFactory  | `Database connection acquired in {ElapsedMs}ms. Pool statistics: {PoolStats}`    |
| Warning     | NpgsqlConnectionFactory  | `Database connection retry {AttemptNumber}/{MaxRetries} after {Delay}ms. Exception: {ExceptionType}` |
| Error       | NpgsqlConnectionFactory  | `Circuit breaker OPENED for database connections. Break duration: {BreakDuration}s. Requests will fail fast until circuit closes.` |
| Information | NpgsqlConnectionFactory  | `Circuit breaker CLOSED. Database connections restored. Normal operation resumed.` |
| Information | NpgsqlConnectionFactory  | `Circuit breaker HALF-OPEN. Testing database connection...`                      |
| Error       | NpgsqlConnectionFactory  | `Database connection failed: Circuit breaker is open. Retry after {RetryAfter}`  |
| Warning     | NpgsqlConnectionFactory  | `Database connectivity check failed`                                             |
| Debug       | NpgsqlConnectionFactory  | `Database connection factory disposed`                                           |

### 6.2 Structured Log Properties

All log events include these enriched properties:

| Property        | Type   | Description                          |
| :-------------- | :----- | :----------------------------------- |
| `MinPool`       | int    | Minimum connection pool size         |
| `MaxPool`       | int    | Maximum connection pool size         |
| `Multiplexing`  | bool   | Whether multiplexing is enabled      |
| `MaxRetries`    | int    | Maximum retry attempts configured    |
| `FailureRatio`  | double | Circuit breaker failure threshold    |
| `ElapsedMs`     | double | Connection acquisition time          |
| `PoolStats`     | string | Current pool state (Idle/Busy/Total) |
| `AttemptNumber` | int    | Current retry attempt                |
| `Delay`         | double | Time until next retry                |
| `BreakDuration` | double | Circuit breaker open duration        |

---

## 7. Security & Safety

### 7.1 Connection String Protection

> [!IMPORTANT]
> Connection strings contain database credentials and MUST be protected.

**Best Practices:**

1. **Never commit to source control:**
   ```gitignore
   # .gitignore
   .env
   appsettings.*.json
   !appsettings.json
   !appsettings.Development.json.example
   ```

2. **Use environment variables in production:**
   ```bash
   export LEXICHORD_DATABASE__CONNECTIONSTRING="Host=prod-db;..."
   ```

3. **Sanitize connection strings in logs:**
   ```csharp
   // CORRECT: Log without password
   logger.LogInformation("Connecting to {Host}:{Port}/{Database}",
       builder.Host, builder.Port, builder.Database);

   // WRONG: Never log full connection string
   logger.LogInformation("Connecting with {ConnectionString}", connectionString);
   ```

### 7.2 Pool Exhaustion Prevention

Configure appropriate pool limits based on expected concurrency:

| Scenario              | MaxPoolSize | MinPoolSize | Rationale                          |
| :-------------------- | :---------- | :---------- | :--------------------------------- |
| Desktop (single user) | 20          | 5           | Low concurrency, minimal resources |
| Web server (medium)   | 100         | 20          | Moderate concurrent requests       |
| High-load service     | 200         | 50          | High concurrency, warm pool        |

---

## 8. Definition of Done

- [ ] `Npgsql` and `Polly` NuGet packages installed
- [ ] `DatabaseOptions` record defined with all configuration properties
- [ ] `RetryOptions` and `CircuitBreakerOptions` nested records defined
- [ ] `IDbConnectionFactory` interface defined in Abstractions
- [ ] `NpgsqlConnectionFactory` implementation complete
- [ ] Connection pooling configured via NpgsqlDataSource
- [ ] Polly retry policy with exponential backoff configured
- [ ] Polly circuit breaker policy configured
- [ ] Transient error detection implemented for PostgreSQL error codes
- [ ] `IsHealthy` property reflects circuit breaker state
- [ ] `CanConnectAsync()` health check method implemented
- [ ] Services registered in DI container
- [ ] Unit tests for options validation passing
- [ ] Unit tests for factory initialization passing
- [ ] Unit tests for transient error detection passing
- [ ] Integration tests for connection lifecycle passing
- [ ] All log events implemented with structured properties

---

## 9. Verification Commands

```bash
# 1. Build to verify packages
dotnet build src/Lexichord.Infrastructure

# 2. Start test database
./scripts/db-start.sh

# 3. Run unit tests
dotnet test --filter "Category=Unit&FullyQualifiedName~Connection"

# 4. Run integration tests
LEXICHORD_TEST_DB="Host=localhost;Port=5432;Database=lexichord;Username=lexichord;Password=lexichord_dev" \
  dotnet test --filter "Category=Integration&FullyQualifiedName~Connection"

# 5. Verify configuration loading
dotnet run --project src/Lexichord.Host -- --debug-mode

# 6. Check logs for connection factory initialization
# Should see: "Database connection factory initialized. Pool: 10-100, ..."

# 7. Test circuit breaker (stop database, attempt connections)
./scripts/db-stop.sh
# Application should log circuit breaker opening after failures
```

---

## 10. Configuration Reference

### appsettings.json

```json
{
  "Database": {
    "ConnectionString": "Host=localhost;Port=5432;Database=lexichord;Username=lexichord;Password=lexichord_dev",
    "MaxPoolSize": 100,
    "MinPoolSize": 10,
    "ConnectionLifetimeSeconds": 300,
    "ConnectionTimeoutSeconds": 30,
    "CommandTimeoutSeconds": 30,
    "EnableMultiplexing": true,
    "Retry": {
      "MaxRetryAttempts": 4,
      "BaseDelayMs": 1000,
      "MaxDelayMs": 30000,
      "UseJitter": true
    },
    "CircuitBreaker": {
      "FailureRatio": 0.5,
      "SamplingDurationSeconds": 30,
      "MinimumThroughput": 5,
      "BreakDurationSeconds": 30
    }
  }
}
```

### Environment Variable Override

```bash
# Override connection string via environment variable
export LEXICHORD_DATABASE__CONNECTIONSTRING="Host=prod-db.example.com;Port=5432;Database=lexichord_prod;Username=app_user;Password=${DB_PASSWORD}"

# Override specific settings
export LEXICHORD_DATABASE__MAXPOOLSIZE=200
export LEXICHORD_DATABASE__ENABLEMULTIPLEXING=false
```
