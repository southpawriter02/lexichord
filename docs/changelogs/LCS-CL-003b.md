# LCS-CL-003b: Changelog — Serilog Pipeline

## Document Control

| Field            | Value                                                  |
| :--------------- | :----------------------------------------------------- |
| **Document ID**  | LCS-CL-003b                                            |
| **Version**      | v0.0.3b                                                |
| **Date**         | 2026-01-28                                             |
| **Author**       | System                                                 |
| **Related Spec** | [LCS-DES-003b](../specs/v0.0.x/v0.0.3/LCS-DES-003b.md) |

---

## Summary

Implemented Serilog as the structured logging framework with bootstrap logger, Console/File sinks, and `ILogger<T>` DI integration.

---

## Changes

### NuGet Packages Added

| Package                         | Version | Purpose                   |
| :------------------------------ | :------ | :------------------------ |
| `Serilog`                       | 4.2.0   | Core logging framework    |
| `Serilog.Extensions.Logging`    | 9.0.0   | ILogger<T> DI integration |
| `Serilog.Sinks.Console`         | 6.0.0   | Colorized console output  |
| `Serilog.Sinks.File`            | 6.0.0   | Rolling file logs         |
| `Serilog.Enrichers.Thread`      | 4.0.0   | Thread ID enrichment      |
| `Serilog.Enrichers.Environment` | 3.0.1   | Machine name enrichment   |

### Files Created

| File                                         | Purpose                     |
| :------------------------------------------- | :-------------------------- |
| `Host/Extensions/SerilogExtensions.cs`       | Full pipeline configuration |
| `Tests.Unit/Host/SerilogIntegrationTests.cs` | DI integration tests        |

### Files Modified

| File                                  | Change                                          |
| :------------------------------------ | :---------------------------------------------- |
| `Host/Lexichord.Host.csproj`          | Added 6 Serilog packages                        |
| `Host/Program.cs`                     | Bootstrap logger, try/catch/finally, exit codes |
| `Host/HostServices.cs`                | Added `AddLogging()` with Serilog               |
| `Host/App.axaml.cs`                   | Calls `ConfigureSerilog()`, added logging       |
| `Host/Services/ThemeManager.cs`       | Added `ILogger<ThemeManager>` injection         |
| `Host/Services/WindowStateService.cs` | Added `ILogger<WindowStateService>` injection   |

---

## Logging Architecture

### Two-Stage Logger

```
Program.Main()
    ↓
Bootstrap Logger (console-only, captures startup errors)
    ↓
OnFrameworkInitializationCompleted()
    ↓
Full Pipeline (console + file + error file)
```

### Sinks Configured

| Sink       | Output Template                                  | Retention |
| :--------- | :----------------------------------------------- | :-------- |
| Console    | `[HH:mm:ss LVL] Message <SourceContext>`         | n/a       |
| File       | `yyyy-MM-dd HH:mm:ss.fff [LVL] [Source] Message` | 30 days   |
| Error File | Same + `{Properties:j}`                          | 90 days   |

### Log Location

- **macOS/Linux:** `~/.config/Lexichord/Logs/`
- **Windows:** `%APPDATA%\Lexichord\Logs\`

---

## Definition of Done Verification

| Criterion                                          | Status  |
| :------------------------------------------------- | :------ |
| Serilog packages installed                         | ✅ Pass |
| Bootstrap logger in Program.cs                     | ✅ Pass |
| try/catch/finally with `Log.Fatal`/`CloseAndFlush` | ✅ Pass |
| `SerilogExtensions.ConfigureSerilog()` exists      | ✅ Pass |
| Console sink with colorized output                 | ✅ Pass |
| Rolling daily file sink (30-day retention)         | ✅ Pass |
| Error-only file sink (90-day retention)            | ✅ Pass |
| `ILogger<T>` registered in DI                      | ✅ Pass |
| `ThemeManager` updated with logging                | ✅ Pass |
| `WindowStateService` updated with logging          | ✅ Pass |
| All log messages use structured templates          | ✅ Pass |
| Unit tests created                                 | ✅ Pass |
