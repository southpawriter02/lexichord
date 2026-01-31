# LCS-CL-017a: Velopack Integration

**Version**: v0.1.7a  
**Category**: Distribution & Updates  
**Status**: ✅ Complete

---

## Overview

Integrates [Velopack](https://velopack.io) for native platform packaging and auto-updates, replacing the stub update implementation with real update functionality.

---

## Changes

### Abstractions (Lexichord.Abstractions)

#### New Files

- [**UpdateOptions.cs**](file:///Volumes/GitHub/github/southpawriter02/lexichord/src/Lexichord.Abstractions/Contracts/UpdateOptions.cs) — Configuration record for update service with channel URLs and behavior settings.

- [**DownloadProgressEventArgs.cs**](file:///Volumes/GitHub/github/southpawriter02/lexichord/src/Lexichord.Abstractions/Contracts/DownloadProgressEventArgs.cs) — Event args for download progress reporting with normalized 0.0-1.0 values.

#### Modified Files

- [**IUpdateService.cs**](file:///Volumes/GitHub/github/southpawriter02/lexichord/src/Lexichord.Abstractions/Contracts/IUpdateService.cs) — Extended interface with:
    - `DownloadUpdatesAsync(UpdateInfo, IProgress<float>?, CancellationToken)` — Download updates with progress
    - `ApplyUpdatesAndRestart()` — Apply downloaded update and restart app
    - `IsUpdateReady` — Property indicating update is downloaded and ready
    - `UpdateProgress` — Event for real-time download progress

---

### Host Infrastructure (Lexichord.Host)

#### Modified Files

- [**Program.cs**](file:///Volumes/GitHub/github/southpawriter02/lexichord/src/Lexichord.Host/Program.cs) — Added `VelopackApp.Build().Run()` as first line of `Main()` per Velopack requirements.

- [**UpdateService.cs**](file:///Volumes/GitHub/github/southpawriter02/lexichord/src/Lexichord.Host/Services/UpdateService.cs) — Enhanced with Velopack integration:
    - Uses `Velopack.UpdateManager` for actual update operations
    - Type aliases (`LexichordUpdateInfo`, `VelopackUpdateInfo`) to avoid ambiguity
    - Skips update checks in development mode (`!IsInstalled`)
    - Caches Velopack `UpdateInfo` for download/apply workflow

- [**HostServices.cs**](file:///Volumes/GitHub/github/southpawriter02/lexichord/src/Lexichord.Host/HostServices.cs) — Registers `UpdateOptions` from configuration.

- [**appsettings.json**](file:///Volumes/GitHub/github/southpawriter02/lexichord/src/Lexichord.Host/appsettings.json) — Added `UpdateOptions` section with placeholder URLs.

- [**Lexichord.Host.csproj**](file:///Volumes/GitHub/github/southpawriter02/lexichord/src/Lexichord.Host/Lexichord.Host.csproj) — Added Velopack NuGet package reference.

- [**Directory.Build.props**](file:///Volumes/GitHub/github/southpawriter02/lexichord/Directory.Build.props) — Added centralized `VelopackVersion` property.

---

### Build Scripts

#### New Files

- [**pack-windows.ps1**](file:///Volumes/GitHub/github/southpawriter02/lexichord/build/scripts/pack-windows.ps1) — PowerShell script for Windows packaging with `vpk pack`.

- [**pack-macos.sh**](file:///Volumes/GitHub/github/southpawriter02/lexichord/build/scripts/pack-macos.sh) — Bash script for macOS packaging with `vpk pack`.

---

### Unit Tests

- [**UpdateServiceTests.cs**](file:///Volumes/GitHub/github/southpawriter02/lexichord/tests/Lexichord.Tests.Unit/Host/UpdateServiceTests.cs) — Updated for v0.1.7a:
    - Updated constructor to use `UpdateOptions` parameter
    - Added tests for `IsUpdateReady`, `DownloadUpdatesAsync`, `ApplyUpdatesAndRestart`
    - Added tests for `UpdateOptions.GetUrlForChannel()`
    - Fixed namespace conflicts using `global::` qualifier

---

## Technical Notes

### Velopack Bootstrap

```csharp
// Must be FIRST line in Main(), before any other code
VelopackApp.Build().Run();
```

This handles install/uninstall/update lifecycle hooks. The call may not return if Velopack is handling a lifecycle event.

### Type Ambiguity

Velopack defines its own `UpdateInfo` and `UpdateOptions` types. We use type aliases to disambiguate:

```csharp
using LexichordUpdateInfo = Lexichord.Abstractions.Contracts.UpdateInfo;
using VelopackUpdateInfo = Velopack.UpdateInfo;
```

### Development Mode

When running in development (not installed via Velopack), `UpdateManager.IsInstalled` returns false. The service logs this and returns `null` from `CheckForUpdatesAsync` without error.

---

## Verification

| Check                                                  | Status |
| ------------------------------------------------------ | ------ |
| `dotnet build src/Lexichord.Host`                      | ✅     |
| 1,267 unit tests pass (28 skipped - platform-specific) | ✅     |
| 23 UpdateServiceTests pass                             | ✅     |
