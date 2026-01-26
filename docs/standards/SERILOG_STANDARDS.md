# Lexichord Serilog Logging Standards

**Version:** 1.0.0
**Last Updated:** 2026-01-26
**Framework:** Serilog 4.x / .NET 9

---

## 1. Configuration

### 1.1 Bootstrap Configuration

```csharp
// Program.cs - Early startup logging
public static class Program
{
    public static void Main(string[] args)
    {
        // Bootstrap logger for startup errors
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File("logs/lexichord-startup.log")
            .CreateBootstrapLogger();

        try
        {
            Log.Information("Starting Lexichord application");
            CreateHostBuilder(args).Build().Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
```

### 1.2 Full Configuration

```csharp
// Extensions/SerilogExtensions.cs
public static class SerilogExtensions
{
    public static IHostBuilder UseLexichordSerilog(this IHostBuilder builder)
    {
        return builder.UseSerilog((context, services, configuration) =>
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var logPath = Path.Combine(appDataPath, "Lexichord", "Logs");

            configuration
                // Minimum Levels
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .MinimumLevel.Override("Avalonia", LogEventLevel.Warning)

                // Enrichers
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .Enrich.WithProcessId()
                .Enrich.WithProperty("Application", "Lexichord")
                .Enrich.WithProperty("Version", Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0")

                // Console Sink (Development)
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} <{SourceContext}>{NewLine}{Exception}",
                    theme: AnsiConsoleTheme.Code)

                // File Sink (Rolling)
                .WriteTo.File(
                    path: Path.Combine(logPath, "lexichord-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    fileSizeLimitBytes: 10_000_000, // 10 MB
                    rollOnFileSizeLimit: true,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")

                // Error-only File
                .WriteTo.File(
                    path: Path.Combine(logPath, "lexichord-errors-.log"),
                    restrictedToMinimumLevel: LogEventLevel.Error,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 90,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Properties:j}{NewLine}{Exception}")

                // Debug Sink (Conditional)
                .WriteTo.Conditional(
                    evt => context.HostingEnvironment.IsDevelopment(),
                    wt => wt.Debug(outputTemplate: "[{Level:u3}] {Message:lj}{NewLine}"));
        });
    }
}
```

### 1.3 appsettings.json Configuration

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning",
        "Lexichord.Modules": "Debug"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "theme": "Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme::Code, Serilog.Sinks.Console"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"],
    "Properties": {
      "Application": "Lexichord"
    }
  }
}
```

---

## 2. Log Levels

### 2.1 Level Definitions

| Level | Code | Usage | Example |
|-------|------|-------|---------|
| **Verbose** | `LogVerbose` | Extremely detailed debugging | Loop iterations, variable states |
| **Debug** | `LogDebug` | Developer diagnostics | Method entry/exit, state changes |
| **Information** | `LogInformation` | Normal application flow | User actions, requests completed |
| **Warning** | `LogWarning` | Unexpected but recoverable | Missing optional config, retries |
| **Error** | `LogError` | Failures that don't crash | API call failed, file not found |
| **Fatal** | `LogFatal` | Application crash | Unhandled exception, startup failure |

### 2.2 Level Guidelines

```csharp
// VERBOSE - Detailed tracing (rarely enabled in production)
logger.LogVerbose("Processing character {Index} of {Total}: '{Char}'", i, text.Length, c);

// DEBUG - Development/troubleshooting
logger.LogDebug("Entering {Method} with parameters: {@Parameters}", nameof(AnalyzeAsync), new { documentId, options });
logger.LogDebug("Cache miss for key {Key}, fetching from database", cacheKey);

// INFORMATION - Normal operation flow
logger.LogInformation("Document {DocumentId} opened by user {UserId}", documentId, userId);
logger.LogInformation("Style analysis completed in {Duration}ms with score {Score}", elapsed, score);

// WARNING - Potential issues, recoverable
logger.LogWarning("Rate limit approaching for {Provider}: {Current}/{Max} requests", provider, current, max);
logger.LogWarning("Configuration {Key} not found, using default value {Default}", key, defaultValue);

// ERROR - Failures (non-crash)
logger.LogError(ex, "Failed to save document {DocumentId}", documentId);
logger.LogError("API call to {Endpoint} failed with status {StatusCode}", endpoint, statusCode);

// FATAL - Application cannot continue
logger.LogFatal(ex, "Database connection failed during startup");
logger.LogFatal("License validation failed: {Reason}", reason);
```

---

## 3. Structured Logging

### 3.1 Message Templates

```csharp
// ✅ CORRECT: Use named placeholders (structured)
logger.LogInformation(
    "Document {DocumentId} analyzed by {UserId} with score {Score}",
    documentId,
    userId,
    score);

// ❌ WRONG: String interpolation (loses structure)
logger.LogInformation($"Document {documentId} analyzed by {userId} with score {score}");

// ❌ WRONG: String concatenation
logger.LogInformation("Document " + documentId + " analyzed");

// ❌ WRONG: String.Format
logger.LogInformation(string.Format("Document {0} analyzed", documentId));
```

### 3.2 Property Naming

```csharp
// ✅ Use PascalCase for property names
logger.LogInformation("Request {RequestId} processed in {DurationMs}ms", requestId, duration);

// ✅ Use consistent names across the application
// Good: DocumentId, UserId, SessionId, RequestId
// Bad: docId, user_id, session-id, reqID

// ✅ Use @ prefix for object destructuring
logger.LogInformation("User profile updated: {@Profile}", userProfile);

// ✅ Use $ prefix for string representation
logger.LogDebug("Processing object: {$Object}", complexObject);
```

### 3.3 Complex Objects

```csharp
// ✅ Destructure objects for full property capture
logger.LogInformation("Document saved: {@Document}", new
{
    document.Id,
    document.Title,
    document.WordCount,
    document.LastModified
});

// ✅ Use selective destructuring for large objects
logger.LogDebug("Analysis result: {@Result}", new
{
    result.Score,
    ViolationCount = result.Violations.Count,
    result.Duration
});

// ❌ AVOID: Logging entire large objects
logger.LogDebug("Full document: {@Document}", hugeDocument); // Too much data
```

---

## 4. Contextual Logging

### 4.1 Log Scopes

```csharp
public class DocumentService(ILogger<DocumentService> logger)
{
    public async Task<Document> ProcessAsync(Guid documentId, Guid userId)
    {
        // Create a scope with contextual properties
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["DocumentId"] = documentId,
            ["UserId"] = userId,
            ["Operation"] = "ProcessDocument"
        }))
        {
            logger.LogInformation("Starting document processing");

            var document = await LoadAsync(documentId);
            logger.LogDebug("Document loaded, {WordCount} words", document.WordCount);

            var result = await AnalyzeAsync(document);
            logger.LogInformation("Analysis complete, score: {Score}", result.Score);

            return document;
        }
        // Scope automatically disposed, context removed
    }
}
```

### 4.2 Correlation IDs

```csharp
// Middleware to add correlation ID
public class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ILogger<CorrelationIdMiddleware> logger)
    {
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            context.Response.Headers["X-Correlation-ID"] = correlationId;
            await next(context);
        }
    }
}

// Or for desktop apps
public class OperationContext : IDisposable
{
    private readonly IDisposable _logContext;

    public string OperationId { get; }

    public OperationContext(string? operationId = null)
    {
        OperationId = operationId ?? Guid.NewGuid().ToString("N")[..8];
        _logContext = LogContext.PushProperty("OperationId", OperationId);
    }

    public void Dispose() => _logContext.Dispose();
}

// Usage
using var operation = new OperationContext();
logger.LogInformation("Starting batch operation");
// All logs within this scope include OperationId
```

---

## 5. Module-Specific Logging

### 5.1 Source Context Prefixes

```csharp
// Each module uses a consistent prefix
namespace Lexichord.Modules.Style.Services;

public class TuningService(ILogger<TuningService> logger)
{
    // Logs appear as: [STY] TuningService - Message
    // SourceContext: Lexichord.Modules.Style.Services.TuningService
}

// Recommended module prefixes:
// [HOST] - Lexichord.Host
// [STY]  - Lexichord.Modules.Style
// [MEM]  - Lexichord.Modules.Memory
// [AGT]  - Lexichord.Modules.Agents
// [GIT]  - Lexichord.Modules.Git
```

### 5.2 Event IDs

```csharp
// Define event IDs for significant operations
public static class LogEvents
{
    // Host Events (1000-1999)
    public static readonly EventId ApplicationStarted = new(1000, "ApplicationStarted");
    public static readonly EventId ApplicationStopped = new(1001, "ApplicationStopped");
    public static readonly EventId ModuleLoaded = new(1100, "ModuleLoaded");
    public static readonly EventId ModuleLoadFailed = new(1101, "ModuleLoadFailed");

    // Style Events (2000-2999)
    public static readonly EventId AnalysisStarted = new(2000, "AnalysisStarted");
    public static readonly EventId AnalysisCompleted = new(2001, "AnalysisCompleted");
    public static readonly EventId ViolationDetected = new(2100, "ViolationDetected");
    public static readonly EventId ViolationFixed = new(2101, "ViolationFixed");

    // Agent Events (3000-3999)
    public static readonly EventId AgentSessionStarted = new(3000, "AgentSessionStarted");
    public static readonly EventId AgentSessionEnded = new(3001, "AgentSessionEnded");
    public static readonly EventId LlmRequestSent = new(3100, "LlmRequestSent");
    public static readonly EventId LlmResponseReceived = new(3101, "LlmResponseReceived");

    // Memory/RAG Events (4000-4999)
    public static readonly EventId IndexingStarted = new(4000, "IndexingStarted");
    public static readonly EventId IndexingCompleted = new(4001, "IndexingCompleted");
    public static readonly EventId SearchExecuted = new(4100, "SearchExecuted");
}

// Usage
logger.LogInformation(LogEvents.AnalysisCompleted,
    "Document {DocumentId} analysis completed with score {Score}",
    documentId, score);
```

---

## 6. Performance Logging

### 6.1 Timing Operations

```csharp
public class PerformanceLogger(ILogger logger)
{
    public IDisposable TimeOperation(string operationName, params object[] args)
    {
        return new TimedOperation(logger, operationName, args);
    }

    private class TimedOperation : IDisposable
    {
        private readonly ILogger _logger;
        private readonly string _operationName;
        private readonly object[] _args;
        private readonly Stopwatch _stopwatch;

        public TimedOperation(ILogger logger, string operationName, object[] args)
        {
            _logger = logger;
            _operationName = operationName;
            _args = args;
            _stopwatch = Stopwatch.StartNew();

            _logger.LogDebug("Starting: " + operationName, args);
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            var allArgs = _args.Append(_stopwatch.ElapsedMilliseconds).ToArray();
            _logger.LogInformation(_operationName + " completed in {DurationMs}ms", allArgs);
        }
    }
}

// Usage
using (performanceLogger.TimeOperation("Document {DocumentId} analysis", documentId))
{
    await AnalyzeAsync(document);
}
// Output: "Document abc123 analysis completed in 245ms"
```

### 6.2 Metrics Logging

```csharp
// Log metrics at regular intervals
public class MetricsLogger(ILogger<MetricsLogger> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var metrics = GatherMetrics();

            logger.LogInformation(
                "Application metrics: " +
                "Memory={MemoryMb}MB, " +
                "GC0={GcGen0}, GC1={GcGen1}, GC2={GcGen2}, " +
                "ThreadCount={ThreadCount}",
                metrics.MemoryMb,
                metrics.GcGen0,
                metrics.GcGen1,
                metrics.GcGen2,
                metrics.ThreadCount);

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }

    private static ApplicationMetrics GatherMetrics() => new(
        MemoryMb: GC.GetTotalMemory(false) / 1_000_000,
        GcGen0: GC.CollectionCount(0),
        GcGen1: GC.CollectionCount(1),
        GcGen2: GC.CollectionCount(2),
        ThreadCount: ThreadPool.ThreadCount
    );

    private record ApplicationMetrics(long MemoryMb, int GcGen0, int GcGen1, int GcGen2, int ThreadCount);
}
```

---

## 7. Exception Logging

### 7.1 Exception Handling

```csharp
// ✅ CORRECT: Pass exception as first parameter
try
{
    await ProcessAsync();
}
catch (DocumentNotFoundException ex)
{
    logger.LogWarning(ex, "Document {DocumentId} not found", ex.DocumentId);
    throw;
}
catch (StyleAnalysisException ex)
{
    logger.LogError(ex, "Style analysis failed for document {DocumentId}", documentId);
    return AnalysisResult.Failed(ex.Message);
}
catch (Exception ex)
{
    logger.LogError(ex, "Unexpected error processing document {DocumentId}", documentId);
    throw;
}

// ❌ WRONG: Exception in message template
logger.LogError("Error: {Exception}", ex); // Loses stack trace

// ❌ WRONG: Exception.Message only
logger.LogError("Error: {Message}", ex.Message); // Loses important info
```

### 7.2 Global Exception Handling

```csharp
// App.axaml.cs
public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        // Global exception handlers
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            var exception = e.ExceptionObject as Exception;
            Log.Fatal(exception, "Unhandled domain exception");
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Log.Error(e.Exception, "Unobserved task exception");
            e.SetObserved();
        };

        RxApp.DefaultExceptionHandler = Observer.Create<Exception>(ex =>
        {
            Log.Error(ex, "Unhandled RxUI exception");
        });
    }
}
```

---

## 8. Sensitive Data

### 8.1 Data Masking

```csharp
// Create a custom destructuring policy
public class SensitiveDataPolicy : IDestructuringPolicy
{
    private static readonly HashSet<string> SensitiveProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "Password", "ApiKey", "Secret", "Token", "Credential",
        "CreditCard", "SSN", "Email", "PhoneNumber"
    };

    public bool TryDestructure(object value, ILogEventPropertyValueFactory factory, out LogEventPropertyValue? result)
    {
        result = null;

        if (value is null) return false;

        var type = value.GetType();
        var properties = new List<LogEventProperty>();

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var propValue = prop.GetValue(value);
            var logValue = SensitiveProperties.Contains(prop.Name)
                ? factory.CreatePropertyValue("***REDACTED***")
                : factory.CreatePropertyValue(propValue, destructureObjects: true);

            properties.Add(new LogEventProperty(prop.Name, logValue));
        }

        result = new StructureValue(properties);
        return true;
    }
}

// Registration
Log.Logger = new LoggerConfiguration()
    .Destructure.With<SensitiveDataPolicy>()
    .CreateLogger();
```

### 8.2 PII Guidelines

```csharp
// ✅ Log identifiers, not values
logger.LogInformation("User {UserId} updated email", userId);

// ❌ NEVER log actual PII
logger.LogInformation("User email changed to {Email}", email); // FORBIDDEN

// ✅ Log counts, not details
logger.LogInformation("Found {Count} matching users", users.Count);

// ❌ NEVER log full records with PII
logger.LogInformation("Users: {@Users}", users); // FORBIDDEN

// ✅ Use audit logging for PII access
auditLogger.LogAccess(
    userId: currentUser.Id,
    resource: "UserProfile",
    resourceId: targetUserId,
    action: "View");
```

---

## 9. Testing

### 9.1 Test Logger

```csharp
// Using Microsoft.Extensions.Logging for testability
public class TestLogger<T> : ILogger<T>
{
    public List<LogEntry> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Entries.Add(new LogEntry(
            logLevel,
            eventId,
            formatter(state, exception),
            exception));
    }

    public record LogEntry(LogLevel Level, EventId EventId, string Message, Exception? Exception);
}

// Usage in tests
[Test]
public async Task AnalyzeAsync_LogsStartAndCompletion()
{
    // Arrange
    var testLogger = new TestLogger<StyleEngine>();
    var sut = new StyleEngine(testLogger, mockRepo.Object);

    // Act
    await sut.AnalyzeAsync(document);

    // Assert
    Assert.That(testLogger.Entries, Has.Count.GreaterThanOrEqualTo(2));
    Assert.That(testLogger.Entries[0].Message, Does.Contain("Starting"));
    Assert.That(testLogger.Entries[^1].Message, Does.Contain("completed"));
}
```

### 9.2 Serilog Test Sink

```csharp
// Using Serilog.Sinks.InMemory
[TestFixture]
public class LoggingTests
{
    private InMemorySink _sink;

    [SetUp]
    public void SetUp()
    {
        _sink = new InMemorySink();
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Sink(_sink)
            .CreateLogger();
    }

    [Test]
    public void Operation_LogsExpectedEvents()
    {
        // Act
        PerformOperation();

        // Assert
        _sink.LogEvents.Should().ContainSingle(e =>
            e.Level == LogEventLevel.Information &&
            e.MessageTemplate.Text.Contains("Operation completed"));
    }
}
```

---

## 10. Production Guidelines

### 10.1 Log Rotation

```csharp
// Configure appropriate retention
.WriteTo.File(
    path: "logs/lexichord-.log",
    rollingInterval: RollingInterval.Day,
    retainedFileCountLimit: 30,        // Keep 30 days
    fileSizeLimitBytes: 50_000_000,    // 50 MB max per file
    rollOnFileSizeLimit: true,
    shared: true,                       // Allow multiple processes
    flushToDiskInterval: TimeSpan.FromSeconds(1))
```

### 10.2 Async Logging

```csharp
// Use async wrapper for production
.WriteTo.Async(a => a.File(
    path: "logs/lexichord-.log",
    rollingInterval: RollingInterval.Day,
    buffered: true,
    flushToDiskInterval: TimeSpan.FromSeconds(1)))
```

### 10.3 Minimum Level by Environment

```csharp
var configuration = new LoggerConfiguration();

if (environment.IsDevelopment())
{
    configuration.MinimumLevel.Debug();
}
else if (environment.IsStaging())
{
    configuration.MinimumLevel.Information();
}
else // Production
{
    configuration.MinimumLevel.Warning()
        .MinimumLevel.Override("Lexichord", LogEventLevel.Information);
}
```

---

## Appendix: Quick Reference

```
╔═══════════════════════════════════════════════════════════════╗
║  SERILOG QUICK REFERENCE                                      ║
╠═══════════════════════════════════════════════════════════════╣
║                                                               ║
║  LOG LEVELS                                                   ║
║  Verbose  → Detailed tracing (development only)               ║
║  Debug    → Developer diagnostics                             ║
║  Info     → Normal application flow                           ║
║  Warning  → Potential issues (recoverable)                    ║
║  Error    → Failures (non-crash)                              ║
║  Fatal    → Application crash                                 ║
║                                                               ║
║  MESSAGE TEMPLATES                                            ║
║  ✓ Use named placeholders: "User {UserId} logged in"          ║
║  ✗ Avoid interpolation: $"User {userId} logged in"            ║
║  ✓ Use @ for objects: "Profile: {@Profile}"                   ║
║  ✓ Use $ for ToString: "Object: {$Complex}"                   ║
║                                                               ║
║  EXCEPTIONS                                                   ║
║  ✓ Pass as first param: LogError(ex, "Message")               ║
║  ✗ Don't: LogError("Error: {Ex}", ex)                         ║
║                                                               ║
║  SCOPES                                                       ║
║  using (logger.BeginScope(new { DocumentId = id }))           ║
║  {                                                            ║
║      // All logs include DocumentId                           ║
║  }                                                            ║
║                                                               ║
║  SENSITIVE DATA                                               ║
║  ✗ Never log: passwords, API keys, emails, SSNs               ║
║  ✓ Log identifiers: UserId, DocumentId, RequestId             ║
║                                                               ║
╚═══════════════════════════════════════════════════════════════╝
```
