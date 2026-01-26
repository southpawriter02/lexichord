# LCS-SBD: Scope Breakdown - v0.0.3

**Target Version:** `v0.0.3`
**Codename:** The Nervous System (Logging & DI)
**Timeline:** Sprint 2 (Core Services Foundation)
**Owner:** Lead Architect
**Prerequisites:** v0.0.2d complete (Host Shell with Theme & Window State).

## 1. Executive Summary

**v0.0.3** establishes the runtime infrastructure that enables the application to "think" and report errors. This release transforms the Avalonia shell from a static UI into a properly instrumented, dependency-injectable application. The success of this release is measured by:

1. Microsoft.Extensions.DependencyInjection is the sole IoC container for the application.
2. Serilog captures all application logs with proper structured formatting.
3. Unhandled exceptions are caught globally and presented via a "Crash Report" dialog.
4. Configuration loads from multiple sources (appsettings.json, environment variables, CLI arguments).

If this foundation is flawed, Module loading (v0.0.4) will fail to register services, and debugging production issues will be impossible.

---

## 2. Sub-Part Specifications

### v0.0.3a: Dependency Injection Root

**Goal:** Establish Microsoft.Extensions.DependencyInjection as the application's IoC container.

- **Task 1.1: Install DI Packages**
    - Add `Microsoft.Extensions.DependencyInjection` NuGet package to `Lexichord.Host`.
    - Add `Microsoft.Extensions.Hosting.Abstractions` for `IHostEnvironment` support.
- **Task 1.2: Create Service Collection Builder**
    - Create `HostServices.cs` static class in `Lexichord.Host/Services/`.
    - Implement `ConfigureServices(IServiceCollection services)` extension method.
    - Register existing services: `IThemeManager`, `IWindowStateService`.
- **Task 1.3: Integrate with Avalonia Lifecycle**
    - Modify `App.axaml.cs` to build `IServiceProvider` during `OnFrameworkInitializationCompleted`.
    - Store `IServiceProvider` as static property for access during application lifetime.
    - Inject services into `MainWindow` via constructor or property injection.
- **Task 1.4: Service Locator Pattern (Transitional)**
    - Create `IServiceLocator` interface in `Lexichord.Abstractions`.
    - Implement `ServiceLocator` in Host for ViewModels that cannot use constructor injection.
    - Mark as `[Obsolete]` to encourage proper DI migration.

**Definition of Done:**

- All services are resolved from `IServiceProvider`.
- No direct `new` instantiation of services in Host code.
- `ThemeManager` and `WindowStateService` are registered as Singletons.

---

### v0.0.3b: Serilog Pipeline

**Goal:** Configure comprehensive structured logging with Console and File sinks.

- **Task 1.1: Install Serilog Packages**
    - Add `Serilog` core package.
    - Add `Serilog.Extensions.Logging` for Microsoft.Extensions.Logging integration.
    - Add `Serilog.Sinks.Console` for development output.
    - Add `Serilog.Sinks.File` for rolling log files.
    - Add `Serilog.Enrichers.Thread` and `Serilog.Enrichers.Environment`.
- **Task 1.2: Bootstrap Logger**
    - Create bootstrap logger in `Program.cs` before Avalonia initialization.
    - Configure minimal console sink for startup errors.
    - Wrap `BuildAvaloniaApp().StartWithClassicDesktopLifetime()` in try/catch.
- **Task 1.3: Full Logger Configuration**
    - Create `SerilogExtensions.cs` in `Lexichord.Host/Extensions/`.
    - Configure Console sink with output template: `[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} <{SourceContext}>{NewLine}{Exception}`.
    - Configure File sink with rolling daily logs to `{AppData}/Lexichord/Logs/lexichord-.log`.
    - Configure error-only File sink to `{AppData}/Lexichord/Logs/lexichord-errors-.log`.
    - Set minimum level overrides for noisy namespaces (Microsoft, System, Avalonia).
- **Task 1.4: DI Integration**
    - Register `ILogger<T>` factory in service collection.
    - Ensure all existing services accept `ILogger<T>` via constructor.
    - Add logging to `ThemeManager` and `WindowStateService`.

**Definition of Done:**

- Console displays formatted log messages during development.
- Log files are created in `{AppData}/Lexichord/Logs/` directory.
- All services use injected `ILogger<T>` for logging.
- Log messages use structured templates (no string interpolation).

---

### v0.0.3c: Global Exception Trap

**Goal:** Capture all unhandled exceptions and present a user-friendly crash dialog.

- **Task 1.1: Exception Handlers**
    - Subscribe to `AppDomain.CurrentDomain.UnhandledException` in `Program.cs`.
    - Subscribe to `TaskScheduler.UnobservedTaskException` for async exceptions.
    - Subscribe to `RxApp.DefaultExceptionHandler` for ReactiveUI (if applicable).
- **Task 1.2: Crash Report Dialog**
    - Create `CrashReportWindow.axaml` in `Lexichord.Host/Views/`.
    - Display: Exception type, message, stack trace (scrollable).
    - Include "Copy to Clipboard" button for support tickets.
    - Include "Close Application" button.
- **Task 1.3: Crash Report Service**
    - Create `ICrashReportService` interface in `Lexichord.Abstractions`.
    - Create `CrashReportService` implementation in Host.
    - Methods: `ShowCrashReport(Exception ex)`, `SaveCrashReport(Exception ex)`.
    - Save crash reports to `{AppData}/Lexichord/CrashReports/crash-{timestamp}.log`.
- **Task 1.4: Graceful Shutdown**
    - Ensure `Log.CloseAndFlush()` is called before application exit.
    - Ensure crash report is saved before showing dialog.
    - Handle case where dialog cannot be shown (fallback to file only).

**Definition of Done:**

- Throwing an unhandled exception shows the Crash Report dialog.
- Crash reports are saved to disk with full stack trace.
- Application exits cleanly after user closes the dialog.
- All exceptions are logged to Serilog before dialog appears.

---

### v0.0.3d: Configuration Service

**Goal:** Load application configuration from multiple sources with proper precedence.

- **Task 1.1: Install Configuration Packages**
    - Add `Microsoft.Extensions.Configuration` core package.
    - Add `Microsoft.Extensions.Configuration.Json` for appsettings.json.
    - Add `Microsoft.Extensions.Configuration.EnvironmentVariables`.
    - Add `Microsoft.Extensions.Configuration.CommandLine`.
- **Task 1.2: Create appsettings.json**
    - Create `appsettings.json` in `Lexichord.Host/` root.
    - Define sections: `Lexichord`, `Serilog`, `FeatureFlags`.
    - Create `appsettings.Development.json` with debug-level logging.
- **Task 1.3: Configuration Builder**
    - Create `ConfigurationBuilder` setup in `Program.cs` or `HostServices.cs`.
    - Load order: appsettings.json → appsettings.{Environment}.json → Environment Variables → CLI Arguments.
    - Environment detection via `DOTNET_ENVIRONMENT` or `LEXICHORD_ENVIRONMENT`.
- **Task 1.4: Configuration Options Pattern**
    - Create `LexichordOptions` record in `Lexichord.Abstractions`.
    - Create `DebugOptions` record for debug-specific settings.
    - Register options via `services.Configure<T>(configuration.GetSection("..."))`.
    - Implement CLI argument parsing for `--debug-mode`, `--log-level`.

**Definition of Done:**

- `IConfiguration` is injectable into any service.
- `appsettings.json` values are loaded on startup.
- Environment variables prefixed with `LEXICHORD_` override JSON settings.
- CLI argument `--debug-mode` enables verbose logging.
- `IOptions<LexichordOptions>` provides strongly-typed access to configuration.

---

## 3. Implementation Checklist (for Developer)

| Step     | Description                                                                 | Status |
| :------- | :-------------------------------------------------------------------------- | :----- |
| **0.3a** | `Microsoft.Extensions.DependencyInjection` package installed.               | [ ]    |
| **0.3a** | `HostServices.ConfigureServices()` registers all existing services.         | [ ]    |
| **0.3a** | `App.axaml.cs` builds `IServiceProvider` on startup.                        | [ ]    |
| **0.3b** | Serilog packages installed (Core, Console, File, Enrichers).                | [ ]    |
| **0.3b** | Bootstrap logger wraps application startup.                                 | [ ]    |
| **0.3b** | Rolling log files created in `{AppData}/Lexichord/Logs/`.                   | [ ]    |
| **0.3b** | `ILogger<T>` injected into `ThemeManager` and `WindowStateService`.         | [ ]    |
| **0.3c** | Global exception handlers registered for AppDomain and TaskScheduler.       | [ ]    |
| **0.3c** | `CrashReportWindow.axaml` displays exception details.                       | [ ]    |
| **0.3c** | Crash reports saved to `{AppData}/Lexichord/CrashReports/`.                 | [ ]    |
| **0.3d** | `appsettings.json` created with Lexichord and Serilog sections.             | [ ]    |
| **0.3d** | Configuration loads from JSON, Environment Variables, and CLI.              | [ ]    |
| **0.3d** | `--debug-mode` CLI argument enables verbose logging.                        | [ ]    |

## 4. Risks & Mitigations

- **Risk:** Serilog not flushing before crash dialog shows.
    - _Mitigation:_ Call `Log.CloseAndFlush()` synchronously before showing dialog.
- **Risk:** Configuration file missing in published build.
    - _Mitigation:_ Set `appsettings.json` as `CopyToOutputDirectory=PreserveNewest` in `.csproj`.
- **Risk:** DI container not available during Avalonia previewer.
    - _Mitigation:_ Use design-time fallbacks in ViewModels with null checks.
- **Risk:** Crash dialog fails to show due to UI thread exception.
    - _Mitigation:_ Always save crash report to file first; dialog is best-effort.
- **Risk:** Log files consume excessive disk space.
    - _Mitigation:_ Configure `retainedFileCountLimit: 30` and `fileSizeLimitBytes: 10MB`.
