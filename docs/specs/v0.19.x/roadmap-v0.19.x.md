# Lexichord Error Handling & Resilience Roadmap (v0.19.1 - v0.19.5)

In v0.18.x, we delivered **Security & Sandboxing** — comprehensive protection mechanisms for AI operations. In v0.19.x, we introduce **Error Handling & Resilience** — robust error management, graceful degradation, recovery mechanisms, and diagnostic capabilities that ensure Lexichord remains stable and reliable under all conditions.

**Architectural Note:** This version introduces `Lexichord.Resilience` module with structured error handling, automatic recovery, health monitoring, and comprehensive diagnostics. Reliability becomes a first-class concern with proactive failure prevention and graceful degradation strategies.

**Total Sub-Parts:** 38 distinct implementation steps across 5 versions.
**Total Estimated Hours:** 278 hours (~7.0 person-months)

---

## Version Overview

| Version | Codename | Focus | Est. Hours |
|:--------|:---------|:------|:-----------|
| v0.19.1-RES | Exception Framework | Structured errors, error codes, error hierarchies, user-friendly messages | 52 |
| v0.19.2-RES | Graceful Degradation | Fallback strategies, partial functionality, circuit breakers | 56 |
| v0.19.3-RES | Recovery & Repair | Crash recovery, data integrity, automatic repair, state restoration | 58 |
| v0.19.4-RES | Diagnostics & Logging | Structured logging, telemetry, performance tracking, debug modes | 54 |
| v0.19.5-RES | Health Monitoring | Health checks, status dashboards, proactive alerts, system status | 58 |

---

## Resilience Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      Error Handling & Resilience System                      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                    Health Monitoring Layer (v0.19.5)                   │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌────────────┐ │ │
│  │  │   Health     │  │    Status    │  │  Proactive   │  │  Resource  │ │ │
│  │  │   Checks     │  │  Dashboard   │  │   Alerts     │  │  Monitor   │ │ │
│  │  └──────────────┘  └──────────────┘  └──────────────┘  └────────────┘ │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                    │                                         │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                   Diagnostics & Logging (v0.19.4)                      │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐                  │ │
│  │  │  Structured  │  │  Telemetry   │  │    Debug     │                  │ │
│  │  │   Logging    │  │   Engine     │  │    Modes     │                  │ │
│  │  └──────────────┘  └──────────────┘  └──────────────┘                  │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                    │                                         │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                    Recovery & Repair (v0.19.3)                         │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐                  │ │
│  │  │    Crash     │  │    Data      │  │    State     │                  │ │
│  │  │   Recovery   │  │  Integrity   │  │ Restoration  │                  │ │
│  │  └──────────────┘  └──────────────┘  └──────────────┘                  │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                    │                                         │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                   Graceful Degradation (v0.19.2)                       │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌────────────┐ │ │
│  │  │   Fallback   │  │   Circuit    │  │   Partial    │  │   Retry    │ │ │
│  │  │  Strategies  │  │   Breakers   │  │   Function   │  │   Policies │ │ │
│  │  └──────────────┘  └──────────────┘  └──────────────┘  └────────────┘ │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                    │                                         │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                    Exception Framework (v0.19.1)                       │ │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌────────────┐ │ │
│  │  │  Structured  │  │    Error     │  │    User      │  │   Error    │ │ │
│  │  │   Errors     │  │    Codes     │  │  Messages    │  │  Handlers  │ │ │
│  │  └──────────────┘  └──────────────┘  └──────────────┘  └────────────┘ │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## v0.19.1-RES: Exception Framework

**Goal:** Establish a comprehensive exception framework with structured errors, consistent error codes, hierarchical error types, and user-friendly messages.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.19.1a | Error Code Registry | 10 |
| v0.19.1b | Exception Hierarchy | 10 |
| v0.19.1c | User-Friendly Messages | 8 |
| v0.19.1d | Error Context & Metadata | 8 |
| v0.19.1e | Global Exception Handlers | 8 |
| v0.19.1f | Error Presentation UI | 8 |

### Key Interfaces

```csharp
/// <summary>
/// Base exception for all Lexichord errors.
/// </summary>
public abstract class LexichordException : Exception
{
    public ErrorCode Code { get; }
    public ErrorSeverity Severity { get; }
    public IReadOnlyDictionary<string, object> Context { get; }
    public string UserMessage { get; }
    public string TechnicalDetails { get; }
    public IReadOnlyList<ErrorAction> SuggestedActions { get; }
    public string? CorrelationId { get; }
    public DateTime Timestamp { get; }
    public bool IsRetryable { get; }
    public TimeSpan? RetryAfter { get; }

    protected LexichordException(
        ErrorCode code,
        string message,
        string userMessage,
        Exception? inner = null,
        IReadOnlyDictionary<string, object>? context = null)
        : base(message, inner)
    {
        Code = code;
        UserMessage = userMessage;
        Context = context ?? new Dictionary<string, object>();
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// Structured error codes with categories.
/// </summary>
public readonly record struct ErrorCode
{
    public ErrorCategory Category { get; init; }
    public int Number { get; init; }
    public string FullCode => $"LCS-{Category.ToCode()}-{Number:D4}";

    public static ErrorCode Parse(string code) { /* ... */ }
}

public enum ErrorCategory
{
    // Core (1xxx)
    General = 1,
    Configuration = 2,
    Initialization = 3,

    // Data (2xxx)
    Database = 20,
    Storage = 21,
    Serialization = 22,
    Validation = 23,

    // AI/Agents (3xxx)
    Agent = 30,
    Orchestration = 31,
    Model = 32,
    Inference = 33,

    // Documents (4xxx)
    Document = 40,
    Editor = 41,
    Formatting = 42,

    // Security (5xxx)
    Permission = 50,
    Authentication = 51,
    Authorization = 52,
    Sandbox = 53,

    // Network (6xxx)
    Network = 60,
    Api = 61,
    Sync = 62,

    // Integration (7xxx)
    Plugin = 70,
    Marketplace = 71,
    ExternalService = 72,

    // System (8xxx)
    Resource = 80,
    Memory = 81,
    Disk = 82,
    Process = 83,

    // User (9xxx)
    UserInput = 90,
    UserAction = 91,
    UserLimit = 92
}

public enum ErrorSeverity
{
    Info,        // Informational, no action needed
    Warning,     // Potential issue, operation continued
    Error,       // Operation failed, recoverable
    Critical,    // Operation failed, may affect system
    Fatal        // System-level failure, requires restart
}

/// <summary>
/// Suggested action for error recovery.
/// </summary>
public record ErrorAction
{
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public ErrorActionType Type { get; init; }
    public Func<Task>? Action { get; init; }
    public string? NavigateTo { get; init; }
    public bool IsDefault { get; init; }
}

public enum ErrorActionType
{
    Retry,
    Dismiss,
    Navigate,
    Configure,
    Report,
    Restart,
    Custom
}

/// <summary>
/// Manages error code registry and lookup.
/// </summary>
public interface IErrorCodeRegistry
{
    /// <summary>
    /// Get error definition by code.
    /// </summary>
    Task<ErrorDefinition?> GetDefinitionAsync(
        ErrorCode code,
        CancellationToken ct = default);

    /// <summary>
    /// Get localized user message for error.
    /// </summary>
    Task<string> GetUserMessageAsync(
        ErrorCode code,
        string locale,
        IReadOnlyDictionary<string, object>? substitutions = null,
        CancellationToken ct = default);

    /// <summary>
    /// Register custom error code.
    /// </summary>
    Task RegisterAsync(
        ErrorDefinition definition,
        CancellationToken ct = default);

    /// <summary>
    /// Get all errors in a category.
    /// </summary>
    Task<IReadOnlyList<ErrorDefinition>> GetByCategoryAsync(
        ErrorCategory category,
        CancellationToken ct = default);
}

public record ErrorDefinition
{
    public ErrorCode Code { get; init; }
    public string Name { get; init; } = "";
    public string DefaultMessage { get; init; } = "";
    public string DefaultUserMessage { get; init; } = "";
    public ErrorSeverity DefaultSeverity { get; init; }
    public bool IsRetryable { get; init; }
    public IReadOnlyList<ErrorAction> DefaultActions { get; init; } = [];
    public string? DocumentationUrl { get; init; }
    public IReadOnlyDictionary<string, string> LocalizedMessages { get; init; }
}

/// <summary>
/// Global exception handling and recovery.
/// </summary>
public interface IGlobalExceptionHandler
{
    /// <summary>
    /// Handle an exception.
    /// </summary>
    Task<ExceptionHandleResult> HandleAsync(
        Exception exception,
        ExceptionContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Register exception handler for specific type.
    /// </summary>
    void RegisterHandler<TException>(
        IExceptionHandler<TException> handler)
        where TException : Exception;

    /// <summary>
    /// Configure global handling behavior.
    /// </summary>
    void Configure(ExceptionHandlerOptions options);

    /// <summary>
    /// Observable stream of handled exceptions.
    /// </summary>
    IObservable<HandledException> ExceptionStream { get; }
}

public record ExceptionHandleResult
{
    public bool Handled { get; init; }
    public bool ShouldRethrow { get; init; }
    public bool ShouldLog { get; init; }
    public bool ShouldNotifyUser { get; init; }
    public UserNotification? Notification { get; init; }
    public Exception? TransformedException { get; init; }
    public IReadOnlyList<ErrorAction> Actions { get; init; } = [];
}

public record ExceptionContext
{
    public string Operation { get; init; } = "";
    public string? Component { get; init; }
    public AgentId? AgentId { get; init; }
    public UserId? UserId { get; init; }
    public string? CorrelationId { get; init; }
    public IReadOnlyDictionary<string, object> Properties { get; init; }
}

/// <summary>
/// Presents errors to users in a friendly way.
/// </summary>
public interface IErrorPresenter
{
    /// <summary>
    /// Show error to user.
    /// </summary>
    Task ShowErrorAsync(
        LexichordException exception,
        ErrorPresentationOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Show error with actions.
    /// </summary>
    Task<ErrorAction?> ShowErrorWithActionsAsync(
        LexichordException exception,
        CancellationToken ct = default);

    /// <summary>
    /// Show transient error notification.
    /// </summary>
    Task ShowToastAsync(
        string message,
        ErrorSeverity severity,
        TimeSpan duration,
        CancellationToken ct = default);

    /// <summary>
    /// Queue error for later presentation.
    /// </summary>
    Task QueueErrorAsync(
        LexichordException exception,
        CancellationToken ct = default);
}

public record ErrorPresentationOptions
{
    public bool ShowTechnicalDetails { get; init; }
    public bool AllowCopyDetails { get; init; } = true;
    public bool AllowReport { get; init; } = true;
    public bool ShowCorrelationId { get; init; } = true;
    public bool AutoDismiss { get; init; }
    public TimeSpan? DismissAfter { get; init; }
}
```

### Error Code Catalog

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          Error Code Catalog                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  GENERAL (LCS-GEN-xxxx)                                                     │
│  ├── LCS-GEN-0001  UnexpectedError         Unexpected error occurred        │
│  ├── LCS-GEN-0002  OperationCancelled      Operation was cancelled          │
│  ├── LCS-GEN-0003  Timeout                 Operation timed out              │
│  └── LCS-GEN-0004  NotImplemented          Feature not implemented          │
│                                                                              │
│  DATABASE (LCS-DB-xxxx)                                                      │
│  ├── LCS-DB-2001   ConnectionFailed        Database connection failed       │
│  ├── LCS-DB-2002   QueryFailed             Database query failed            │
│  ├── LCS-DB-2003   TransactionFailed       Transaction commit failed        │
│  ├── LCS-DB-2004   MigrationFailed         Database migration failed        │
│  └── LCS-DB-2005   ConstraintViolation     Database constraint violated     │
│                                                                              │
│  AGENT (LCS-AGT-xxxx)                                                        │
│  ├── LCS-AGT-3001  AgentNotFound           Agent not found                  │
│  ├── LCS-AGT-3002  AgentTimeout            Agent response timeout           │
│  ├── LCS-AGT-3003  AgentCrashed            Agent process crashed            │
│  ├── LCS-AGT-3004  OrchestrationFailed     Orchestration failed             │
│  └── LCS-AGT-3005  ModelLoadFailed         Failed to load AI model          │
│                                                                              │
│  DOCUMENT (LCS-DOC-xxxx)                                                     │
│  ├── LCS-DOC-4001  DocumentNotFound        Document not found               │
│  ├── LCS-DOC-4002  DocumentLocked          Document is locked               │
│  ├── LCS-DOC-4003  DocumentCorrupted       Document file corrupted          │
│  ├── LCS-DOC-4004  FormatNotSupported      Document format not supported    │
│  └── LCS-DOC-4005  SaveFailed              Failed to save document          │
│                                                                              │
│  PERMISSION (LCS-PRM-xxxx)                                                   │
│  ├── LCS-PRM-5001  PermissionDenied        Permission denied                │
│  ├── LCS-PRM-5002  PermissionExpired       Permission has expired           │
│  ├── LCS-PRM-5003  CommandBlocked          Command blocked by security      │
│  └── LCS-PRM-5004  RateLimited             Too many requests                │
│                                                                              │
│  NETWORK (LCS-NET-xxxx)                                                      │
│  ├── LCS-NET-6001  NetworkUnavailable      Network is unavailable           │
│  ├── LCS-NET-6002  HostUnreachable         Host unreachable                 │
│  ├── LCS-NET-6003  SslError                SSL/TLS error                    │
│  ├── LCS-NET-6004  ApiError                API returned error               │
│  └── LCS-NET-6005  SyncConflict            Sync conflict detected           │
│                                                                              │
│  RESOURCE (LCS-RES-xxxx)                                                     │
│  ├── LCS-RES-8001  OutOfMemory             Out of memory                    │
│  ├── LCS-RES-8002  DiskFull                Disk space exhausted             │
│  ├── LCS-RES-8003  FileNotFound            File not found                   │
│  ├── LCS-RES-8004  FileAccessDenied        File access denied               │
│  └── LCS-RES-8005  ResourceExhausted       Resource limit reached           │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## v0.19.2-RES: Graceful Degradation

**Goal:** Implement fallback strategies, partial functionality modes, circuit breakers, and retry policies that ensure Lexichord continues operating even when components fail.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.19.2a | Fallback Strategy Engine | 12 |
| v0.19.2b | Circuit Breaker Pattern | 10 |
| v0.19.2c | Retry Policies | 8 |
| v0.19.2d | Partial Functionality Mode | 10 |
| v0.19.2e | Dependency Health Tracking | 8 |
| v0.19.2f | Graceful Shutdown | 8 |

### Key Interfaces

```csharp
/// <summary>
/// Manages fallback strategies for degraded operation.
/// </summary>
public interface IFallbackStrategyEngine
{
    /// <summary>
    /// Execute with fallback.
    /// </summary>
    Task<T> ExecuteWithFallbackAsync<T>(
        Func<CancellationToken, Task<T>> primary,
        FallbackChain<T> fallbacks,
        CancellationToken ct = default);

    /// <summary>
    /// Register fallback for a service.
    /// </summary>
    void RegisterFallback<TService>(
        FallbackStrategy<TService> fallback);

    /// <summary>
    /// Get current fallback status.
    /// </summary>
    Task<FallbackStatus> GetStatusAsync(
        string serviceId,
        CancellationToken ct = default);

    /// <summary>
    /// Observable stream of fallback events.
    /// </summary>
    IObservable<FallbackEvent> FallbackEvents { get; }
}

public record FallbackChain<T>
{
    public IReadOnlyList<FallbackOption<T>> Options { get; init; } = [];
    public T? DefaultValue { get; init; }
    public bool AllowDefault { get; init; }
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);
}

public record FallbackOption<T>
{
    public string Name { get; init; } = "";
    public Func<Exception, CancellationToken, Task<T>> Fallback { get; init; }
    public Func<Exception, bool>? Condition { get; init; }
    public int Priority { get; init; }
    public bool IsPartialResult { get; init; }
}

public enum FallbackStatus
{
    Primary,           // Using primary service
    Fallback,          // Using fallback
    Degraded,          // Partial functionality
    Unavailable        // No fallback available
}

/// <summary>
/// Circuit breaker for preventing cascade failures.
/// </summary>
public interface ICircuitBreaker
{
    /// <summary>
    /// Current circuit state.
    /// </summary>
    CircuitState State { get; }

    /// <summary>
    /// Execute through circuit breaker.
    /// </summary>
    Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> action,
        CancellationToken ct = default);

    /// <summary>
    /// Record a failure.
    /// </summary>
    void RecordFailure(Exception exception);

    /// <summary>
    /// Record a success.
    /// </summary>
    void RecordSuccess();

    /// <summary>
    /// Manually trip the circuit.
    /// </summary>
    void Trip(string reason);

    /// <summary>
    /// Reset the circuit.
    /// </summary>
    void Reset();

    /// <summary>
    /// Observable stream of state changes.
    /// </summary>
    IObservable<CircuitStateChange> StateChanges { get; }
}

public enum CircuitState
{
    Closed,      // Normal operation
    Open,        // Failures blocked
    HalfOpen     // Testing if recovered
}

public record CircuitBreakerOptions
{
    public int FailureThreshold { get; init; } = 5;
    public TimeSpan FailureWindow { get; init; } = TimeSpan.FromMinutes(1);
    public TimeSpan OpenDuration { get; init; } = TimeSpan.FromSeconds(30);
    public int HalfOpenSuccesses { get; init; } = 2;
    public Func<Exception, bool>? ShouldHandle { get; init; }
}

/// <summary>
/// Factory for creating circuit breakers.
/// </summary>
public interface ICircuitBreakerFactory
{
    /// <summary>
    /// Get or create circuit breaker for a service.
    /// </summary>
    ICircuitBreaker GetOrCreate(
        string serviceId,
        CircuitBreakerOptions? options = null);

    /// <summary>
    /// Get all circuit breaker states.
    /// </summary>
    Task<IReadOnlyDictionary<string, CircuitState>> GetAllStatesAsync(
        CancellationToken ct = default);
}

/// <summary>
/// Configurable retry policies.
/// </summary>
public interface IRetryPolicy
{
    /// <summary>
    /// Execute with retry.
    /// </summary>
    Task<T> ExecuteAsync<T>(
        Func<RetryContext, CancellationToken, Task<T>> action,
        CancellationToken ct = default);

    /// <summary>
    /// Get current policy configuration.
    /// </summary>
    RetryPolicyConfiguration Configuration { get; }
}

public record RetryPolicyConfiguration
{
    public int MaxRetries { get; init; } = 3;
    public TimeSpan InitialDelay { get; init; } = TimeSpan.FromSeconds(1);
    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromSeconds(30);
    public double BackoffMultiplier { get; init; } = 2.0;
    public bool UseJitter { get; init; } = true;
    public Func<Exception, bool>? RetryableExceptions { get; init; }
    public Func<int, Exception, TimeSpan>? DelayCalculator { get; init; }
}

public record RetryContext
{
    public int AttemptNumber { get; init; }
    public Exception? LastException { get; init; }
    public TimeSpan TotalElapsed { get; init; }
    public DateTime StartTime { get; init; }
}

/// <summary>
/// Manages partial functionality when components fail.
/// </summary>
public interface IPartialFunctionalityManager
{
    /// <summary>
    /// Get current functionality status.
    /// </summary>
    Task<FunctionalityStatus> GetStatusAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Check if a feature is available.
    /// </summary>
    Task<bool> IsFeatureAvailableAsync(
        FeatureId feature,
        CancellationToken ct = default);

    /// <summary>
    /// Get degraded features.
    /// </summary>
    Task<IReadOnlyList<DegradedFeature>> GetDegradedFeaturesAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Register feature dependency.
    /// </summary>
    void RegisterDependency(
        FeatureId feature,
        IReadOnlyList<string> dependencies);

    /// <summary>
    /// Observable stream of functionality changes.
    /// </summary>
    IObservable<FunctionalityChange> FunctionalityChanges { get; }
}

public record FunctionalityStatus
{
    public OverallHealth Health { get; init; }
    public int AvailableFeatures { get; init; }
    public int DegradedFeatures { get; init; }
    public int UnavailableFeatures { get; init; }
    public IReadOnlyList<string> CriticalIssues { get; init; } = [];
}

public record DegradedFeature
{
    public FeatureId Feature { get; init; }
    public string Name { get; init; } = "";
    public DegradationLevel Level { get; init; }
    public string Reason { get; init; } = "";
    public IReadOnlyList<string> AffectedCapabilities { get; init; } = [];
    public DateTime Since { get; init; }
    public DateTime? ExpectedRecovery { get; init; }
}

public enum DegradationLevel
{
    Full,           // Fully functional
    Reduced,        // Some features limited
    Minimal,        // Basic functionality only
    Unavailable     // Feature unavailable
}

/// <summary>
/// Manages graceful shutdown procedures.
/// </summary>
public interface IGracefulShutdownManager
{
    /// <summary>
    /// Initiate graceful shutdown.
    /// </summary>
    Task ShutdownAsync(
        ShutdownOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Register shutdown handler.
    /// </summary>
    void RegisterHandler(
        IShutdownHandler handler,
        int priority = 0);

    /// <summary>
    /// Get shutdown progress.
    /// </summary>
    Task<ShutdownProgress> GetProgressAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Check if shutdown is in progress.
    /// </summary>
    bool IsShuttingDown { get; }
}

public interface IShutdownHandler
{
    string Name { get; }
    Task OnShutdownAsync(ShutdownContext context, CancellationToken ct);
}

public record ShutdownOptions
{
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);
    public bool SaveState { get; init; } = true;
    public bool WaitForOperations { get; init; } = true;
    public bool NotifyUser { get; init; } = true;
}
```

### Fallback Strategies

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         Fallback Strategy Matrix                             │
├────────────────────┬────────────────────┬───────────────────────────────────┤
│ Component          │ Primary            │ Fallback Strategy                 │
├────────────────────┼────────────────────┼───────────────────────────────────┤
│ Cloud AI           │ Claude API         │ 1. Local LLM                      │
│                    │                    │ 2. Cached responses               │
│                    │                    │ 3. Offline mode (basic features)  │
├────────────────────┼────────────────────┼───────────────────────────────────┤
│ Database           │ PostgreSQL         │ 1. SQLite local cache             │
│                    │                    │ 2. In-memory store                │
│                    │                    │ 3. File-based persistence         │
├────────────────────┼────────────────────┼───────────────────────────────────┤
│ Sync Service       │ Real-time sync     │ 1. Periodic sync                  │
│                    │                    │ 2. Manual sync                    │
│                    │                    │ 3. Local-only mode                │
├────────────────────┼────────────────────┼───────────────────────────────────┤
│ Plugin Store       │ Live marketplace   │ 1. Cached catalog                 │
│                    │                    │ 2. Local plugins only             │
│                    │                    │ 3. Disabled                       │
├────────────────────┼────────────────────┼───────────────────────────────────┤
│ Model Download     │ Hugging Face       │ 1. Mirror sources                 │
│                    │                    │ 2. Local model library            │
│                    │                    │ 3. Bundled models                 │
├────────────────────┼────────────────────┼───────────────────────────────────┤
│ Auto-save          │ Continuous         │ 1. Periodic (30s)                 │
│                    │                    │ 2. On-blur save                   │
│                    │                    │ 3. Manual save only               │
└────────────────────┴────────────────────┴───────────────────────────────────┘
```

---

## v0.19.3-RES: Recovery & Repair

**Goal:** Implement crash recovery, data integrity validation, automatic repair mechanisms, and state restoration to ensure Lexichord can recover from failures.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.19.3a | Crash Detection & Recovery | 12 |
| v0.19.3b | Data Integrity Validation | 10 |
| v0.19.3c | Automatic Repair Engine | 12 |
| v0.19.3d | State Snapshot & Restore | 10 |
| v0.19.3e | Transaction Journal | 8 |
| v0.19.3f | Corruption Detection | 6 |

### Key Interfaces

```csharp
/// <summary>
/// Detects crashes and manages recovery.
/// </summary>
public interface ICrashRecoveryManager
{
    /// <summary>
    /// Check if recovery is needed on startup.
    /// </summary>
    Task<RecoveryNeeded> CheckRecoveryNeededAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Perform crash recovery.
    /// </summary>
    Task<RecoveryResult> RecoverAsync(
        RecoveryOptions options,
        IProgress<RecoveryProgress>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Record clean shutdown.
    /// </summary>
    Task RecordCleanShutdownAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Get recovery history.
    /// </summary>
    Task<IReadOnlyList<RecoveryRecord>> GetHistoryAsync(
        CancellationToken ct = default);
}

public record RecoveryNeeded
{
    public bool NeedsRecovery { get; init; }
    public CrashType? CrashType { get; init; }
    public DateTime? CrashTime { get; init; }
    public IReadOnlyList<string> AffectedComponents { get; init; } = [];
    public IReadOnlyList<RecoverableItem> RecoverableItems { get; init; } = [];
}

public enum CrashType
{
    ApplicationCrash,
    SystemCrash,
    PowerFailure,
    ProcessKilled,
    OutOfMemory,
    Unknown
}

public record RecoverableItem
{
    public RecoverableItemType Type { get; init; }
    public string Identifier { get; init; } = "";
    public string Description { get; init; } = "";
    public DateTime LastModified { get; init; }
    public RecoveryConfidence Confidence { get; init; }
}

public enum RecoverableItemType
{
    UnsavedDocument,
    EditorState,
    AgentSession,
    Transaction,
    Configuration
}

public enum RecoveryConfidence
{
    High,       // Full recovery expected
    Medium,     // Partial recovery possible
    Low,        // Recovery uncertain
    None        // Unlikely to recover
}

public record RecoveryResult
{
    public bool Success { get; init; }
    public IReadOnlyList<RecoveredItem> RecoveredItems { get; init; } = [];
    public IReadOnlyList<LostItem> LostItems { get; init; } = [];
    public TimeSpan Duration { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

/// <summary>
/// Validates data integrity across the system.
/// </summary>
public interface IDataIntegrityValidator
{
    /// <summary>
    /// Validate all data stores.
    /// </summary>
    Task<IntegrityReport> ValidateAllAsync(
        IntegrityValidationOptions options,
        IProgress<ValidationProgress>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Validate specific data store.
    /// </summary>
    Task<StoreIntegrityReport> ValidateStoreAsync(
        string storeId,
        CancellationToken ct = default);

    /// <summary>
    /// Validate a specific document.
    /// </summary>
    Task<DocumentIntegrityReport> ValidateDocumentAsync(
        DocumentId documentId,
        CancellationToken ct = default);

    /// <summary>
    /// Schedule periodic validation.
    /// </summary>
    Task ScheduleValidationAsync(
        ValidationSchedule schedule,
        CancellationToken ct = default);
}

public record IntegrityReport
{
    public DateTime Timestamp { get; init; }
    public OverallIntegrity Status { get; init; }
    public int TotalChecks { get; init; }
    public int PassedChecks { get; init; }
    public int FailedChecks { get; init; }
    public int Warnings { get; init; }
    public IReadOnlyList<IntegrityIssue> Issues { get; init; } = [];
    public TimeSpan Duration { get; init; }
}

public record IntegrityIssue
{
    public string IssueId { get; init; } = "";
    public IntegritySeverity Severity { get; init; }
    public string Component { get; init; } = "";
    public string Description { get; init; } = "";
    public bool IsRepairable { get; init; }
    public string? RepairAction { get; init; }
}

public enum IntegritySeverity
{
    Info,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Automatically repairs detected issues.
/// </summary>
public interface IAutoRepairEngine
{
    /// <summary>
    /// Attempt to repair issues.
    /// </summary>
    Task<RepairResult> RepairAsync(
        IReadOnlyList<IntegrityIssue> issues,
        RepairOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Get available repair actions.
    /// </summary>
    Task<IReadOnlyList<RepairAction>> GetAvailableActionsAsync(
        IntegrityIssue issue,
        CancellationToken ct = default);

    /// <summary>
    /// Preview repair without applying.
    /// </summary>
    Task<RepairPreview> PreviewRepairAsync(
        RepairAction action,
        CancellationToken ct = default);

    /// <summary>
    /// Rollback a repair.
    /// </summary>
    Task RollbackRepairAsync(
        RepairId repairId,
        CancellationToken ct = default);
}

public record RepairResult
{
    public int TotalIssues { get; init; }
    public int Repaired { get; init; }
    public int Failed { get; init; }
    public int Skipped { get; init; }
    public IReadOnlyList<RepairOutcome> Outcomes { get; init; } = [];
    public TimeSpan Duration { get; init; }
}

public record RepairAction
{
    public RepairActionId Id { get; init; }
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public RepairRisk Risk { get; init; }
    public bool RequiresBackup { get; init; }
    public bool IsAutomatic { get; init; }
}

public enum RepairRisk
{
    None,       // Safe to apply
    Low,        // Minor risk
    Medium,     // May affect data
    High        // Potential data loss
}

/// <summary>
/// Manages state snapshots for recovery.
/// </summary>
public interface IStateSnapshotManager
{
    /// <summary>
    /// Create a state snapshot.
    /// </summary>
    Task<SnapshotId> CreateSnapshotAsync(
        SnapshotOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Restore from snapshot.
    /// </summary>
    Task<RestoreResult> RestoreAsync(
        SnapshotId snapshotId,
        RestoreOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// List available snapshots.
    /// </summary>
    Task<IReadOnlyList<Snapshot>> GetSnapshotsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Configure automatic snapshots.
    /// </summary>
    Task ConfigureAutoSnapshotAsync(
        AutoSnapshotConfiguration config,
        CancellationToken ct = default);

    /// <summary>
    /// Delete old snapshots.
    /// </summary>
    Task PruneSnapshotsAsync(
        SnapshotRetentionPolicy policy,
        CancellationToken ct = default);
}

public record Snapshot
{
    public SnapshotId Id { get; init; }
    public string Name { get; init; } = "";
    public DateTime CreatedAt { get; init; }
    public SnapshotType Type { get; init; }
    public long SizeBytes { get; init; }
    public IReadOnlyList<string> IncludedComponents { get; init; } = [];
    public string? Description { get; init; }
}

public enum SnapshotType
{
    Full,           // Complete state
    Incremental,    // Changes since last
    Automatic,      // System-created
    Manual,         // User-created
    PreUpdate       // Before version update
}

/// <summary>
/// Transaction journal for recovery.
/// </summary>
public interface ITransactionJournal
{
    /// <summary>
    /// Begin a journaled transaction.
    /// </summary>
    Task<JournalTransaction> BeginAsync(
        string operation,
        CancellationToken ct = default);

    /// <summary>
    /// Commit transaction.
    /// </summary>
    Task CommitAsync(
        JournalTransaction transaction,
        CancellationToken ct = default);

    /// <summary>
    /// Rollback transaction.
    /// </summary>
    Task RollbackAsync(
        JournalTransaction transaction,
        CancellationToken ct = default);

    /// <summary>
    /// Get uncommitted transactions.
    /// </summary>
    Task<IReadOnlyList<JournalTransaction>> GetUncommittedAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Replay journal entries.
    /// </summary>
    Task ReplayAsync(
        JournalReplayOptions options,
        CancellationToken ct = default);
}
```

---

## v0.19.4-RES: Diagnostics & Logging

**Goal:** Implement structured logging, telemetry collection, performance tracking, and debug modes for comprehensive system observability.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.19.4a | Structured Logging Engine | 10 |
| v0.19.4b | Telemetry Collection | 10 |
| v0.19.4c | Performance Tracking | 10 |
| v0.19.4d | Debug Mode System | 8 |
| v0.19.4e | Log Viewer & Search | 10 |
| v0.19.4f | Diagnostic Export | 6 |

### Key Interfaces

```csharp
/// <summary>
/// Structured logging with rich context.
/// </summary>
public interface IStructuredLogger
{
    /// <summary>
    /// Log with structured data.
    /// </summary>
    void Log(
        LogLevel level,
        string message,
        IReadOnlyDictionary<string, object>? properties = null,
        Exception? exception = null);

    /// <summary>
    /// Create a scoped logger with context.
    /// </summary>
    IDisposable BeginScope(
        string scopeName,
        IReadOnlyDictionary<string, object>? properties = null);

    /// <summary>
    /// Create a child logger.
    /// </summary>
    IStructuredLogger CreateChild(string category);

    /// <summary>
    /// Add persistent property to all logs.
    /// </summary>
    void AddProperty(string key, object value);
}

public enum LogLevel
{
    Trace,
    Debug,
    Information,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Log entry with full context.
/// </summary>
public record LogEntry
{
    public LogEntryId Id { get; init; }
    public DateTime Timestamp { get; init; }
    public LogLevel Level { get; init; }
    public string Category { get; init; } = "";
    public string Message { get; init; } = "";
    public string? MessageTemplate { get; init; }
    public IReadOnlyDictionary<string, object> Properties { get; init; }
    public ExceptionInfo? Exception { get; init; }
    public string? CorrelationId { get; init; }
    public string? SpanId { get; init; }
    public IReadOnlyList<string> Scopes { get; init; } = [];
}

/// <summary>
/// Collects and manages telemetry data.
/// </summary>
public interface ITelemetryCollector
{
    /// <summary>
    /// Track an event.
    /// </summary>
    void TrackEvent(
        string eventName,
        IReadOnlyDictionary<string, string>? properties = null,
        IReadOnlyDictionary<string, double>? metrics = null);

    /// <summary>
    /// Track a metric.
    /// </summary>
    void TrackMetric(
        string name,
        double value,
        IReadOnlyDictionary<string, string>? dimensions = null);

    /// <summary>
    /// Track a dependency call.
    /// </summary>
    void TrackDependency(
        string type,
        string name,
        string target,
        TimeSpan duration,
        bool success);

    /// <summary>
    /// Track a request.
    /// </summary>
    void TrackRequest(
        string name,
        DateTime startTime,
        TimeSpan duration,
        string responseCode,
        bool success);

    /// <summary>
    /// Flush pending telemetry.
    /// </summary>
    Task FlushAsync(CancellationToken ct = default);

    /// <summary>
    /// Configure telemetry collection.
    /// </summary>
    void Configure(TelemetryConfiguration config);
}

public record TelemetryConfiguration
{
    public bool Enabled { get; init; } = true;
    public bool CollectPerformanceMetrics { get; init; } = true;
    public bool CollectExceptions { get; init; } = true;
    public bool CollectDependencies { get; init; } = true;
    public double SamplingPercentage { get; init; } = 100;
    public IReadOnlyList<string> ExcludedCategories { get; init; } = [];
    public TimeSpan FlushInterval { get; init; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// Tracks performance metrics across the application.
/// </summary>
public interface IPerformanceTracker
{
    /// <summary>
    /// Start a performance measurement.
    /// </summary>
    IPerformanceScope StartMeasurement(
        string operation,
        IReadOnlyDictionary<string, string>? tags = null);

    /// <summary>
    /// Record a timing measurement.
    /// </summary>
    void RecordTiming(
        string operation,
        TimeSpan duration,
        IReadOnlyDictionary<string, string>? tags = null);

    /// <summary>
    /// Record a counter increment.
    /// </summary>
    void IncrementCounter(
        string name,
        long value = 1,
        IReadOnlyDictionary<string, string>? tags = null);

    /// <summary>
    /// Record a gauge value.
    /// </summary>
    void RecordGauge(
        string name,
        double value,
        IReadOnlyDictionary<string, string>? tags = null);

    /// <summary>
    /// Get performance summary.
    /// </summary>
    Task<PerformanceSummary> GetSummaryAsync(
        TimeSpan period,
        CancellationToken ct = default);
}

public interface IPerformanceScope : IDisposable
{
    void AddTag(string key, string value);
    void RecordCheckpoint(string name);
    void SetSuccess(bool success);
}

public record PerformanceSummary
{
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public IReadOnlyList<OperationMetrics> Operations { get; init; } = [];
    public IReadOnlyList<CounterMetrics> Counters { get; init; } = [];
    public ResourceMetrics Resources { get; init; }
}

public record OperationMetrics
{
    public string Name { get; init; } = "";
    public long Count { get; init; }
    public TimeSpan MinDuration { get; init; }
    public TimeSpan MaxDuration { get; init; }
    public TimeSpan AvgDuration { get; init; }
    public TimeSpan P95Duration { get; init; }
    public TimeSpan P99Duration { get; init; }
    public double SuccessRate { get; init; }
}

/// <summary>
/// Manages debug modes and diagnostics.
/// </summary>
public interface IDebugModeManager
{
    /// <summary>
    /// Enable debug mode.
    /// </summary>
    Task EnableAsync(
        DebugModeOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Disable debug mode.
    /// </summary>
    Task DisableAsync(CancellationToken ct = default);

    /// <summary>
    /// Check if debug mode is active.
    /// </summary>
    bool IsDebugModeActive { get; }

    /// <summary>
    /// Get current debug configuration.
    /// </summary>
    DebugModeOptions? CurrentConfiguration { get; }

    /// <summary>
    /// Enable component-specific debug.
    /// </summary>
    Task EnableComponentDebugAsync(
        string component,
        ComponentDebugOptions options,
        CancellationToken ct = default);
}

public record DebugModeOptions
{
    public bool VerboseLogging { get; init; } = true;
    public bool PerformanceTracing { get; init; } = true;
    public bool NetworkInspection { get; init; }
    public bool MemoryProfiling { get; init; }
    public bool SqlQueryLogging { get; init; }
    public bool AgentInternals { get; init; }
    public TimeSpan? AutoDisableAfter { get; init; }
    public IReadOnlyList<string>? EnabledComponents { get; init; }
}

/// <summary>
/// In-app log viewer with search.
/// </summary>
public interface ILogViewer
{
    /// <summary>
    /// Query logs.
    /// </summary>
    Task<LogQueryResult> QueryAsync(
        LogQuery query,
        CancellationToken ct = default);

    /// <summary>
    /// Get real-time log stream.
    /// </summary>
    IObservable<LogEntry> GetLiveStream(
        LogFilter? filter = null);

    /// <summary>
    /// Export logs.
    /// </summary>
    Task<Stream> ExportAsync(
        LogExportOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Get log statistics.
    /// </summary>
    Task<LogStatistics> GetStatisticsAsync(
        TimeSpan period,
        CancellationToken ct = default);
}

public record LogQuery
{
    public DateTime? StartTime { get; init; }
    public DateTime? EndTime { get; init; }
    public IReadOnlyList<LogLevel>? Levels { get; init; }
    public IReadOnlyList<string>? Categories { get; init; }
    public string? SearchText { get; init; }
    public string? CorrelationId { get; init; }
    public bool IncludeExceptions { get; init; }
    public int MaxResults { get; init; } = 1000;
    public int Skip { get; init; }
}

/// <summary>
/// Exports diagnostic information for support.
/// </summary>
public interface IDiagnosticExporter
{
    /// <summary>
    /// Generate diagnostic bundle.
    /// </summary>
    Task<DiagnosticBundle> GenerateBundleAsync(
        DiagnosticBundleOptions options,
        IProgress<ExportProgress>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Export to file.
    /// </summary>
    Task<string> ExportToFileAsync(
        DiagnosticBundle bundle,
        string outputPath,
        CancellationToken ct = default);

    /// <summary>
    /// Submit to support.
    /// </summary>
    Task<SupportTicketId> SubmitToSupportAsync(
        DiagnosticBundle bundle,
        SupportSubmissionOptions options,
        CancellationToken ct = default);
}

public record DiagnosticBundleOptions
{
    public bool IncludeLogs { get; init; } = true;
    public TimeSpan LogPeriod { get; init; } = TimeSpan.FromHours(24);
    public bool IncludeConfiguration { get; init; } = true;
    public bool IncludeSystemInfo { get; init; } = true;
    public bool IncludePerformanceData { get; init; } = true;
    public bool IncludeErrorHistory { get; init; } = true;
    public bool SanitizeSensitiveData { get; init; } = true;
    public IReadOnlyList<string>? ExcludeCategories { get; init; }
}

public record DiagnosticBundle
{
    public DiagnosticBundleId Id { get; init; }
    public DateTime GeneratedAt { get; init; }
    public SystemInfo SystemInfo { get; init; }
    public IReadOnlyList<LogEntry> Logs { get; init; } = [];
    public IReadOnlyDictionary<string, object> Configuration { get; init; }
    public PerformanceSummary? PerformanceData { get; init; }
    public IReadOnlyList<ErrorSummary> ErrorHistory { get; init; } = [];
    public long SizeBytes { get; init; }
}
```

---

## v0.19.5-RES: Health Monitoring

**Goal:** Implement comprehensive health checks, status dashboards, proactive alerts, and system status monitoring for production reliability.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.19.5a | Health Check Framework | 12 |
| v0.19.5b | Status Dashboard UI | 10 |
| v0.19.5c | Proactive Alerting | 10 |
| v0.19.5d | Resource Monitoring | 10 |
| v0.19.5e | Dependency Health | 8 |
| v0.19.5f | Health API & Endpoints | 8 |

### Key Interfaces

```csharp
/// <summary>
/// Manages health checks across the system.
/// </summary>
public interface IHealthCheckManager
{
    /// <summary>
    /// Run all health checks.
    /// </summary>
    Task<HealthReport> CheckAllAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Run specific health check.
    /// </summary>
    Task<HealthCheckResult> CheckAsync(
        string checkName,
        CancellationToken ct = default);

    /// <summary>
    /// Register a health check.
    /// </summary>
    void RegisterCheck(
        string name,
        IHealthCheck check,
        HealthCheckOptions options);

    /// <summary>
    /// Get registered health checks.
    /// </summary>
    Task<IReadOnlyList<HealthCheckRegistration>> GetRegistrationsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Observable stream of health changes.
    /// </summary>
    IObservable<HealthChange> HealthChanges { get; }
}

public interface IHealthCheck
{
    string Name { get; }
    string Description { get; }
    HealthCheckCategory Category { get; }

    Task<HealthCheckResult> CheckAsync(
        HealthCheckContext context,
        CancellationToken ct = default);
}

public record HealthCheckResult
{
    public HealthStatus Status { get; init; }
    public string Description { get; init; } = "";
    public TimeSpan Duration { get; init; }
    public IReadOnlyDictionary<string, object> Data { get; init; }
    public Exception? Exception { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
}

public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy
}

public enum HealthCheckCategory
{
    Core,
    Database,
    Network,
    AI,
    Storage,
    External,
    Resource
}

public record HealthReport
{
    public DateTime Timestamp { get; init; }
    public HealthStatus OverallStatus { get; init; }
    public TimeSpan TotalDuration { get; init; }
    public IReadOnlyDictionary<string, HealthCheckResult> Entries { get; init; }
    public IReadOnlyDictionary<HealthCheckCategory, HealthStatus> CategoryStatus { get; init; }
}

/// <summary>
/// Status dashboard for health visualization.
/// </summary>
public interface IStatusDashboard
{
    /// <summary>
    /// Get dashboard data.
    /// </summary>
    Task<DashboardData> GetDataAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Get historical status.
    /// </summary>
    Task<IReadOnlyList<HistoricalStatus>> GetHistoryAsync(
        TimeSpan period,
        CancellationToken ct = default);

    /// <summary>
    /// Subscribe to dashboard updates.
    /// </summary>
    IObservable<DashboardData> Updates { get; }

    /// <summary>
    /// Get incident history.
    /// </summary>
    Task<IReadOnlyList<Incident>> GetIncidentsAsync(
        IncidentQuery query,
        CancellationToken ct = default);
}

public record DashboardData
{
    public DateTime Timestamp { get; init; }
    public OverallHealth OverallHealth { get; init; }
    public double UptimePercentage { get; init; }
    public TimeSpan LastIncidentAgo { get; init; }
    public IReadOnlyList<ComponentStatus> Components { get; init; } = [];
    public ResourceUsage Resources { get; init; }
    public IReadOnlyList<ActiveAlert> ActiveAlerts { get; init; } = [];
    public PerformanceMetrics Performance { get; init; }
}

public enum OverallHealth
{
    Operational,        // All systems go
    Degraded,          // Some issues
    PartialOutage,     // Significant problems
    MajorOutage,       // Critical failure
    Maintenance        // Planned maintenance
}

public record ComponentStatus
{
    public string Name { get; init; } = "";
    public HealthStatus Status { get; init; }
    public string StatusMessage { get; init; } = "";
    public DateTime LastChecked { get; init; }
    public double ResponseTimeMs { get; init; }
    public IReadOnlyList<string> SubComponents { get; init; } = [];
}

/// <summary>
/// Proactive alerting based on health changes.
/// </summary>
public interface IProactiveAlertManager
{
    /// <summary>
    /// Configure alert rule.
    /// </summary>
    Task<AlertRuleId> ConfigureRuleAsync(
        HealthAlertRule rule,
        CancellationToken ct = default);

    /// <summary>
    /// Get active alerts.
    /// </summary>
    Task<IReadOnlyList<ActiveAlert>> GetActiveAlertsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Acknowledge an alert.
    /// </summary>
    Task AcknowledgeAsync(
        AlertId alertId,
        string acknowledgedBy,
        CancellationToken ct = default);

    /// <summary>
    /// Configure notification channels.
    /// </summary>
    Task ConfigureChannelAsync(
        NotificationChannel channel,
        CancellationToken ct = default);

    /// <summary>
    /// Subscribe to alerts.
    /// </summary>
    IObservable<ActiveAlert> Alerts { get; }
}

public record HealthAlertRule
{
    public AlertRuleId Id { get; init; }
    public string Name { get; init; } = "";
    public string Condition { get; init; } = "";
    public AlertSeverity Severity { get; init; }
    public TimeSpan EvaluationInterval { get; init; }
    public TimeSpan? AlertAfterDuration { get; init; }
    public IReadOnlyList<string> NotificationChannels { get; init; } = [];
    public bool IsEnabled { get; init; } = true;
}

public record ActiveAlert
{
    public AlertId Id { get; init; }
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public AlertSeverity Severity { get; init; }
    public DateTime TriggeredAt { get; init; }
    public AlertRuleId RuleId { get; init; }
    public IReadOnlyDictionary<string, object> Context { get; init; }
    public bool IsAcknowledged { get; init; }
}

/// <summary>
/// Monitors system resources.
/// </summary>
public interface IResourceMonitor
{
    /// <summary>
    /// Get current resource usage.
    /// </summary>
    Task<ResourceUsage> GetCurrentUsageAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Get resource history.
    /// </summary>
    Task<IReadOnlyList<ResourceUsage>> GetHistoryAsync(
        TimeSpan period,
        TimeSpan resolution,
        CancellationToken ct = default);

    /// <summary>
    /// Set resource thresholds.
    /// </summary>
    Task SetThresholdsAsync(
        ResourceThresholds thresholds,
        CancellationToken ct = default);

    /// <summary>
    /// Subscribe to resource updates.
    /// </summary>
    IObservable<ResourceUsage> UsageStream { get; }

    /// <summary>
    /// Subscribe to threshold violations.
    /// </summary>
    IObservable<ThresholdViolation> ThresholdViolations { get; }
}

public record ResourceUsage
{
    public DateTime Timestamp { get; init; }
    public CpuUsage Cpu { get; init; }
    public MemoryUsage Memory { get; init; }
    public DiskUsage Disk { get; init; }
    public NetworkUsage Network { get; init; }
    public GpuUsage? Gpu { get; init; }
}

public record CpuUsage
{
    public double PercentUsed { get; init; }
    public int ProcessorCount { get; init; }
    public double ProcessCpuPercent { get; init; }
    public IReadOnlyList<double> CoreUsage { get; init; } = [];
}

public record MemoryUsage
{
    public long TotalBytes { get; init; }
    public long UsedBytes { get; init; }
    public long AvailableBytes { get; init; }
    public long ProcessUsedBytes { get; init; }
    public long ProcessPrivateBytes { get; init; }
    public double PercentUsed => (double)UsedBytes / TotalBytes * 100;
}

public record DiskUsage
{
    public IReadOnlyList<DriveUsage> Drives { get; init; } = [];
    public long TotalReadBytes { get; init; }
    public long TotalWriteBytes { get; init; }
    public double ReadBytesPerSecond { get; init; }
    public double WriteBytesPerSecond { get; init; }
}

public record ResourceThresholds
{
    public double CpuWarningPercent { get; init; } = 70;
    public double CpuCriticalPercent { get; init; } = 90;
    public double MemoryWarningPercent { get; init; } = 75;
    public double MemoryCriticalPercent { get; init; } = 90;
    public double DiskWarningPercent { get; init; } = 80;
    public double DiskCriticalPercent { get; init; } = 95;
}
```

---

## PostgreSQL Schema

```sql
-- Error Tracking
CREATE TABLE resilience.error_log (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    error_code VARCHAR(20) NOT NULL,
    category VARCHAR(30) NOT NULL,
    severity VARCHAR(20) NOT NULL,
    message TEXT NOT NULL,
    user_message TEXT,
    technical_details TEXT,
    exception_type VARCHAR(255),
    exception_message TEXT,
    stack_trace TEXT,
    context JSONB DEFAULT '{}',
    correlation_id VARCHAR(100),
    agent_id UUID,
    user_id UUID,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
) PARTITION BY RANGE (created_at);

CREATE INDEX idx_error_log_code ON resilience.error_log(error_code);
CREATE INDEX idx_error_log_severity ON resilience.error_log(severity);
CREATE INDEX idx_error_log_time ON resilience.error_log(created_at DESC);
CREATE INDEX idx_error_log_correlation ON resilience.error_log(correlation_id);

-- Health Check History
CREATE TABLE resilience.health_check_history (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    check_name VARCHAR(100) NOT NULL,
    category VARCHAR(30) NOT NULL,
    status VARCHAR(20) NOT NULL,
    duration_ms INTEGER NOT NULL,
    description TEXT,
    data JSONB DEFAULT '{}',
    exception_info JSONB,
    checked_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
) PARTITION BY RANGE (checked_at);

CREATE INDEX idx_health_history_name ON resilience.health_check_history(check_name);
CREATE INDEX idx_health_history_status ON resilience.health_check_history(status);
CREATE INDEX idx_health_history_time ON resilience.health_check_history(checked_at DESC);

-- Crash Recovery Records
CREATE TABLE resilience.crash_recovery_records (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    crash_type VARCHAR(30) NOT NULL,
    crash_time TIMESTAMPTZ NOT NULL,
    detection_time TIMESTAMPTZ NOT NULL,
    recovery_time TIMESTAMPTZ,
    affected_components TEXT[] DEFAULT '{}',
    recovered_items JSONB DEFAULT '[]',
    lost_items JSONB DEFAULT '[]',
    recovery_success BOOLEAN,
    recovery_notes TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_crash_recovery_time ON resilience.crash_recovery_records(crash_time DESC);

-- State Snapshots
CREATE TABLE resilience.state_snapshots (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    snapshot_type VARCHAR(30) NOT NULL,
    included_components TEXT[] DEFAULT '{}',
    size_bytes BIGINT NOT NULL,
    storage_path TEXT NOT NULL,
    checksum VARCHAR(64) NOT NULL,
    description TEXT,
    created_by UUID,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMPTZ,
    is_deleted BOOLEAN DEFAULT FALSE
);

CREATE INDEX idx_snapshots_type ON resilience.state_snapshots(snapshot_type);
CREATE INDEX idx_snapshots_created ON resilience.state_snapshots(created_at DESC);

-- Structured Logs
CREATE TABLE resilience.structured_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    level VARCHAR(20) NOT NULL,
    category VARCHAR(100) NOT NULL,
    message TEXT NOT NULL,
    message_template TEXT,
    properties JSONB DEFAULT '{}',
    exception_info JSONB,
    correlation_id VARCHAR(100),
    span_id VARCHAR(50),
    scopes TEXT[] DEFAULT '{}',
    source_context VARCHAR(255),
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW()
) PARTITION BY RANGE (timestamp);

CREATE INDEX idx_structured_logs_level ON resilience.structured_logs(level);
CREATE INDEX idx_structured_logs_category ON resilience.structured_logs(category);
CREATE INDEX idx_structured_logs_time ON resilience.structured_logs(timestamp DESC);
CREATE INDEX idx_structured_logs_correlation ON resilience.structured_logs(correlation_id);
CREATE INDEX idx_structured_logs_properties ON resilience.structured_logs USING GIN (properties);

-- Performance Metrics
CREATE TABLE resilience.performance_metrics (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    metric_type VARCHAR(30) NOT NULL,
    name VARCHAR(100) NOT NULL,
    value DOUBLE PRECISION NOT NULL,
    tags JSONB DEFAULT '{}',
    recorded_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
) PARTITION BY RANGE (recorded_at);

CREATE INDEX idx_perf_metrics_type ON resilience.performance_metrics(metric_type);
CREATE INDEX idx_perf_metrics_name ON resilience.performance_metrics(name);
CREATE INDEX idx_perf_metrics_time ON resilience.performance_metrics(recorded_at DESC);

-- Resource Usage History
CREATE TABLE resilience.resource_usage (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    cpu_percent DOUBLE PRECISION NOT NULL,
    memory_used_bytes BIGINT NOT NULL,
    memory_total_bytes BIGINT NOT NULL,
    disk_used_bytes BIGINT NOT NULL,
    disk_total_bytes BIGINT NOT NULL,
    network_in_bytes BIGINT,
    network_out_bytes BIGINT,
    gpu_percent DOUBLE PRECISION,
    gpu_memory_bytes BIGINT,
    additional_metrics JSONB DEFAULT '{}',
    recorded_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
) PARTITION BY RANGE (recorded_at);

CREATE INDEX idx_resource_usage_time ON resilience.resource_usage(recorded_at DESC);

-- Alerts
CREATE TABLE resilience.alerts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    rule_id UUID NOT NULL,
    title VARCHAR(500) NOT NULL,
    description TEXT,
    severity VARCHAR(20) NOT NULL,
    context JSONB DEFAULT '{}',
    is_acknowledged BOOLEAN DEFAULT FALSE,
    acknowledged_by UUID,
    acknowledged_at TIMESTAMPTZ,
    resolved_at TIMESTAMPTZ,
    triggered_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_alerts_severity ON resilience.alerts(severity);
CREATE INDEX idx_alerts_acknowledged ON resilience.alerts(is_acknowledged);
CREATE INDEX idx_alerts_triggered ON resilience.alerts(triggered_at DESC);
```

---

## MediatR Events

```csharp
// Error Events
public record ErrorOccurredEvent(
    LexichordException Exception,
    ExceptionContext Context
) : INotification;

public record ErrorHandledEvent(
    LexichordException Exception,
    ExceptionHandleResult Result
) : INotification;

// Circuit Breaker Events
public record CircuitStateChangedEvent(
    string ServiceId,
    CircuitState OldState,
    CircuitState NewState,
    string? Reason
) : INotification;

// Recovery Events
public record CrashDetectedEvent(
    CrashType Type,
    DateTime CrashTime,
    IReadOnlyList<string> AffectedComponents
) : INotification;

public record RecoveryCompletedEvent(
    RecoveryResult Result
) : INotification;

public record SnapshotCreatedEvent(
    Snapshot Snapshot
) : INotification;

// Health Events
public record HealthStatusChangedEvent(
    string CheckName,
    HealthStatus OldStatus,
    HealthStatus NewStatus
) : INotification;

public record ResourceThresholdViolatedEvent(
    ThresholdViolation Violation
) : INotification;

public record AlertTriggeredEvent(
    ActiveAlert Alert
) : INotification;
```

---

## License Gating

| Feature | Core | WriterPro | Teams | Enterprise |
|:--------|:----:|:---------:|:-----:|:----------:|
| Error Messages | ✓ | ✓ | ✓ | ✓ |
| Basic Recovery | ✓ | ✓ | ✓ | ✓ |
| Auto-save Recovery | ✓ | ✓ | ✓ | ✓ |
| Snapshots | 3 | 10 | 50 | Unlimited |
| Log Retention | 7 days | 30 days | 90 days | 1 year |
| Health Dashboard | - | Basic | Full | Full |
| Custom Alerts | - | - | 10 | Unlimited |
| Performance Metrics | - | - | ✓ | ✓ |
| Diagnostic Export | Basic | Full | Full | Full |
| API Health Endpoints | - | - | - | ✓ |
| SIEM Integration | - | - | - | ✓ |

---

## Dependencies

```
v0.19.1-RES (Exception Framework)
    └── Core Platform (v0.1.x-v0.18.x)

v0.19.2-RES (Graceful Degradation)
    └── v0.19.1-RES (Exception Framework)

v0.19.3-RES (Recovery & Repair)
    ├── v0.19.1-RES (Exception Framework)
    └── v0.19.2-RES (Graceful Degradation)

v0.19.4-RES (Diagnostics & Logging)
    └── v0.19.1-RES (Exception Framework)

v0.19.5-RES (Health Monitoring)
    ├── v0.19.1-RES (Exception Framework)
    ├── v0.19.2-RES (Graceful Degradation)
    └── v0.19.4-RES (Diagnostics & Logging)
```

---

## Performance Targets

| Metric | Target | Critical Threshold |
|:-------|:-------|:-------------------|
| Error handling overhead | < 1ms | < 5ms |
| Circuit breaker check | < 0.1ms | < 1ms |
| Health check (individual) | < 500ms | < 2s |
| Health check (full) | < 5s | < 15s |
| Log write latency | < 1ms | < 5ms |
| Snapshot creation (100MB) | < 10s | < 30s |
| Recovery startup | < 30s | < 60s |

---

## Risk Assessment

| Risk | Probability | Impact | Mitigation |
|:-----|:------------|:-------|:-----------|
| Recovery data corruption | Low | Critical | Checksums, validation |
| Logging performance impact | Medium | Medium | Async logging, sampling |
| False health alarms | Medium | Low | Tunable thresholds |
| Snapshot storage bloat | Medium | Medium | Retention policies |
| Circuit breaker stuck open | Low | High | Timeout, manual reset |
| Diagnostic data exposure | Low | High | Sanitization, encryption |
