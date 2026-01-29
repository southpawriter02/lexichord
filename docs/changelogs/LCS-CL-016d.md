# LCS-CL-016d: Update Channel Selector

**Version:** v0.1.6d  
**Date:** 2026-01-29  
**Status:** ✅ Complete

---

## Summary

This sub-part implements the Update Channel Selector for the Settings dialog, enabling users to switch between Stable and Insider update channels, view current application version information, and manually check for updates (stub implementation).

---

## What's New

### Core Abstractions

| Type      | Name                            | Purpose                                       |
| --------- | ------------------------------- | --------------------------------------------- |
| Enum      | `UpdateChannel`                 | Enumerates Stable and Insider update channels |
| Record    | `VersionInfo`                   | Encapsulates detailed version information     |
| Record    | `UpdateInfo`                    | Describes available update details            |
| Interface | `IUpdateService`                | Service interface for update management       |
| Class     | `UpdateAvailableEventArgs`      | Event args for update available notifications |
| Class     | `UpdateChannelChangedEventArgs` | Event args for channel change notifications   |

### MediatR Domain Events

| Event                       | Published When                       |
| --------------------------- | ------------------------------------ |
| `UpdateChannelChangedEvent` | Update channel changed by user       |
| `UpdateAvailableEvent`      | A new update is available            |
| `UpdateCheckCompletedEvent` | Update check completed (with result) |

### UpdateService Implementation

Key features:

- Channel switching between Stable and Insider
- Settings persistence to `~/Library/Application Support/Lexichord/update-settings.json`
- Version information extraction from assembly metadata
- Stub update checking with simulated delay (actual implementation in v0.2.x)
- MediatR event publishing for channel changes and check completion

### Updates Settings UI

| Component                   | Purpose                                                 |
| --------------------------- | ------------------------------------------------------- |
| `UpdatesSettingsPage`       | `ISettingsPage` implementation for Updates category     |
| `UpdatesSettingsView.axaml` | XAML layout with version info, channel selection, check |
| `UpdatesSettingsViewModel`  | Commands for channel switching and update checking      |

UI Features:

- Version information display (version, build, runtime)
- Radio button channel selection with descriptions
- "Check Now" button with loading indicator
- Last check time display with relative formatting

---

## Files Changed

### New Files

| File                                                                    | Purpose                     |
| ----------------------------------------------------------------------- | --------------------------- |
| `src/Lexichord.Abstractions/Contracts/UpdateChannel.cs`                 | Channel enum                |
| `src/Lexichord.Abstractions/Contracts/VersionInfo.cs`                   | Version info record         |
| `src/Lexichord.Abstractions/Contracts/UpdateInfo.cs`                    | Update info record          |
| `src/Lexichord.Abstractions/Contracts/IUpdateService.cs`                | Service interface           |
| `src/Lexichord.Abstractions/Contracts/UpdateAvailableEventArgs.cs`      | Event args                  |
| `src/Lexichord.Abstractions/Contracts/UpdateChannelChangedEventArgs.cs` | Event args                  |
| `src/Lexichord.Abstractions/Events/UpdateChannelChangedEvent.cs`        | MediatR event               |
| `src/Lexichord.Abstractions/Events/UpdateAvailableEvent.cs`             | MediatR event               |
| `src/Lexichord.Abstractions/Events/UpdateCheckCompletedEvent.cs`        | MediatR event               |
| `src/Lexichord.Host/Settings/UpdateSettings.cs`                         | Settings persistence record |
| `src/Lexichord.Host/Services/UpdateService.cs`                          | Service implementation      |
| `src/Lexichord.Host/Settings/Pages/UpdatesSettingsPage.cs`              | Settings page               |
| `src/Lexichord.Host/Settings/Views/UpdatesSettingsView.axaml`           | UI layout                   |
| `src/Lexichord.Host/Settings/Views/UpdatesSettingsView.axaml.cs`        | Code-behind                 |
| `src/Lexichord.Host/Settings/UpdatesSettingsViewModel.cs`               | ViewModel                   |

### Modified Files

| File                                 | Change                                                                |
| ------------------------------------ | --------------------------------------------------------------------- |
| `src/Lexichord.Host/HostServices.cs` | Added DI registrations for `IUpdateService` and `UpdatesSettingsPage` |

### Test Files

| File                                                               | Tests    |
| ------------------------------------------------------------------ | -------- |
| `tests/Lexichord.Tests.Unit/Host/UpdateServiceTests.cs`            | 15 tests |
| `tests/Lexichord.Tests.Unit/Host/UpdatesSettingsViewModelTests.cs` | 17 tests |

---

## Breaking Changes

None.

---

## Dependencies

| Dependency              | Version | Purpose                    |
| ----------------------- | ------- | -------------------------- |
| `ISettingsPageRegistry` | v0.1.6a | Settings page registration |
| `MediatR`               | v0.0.7a | Domain event publishing    |

---

## Testing

### Unit Tests Added

- **UpdateServiceTests** — Initialization, channel switching, update checking, event publication
- **UpdatesSettingsViewModelTests** — Command execution, channel selection, display properties

### Test Results

```
Passed!  - All Update-related tests passing
```

---

## Verification

```bash
# Build
dotnet build src/Lexichord.Host/Lexichord.Host.csproj

# Run update-related tests
dotnet test tests/Lexichord.Tests.Unit --filter "Update"

# Run all unit tests
dotnet test tests/Lexichord.Tests.Unit
```

---

## Related Documents

- [LCS-DES-016d](../specs/v0.1.x/v0.1.6/LCS-DES-016d.md) — Design Specification
- [LCS-SBD-016](../specs/v0.1.x/v0.1.6/LCS-SBD-016.md) — Scope Breakdown
- [LCS-DES-016-INDEX](../specs/v0.1.x/v0.1.6/LCS-DES-016-INDEX.md) — Version Index
