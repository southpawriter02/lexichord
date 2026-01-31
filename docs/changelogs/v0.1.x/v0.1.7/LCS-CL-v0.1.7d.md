# LCS-CL-017d: Telemetry Hooks

## Overview

| Attribute | Value           |
| --------- | --------------- |
| Version   | v0.1.7d         |
| Status    | ✅ Complete     |
| Feature   | Telemetry Hooks |
| Date      | 2026-01-30      |

## Summary

This sub-part implements opt-in crash reporting via Sentry SDK integration, with robust PII scrubbing and user-configurable privacy settings.

## Changes

### Abstractions Layer

#### [ITelemetryService.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Abstractions/Contracts/ITelemetryService.cs)

- Defines the `ITelemetryService` interface with:
    - `IsEnabled` property for querying current state
    - `Enable()` / `Disable()` methods for opt-in control
    - `CaptureException()` with optional tags
    - `CaptureMessage()` with severity level
    - `AddBreadcrumb()` for context trail
    - `BeginScope()` for operation grouping
    - `SetUser()` for session correlation
    - `Flush()` for graceful shutdown
- Defines `TelemetryLevel` enum (Debug, Info, Warning, Error, Fatal)

#### [TelemetrySettings.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Abstractions/Contracts/TelemetrySettings.cs)

- Immutable record for persisted preferences:
    - `CrashReportingEnabled` (default: false)
    - `ConsentPromptShown` flag
    - `ConsentDate` timestamp
    - `InstallationId` for anonymous correlation

#### [TelemetryEvents.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Abstractions/Events/TelemetryEvents.cs)

- `TelemetryPreferenceChangedEvent` for preference auditing
- `CrashCapturedEvent` for exception type tracking

---

### Host Layer

#### [TelemetryService.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Host/Services/TelemetryService.cs)

- Full implementation with Sentry SDK:
    - Opt-in by default via `SendDefaultPii = false`
    - PII scrubbing with 1-second regex timeout:
        - Windows paths (`C:\Users\username\...`)
        - macOS paths (`/Users/username/...`)
        - Linux paths (`/home/username/...`)
        - Email addresses
    - JSON settings persistence
    - Degraded mode without DSN (local logging only)
    - MediatR event publishing

#### [GlobalExceptionHandler.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Host/GlobalExceptionHandler.cs)

- Static handler for unhandled exceptions:
    - `AppDomain.UnhandledException` subscription
    - `TaskScheduler.UnobservedTaskException` subscription
    - Routes to `ITelemetryService` when enabled

#### [SerilogSentryExtensions.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Host/Configuration/SerilogSentryExtensions.cs)

- Extension methods for conditional Sentry sink configuration

---

### Settings UI

#### [PrivacySettingsViewModel.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Host/Settings/PrivacySettingsViewModel.cs)

- Two-way binding for crash reporting toggle
- `LearnMoreCommand` for privacy policy link

#### [PrivacySettingsPage.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Host/Settings/Pages/PrivacySettingsPage.cs)

- Settings page registration with:
    - CategoryId: "privacy"
    - Icon: "Shield"
    - SortOrder: 50

#### [PrivacySettingsView.axaml](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Host/Settings/PrivacySettingsView.axaml)

- Privacy settings UI with:
    - Toggle switch for crash reporting
    - Clear disclosure of collected data
    - "Learn More" link button

---

### Integration

#### [HostServices.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Host/HostServices.cs)

- Registered `ITelemetryService` as singleton
- Registered `PrivacySettingsViewModel` and `PrivacySettingsPage`

#### [App.axaml.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Host/App.axaml.cs)

- Integrated `GlobalExceptionHandler.Initialize()` in exception handler setup

---

### Package Dependencies

#### [Directory.Build.props](file:///Users/ryan/Documents/GitHub/lexichord/Directory.Build.props)

- Added `SentryVersion` = 5.5.0

#### [Lexichord.Host.csproj](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Host/Lexichord.Host.csproj)

- Added `Sentry` and `Sentry.Serilog` packages

---

### Unit Tests

#### [TelemetryServiceTests.cs](file:///Users/ryan/Documents/GitHub/lexichord/tests/Lexichord.Tests.Unit/Host/TelemetryServiceTests.cs)

- 19 tests covering:
    - PII scrubbing (Windows/macOS/Linux paths, emails)
    - Enable/disable state management
    - Operations when disabled (no-throw)
    - Settings persistence
    - Disposal safety

#### [PrivacySettingsViewModelTests.cs](file:///Users/ryan/Documents/GitHub/lexichord/tests/Lexichord.Tests.Unit/Host/PrivacySettingsViewModelTests.cs)

- 8 tests covering:
    - Initial state synchronization
    - Toggle behavior
    - Property change notifications
    - Null guard validation

## Testing Verified

- ✅ Build succeeds with zero warnings
- ✅ All 34 unit tests pass
- ✅ PII scrubbing covers all path formats
- ✅ Toggle immediately reflects service state
