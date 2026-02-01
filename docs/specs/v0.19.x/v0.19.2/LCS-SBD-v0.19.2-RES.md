# Lexichord Scope Breakdown Document - v0.19.2-RES
## Graceful Degradation Implementation

---

## 1. DOCUMENT CONTROL

| Field | Value |
|-------|-------|
| **Document ID** | LCS-SBD-v0.19.2-RES |
| **Title** | Graceful Degradation Implementation Scope |
| **Version** | 1.0 |
| **Release Target** | v0.19.2-RES |
| **Status** | In Definition |
| **Last Updated** | 2026-02-01 |
| **Total Effort** | 56 hours |
| **Primary Focus** | Resilience & Graceful Degradation |
| **Author** | Lexichord Development Team |
| **Classification** | Technical Specification |

**Key Thematic Focus**: Ensuring Lexichord continues operating with gracefully degraded functionality when internal components or external dependencies fail, preventing catastrophic outages through intelligent fallback chains, circuit breaker patterns, and dependency health tracking.

---

## 2. EXECUTIVE SUMMARY

### 2.1 Vision Statement

Lexichord v0.19.2-RES implements enterprise-grade resilience mechanisms to ensure the platform remains operational even when critical components fail. Rather than cascading failures that take the entire system offline, v0.19.2 introduces intelligent degradation that:

- Activates fallback strategies when primary services become unavailable
- Implements circuit breakers to prevent resource exhaustion
- Executes sophisticated retry policies with exponential backoff
- Operates in partial functionality modes with explicit user feedback
- Tracks dependency health in real-time
- Performs graceful shutdowns to prevent data loss

### 2.2 Strategic Objectives

1. **Eliminate Cascading Failures**: Prevent failures in one component from affecting the entire system
2. **Improve Availability**: Maintain service availability even during partial outages
3. **Reduce Mean Time to Recovery (MTTR)**: Enable faster recovery through automated retry mechanisms
4. **Transparent Degradation**: Users understand when operating in degraded mode
5. **Data Safety**: Graceful shutdowns ensure no data loss during component failures
6. **Observability**: Complete visibility into system health and degradation events

### 2.3 Business Impact

- **Uptime Improvement**: Target 99.9% availability (4.38 hours downtime/month) vs. current ~95%
- **User Trust**: Transparent degradation prevents unexpected failures
- **Cost Reduction**: Reduces manual intervention and incident response costs
- **Competitive Advantage**: Enterprise-grade resilience attracts larger customers
- **Developer Experience**: Clear patterns for adding resilience to new features

### 2.4 Technical Principles

```
Resilience Hierarchy:
  Level 1: Primary Service (Full Functionality)
  Level 2: Fallback Service (Reduced Functionality)
  Level 3: Cache/Local Fallback (Limited Functionality)
  Level 4: Offline Mode (Minimal Functionality)
  Level 5: Graceful Shutdown (Data Preservation)
```

---

## 3. DETAILED SUB-PARTS BREAKDOWN

### 3.1 v0.19.2a: Fallback Strategy Engine
**Time Allocation**: 12 hours

#### 3.1.1 Overview
Implements a pluggable fallback strategy system that automatically switches between primary and backup implementations when failures are detected.

#### 3.1.2 Acceptance Criteria

- [ ] **AC-3.1.1**: Create `IFallbackStrategyEngine` interface with strategy registration, execution, and monitoring
- [ ] **AC-3.1.2**: Implement at least 3 concrete fallback strategies (Chain of Responsibility pattern)
- [ ] **AC-3.1.3**: Fallback activation occurs within 50ms of primary failure detection
- [ ] **AC-3.1.4**: Support nested fallback chains (Primary → Fallback1 → Fallback2 → Fallback3)
- [ ] **AC-3.1.5**: Track fallback activation metrics per strategy type
- [ ] **AC-3.1.6**: Support context propagation across fallback chain
- [ ] **AC-3.1.7**: Implement fallback strategy timeout handling
- [ ] **AC-3.1.8**: Create comprehensive unit tests with 90%+ code coverage
- [ ] **AC-3.1.9**: Document all built-in fallback strategies
- [ ] **AC-3.1.10**: Support async/await throughout fallback execution

#### 3.1.3 Key Features
- Dynamic strategy registration at runtime
- Chain of Responsibility pattern for sequential fallbacks
- Intelligent fallback selection based on failure type
- Metrics collection for observability
- Integration with MediatR for event publishing

#### 3.1.4 Deliverables
- `IFallbackStrategyEngine` and implementation
- 3+ concrete strategy implementations
- Fallback Strategy Matrix document
- Unit tests with mocking framework
- Integration tests with multiple fallback chains
- Performance benchmarks

---

### 3.2 v0.19.2b: Circuit Breaker Pattern
**Time Allocation**: 10 hours

#### 3.2.1 Overview
Implements the Circuit Breaker pattern to prevent cascading failures by stopping requests to failing services and periodically attempting recovery.

#### 3.2.2 Acceptance Criteria

- [ ] **AC-3.2.1**: Create `ICircuitBreaker` interface with Closed/Open/Half-Open states
- [ ] **AC-3.2.2**: Create `ICircuitBreakerFactory` for centralized creation and management
- [ ] **AC-3.2.3**: Circuit breaker state transitions within 0.1ms latency
- [ ] **AC-3.2.4**: Configurable failure thresholds (count and time window)
- [ ] **AC-3.2.5**: Configurable timeout for recovery attempts (Half-Open state)
- [ ] **AC-3.2.6**: Support circuit breaker cascading (a failing breaker opens others)
- [ ] **AC-3.2.7**: Publish `CircuitStateChangedEvent` via MediatR when state changes
- [ ] **AC-3.2.8**: Persist circuit breaker state to PostgreSQL for recovery after restart
- [ ] **AC-3.2.9**: Provide dashboard metrics for all active circuit breakers
- [ ] **AC-3.2.10**: Support different failure strategies per circuit breaker

#### 3.2.3 State Machine
```
States:
  Closed → (Failure threshold exceeded) → Open
  Open → (Timeout reached) → Half-Open
  Half-Open → (Success) → Closed
  Half-Open → (Failure) → Open
```

#### 3.2.4 Deliverables
- `ICircuitBreaker` and implementation
- `ICircuitBreakerFactory` implementation
- State machine documentation
- ASCII state diagrams
- PostgreSQL persistence layer
- Unit and integration tests
- Performance benchmarks

---

### 3.3 v0.19.2c: Retry Policies
**Time Allocation**: 8 hours

#### 3.3.1 Overview
Implements configurable retry policies with exponential backoff, jitter, and dead-letter handling for transient failures.

#### 3.3.2 Acceptance Criteria

- [ ] **AC-3.3.1**: Create `IRetryPolicy` interface with configurable backoff strategies
- [ ] **AC-3.3.2**: Create `IRetryPolicyFactory` for simplified policy creation
- [ ] **AC-3.3.3**: Implement exponential backoff with jitter (prevents thundering herd)
- [ ] **AC-3.3.4**: Support max retry attempts configuration
- [ ] **AC-3.3.5**: Support predicate-based retry decisions (retry on specific exceptions)
- [ ] **AC-3.3.6**: Implement dead-letter queue for exhausted retries
- [ ] **AC-3.3.7**: Track retry metrics (attempt count, success rate, backoff time)
- [ ] **AC-3.3.8**: Support retry budget constraints (fail-fast when budget exhausted)
- [ ] **AC-3.3.9**: Publish `RetryAttemptedEvent` via MediatR
- [ ] **AC-3.3.10**: Support async execution with cancellation tokens

#### 3.3.3 Backoff Strategies
- **Linear**: Wait time = baseDelay * attemptNumber
- **Exponential**: Wait time = baseDelay * (multiplier ^ attemptNumber)
- **Exponential with Jitter**: Random variation to prevent synchronized retries
- **Fibonacci**: Wait time increases in Fibonacci sequence

#### 3.3.4 Deliverables
- `IRetryPolicy` and implementation
- `IRetryPolicyFactory` implementation
- Multiple backoff strategy implementations
- Dead-letter queue infrastructure
- Retry metrics collection
- Unit and integration tests

---

### 3.4 v0.19.2d: Partial Functionality Mode
**Time Allocation**: 10 hours

#### 3.4.1 Overview
Enables the system to operate in degraded modes with explicit indication of reduced functionality to users. Different degradation levels provide different feature sets.

#### 3.4.2 Acceptance Criteria

- [ ] **AC-3.4.1**: Create `IPartialFunctionalityManager` interface
- [ ] **AC-3.4.2**: Define 4 degradation levels (Full, Degraded-Moderate, Degraded-Severe, Offline)
- [ ] **AC-3.4.3**: Per-feature degradation level mapping
- [ ] **AC-3.4.4**: Automatic feature availability determination based on health
- [ ] **AC-3.4.5**: UI indicators show degradation level to users
- [ ] **AC-3.4.6**: API responses include degradation metadata
- [ ] **AC-3.4.7**: Publish `DegradationLevelChangedEvent` via MediatR
- [ ] **AC-3.4.8**: Support read-only mode for database failures
- [ ] **AC-3.4.9**: Cache-backed operation when services fail
- [ ] **AC-3.4.10**: Document all feature degradation scenarios

#### 3.4.3 Degradation Levels

| Level | Name | Features Available | Use Case |
|-------|------|-------------------|----------|
| 0 | Full | All features | Normal operation |
| 1 | Degraded-Moderate | Core + Search | API service down |
| 2 | Degraded-Severe | Core only (read) | Database slow |
| 3 | Offline | Core offline | All external deps down |

#### 3.4.4 Deliverables
- `IPartialFunctionalityManager` implementation
- Feature degradation mapping system
- UI component for degradation indicators
- API middleware for degradation metadata
- Documentation of all degradation scenarios
- Unit and integration tests

---

### 3.5 v0.19.2e: Dependency Health Tracking
**Time Allocation**: 8 hours

#### 3.5.1 Overview
Implements real-time monitoring of external and internal dependencies with health status aggregation and alerting.

#### 3.5.2 Acceptance Criteria

- [ ] **AC-3.5.1**: Create `IDependencyHealthTracker` interface
- [ ] **AC-3.5.2**: Health check execution at configurable intervals (default: 30 seconds)
- [ ] **AC-3.5.3**: Support both synchronous and asynchronous health checks
- [ ] **AC-3.5.4**: Aggregate health status across all dependencies
- [ ] **AC-3.5.5**: Track health check history (last 100 checks per dependency)
- [ ] **AC-3.5.6**: Configurable thresholds for healthy/degraded/critical states
- [ ] **AC-3.5.7**: Publish health change events via MediatR
- [ ] **AC-3.5.8**: Expose health metrics for Prometheus/monitoring systems
- [ ] **AC-3.5.9**: Support dependency chain analysis (A → B → C)
- [ ] **AC-3.5.10**: Implement exponential backoff for failing health checks

#### 3.5.3 Health Status Enum
```csharp
public enum DependencyHealthStatus
{
    Healthy = 0,
    Degraded = 1,
    Critical = 2,
    Unknown = 3
}
```

#### 3.5.4 Deliverables
- `IDependencyHealthTracker` implementation
- Health check implementations for 5+ common services
- Aggregation algorithm documentation
- Prometheus metrics exporter
- Dashboard component showing dependency status
- Historical data storage and retrieval

---

### 3.6 v0.19.2f: Graceful Shutdown
**Time Allocation**: 8 hours

#### 3.6.1 Overview
Implements graceful shutdown procedures that allow in-flight requests to complete while preventing new requests and ensuring data consistency.

#### 3.6.2 Acceptance Criteria

- [ ] **AC-3.6.1**: Create `IGracefulShutdownManager` interface
- [ ] **AC-3.6.2**: Support shutdown phases: Grace Period → In-Flight Completion → Data Flush → Stop
- [ ] **AC-3.6.3**: Grace period allows new requests (default: 30 seconds)
- [ ] **AC-3.6.4**: In-flight request completion timeout (default: 5 minutes)
- [ ] **AC-3.6.5**: Track shutdown progress and publish `ShutdownInitiatedEvent`
- [ ] **AC-3.6.6**: Cancel new requests after grace period
- [ ] **AC-3.6.7**: Force shutdown after timeout with warning
- [ ] **AC-3.6.8**: Flush all buffered data to persistent storage
- [ ] **AC-3.6.9**: Gracefully close database connections
- [ ] **AC-3.6.10**: Publish shutdown completion event with metrics

#### 3.6.3 Shutdown Phases
```
Phase 1: Grace Period (30s)
  - Accept new requests
  - Publish ShutdownInitiatedEvent
  - UI shows "maintenance mode" warning

Phase 2: In-Flight Completion (5m)
  - Reject new requests
  - Allow existing requests to complete
  - Monitor request completion

Phase 3: Data Flush (30s)
  - Flush buffers
  - Persist caches
  - Close connections gracefully

Phase 4: Stop
  - Exit application
```

#### 3.6.4 Deliverables
- `IGracefulShutdownManager` implementation
- Shutdown middleware integration
- Progress tracking and reporting
- PostgreSQL audit logging
- Shutdown scenario documentation
- Integration tests with simulated scenarios

---

## 4. C# INTERFACE SPECIFICATIONS

### 4.1 IFallbackStrategyEngine

```csharp
/// <summary>
/// Manages fallback strategies for handling service failures.
/// Implements Chain of Responsibility pattern with automatic failover.
/// </summary>
public interface IFallbackStrategyEngine
{
    /// <summary>
    /// Registers a fallback strategy for a service.
    /// </summary>
    /// <param name="serviceName">Unique service identifier</param>
    /// <param name="strategy">Fallback strategy implementation</param>
    /// <param name="priority">Lower number = higher priority (0 = primary)</param>
    Task RegisterStrategyAsync(string serviceName, IFallbackStrategy strategy, int priority);

    /// <summary>
    /// Executes a service call with automatic fallback on failure.
    /// </summary>
    /// <param name="serviceName">Service to execute</param>
    /// <param name="operation">The async operation to perform</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Operation result or fallback result</returns>
    Task<T> ExecuteWithFallbackAsync<T>(
        string serviceName,
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a service call with timeout and fallback.
    /// </summary>
    Task<T> ExecuteWithTimeoutAndFallbackAsync<T>(
        string serviceName,
        Func<Task<T>> operation,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current fallback strategy execution metrics.
    /// </summary>
    IReadOnlyDictionary<string, FallbackStrategyMetrics> GetMetrics();

    /// <summary>
    /// Resets metrics for a specific service.
    /// </summary>
    Task ResetMetricsAsync(string serviceName);

    /// <summary>
    /// Unregisters a strategy.
    /// </summary>
    Task UnregisterStrategyAsync(string serviceName);

    /// <summary>
    /// Gets the current active strategy for a service.
    /// </summary>
    IFallbackStrategy? GetActiveStrategy(string serviceName);
}

/// <summary>
/// Represents a single fallback strategy.
/// </summary>
public interface IFallbackStrategy
{
    /// <summary>
    /// Unique name for this strategy.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Executes the fallback operation.
    /// </summary>
    Task<T> ExecuteAsync<T>(
        Func<Task<T>> primaryOperation,
        FallbackContext context,
        CancellationToken cancellationToken);

    /// <summary>
    /// Determines if this strategy can handle the given exception.
    /// </summary>
    bool CanHandle(Exception exception);

    /// <summary>
    /// Priority of this strategy (0 = highest, affects execution order).
    /// </summary>
    int Priority { get; }
}

/// <summary>
/// Context passed through fallback chain.
/// </summary>
public class FallbackContext
{
    public string ServiceName { get; set; }
    public Exception? OriginalException { get; set; }
    public int AttemptNumber { get; set; }
    public TimeSpan ElapsedTime { get; set; }
    public Dictionary<string, object> ContextData { get; set; }
}

/// <summary>
/// Metrics for fallback strategy execution.
/// </summary>
public class FallbackStrategyMetrics
{
    public string ServiceName { get; set; }
    public int TotalAttempts { get; set; }
    public int SuccessfulFallbacks { get; set; }
    public int FailedFallbacks { get; set; }
    public double AverageFallbackTimeMs { get; set; }
    public DateTime LastActivated { get; set; }
}
```

### 4.2 ICircuitBreaker

```csharp
/// <summary>
/// Implements the Circuit Breaker pattern for preventing cascading failures.
/// States: Closed (normal) → Open (failing) → Half-Open (recovery) → Closed
/// </summary>
public interface ICircuitBreaker
{
    /// <summary>
    /// Unique identifier for this circuit breaker.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Current state of the circuit breaker.
    /// </summary>
    CircuitBreakerState State { get; }

    /// <summary>
    /// Executes an operation, tripping the circuit on failure.
    /// </summary>
    Task<T> ExecuteAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a fire-and-forget operation.
    /// </summary>
    Task ExecuteAsync(
        Func<Task> operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed state information.
    /// </summary>
    CircuitBreakerStatus GetStatus();

    /// <summary>
    /// Manually opens the circuit breaker.
    /// </summary>
    Task OpenAsync(string reason);

    /// <summary>
    /// Manually closes the circuit breaker.
    /// </summary>
    Task CloseAsync();

    /// <summary>
    /// Manually transitions to Half-Open for testing.
    /// </summary>
    Task ResetAsync();

    /// <summary>
    /// Records a successful operation.
    /// </summary>
    void RecordSuccess();

    /// <summary>
    /// Records a failure.
    /// </summary>
    void RecordFailure(Exception exception);
}

/// <summary>
/// Circuit breaker states.
/// </summary>
public enum CircuitBreakerState
{
    Closed = 0,      // Normal operation, requests pass through
    Open = 1,        // Failing, requests rejected immediately
    HalfOpen = 2     // Recovery attempt, limited requests allowed
}

/// <summary>
/// Detailed circuit breaker status.
/// </summary>
public class CircuitBreakerStatus
{
    public string Name { get; set; }
    public CircuitBreakerState State { get; set; }
    public int FailureCount { get; set; }
    public int SuccessCount { get; set; }
    public DateTime LastFailureTime { get; set; }
    public DateTime LastStateChangeTime { get; set; }
    public double FailureRatePercentage { get; set; }
    public int HalfOpenAttempts { get; set; }
    public TimeSpan TimeUntilNextAttempt { get; set; }
}

/// <summary>
/// Configuration for circuit breaker behavior.
/// </summary>
public class CircuitBreakerConfig
{
    public string Name { get; set; }
    public int FailureThreshold { get; set; } = 5;
    public TimeSpan EvaluationPeriod { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan OpenTimeout { get; set; } = TimeSpan.FromSeconds(60);
    public int HalfOpenMaxAttempts { get; set; } = 3;
    public Func<Exception, bool> ShouldTrip { get; set; }
}
```

### 4.3 ICircuitBreakerFactory

```csharp
/// <summary>
/// Factory for creating and managing circuit breakers.
/// </summary>
public interface ICircuitBreakerFactory
{
    /// <summary>
    /// Creates or retrieves an existing circuit breaker.
    /// </summary>
    ICircuitBreaker GetOrCreateCircuitBreaker(CircuitBreakerConfig config);

    /// <summary>
    /// Gets an existing circuit breaker by name.
    /// </summary>
    ICircuitBreaker? GetCircuitBreaker(string name);

    /// <summary>
    /// Gets all registered circuit breakers.
    /// </summary>
    IReadOnlyDictionary<string, ICircuitBreaker> GetAllCircuitBreakers();

    /// <summary>
    /// Removes a circuit breaker.
    /// </summary>
    Task RemoveAsync(string name);

    /// <summary>
    /// Gets aggregated status of all circuit breakers.
    /// </summary>
    CircuitBreakerFactoryStatus GetStatus();

    /// <summary>
    /// Persists all circuit breaker states to storage.
    /// </summary>
    Task PersistStateAsync();

    /// <summary>
    /// Restores circuit breaker states from storage.
    /// </summary>
    Task RestoreStateAsync();
}

/// <summary>
/// Aggregated status across all circuit breakers.
/// </summary>
public class CircuitBreakerFactoryStatus
{
    public int TotalCircuitBreakers { get; set; }
    public int ClosedCount { get; set; }
    public int OpenCount { get; set; }
    public int HalfOpenCount { get; set; }
    public List<CircuitBreakerStatus> AllStatuses { get; set; }
}
```

### 4.4 IRetryPolicy

```csharp
/// <summary>
/// Defines retry behavior with configurable backoff strategies.
/// </summary>
public interface IRetryPolicy
{
    /// <summary>
    /// Executes an operation with automatic retries.
    /// </summary>
    Task<T> ExecuteAsync<T>(
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a fire-and-forget operation with retries.
    /// </summary>
    Task ExecuteAsync(
        Func<Task> operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets retry attempt metrics.
    /// </summary>
    RetryPolicyMetrics GetMetrics();

    /// <summary>
    /// Resets metrics.
    /// </summary>
    Task ResetMetricsAsync();
}

/// <summary>
/// Retry attempt information.
/// </summary>
public class RetryAttempt
{
    public int AttemptNumber { get; set; }
    public Exception? LastException { get; set; }
    public TimeSpan WaitTime { get; set; }
    public DateTime AttemptTime { get; set; }
}

/// <summary>
/// Backoff strategies for retry delays.
/// </summary>
public enum BackoffStrategy
{
    Linear = 0,
    Exponential = 1,
    ExponentialWithJitter = 2,
    Fibonacci = 3
}

/// <summary>
/// Configuration for retry policy.
/// </summary>
public class RetryPolicyConfig
{
    public string Name { get; set; }
    public int MaxAttempts { get; set; } = 3;
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMilliseconds(100);
    public double Multiplier { get; set; } = 2.0;
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);
    public BackoffStrategy Strategy { get; set; } = BackoffStrategy.ExponentialWithJitter;
    public Func<Exception, bool> ShouldRetry { get; set; }
    public TimeSpan RetryBudget { get; set; } = TimeSpan.FromSeconds(300);
}

/// <summary>
/// Metrics for retry policy execution.
/// </summary>
public class RetryPolicyMetrics
{
    public string PolicyName { get; set; }
    public int TotalAttempts { get; set; }
    public int SuccessAfterRetry { get; set; }
    public int ExhaustedRetries { get; set; }
    public double AverageAttemptsPerOperation { get; set; }
    public double SuccessRatePercentage { get; set; }
    public TimeSpan TotalBackoffTime { get; set; }
}
```

### 4.5 IRetryPolicyFactory

```csharp
/// <summary>
/// Factory for creating and managing retry policies.
/// </summary>
public interface IRetryPolicyFactory
{
    /// <summary>
    /// Creates a retry policy with standard exponential backoff.
    /// </summary>
    IRetryPolicy CreateExponentialBackoffPolicy(
        int maxAttempts = 3,
        TimeSpan? initialDelay = null);

    /// <summary>
    /// Creates a retry policy with custom configuration.
    /// </summary>
    IRetryPolicy CreatePolicy(RetryPolicyConfig config);

    /// <summary>
    /// Gets an existing policy by name.
    /// </summary>
    IRetryPolicy? GetPolicy(string name);

    /// <summary>
    /// Gets all registered policies.
    /// </summary>
    IReadOnlyDictionary<string, IRetryPolicy> GetAllPolicies();

    /// <summary>
    /// Removes a policy.
    /// </summary>
    void RemovePolicy(string name);
}
```

### 4.6 IPartialFunctionalityManager

```csharp
/// <summary>
/// Manages partial functionality mode when services are degraded.
/// </summary>
public interface IPartialFunctionalityManager
{
    /// <summary>
    /// Gets the current degradation level.
    /// </summary>
    DegradationLevel GetCurrentDegradationLevel();

    /// <summary>
    /// Sets the degradation level.
    /// </summary>
    Task SetDegradationLevelAsync(DegradationLevel level, string reason);

    /// <summary>
    /// Checks if a feature is available at current degradation level.
    /// </summary>
    bool IsFeatureAvailable(string featureName);

    /// <summary>
    /// Gets all available features at current degradation level.
    /// </summary>
    IReadOnlyList<string> GetAvailableFeatures();

    /// <summary>
    /// Gets all unavailable features at current degradation level.
    /// </summary>
    IReadOnlyList<string> GetUnavailableFeatures();

    /// <summary>
    /// Gets degradation metadata for API responses.
    /// </summary>
    DegradationMetadata GetDegradationMetadata();

    /// <summary>
    /// Registers a feature with its degradation requirements.
    /// </summary>
    void RegisterFeature(string featureName, DegradationLevel minimumLevel);

    /// <summary>
    /// Subscribes to degradation level changes.
    /// </summary>
    IDisposable SubscribeToDegradationChanges(
        Func<DegradationLevelChanged, Task> handler);
}

/// <summary>
/// Degradation levels indicating feature availability.
/// </summary>
public enum DegradationLevel
{
    Full = 0,                // All features available
    DegradedModerate = 1,    // Core + limited features
    DegradedSevere = 2,      // Core features only (read-only)
    Offline = 3              // Minimal offline functionality
}

/// <summary>
/// Metadata about current degradation state.
/// </summary>
public class DegradationMetadata
{
    public DegradationLevel CurrentLevel { get; set; }
    public string Reason { get; set; }
    public DateTime ChangedAt { get; set; }
    public double EstimatedRecoveryMinutes { get; set; }
    public List<string> AffectedServices { get; set; }
}

/// <summary>
/// Event raised when degradation level changes.
/// </summary>
public class DegradationLevelChanged
{
    public DegradationLevel PreviousLevel { get; set; }
    public DegradationLevel NewLevel { get; set; }
    public string Reason { get; set; }
    public DateTime ChangedAt { get; set; }
}
```

### 4.7 IDependencyHealthTracker

```csharp
/// <summary>
/// Tracks health status of internal and external dependencies.
/// </summary>
public interface IDependencyHealthTracker
{
    /// <summary>
    /// Registers a dependency for health monitoring.
    /// </summary>
    Task RegisterDependencyAsync(
        string name,
        IHealthCheck healthCheck,
        TimeSpan checkInterval);

    /// <summary>
    /// Gets health status of a specific dependency.
    /// </summary>
    DependencyHealth GetDependencyHealth(string name);

    /// <summary>
    /// Gets health status of all dependencies.
    /// </summary>
    IReadOnlyList<DependencyHealth> GetAllDependencies();

    /// <summary>
    /// Gets aggregated system health.
    /// </summary>
    SystemHealthStatus GetSystemHealth();

    /// <summary>
    /// Unregisters a dependency.
    /// </summary>
    Task UnregisterDependencyAsync(string name);

    /// <summary>
    /// Gets health check history for a dependency.
    /// </summary>
    IReadOnlyList<HealthCheckResult> GetHealthHistory(string name, int maxResults = 100);

    /// <summary>
    /// Manually triggers a health check for a dependency.
    /// </summary>
    Task<HealthCheckResult> CheckHealthAsync(string name);

    /// <summary>
    /// Subscribes to health status changes.
    /// </summary>
    IDisposable SubscribeToHealthChanges(
        Func<DependencyHealthChanged, Task> handler);
}

/// <summary>
/// Performs health checks for a dependency.
/// </summary>
public interface IHealthCheck
{
    /// <summary>
    /// Name of the health check.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Performs the health check.
    /// </summary>
    Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Result of a health check.
/// </summary>
public class HealthCheckResult
{
    public bool IsHealthy { get; set; }
    public DependencyHealthStatus Status { get; set; }
    public string Message { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public DateTime CheckedAt { get; set; }
    public Exception? Exception { get; set; }
}

/// <summary>
/// Health status of a dependency.
/// </summary>
public enum DependencyHealthStatus
{
    Healthy = 0,
    Degraded = 1,
    Critical = 2,
    Unknown = 3
}

/// <summary>
/// Current health of a dependency.
/// </summary>
public class DependencyHealth
{
    public string Name { get; set; }
    public DependencyHealthStatus Status { get; set; }
    public DateTime LastCheckedAt { get; set; }
    public TimeSpan LastResponseTime { get; set; }
    public int ConsecutiveFailures { get; set; }
    public DateTime LastStatusChangeAt { get; set; }
    public string LastMessage { get; set; }
}

/// <summary>
/// System-wide health aggregation.
/// </summary>
public class SystemHealthStatus
{
    public DependencyHealthStatus OverallStatus { get; set; }
    public int HealthyDependencies { get; set; }
    public int DegradedDependencies { get; set; }
    public int CriticalDependencies { get; set; }
    public int UnknownDependencies { get; set; }
    public List<DependencyHealth> Details { get; set; }
}

/// <summary>
/// Event raised when dependency health changes.
/// </summary>
public class DependencyHealthChanged
{
    public string DependencyName { get; set; }
    public DependencyHealthStatus PreviousStatus { get; set; }
    public DependencyHealthStatus NewStatus { get; set; }
    public string Reason { get; set; }
    public DateTime ChangedAt { get; set; }
}
```

### 4.8 IGracefulShutdownManager

```csharp
/// <summary>
/// Manages graceful shutdown procedures with multiple phases.
/// </summary>
public interface IGracefulShutdownManager
{
    /// <summary>
    /// Gets current shutdown state.
    /// </summary>
    ShutdownState GetState();

    /// <summary>
    /// Initiates graceful shutdown.
    /// </summary>
    Task<ShutdownProgress> InitiateShutdownAsync(
        string reason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the system is shutting down.
    /// </summary>
    bool IsShuttingDown { get; }

    /// <summary>
    /// Checks if new requests should be accepted.
    /// </summary>
    bool ShouldAcceptNewRequests { get; }

    /// <summary>
    /// Waits for all in-flight requests to complete.
    /// </summary>
    Task WaitForInFlightRequestsAsync(TimeSpan timeout);

    /// <summary>
    /// Registers a graceful shutdown handler.
    /// </summary>
    void RegisterShutdownHandler(Func<Task> handler);

    /// <summary>
    /// Gets shutdown progress.
    /// </summary>
    ShutdownProgress GetProgress();

    /// <summary>
    /// Subscribes to shutdown events.
    /// </summary>
    IDisposable SubscribeToShutdownEvents(
        Func<ShutdownEvent, Task> handler);

    /// <summary>
    /// Forces shutdown after timeout.
    /// </summary>
    Task ForceShutdownAsync();
}

/// <summary>
/// Current state of shutdown process.
/// </summary>
public enum ShutdownState
{
    Running = 0,
    GracePeriod = 1,
    InFlightCompletion = 2,
    DataFlush = 3,
    Stopped = 4,
    ForcedStop = 5
}

/// <summary>
/// Progress of shutdown process.
/// </summary>
public class ShutdownProgress
{
    public ShutdownState CurrentState { get; set; }
    public string Reason { get; set; }
    public DateTime StartedAt { get; set; }
    public int InFlightRequests { get; set; }
    public int CompletedRequests { get; set; }
    public double ProgressPercentage { get; set; }
    public TimeSpan ElapsedTime { get; set; }
    public TimeSpan RemainingTime { get; set; }
}

/// <summary>
/// Configuration for graceful shutdown.
/// </summary>
public class GracefulShutdownConfig
{
    public TimeSpan GracePeriod { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan InFlightCompletionTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan DataFlushTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool LogShutdownMetrics { get; set; } = true;
    public bool PersistShutdownInfo { get; set; } = true;
}

/// <summary>
/// Base class for shutdown events.
/// </summary>
public abstract class ShutdownEvent
{
    public DateTime OccurredAt { get; set; }
}

/// <summary>
/// Event: Shutdown initiated.
/// </summary>
public class ShutdownInitiated : ShutdownEvent
{
    public string Reason { get; set; }
}

/// <summary>
/// Event: Transitioning between shutdown phases.
/// </summary>
public class ShutdownPhaseChanged : ShutdownEvent
{
    public ShutdownState PreviousState { get; set; }
    public ShutdownState NewState { get; set; }
}

/// <summary>
/// Event: Shutdown completed.
/// </summary>
public class ShutdownCompleted : ShutdownEvent
{
    public int TotalRequestsProcessed { get; set; }
    public TimeSpan TotalDuration { get; set; }
}
```

---

## 5. ASCII ARCHITECTURE DIAGRAMS

### 5.1 Circuit Breaker State Machine

```
┌─────────────────────────────────────────────────────────────────┐
│                   CIRCUIT BREAKER STATE MACHINE                  │
└─────────────────────────────────────────────────────────────────┘

                              ┌─────────────┐
                              │   CLOSED    │
                              │  (Normal)   │
                              └──────┬──────┘
                                     │
                    ┌────────────────┼────────────────┐
                    │                │                │
            (Success)          (Failure)        (Failure)
                    │          Threshold            Count
                    │          Exceeded             Exceeded
                    │                │                │
                    ↓                ↓                ↓
              ┌──────────┐      ┌─────────┐
              │ CLOSED   │◄─────┤  OPEN   │
              │(Continue)│      │(Fail    │
              └──────────┘      │ Fast)   │
                    ▲            └────┬────┘
                    │                 │
                    │         (Timeout)
                    │          Reached
                    │             │
                    │             ↓
                    │        ┌──────────────┐
                    │        │ HALF-OPEN    │
                    │        │(Test/Recover)│
                    │        └──────┬───────┘
                    │               │
                    ├───────────────┤
                    │               │
               (Success)       (Failure)
                    │               │
                    │               ↓
                    └────────► ┌─────────┐
                               │  OPEN   │
                               │ (Fail   │
                               │  Fast)  │
                               └─────────┘

Key Transitions:
  Closed → Open: Failure threshold exceeded (5 failures in 30s)
  Open → Half-Open: Timeout elapsed (60s)
  Half-Open → Closed: Test request succeeds
  Half-Open → Open: Test request fails
```

### 5.2 Fallback Strategy Chain

```
┌──────────────────────────────────────────────────────────────────┐
│                   FALLBACK STRATEGY CHAIN                         │
└──────────────────────────────────────────────────────────────────┘

Request
  │
  ↓
┌──────────────────────┐
│ Primary Service      │
│ (Cloud AI Provider)  │
└──────┬───────────────┘
       │
       ├─→ Success → Return Result
       │
       └─→ Failure
           │
           ↓
    ┌──────────────────────┐
    │ Fallback 1           │
    │ (Local LLM)          │
    └──────┬───────────────┘
           │
           ├─→ Success → Return Result
           │
           └─→ Failure
               │
               ↓
        ┌──────────────────────┐
        │ Fallback 2           │
        │ (Cached Results)     │
        └──────┬───────────────┘
               │
               ├─→ Success → Return Cached Result
               │
               └─→ No Cache
                   │
                   ↓
            ┌──────────────────────┐
            │ Fallback 3           │
            │ (Offline Mode)       │
            │ (Placeholder/Error)  │
            └──────────────────────┘

Execution Time Budget: 5000ms
  Primary: 3000ms max
  Fallback 1: 1500ms max
  Fallback 2: 300ms max
  Fallback 3: 200ms max
```

### 5.3 System Degradation Levels

```
┌──────────────────────────────────────────────────────────────────┐
│              SYSTEM DEGRADATION LEVELS & FEATURES                 │
└──────────────────────────────────────────────────────────────────┘

Level 0: FULL OPERATION
├─ All APIs operational
├─ Full database access (read/write)
├─ All AI models available
├─ Real-time search/indexing
├─ External integrations
└─ Normal latencies

         ↓ (Services degrading)

Level 1: DEGRADED-MODERATE
├─ Core APIs only
├─ Full database access (read/write)
├─ Cached AI responses
├─ No external integrations
├─ Slower response times (100ms additional)
└─ ~85% normal functionality

         ↓ (Services failing)

Level 2: DEGRADED-SEVERE
├─ Core read-only APIs
├─ Database read-only access
├─ Cached responses only
├─ No write operations
├─ Slower response times (500ms additional)
└─ ~40% normal functionality

         ↓ (All services unavailable)

Level 3: OFFLINE
├─ Minimal offline functionality
├─ Local cache only
├─ No external connectivity
├─ No database access
└─ ~10% normal functionality (emergency mode)
```

### 5.4 Dependency Health Tracking

```
┌──────────────────────────────────────────────────────────────────┐
│              DEPENDENCY HEALTH TRACKING SYSTEM                    │
└──────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                    DEPENDENCY HEALTH MONITOR                     │
└──────────────────┬────────────────────────────────────────────┬──┘
                   │                                            │
        ┌──────────┴──────────┐                  ┌──────────────┴───────────┐
        │                     │                  │                          │
     Health Check           Health Check      Health Check             Health Check
     Every 30s              Every 30s         Every 30s                Every 30s
        │                     │                  │                          │
    Database              Cache Service     AI Provider               External API
        │                     │                  │                          │
        ↓                     ↓                  ↓                          ↓
    ┌────────┐           ┌────────┐        ┌────────┐              ┌────────────┐
    │ HEALTHY│           │ HEALTHY│        │DEGRADED│              │ CRITICAL   │
    │ 200ms  │           │ 45ms   │        │ 3000ms │              │ TIMEOUT    │
    └────────┘           └────────┘        └────────┘              └────────────┘

           ↓                  ↓                  ↓                      ↓
        [✓ OK]            [✓ OK]            [△ WARN]              [✗ FAIL]
           │                  │                  │                      │
           └──────────────────┬──────────────────┴──────────────────────┘
                              │
                              ↓
                    ┌──────────────────────┐
                    │ Aggregation Engine   │
                    │ (DependencyHealth    │
                    │  Tracker)            │
                    └──────────┬───────────┘
                               │
                    ┌──────────┴──────────────┐
                    │                        │
                    ↓                        ↓
            ┌──────────────┐        ┌───────────────┐
            │System Health │        │Degradation    │
            │: DEGRADED    │        │Level: MODERATE│
            └──────────────┘        └───────────────┘
                    │
                    ↓
            [Trigger Actions]
            ├─ Publish HealthChanged event
            ├─ Update degradation level
            ├─ Activate fallbacks
            └─ Alert monitoring systems
```

### 5.5 Graceful Shutdown Phases

```
┌──────────────────────────────────────────────────────────────────┐
│              GRACEFUL SHUTDOWN PHASE SEQUENCE                     │
└──────────────────────────────────────────────────────────────────┘

Phase 1: GRACE PERIOD (30 seconds)
┌────────────────────────────────────┐
│ Duration: 30s                      │
│ ✓ Accept new requests              │
│ ✓ Publish ShutdownInitiatedEvent   │
│ ✓ UI shows "Maintenance in 30s"    │
│ ✓ Log shutdown to PostgreSQL       │
└────────────────────────────────────┘
         │
         ↓ (30s elapsed)

Phase 2: IN-FLIGHT COMPLETION (5 minutes)
┌────────────────────────────────────┐
│ Duration: 5m (max)                 │
│ ✗ Reject new requests (408)        │
│ ✓ Process existing requests        │
│ ✓ Monitor completion               │
│ ✓ UI shows "System shutting down"  │
│ ? Force stop after 5m              │
└────────────────────────────────────┘
         │
         ↓ (All requests done or timeout)

Phase 3: DATA FLUSH (30 seconds)
┌────────────────────────────────────┐
│ Duration: 30s                      │
│ ✗ No requests accepted             │
│ ✓ Flush all caches                 │
│ ✓ Persist pending writes           │
│ ✓ Close DB connections (gracefully)│
│ ✓ Publish event logs               │
└────────────────────────────────────┘
         │
         ↓ (All data flushed)

Phase 4: STOP
┌────────────────────────────────────┐
│ ✓ Close remaining connections      │
│ ✓ Publish ShutdownCompleted event  │
│ ✓ Log final metrics                │
│ ✓ Exit application (code 0)        │
└────────────────────────────────────┘

Request Timeline:
  Time 0s    ⟿ New request arrives (accepted)
  Time 10s   ⟿ New request arrives (accepted)
  Time 31s   ⟿ New request arrives (rejected with 408)
  Time 35s   ⟿ Request from 10s completes (processed)
  Time 50s   ⟿ Request from 0s completes (processed)
  Time 65s   ⟿ All requests done, moving to flush
  Time 95s   ⟿ All data flushed, system stops
```

---

## 6. FALLBACK STRATEGY MATRIX

### 6.1 Cloud AI Service Fallback Chain

| Level | Service | Timeout | Cost | Latency | Quality | When Used |
|-------|---------|---------|------|---------|---------|-----------|
| 1 (Primary) | Cloud AI Provider (OpenAI/Claude) | 30s | High | 1-3s | Best | All requests |
| 2 | Local LLM (Ollama) | 20s | Zero | 2-5s | Good | Cloud timeout |
| 3 | Cached Results | 1s | Zero | <50ms | Same | LLM unavailable |
| 4 | Offline Mode | 100ms | Zero | <10ms | Limited | All fail |

### 6.2 Database Service Fallback Chain

| Level | Service | Mode | Latency | Features | When Used |
|-------|---------|------|---------|----------|-----------|
| 1 (Primary) | PostgreSQL | Full R/W | 10-50ms | All | Normal |
| 2 | Replica DB | Read-Only | 50-100ms | Queries | Primary down |
| 3 | SQLite | Local R/W | 5-20ms | Core schema | Replica down |
| 4 | Memory Cache | Read-Only | <1ms | Subset | All fail |

### 6.3 Search Service Fallback Chain

| Level | Service | Method | Latency | Completeness | When Used |
|-------|---------|--------|---------|--------------|-----------|
| 1 (Primary) | Elasticsearch | Full-Text | 50-200ms | 100% | Normal |
| 2 | PostgreSQL FTS | Native FTS | 100-500ms | 100% | ES down |
| 3 | Memory Index | In-Memory | 10-50ms | 70% | Both down |
| 4 | Keyword Match | Simple | <10ms | 30% | All fail |

### 6.4 Cache Service Fallback Chain

| Level | Service | Location | Latency | Capacity | When Used |
|-------|---------|----------|---------|----------|-----------|
| 1 (Primary) | Redis Cluster | Distributed | 1-5ms | Large | Normal |
| 2 | Redis Node | Single | 2-10ms | Medium | Cluster down |
| 3 | In-Memory Cache | Process | <1ms | Small | Redis down |
| 4 | Database Query | PostgreSQL | 20-100ms | Full | Memory full |

### 6.5 Authentication Fallback Chain

| Level | Service | Method | Latency | When Used |
|-------|---------|--------|---------|-----------|
| 1 (Primary) | OAuth Provider | Cloud | 500-1000ms | Normal |
| 2 | Local Validation | Cached | 5-20ms | Provider down |
| 3 | Session Token | Verified | <1ms | Cache expired |
| 4 | Read-Only Access | Limited | N/A | All fail |

---

## 7. POSTGRESQL SCHEMA

### 7.1 Circuit Breaker States Table

```sql
CREATE TABLE circuit_breaker_states (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL UNIQUE,
    current_state VARCHAR(20) NOT NULL CHECK (current_state IN ('Closed', 'Open', 'HalfOpen')),
    failure_count INT NOT NULL DEFAULT 0,
    success_count INT NOT NULL DEFAULT 0,
    last_failure_time TIMESTAMP WITH TIME ZONE,
    last_state_change_time TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    open_until TIMESTAMP WITH TIME ZONE,
    half_open_attempts INT NOT NULL DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    metadata JSONB,

    INDEX idx_circuit_breaker_state (current_state),
    INDEX idx_circuit_breaker_updated (updated_at DESC)
);

CREATE TABLE circuit_breaker_transitions (
    id BIGSERIAL PRIMARY KEY,
    circuit_breaker_id BIGINT NOT NULL REFERENCES circuit_breaker_states(id) ON DELETE CASCADE,
    from_state VARCHAR(20) NOT NULL,
    to_state VARCHAR(20) NOT NULL,
    reason VARCHAR(500),
    failure_exception TEXT,
    transitioned_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),

    INDEX idx_circuit_breaker_transitions (circuit_breaker_id, transitioned_at DESC),
    INDEX idx_transitions_time (transitioned_at DESC)
);
```

### 7.2 Fallback Executions Table

```sql
CREATE TABLE fallback_executions (
    id BIGSERIAL PRIMARY KEY,
    service_name VARCHAR(255) NOT NULL,
    strategy_name VARCHAR(255) NOT NULL,
    attempt_number INT NOT NULL,
    success BOOLEAN NOT NULL,
    primary_exception TEXT,
    fallback_exception TEXT,
    execution_time_ms INT NOT NULL,
    response_time_ms INT NOT NULL,
    executed_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    context_data JSONB,

    INDEX idx_fallback_service (service_name),
    INDEX idx_fallback_strategy (strategy_name),
    INDEX idx_fallback_executed (executed_at DESC),
    INDEX idx_fallback_success (success)
);

CREATE TABLE fallback_strategy_metrics (
    id BIGSERIAL PRIMARY KEY,
    service_name VARCHAR(255) NOT NULL UNIQUE,
    total_attempts INT NOT NULL DEFAULT 0,
    successful_fallbacks INT NOT NULL DEFAULT 0,
    failed_fallbacks INT NOT NULL DEFAULT 0,
    average_fallback_time_ms DECIMAL(10, 2),
    last_activated TIMESTAMP WITH TIME ZONE,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),

    INDEX idx_metrics_service (service_name)
);
```

### 7.3 Dependency Health Table

```sql
CREATE TABLE dependency_health_checks (
    id BIGSERIAL PRIMARY KEY,
    dependency_name VARCHAR(255) NOT NULL,
    health_status VARCHAR(20) NOT NULL CHECK (health_status IN ('Healthy', 'Degraded', 'Critical', 'Unknown')),
    response_time_ms INT,
    is_healthy BOOLEAN NOT NULL,
    message TEXT,
    exception_type VARCHAR(255),
    exception_message TEXT,
    checked_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),

    INDEX idx_dependency_health_status (dependency_name, health_status),
    INDEX idx_dependency_health_checked (checked_at DESC),
    INDEX idx_dependency_health_latest (dependency_name, checked_at DESC)
);

CREATE TABLE dependency_health_summary (
    id BIGSERIAL PRIMARY KEY,
    dependency_name VARCHAR(255) NOT NULL UNIQUE,
    current_status VARCHAR(20) NOT NULL,
    last_checked_at TIMESTAMP WITH TIME ZONE,
    response_time_ms INT,
    consecutive_failures INT DEFAULT 0,
    last_status_change_at TIMESTAMP WITH TIME ZONE,
    last_message TEXT,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),

    INDEX idx_dependency_summary_status (current_status)
);

CREATE TABLE dependency_relationships (
    id BIGSERIAL PRIMARY KEY,
    parent_dependency VARCHAR(255) NOT NULL,
    child_dependency VARCHAR(255) NOT NULL,
    depends_on_order INT NOT NULL DEFAULT 0,
    critical BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),

    UNIQUE(parent_dependency, child_dependency),
    INDEX idx_dependency_relationships (parent_dependency)
);
```

### 7.4 Retry Policy Executions Table

```sql
CREATE TABLE retry_policy_executions (
    id BIGSERIAL PRIMARY KEY,
    policy_name VARCHAR(255) NOT NULL,
    operation_name VARCHAR(255),
    total_attempts INT NOT NULL,
    successful BOOLEAN NOT NULL,
    final_exception TEXT,
    total_backoff_time_ms INT,
    total_execution_time_ms INT,
    executed_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),

    INDEX idx_retry_policy_name (policy_name),
    INDEX idx_retry_successful (successful),
    INDEX idx_retry_executed (executed_at DESC)
);

CREATE TABLE retry_attempt_log (
    id BIGSERIAL PRIMARY KEY,
    execution_id BIGINT NOT NULL REFERENCES retry_policy_executions(id) ON DELETE CASCADE,
    attempt_number INT NOT NULL,
    exception_type VARCHAR(255),
    exception_message TEXT,
    wait_time_ms INT,
    attempt_time TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),

    INDEX idx_retry_attempt_execution (execution_id),
    INDEX idx_retry_attempt_time (attempt_time DESC)
);
```

### 7.5 Graceful Shutdown Records Table

```sql
CREATE TABLE graceful_shutdown_records (
    id BIGSERIAL PRIMARY KEY,
    shutdown_id UUID NOT NULL UNIQUE DEFAULT gen_random_uuid(),
    reason VARCHAR(500) NOT NULL,
    initiated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    completed_at TIMESTAMP WITH TIME ZONE,
    shutdown_state VARCHAR(20) NOT NULL,
    total_requests_processed INT,
    total_duration_ms INT,
    force_stopped BOOLEAN DEFAULT FALSE,

    INDEX idx_shutdown_initiated (initiated_at DESC),
    INDEX idx_shutdown_state (shutdown_state)
);

CREATE TABLE shutdown_phase_events (
    id BIGSERIAL PRIMARY KEY,
    shutdown_id UUID NOT NULL REFERENCES graceful_shutdown_records(shutdown_id) ON DELETE CASCADE,
    from_state VARCHAR(20) NOT NULL,
    to_state VARCHAR(20) NOT NULL,
    transitioned_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    in_flight_requests INT,

    INDEX idx_shutdown_phase_events (shutdown_id, transitioned_at DESC)
);

CREATE TABLE shutdown_metrics (
    id BIGSERIAL PRIMARY KEY,
    shutdown_id UUID NOT NULL REFERENCES graceful_shutdown_records(shutdown_id) ON DELETE CASCADE,
    grace_period_ms INT,
    in_flight_completion_ms INT,
    data_flush_ms INT,
    total_shutdown_ms INT,
    requests_in_grace_period INT,
    requests_rejected INT,
    requests_completed INT,
    recorded_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);
```

### 7.6 Degradation State Table

```sql
CREATE TABLE system_degradation_state (
    id BIGSERIAL PRIMARY KEY,
    current_level VARCHAR(30) NOT NULL CHECK (current_level IN ('Full', 'DegradedModerate', 'DegradedSevere', 'Offline')),
    reason VARCHAR(500),
    changed_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    estimated_recovery_minutes INT,
    affected_services TEXT[], -- Array of service names
    previous_level VARCHAR(30),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),

    INDEX idx_degradation_current_level (current_level),
    INDEX idx_degradation_changed (changed_at DESC)
);

CREATE TABLE degradation_level_history (
    id BIGSERIAL PRIMARY KEY,
    from_level VARCHAR(30) NOT NULL,
    to_level VARCHAR(30) NOT NULL,
    reason VARCHAR(500),
    changed_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    duration_seconds INT,

    INDEX idx_degradation_history_changed (changed_at DESC)
);

CREATE TABLE feature_availability (
    id BIGSERIAL PRIMARY KEY,
    feature_name VARCHAR(255) NOT NULL UNIQUE,
    minimum_degradation_level VARCHAR(30) NOT NULL,
    description TEXT,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);
```

---

## 8. UI MOCKUPS & COMPONENTS

### 8.1 Dashboard - System Health Overview

```
╔═══════════════════════════════════════════════════════════════════════════╗
║                        LEXICHORD SYSTEM DASHBOARD                         ║
╠═══════════════════════════════════════════════════════════════════════════╣
║                                                                           ║
║  System Status: 🟡 DEGRADED                                              ║
║  Degradation Level: MODERATE                                              ║
║  Uptime: 47d 12h 34m                                                      ║
║                                                                           ║
╠═══════════════════════════════════════════════════════════════════════════╣
║  DEPENDENCIES STATUS                                                      ║
╠═══════════════════════════════════════════════════════════════════════════╣
║                                                                           ║
║  🟢 PostgreSQL          [Healthy]  (15ms)   ✓ Read/Write                 ║
║  🟡 Elasticsearch       [Degraded] (850ms)  ⚠ Slow queries               ║
║  🔴 AI Provider (Claude)[Critical] (Timeout)✗ Using fallback             ║
║  🟢 Redis Cache         [Healthy]  (3ms)    ✓ Working                    ║
║  🟡 OAuth Provider      [Degraded] (1200ms) ⚠ Slow login                 ║
║                                                                           ║
╠═══════════════════════════════════════════════════════════════════════════╣
║  CIRCUIT BREAKERS STATUS                                                  ║
╠═══════════════════════════════════════════════════════════════════════════╣
║                                                                           ║
║  Service             State        Failures  Success Rate                 ║
║  ───────────────────────────────────────────────────────────             ║
║  ai_service          [HALF-OPEN]  2/5       60%  [Testing...]            ║
║  elasticsearch       [HALF-OPEN]  1/5       80%  [Testing...]            ║
║  external_api        [CLOSED]     0/5       100% [OK]                    ║
║  cache_service       [CLOSED]     0/5       100% [OK]                    ║
║                                                                           ║
╠═══════════════════════════════════════════════════════════════════════════╣
║  DEGRADATION DETAILS                                                      ║
╠═══════════════════════════════════════════════════════════════════════════╣
║                                                                           ║
║  Reason: AI Provider unavailable, Elasticsearch slow                      ║
║  Affected Services: AI Transcription, Advanced Search, Semantic Analysis   ║
║  Available Features: Core APIs, Caching, Basic Search                     ║
║  Estimated Recovery: ~15 minutes                                          ║
║                                                                           ║
║  [View Detailed Logs]  [Trigger Manual Recovery]  [Force Restart]         ║
║                                                                           ║
╚═══════════════════════════════════════════════════════════════════════════╝
```

### 8.2 Dependency Health Panel

```
╔═══════════════════════════════════════════════════════════════════════════╗
║                    DEPENDENCY HEALTH TRACKING PANEL                       ║
╠═══════════════════════════════════════════════════════════════════════════╣
║                                                                           ║
║  Filter: [All ▼]  Last Updated: 2026-02-01 12:34:56 UTC                  ║
║                                                                           ║
╠═══════════════════════════════════════════════════════════════════════════╣
║  EXTERNAL DEPENDENCIES                                                    ║
╠═══════════════════════════════════════════════════════════════════════════╣
║                                                                           ║
║  Claude AI Provider                                                       ║
║  Status: 🔴 CRITICAL                                                      ║
║  Response Time: [████████░░] TIMEOUT (30000ms)                            ║
║  Health Checks: ✗ Failed 5 in a row                                       ║
║  Last Healthy: 2026-02-01 11:45:23 UTC                                    ║
║  Fallback Active: Local LLM Model                                         ║
║  [View Details]                                                           ║
║                                                                           ║
║  OpenWeather API                                                          ║
║  Status: 🟢 HEALTHY                                                       ║
║  Response Time: [██████░░░░] 245ms                                        ║
║  Health Checks: ✓ Passed 47/47                                            ║
║  [View Details]                                                           ║
║                                                                           ║
╠═══════════════════════════════════════════════════════════════════════════╣
║  INTERNAL DEPENDENCIES                                                    ║
╠═══════════════════════════════════════════════════════════════════════════╣
║                                                                           ║
║  PostgreSQL (Primary)                                                     ║
║  Status: 🟢 HEALTHY                                                       ║
║  Response Time: [██░░░░░░░░] 18ms                                         ║
║  Health Checks: ✓ Passed 288/288                                          ║
║  Write Lag: 2ms                                                           ║
║                                                                           ║
║  Elasticsearch Cluster                                                    ║
║  Status: 🟡 DEGRADED                                                      ║
║  Response Time: [███████████] 850ms (warn: >500ms)                        ║
║  Health Checks: ⚠ Passed 45/47 (1 shard down)                             ║
║  Disk Usage: 87% (warn: >80%)                                             ║
║  Query Timeout Rate: 12% (warn: >5%)                                      ║
║  [View Details]  [Rebalance Shards]                                       ║
║                                                                           ║
║  Redis Cache                                                              ║
║  Status: 🟢 HEALTHY                                                       ║
║  Response Time: [█░░░░░░░░░] 2ms                                          ║
║  Memory Usage: 45% (optimal)                                              ║
║  Hit Rate: 94%                                                            ║
║                                                                           ║
╚═══════════════════════════════════════════════════════════════════════════╝
```

### 8.3 Circuit Breaker Status Widget

```
╔═══════════════════════════════════════════════════════════════════════════╗
║                      CIRCUIT BREAKER DASHBOARD                            ║
╠═══════════════════════════════════════════════════════════════════════════╣
║                                                                           ║
║  AI Transcription Service - Circuit Breaker                               ║
║  ┌───────────────────────────────────────────────────────────────────┐   ║
║  │ State: [HALF-OPEN]                                                │   ║
║  │                                                                   │   ║
║  │ ┌─ Closed (Normal)  ┌─ Open (Failing) → ┌─ Half-Open (Testing)   │   ║
║  │ │                    │                     │                      │   ║
║  │ └─────────────────────────────────────────┘                       │   ║
║  │          (5 failures)       (60s timeout)                          │   ║
║  │                                                                   │   ║
║  │ Failure Count: 4/5                                                │   ║
║  │ Success Count: 18                                                 │   ║
║  │ Success Rate: 81.8%                                               │   ║
║  │ Half-Open Attempts: 2/3                                           │   ║
║  │ Time Until Next Transition: 8s                                    │   ║
║  │                                                                   │   ║
║  │ Last Failure: 2026-02-01 12:28:45 UTC                             │   ║
║  │ State Changed: 2026-02-01 12:33:02 UTC (1m 25s ago)               │   ║
║  │                                                                   │   ║
║  │ Actions: [Manual Reset] [Manual Open] [View Logs]                 │   ║
║  └───────────────────────────────────────────────────────────────────┘   ║
║                                                                           ║
║  Elasticsearch Search Service - Circuit Breaker                           ║
║  ┌───────────────────────────────────────────────────────────────────┐   ║
║  │ State: [HALF-OPEN]                                                │   ║
║  │ Failure Count: 1/5                                                │   ║
║  │ Success Count: 142                                                │   ║
║  │ Success Rate: 99.3%                                               │   ║
║  │ Last Failure: 2026-02-01 12:31:15 UTC                             │   ║
║  │ State Changed: 2026-02-01 12:31:35 UTC (3m 22s ago)               │   ║
║  │                                                                   │   ║
║  │ [Status: Recovering ✓]                                            │   ║
║  └───────────────────────────────────────────────────────────────────┘   ║
║                                                                           ║
║  External API Service - Circuit Breaker                                   ║
║  ┌───────────────────────────────────────────────────────────────────┐   ║
║  │ State: [CLOSED] ✓ Normal                                          │   ║
║  │ Failure Count: 0/5                                                │   ║
║  │ Success Count: 5,847                                              │   ║
║  │ Success Rate: 100%                                                │   ║
║  │ Last Failure: Never                                               │   ║
║  │                                                                   │   ║
║  │ [Status: Healthy ✓]                                               │   ║
║  └───────────────────────────────────────────────────────────────────┘   ║
║                                                                           ║
╚═══════════════════════════════════════════════════════════════════════════╝
```

### 8.4 Shutdown Progress UI

```
╔═══════════════════════════════════════════════════════════════════════════╗
║                         GRACEFUL SHUTDOWN IN PROGRESS                     ║
╠═══════════════════════════════════════════════════════════════════════════╣
║                                                                           ║
║  🟡 SYSTEM MAINTENANCE                                                    ║
║  The system will be unavailable for maintenance in 30 seconds.            ║
║  Current connections will be preserved.                                   ║
║                                                                           ║
╠═══════════════════════════════════════════════════════════════════════════╣
║  SHUTDOWN PROGRESS                                                        ║
╠═══════════════════════════════════════════════════════════════════════════╣
║                                                                           ║
║  Phase 1: Grace Period                          [████████████████░░] 95%  ║
║           Time Remaining: 1s                    [Accepting new requests]  ║
║                                                                           ║
║  Phase 2: In-Flight Completion                  [░░░░░░░░░░░░░░░░░░░░] 0% ║
║           (Starts in 1s)                                                  ║
║                                                                           ║
║  Phase 3: Data Flush                            [░░░░░░░░░░░░░░░░░░░░] 0% ║
║           (Starts after all requests complete)                            ║
║                                                                           ║
║  Overall Progress                               [████████████████░░] 60%  ║
║  Total Elapsed Time: 18 seconds                                           ║
║  Estimated Total Time: 6 minutes 8 seconds                                ║
║                                                                           ║
╠═══════════════════════════════════════════════════════════════════════════╣
║  CURRENT STATISTICS                                                       ║
╠═══════════════════════════════════════════════════════════════════════════╣
║                                                                           ║
║  Requests Accepted in Grace Period: 23                                    ║
║  Requests Currently In Flight: 8                                          ║
║  Requests Completed: 15                                                   ║
║  Estimated Time for All Requests: 2m 30s                                  ║
║                                                                           ║
║  Shutdown Reason: Scheduled maintenance - Database migration               ║
║                                                                           ║
╠═══════════════════════════════════════════════════════════════════════════╣
║  ACTIONS                                                                  ║
╠═══════════════════════════════════════════════════════════════════════════╣
║                                                                           ║
║  [Cancel Shutdown]  [Force Shutdown Now]  [View Shutdown Log]             ║
║                                                                           ║
╚═══════════════════════════════════════════════════════════════════════════╝
```

---

## 9. DEPENDENCY CHAIN ANALYSIS

### 9.1 Critical Dependency Chains

```
Critical Chain 1: Search Functionality
  Search Request
    ↓
  Elasticsearch (Primary) [Critical]
    ↓ (Fallback on timeout)
  PostgreSQL Full-Text Search [Critical]
    ↓ (Fallback if slow)
  In-Memory Index [Required]
    ↓ (Fallback if memory full)
  Keyword Matching [Required]

  Break Point: If all fail → Search feature unavailable

Critical Chain 2: AI Transcription
  Transcription Request
    ↓
  Claude AI API [Critical]
    ↓ (Fallback on timeout/error)
  Local Ollama Model [Critical]
    ↓ (Fallback if unavailable)
  Cached Transcriptions [Important]
    ↓ (Fallback if not cached)
  Text Placeholder [Required]

  Break Point: If all fail → Transcription unavailable (read-only mode)

Critical Chain 3: Data Persistence
  Write Operation
    ↓
  PostgreSQL [Critical]
    ↓ (Fallback on connection loss)
  Write Buffer [Important]
    ↓ (Fallback to queue)
  Retry Queue [Important]
    ↓
  Graceful Shutdown Flush [Required]

  Break Point: If PostgreSQL down → Write-only buffer mode
```

### 9.2 Dependency Health Propagation

```
Dependency Health → System Degradation Level:

All Healthy
  └─ System Level: FULL ✓

1 Non-Critical Degraded
  └─ System Level: FULL (feature-specific fallback)

1 Critical Degraded (e.g., Elasticsearch)
  └─ System Level: DEGRADED-MODERATE (search unavailable, cache used)

2 Critical Degraded (e.g., ES + AI Provider)
  └─ System Level: DEGRADED-MODERATE (both features reduced)

PostgreSQL Down
  └─ System Level: DEGRADED-SEVERE (read-only mode)

3+ Critical Down
  └─ System Level: OFFLINE (minimal functionality)
```

---

## 10. LICENSE GATING TABLE

| Feature | Free | Pro | Enterprise |
|---------|------|-----|------------|
| Circuit Breaker Pattern | 2 breakers | 10 breakers | Unlimited |
| Fallback Strategies | 1 primary fallback | 3 fallback tiers | 5+ tiers |
| Retry Policies | Basic (3 attempts) | Advanced (configurable) | Custom policies |
| Dependency Health Tracking | Basic (5 dependencies) | Standard (50 deps) | Unlimited |
| Graceful Shutdown | 30s grace period | 2m grace period | Custom |
| SLA Uptime Target | 95% | 99.5% | 99.95% |
| Health Check Interval | 5 minutes | 30 seconds | 10 seconds |
| Metrics Retention | 7 days | 30 days | 1 year |
| Alert Notifications | Email only | Email + SMS + Slack | Custom webhook |
| Support Priority | Community | Priority | Dedicated |

---

## 11. PERFORMANCE TARGETS

### 11.1 Circuit Breaker Performance

| Metric | Target | Rationale |
|--------|--------|-----------|
| State Check Latency | < 0.1ms | Minimal overhead |
| State Transition Latency | < 1ms | Fast response to failures |
| Failure Threshold Eval | < 0.05ms | Constant-time lookup |
| Memory per Breaker | < 1KB | Lightweight |
| Maximum Breakers | 1000+ | Enterprise scale |

### 11.2 Fallback Strategy Performance

| Metric | Target | Rationale |
|--------|--------|-----------|
| Fallback Switch Time | < 50ms | Perceivable to user |
| Strategy Registration | < 10ms | Startup performance |
| Chain Depth Overhead | < 1ms per level | Acceptable for 3-4 levels |
| Memory per Strategy | < 2KB | Lightweight |
| Metrics Recording | < 0.5ms | Low overhead |

### 11.3 Retry Policy Performance

| Metric | Target | Rationale |
|--------|--------|-----------|
| Retry Decision Time | < 0.1ms | Minimal overhead |
| Backoff Calculation | < 0.5ms | Fast scheduling |
| Jitter Generation | < 1ms | Prevent thundering herd |
| Memory per Policy | < 3KB | Lightweight |
| Retry Budget Check | < 0.05ms | Constant-time |

### 11.4 Health Tracking Performance

| Metric | Target | Rationale |
|--------|--------|-----------|
| Health Check Execution | < 100ms | 30s interval allows this |
| Status Aggregation | < 10ms | Per 50 dependencies |
| Database Insert | < 5ms | PostgreSQL write |
| Event Publishing | < 1ms | Async operation |
| Memory per Dependency | < 2KB | Store last 100 checks |

### 11.5 Graceful Shutdown Performance

| Metric | Target | Rationale |
|--------|--------|-----------|
| Shutdown Signal Processing | < 10ms | Immediate response |
| Grace Period Announcement | < 50ms | User notification |
| Request Cancellation | < 100ms per 1000 reqs | Batch operation |
| Data Flush | < 5s for 10MB buffer | I/O bound |
| Connection Closure | < 30s | Graceful close all |

---

## 12. TESTING STRATEGY

### 12.1 Unit Testing

#### Circuit Breaker Tests
```csharp
[TestFixture]
public class CircuitBreakerTests
{
    [Test]
    public async Task FailureThresholdTripsCircuit()
    {
        // Arrange
        var breaker = new CircuitBreaker("test", config: FailureThreshold = 5);

        // Act
        for (int i = 0; i < 5; i++)
            await breaker.ExecuteAsync(() => FailingOperation());

        // Assert
        Assert.That(breaker.State, Is.EqualTo(CircuitBreakerState.Open));
    }

    [Test]
    public async Task ClosedCircuitAllowsSuccessfulCalls()
    {
        // Verify normal operation passes through
    }

    [Test]
    public async Task OpenCircuitRejectsImmediately()
    {
        // Verify circuit breaker exception thrown without execution
    }

    [Test]
    public async Task HalfOpenTriesRecovery()
    {
        // Verify state transition and recovery attempt
    }
}
```

#### Fallback Strategy Tests
```csharp
[TestFixture]
public class FallbackStrategyTests
{
    [Test]
    public async Task ActivatesFallbackOnPrimaryFailure()
    {
        // Verify fallback chain execution
    }

    [Test]
    public async Task SkipsFailedFallbacksInChain()
    {
        // Verify chain of responsibility
    }

    [Test]
    public async Task TimeoutSwitchesToFallback()
    {
        // Verify timeout-based fallback
    }

    [Test]
    public async Task ContextPropagatesAcrossChain()
    {
        // Verify context data maintained
    }
}
```

#### Retry Policy Tests
```csharp
[TestFixture]
public class RetryPolicyTests
{
    [Test]
    public async Task SucceedsAfterTransientFailure()
    {
        // Verify successful retry
    }

    [Test]
    public async Task ExhaustsAttemptsAfterMaxRetries()
    {
        // Verify max attempts respected
    }

    [Test]
    public async Task ExponentialBackoffIncreases()
    {
        // Verify backoff timing
    }

    [Test]
    public async Task JitterPreventsThunderingHerd()
    {
        // Verify jitter distribution
    }
}
```

### 12.2 Integration Testing

#### End-to-End Failure Scenarios

```csharp
[TestFixture]
public class IntegrationTests
{
    [Test]
    public async Task ServiceFailureDegradesToFallback()
    {
        // Start primary service
        // Simulate service failure
        // Verify fallback activated
        // Verify degradation level changed
        // Verify MediatR events published
    }

    [Test]
    public async Task MultipleFailuresDegradeSeverely()
    {
        // Fail multiple critical services
        // Verify system degradation to severe level
        // Verify read-only mode activated
        // Verify cache-backed responses
    }

    [Test]
    public async Task PartialRecoveryUpgradesToModerate()
    {
        // Degrade system
        // Recover one dependency
        // Verify degradation level improved
    }

    [Test]
    public async Task FullRecoveryRestoresFullOperation()
    {
        // Degrade system
        // Recover all dependencies
        // Verify full operation restored
    }
}
```

### 12.3 Chaos Engineering Approach

#### Chaos Experiments

```
Experiment 1: Database Connection Loss
  Setup: Normal traffic with active users
  Chaos: Close all PostgreSQL connections
  Expected:
    - Write buffer activates immediately
    - Read operations fallback to cache
    - Graceful degradation to read-only
    - Automatic recovery when DB reconnects
  Recovery: Connection restore
  Rollback: N/A (automatic)

Experiment 2: Cascading Dependency Failures
  Setup: All dependencies healthy
  Chaos: Fail AI Provider, then Elasticsearch in sequence
  Expected:
    - AI Provider fallback activates (Local LLM)
    - Elasticsearch fallback activates (DB FTS)
    - System degrades to moderate level
    - Features disabled gracefully
  Verification:
    - UI shows all disabled features
    - API returns degradation metadata
    - Users notified of status
  Recovery: Manual restoration

Experiment 3: High Latency on Critical Service
  Setup: Normal load (100 req/s)
  Chaos: PostgreSQL latency increases to 500ms
  Expected:
    - Circuit breaker enters half-open
    - Fallback to Redis cache for read-heavy ops
    - System monitors for recovery
    - Automatic recovery when latency normalizes
  Metrics:
    - P99 latency increase < 100ms
    - Error rate < 2%
    - Cache hit rate increases

Experiment 4: Graceful Shutdown Under Load
  Setup: Sustained traffic (500 req/s)
  Chaos: Initiate graceful shutdown
  Expected:
    - Grace period accepts 100+ new requests
    - In-flight requests complete (no timeout)
    - All data flushed to disk
    - Clean shutdown with zero data loss
  Verification:
    - Data consistency check
    - No orphaned transactions
    - All caches flushed

Experiment 5: Retry Budget Exhaustion
  Setup: Service with transient failures (20% fail rate)
  Chaos: Sustained failures for 5 minutes
  Expected:
    - Initial retries succeed (70%+)
    - Retry budget exhausted after 5m
    - Dead-letter queue activates
    - Fast-fail with 429 errors
  Verification:
    - DLQ contains failed operations
    - Client receives proper error codes
    - No cascading failures
```

### 12.4 Performance Testing

```
Load Test 1: Circuit Breaker Overhead
  Load: 10,000 req/s with 50 active circuit breakers
  Expected: < 0.1ms latency addition
  Metrics: P50, P95, P99 latencies

Load Test 2: Fallback Chain Performance
  Load: 1,000 req/s with 4-tier fallback chain
  Expected: < 50ms to switch to fallback
  Metrics: Fallback activation latency

Load Test 3: Health Check Impact
  Load: Baseline traffic + 500 health checks/minute
  Expected: < 1% CPU overhead
  Metrics: Resource utilization

Load Test 4: Graceful Shutdown Throughput
  Load: 100-500 req/s sustained
  Chaos: Initiate shutdown
  Expected: Complete in < 6 minutes
  Metrics: Request completion time
```

---

## 13. RISKS & MITIGATIONS

### 13.1 Technical Risks

| Risk | Severity | Probability | Mitigation |
|------|----------|-------------|-----------|
| Circuit breaker state loss on restart | High | Medium | Persist state to PostgreSQL, restore on startup |
| Thundering herd during recovery | High | Medium | Exponential backoff with jitter, circuit breaker half-open limiting |
| Cascade failure despite fallbacks | High | Low | Independent fallback implementations, cascading circuit breakers |
| Fallback strategy stale data | Medium | Medium | Cache versioning, TTL-based invalidation |
| Resource exhaustion during retry storms | High | Low | Retry budget constraints, fast-fail mechanisms |
| Graceful shutdown timeout exceeded | Medium | Low | Force shutdown after timeout, audit logging |
| Performance regression from health checks | Medium | Medium | Async checks, configurable intervals, caching |
| Incorrect degradation level detection | High | Low | Multiple health check methods, manual override option |

### 13.2 Operational Risks

| Risk | Severity | Probability | Mitigation |
|------|----------|-------------|-----------|
| Operator misconfiguration | High | Medium | Configuration validation, defaults with documentation |
| Silent failures in fallback chains | High | Low | Comprehensive logging, monitoring alerts |
| Incomplete shutdown during deployment | Medium | Medium | Shutdown signal handlers, audit logging, graceful container termination |
| Health check false positives | Medium | Medium | Multiple check strategies, moving average thresholds |
| Storage exhaustion from logs | Medium | Low | Log rotation, configurable retention, compression |
| Monitoring system failure during incident | High | Low | Local state tracking, PostgreSQL as backup storage |

### 13.3 Organizational Risks

| Risk | Severity | Probability | Mitigation |
|------|----------|-------------|-----------|
| Lack of team training on new systems | Medium | Medium | Comprehensive documentation, training sessions, runbooks |
| Poor observability of degradation | High | Medium | Dashboard, alerts, email/Slack notifications |
| Delayed incident response | Medium | Low | Automated alerts, on-call rotation, escalation procedures |
| Customer communication delays | High | Medium | Automated status page updates, email notifications |

---

## 14. MEDIATSR EVENTS

### 14.1 Circuit Breaker Events

```csharp
/// <summary>
/// Published when circuit breaker state changes.
/// </summary>
public class CircuitStateChangedEvent : INotification
{
    public string CircuitBreakerName { get; set; }
    public CircuitBreakerState FromState { get; set; }
    public CircuitBreakerState ToState { get; set; }
    public string Reason { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public CircuitBreakerStatus Status { get; set; }

    public CircuitStateChangedEvent(
        string name,
        CircuitBreakerState from,
        CircuitBreakerState to,
        string reason)
    {
        CircuitBreakerName = name;
        FromState = from;
        ToState = to;
        Reason = reason;
    }
}

/// <summary>
/// Handles circuit state changed events (e.g., logging, alerting).
/// </summary>
public class CircuitStateChangedHandler : INotificationHandler<CircuitStateChangedEvent>
{
    private readonly ILogger<CircuitStateChangedHandler> _logger;
    private readonly INotificationService _notificationService;

    public async Task Handle(CircuitStateChangedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Circuit Breaker '{CircuitName}' transitioned from {FromState} to {ToState}. Reason: {Reason}",
            notification.CircuitBreakerName,
            notification.FromState,
            notification.ToState,
            notification.Reason);

        if (notification.ToState == CircuitBreakerState.Open)
        {
            await _notificationService.AlertAsync(
                severity: AlertSeverity.High,
                message: $"Circuit breaker '{notification.CircuitBreakerName}' is now OPEN",
                cancellationToken);
        }
    }
}
```

### 14.2 Fallback Events

```csharp
/// <summary>
/// Published when fallback strategy is activated.
/// </summary>
public class FallbackActivatedEvent : INotification
{
    public string ServiceName { get; set; }
    public string StrategyName { get; set; }
    public int AttemptNumber { get; set; }
    public Exception PrimaryException { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    public FallbackActivatedEvent(
        string service,
        string strategy,
        int attempt,
        Exception exception)
    {
        ServiceName = service;
        StrategyName = strategy;
        AttemptNumber = attempt;
        PrimaryException = exception;
    }
}

/// <summary>
/// Published when fallback strategy completes (success or failure).
/// </summary>
public class FallbackCompletedEvent : INotification
{
    public string ServiceName { get; set; }
    public string StrategyName { get; set; }
    public bool Success { get; set; }
    public Exception? FallbackException { get; set; }
    public TimeSpan TotalTime { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
```

### 14.3 Degradation Events

```csharp
/// <summary>
/// Published when system degradation level changes.
/// </summary>
public class DegradationLevelChangedEvent : INotification
{
    public DegradationLevel FromLevel { get; set; }
    public DegradationLevel ToLevel { get; set; }
    public string Reason { get; set; }
    public List<string> AffectedServices { get; set; } = new();
    public List<string> AffectedFeatures { get; set; } = new();
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Handler for degradation level changes.
/// </summary>
public class DegradationLevelChangedHandler : INotificationHandler<DegradationLevelChangedEvent>
{
    private readonly ILogger<DegradationLevelChangedHandler> _logger;
    private readonly IStatusPageService _statusPageService;
    private readonly IHubContext<SystemStatusHub> _hubContext;

    public async Task Handle(DegradationLevelChangedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "System degradation changed from {FromLevel} to {ToLevel}. Reason: {Reason}",
            notification.FromLevel,
            notification.ToLevel,
            notification.Reason);

        // Update status page
        await _statusPageService.UpdateStatusAsync(
            status: GetStatusPageStatus(notification.ToLevel),
            message: $"System degraded: {notification.Reason}",
            affectedComponents: notification.AffectedServices,
            cancellationToken);

        // Notify connected clients
        await _hubContext.Clients.All.SendAsync(
            "DegradationStatusChanged",
            new
            {
                Level = notification.ToLevel.ToString(),
                Reason = notification.Reason,
                AffectedServices = notification.AffectedServices
            },
            cancellationToken);
    }
}
```

### 14.4 Health Check Events

```csharp
/// <summary>
/// Published when dependency health status changes.
/// </summary>
public class DependencyHealthChangedEvent : INotification
{
    public string DependencyName { get; set; }
    public DependencyHealthStatus FromStatus { get; set; }
    public DependencyHealthStatus ToStatus { get; set; }
    public string Message { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Published periodically with system health summary.
/// </summary>
public class SystemHealthCheckCompletedEvent : INotification
{
    public SystemHealthStatus OverallStatus { get; set; }
    public List<DependencyHealth> DependencyStatuses { get; set; } = new();
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}
```

### 14.5 Shutdown Events

```csharp
/// <summary>
/// Published when graceful shutdown is initiated.
/// </summary>
public class ShutdownInitiatedEvent : INotification
{
    public string Reason { get; set; }
    public TimeSpan GracePeriodDuration { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public Guid ShutdownId { get; set; } = Guid.NewGuid();
}

/// <summary>
/// Published when shutdown transitions between phases.
/// </summary>
public class ShutdownPhaseChangedEvent : INotification
{
    public Guid ShutdownId { get; set; }
    public ShutdownState FromPhase { get; set; }
    public ShutdownState ToPhase { get; set; }
    public int InFlightRequests { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Published when graceful shutdown completes.
/// </summary>
public class ShutdownCompletedEvent : INotification
{
    public Guid ShutdownId { get; set; }
    public string Reason { get; set; }
    public int TotalRequestsProcessed { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public bool WasForced { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
```

### 14.6 Retry Events

```csharp
/// <summary>
/// Published when an operation is retried.
/// </summary>
public class RetryAttemptedEvent : INotification
{
    public string PolicyName { get; set; }
    public int AttemptNumber { get; set; }
    public int MaxAttempts { get; set; }
    public Exception? LastException { get; set; }
    public TimeSpan WaitTime { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Published when retry attempts are exhausted.
/// </summary>
public class RetryExhaustedEvent : INotification
{
    public string PolicyName { get; set; }
    public int TotalAttempts { get; set; }
    public Exception FinalException { get; set; }
    public TimeSpan TotalTime { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
```

---

## 15. IMPLEMENTATION ROADMAP

### Phase 1: Core Infrastructure (Weeks 1-2)
- [ ] Implement ICircuitBreaker and factory
- [ ] Create PostgreSQL persistence layer
- [ ] Implement basic state management
- [ ] Set up MediatR event publishing
- [ ] Unit tests for circuit breaker

### Phase 2: Fallback System (Weeks 3-4)
- [ ] Implement IFallbackStrategyEngine
- [ ] Create 3 concrete strategies
- [ ] Fallback chain execution
- [ ] Integration tests
- [ ] Performance benchmarks

### Phase 3: Retry Policies (Week 5)
- [ ] Implement IRetryPolicy and factory
- [ ] Backoff strategies
- [ ] Dead-letter queue
- [ ] Metrics collection
- [ ] Integration tests

### Phase 4: Health Tracking (Week 6)
- [ ] Implement IDependencyHealthTracker
- [ ] Health check scheduler
- [ ] Aggregation engine
- [ ] Dashboard components
- [ ] Historical storage

### Phase 5: Degradation Management (Week 7)
- [ ] Implement IPartialFunctionalityManager
- [ ] Degradation level logic
- [ ] Feature availability mapping
- [ ] UI indicators
- [ ] API middleware

### Phase 6: Graceful Shutdown (Week 8)
- [ ] Implement IGracefulShutdownManager
- [ ] Shutdown phases
- [ ] Request tracking
- [ ] Data flush procedures
- [ ] Integration tests

### Phase 7: Testing & Polish (Week 9)
- [ ] Chaos engineering tests
- [ ] Load testing
- [ ] Documentation
- [ ] Performance optimization
- [ ] Security review

---

## 16. ACCEPTANCE CRITERIA SUMMARY

All 60 acceptance criteria across 6 sub-parts must be satisfied:
- v0.19.2a: 10/10 criteria
- v0.19.2b: 10/10 criteria
- v0.19.2c: 10/10 criteria
- v0.19.2d: 10/10 criteria
- v0.19.2e: 10/10 criteria
- v0.19.2f: 10/10 criteria

**Success Metrics**:
- Code coverage > 85%
- All acceptance criteria satisfied
- Performance targets met
- Chaos tests passing
- Zero data loss in shutdown scenarios
- Documentation complete

---

## 17. CONCLUSION

v0.19.2-RES (Graceful Degradation) represents a fundamental shift in Lexichord's reliability posture. By implementing enterprise-grade resilience patterns, the platform will continue operating gracefully even when critical components fail.

**Key Outcomes**:
- 99.9% uptime SLA achievable
- Zero-downtime graceful shutdowns
- Transparent degradation to users
- Automated recovery mechanisms
- Enterprise-ready observability

**Next Steps**:
1. Architecture review with team
2. Detailed design for each sub-part
3. Sprint planning and assignment
4. Development and testing
5. Production deployment with monitoring

---

**Document Version**: 1.0
**Last Updated**: 2026-02-01
**Status**: Ready for Development Sprint Planning
