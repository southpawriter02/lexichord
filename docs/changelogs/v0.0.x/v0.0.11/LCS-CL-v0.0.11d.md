# LCS-CL-011d: Tab Infrastructure

**Version**: v0.1.1d  
**Codename**: Tab Infrastructure  
**Date**: 2026-01-28

---

## Summary

This release implements the tab infrastructure layer for Lexichord's docking system. It provides core abstractions and services for managing document tabs including dirty state tracking, pinning, close confirmation workflows, and tab coordination.

---

## New Interfaces & Types

### Lexichord.Abstractions.Layout

| Type                            | Description                                                                                  |
| ------------------------------- | -------------------------------------------------------------------------------------------- |
| `IDocumentTab`                  | Contract for tab-aware document ViewModels with dirty state, pinning, and close confirmation |
| `ITabService`                   | High-level tab coordination service for close operations, pinning, and tab ordering          |
| `SaveDialogResult`              | Enum for save confirmation dialog results (Save, DontSave, Cancel)                           |
| `DocumentStateChangedEventArgs` | Record for state change notifications                                                        |

### Lexichord.Abstractions.ViewModels

| Type                    | Description                                                        |
| ----------------------- | ------------------------------------------------------------------ |
| `DocumentViewModelBase` | Abstract base class implementing `IDocumentTab` with MVVM bindings |

### Lexichord.Abstractions.Services

| Type                 | Description                                             |
| -------------------- | ------------------------------------------------------- |
| `ISaveDialogService` | Service interface for showing save confirmation dialogs |

### Lexichord.Abstractions.Messaging

| Type                          | Description                                      |
| ----------------------------- | ------------------------------------------------ |
| `CloseDocumentCommand`        | MediatR command to close a document              |
| `CloseAllDocumentsCommand`    | MediatR command to close all documents           |
| `CloseAllButThisCommand`      | MediatR command to close all except one document |
| `CloseToTheRightCommand`      | MediatR command to close documents to the right  |
| `PinDocumentCommand`          | MediatR command to pin/unpin a document          |
| `DocumentClosingNotification` | Published before a document closes               |
| `DocumentClosedNotification`  | Published after a document closes                |
| `DocumentPinnedNotification`  | Published when pin state changes                 |
| `DocumentDirtyNotification`   | Published when dirty state changes               |

---

## Implementation Details

### DocumentViewModelBase

The base class simplifies document ViewModel creation:

```csharp
public abstract partial class DocumentViewModelBase : ObservableObject, IDocumentTab
{
    // Auto-updating DisplayTitle with asterisk when dirty
    public virtual string DisplayTitle => IsDirty ? $"{Title}*" : Title;

    // CanCloseAsync with save dialog integration
    public virtual async Task<bool> CanCloseAsync()
    {
        if (!IsDirty) return true;
        if (_saveDialogService == null) return false;

        var result = await _saveDialogService.ShowSaveDialogAsync(Title);
        return result switch
        {
            SaveDialogResult.Save => await SaveAsync(),
            SaveDialogResult.DontSave => true,
            SaveDialogResult.Cancel => false,
            _ => false
        };
    }

    // StateChanged event for UI/service coordination
    protected virtual void OnStateChanged(string propertyName, object? oldValue, object? newValue)
    {
        StateChanged?.Invoke(this, new DocumentStateChangedEventArgs(propertyName, oldValue, newValue));
    }
}
```

### TabService

Coordinates tab operations between documents and the region manager:

- **Close operations**: Single, all, all-but-this, to-the-right
- **Pin management**: Updates pin state and reorders tabs
- **Tab ordering**: Pinned documents appear first
- **MediatR integration**: Publishes lifecycle notifications
- **Thread-safe**: Uses locking for collection access

### Save Confirmation Dialog

Avalonia dialog with Save, Don't Save, and Cancel buttons:

```
┌──────────────────────────────────────────┐
│ Do you want to save changes to           │
│ Chapter 1.md                             │
│                                          │
│          [Don't Save] [Cancel] [Save]    │
└──────────────────────────────────────────┘
```

### Tab Context Menu

Standard IDE-style context menu with actions:

- Close (Ctrl+W)
- Close All
- Close All But This
- Close to the Right
- Pin/Unpin
- Copy Path (file-based documents)
- Reveal in Explorer (file-based documents)

---

## Modified Files

### Lexichord.Abstractions

| File                            | Change                                          |
| ------------------------------- | ----------------------------------------------- |
| `Lexichord.Abstractions.csproj` | Added `CommunityToolkit.Mvvm` package reference |

### Lexichord.Host

| File              | Change                                          |
| ----------------- | ----------------------------------------------- |
| `HostServices.cs` | Register `ITabService` and `ISaveDialogService` |

---

## New Files

### Lexichord.Abstractions

- `Layout/IDocumentTab.cs`
- `Layout/ITabService.cs`
- `Layout/SaveDialogResult.cs`
- `Layout/DocumentStateChangedEventArgs.cs`
- `Services/ISaveDialogService.cs`
- `ViewModels/DocumentViewModelBase.cs`
- `Messaging/TabCommands.cs`
- `Messaging/TabNotifications.cs`

### Lexichord.Host

- `Layout/TabService.cs`
- `Services/SaveDialogService.cs`
- `Views/SaveConfirmationDialog.axaml`
- `Views/SaveConfirmationDialog.axaml.cs`
- `ViewModels/SaveConfirmationViewModel.cs`
- `Views/TabContextMenu.axaml`
- `Views/TabContextMenu.axaml.cs`

---

## Unit Tests

### DocumentViewModelBaseTests (11 tests)

| Test                                                                    | Description                         |
| ----------------------------------------------------------------------- | ----------------------------------- |
| `DisplayTitle_WhenClean_ReturnsTitle`                                   | Verifies clean title display        |
| `DisplayTitle_WhenDirty_ReturnsTitleWithAsterisk`                       | Verifies asterisk suffix when dirty |
| `MarkDirty_SetsIsDirtyTrue`                                             | Verifies dirty marking              |
| `MarkClean_SetsIsDirtyFalse`                                            | Verifies clean marking              |
| `CanClose_ByDefault_ReturnsTrue`                                        | Verifies default closability        |
| `CanCloseAsync_WhenClean_ReturnsTrue`                                   | Clean documents close immediately   |
| `CanCloseAsync_WhenDirtyAndNoDialogService_ReturnsFalse`                | Blocks close without dialog service |
| `CanCloseAsync_WhenDirtyAndUserClicksSave_CallsSaveAndReturnsResult`    | Save flow                           |
| `CanCloseAsync_WhenDirtyAndUserClicksSaveButSaveFails_ReturnsFalse`     | Failed save flow                    |
| `CanCloseAsync_WhenDirtyAndUserClicksDontSave_ReturnsTrueWithoutSaving` | Discard flow                        |
| `CanCloseAsync_WhenDirtyAndUserClicksCancel_ReturnsFalse`               | Cancel flow                         |

### TabServiceTests (20 tests)

| Test                                                              | Description               |
| ----------------------------------------------------------------- | ------------------------- |
| `CloseDocumentAsync_WhenDocumentNotRegistered_ReturnsFalse`       | Unknown document handling |
| `CloseDocumentAsync_WhenDocumentIsClean_ClosesAndReturnsTrue`     | Clean close flow          |
| `CloseDocumentAsync_WhenDocumentCanCloseReturnsFalse_abortsClose` | Cancelled close           |
| `CloseDocumentAsync_WhenForced_BypassesCanCloseAsync`             | Force close               |
| `CloseAllDocumentsAsync_ClosesAllDocuments`                       | Close all flow            |
| `CloseAllDocumentsAsync_WhenSkipPinned_SkipsPinnedDocuments`      | Skip pinned               |
| `CloseAllButThisAsync_ClosesAllExceptSpecified`                   | Close all but this        |
| `CloseToTheRightAsync_ClosesDocumentsAfterSpecified`              | Close to right            |
| `PinDocumentAsync_SetsIsPinnedAndPublishesNotification`           | Pin operation             |
| `GetDirtyDocumentIds_ReturnsOnlyDirtyDocuments`                   | Dirty tracking            |
| `HasUnsavedChanges_WhenNoDirtyDocuments_ReturnsFalse`             | No unsaved check          |
| `HasUnsavedChanges_WhenAnyDirtyDocument_ReturnsTrue`              | Has unsaved check         |
| `GetTabOrder_ReturnsPinnedDocumentsFirst`                         | Tab ordering              |
| `GetDocument_WhenExists_ReturnsDocument`                          | Document lookup           |
| `GetDocument_WhenNotExists_ReturnsNull`                           | Missing document          |
| `RegisterDocument_WhenAlreadyRegistered_DoesNotDuplicate`         | Duplicate prevention      |
| `UnregisterDocument_RemovesDocumentFromTracking`                  | Unregistration            |

---

## Verification Commands

```bash
# Build
dotnet build

# Run tab infrastructure tests
dotnet test --filter "FullyQualifiedName~DocumentViewModelBaseTests|FullyQualifiedName~TabServiceTests"

# Run all tests
dotnet test
```

---

## Deferred to v0.1.1e

The following features were scoped out to reduce complexity:

- **Drag-and-drop reordering**: Requires `Dock.Avalonia` DragDrop integration
- **Tear-out to floating windows**: Complex window management with `IHostWindow`

These will be implemented in v0.1.1e: Advanced Tab Behaviors.
