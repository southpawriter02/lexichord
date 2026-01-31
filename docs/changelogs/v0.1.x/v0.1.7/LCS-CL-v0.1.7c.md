# LCS-CL-017c: Release Notes Viewer

## Overview

| Attribute | Value                |
| --------- | -------------------- |
| Version   | v0.1.7c              |
| Status    | ✅ Complete          |
| Feature   | Release Notes Viewer |
| Date      | 2026-01-29           |

## Summary

This sub-part implements automatic release notes display after application updates by tracking version history and opening `CHANGELOG.md` in the editor.

## Changes

### Abstractions Layer

| File                  | Change | Description                                               |
| --------------------- | ------ | --------------------------------------------------------- |
| `IFirstRunService.cs` | NEW    | Interface for version tracking and first-run detection    |
| `FirstRunSettings.cs` | NEW    | Settings record for version, installation ID, preferences |
| `FirstRunEvents.cs`   | NEW    | `FirstRunDetectedEvent` and `ReleaseNotesDisplayedEvent`  |

### Host Implementation

| File                    | Change | Description                                      |
| ----------------------- | ------ | ------------------------------------------------ |
| `FirstRunService.cs`    | NEW    | Implementation with lazy init, JSON persistence  |
| `HostServices.cs`       | MOD    | Registered `IFirstRunService` as singleton       |
| `App.axaml.cs`          | MOD    | Added `HandleFirstRunAsync` to startup sequence  |
| `Lexichord.Host.csproj` | MOD    | Configured CHANGELOG.md copy to output directory |

### Unit Tests

| File                      | Tests | Description                                           |
| ------------------------- | ----- | ----------------------------------------------------- |
| `FirstRunServiceTests.cs` | 18    | Version detection, persistence, events, release notes |

## Key Implementation Details

### Version Comparison

Versions are normalized before comparison:

- Remove leading `v` or `V`
- Trim trailing `.0` segments
- Case-insensitive comparison

```csharp
// "v1.0.0.0" == "1.0.0" == "1.0" -> true
VersionsMatch("v1.0.0.0", "1.0.0"); // true
```

### First-Run Detection Logic

1. **Fresh Install**: No stored version → `IsFirstRunEver = true`
2. **Update**: Stored version differs → `IsFirstRunAfterUpdate = true`
3. **Normal Run**: Versions match → both flags false
4. **Velopack Override**: Environment variables can force flags

### Settings Persistence

Settings stored as JSON in `~/.config/Lexichord/first-run-settings.json`:

```json
{
    "LastRunVersion": "0.1.7",
    "ShowReleaseNotesOnUpdate": true,
    "ShowWelcomeOnFirstRun": true,
    "FirstRunDate": "2026-01-29T15:00:00Z",
    "InstallationId": "a1b2c3d4e5f6"
}
```

## Testing

```bash
# Run FirstRunService tests
dotnet test --filter "FullyQualifiedName~FirstRunService"

# Verify CHANGELOG.md bundling
dotnet publish src/Lexichord.Host -c Release -o ./publish
ls ./publish/CHANGELOG.md  # Should exist
```

## Dependencies

- `MediatR` — Event publication
- `System.Text.Json` — Settings serialization
- `System.Reflection` — Assembly version extraction

## Related Documents

- [LCS-DES-017c](../../specs/v0.1.x/v0.1.7/LCS-DES-017c.md) — Design specification
- [DEPENDENCY-MATRIX](../../specs/DEPENDENCY-MATRIX.md) — Interface registry
