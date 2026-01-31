# LCS-CL-003c: Changelog — Global Exception Trap

## Document Control

| Field            | Value                                                  |
| :--------------- | :----------------------------------------------------- |
| **Document ID**  | LCS-CL-003c                                            |
| **Version**      | v0.0.3c                                                |
| **Date**         | 2026-01-28                                             |
| **Author**       | System                                                 |
| **Related Spec** | [LCS-DES-003c](../specs/v0.0.x/v0.0.3/LCS-DES-003c.md) |

---

## Summary

Implemented global exception handling with crash report generation, user-facing dialog, and platform-specific report storage.

---

## Changes

### Files Created

| File                                            | Purpose                              |
| :---------------------------------------------- | :----------------------------------- |
| `Abstractions/Contracts/ICrashReportService.cs` | Contract for crash report management |
| `Host/Services/CrashReportService.cs`           | Crash report save and display logic  |
| `Host/Views/CrashReportWindow.axaml`            | Crash dialog XAML layout             |
| `Host/Views/CrashReportWindow.axaml.cs`         | Crash dialog code-behind             |
| `Tests.Unit/Host/CrashReportServiceTests.cs`    | Unit tests (9 tests)                 |

### Files Modified

| File                         | Change                                     |
| :--------------------------- | :----------------------------------------- |
| `Host/Lexichord.Host.csproj` | Added `InternalsVisibleTo` for test access |
| `Host/HostServices.cs`       | Registered `ICrashReportService` in DI     |
| `Host/App.axaml.cs`          | Added `RegisterExceptionHandlers()` method |

---

## Exception Handler Architecture

### Registration Flow

```
OnFrameworkInitializationCompleted()
    ↓
RegisterExceptionHandlers()
    ↓
┌─────────────────────────────────────┐
│ AppDomain.UnhandledException        │ → Log + Show Dialog (if terminating)
├─────────────────────────────────────┤
│ TaskScheduler.UnobservedTaskException│ → Log + SetObserved()
└─────────────────────────────────────┘
```

### Crash Report Flow

```
Exception occurs
    ↓
Log.Fatal (critical error)
    ↓
SaveCrashReportAsync() → crash-2026-01-28_HH-mm-ss-fff.log
    ↓
ShowCrashReport() → CrashReportWindow
    ↓
User: Copy / Open Folder / Close
```

### Report Location

| Platform | Path                                                    |
| :------- | :------------------------------------------------------ |
| Windows  | `%APPDATA%\Lexichord\CrashReports\`                     |
| macOS    | `~/Library/Application Support/Lexichord/CrashReports/` |
| Linux    | `~/.config/Lexichord/CrashReports/`                     |

---

## Crash Report Contents

| Section            | Details                                      |
| :----------------- | :------------------------------------------- |
| System Information | OS, .NET version, machine name, memory usage |
| Exception Details  | Type, message, source, HResult               |
| Stack Trace        | Full call stack                              |
| Inner Exceptions   | Recursive unwrapping of all inner exceptions |
| Additional Data    | Exception.Data dictionary entries            |

---

## Definition of Done Verification

| Criterion                                    | Status  |
| :------------------------------------------- | :------ |
| `ICrashReportService` interface created      | ✅ Pass |
| `CrashReportService` implementation complete | ✅ Pass |
| `CrashReportWindow` dialog implemented       | ✅ Pass |
| DI registration in `HostServices.cs`         | ✅ Pass |
| Exception handlers in `App.axaml.cs`         | ✅ Pass |
| Unit tests for `CrashReportService`          | ✅ Pass |
| All tests passing (9 tests)                  | ✅ Pass |
| Build succeeds                               | ✅ Pass |
