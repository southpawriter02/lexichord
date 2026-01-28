# LCS-CL-005b: Database Connector

## Version Information

| Field        | Value              |
| :----------- | :----------------- |
| Version      | v0.0.5b            |
| Feature Name | Database Connector |
| Release Date | 2026-01-28         |
| Status       | ✅ Complete        |

---

## Summary

Implemented Npgsql-based database connectivity with connection pooling and Polly resilience patterns.
This provides robust PostgreSQL connections with automatic retry for transient failures and circuit
breaker protection against cascade failures during outages.

---

## What's New

### Database Configuration Options

- **`DatabaseOptions`** — Configuration record with connection settings
    - Connection string, pool sizing (min/max), timeouts
    - Multiplexing support for improved async throughput
    - Nested `RetryOptions` and `CircuitBreakerOptions`

### Connection Factory Interface

- **`IDbConnectionFactory`** — Abstraction for database connectivity
    - `CreateConnectionAsync()` — Returns pooled, open connection
    - `DataSource` — Access to underlying `NpgsqlDataSource`
    - `IsHealthy` — Circuit breaker state for health checks
    - `CanConnectAsync()` — Connectivity verification

### Resilient Connection Factory

- **`NpgsqlConnectionFactory`** — Production implementation
    - `NpgsqlDataSource` for connection pooling
    - Polly retry with exponential backoff and jitter
    - Polly circuit breaker with configurable thresholds
    - Transient error detection for PostgreSQL error classes (08, 40, 53, 57, 58)
    - Structured logging for observability

### DI Registration

- **`InfrastructureServices.AddDatabaseServices()`** — Extension method
    - Binds `DatabaseOptions` from configuration
    - Registers `IDbConnectionFactory` as singleton

---

## Files Created

| File                                                                                    | Purpose                      |
| :-------------------------------------------------------------------------------------- | :--------------------------- |
| `src/Lexichord.Infrastructure/Lexichord.Infrastructure.csproj`                          | Infrastructure project       |
| `src/Lexichord.Infrastructure/Data/NpgsqlConnectionFactory.cs`                          | Connection factory impl      |
| `src/Lexichord.Infrastructure/InfrastructureServices.cs`                                | DI registration              |
| `src/Lexichord.Abstractions/Contracts/DatabaseOptions.cs`                               | Configuration options        |
| `src/Lexichord.Abstractions/Contracts/IDbConnectionFactory.cs`                          | Connection factory interface |
| `tests/Lexichord.Tests.Unit/Infrastructure/DatabaseOptionsTests.cs`                     | Options unit tests           |
| `tests/Lexichord.Tests.Unit/Infrastructure/NpgsqlConnectionFactoryTests.cs`             | Factory unit tests           |
| `tests/Lexichord.Tests.Unit/Infrastructure/TransientErrorDetectionTests.cs`             | Error detection tests        |
| `tests/Lexichord.Tests.Integration/Infrastructure/ConnectionFactoryIntegrationTests.cs` | Integration tests            |

---

## Configuration

### appsettings.json

```json
{
    "Database": {
        "ConnectionString": "Host=localhost;Port=5432;Database=lexichord;Username=lexichord;Password=...",
        "MaxPoolSize": 100,
        "MinPoolSize": 10,
        "EnableMultiplexing": true,
        "Retry": {
            "MaxRetryAttempts": 4,
            "BaseDelayMs": 1000
        },
        "CircuitBreaker": {
            "FailureRatio": 0.5,
            "BreakDurationSeconds": 30
        }
    }
}
```

---

## Usage

### Basic Connection

```csharp
// Injected via DI
public class MyRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MyRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> GetCountAsync()
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM my_table";
        return (int)(long)(await command.ExecuteScalarAsync())!;
    }
}
```

### Health Check

```csharp
if (_connectionFactory.IsHealthy)
{
    var canConnect = await _connectionFactory.CanConnectAsync();
    // Use for readiness probes
}
```

---

## Verification

```bash
# Build solution
dotnet build

# Run infrastructure unit tests
dotnet test tests/Lexichord.Tests.Unit --filter "FullyQualifiedName~Infrastructure"

# Run integration tests (requires Docker)
./scripts/db-start.sh
dotnet test tests/Lexichord.Tests.Integration --filter "FullyQualifiedName~ConnectionFactory"
```

---

## Related Documents

- **Design Specification**: [LCS-DES-005b.md](../specs/v0.0.x/v0.0.5/LCS-DES-005b.md)
- **Parent Version**: [LCS-DES-005-INDEX.md](../specs/v0.0.x/v0.0.5/LCS-DES-005-INDEX.md)
- **Scope Breakdown**: [LCS-SBD-005.md](../specs/v0.0.x/v0.0.5/LCS-SBD-005.md)
- **Prerequisite**: [LCS-CL-005a.md](./LCS-CL-005a.md) (Docker Orchestration)
