# LCS-CL-012b: File System Watcher

**Version:** v0.1.2b  
**Category:** Modules  
**Feature Name:** Robust File System Watcher  
**Date:** 2026-01-29

---

## Summary

Implements a robust file system watcher that wraps `System.IO.FileSystemWatcher` with enhanced capabilities including debouncing, batching, ignore patterns, buffer overflow detection, and automatic error recovery. This replaces the stub implementation from v0.1.2a.

---

## New Features

### RobustFileSystemWatcher Implementation

- **Debouncing** — Accumulates file changes for a configurable delay (default: 100ms) before emitting events. This prevents event storms when many changes occur rapidly (e.g., git checkout, npm install).

- **Batching** — Collects multiple changes into a single `FileSystemChangeBatchEventArgs` event. Uses `ConcurrentDictionary` for thread-safe accumulation with automatic deduplication of rapid changes to the same file.

- **Ignore Patterns** — Configurable glob-style patterns to filter unwanted events:
    - Default patterns: `.git`, `.svn`, `.hg`, `node_modules`, `__pycache__`, `.DS_Store`, `Thumbs.db`, `*.tmp`, `*.temp`, `~$*`
    - Supports extension matching (`*.ext`), prefix matching (`prefix*`), and exact segment matching (`node_modules`)
    - Patterns can be dynamically added/removed via `IgnorePatterns` property

- **Buffer Overflow Detection** — Detects when `System.IO.FileSystemWatcher`'s internal buffer overflows and raises the `BufferOverflow` event. This signals to consumers that a full directory rescan may be needed.

- **Error Recovery** — Automatically attempts to restart the watcher on errors (up to 3 recovery attempts with 1-second delays). Distinguishes between recoverable errors and buffer overflows.

- **Cross-Platform Path Normalization** — Uses `Path.GetFullPath()` for consistent path handling and normalizes path separators for pattern matching.

### MediatR Event Integration

- **ExternalFileChangesEvent** — New MediatR notification published when file changes are detected:
    - `Changes` — Read-only list of `FileSystemChangeInfo` describing each change
    - Enables cross-module notification without tight coupling

### WorkspaceService Integration

- Updated to subscribe to `ChangesDetected` event from the file watcher
- Publishes `ExternalFileChangesEvent` via MediatR when changes are detected
- Maintains error event handling for workspace-level notifications

---

## Files Added

### Lexichord.Modules.Workspace

| File                                                                  | Description                                                               |
| :-------------------------------------------------------------------- | :------------------------------------------------------------------------ |
| `src/Lexichord.Modules.Workspace/Services/RobustFileSystemWatcher.cs` | Full watcher implementation with debouncing, batching, and error recovery |

### Unit Tests

| File                                                                           | Description                                                   |
| :----------------------------------------------------------------------------- | :------------------------------------------------------------ |
| `tests/Lexichord.Tests.Unit/Modules/Workspace/RobustFileSystemWatcherTests.cs` | 27 unit tests covering watcher lifecycle                      |
| Same file                                                                      | `IgnorePatternTests` class with 22 tests for pattern matching |

---

## Files Modified

### Lexichord.Abstractions

| File                                                   | Modification                                          |
| :----------------------------------------------------- | :---------------------------------------------------- |
| `src/Lexichord.Abstractions/Events/WorkspaceEvents.cs` | Added `ExternalFileChangesEvent` MediatR notification |

### Lexichord.Modules.Workspace

| File                                                                 | Modification                                                      |
| :------------------------------------------------------------------- | :---------------------------------------------------------------- |
| `src/Lexichord.Modules.Workspace/WorkspaceModule.cs`                 | Updated to register `RobustFileSystemWatcher` instead of stub     |
| `src/Lexichord.Modules.Workspace/Services/WorkspaceService.cs`       | Added `ChangesDetected` event subscription and MediatR publishing |
| `src/Lexichord.Modules.Workspace/Lexichord.Modules.Workspace.csproj` | Added `InternalsVisibleTo` for test access                        |

---

## Configuration

### Debounce Delay

The debounce delay can be configured via the `DebounceDelayMs` property:

```csharp
fileWatcher.DebounceDelayMs = 200; // 200ms delay
```

### Ignore Patterns

Patterns can be added or removed dynamically:

```csharp
// Add custom pattern
fileWatcher.IgnorePatterns.Add("*.log");

// Remove default pattern
fileWatcher.IgnorePatterns.Remove(".git");

// Clear all patterns
fileWatcher.IgnorePatterns.Clear();
```

Pattern matching supports:

- `*.ext` — Files ending with extension
- `prefix*` — Files starting with prefix
- `dirname` — Any path segment matching exactly

---

## Architecture

### Event Flow

```
System.IO.FileSystemWatcher
        │
        ▼ (Created/Changed/Deleted/Renamed)
RobustFileSystemWatcher
        │
        ├─── ShouldIgnore() check
        │
        ▼ (if not ignored)
ConcurrentDictionary buffer
        │
        ▼ (after debounce timer)
ChangesDetected event
        │
        ▼
WorkspaceService
        │
        ▼
ExternalFileChangesEvent (MediatR)
        │
        ▼
Subscribed handlers (Project Explorer, etc.)
```

### Error Handling Flow

```
FileSystemWatcher.Error
        │
        ▼
InternalBufferOverflowException?
        │
        ├─── Yes ───> BufferOverflow event (consumer should rescan)
        │
        └─── No ────> TryRecoverWatcher()
                        │
                        ├─── Cleanup + Wait + Restart
                        │
                        └─── Error event (with IsRecoverable)
```

---

## Test Coverage

### RobustFileSystemWatcherTests (27 tests)

- **Initial State (4):** Constructor defaults, debounce delay, ignore patterns, watch state
- **StartWatching (7):** Valid path, non-existent path, null/empty paths, switching watchers, post-dispose
- **StopWatching (3):** State cleanup, path clearing, idempotent behavior
- **Dispose (2):** Stops watching, multiple calls safe
- **Configuration (3):** Debounce delay modification, pattern modification, pattern clearing

### IgnorePatternTests (22 tests)

- **Extension Patterns (5):** `.tmp`, `.temp`, normal extensions
- **Prefix Patterns (4):** `~$*` prefix matching
- **Directory Patterns (6):** `.git`, `node_modules`, `__pycache__`, VCS directories
- **File Patterns (4):** `.DS_Store`, `Thumbs.db`
- **Custom Patterns (3):** Adding/removing patterns
- **Edge Cases (4):** Empty paths, null paths, normal paths

---

## Deprecations

- **StubFileSystemWatcher** — Now unused but retained for reference. Will be removed in a future cleanup version.

---

## Breaking Changes

None. This is a backward-compatible implementation of the `IFileSystemWatcher` interface.

---

## Related Documents

| Document       | Description                                  |
| :------------- | :------------------------------------------- |
| `LCS-DES-012b` | Design specification for file system watcher |
| `LCS-SBD-012`  | Scope breakdown for Explorer module          |
| `LCS-CL-012a`  | Previous changelog defining the interface    |

---

## Verification

```bash
# Build solution
dotnet build

# Run all file watcher tests
dotnet test --filter "FullyQualifiedName~RobustFileSystemWatcher|FullyQualifiedName~IgnorePattern"

# Run full test suite
dotnet test
```

---

## Next Steps

- **v0.1.2c:** Project Explorer View — Tree-based project file browser
- **v0.1.2d:** Project Explorer behaviors — Expand/collapse, selection, context menus
