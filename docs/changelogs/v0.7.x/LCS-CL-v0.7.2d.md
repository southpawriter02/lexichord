# Changelog: v0.7.2d — Context Preview Panel

**Feature ID:** CTX-072d
**Version:** 0.7.2d
**Date:** 2026-02-14
**Status:** ✅ Complete

---

## Overview

Implements the Context Preview Panel — a real-time UI panel that displays assembled context fragments, token budget usage, strategy toggle controls, and assembly duration. Bridges the orchestrator's MediatR event notifications to the Avalonia ViewModel layer via a Singleton event bridge pattern, completing the Context Assembler system (v0.7.2).

This sub-part builds upon v0.7.2c's orchestrator and its `ContextAssembledEvent`/`StrategyToggleEvent` MediatR notifications to deliver the user-facing transparency layer for the Context Assembler.

---

## What's New

### ContextPreviewBridge

MediatR notification handler bridge for ViewModel event forwarding:
- **Namespace:** `Lexichord.Modules.Agents.Context`
- **Pattern:** Singleton service implementing `INotificationHandler<ContextAssembledEvent>` and `INotificationHandler<StrategyToggleEvent>`
- **Events:** `ContextAssembled` and `StrategyToggled` C# events
- **Purpose:** Solves MediatR-to-Transient-ViewModel lifetime mismatch by re-publishing notifications as C# events that ViewModels subscribe/unsubscribe to in constructor/dispose
- EventId constants: 7100-7101

### FragmentViewModel

Per-fragment display ViewModel:
- **Namespace:** `Lexichord.Modules.Agents.Chat.ViewModels`
- **Base Class:** `ObservableObject` (CommunityToolkit.Mvvm)
- **Properties:** `SourceId`, `Label`, `FullContent`, `TokenCount`, `Relevance`, `TruncatedContent` (first 200 chars), `TokenCountText`, `RelevanceText`, `DisplayContent`, `NeedsExpansion`, `ExpandButtonText`, `SourceIcon`
- **Observable:** `IsExpanded` with `[NotifyPropertyChangedFor]` cascading to `DisplayContent` and `ExpandButtonText`
- **Commands:** `ToggleExpandedCommand`
- Source icon mapping: document→📄, selection→✂️, cursor→📍, heading→📑, rag→🔍, style→🎨, unknown→📋
- EventId constants: 7102-7103

### StrategyToggleItem

Strategy enable/disable toggle ViewModel:
- **Namespace:** `Lexichord.Modules.Agents.Chat.ViewModels`
- **Base Class:** `ObservableObject` (CommunityToolkit.Mvvm)
- **Properties:** `StrategyId`, `DisplayName`, `Tooltip` (strategy-specific descriptions)
- **Observable:** `IsEnabled` — triggers `onToggle` callback via `partial void OnIsEnabledChanged()`
- **Constructor:** Takes `(strategyId, displayName, isEnabled, onToggle, logger?)` with null guards
- Decoupled from `IContextOrchestrator` via callback delegate pattern
- EventId constant: 7104

### ContextPreviewViewModel

Main panel ViewModel orchestrating all sub-components:
- **Namespace:** `Lexichord.Modules.Agents.Chat.ViewModels`
- **Base Class:** `ObservableObject`, `IDisposable`
- **Dependencies:** `IContextOrchestrator`, `ContextPreviewBridge`, `ILogger<ContextPreviewViewModel>`, optional `Action<Action>` dispatch delegate
- **Observable Properties:** `IsAssembling`, `TotalTokens`, `TokenBudget` (default 8000), `AssemblyDuration`, `IsExpanded`
- **Collections:** `ObservableCollection<FragmentViewModel> Fragments`, `ObservableCollection<StrategyToggleItem> Strategies`
- **Computed:** `HasContext`, `BudgetPercentage` (capped at 1.0), `BudgetStatusText`, `DurationText`, `CombinedContent`
- **Commands:** `ToggleExpandedCommand`, `EnableAllCommand`, `DisableAllCommand`
- **Lifecycle:** Subscribes to bridge events in constructor, unsubscribes in `Dispose()`
- **Dispatch Pattern:** Injectable `Action<Action>` delegate (defaults to synchronous for testing, production uses `Dispatcher.UIThread.Post`)
- EventId constants: 7105-7109

### ContextPreviewView (AXAML)

Avalonia UserControl with compiled bindings:
- **Namespace:** `Lexichord.Modules.Agents.Chat.Views`
- **Layout:** Header (title + toggle), Token Budget (progress bar + status text + duration), Strategy Toggles (WrapPanel with CheckBoxes), Fragment List (scrollable ItemsControl with expandable cards), Loading Overlay, Footer (Enable All / Disable All)
- Uses `DynamicResource` theming: `SurfaceBrush`, `SurfaceElevatedBrush`, `BorderSubtleBrush`, `AccentBrush`
- Custom styles: `budget-bar`, `fragment-card`, `section-header`, `link-button`
- Monospace content display with `SelectableTextBlock`

---

## DI Registration

### AddContextPreviewPanel Extension Method

Added to `AgentsServiceCollectionExtensions` and called from `AddContextStrategies()`:
- `AddSingleton<ContextPreviewBridge>()` — Singleton for event forwarding
- `AddSingleton<INotificationHandler<ContextAssembledEvent>>(sp => sp.GetRequiredService<ContextPreviewBridge>())` — MediatR handler forwarding
- `AddSingleton<INotificationHandler<StrategyToggleEvent>>(sp => sp.GetRequiredService<ContextPreviewBridge>())` — MediatR handler forwarding
- `AddTransient<ContextPreviewViewModel>()` — Per-panel instance

### AgentsModule.RegisterServices() Update

Added `services.AddContextStrategies()` call to wire up the entire v0.7.2 Context Assembler pipeline (v0.7.2a factory + v0.7.2b strategies + v0.7.2c orchestrator + v0.7.2d preview panel).

---

## Files Changed

### New Files (8)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/.../Context/ContextPreviewBridge.cs` | Bridge | MediatR → ViewModel event bridge |
| `src/.../Chat/ViewModels/FragmentViewModel.cs` | ViewModel | Per-fragment display |
| `src/.../Chat/ViewModels/StrategyToggleItem.cs` | ViewModel | Strategy toggle control |
| `src/.../Chat/ViewModels/ContextPreviewViewModel.cs` | ViewModel | Main preview panel |
| `src/.../Chat/Views/ContextPreviewView.axaml` | View | AXAML layout |
| `src/.../Chat/Views/ContextPreviewView.axaml.cs` | Codebehind | View initialization |
| `tests/.../Context/ContextPreviewBridgeTests.cs` | Tests | 7 tests |
| `tests/.../Context/ContextPreviewViewModelTests.cs` | Tests | 23 tests |

### New Files (cont.)

| File | Type | Description |
|:-----|:-----|:------------|
| `tests/.../ViewModels/FragmentViewModelTests.cs` | Tests | 18 tests |
| `tests/.../ViewModels/StrategyToggleItemTests.cs` | Tests | 10 tests |

### Modified Files (2)

| File | Changes |
|:-----|:--------|
| `src/.../Extensions/AgentsServiceCollectionExtensions.cs` | Added `AddContextPreviewPanel()` method, MediatR using, called from `AddContextStrategies()` |
| `src/.../AgentsModule.cs` | Added `services.AddContextStrategies()` call in `RegisterServices()` |

---

## Spec-to-Codebase Adaptations

| Spec Reference | Actual Codebase | Adaptation |
|:---|:---|:---|
| `IEventBus.Subscribe()` | `ContextPreviewBridge` C# events | IEventBus doesn't exist; Singleton bridge pattern with MediatR `INotificationHandler<T>` |
| `IDispatcher.InvokeAsync()` | `Action<Action>` dispatch delegate | IDispatcher doesn't exist; injectable delegate defaults to sync for tests |
| `CompositeDisposable` | `IDisposable` with `_disposed` guard | Rx not used in codebase; standard dispose pattern |
| `ContextAssemblyStartedEvent` | Local `IsAssembling` property | Event doesn't exist; orchestrator only publishes completion |
| `ViewModelBase` | `ObservableObject` | ViewModelBase doesn't exist; CommunityToolkit.Mvvm pattern |
| `Lexichord.Presentation.ViewModels.Agents` | `Lexichord.Modules.Agents.Chat.ViewModels` | Actual project namespace |
| `Application.Current!.Clipboard!` | Clipboard deferred to future integration | View-level clipboard access requires `TopLevel.GetTopLevel(this)` |
| `[Trait("Version", ...)]` | `[Trait("SubPart", "v0.7.2d")]` | Codebase standard trait key |
| Moq test framework | NSubstitute | Codebase uses NSubstitute |

---

## Testing

### Test Summary

| Test File | Tests | Key Coverage |
|:----------|:-----:|:-------------|
| ContextPreviewBridgeTests | 7 | Constructor null guard, event firing, no subscribers, multiple subscribers |
| ContextPreviewViewModelTests | 23 | Constructor null guards, initial state, strategy population, bridge events, fragments, budget, commands, dispatch, dispose, PropertyChanged |
| FragmentViewModelTests | 18 | Constructor, properties, truncation, expansion, icons, PropertyChanged |
| StrategyToggleItemTests | 10 | Constructor null guards, toggle callback, same-value skip, tooltips |
| **Total** | **58** | All v0.7.2d tests |

### Test Traits

All new tests use: `[Trait("Category", "Unit")]`, `[Trait("SubPart", "v0.7.2d")]`

---

## Design Decisions

1. **Singleton Bridge Pattern** — MediatR handlers are resolved per-notification. Transient ViewModels would get new instances per event. The bridge receives all notifications as a singleton and re-publishes via C# events that the existing ViewModel instance subscribes to.

2. **Injectable Dispatch Delegate** — Instead of a non-existent `IDispatcher`, uses `Action<Action>` with a default synchronous invocation for unit testing. Production code passes `Dispatcher.UIThread.Post`.

3. **Callback-based Strategy Toggle** — `StrategyToggleItem` takes an `Action<string, bool>` callback rather than a direct `IContextOrchestrator` reference. This decouples the child ViewModel from the orchestrator and allows the parent ViewModel to control toggle behavior.

4. **No Clipboard in ViewModel** — The spec's `Application.Current!.Clipboard!` was not implemented in the ViewModel layer. Clipboard access in Avalonia requires a `TopLevel` reference, which is a View-layer concern. The `CombinedContent` property provides the formatted string for future View-level clipboard integration.

5. **Fire-and-forget Strategy Sync** — When `OnStrategyToggled` updates a `StrategyToggleItem.IsEnabled`, the toggle callback re-calls `SetStrategyEnabled()` on the orchestrator. This is acceptable because `SetStrategyEnabled()` is idempotent.

6. **Internal Visibility** — All new types are `internal sealed` following the codebase pattern for module-specific implementations. Only `IContextOrchestrator` (in Abstractions) is public.

---

## Dependencies

### Consumed (from existing modules)

| Interface | Module | Used By |
|:----------|:-------|:--------|
| `IContextOrchestrator` | Lexichord.Abstractions | ContextPreviewViewModel (strategy management) |
| `ContextAssembledEvent` | Lexichord.Modules.Agents | ContextPreviewBridge (event handling) |
| `StrategyToggleEvent` | Lexichord.Modules.Agents | ContextPreviewBridge (event handling) |
| `ContextFragment` | Lexichord.Abstractions | FragmentViewModel (data display) |
| `IMediator` (MediatR) | MediatR | ContextPreviewBridge (notification handling) |

### No New NuGet Packages

All dependencies are existing project references. MediatR, CommunityToolkit.Mvvm, and Avalonia were already referenced.

---

## EventId Registry

| EventId | Constant Name | Component | Description |
|:--------|:-------------|:----------|:------------|
| 7100 | EventIdContextAssembledReceived | ContextPreviewBridge | Context assembled notification forwarded |
| 7101 | EventIdStrategyToggleReceived | ContextPreviewBridge | Strategy toggle notification forwarded |
| 7102 | EventIdExpansionToggled | FragmentViewModel | Fragment expand/collapse toggled |
| 7103 | EventIdContentCopied | FragmentViewModel | (Reserved for future clipboard) |
| 7104 | EventIdToggleChanged | StrategyToggleItem | Strategy enabled/disabled |
| 7105 | EventIdInitialized | ContextPreviewViewModel | ViewModel created |
| 7106 | EventIdPreviewUpdated | ContextPreviewViewModel | Fragments updated from event |
| 7107 | EventIdStrategyToggled | ContextPreviewViewModel | Strategy toggle action |
| 7108 | EventIdCopyAll | ContextPreviewViewModel | (Reserved for future Copy All) |
| 7109 | EventIdDisposed | ContextPreviewViewModel | ViewModel disposed |

---

## Known Limitations

1. **No clipboard integration** — Copy All and per-fragment Copy commands are not yet wired to the system clipboard. The `CombinedContent` property provides the formatted string for future View-level integration via `TopLevel.GetTopLevel(this)?.Clipboard`.
2. **No Refresh command** — The spec's Refresh button is rendered but not connected to a context re-assembly trigger. This requires integration with the active agent's context request pipeline.
3. **Strategy state not persisted** — Toggle state persists only for the application session (via the orchestrator's `ConcurrentDictionary`), not to disk.
4. **No license badge on strategies** — The spec's `IsAvailable` and `LicenseInfo` properties are not displayed. Strategy availability is handled by the factory's license-tier filtering before strategies reach the ViewModel.
