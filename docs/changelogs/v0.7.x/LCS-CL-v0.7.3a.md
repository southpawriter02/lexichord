# Changelog: v0.7.3a — EditorViewModel Integration

**Feature ID:** EDT-073a
**Version:** 0.7.3a
**Date:** 2026-02-14
**Status:** ✅ Complete

---

## Overview

Introduces the Editor Agent's context menu integration, enabling writers to select text, right-click, and access AI-powered rewriting options. This is the first sub-part of v0.7.3 "The Editor Agent" and establishes the UI integration pattern that subsequent sub-parts (v0.7.3b-d) will build upon.

The implementation provides four rewrite options: **Rewrite Formally**, **Simplify**, **Expand**, and **Custom Rewrite**. Each option is gated by WriterPro license tier and requires an active text selection. The architecture uses MediatR events to decouple the context menu provider from the rewrite handler (implemented in v0.7.3b).

---

## What's New

### Rewrite Intent Enum

Added `RewriteIntent` enum in `Lexichord.Abstractions.Agents.Editor`:
- **Formal** — Transform casual text to formal, professional tone
- **Simplified** — Simplify text for a broader audience
- **Expanded** — Expand text with more detail and explanation
- **Custom** — User-provided transformation instruction (opens dialog)

### RewriteCommandOption Record

Added `RewriteCommandOption` record with:
- `CommandId` — Unique kebab-case identifier (e.g., "rewrite-formal")
- `DisplayName` — Menu item text (e.g., "Rewrite Formally")
- `Description` — Tooltip description
- `Icon` — Icon identifier from the icon library
- `KeyboardShortcut` — Optional shortcut display text (e.g., "Ctrl+Shift+R")
- `Intent` — The `RewriteIntent` this command triggers
- `OpensDialog` — Whether this command opens a dialog
- `Validate()` — Validates command option correctness

### IEditorAgentContextMenuProvider Interface

New interface for context menu integration:
- `GetRewriteMenuItems()` — Returns the four predefined rewrite options
- `CanRewrite` — Combined check of `HasSelection` and `IsLicensed`
- `HasSelection` — Whether the editor has an active text selection
- `IsLicensed` — Whether the user has WriterPro or higher tier
- `CanRewriteChanged` — Event raised when `CanRewrite` changes
- `ExecuteRewriteAsync()` — Executes a rewrite command

### EditorAgentContextMenuProvider Implementation

Singleton service that:
- Maintains the static list of four rewrite command options
- Subscribes to `IEditorService.SelectionChanged` events
- Subscribes to `ILicenseContext.LicenseChanged` events
- Publishes MediatR events for decoupled command execution
- Implements `IDisposable` for proper event cleanup

### MediatR Events

Three new events for decoupled communication:

| Event | Purpose |
|:------|:--------|
| `RewriteRequestedEvent` | Carries intent, selected text, span, document path, custom instruction |
| `ShowUpgradeModalEvent` | Triggers upgrade modal when user lacks license |
| `ShowCustomRewriteDialogEvent` | Opens dialog for custom transformation instruction |

### RewriteCommandViewModel

MVVM ViewModel with:
- `RewriteFormallyCommand` — Async RelayCommand for formal rewrite
- `SimplifyCommand` — Async RelayCommand for simplification
- `ExpandCommand` — Async RelayCommand for expansion
- `CustomRewriteCommand` — Async RelayCommand for custom rewrite dialog
- `IsExecuting` — Tracks execution state
- `Progress` / `ProgressMessage` — Progress indicators
- `CanRewrite` / `IsLicensed` — State from provider

### RewriteKeyboardShortcuts

Keyboard bindings for rewrite commands:
| Gesture | Command |
|:--------|:--------|
| `Ctrl+Shift+R` | Rewrite Formally |
| `Ctrl+Shift+S` | Simplify |
| `Ctrl+Shift+E` | Expand |
| `Ctrl+Shift+C` | Custom Rewrite |

All bindings use `EditorHasSelection` context condition.

### Feature Code

Added `FeatureCodes.EditorAgent` constant for license gating:
```csharp
public const string EditorAgent = "Feature.EditorAgent";
```

---

## Files Created

### Abstractions (2 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Abstractions/Agents/Editor/RewriteIntent.cs` | Enum | Formal, Simplified, Expanded, Custom intents |
| `src/Lexichord.Abstractions/Agents/Editor/RewriteCommandOption.cs` | Record | Command option metadata with validation |

### Events (3 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Modules.Agents/Editor/Events/RewriteRequestedEvent.cs` | INotification | Carries rewrite request data |
| `src/Lexichord.Modules.Agents/Editor/Events/ShowUpgradeModalEvent.cs` | INotification | Triggers license upgrade modal |
| `src/Lexichord.Modules.Agents/Editor/Events/ShowCustomRewriteDialogEvent.cs` | INotification | Opens custom instruction dialog |

### Core Implementation (4 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Modules.Agents/Editor/IEditorAgentContextMenuProvider.cs` | Interface | Context menu provider contract |
| `src/Lexichord.Modules.Agents/Editor/EditorAgentContextMenuProvider.cs` | Class | Main implementation |
| `src/Lexichord.Modules.Agents/Editor/RewriteCommandViewModel.cs` | ViewModel | MVVM commands and state |
| `src/Lexichord.Modules.Agents/Editor/RewriteKeyboardShortcuts.cs` | Class | IKeyBindingConfiguration |

### Extensions (1 file)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Modules.Agents/Extensions/EditorAgentServiceCollectionExtensions.cs` | Extension | AddEditorAgentContextMenu() DI registration |

### Tests (2 files)

| File | Tests | Description |
|:-----|:-----:|:------------|
| `tests/Lexichord.Tests.Unit/Modules/Agents/Editor/EditorAgentContextMenuProviderTests.cs` | 18 | Provider behavior |
| `tests/Lexichord.Tests.Unit/Modules/Agents/Editor/RewriteCommandViewModelTests.cs` | 16 | ViewModel behavior |

---

## Files Modified

| File | Changes |
|:-----|:--------|
| `src/Lexichord.Abstractions/Constants/FeatureCodes.cs` | Added `EditorAgent` constant in new region |
| `src/Lexichord.Modules.Agents/AgentsModule.cs` | Added `services.AddEditorAgentContextMenu()`, updated version to 0.7.3, added initialization verification |

---

## Testing

### Test Summary

| Test File | Tests | Key Coverage |
|:----------|:-----:|:-------------|
| EditorAgentContextMenuProviderTests | 18 | Menu items, license gating, selection state, events |
| RewriteCommandViewModelTests | 16 | Commands, execution state, dispose behavior |
| **Total** | **34** | All v0.7.3a functionality |

### Test Groups

| Group | Tests | Description |
|:------|:------|:------------|
| Menu Items | 6 | Count, intents, properties, validation |
| License Gating | 3 | WriterPro, Core, Teams tier behavior |
| Selection State | 2 | Initial state, editor has selection |
| CanRewrite | 2 | Combined check, both conditions met |
| Events | 2 | CanRewriteChanged on selection/license change |
| Execute Rewrite | 4 | Upgrade modal, formal/custom events, no selection |
| ViewModel State | 5 | Initial state, menu items delegation |
| Commands | 4 | All four commands delegate to provider |
| CanExecute | 2 | Commands disabled/enabled based on CanRewrite |
| IsExecuting | 2 | State during execution, progress message |
| Event Handling | 2 | State updates on CanRewriteChanged |

### Test Traits

All tests use: `[Trait("Category", "Unit")]`, `[Trait("SubPart", "v0.7.3a")]`

---

## Design Decisions

1. **MediatR Decoupling** — The spec references `IRewriteCommandHandler` as a dependency, but this interface is part of v0.7.3b. To avoid circular dependencies, v0.7.3a uses MediatR events (`RewriteRequestedEvent`) to decouple the context menu provider from the command handler.

2. **No ContextMenuItem.Children** — The existing `ContextMenuItem` class lacks submenu support. Instead of modifying the class, rewrite items are registered in the same group ("AI Rewrite") with sequential Order values.

3. **Singleton Provider** — `EditorAgentContextMenuProvider` is registered as singleton to maintain event subscriptions (SelectionChanged, LicenseChanged) across the application lifetime.

4. **Transient ViewModel** — `RewriteCommandViewModel` is registered as transient for per-view isolation of IsExecuting and progress state.

5. **Static Rewrite Options** — The four rewrite options are defined as a static readonly list. Adding new options requires code changes, but this ensures compile-time validation of command IDs and keyboard shortcuts.

---

## Dependencies

### Consumed (from existing modules)

| Interface | Version | Used By |
|:----------|:--------|:--------|
| `IEditorService` | v0.6.7a | EditorAgentContextMenuProvider (selection state) |
| `ILicenseContext` | v0.0.4c | EditorAgentContextMenuProvider (tier checking) |
| `IMediator` | v0.0.7a | EditorAgentContextMenuProvider (event publishing) |
| `IKeyBindingService` | v0.6.7a | RewriteKeyboardShortcuts (binding registration) |
| `TextSpan` | v0.6.7b | Events (selection span) |
| `ContextMenuItem` | v0.6.7a | Pattern reference (not directly used yet) |

### Produced (new interfaces)

| Interface | Consumers |
|:----------|:----------|
| `IEditorAgentContextMenuProvider` | v0.7.3b RewriteCommandHandler |
| `RewriteRequestedEvent` | v0.7.3b RewriteCommandHandler |
| `ShowUpgradeModalEvent` | Shell/Dialog service |
| `ShowCustomRewriteDialogEvent` | Shell/Dialog service |

### No New NuGet Packages

All dependencies are existing project references:
- `CommunityToolkit.Mvvm` (existing)
- `MediatR` (existing)
- `Avalonia` (existing)

---

## Known Limitations

1. **No actual rewrite execution** — v0.7.3a only publishes events. The actual rewrite logic is implemented in v0.7.3b (Command Handler).

2. **No context menu registration call** — The provider is registered in DI but `IEditorService.RegisterContextMenuItem()` is not called in this sub-part. This will be wired in the shell module during v0.7.3 completion.

3. **No upgrade modal handler** — `ShowUpgradeModalEvent` is published but no handler exists yet. The shell module will implement the handler.

4. **No custom dialog handler** — `ShowCustomRewriteDialogEvent` is published but no handler exists yet. The shell module will implement the handler.

---

## v0.7.3 Sub-Part Status

| Sub-Part | Title | Status |
|:---------|:------|:-------|
| **v0.7.3a** | **EditorViewModel Integration** | **✅ Complete** |
| **v0.7.3b** | **Agent Command Pipeline** | **✅ Complete** |
| v0.7.3c | Streaming Rewrite UI | ⏳ Pending |
| v0.7.3d | History & Undo | ⏳ Pending |

---

## Migration Notes

No breaking changes. This sub-part adds new functionality without modifying existing APIs.
