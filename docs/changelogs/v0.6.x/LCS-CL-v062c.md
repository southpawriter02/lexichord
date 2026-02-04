# LCS-CL-062c: Detailed Changelog — Retry Policy Implementation

## Metadata

| Field        | Value                                        |
| :----------- | :------------------------------------------- |
| **Version**  | v0.6.2c                                      |
| **Released** | 2026-02-04                                   |
| **Category** | Implementation / Module                      |
| **Parent**   | [v0.6.2 Changelog](../CHANGELOG.md#v062)     |
| **Spec**     | [LCS-DES-062c](../../specs/v0.6.x/v0.6.2/LCS-DES-v0.6.2c.md) |

---

## Summary

This release implements centralized resilience infrastructure for the LLM module using Polly. It consolidates the previously duplicated retry and circuit breaker policies from the OpenAI and Anthropic provider extensions into a reusable resilience pipeline. The implementation includes four policy layers (Retry, Circuit Breaker, Timeout, Bulkhead), telemetry collection for observability, health check integration, and comprehensive configuration options.

---

## New Features

### 1. Resilience Options Record

Added `ResilienceOptions` record for configuring all resilience parameters:

```csharp
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
    public const string SectionName = "LLM:Resilience";
    public static ResilienceOptions Default { get; }
    public static ResilienceOptions Aggressive { get; }
    public static ResilienceOptions Minimal { get; }
}
```

**Configuration Section:** `LLM:Resilience`

**Static Presets:**
| Preset | Use Case | Key Differences |
| ------ | -------- | --------------- |
| Default | General-purpose | 3 retries, 30s timeout, threshold 5 |
| Aggressive | Batch processing | 5 retries, 60s timeout, threshold 10 |
| Minimal | Real-time interactions | 1 retry, 10s timeout, threshold 3 |

**Validation:**
- `Validate()` throws `ArgumentOutOfRangeException` on first invalid value
- `GetValidationErrors()` returns all validation errors as list
- `IsValid` property for quick validation check

**TimeSpan Properties:**
- `RetryBaseDelay`, `RetryMaxDelay`, `CircuitBreakerDuration`, `Timeout`

**File:** `src/Lexichord.Modules.LLM/Resilience/ResilienceOptions.cs`

### 2. Circuit State Enum

Added `CircuitState` enum for circuit breaker state tracking:

```csharp
public enum CircuitState
{
    Closed = 0,    // Normal operation
    Open = 1,      // Rejecting requests
    HalfOpen = 2,  // Testing recovery
    Isolated = 3   // Manually isolated
}
```

**State Transitions:**
- `Closed → Open`: After threshold failures
- `Open → HalfOpen`: After break duration elapses
- `HalfOpen → Closed`: After successful test request
- `HalfOpen → Open`: After failed test request
- `Any → Isolated`: Manual isolation via policy

**File:** `src/Lexichord.Modules.LLM/Resilience/CircuitState.cs`

### 3. Resilience Event Record

Added `ResilienceEvent` record for telemetry event capture:

```csharp
public record ResilienceEvent(
    string PolicyName,
    string EventType,
    TimeSpan? Duration,
    Exception? Exception,
    int? AttemptNumber)
{
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public bool IsRetry { get; }
    public bool IsCircuitBreak { get; }
    public bool IsTimeout { get; }
    public bool IsBulkheadRejection { get; }
}
```

**Factory Methods:**
| Method | Description |
| ------ | ----------- |
| `CreateRetry(attempt, delay, exception?)` | Retry event with backoff info |
| `CreateCircuitBreak(duration, exception?)` | Circuit break event |
| `CreateCircuitReset()` | Circuit reset event |
| `CreateCircuitHalfOpen()` | Circuit half-open transition |
| `CreateTimeout(duration)` | Timeout event |
| `CreateBulkheadRejection()` | Bulkhead rejection event |

**Event Type Constants:**
- `PolicyNames`: Retry, CircuitBreaker, Timeout, Bulkhead
- `RetryEventTypes`: Retry, Backoff, RetryAfter
- `CircuitBreakerEventTypes`: Break, Reset, HalfOpen, Isolated
- `TimeoutEventTypes`: Timeout
- `BulkheadEventTypes`: Rejected

**File:** `src/Lexichord.Modules.LLM/Resilience/ResilienceEvent.cs`

### 4. Resilience Pipeline Interface

Added `IResiliencePipeline` interface for resilience execution:

```csharp
public interface IResiliencePipeline
{
    Task<HttpResponseMessage> ExecuteAsync(
        Func<CancellationToken, Task<HttpResponseMessage>> operation,
        CancellationToken cancellationToken = default);

    CircuitState CircuitState { get; }

    event EventHandler<ResilienceEvent>? OnPolicyEvent;
}
```

**Features:**
- Async execution through policy pipeline
- Circuit state visibility for monitoring
- Event-driven telemetry integration

**File:** `src/Lexichord.Modules.LLM/Resilience/IResiliencePipeline.cs`

### 5. Resilience Policy Builder

Added `ResiliencePolicyBuilder` for constructing individual Polly policies:

| Method | Return | Description |
| ------ | ------ | ----------- |
| `BuildRetryPolicy(options)` | `IAsyncPolicy<HttpResponseMessage>` | Retry with exponential backoff |
| `BuildCircuitBreakerPolicy(options)` | `AsyncCircuitBreakerPolicy<HttpResponseMessage>` | Circuit breaker with state callbacks |
| `BuildSimpleCircuitBreakerPolicy(options)` | `IAsyncPolicy<HttpResponseMessage>` | Simple circuit breaker for HTTP client |
| `BuildTimeoutPolicy(options)` | `IAsyncPolicy<HttpResponseMessage>` | Timeout with pessimistic strategy |
| `BuildBulkheadPolicy(options)` | `IAsyncPolicy<HttpResponseMessage>` | Bulkhead with concurrency/queue limits |

**Retry Policy Features:**
- Handles transient HTTP errors (5xx, 408)
- Handles 429 Too Many Requests
- Handles 529 Anthropic Overloaded
- Respects Retry-After header
- Exponential backoff with jitter (capped at max delay)
- Logging callbacks for each retry

**File:** `src/Lexichord.Modules.LLM/Resilience/ResiliencePolicyBuilder.cs`

### 6. LLM Resilience Pipeline

Added `LLMResiliencePipeline` implementing `IResiliencePipeline`:

**Policy Execution Order (outer to inner):**
1. Bulkhead → Limits concurrent requests
2. Timeout → Per-request time limit
3. Circuit Breaker → Fail-fast during outages
4. Retry → Transient error recovery

**Features:**
- Maps Polly `CircuitState` to custom `CircuitState` enum
- Raises telemetry events via `OnPolicyEvent`
- Comprehensive logging for all policy actions
- Exception handling with structured logging

**File:** `src/Lexichord.Modules.LLM/Resilience/LLMResiliencePipeline.cs`

### 7. Resilience Telemetry

Added `ResilienceTelemetry` class for metrics collection:

| Property | Type | Description |
| -------- | ---- | ----------- |
| `TotalRequests` | `long` | Total requests processed |
| `SuccessfulRequests` | `long` | Successful request count |
| `FailedRequests` | `long` | Failed request count |
| `TotalRetries` | `long` | Total retry attempts |
| `CircuitBreakerOpens` | `long` | Times circuit opened |
| `CircuitBreakerResets` | `long` | Times circuit reset |
| `Timeouts` | `long` | Timeout occurrences |
| `BulkheadRejections` | `long` | Bulkhead rejections |
| `SuccessRate` | `double` | Success percentage (0-100) |

**Methods:**
| Method | Description |
| ------ | ----------- |
| `RecordSuccess(latencyMs)` | Record successful request |
| `RecordFailure(latencyMs)` | Record failed request |
| `RecordRetry(attemptNumber)` | Record retry attempt |
| `RecordEvent(evt)` | Record resilience event |
| `GetP50Latency()` | Get P50 latency |
| `GetP90Latency()` | Get P90 latency |
| `GetP99Latency()` | Get P99 latency |
| `GetSnapshot()` | Get point-in-time telemetry |
| `Reset()` | Clear all counters |

**Thread Safety:** All operations use `Interlocked` for thread-safe updates.

**File:** `src/Lexichord.Modules.LLM/Resilience/ResilienceTelemetry.cs`

### 8. Circuit Breaker Health Check

Added `LLMCircuitBreakerHealthCheck` implementing `IHealthCheck`:

| Circuit State | Health Status | Description |
| ------------- | ------------- | ----------- |
| Closed | Healthy | Normal operation |
| HalfOpen | Degraded | Testing recovery |
| Open | Unhealthy | Experiencing failures |
| Isolated | Unhealthy | Manually isolated |

**Registration:**
```csharp
services.AddHealthChecks()
    .AddCheck<LLMCircuitBreakerHealthCheck>("llm-circuit-breaker");
```

**File:** `src/Lexichord.Modules.LLM/Resilience/LLMCircuitBreakerHealthCheck.cs`

### 9. DI Registration Extensions

Added `ResilienceServiceCollectionExtensions` for service registration:

```csharp
// Core resilience services
services.AddLLMResilience(configuration);

// HTTP client with policies
services.AddHttpClient("MyProvider")
    .AddLLMResiliencePolicies(configuration);
```

**`AddLLMResilience()` Registers:**
- `ResilienceOptions` bound from `LLM:Resilience` configuration
- `IResiliencePipeline` as singleton (shared circuit breaker state)
- `ResilienceTelemetry` as singleton (aggregated metrics)

**`AddLLMResiliencePolicies()` Adds:**
- Retry policy with exponential backoff
- Circuit breaker policy
- (Policies integrated with `IHttpClientFactory`)

**File:** `src/Lexichord.Modules.LLM/Extensions/ResilienceServiceCollectionExtensions.cs`

### 10. Structured Logging Events (LLM Module)

Added resilience logging events (1800-1899 range):

| Event ID | Level | Description |
| -------- | ----- | ----------- |
| 1800 | Debug | Resilience pipeline created |
| 1801 | Debug | Executing through resilience pipeline |
| 1802 | Warning | Retry attempt triggered |
| 1803 | Warning | Circuit breaker opened |
| 1804 | Information | Circuit breaker reset |
| 1805 | Information | Circuit breaker half-open |
| 1806 | Warning | Request timed out |
| 1807 | Warning | Bulkhead rejected request |
| 1808 | Debug | Policy wrap constructed |
| 1809 | Debug | Using Retry-After header |
| 1810 | Debug | Calculating exponential backoff |
| 1811 | Trace | Resilience event raised |
| 1812 | Information | Resilience configuration loaded |
| 1813 | Warning | Resilience options validation warning |
| 1814 | Error | Resilience pipeline execution failed |
| 1815 | Debug | Health check queried |
| 1816 | Debug | Building retry policy |
| 1817 | Debug | Building circuit breaker policy |
| 1818 | Debug | Building timeout policy |
| 1819 | Debug | Building bulkhead policy |

**File:** `src/Lexichord.Modules.LLM/Logging/LLMLogEvents.cs`

---

## Refactored Files

### OpenAIServiceCollectionExtensions

Refactored to use centralized resilience policies:

**Before:**
```csharp
services.AddHttpClient(OpenAIOptions.HttpClientName)
    .AddPolicyHandler(GetRetryPolicy(maxRetries))
    .AddPolicyHandler(GetCircuitBreakerPolicy());

private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int maxRetries) { ... }
private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy() { ... }
```

**After:**
```csharp
var resilienceOptions = ResilienceServiceCollectionExtensions.GetResilienceOptions(configuration);

services.AddHttpClient(OpenAIOptions.HttpClientName)
    .AddLLMResiliencePolicies(resilienceOptions);
```

**File:** `src/Lexichord.Modules.LLM/Extensions/OpenAIServiceCollectionExtensions.cs`

### AnthropicServiceCollectionExtensions

Refactored to use centralized resilience policies:

**Before:**
```csharp
services.AddHttpClient(AnthropicOptions.HttpClientName)
    .AddPolicyHandler(GetRetryPolicy(maxRetries))
    .AddPolicyHandler(GetCircuitBreakerPolicy());

private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(int maxRetries) { ... }
private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy() { ... }
```

**After:**
```csharp
var resilienceOptions = ResilienceServiceCollectionExtensions.GetResilienceOptions(configuration);

services.AddHttpClient(AnthropicOptions.HttpClientName)
    .AddLLMResiliencePolicies(resilienceOptions);
```

**File:** `src/Lexichord.Modules.LLM/Extensions/AnthropicServiceCollectionExtensions.cs`

### LLMModule

Added resilience services registration:

```csharp
// LOGIC: Register centralized resilience services (v0.6.2c).
services.AddLLMResilience(configuration);
```

Updated module description to include resilience policies.

**File:** `src/Lexichord.Modules.LLM/LLMModule.cs`

---

## New Files

| File Path | Description |
| --------- | ----------- |
| `src/Lexichord.Modules.LLM/Resilience/ResilienceOptions.cs` | Configuration record with validation |
| `src/Lexichord.Modules.LLM/Resilience/CircuitState.cs` | Circuit breaker state enum |
| `src/Lexichord.Modules.LLM/Resilience/ResilienceEvent.cs` | Telemetry event record |
| `src/Lexichord.Modules.LLM/Resilience/IResiliencePipeline.cs` | Pipeline interface |
| `src/Lexichord.Modules.LLM/Resilience/ResiliencePolicyBuilder.cs` | Policy factory class |
| `src/Lexichord.Modules.LLM/Resilience/LLMResiliencePipeline.cs` | Pipeline implementation |
| `src/Lexichord.Modules.LLM/Resilience/ResilienceTelemetry.cs` | Metrics collection |
| `src/Lexichord.Modules.LLM/Resilience/LLMCircuitBreakerHealthCheck.cs` | Health check |
| `src/Lexichord.Modules.LLM/Extensions/ResilienceServiceCollectionExtensions.cs` | DI registration |

---

## Unit Tests

Added comprehensive unit tests for resilience components:

| Test File | Test Count | Coverage |
| --------- | ---------- | -------- |
| `ResilienceOptionsTests.cs` | ~40 | Defaults, presets, validation, TimeSpan properties |
| `ResilienceTelemetryTests.cs` | ~45 | Counters, latency, events, thread safety |
| `ResilienceEventTests.cs` | ~35 | Factory methods, boolean properties, constants |
| `CircuitStateTests.cs` | ~20 | Enum values, parsing, string representation |
| `LLMCircuitBreakerHealthCheckTests.cs` | ~20 | All circuit states, health status mapping |
| **Total** | **~160** | |

---

## Configuration

### appsettings.json Example

```json
{
  "LLM": {
    "Resilience": {
      "RetryCount": 3,
      "RetryBaseDelaySeconds": 1.0,
      "RetryMaxDelaySeconds": 30.0,
      "CircuitBreakerThreshold": 5,
      "CircuitBreakerDurationSeconds": 30,
      "TimeoutSeconds": 30,
      "BulkheadMaxConcurrency": 10,
      "BulkheadMaxQueue": 100
    }
  }
}
```

---

## Dependencies

### Internal Dependencies

| Interface | Version | Usage |
| --------- | ------- | ----- |
| `ILogger<T>` | v0.0.3b | Structured logging |
| `IConfiguration` | v0.0.3d | Configuration binding |
| `IHealthCheck` | .NET | Health check integration |

### External Dependencies

| Package | Version | Usage |
| ------- | ------- | ----- |
| `Microsoft.Extensions.Http.Polly` | 9.0.0 | Polly HTTP integration |
| `Polly` | (transitive) | Resilience policies |

---

## Breaking Changes

None. The refactoring maintains backward compatibility with existing provider configurations.

---

## Migration Guide

No migration required. The centralized resilience policies use the same default values as the previous inline implementations. Optional configuration can be added to `LLM:Resilience` section for customization.
