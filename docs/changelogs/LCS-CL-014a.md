# LCS-CL-014a: Dirty State Tracking

**Version**: v0.1.4a  
**Released**: 2026-01-29  
**Status**: ✅ Complete

---

## Overview

Implements visual feedback for unsaved document changes through the `IDirtyStateTracker` interface and debounced dirty state management.

---

## Changes

### New Files

| File                                               | Purpose                                                      |
| :------------------------------------------------- | :----------------------------------------------------------- |
| `Abstractions/Contracts/IDirtyStateTracker.cs`     | Interface defining dirty state tracking contract with events |
| `Abstractions/Events/DocumentDirtyChangedEvent.cs` | MediatR domain event for cross-module dirty state awareness  |

### Modified Files

| File                                                    | Changes                                                                                           |
| :------------------------------------------------------ | :------------------------------------------------------------------------------------------------ |
| `Editor/ViewModels/ManuscriptViewModel.cs`              | Implements `IDirtyStateTracker` with debouncing, SHA256 content hashing, MediatR event publishing |
| `Tests.Unit/Modules/Editor/ManuscriptViewModelTests.cs` | Added 9 dirty state tracking tests                                                                |

---

## Technical Details

### Debouncing

Content changes trigger a 50ms debounce timer. The `IsDirty` transition only occurs after the timer expires, preventing excessive state changes during rapid typing. Implementation uses `System.Timers.Timer` with `Dispatcher.UIThread.Post()` for thread-safe UI updates.

### Content Hashing

SHA256 hashes are computed for document content:

- Initial hash stored on document load
- Hash updated on each save operation
- `ContentMatchesLastSaved()` enables undo-to-saved detection

### Events

Two event mechanisms:

1. **`DirtyStateChanged`** — Local `EventHandler` for immediate subscribers
2. **`DocumentDirtyChangedEvent`** — MediatR `INotification` for cross-module awareness

---

## Test Coverage

| Test                                                      | Description                   |
| :-------------------------------------------------------- | :---------------------------- |
| `Initialize_SetsLastSavedContentHash`                     | Initial hash computed on load |
| `ContentMatchesLastSaved_AfterInitialize_ReturnsTrue`     | Hash comparison works         |
| `ContentMatchesLastSaved_AfterContentChange_ReturnsFalse` | Change detection works        |
| `ClearDirty_WithHash_UpdatesLastSavedContentHash`         | Save updates hash             |
| `ClearDirty_WithHash_SetsIsDirtyFalse`                    | Save clears dirty state       |
| `DirtyStateChanged_WhenClearDirtyCalled_EventRaised`      | Local event fires             |
| `DisplayTitle_WhenDirty_ShowsAsterisk`                    | Visual indicator works        |
| `DisplayTitle_WhenClean_NoAsterisk`                       | Clean state display           |
| `Dispose_DoesNotThrow`                                    | Resource cleanup works        |

---

## Dependencies

- v0.1.3 (Editor Module) — Provides `ManuscriptViewModel` base
- v0.0.7 (Event Bus) — Provides MediatR infrastructure
