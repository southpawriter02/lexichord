# LCS-DES-003b: Serilog Pipeline

## 1. Metadata & Categorization

| Field                | Value                                | Description                                  |
| :------------------- | :----------------------------------- | :------------------------------------------- |
| **Feature ID**       | `INF-003b`                           | Infrastructure - Serilog Logging Pipeline    |
| **Feature Name**     | Serilog Pipeline                     | Structured logging with Console and File sinks |
| **Target Version**   | `v0.0.3b`                            | Second sub-part of v0.0.3                    |
| **Module Scope**     | `Lexichord.Host`                     | Primary application executable               |
| **Swimlane**         | `Infrastructure`                     | The Podium (Platform)                        |
| **License Tier**     | `Core`                               | Foundation (Required for all tiers)          |
| **Author**           | System Architect                     |                                              |
| **Status**           | **Draft**                            | Pending implementation                       |
| **Last Updated**     | 2026-01-26                           |                                              |

---

## 2. Executive Summary

### 2.1 The Requirement

The Lexichord Host currently lacks structured logging. Without proper logging:

- Production bugs are impossible to diagnose.
- Application behavior cannot be audited.
- Performance issues cannot be identified.
- Module loading failures are silent.

### 2.2 The Proposed Solution

We **SHALL** implement Serilog as the application's logging framework with:

1. **Bootstrap Logger** — Captures startup errors before full configuration loads.
2. **Console Sink** — Development-friendly colorized output.
3. **File Sink** — Rolling daily logs with retention limits.
4. **Error File Sink** — Separate file for errors with longer retention.
5. **Microsoft.Extensions.Logging Integration** — `ILogger<T>` support for DI.

---

## 3. Implementation Tasks

### Task 1.1: Install Serilog Packages

**NuGet Packages to Add:**

```xml
<PackageReference Include="Serilog" Version="4.2.0" />
<PackageReference Include="Serilog.Extensions.Logging" Version="9.0.0" />
<PackageReference Include="Serilog.Sinks.Console" Version="6.0.0" />
<PackageReference Include="Serilog.Sinks.File" Version="6.0.0" />
<PackageReference Include="Serilog.Enrichers.Thread" Version="4.0.0" />
<PackageReference Include="Serilog.Enrichers.Environment" Version="3.0.1" />
```

**Rationale:** These packages provide the complete Serilog ecosystem with structured logging, multiple sinks, and enrichers for diagnostic context.

---

### Task 1.2: Bootstrap Logger

**File:** `src/Lexichord.Host/Program.cs` (Modified)

```csharp
using Avalonia;
using Serilog;
using Serilog.Events;
using System;

namespace Lexichord.Host;

/// <summary>
/// Application entry point with bootstrap logging and exception handling.
/// </summary>
/// <remarks>
/// LOGIC: The entry point establishes a bootstrap logger before Avalonia initializes.
/// This captures any startup failures that occur before the full logging pipeline is ready.
/// The bootstrap logger is minimal (console only) and is replaced once configuration loads.
/// </remarks>
internal sealed class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>Exit code (0 = success, 1 = error).</returns>
    [STAThread]
    public static int Main(string[] args)
    {
        // LOGIC: Create bootstrap logger immediately for startup error capture
        // This minimal logger writes to console only until full configuration loads
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateBootstrapLogger();

        try
        {
            Log.Information("Starting Lexichord application");
            Log.Debug("Command line arguments: {Args}", args);

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);

            Log.Information("Lexichord application shutdown complete");
            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Lexichord application terminated unexpectedly");
            return 1;
        }
        finally
        {
            // LOGIC: Ensure all log entries are written before process exits
            Log.CloseAndFlush();
        }
    }

    /// <summary>
    /// Builds the Avalonia application configuration.
    /// </summary>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
```

---

### Task 1.3: Full Logger Configuration

**File:** `src/Lexichord.Host/Extensions/SerilogExtensions.cs`

```csharp
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using System;
using System.IO;
using System.Reflection;

namespace Lexichord.Host.Extensions;

/// <summary>
/// Extension methods for configuring Serilog logging.
/// </summary>
/// <remarks>
/// LOGIC: This class centralizes all Serilog configuration.
/// The ConfigureSerilog method replaces the bootstrap logger with the full pipeline.
/// </remarks>
public static class SerilogExtensions
{
    /// <summary>
    /// Configures the full Serilog pipeline from configuration.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <remarks>
    /// LOGIC: This method replaces the bootstrap logger with the full configuration.
    /// It sets up multiple sinks with appropriate output templates and rolling policies:
    /// - Console: Colorized output for development
    /// - File: Daily rolling logs with 30-day retention
    /// - Error File: Error-only logs with 90-day retention
    /// </remarks>
    public static void ConfigureSerilog(IConfiguration configuration)
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var logPath = Path.Combine(appDataPath, "Lexichord", "Logs");
        Directory.CreateDirectory(logPath);

        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";
        var debugMode = configuration.GetValue<bool>("Lexichord:DebugMode");
        var minimumLevel = debugMode ? LogEventLevel.Debug : LogEventLevel.Information;

        Log.Logger = new LoggerConfiguration()
            // Minimum Levels
            .MinimumLevel.Is(minimumLevel)
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .MinimumLevel.Override("Avalonia", LogEventLevel.Warning)

            // Enrichers - Add context to every log entry
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("Application", "Lexichord")
            .Enrich.WithProperty("Version", version)

            // Console Sink (Development)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} <{SourceContext}>{NewLine}{Exception}",
                theme: AnsiConsoleTheme.Code,
                restrictedToMinimumLevel: debugMode ? LogEventLevel.Debug : LogEventLevel.Information)

            // File Sink (Rolling daily)
            .WriteTo.File(
                path: Path.Combine(logPath, "lexichord-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                fileSizeLimitBytes: 10_000_000, // 10 MB
                rollOnFileSizeLimit: true,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1))

            // Error-only File Sink (Longer retention)
            .WriteTo.File(
                path: Path.Combine(logPath, "lexichord-errors-.log"),
                restrictedToMinimumLevel: LogEventLevel.Error,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 90,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Properties:j}{NewLine}{Exception}")

            .CreateLogger();

        Log.Information("Serilog configured. Log path: {LogPath}, Debug mode: {DebugMode}",
            logPath, debugMode);
    }
}
```

---

### Task 1.4: DI Integration

**Updates to `HostServices.cs`:**

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

// Add to ConfigureServices method:
services.AddLogging(builder =>
{
    builder.ClearProviders();
    builder.AddSerilog(dispose: true);
});
```

**Update existing services to accept `ILogger<T>`:**

```csharp
// ThemeManager.cs
public sealed class ThemeManager(
    Application application,
    ILogger<ThemeManager> logger) : IThemeManager
{
    public void SetTheme(ThemeMode mode)
    {
        var oldTheme = _currentTheme;
        _currentTheme = mode;

        logger.LogInformation("Theme changed from {OldTheme} to {NewTheme}", oldTheme, mode);

        // ... rest of implementation
    }
}

// WindowStateService.cs
public sealed class WindowStateService(
    ILogger<WindowStateService> logger) : IWindowStateService
{
    public async Task<WindowStateRecord?> LoadAsync()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                logger.LogDebug("No saved window state found at {FilePath}", _filePath);
                return null;
            }

            var json = await File.ReadAllTextAsync(_filePath);
            var state = JsonSerializer.Deserialize<WindowStateRecord>(json, JsonOptions);

            logger.LogDebug(
                "Loaded window state: Position=({X},{Y}), Size=({Width}x{Height})",
                state?.X, state?.Y, state?.Width, state?.Height);

            return state;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load window state from {FilePath}", _filePath);
            return null;
        }
    }
}
```

---

## 4. Decision Tree: Log Level Selection

```text
START: "What log level should I use?"
│
├── Is this a normal application event?
│   └── Information
│       "User {UserId} opened document {DocumentId}"
│       "Theme changed to {Theme}"
│
├── Is this developer/diagnostic detail?
│   └── Debug
│       "Entering method {MethodName} with {Parameters}"
│       "Cache hit for key {CacheKey}"
│
├── Is this very detailed tracing?
│   └── Verbose (rarely used)
│       "Loop iteration {N} of {Total}"
│
├── Is this unexpected but recoverable?
│   └── Warning
│       "Configuration key {Key} not found, using default"
│       "Rate limit approaching: {Current}/{Max}"
│
├── Is this a failure that doesn't crash the app?
│   └── Error
│       "Failed to save document {DocumentId}: {Error}"
│       "API call to {Endpoint} failed"
│
└── Is this a fatal crash?
    └── Fatal
        "Unhandled exception in application startup"
        "Database connection failed, cannot continue"
```

---

## 5. Unit Testing Requirements

### 5.1 Test: Logger Configuration

```csharp
[TestFixture]
[Category("Unit")]
public class SerilogConfigurationTests
{
    [Test]
    public void ConfigureSerilog_CreatesLogDirectory()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Lexichord:DebugMode", "true" }
            })
            .Build();

        try
        {
            // Act
            Environment.SetEnvironmentVariable("APPDATA", tempDir);
            SerilogExtensions.ConfigureSerilog(config);

            // Assert
            var logDir = Path.Combine(tempDir, "Lexichord", "Logs");
            Assert.That(Directory.Exists(logDir), Is.True);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public void ConfigureSerilog_SetsMinimumLevelBasedOnDebugMode()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Lexichord:DebugMode", "true" }
            })
            .Build();

        // Act
        SerilogExtensions.ConfigureSerilog(config);

        // Assert - Debug messages should be logged
        using var testSink = new TestSink();
        Log.Debug("Test debug message");
        Assert.That(testSink.LogEvents, Has.Some.Matches<LogEvent>(
            e => e.Level == LogEventLevel.Debug));
    }
}
```

### 5.2 Test: Service Logging Integration

```csharp
[TestFixture]
[Category("Unit")]
public class ServiceLoggingTests
{
    [Test]
    public void ThemeManager_SetTheme_LogsThemeChange()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ThemeManager>>();
        var mockApp = CreateMockApplication();
        var sut = new ThemeManager(mockApp, mockLogger.Object);

        // Act
        sut.SetTheme(ThemeMode.Light);

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Theme changed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public void WindowStateService_LoadAsync_LogsOnMissingFile()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<WindowStateService>>();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var sut = new WindowStateService(mockLogger.Object, tempDir);

        try
        {
            // Act
            var result = sut.LoadAsync().GetAwaiter().GetResult();

            // Assert
            Assert.That(result, Is.Null);
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No saved window state")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
```

---

## 6. Observability & Logging

### 6.1 Log Events Defined

| Level       | Context           | Message Template                                                     |
| :---------- | :---------------- | :------------------------------------------------------------------- |
| Information | Program           | `Starting Lexichord application`                                     |
| Debug       | Program           | `Command line arguments: {Args}`                                     |
| Information | Program           | `Lexichord application shutdown complete`                            |
| Fatal       | Program           | `Lexichord application terminated unexpectedly`                      |
| Information | SerilogExtensions | `Serilog configured. Log path: {LogPath}, Debug mode: {DebugMode}`   |
| Information | ThemeManager      | `Theme changed from {OldTheme} to {NewTheme}`                        |
| Debug       | WindowStateService| `No saved window state found at {FilePath}`                          |
| Debug       | WindowStateService| `Loaded window state: Position=({X},{Y}), Size=({Width}x{Height})`   |
| Debug       | WindowStateService| `Saved window state to {FilePath}`                                   |
| Warning     | WindowStateService| `Failed to load window state from {FilePath}`                        |

### 6.2 Output Format Examples

**Console (Development):**
```
[14:32:05 INF] Starting Lexichord application
[14:32:05 DBG] Command line arguments: ["--debug-mode"] <Lexichord.Host.Program>
[14:32:06 INF] Serilog configured. Log path: /Users/dev/Library/Application Support/Lexichord/Logs, Debug mode: True <Lexichord.Host.Extensions.SerilogExtensions>
[14:32:06 INF] Theme changed from System to Dark <Lexichord.Host.Services.ThemeManager>
```

**File (Rolling):**
```
2026-01-26 14:32:05.123 +00:00 [INF] [Lexichord.Host.Program] Starting Lexichord application
2026-01-26 14:32:05.456 +00:00 [DBG] [Lexichord.Host.Program] Command line arguments: ["--debug-mode"]
2026-01-26 14:32:06.789 +00:00 [INF] [Lexichord.Host.Extensions.SerilogExtensions] Serilog configured. Log path: /Users/dev/Library/Application Support/Lexichord/Logs, Debug mode: True
```

---

## 7. Security & Safety

### 7.1 Logging Safety Rules

> [!WARNING]
> **Never log sensitive data.** Follow these rules:

```csharp
// ✅ CORRECT: Log identifiers, not values
logger.LogInformation("User {UserId} authenticated", userId);
logger.LogInformation("Processing document {DocumentId}", doc.Id);

// ❌ WRONG: Never log credentials or PII
logger.LogInformation("User {Password} entered", password);  // FORBIDDEN
logger.LogInformation("Email is {Email}", userEmail);        // FORBIDDEN
logger.LogInformation("API Key: {ApiKey}", apiKey);          // FORBIDDEN
```

### 7.2 File Permissions

Log files are written to user's AppData with default permissions. No sensitive data is written, so standard file permissions are acceptable.

---

## 8. Definition of Done

- [ ] Serilog packages installed (`Serilog`, `Serilog.Extensions.Logging`, `Serilog.Sinks.Console`, `Serilog.Sinks.File`, `Serilog.Enrichers.Thread`, `Serilog.Enrichers.Environment`)
- [ ] `Program.cs` creates bootstrap logger before Avalonia initialization
- [ ] `Program.cs` wraps application lifecycle in try/catch with Fatal logging
- [ ] `Program.cs` calls `Log.CloseAndFlush()` in finally block
- [ ] `SerilogExtensions.ConfigureSerilog()` configures full pipeline
- [ ] Console sink uses colorized output template with SourceContext
- [ ] File sink creates rolling daily logs in `{AppData}/Lexichord/Logs/`
- [ ] Error-only file sink has separate 90-day retention
- [ ] `ILogger<T>` registered in DI container
- [ ] `ThemeManager` updated to use `ILogger<ThemeManager>`
- [ ] `WindowStateService` updated to use `ILogger<WindowStateService>`
- [ ] All log messages use structured templates (no string interpolation)
- [ ] Unit tests for Serilog configuration passing
- [ ] Unit tests for service logging integration passing

---

## 9. Verification Commands

```bash
# Build to verify packages and compilation
dotnet build src/Lexichord.Host

# Run with debug mode to see verbose console output
dotnet run --project src/Lexichord.Host -- --debug-mode

# Verify log files are created
# Windows:
dir "%APPDATA%\Lexichord\Logs"
# macOS/Linux:
ls -la ~/.config/Lexichord/Logs/

# Run unit tests
dotnet test --filter "FullyQualifiedName~SerilogConfigurationTests"
dotnet test --filter "FullyQualifiedName~ServiceLoggingTests"

# Check log file contents
# macOS/Linux:
cat ~/.config/Lexichord/Logs/lexichord-$(date +%Y%m%d).log
```
