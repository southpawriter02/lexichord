# Changelog: v0.7.3b — Agent Command Pipeline

**Feature ID:** EDT-073b
**Version:** 0.7.3b
**Date:** 2026-02-14
**Status:** ✅ Complete

---

## Overview

Implements the Editor Agent command pipeline, completing the path from user-initiated rewrite events (v0.7.3a) through LLM invocation to rewrite results. This is the second sub-part of v0.7.3 "The Editor Agent" and provides the core rewrite engine: the `EditorAgent` (LLM invocation with context assembly), `RewriteCommandHandler` (pipeline orchestration with license gating, event publishing, and applicator delegation), and `RewriteRequestedEventHandler` (MediatR bridge from v0.7.3a events).

The implementation includes four intent-specific YAML prompt templates (Formal, Simplify, Expand, Custom), data records for request/result/progress, forward-declared interfaces for v0.7.3d integration, and 53 unit tests covering all pipeline components.

---

## What's New

### RewriteRequest Record

Input record for the rewrite pipeline:
- `required string SelectedText` — The text selected by the user for rewriting
- `required TextSpan SelectionSpan` — The span of the selection in the document
- `required RewriteIntent Intent` — The type of rewrite transformation requested
- `string? CustomInstruction` — Custom instruction for `RewriteIntent.Custom`
- `string? DocumentPath` — Path to the document being edited
- `IReadOnlyDictionary<string, object>? AdditionalContext` — Additional context for the prompt
- `TimeSpan Timeout` — LLM invocation timeout (default 30 seconds)
- `int EstimatedTokens` — Computed token estimate (~4 chars/token)
- `Validate()` — Validates empty text, max length (50,000 chars), Custom without instruction

### RewriteResult Record

Output record from the rewrite pipeline:
- `required string OriginalText` — The original text before rewriting
- `required string RewrittenText` — The LLM-generated rewritten text
- `required RewriteIntent Intent` — The intent used for the rewrite
- `required bool Success` — Whether the rewrite completed successfully
- `string? ErrorMessage` — Error message on failure
- `required UsageMetrics Usage` — Token usage and cost metrics
- `required TimeSpan Duration` — Total pipeline execution time
- `static RewriteResult Failed(...)` — Factory method for failure results

### RewriteProgressUpdate Record & RewriteProgressState Enum

Streaming progress notification:
- `required string PartialText` — Accumulated rewritten text so far
- `required double ProgressPercentage` — Progress from 0.0 to 100.0
- `required RewriteProgressState State` — Current streaming state
- `string? StatusMessage` — Human-readable status message

States: `Initializing`, `GatheringContext`, `GeneratingRewrite`, `Completed`, `Failed`

### IEditorAgent Interface

Editor-specific agent interface extending `IAgent`:
- `Task<RewriteResult> RewriteAsync(RewriteRequest, CancellationToken)` — Non-streaming rewrite
- `IAsyncEnumerable<RewriteProgressUpdate> RewriteStreamingAsync(RewriteRequest, CancellationToken)` — Streaming rewrite
- `string GetTemplateId(RewriteIntent intent)` — Maps intent to prompt template ID

### IRewriteCommandHandler Interface

Command pipeline orchestration:
- `Task<RewriteResult> ExecuteAsync(RewriteRequest, CancellationToken)` — Full pipeline execution
- `IAsyncEnumerable<RewriteProgressUpdate> ExecuteStreamingAsync(RewriteRequest, CancellationToken)` — Streaming pipeline
- `void Cancel()` — Cancels in-progress rewrite
- `bool IsExecuting { get; }` — Tracks execution state

### IRewriteApplicator Interface (Forward-Declared)

Forward-declared interface for v0.7.3d document application:
- `Task<bool> ApplyRewriteAsync(string, TextSpan, RewriteResult, CancellationToken)` — Apply rewrite to document
- `Task PreviewRewriteAsync(string, TextSpan, string, CancellationToken)` — Preview before applying
- `Task CommitPreviewAsync(CancellationToken)` — Commit previewed rewrite
- `Task CancelPreviewAsync(CancellationToken)` — Cancel preview

### EditorAgent Implementation

The Editor Agent (`[AgentDefinition("editor", Priority = 101)]`):
- Implements both `IAgent` and `IEditorAgent`
- Attributes: `[RequiresLicense(LicenseTier.WriterPro)]`
- `AgentId => "editor"`, `Name => "The Editor"`
- Capabilities: `Chat | DocumentContext | StyleEnforcement | Streaming`
- `RewriteAsync()`: validate → gather context via `IContextOrchestrator` → get template → build variables → render messages → invoke LLM with timeout CTS → calculate `UsageMetrics` → return result
- `RewriteStreamingAsync()`: yield progress updates → gather context → stream LLM → yield partial text → yield completed
- `InvokeAsync()` (IAgent): delegates to `RewriteAsync` with Custom intent wrapper
- Context gathering: 4000-token budget, required strategies ["style", "terminology"]
- Temperature per intent: Formal=0.3, Simplified=0.4, Expanded=0.5, Custom=0.5
- Error handling: distinguishes user cancellation from timeout, graceful context assembly failure degradation

### RewriteCommandHandler Implementation

Pipeline orchestrator:
1. Verify license (`FeatureCodes.EditorAgent`)
2. Set `IsExecuting = true`
3. Create linked `CancellationTokenSource` for `Cancel()` support
4. Publish `RewriteStartedEvent` via MediatR
5. Delegate to `IEditorAgent.RewriteAsync`
6. Delegate to `IRewriteApplicator` (if registered, v0.7.3d)
7. Publish `RewriteCompletedEvent` via MediatR
8. Return result; reset `IsExecuting` in finally block

### RewriteRequestedEventHandler

MediatR notification handler bridging v0.7.3a events to the v0.7.3b pipeline:
- Implements `INotificationHandler<RewriteRequestedEvent>`
- Maps `RewriteRequestedEvent` fields → `RewriteRequest` → calls `IRewriteCommandHandler.ExecuteAsync()`
- Catches and logs all exceptions (MediatR notification handlers should not throw)
- Distinguishes `OperationCanceledException` from general exceptions

### MediatR Events

Two new lifecycle events for observability:

| Event | Purpose |
|:------|:--------|
| `RewriteStartedEvent` | Published when pipeline begins: intent, character count, document path |
| `RewriteCompletedEvent` | Published when pipeline finishes: intent, success, usage, duration, error |

### Prompt Templates

Four YAML prompt templates with Mustache syntax:

| Template ID | Intent | Temperature | Description |
|:------------|:-------|:------------|:------------|
| `editor-rewrite-formal` | Formal | 0.3 | Transform text to formal, professional tone |
| `editor-rewrite-simplify` | Simplified | 0.4 | Simplify text for broader audience |
| `editor-rewrite-expand` | Expanded | 0.5 | Expand text with more detail and explanation |
| `editor-rewrite-custom` | Custom | 0.5 | User-provided transformation instruction |

All templates use: `{{selection}}`, `{{#style_rules}}`, `{{#terminology}}`, `{{#surrounding_context}}`. The custom template adds `{{custom_instruction}}` as required.

### DI Registration

Added `AddEditorAgentPipeline()` extension method:
```csharp
services.AddSingleton<IEditorAgent, EditorAgent>();
services.AddSingleton<IAgent>(sp => sp.GetRequiredService<IEditorAgent>());
services.AddScoped<IRewriteCommandHandler, RewriteCommandHandler>();
```

`IRewriteApplicator` is NOT registered (provided by v0.7.3d). `RewriteRequestedEventHandler` is auto-discovered by MediatR assembly scanning.

---

## Files Created

### Data Records (3 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Modules.Agents/Editor/RewriteRequest.cs` | Record | Rewrite command input with validation |
| `src/Lexichord.Modules.Agents/Editor/RewriteResult.cs` | Record | Rewrite command output with Failed() factory |
| `src/Lexichord.Modules.Agents/Editor/RewriteProgressUpdate.cs` | Record + Enum | Streaming progress and RewriteProgressState |

### Interfaces (3 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Modules.Agents/Editor/IEditorAgent.cs` | Interface | Editor-specific agent extending IAgent |
| `src/Lexichord.Modules.Agents/Editor/IRewriteCommandHandler.cs` | Interface | Pipeline orchestration contract |
| `src/Lexichord.Modules.Agents/Editor/IRewriteApplicator.cs` | Interface | Forward-declared for v0.7.3d |

### Events (2 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Modules.Agents/Editor/Events/RewriteStartedEvent.cs` | INotification | Pipeline start event with Create() factory |
| `src/Lexichord.Modules.Agents/Editor/Events/RewriteCompletedEvent.cs` | INotification | Pipeline completion event with Create() factory |

### Implementations (3 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Modules.Agents/Editor/EditorAgent.cs` | Class | The Editor Agent (IAgent + IEditorAgent) |
| `src/Lexichord.Modules.Agents/Editor/RewriteCommandHandler.cs` | Class | Pipeline orchestrator (IRewriteCommandHandler) |
| `src/Lexichord.Modules.Agents/Editor/RewriteRequestedEventHandler.cs` | Class | MediatR bridge from v0.7.3a events |

### Prompt Templates (4 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Modules.Agents/Resources/Prompts/editor-rewrite-formal.yaml` | YAML | Formal rewrite prompt template |
| `src/Lexichord.Modules.Agents/Resources/Prompts/editor-rewrite-simplify.yaml` | YAML | Simplify rewrite prompt template |
| `src/Lexichord.Modules.Agents/Resources/Prompts/editor-rewrite-expand.yaml` | YAML | Expand rewrite prompt template |
| `src/Lexichord.Modules.Agents/Resources/Prompts/editor-rewrite-custom.yaml` | YAML | Custom rewrite prompt template |

### Tests (4 files)

| File | Tests | Description |
|:-----|:-----:|:------------|
| `tests/Lexichord.Tests.Unit/Modules/Agents/Editor/RewriteRequestTests.cs` | 12 | Request validation and token estimation |
| `tests/Lexichord.Tests.Unit/Modules/Agents/Editor/EditorAgentTests.cs` | 21 | Agent behavior, intents, context, errors |
| `tests/Lexichord.Tests.Unit/Modules/Agents/Editor/RewriteCommandHandlerTests.cs` | 12 | Pipeline orchestration, license, events |
| `tests/Lexichord.Tests.Unit/Modules/Agents/Editor/RewriteRequestedEventHandlerTests.cs` | 6 | Event handler mapping and error safety |

---

## Files Modified

| File | Changes |
|:-----|:--------|
| `src/Lexichord.Modules.Agents/Extensions/EditorAgentServiceCollectionExtensions.cs` | Added `AddEditorAgentPipeline()` method, updated XML docs |
| `src/Lexichord.Modules.Agents/AgentsModule.cs` | Added `services.AddEditorAgentPipeline()` call, added EditorAgent initialization verification |

---

## Testing

### Test Summary

| Test File | Tests | Key Coverage |
|:----------|:-----:|:-------------|
| RewriteRequestTests | 12 | Validation, token estimation, defaults |
| EditorAgentTests | 21 | All intents, context gathering, error handling |
| RewriteCommandHandlerTests | 12 | License gating, events, applicator, cancellation |
| RewriteRequestedEventHandlerTests | 6 | Field mapping, error swallowing, custom intent |
| **Total** | **53** | All v0.7.3b functionality |

### Test Groups

| Group | Tests | Description |
|:------|:------|:------------|
| Request Validation | 8 | Empty, whitespace, max length, Custom intent |
| Token Estimation | 3 | Text-only, with custom instruction, null instruction |
| Agent Properties | 3 | AgentId, Name, Capabilities |
| Template Mapping | 5 | All intents + invalid intent (Theory) |
| Rewrite Execution | 7 | Formal, Simplified, Expanded, Custom, trim, metrics |
| Error Handling | 4 | Cancellation, LLM exception, template not found, context failure |
| Context Assembly | 2 | Budget, variable building from fragments |
| License Gating | 3 | With license, without license, agent not invoked |
| Event Publishing | 3 | Start + completed, field values, failure event |
| Execution State | 2 | IsExecuting during call, reset on failure |
| Applicator | 3 | With applicator, without applicator, applicator fails |
| Cancel | 1 | Cancel when not executing |
| Event Handler | 6 | Mapping, custom intent, throws, cancellation |

### Test Traits

All tests use: `[Trait("Category", "Unit")]`, `[Trait("SubPart", "v0.7.3b")]`

---

## Design Decisions

1. **IAgent Direct Implementation** — The spec references `BaseAgent` as a base class, but it does not exist in the codebase. `EditorAgent` implements `IAgent` directly, following the `CoPilotAgent` pattern.

2. **Forward-Declared IRewriteApplicator** — The applicator interface is defined in v0.7.3b but implemented in v0.7.3d. `RewriteCommandHandler` accepts it as nullable (`IRewriteApplicator?`) and skips document application when null.

3. **MediatR Bridge Pattern** — `RewriteRequestedEventHandler` bridges v0.7.3a's event-driven architecture to the v0.7.3b command pipeline, maintaining the decoupled design established in v0.7.3a.

4. **Scoped Command Handler** — `RewriteCommandHandler` is registered as Scoped to ensure per-operation isolation of `IsExecuting` state and the internal `CancellationTokenSource`.

5. **Intent-Specific Temperatures** — Each rewrite intent uses a calibrated temperature: Formal (0.3, conservative), Simplified (0.4), Expanded/Custom (0.5, more creative).

6. **Context Orchestrator Integration** — The EditorAgent uses `IContextOrchestrator` (v0.7.2c) with a 4000-token budget and required strategies ["style", "terminology"] for consistent context gathering.

7. **Dual IAgent Registration** — `EditorAgent` is registered as both `IEditorAgent` (for the pipeline) and `IAgent` (for the agent registry) via forward-resolution to avoid duplicate instances.

8. **Error Swallowing in Event Handler** — `RewriteRequestedEventHandler` catches and logs all exceptions rather than rethrowing, following MediatR notification handler best practices to prevent affecting other subscribers.

---

## Dependencies

### Consumed (from existing modules)

| Interface | Version | Used By |
|:----------|:--------|:--------|
| `IAgent` | v0.6.6a | EditorAgent (implementation target) |
| `IChatCompletionService` | v0.5.x | EditorAgent (LLM invocation) |
| `IPromptRenderer` | v0.5.x | EditorAgent (prompt rendering) |
| `IPromptTemplateRepository` | v0.5.x | EditorAgent (template lookup) |
| `IContextOrchestrator` | v0.7.2c | EditorAgent (context assembly) |
| `ILicenseContext` | v0.0.4c | RewriteCommandHandler (license gating) |
| `IMediator` | v0.0.7a | RewriteCommandHandler (event publishing) |
| `FeatureCodes.EditorAgent` | v0.7.3a | RewriteCommandHandler (feature code) |
| `RewriteRequestedEvent` | v0.7.3a | RewriteRequestedEventHandler (event source) |
| `RewriteIntent` | v0.7.3a | All records and implementations |
| `TextSpan` | v0.6.7b | RewriteRequest (selection span) |
| `UsageMetrics` | v0.6.8a | RewriteResult (token usage) |

### Produced (new interfaces)

| Interface | Consumers |
|:----------|:----------|
| `IEditorAgent` | RewriteCommandHandler, AgentRegistry |
| `IRewriteCommandHandler` | RewriteRequestedEventHandler, RewriteCommandViewModel |
| `IRewriteApplicator` | v0.7.3d (implementation), RewriteCommandHandler (consumer) |
| `RewriteRequest` | EditorAgent, RewriteCommandHandler |
| `RewriteResult` | All pipeline stages |
| `RewriteStartedEvent` | UI/Telemetry subscribers |
| `RewriteCompletedEvent` | UI/Telemetry subscribers |

### No New NuGet Packages

All dependencies are existing project references:
- `MediatR` (existing)
- `Microsoft.Extensions.Logging` (existing)
- `Microsoft.Extensions.DependencyInjection` (existing)

---

## Known Limitations

1. **No document application** — v0.7.3b returns rewrite results but does not apply them to the document. `IRewriteApplicator` is forward-declared; the concrete implementation is provided by v0.7.3d.

2. **No streaming UI** — `ExecuteStreamingAsync` is implemented but there is no UI to consume the streaming progress updates. The streaming UI is implemented in v0.7.3c.

3. **No undo/redo** — Rewrite operations are not undoable. Undo/redo support is provided by v0.7.3d.

4. **No context strategy contribution** — The EditorAgent uses existing context strategies via `IContextOrchestrator` but does not add new editor-specific strategies. The context strategies referenced in the spec (SurroundingText, StyleRules, Terminology) are already provided by v0.7.2b.

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

No breaking changes. This sub-part adds new functionality without modifying existing APIs. The `IRewriteApplicator` interface is forward-declared for v0.7.3d but has no implementation yet — `RewriteCommandHandler` handles this gracefully by skipping document application when the applicator is null.
