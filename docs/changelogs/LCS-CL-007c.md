# LCS-CL-007c: Logging Pipeline Behavior

**Version:** v0.0.7c  
**Category:** Infrastructure  
**Feature Name:** Logging Pipeline Behavior  
**Date:** 2026-01-28

---

## Summary

Implements automatic request/response logging for all MediatR commands and queries with timing, sensitive data redaction, and configurable thresholds.

---

## New Features

### Attributes

- **SensitiveDataAttribute** — Marks properties for redaction in logs
    - Customizable redaction text (default: `[REDACTED]`)
    - Supports `[SensitiveData("[API_KEY]")]` for custom labels

- **NoLogAttribute** — Completely excludes properties from log output
    - Useful for large binary data or derived properties

### Pipeline Behavior

- **LoggingBehavior<TRequest, TResponse>** — Outermost pipeline behavior
    - Logs request start with type and correlation ID
    - Logs request completion with duration in milliseconds
    - Warns on slow requests exceeding configurable threshold
    - Logs exceptions with context before re-throwing
    - Auto-redacts common sensitive property names

### Configuration

- **LoggingBehaviorOptions** — Bound from `MediatR:Logging` in appsettings.json
    - `SlowRequestThresholdMs` — Warning threshold (default: 500ms)
    - `LogRequestProperties` — Enable/disable property logging
    - `LogResponseProperties` — Enable/disable response logging
    - `LogFullExceptions` — Full stack trace vs message only
    - `ExcludedRequestTypes` — Skip logging for specific types

---

## Files Added

| File                                                                       | Description           |
| :------------------------------------------------------------------------- | :-------------------- |
| `src/Lexichord.Abstractions/Attributes/SensitiveDataAttribute.cs`          | Redaction attribute   |
| `src/Lexichord.Abstractions/Attributes/NoLogAttribute.cs`                  | Exclusion attribute   |
| `src/Lexichord.Host/Infrastructure/Options/LoggingBehaviorOptions.cs`      | Configuration options |
| `src/Lexichord.Host/Infrastructure/Behaviors/LoggingBehavior.cs`           | Pipeline behavior     |
| `tests/Lexichord.Tests.Unit/TestUtilities/FakeLogger.cs`                   | Test utility          |
| `tests/Lexichord.Tests.Unit/Host/Behaviors/LoggingBehaviorTests.cs`        | Behavior tests        |
| `tests/Lexichord.Tests.Unit/Host/Behaviors/SensitiveDataRedactionTests.cs` | Redaction tests       |

## Files Modified

| File                                                            | Description                 |
| :-------------------------------------------------------------- | :-------------------------- |
| `src/Lexichord.Host/Infrastructure/MediatRServiceExtensions.cs` | Register behavior + options |
| `src/Lexichord.Host/appsettings.json`                           | Add MediatR:Logging section |

---

## Usage

### Marking Sensitive Properties

```csharp
public record CreateUserCommand : ICommand<UserId>
{
    public string Username { get; init; }

    [SensitiveData]
    public string Password { get; init; }

    [SensitiveData("[EMAIL]")]
    public string Email { get; init; }

    [NoLog]
    public byte[] ProfilePhoto { get; init; }
}
```

### Log Output Example

```text
[DBG] [LoggingBehavior] Handling CreateUserCommand [CorrelationId: abc-123]
[DBG] [LoggingBehavior] Request: {"username":"john","password":"[REDACTED]","email":"[EMAIL]"}
... handler executes ...
[INF] [LoggingBehavior] Handled CreateUserCommand in 45ms
```

### Slow Request Warning

```text
[WRN] [LoggingBehavior] Slow request: SlowQuery took 1250ms (threshold: 500ms)
[INF] [LoggingBehavior] Handled SlowQuery in 1250ms
```

---

## Configuration Example

```json
{
    "MediatR": {
        "Logging": {
            "SlowRequestThresholdMs": 500,
            "LogRequestProperties": true,
            "LogResponseProperties": false,
            "LogFullExceptions": true,
            "ExcludedRequestTypes": ["HealthCheckQuery"]
        }
    }
}
```

---

## Verification Commands

```bash
# 1. Build solution
dotnet build

# 2. Run LoggingBehavior unit tests
dotnet test tests/Lexichord.Tests.Unit --filter "FullyQualifiedName~LoggingBehavior"

# 3. Verify files exist
ls src/Lexichord.Abstractions/Attributes/
ls src/Lexichord.Host/Infrastructure/Behaviors/
ls src/Lexichord.Host/Infrastructure/Options/
```

---

## Test Summary

| Test Class                  | Tests  | Status |
| :-------------------------- | :----- | :----- |
| LoggingBehaviorTests        | 7      | ✅     |
| SensitiveDataRedactionTests | 5      | ✅     |
| **Total**                   | **12** | **✅** |

---

## Dependencies

- **From v0.0.7a:** `ICommand<T>`, `IQuery<T>` interfaces
- **From v0.0.7b:** Correlation ID support in domain events
- **NuGet:** MediatR 12.4.1

## Enables

- **v0.0.7d:** Validation Pipeline Behavior (pipeline ordering)
- **v0.0.8+:** All future commands/queries automatically logged
