# Changelog: v0.7.2c — Context Orchestrator

**Feature ID:** CTX-072c
**Version:** 0.7.2c
**Date:** 2026-02-14
**Status:** ✅ Complete

---

## Overview

Implements the Context Orchestrator — the central coordination component that executes context strategies in parallel, deduplicates overlapping content, enforces token budgets via priority-based trimming, extracts template variables, and publishes assembly events via MediatR. Introduces the `IContextOrchestrator` interface in the Abstractions layer and the `ContextOrchestrator` sealed implementation in Modules.Agents, along with supporting types for configuration, deduplication, and event notification.

This sub-part builds upon v0.7.2a's abstraction layer (interfaces, records, base class, factory) and v0.7.2b's concrete strategy implementations to deliver the orchestration engine that ties the Context Assembler system together.

---

## What's New

### IContextOrchestrator Interface

Abstraction defining the context assembly contract:
- **Namespace:** `Lexichord.Abstractions.Agents.Context`
- **Members:** `AssembleAsync(request, budget, ct)`, `GetStrategies()`, `SetStrategyEnabled(id, enabled)`, `IsStrategyEnabled(id)`
- Full XML documentation with execution flow, thread safety guarantees, and code examples

### AssembledContext Record

Immutable result of context assembly:
- **Namespace:** `Lexichord.Abstractions.Agents.Context`
- **Primary Constructor:** `(IReadOnlyList<ContextFragment> Fragments, int TotalTokens, IReadOnlyDictionary<string, object> Variables, TimeSpan AssemblyDuration)`
- **Members:** `Empty` static property, `HasContext`, `GetCombinedContent(separator)`, `GetFragment(sourceId)`, `HasFragmentFrom(sourceId)`
- Formats fragments as labeled markdown sections with `## Label` headings

### ContextOrchestrator Implementation

Core orchestration engine with 8-step assembly pipeline:
- **Namespace:** `Lexichord.Modules.Agents.Context`
- **Dependencies:** `IContextStrategyFactory`, `ITokenCounter`, `IMediator`, `IOptions<ContextOptions>`, `ILogger<ContextOrchestrator>`
- **State:** `ConcurrentDictionary<string, bool>` for strategy enable/disable persistence
- **Assembly Pipeline:**
  1. Filter strategies by enabled state, budget exclusions, and license tier
  2. Execute all eligible strategies in parallel via `Parallel.ForEachAsync` with per-strategy timeout
  3. Collect non-null, non-empty fragments in `ConcurrentBag<ContextFragment>`
  4. Deduplicate fragments using Jaccard word-set similarity (configurable threshold, default 85%)
  5. Sort by orchestrator-level priority (descending), then relevance (descending)
  6. Trim to fit token budget with smart truncation for partially-fitting fragments
  7. Extract template variables (DocumentName, DocumentPath, CursorPosition, SelectionLength, FragmentCount, TotalTokens)
  8. Publish `ContextAssembledEvent` via MediatR for UI and telemetry

### ContextOptions Configuration

Options class for assembly behavior:
- **Section:** `"Context"`
- **Properties:** `DefaultBudget=8000`, `StrategyTimeout=5000ms`, `EnableDeduplication=true`, `DeduplicationThreshold=0.85f`, `MaxParallelism=6`
- Registered via `AddOptions<ContextOptions>()` with defaults (no `IConfiguration` binding required)

### ContentDeduplicator

Static utility for content similarity detection:
- **Method:** `CalculateJaccardSimilarity(textA, textB)` → `float`
- **Pipeline:** Lowercase → split on whitespace → strip punctuation → filter words ≤2 chars → HashSet intersection/union
- Returns 0.0 (no overlap) to 1.0 (identical word sets)

### MediatR Events

Two new `INotification` records for event-driven UI synchronization:
- **`ContextAssembledEvent`** — Published after each assembly with `AgentId`, `Fragments`, `TotalTokens`, `Duration`
- **`StrategyToggleEvent`** — Published when a strategy is enabled/disabled with `StrategyId`, `IsEnabled`

---

## DI Registration

### AddContextOrchestrator Extension Method

Added to `AgentsServiceCollectionExtensions` and called from `AddContextStrategies()`:
- `AddOptions<ContextOptions>()` — Default configuration (matching `PerformanceOptions` pattern)
- `AddSingleton<IContextOrchestrator, ContextOrchestrator>()` — Singleton for ConcurrentDictionary state persistence

---

## Files Changed

### New Files (10)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/.../Agents/Context/IContextOrchestrator.cs` | Interface | Context orchestrator contract |
| `src/.../Agents/Context/AssembledContext.cs` | Record | Assembly result with helpers |
| `src/.../Agents/Context/ContextOrchestrator.cs` | Implementation | Core orchestration engine |
| `src/.../Agents/Context/ContextOptions.cs` | Configuration | Assembly options |
| `src/.../Agents/Context/ContentDeduplicator.cs` | Utility | Jaccard similarity calculator |
| `src/.../Agents/Context/ContextAssembledEvent.cs` | Event | Assembly completion event |
| `src/.../Agents/Context/StrategyToggleEvent.cs` | Event | Strategy toggle event |
| `tests/.../Abstractions/Agents/Context/AssembledContextTests.cs` | Tests | 13 tests |
| `tests/.../Modules/Agents/Context/ContentDeduplicatorTests.cs` | Tests | 12 tests |
| `tests/.../Modules/Agents/Context/ContextOrchestratorTests.cs` | Tests | 23 tests |

### Modified Files (1)

| File | Changes |
|:-----|:--------|
| `src/.../Extensions/AgentsServiceCollectionExtensions.cs` | Added `AddContextOrchestrator()` method, added usings, called from `AddContextStrategies()` |

---

## Spec-to-Codebase Adaptations

| Spec Reference | Actual Codebase | Adaptation |
|:---|:---|:---|
| `IEventBus.PublishAsync()` | `IMediator.Publish()` | Used MediatR `INotification` pattern matching `AgentErrorEvent` |
| `IEventBus` dependency | `IMediator` dependency | Constructor takes `IMediator` instead of non-existent `IEventBus` |
| `ContextOptions` bound via `IConfiguration` | `AddOptions<ContextOptions>()` | `AgentsModule.RegisterServices()` does not receive `IConfiguration` |
| Moq test framework | NSubstitute | Codebase uses NSubstitute, not Moq |

---

## Testing

### Test Summary

| Test File | Tests | Key Coverage |
|:----------|:-----:|:-------------|
| AssembledContextTests | 13 | Empty, HasContext, GetCombinedContent, GetFragment, HasFragmentFrom |
| ContentDeduplicatorTests | 12 | Identical (1.0), partial overlap, no overlap (0.0), null, empty, punctuation, case, short words, whitespace |
| ContextOrchestratorTests | 23 | Constructor null guards, multi-strategy assembly, budget trimming, deduplication, priority sorting, timeout, disabled strategy, excluded strategy, event publishing, variable extraction, empty strategies, failing strategy, duration, dedup disabled, enable/disable state, toggle events, GetStrategies delegation |
| **Total** | **48** | All v0.7.2c tests |

### Test Traits

All new tests use: `[Trait("Category", "Unit")]`, `[Trait("SubPart", "v0.7.2c")]`

---

## Design Decisions

1. **MediatR over IEventBus** — The spec referenced `IEventBus` which does not exist in the codebase. All existing event publishing uses `MediatR.IMediator.Publish()` with `INotification` records. This is consistent with `AgentErrorEvent`, `DocumentChangedEvent`, and other codebase events.

2. **Singleton lifetime** — The orchestrator maintains per-strategy enabled/disabled state in a `ConcurrentDictionary`. Singleton ensures this state persists across all context assembly requests for the application lifetime.

3. **Fire-and-forget toggle events** — `SetStrategyEnabled()` publishes `StrategyToggleEvent` without awaiting, matching the fire-and-forget pattern used elsewhere (e.g., agent registry events). Handler failures do not affect toggle operations.

4. **Orchestrator-level priority mapping** — The orchestrator maintains its own priority mapping via `GetPriorityForSource()` rather than using strategies' self-reported priorities. This provides independent control over trimming order when strategies with the same priority need differentiation.

5. **Smart truncation with 100-token minimum** — When trimming to budget, fragments are only truncated if at least 100 tokens remain. This prevents wasting budget space on fragments too small to provide meaningful context.

6. **Public ContentDeduplicator** — Made `public static` for direct testability, allowing unit tests to verify Jaccard similarity calculations independently of the orchestrator.

---

## Dependencies

### Consumed (from existing modules)

| Interface | Module | Used By |
|:----------|:-------|:--------|
| `IContextStrategyFactory` | Lexichord.Modules.Agents | ContextOrchestrator (strategy creation) |
| `ITokenCounter` | Lexichord.Abstractions | ContextOrchestrator (budget trimming) |
| `IMediator` | MediatR | ContextOrchestrator (event publishing) |
| `IOptions<ContextOptions>` | Microsoft.Extensions.Options | ContextOrchestrator (configuration) |

### No New NuGet Packages

All dependencies are existing project references. MediatR and Microsoft.Extensions.Options were already referenced.

---

## Priority Mapping

| Source ID | Orchestrator Priority | StrategyPriority Constant |
|:----------|:---------------------|:--------------------------|
| `document` | 100 | Critical |
| `selection` | 80 | High |
| `cursor` | 70 | High - 10 |
| `heading` | 70 | Medium + 10 |
| `rag` | 60 | Medium |
| `style` | 50 | Optional + 30 |
| unknown | 40 | Low |

---

## Known Limitations

1. **No configuration binding** — `ContextOptions` uses hardcoded defaults via `AddOptions<ContextOptions>()`. Host applications can bind to configuration by calling `services.Configure<ContextOptions>(configuration.GetSection("Context"))`.
2. **Strategy state is session-scoped** — Enabled/disabled state in `ConcurrentDictionary` persists only for the application session (not serialized to disk). Configuration deferred to future settings integration.
3. **Linear deduplication** — O(n²) pairwise comparison for deduplication. Acceptable for ≤6 strategies but would need optimization for larger strategy counts.
4. **No required strategy enforcement** — `ContextBudget.RequiredStrategies` is checked by `ShouldExecute()` for execution filtering but not enforced during budget trimming (required strategies can still be dropped if over budget).
