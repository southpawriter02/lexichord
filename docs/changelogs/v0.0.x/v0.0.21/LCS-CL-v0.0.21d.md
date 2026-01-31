# v0.2.1d: Configuration Watcher (Hot Reload)

**Released:** 2026-01-29  
**Status:** ✅ Complete

## Summary

Implements live reloading of `.lexichord/style.yaml` using `FileSystemWatcher` with debouncing, license gating, and MediatR event publishing.

---

## New Files

| File                                                                | Description                                   |
| ------------------------------------------------------------------- | --------------------------------------------- |
| `Lexichord.Abstractions/Events/StyleDomainEvents.cs`                | MediatR events for reload/error notifications |
| `Lexichord.Tests.Unit/Modules/Style/FileSystemStyleWatcherTests.cs` | 29 unit tests                                 |

---

## Modified Files

### IStyleConfigurationWatcher.cs

- Added `int DebounceDelayMs { get; set; }` property
- Added `Task ForceReloadAsync()` method

### FileSystemStyleWatcher.cs

Full implementation replacing v0.2.1a stub:

- `FileSystemWatcher` on `.lexichord/` directory for `style.yaml`
- 300ms debounce via `System.Threading.Timer`
- License gate: `LicenseTier.WriterPro` required
- Graceful fallback: keeps previous rules on YAML errors
- Publishes `StyleSheetReloadedEvent` and `StyleWatcherErrorEvent`

### StyleModule.cs

- Wires up watcher during `InitializeAsync()`
- Loads custom rules from workspace if present

---

## Domain Events

```csharp
// Published on successful reload
record StyleSheetReloadedEvent(
    string FilePath,
    StyleSheet NewStyleSheet,
    StyleSheet PreviousStyleSheet,
    StyleReloadSource ReloadSource
) : DomainEventBase, INotification;

// Published on watcher/parse errors
record StyleWatcherErrorEvent(
    string? FilePath,
    string ErrorMessage,
    Exception? Exception,
    StyleWatcherErrorType ErrorType
) : DomainEventBase, INotification;
```

---

## Test Coverage

| Category          | Tests                                            |
| ----------------- | ------------------------------------------------ |
| License Gating    | 4 tests (Core, WriterPro, Teams, Enterprise)     |
| Lifecycle         | 10 tests (Start, Stop, Dispose, idempotency)     |
| Path Validation   | 3 tests (null, empty, whitespace)                |
| Debounce Config   | 2 tests (default, setter)                        |
| ForceReloadAsync  | 5 tests (not watching, file exists/not, MediatR) |
| Events            | 4 tests (success/error local and MediatR events) |
| Graceful Fallback | 1 test (keeps previous on error)                 |

**Total: 29 new tests, all passing**

---

## Verification

```bash
# Build
dotnet build  # 0 errors, 0 warnings

# Run new tests
dotnet test --filter "FullyQualifiedName~FileSystemStyleWatcher"  # 29 passed

# Run all Style tests
dotnet test --filter "FullyQualifiedName~Style"  # 142 passed
```

---

## Dependencies

- v0.2.1a: `IStyleConfigurationWatcher` interface
- v0.2.1c: `IStyleSheetLoader` for YAML loading
- v0.0.7a: MediatR for event publishing
- v0.0.4c: `ILicenseContext` for tier checking
