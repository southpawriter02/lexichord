# Changelog: v0.7.2a — Context Strategy Interface

**Feature ID:** CTX-072a
**Version:** 0.7.2a
**Date:** 2026-02-13
**Status:** ✅ Complete

---

## Overview

Introduces the pluggable context strategy abstraction layer for the Context Assembler system (v0.7.2). Defines interfaces and base classes for gathering contextual information from multiple sources (document, selection, cursor, RAG, style rules) to provide to AI agents during request processing.

This is the foundation for intelligent context assembly with priority-based execution, token budget management, and license-aware filtering.

---

## What's New

### StrategyPriority Constants

Standard priority levels for context strategies:
- **Critical** (100) — Always needed, never trimmed first
- **High** (80) — Usually needed for good results
- **Medium** (60) — Helpful but not essential
- **Low** (40) — Nice to have
- **Optional** (20) — Include only if budget allows

Priority values control execution order (higher first) and trimming order (lower first).

### ContextGatheringRequest Record

Request data for context gathering operations:
- **DocumentPath** (string?) — Path to active document
- **CursorPosition** (int?) — Cursor offset in document
- **SelectedText** (string?) — Currently selected text
- **AgentId** (string) — ID of requesting agent
- **Hints** (IReadOnlyDictionary<string, object>?) — Strategy-specific parameters

**Methods:**
- `Empty(string agentId)` — Creates minimal request with only agent ID
- `GetHint<T>(string key, T defaultValue)` — Type-safe hint access with fallback

**Computed Properties:**
- `HasDocument`, `HasSelection`, `HasCursor` — Presence checks

### ContextFragment Record

Context data produced by strategies:
- **SourceId** (string) — Strategy identifier
- **Label** (string) — Display name for UI/logging
- **Content** (string) — Actual context text
- **TokenEstimate** (int) — Estimated token count
- **Relevance** (float) — Relevance score 0.0-1.0

**Methods:**
- `Empty(string sourceId, string label)` — Creates empty fragment
- `TruncateTo(int maxTokens, ITokenCounter)` — Smart paragraph-aware truncation

**Computed Properties:**
- `HasContent` — Non-empty content check

### ContextBudget Record

Token budget configuration:
- **MaxTokens** (int) — Total token limit (default: 8000)
- **RequiredStrategies** (IReadOnlyList<string>?) — Must execute regardless of budget
- **ExcludedStrategies** (IReadOnlyList<string>?) — Skip these strategies

**Methods:**
- `Default` — Static property with 8000 token limit
- `WithLimit(int maxTokens)` — Factory for custom limit
- `IsRequired(string strategyId)`, `IsExcluded(string strategyId)`, `ShouldExecute(string strategyId)` — Filtering helpers

### IContextStrategy Interface

Core interface for all context strategies:
- **StrategyId** (string) — Unique identifier
- **DisplayName** (string) — Human-readable name
- **Priority** (int) — Execution/trimming priority
- **MaxTokens** (int) — Self-imposed token limit
- **GatherAsync(ContextGatheringRequest, CancellationToken)** — Async context gathering

**Error Handling:**
- Return `null` when no context available (normal case)
- Throw exceptions for actual errors (file access denied, service unavailable)

### IContextStrategyFactory Interface

Factory for creating context strategies:
- **AvailableStrategyIds** (IReadOnlyList<string>) — All strategies for current license tier
- **CreateStrategy(string strategyId)** — Create single strategy instance
- **CreateAllStrategies()** — Create all available strategies
- **IsAvailable(string strategyId, LicenseTier tier)** — Check tier requirements

**License Tier Mapping:**
- WriterPro: document, selection, cursor, heading strategies
- Teams: all WriterPro + rag, style strategies

### ContextStrategyBase Abstract Class

Base class for strategy implementations:
- **Protected Constructor:** `(ITokenCounter tokenCounter, ILogger logger)`
- **Helper Methods:**
  - `CreateFragment(content, relevance, customLabel?)` — Auto token estimation with logging
  - `TruncateToMaxTokens(content)` — Smart paragraph truncation with warning logging
  - `ValidateRequest(request, requireDocument, requireSelection, requireCursor)` — Prerequisite checking with debug logging

### ContextStrategyFactory Implementation

Default factory with static registration:
- **Registration Dictionary:** Maps strategy ID → (Implementation Type, Minimum Tier)
- **v0.7.2a Note:** Dictionary intentionally empty; concrete strategies added in v0.7.2b
- **License Checking:** Filters strategies via `ILicenseContext.Tier`
- **DI Resolution:** Resolves strategy instances with dependency injection

### AgentCapabilitiesExtensions.ToDisplayString()

New extension method added to support Agent Selector UI:
```csharp
public static string ToDisplayString(this AgentCapabilities capabilities)
```
- Joins `GetCapabilityNames()` output with commas
- Returns "None" for `AgentCapabilities.None`
- **Introduced in:** v0.7.2a

---

## Files Changed

### New Files (14)

**Abstractions (6 files):**

| File | Lines | Description |
|------|-------|-------------|
| `src/Lexichord.Abstractions/Agents/Context/StrategyPriority.cs` | ~116 | Priority constants |
| `src/Lexichord.Abstractions/Agents/Context/ContextGatheringRequest.cs` | ~180 | Request record |
| `src/Lexichord.Abstractions/Agents/Context/ContextFragment.cs` | ~230 | Fragment record |
| `src/Lexichord.Abstractions/Agents/Context/ContextBudget.cs` | ~150 | Budget record |
| `src/Lexichord.Abstractions/Agents/Context/IContextStrategy.cs` | ~140 | Strategy interface |
| `src/Lexichord.Abstractions/Agents/Context/IContextStrategyFactory.cs` | ~120 | Factory interface |

**Implementations (2 files):**

| File | Lines | Description |
|------|-------|-------------|
| `src/Lexichord.Modules.Agents/Context/ContextStrategyBase.cs` | ~245 | Abstract base class |
| `src/Lexichord.Modules.Agents/Context/ContextStrategyFactory.cs` | ~192 | Factory implementation |

**Test Files (8 files):**

| File | Tests | Coverage |
|------|-------|----------|
| `tests/Lexichord.Tests.Unit/Abstractions/Agents/Context/StrategyPriorityTests.cs` | 6 | 100% |
| `tests/Lexichord.Tests.Unit/Abstractions/Agents/Context/ContextGatheringRequestTests.cs` | 12 | 100% |
| `tests/Lexichord.Tests.Unit/Abstractions/Agents/Context/ContextFragmentTests.cs` | 10 | 100% |
| `tests/Lexichord.Tests.Unit/Abstractions/Agents/Context/ContextBudgetTests.cs` | 9 | 100% |
| `tests/Lexichord.Tests.Unit/Abstractions/Agents/Context/IContextStrategyTests.cs` | 6 | 100% |
| `tests/Lexichord.Tests.Unit/Abstractions/Agents/Context/IContextStrategyFactoryTests.cs` | 5 | 100% |
| `tests/Lexichord.Tests.Unit/Modules/Agents/Context/ContextStrategyBaseTests.cs` | 23 | 100% |
| `tests/Lexichord.Tests.Unit/Modules/Agents/Context/ContextStrategyFactoryTests.cs` | 17 | 100% |

**Total Tests:** 88 new tests

### Modified Files (2)

| File | Change |
|------|--------|
| `src/Lexichord.Modules.Agents/Extensions/AgentsServiceCollectionExtensions.cs` | Added `AddContextStrategies()` method |
| `src/Lexichord.Abstractions/Agents/AgentCapabilitiesExtensions.cs` | Added `ToDisplayString()` extension method |

---

## API Reference

### IContextStrategy

```csharp
public interface IContextStrategy
{
    string StrategyId { get; }
    string DisplayName { get; }
    int Priority { get; }
    int MaxTokens { get; }
    Task<ContextFragment?> GatherAsync(
        ContextGatheringRequest request,
        CancellationToken cancellationToken = default);
}
```

### ContextStrategyBase

```csharp
public abstract class ContextStrategyBase : IContextStrategy
{
    protected ContextStrategyBase(
        ITokenCounter tokenCounter,
        ILogger logger);

    protected ContextFragment CreateFragment(
        string content,
        float relevance = 1.0f,
        string? customLabel = null);

    protected string TruncateToMaxTokens(string content);

    protected bool ValidateRequest(
        ContextGatheringRequest request,
        bool requireDocument = false,
        bool requireSelection = false,
        bool requireCursor = false);
}
```

### Usage Example

```csharp
// Factory usage
var factory = serviceProvider.GetRequiredService<IContextStrategyFactory>();

// Get available strategies for current tier
var available = factory.AvailableStrategyIds;
Console.WriteLine($"Available: {string.Join(", ", available)}");

// Create all strategies
var strategies = factory.CreateAllStrategies();
foreach (var strategy in strategies.OrderByDescending(s => s.Priority))
{
    Console.WriteLine($"{strategy.DisplayName}: Priority {strategy.Priority}");
}

// Strategy implementation example
public class DocumentContextStrategy : ContextStrategyBase
{
    public override string StrategyId => "document";
    public override string DisplayName => "Document Content";
    public override int Priority => StrategyPriority.Critical;
    public override int MaxTokens => 4000;

    public override async Task<ContextFragment?> GatherAsync(
        ContextGatheringRequest request,
        CancellationToken cancellationToken = default)
    {
        // Validate prerequisites
        if (!ValidateRequest(request, requireDocument: true))
            return null;

        // Gather context
        var content = await LoadDocumentAsync(request.DocumentPath!);

        // Apply truncation if needed
        content = TruncateToMaxTokens(content);

        // Create fragment with auto token estimation
        return CreateFragment(content, relevance: 1.0f);
    }
}
```

---

## Dependencies

### Existing Dependencies (Reused)

- **v0.6.1b (ITokenCounter)** — Token counting and truncation
- **v0.0.6a (ILicenseContext)** — License tier checking

### No New NuGet Packages

All functionality implemented using existing dependencies.

---

## Design Decisions

### 1. Relationship to IContextProvider (v0.6.3d)

- **Coexist, don't replace:** IContextStrategy complements IContextProvider
- **Different use cases:** IContextProvider → template variables; IContextStrategy → agent context assembly
- **Different outputs:** IContextProvider returns `IDictionary<string, object>`; IContextStrategy returns `ContextFragment?`

### 2. Token Counting Integration

- Pass `ITokenCounter` to strategies via constructor injection in `ContextStrategyBase`
- Base class provides helpers: `CreateFragment()`, `TruncateToMaxTokens()`
- Fragments include `TokenEstimate` for budget management

### 3. License Gating

- **Factory-level checking** via `IContextStrategyFactory.IsAvailable(strategyId, tier)`
- Strategies are license-agnostic (simpler implementation)
- License tier mapping defined in factory documentation

### 4. Error Handling

- Return `null` from `GatherAsync()` when no context available (normal case)
- Throw exceptions for actual errors (file access denied, service unavailable)
- Orchestrator handles both gracefully

### 5. Service Lifetime

- **IContextStrategyFactory:** Singleton (stateless, shared)
- **IContextStrategy implementations:** Transient (created per request via factory)

---

## Breaking Changes

None. This is a new feature that doesn't modify existing APIs.

---

## Migration Guide

N/A — New feature, no migration required.

---

## Known Limitations

1. **No Concrete Strategies:** v0.7.2a defines interfaces only; concrete strategies implemented in v0.7.2b
2. **Empty Factory Registration:** `ContextStrategyFactory._registrations` dictionary intentionally empty
3. **No Orchestrator:** Context budget management and strategy orchestration added in v0.7.2c

---

## Next Steps

- **v0.7.2b:** Implement concrete strategies (document, selection, cursor, heading, rag, style)
- **v0.7.2c:** Add context orchestrator with budget management and priority-based execution
- **v0.7.2d:** Add context preview UI for debugging and transparency

---

## References

- **Specification:** `docs/specs/v0.7.x/v0.7.2/LCS-DES-v0.7.2a.md`
- **Scope Breakdown:** `docs/specs/v0.7.x/v0.7.2/LCS-SBD-v0.7.2.md`
- **Dependency Matrix:** `docs/specs/DEPENDENCY-MATRIX.md`

---

**End of Changelog**
