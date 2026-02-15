# Changelog: v0.7.3d — Undo/Redo Integration

**Feature ID:** AGT-073d
**Version:** 0.7.3d
**Date:** 2026-02-14
**Status:** ✅ Complete

---

## Overview

Implements undo/redo integration for the Editor Agent, enabling users to safely experiment with AI rewrites and revert via Ctrl+Z. This is the fourth and final sub-part of v0.7.3 "The Editor Agent" and completes the `IRewriteApplicator` interface forward-declared in v0.7.3b. Adds `RewriteUndoableOperation` implementing the new `IUndoableOperation` abstraction, `RewriteApplicator` with preview/commit/cancel lifecycle, 6 MediatR events for undo/redo integration, and the `IUndoRedoService`/`IUndoableOperation` abstractions (referenced as v0.1.4a in the spec but never previously implemented). Includes 49 unit tests with 100% pass rate.

---

## What's New

### IUndoableOperation Abstraction

Undoable operation contract for reversible editor operations:
- **Namespace:** `Lexichord.Abstractions.Contracts.Undo`
- **Interface members:** `Id`, `DisplayName`, `Timestamp`, `ExecuteAsync(ct)`, `UndoAsync(ct)`, `RedoAsync(ct)`
- Referenced as v0.1.4a in specs but never existed — created as a new abstraction to fulfill the spec requirement

### IUndoRedoService Abstraction

Undo/redo stack management contract:
- **Namespace:** `Lexichord.Abstractions.Contracts.Undo`
- **Interface members:** `Push(IUndoableOperation)`, `CanUndo`, `CanRedo`, `UndoAsync(ct)`, `RedoAsync(ct)`, `Clear()`, `UndoStackChanged` event
- **Event args:** `UndoRedoChangedEventArgs` with `CanUndo`, `CanRedo`, `LastOperationName` properties
- No concrete implementation yet — this is an infrastructure concern for a future version

### RewriteUndoableOperation

Implements `IUndoableOperation` for AI text rewrites:
- Uses sync `IEditorService` APIs: `BeginUndoGroup(DisplayName)` → `DeleteText(start, len)` → `InsertText(start, text)` → `EndUndoGroup()` for atomic replacement
- **DisplayName format:** `"AI Rewrite ({intent})"` matching spec
- **ExecuteAsync:** Replaces original with rewritten text at `_originalSpan.Start`
- **UndoAsync:** Calculates rewritten span using `_rewrittenText.Length`, replaces back to original
- **RedoAsync:** Delegates to same logic as `ExecuteAsync`
- Constructor validates non-null arguments with `ArgumentNullException.ThrowIfNull`
- Returns `Task.CompletedTask` since editor operations are synchronous — avoids unnecessary `Task.Run()` overhead
- **ToString():** Returns `"RewriteOperation({Id}): {intent}, {originalLength} -> {rewrittenLength} chars"`

### RewriteApplicator

Implements `IRewriteApplicator` + `IDisposable` with preview/commit/cancel lifecycle:
- **Constructor:** `(IEditorService, IUndoRedoService?, IMediator, ILoggerFactory)` — `IUndoRedoService` is nullable (mirrors v0.7.3b's nullable `IRewriteApplicator?` pattern)
- **ApplyRewriteAsync:** Cancels active preview, creates `RewriteUndoableOperation`, executes, pushes to undo service (if available), publishes `RewriteAppliedEvent`
- **PreviewRewriteAsync:** Gets original text via `GetDocumentByPath()?.Content?.Substring()` with `GetDocumentText()` fallback, replaces text using `BeginUndoGroup`/`DeleteText`/`InsertText`/`EndUndoGroup`, starts 5-min timeout with `CancellationTokenSource`, publishes `RewritePreviewStartedEvent`
- **CommitPreviewAsync:** Creates `RewriteUndoableOperation` without re-executing (text already applied), pushes to undo service, clears preview state, publishes `RewritePreviewCommittedEvent`
- **CancelPreviewAsync:** Restores original text using same delete+insert pattern, clears preview state, publishes `RewritePreviewCancelledEvent`
- **IsPreviewActive:** Thread-safe check under `object _previewLock`
- **Dispose:** Cancels preview timeout, safe for multiple calls

### MediatR Events

Six new events in `Lexichord.Modules.Agents.Editor.Events`, following existing pattern (record with `DateTime Timestamp`, `INotification`, static `Create()` factory):

1. **RewriteAppliedEvent** — `(DocumentPath, OriginalText, RewrittenText, Intent, OperationId, Timestamp)`
2. **RewriteUndoneEvent** — `(DocumentPath, OperationId, Timestamp)`
3. **RewriteRedoneEvent** — `(DocumentPath, OperationId, Timestamp)`
4. **RewritePreviewStartedEvent** — `(DocumentPath, PreviewText, Timestamp)`
5. **RewritePreviewCommittedEvent** — `(DocumentPath, OperationId, Timestamp)`
6. **RewritePreviewCancelledEvent** — `(DocumentPath, Timestamp)`

### DI Registration

Added `AddEditorAgentUndoIntegration()` extension method in `EditorAgentServiceCollectionExtensions`:
```csharp
services.AddScoped<IRewriteApplicator, RewriteApplicator>();
```

Called from `AgentsModule.RegisterServices()` after `AddEditorAgentContextStrategies()`. Initialization verification in `AgentsModule.InitializeAsync()` resolves `IRewriteApplicator` from scoped provider to confirm registration.

---

## Files Created

### Undo Abstractions (3 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Abstractions/Contracts/Undo/IUndoableOperation.cs` | Interface | Undoable operation contract |
| `src/Lexichord.Abstractions/Contracts/Undo/IUndoRedoService.cs` | Interface | Undo/redo stack management |
| `src/Lexichord.Abstractions/Contracts/Undo/UndoRedoChangedEventArgs.cs` | EventArgs | Undo stack change notification |

### Undo Integration (2 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Modules.Agents/Editor/RewriteUndoableOperation.cs` | Class | IUndoableOperation for rewrites |
| `src/Lexichord.Modules.Agents/Editor/RewriteApplicator.cs` | Class | IRewriteApplicator implementation |

### Events (6 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Modules.Agents/Editor/Events/RewriteAppliedEvent.cs` | Record | Rewrite applied notification |
| `src/Lexichord.Modules.Agents/Editor/Events/RewriteUndoneEvent.cs` | Record | Rewrite undone notification |
| `src/Lexichord.Modules.Agents/Editor/Events/RewriteRedoneEvent.cs` | Record | Rewrite redone notification |
| `src/Lexichord.Modules.Agents/Editor/Events/RewritePreviewStartedEvent.cs` | Record | Preview started notification |
| `src/Lexichord.Modules.Agents/Editor/Events/RewritePreviewCommittedEvent.cs` | Record | Preview committed notification |
| `src/Lexichord.Modules.Agents/Editor/Events/RewritePreviewCancelledEvent.cs` | Record | Preview cancelled notification |

### Tests (2 files)

| File | Tests | Description |
|:-----|:-----:|:------------|
| `tests/Lexichord.Tests.Unit/Modules/Agents/Editor/RewriteUndoableOperationTests.cs` | 18 | Constructor validation, properties, execute/undo/redo, cycle test |
| `tests/Lexichord.Tests.Unit/Modules/Agents/Editor/RewriteApplicatorTests.cs` | 31 | Constructor validation, apply/preview/commit/cancel, preview lifecycle |

---

## Files Modified

| File | Changes |
|:-----|:--------|
| `src/Lexichord.Modules.Agents/Editor/IRewriteApplicator.cs` | Added `bool IsPreviewActive { get; }` property, updated XML docs |
| `src/Lexichord.Modules.Agents/Extensions/EditorAgentServiceCollectionExtensions.cs` | Added `AddEditorAgentUndoIntegration()` method, updated XML docs for v0.7.3d |
| `src/Lexichord.Modules.Agents/AgentsModule.cs` | Added `services.AddEditorAgentUndoIntegration()` call, init verification for `IRewriteApplicator`, updated description |

---

## Testing

### Test Summary

| Test File | Tests | Key Coverage |
|:----------|:-----:|:-------------|
| RewriteUndoableOperationTests | 18 | Constructor, properties, execute/undo/redo, cycle, toString |
| RewriteApplicatorTests | 31 | Constructor, apply, preview, commit, cancel, lifecycle |
| **Total** | **49** | All v0.7.3d functionality |

### Test Groups

| Group | Tests | Description |
|:------|:------|:------------|
| Constructor Validation | 8 | Null argument checks for operation (4) and applicator (4) |
| Properties | 6 | Id uniqueness, Id valid GUID, DisplayName for 4 intents, Timestamp |
| ExecuteAsync | 3 | Delete+insert sequence, undo group wrapping, display name |
| UndoAsync | 2 | Restores original text, uses rewritten text length for span |
| RedoAsync | 1 | Reapplies rewritten text |
| Full Cycle | 1 | Execute→Undo→Redo maintains correct state |
| ToString | 1 | Contains operation info |
| ApplyRewriteAsync | 8 | Success/fail/exception paths, undo push, event publishing |
| PreviewRewriteAsync | 4 | Sets active, replaces text, publishes event, no content fallback |
| CommitPreviewAsync | 4 | Clears state, pushes undo, publishes event, no-op without preview |
| CancelPreviewAsync | 5 | Restores text, clears state, publishes event, no undo push, no-op |
| Preview Lifecycle | 3 | IsPreviewActive, replace existing preview, apply cancels preview |
| Dispose | 1 | Multiple dispose calls don't throw |

### Test Traits

All tests use: `[Trait("Category", "Unit")]`, `[Trait("SubPart", "v0.7.3d")]`

---

## Design Decisions

1. **New Undo Abstractions** — The spec references `IUndoableOperation` and `IUndoRedoService` from v0.1.4a, but these were never implemented. Created as new abstractions in `Lexichord.Abstractions.Contracts.Undo` to fulfill the spec requirement.

2. **Sync Editor API Adaptation** — The spec uses `ReplaceTextAsync(path, span, text, ct)` which doesn't exist on `IEditorService`. Adapted to use `BeginUndoGroup` → `DeleteText(offset, len)` → `InsertText(offset, text)` → `EndUndoGroup()` for atomic replacement.

3. **Nullable IUndoRedoService** — `IUndoRedoService` has no concrete implementation yet (infrastructure concern for a future version). `RewriteApplicator` accepts it as nullable `IUndoRedoService?`, mirroring the v0.7.3b pattern where `RewriteCommandHandler` accepts nullable `IRewriteApplicator?`.

4. **Text Retrieval for Preview** — The spec's `GetTextAsync(path, span, ct)` doesn't exist. Used `IEditorService.GetDocumentByPath(path)?.Content?.Substring(start, length)` with `GetDocumentText()` fallback for the active document, following the `SurroundingTextContextStrategy` pattern.

5. **Synchronous Operations with Task.CompletedTask** — All `IEditorService` text manipulation APIs are synchronous. `RewriteUndoableOperation` returns `Task.CompletedTask` since there's no async work. This avoids unnecessary `Task.Run()` overhead.

6. **Preview Timeout with CancellationTokenSource** — Preview auto-cancels after 5 minutes using `CancellationTokenSource` with `Task.Delay`, following the spec's preview timeout requirement. Cancellation is cooperative and cleans up on dispose.

7. **Lock-Based Thread Safety** — Preview state is protected by `object _previewLock` for thread safety, ensuring `IsPreviewActive` reads are consistent and state transitions are atomic.

8. **Scoped Lifetime for RewriteApplicator** — Registered as Scoped rather than Singleton because each operation scope needs isolated preview state. Follows the `RewriteCommandHandler` registration pattern.

---

## Dependencies

### Consumed (from existing modules)

| Interface | Version | Used By |
|:----------|:--------|:--------|
| `IEditorService` | v0.1.3a | RewriteUndoableOperation, RewriteApplicator |
| `IMediator` | v0.0.7a | RewriteApplicator (event publishing) |
| `RewriteResult` | v0.7.3b | RewriteApplicator (apply input) |
| `RewriteIntent` | v0.7.3a | RewriteUndoableOperation (display name) |
| `TextSpan` | v0.1.3a | RewriteUndoableOperation (span tracking) |
| `IManuscriptViewModel` | v0.1.3a | RewriteApplicator (document content access) |

### Produced (new classes)

| Class | Consumers |
|:------|:----------|
| `IUndoableOperation` | RewriteUndoableOperation, future undo integrations |
| `IUndoRedoService` | RewriteApplicator (nullable), future implementation |
| `RewriteUndoableOperation` | RewriteApplicator |
| `RewriteApplicator` | RewriteCommandHandler (via IRewriteApplicator) |

### No New NuGet Packages

All dependencies are existing project references.

---

## Spec Adaptations

| Spec Reference | Actual Codebase | Adaptation |
|:---|:---|:---|
| `IUndoRedoService` / `IUndoableOperation` (v0.1.4a) | Never implemented | Created as new abstractions in Contracts.Undo |
| `IEditorService.ReplaceTextAsync(path, span, text, ct)` | Sync `DeleteText` + `InsertText` | Wrapped in `BeginUndoGroup`/`EndUndoGroup` for atomic replace |
| `IEditorService.GetTextAsync(path, span, ct)` | Does not exist | Used `GetDocumentByPath(path)?.Content?.Substring()` with `GetDocumentText()` fallback |
| `IUndoRedoService` required in `RewriteApplicator` | Service has no implementation | Accepted as nullable `IUndoRedoService?` |
| `IRewriteApplicator.IsPreviewActive` | Not on forward-declared interface | Added the property |
| `Namespace: Lexichord.Abstractions.Undo` | Codebase uses nested Contracts | Used `Lexichord.Abstractions.Contracts.Undo` |
| Events without Timestamp parameter | Codebase pattern includes Timestamp | Added `DateTime Timestamp` + `static Create()` factory to all events |

---

## Build & Test Results

```
Build:     Succeeded (0 errors, 1 pre-existing warning)
v0.7.3d:   49 passed, 0 failed
Editor:    184 passed, 0 failed (no regressions)
```
