# v0.1.3a: AvalonEdit Integration

**Specification**: LCS-DES-013a (Editor Module Design) / LCS-SBD-013 (Scope Breakdown)  
**Released**: 2025-01 (Alpha)  
**Breaking Changes**: None

---

## Overview

Implementation of the core AvalonEdit integration for the Editor module, providing a high-performance text editing experience. This version establishes the foundation for manuscript editing with syntax highlighting, search, and configuration features to follow in subsequent releases.

---

## New Files

### Abstractions Layer

| File                                              | Description                                                                                              |
| ------------------------------------------------- | -------------------------------------------------------------------------------------------------------- |
| `Contracts/Editor/IEditorService.cs`              | Document lifecycle management interface                                                                  |
| `Contracts/Editor/IEditorConfigurationService.cs` | Editor settings access interface (stub)                                                                  |
| `Contracts/Editor/IManuscriptViewModel.cs`        | Contract for manuscript ViewModels                                                                       |
| `Contracts/Editor/EditorRecords.cs`               | Supporting records: `CaretPosition`, `TextSelection`, `EditorSettings`, `EditorSettingsChangedEventArgs` |

### Editor Module

| File                                     | Description                                            |
| ---------------------------------------- | ------------------------------------------------------ |
| `Lexichord.Modules.Editor.csproj`        | New project file with Avalonia.AvaloniaEdit dependency |
| `EditorModule.cs`                        | `IModule` implementation for service registration      |
| `Views/ManuscriptView.axaml`             | XAML view wrapping AvaloniaEdit TextEditor             |
| `Views/ManuscriptView.axaml.cs`          | Code-behind with text sync, caret tracking, shortcuts  |
| `ViewModels/ManuscriptViewModel.cs`      | Document ViewModel extending `DocumentViewModelBase`   |
| `Services/EditorService.cs`              | Document open/create/save/close lifecycle              |
| `Services/EditorConfigurationService.cs` | Stub configuration service with defaults               |

### Tests

| File                                         | Description                           |
| -------------------------------------------- | ------------------------------------- |
| `Modules/Editor/ManuscriptViewModelTests.cs` | 15 unit tests for ManuscriptViewModel |
| `Modules/Editor/EditorServiceTests.cs`       | 14 unit tests for EditorService       |

---

## New Contracts

### IEditorService

```csharp
public interface IEditorService
{
    Task<IManuscriptViewModel> OpenDocumentAsync(string filePath);
    Task<IManuscriptViewModel> CreateDocumentAsync(string? title = null);
    Task<bool> SaveDocumentAsync(IManuscriptViewModel document);
    Task<bool> CloseDocumentAsync(IManuscriptViewModel document);
    IReadOnlyList<IManuscriptViewModel> GetOpenDocuments();
    IManuscriptViewModel? GetDocumentByPath(string filePath);
}
```

### IManuscriptViewModel

Extends `IDocumentTab` with:

- `FilePath`, `FileExtension`, `Content`, `Encoding`
- `CaretPosition`, `Selection`
- `LineCount`, `CharacterCount`, `WordCount`
- Editor settings bindings (line numbers, word wrap, font)
- Commands: `SaveCommand`, `ShowSearchCommand`, `HideSearchCommand`, `GoToLineCommand`

### IEditorConfigurationService

```csharp
public interface IEditorConfigurationService
{
    EditorSettings GetSettings();
    Task SaveSettingsAsync(EditorSettings settings);
    Task LoadSettingsAsync();
    event EventHandler<EditorSettingsChangedEventArgs>? SettingsChanged;
}
```

---

## Implementation Details

### EditorModule Registration

```csharp
public void RegisterServices(IServiceCollection services)
{
    services.AddSingleton<IEditorConfigurationService, EditorConfigurationService>();
    services.AddSingleton<IEditorService, EditorService>();
    services.AddTransient<ManuscriptViewModel>();
}
```

### ManuscriptViewModel Features

- **Extends `DocumentViewModelBase`**: Inherits dirty state, save dialogs, pinning support
- **Two-way Content Binding**: Synchronizes with AvaloniaEdit TextEditor
- **Settings Reactivity**: Subscribes to `SettingsChanged` events for live updates
- **Document Statistics**: Real-time line/word/character counts
- **Selection Tracking**: Updates `CaretPosition` and `TextSelection` records

### ManuscriptView Features

- Hosts AvaloniaEdit `TextEditor` control
- Keyboard shortcuts: Ctrl+S (save), Ctrl+F (search), Ctrl+G (go to line)
- Text synchronization with loop prevention
- Caret and selection event forwarding

### EditorService Features

- **File I/O**: UTF-8 encoding detection, async read/write
- **Document Tracking**: Thread-safe open document list with locking
- **Deduplication**: Returns existing ViewModel for already-open files
- **Untitled Counter**: Auto-incrementing "Untitled-N" titles

---

## Configuration

### Default EditorSettings

| Setting              | Default                              |
| -------------------- | ------------------------------------ |
| FontFamily           | "Cascadia Code, Consolas, monospace" |
| FontSize             | 14.0                                 |
| ShowLineNumbers      | true                                 |
| WordWrap             | true                                 |
| HighlightCurrentLine | true                                 |
| ShowWhitespace       | false                                |
| ShowEndOfLine        | false                                |
| UseSpacesForTabs     | true                                 |
| IndentSize           | 4                                    |

---

## Test Summary

| Test Class               | Tests  | Status        |
| ------------------------ | ------ | ------------- |
| ManuscriptViewModelTests | 15     | ✅ Passed     |
| EditorServiceTests       | 14     | ✅ Passed     |
| **Total**                | **29** | ✅ All Passed |

### Coverage Areas

- Document initialization and property setting
- Content change and dirty state tracking
- Word/line/character counting
- Settings binding reactivity
- Selection and caret position updates
- File open/create/save/close lifecycle
- Document deduplication

---

## Dependencies

| Dependency             | Version | Purpose                              |
| ---------------------- | ------- | ------------------------------------ |
| Avalonia.AvaloniaEdit  | 11.1.0  | High-performance text editor control |
| CommunityToolkit.Mvvm  | 8.4.0   | MVVM source generators               |
| Lexichord.Abstractions | -       | Core contracts and base classes      |

---

## Deferred Features

| Feature              | Target Version |
| -------------------- | -------------- |
| Syntax highlighting  | v0.1.3b        |
| Search overlay       | v0.1.3c        |
| Settings persistence | v0.1.3d        |

---

## Migration Guide

No migration required - this is the initial Editor module implementation.

---

## Related Specifications

- [LCS-DES-013a: Editor Module Design](../specs/LCS-DES-013a.md)
- [LCS-DES-013-INDEX: Editor Design Index](../specs/LCS-DES-013-INDEX.md)
- [LCS-SBD-013: v0.1.3 Scope Breakdown](../specs/LCS-SBD-013.md)
