# v0.1.2d: Context Menu Actions

> Part of [v0.1.2: The Explorer (Project Management)](LCS-DES-012-INDEX.md)

## Summary

Implements file and folder management capabilities for the Project Explorer via context menus and keyboard shortcuts. This version introduces the `IFileOperationService` abstraction for centralized file operations with validation, event publishing, and cross-platform support.

## New Features

### Context Menu

- **New File** (Ctrl+N): Creates a new file with inline name editing
- **New Folder** (Ctrl+Shift+N): Creates a new folder with inline name editing
- **Rename** (F2): Starts inline renaming of the selected item
- **Delete**: Deletes the selected file or folder with recursive support
- **Reveal in Explorer**: Opens the containing folder in the OS file explorer

### File Operations Service

- Centralized `IFileOperationService` for all file/folder modifications
- Comprehensive name validation:
    - Empty/whitespace checks
    - Illegal character detection (platform-specific)
    - Reserved name blocking (CON, PRN, NUL, COM1-9, LPT1-9)
    - Path separator and traversal prevention
    - Length limit enforcement (255 characters)
- Automatic unique name generation with numbered suffixes
- Protected path enforcement (.git folders, workspace root)
- Cross-platform "Reveal in Explorer" support (Windows/macOS/Linux)

### Event-Driven Architecture

- `FileCreatedEvent`: Published when files/folders are created
- `FileDeletedEvent`: Published when files/folders are deleted
- `FileRenamedEvent`: Published when files/folders are renamed

## Files Added

### Lexichord.Abstractions

| File                                 | Purpose                                    |
| ------------------------------------ | ------------------------------------------ |
| `Contracts/FileOperationError.cs`    | Error categories for file operations       |
| `Contracts/FileOperationResult.cs`   | Structured result type for file operations |
| `Contracts/NameValidationResult.cs`  | Result type for name validation            |
| `Contracts/IFileOperationService.cs` | Service interface for file operations      |
| `Events/FileCreatedEvent.cs`         | MediatR notification for file creation     |
| `Events/FileDeletedEvent.cs`         | MediatR notification for file deletion     |
| `Events/FileRenamedEvent.cs`         | MediatR notification for file renaming     |

### Lexichord.Modules.Workspace

| File                               | Purpose                                   |
| ---------------------------------- | ----------------------------------------- |
| `Services/FileOperationService.cs` | Implementation of `IFileOperationService` |

### Tests

| File                                             | Purpose                               |
| ------------------------------------------------ | ------------------------------------- |
| `Modules/Workspace/FileOperationServiceTests.cs` | Unit tests for `FileOperationService` |

## Files Modified

### Lexichord.Modules.Workspace

| File                                     | Changes                                        |
| ---------------------------------------- | ---------------------------------------------- |
| `WorkspaceModule.cs`                     | Added `IFileOperationService` registration     |
| `ViewModels/ProjectExplorerViewModel.cs` | Added context menu commands and helper methods |
| `Views/ProjectExplorerView.axaml`        | Added context menu and keyboard bindings       |
| `Views/ProjectExplorerView.axaml.cs`     | Updated keyboard handling and rename logic     |

### Tests

| File                                                 | Changes                            |
| ---------------------------------------------------- | ---------------------------------- |
| `Modules/Workspace/ProjectExplorerViewModelTests.cs` | Added `IFileOperationService` mock |

## Architecture

### File Operation Flow

```
User Action → ViewModel Command → FileOperationService
    ↓
Validation → Disk Operation → MediatR Event
    ↓
ProjectExplorerViewModel (via event handler) → Tree Update
```

### Protected Path Hierarchy

```
Workspace Root (cannot delete/rename)
├── .git/ (cannot delete/rename)
│   └── * (all contents protected)
└── Other files (modifiable)
```

## Test Coverage

### FileOperationServiceTests (30+ tests)

| Category           | Tests                                                                                                        |
| ------------------ | ------------------------------------------------------------------------------------------------------------ |
| ValidateName       | Empty, whitespace, illegal chars, reserved names, path separators, length                                    |
| GenerateUniqueName | No conflict, single conflict, multiple conflicts, folders                                                    |
| CreateFileAsync    | Success, event publishing, invalid name, parent not found, already exists                                    |
| CreateFolderAsync  | Success, event publishing, invalid name                                                                      |
| RenameAsync        | File/folder, event publishing, path not found, target exists, .git protection, same name                     |
| DeleteAsync        | File, empty folder, non-empty without recursive, non-empty with recursive, event publishing, protected paths |

## Keyboard Shortcuts

| Shortcut     | Action     |
| ------------ | ---------- |
| Ctrl+N       | New File   |
| Ctrl+Shift+N | New Folder |
| F2           | Rename     |
| Delete       | Delete     |

## Dependencies

- MediatR (event publishing)
- System.Diagnostics.Process (Reveal in Explorer)
- System.Runtime.InteropServices.RuntimeInformation (platform detection)

## Breaking Changes

None. This is an additive feature.

## Related Documents

- [LCS-DES-012d.md](../specs/v0.1.x/v0.1.2/LCS-DES-012d.md) - Design specification
- [LCS-SBD-012.md](../specs/v0.1.x/v0.1.2/LCS-SBD-012.md) - Scope breakdown

## Verification

1. Build succeeds with `dotnet build`
2. All 600+ unit tests pass with `dotnet test`
3. Context menu appears on right-click in Project Explorer
4. Keyboard shortcuts work as documented
5. File/folder operations update tree via events
