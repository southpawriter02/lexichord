# Changelog: v0.7.2e — Knowledge Context Strategy

**Feature ID:** CTX-072e
**Version:** 0.7.2e
**Date:** 2026-02-14
**Status:** ✅ Complete

---

## Overview

Implements the Knowledge Context Strategy — a new context strategy that bridges the v0.6.6e Knowledge Graph pipeline (`IKnowledgeContextProvider`) into the v0.7.2 Context Assembler. This enables all specialist agents (Editor, Simplifier, Tuning, Summarizer, Co-pilot) to receive knowledge graph entities, relationships, and axioms as contextual information during request processing.

This sub-part builds upon v0.7.2a's strategy interface and v0.7.2b's strategy pattern to add the 7th built-in context strategy, connecting the CKVS Knowledge Graph to the agent layer.

---

## What's New

### KnowledgeContextConfig

Agent-specific configuration record for knowledge context retrieval:
- **Namespace:** `Lexichord.Modules.Agents.Context.Strategies`
- **Pattern:** Immutable `sealed record` with `init` setters for `with`-expression composition
- **Properties:** `MaxTokens` (4000), `IncludeEntityTypes` (`IReadOnlyList<string>?`), `MinRelevanceScore` (0.5f), `IncludeRelationships` (true), `IncludeAxioms` (true), `MaxEntities` (20), `Format` (ContextFormat.Yaml), `MaxPropertiesPerEntity` (10)
- **Purpose:** Enables per-agent configuration balancing context richness against token budget, with `with`-expression support for license-tier restriction application

### KnowledgeContextStrategy

Core strategy implementation extending `ContextStrategyBase`:
- **Namespace:** `Lexichord.Modules.Agents.Context.Strategies`
- **Attribute:** `[RequiresLicense(LicenseTier.Teams)]`
- **StrategyId:** `"knowledge"`
- **DisplayName:** `"Knowledge Graph"`
- **Priority:** 30 — Supplementary domain information, after Document (100), Selection (80), Cursor (70), Heading (70), RAG (60), and Style (50)
- **MaxTokens:** 4000
- **Dependencies:** `IKnowledgeContextProvider` (v0.6.6e), `ILicenseContext`, `ITokenCounter`, `ILogger<KnowledgeContextStrategy>`

**Agent Configuration Dictionary:**
Static mapping of agent IDs to `KnowledgeContextConfig`:
- `"editor"`: 30 entities, 5000 tokens, all types, axioms + relationships, score ≥ 0.4
- `"simplifier"`: 10 entities, 2000 tokens, Concept/Term/Definition only, no axioms/rels, score ≥ 0.6
- `"tuning"`: 20 entities, 4000 tokens, Endpoint/Parameter/Response/Schema, axioms + relationships, score ≥ 0.5
- `"summarizer"`: 15 entities, 3000 tokens, Product/Component/Feature/Service, no axioms, relationships, score ≥ 0.5
- `"co-pilot"`: 25 entities, 4000 tokens, all types, axioms + relationships, score ≥ 0.4

**Configurable Hint Keys:**
- `MaxEntities` (int): Override maximum entity count
- `MinRelevanceScore` (float): Override minimum relevance threshold
- `IncludeAxioms` (bool): Override axiom inclusion
- `IncludeRelationships` (bool): Override relationship inclusion
- `KnowledgeFormat` (string): Override output format ("Markdown", "Yaml", "Json", "Plain")

**GatherAsync Pipeline:**
1. Build search query from selected text (preferred) or document path filename
2. Resolve agent-specific config from `AgentConfigs` dictionary
3. Apply hint overrides from request's Hints dictionary
4. Apply license restrictions (WriterPro: max 10 entities, no axioms, no relationships)
5. Build `KnowledgeContextOptions` (converting `IReadOnlyList<string>?` → `IReadOnlySet<string>?`)
6. Delegate to `IKnowledgeContextProvider.GetContextAsync()`
7. Apply base class token truncation via `TruncateToMaxTokens()`
8. Calculate relevance score: 0.7f if truncated, else scaled by entity count (min 1.0)
9. Create fragment via `CreateFragment()`

**Error Handling:**
- `FeatureNotLicensedException` → return null (graceful)
- General `Exception` (non-cancellation) → return null (graceful)
- `OperationCanceledException` → re-thrown (respect cancellation)

---

## DI Registration

### ContextStrategyFactory Registration

Added `"knowledge"` entry to `_registrations` dictionary:
```csharp
["knowledge"] = (typeof(KnowledgeContextStrategy), LicenseTier.Teams),
```

Updated strategy count from 6 to 7 in XML documentation.

### AgentsServiceCollectionExtensions

Added transient registration in `AddContextStrategies()`:
```csharp
services.AddTransient<KnowledgeContextStrategy>();
```

---

## Files Changed

### New Files (2)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/.../Context/Strategies/KnowledgeContextConfig.cs` | Record | Agent-specific configuration |
| `src/.../Context/Strategies/KnowledgeContextStrategy.cs` | Strategy | Core strategy implementation |

### New Test File (1)

| File | Type | Description |
|:-----|:-----|:------------|
| `tests/.../Context/Strategies/KnowledgeContextStrategyTests.cs` | Tests | 46 unit tests |

### Modified Files (3)

| File | Changes |
|:-----|:--------|
| `src/.../Context/ContextStrategyFactory.cs` | Added `"knowledge"` registration, updated doc comments (6→7 strategies) |
| `src/.../Extensions/AgentsServiceCollectionExtensions.cs` | Added `AddTransient<KnowledgeContextStrategy>()`, updated doc comments |
| `tests/.../Context/ContextStrategyFactoryTests.cs` | Updated assertions (6→7 count), added `"knowledge"` contains check, added `IsAvailable_KnowledgeStrategy_ChecksTierRequirement` theory |

---

## Spec-to-Codebase Adaptations

| Spec Reference | Actual Codebase | Adaptation |
|:---|:---|:---|
| `IKnowledgeContextStrategy : IContextStrategy` | Extend `ContextStrategyBase` | No new interface — follow RAG/Style pattern |
| `ILicenseService` | `ILicenseContext` (`.Tier` property) | Use `[RequiresLicense(LicenseTier.Teams)]` attribute |
| `ContextRequest` with `AgentType` enum | `ContextGatheringRequest` with `string AgentId` | Map `AgentId` strings to configs via dictionary |
| `KnowledgeContextFragment : ContextFragment` | Not possible (positional record) | Return standard `ContextFragment` via `CreateFragment()` |
| `IEntityRelevanceScorer.ScoreEntitiesAsync()` | Wrapped by `IKnowledgeContextProvider` | Use existing provider which encapsulates ranking |
| `IAxiomStore.GetAxiomsForTypeAsync()` | Not in Abstractions module | Handled internally by `IKnowledgeContextProvider` |
| `IGraphRepository` + `IEntityRelevanceRanker` + `IKnowledgeContextFormatter` | `IKnowledgeContextProvider` | Single Abstractions-layer interface wraps entire pipeline |
| `Lexichord.KnowledgeGraph.Context.Strategy` | `Lexichord.Modules.Agents.Context.Strategies` | Codebase namespace convention |
| `Moq` test framework | `NSubstitute` | Codebase standard |
| `[Trait("Version", ...)]` | `[Trait("SubPart", "v0.7.2e")]` | Codebase standard trait key |

---

## Testing

### Test Summary

| Test File | Tests | Key Coverage |
|:----------|:-----:|:-------------|
| KnowledgeContextStrategyTests | 46 | Constructor null guards, properties, query building, results, errors, agent configs, license restrictions, hint overrides, options building |

### Test Breakdown

| Region | Count | Description |
|:-------|:-----:|:------------|
| Constructor Tests | 5 | Null guards for contextProvider, license, tokenCounter, logger + valid params |
| Property Tests | 4 | StrategyId, DisplayName, Priority=30, MaxTokens=4000 |
| GatherAsync Query Building | 4 | No selection/document → null, selection as query, document fallback, 200-char truncation |
| GatherAsync Results | 7 | No entities → null, empty formatted → null, fragment content, SourceId/Label, truncated relevance, entity count scaling, relevance cap |
| Error Handling | 3 | FeatureNotLicensedException → null, general exception → null, OperationCanceledException → rethrows |
| Agent Configuration | 5 | Editor (30), simplifier (10), unknown default (20), co-pilot (25), entity type filtering |
| License Restrictions | 3 | WriterPro caps, Teams no restrictions, WriterPro doesn't increase |
| Hint Overrides | 5 | MaxEntities, MinRelevanceScore, IncludeAxioms, KnowledgeFormat, invalid format fallback |
| GetConfigForAgent | 6 | All 5 agents + unknown |
| Options Building | 3 | Default Yaml, entity types as IReadOnlySet, null for all types |

### Test Traits

All new tests use: `[Trait("Category", "Unit")]`, `[Trait("SubPart", "v0.7.2e")]`

---

## Design Decisions

1. **Single Provider Dependency** — Instead of directly depending on `IGraphRepository`, `IEntityRelevanceRanker`, `IKnowledgeContextFormatter`, and `IAxiomStore` (which lives in the Knowledge module), the strategy depends only on `IKnowledgeContextProvider` from the Abstractions layer. This respects the module boundary constraint (Agents references only Abstractions) and delegates the full search→rank→select→format pipeline to the existing v0.6.6e implementation.

2. **Static Agent Configuration Dictionary** — Agent-specific configs are stored as a static `Dictionary<string, KnowledgeContextConfig>` within the strategy class. This keeps configuration co-located with the strategy and avoids additional DI complexity. Unknown agents receive a sensible default configuration.

3. **Entity Type List-to-Set Conversion** — The config uses `IReadOnlyList<string>?` for developer ergonomics (collection expression syntax: `["Concept", "Term"]`), while `KnowledgeContextOptions.EntityTypes` requires `IReadOnlySet<string>?`. The `BuildOptions()` method handles this conversion.

4. **WriterPro Partial Access** — WriterPro users receive entities-only context (max 10, no axioms, no relationships) rather than being fully blocked. The `[RequiresLicense(LicenseTier.Teams)]` attribute handles Core-tier blocking at the factory level.

5. **Relevance Score Heuristic** — The fragment's relevance score is derived from a simple heuristic: 0.7f when content was truncated (budget-constrained but relevant), otherwise scaled by entity count (count/10.0, capped at 1.0). This avoids the need for a weighted average of individual entity scores.

6. **Internal Visibility** — Both `KnowledgeContextConfig` and `KnowledgeContextStrategy` are `internal sealed`, following the codebase pattern for module-specific implementations.

---

## Dependencies

### Consumed (from existing modules)

| Interface | Module | Used By |
|:----------|:-------|:--------|
| `IKnowledgeContextProvider` | Lexichord.Abstractions (v0.6.6e) | KnowledgeContextStrategy (search, rank, format pipeline) |
| `ILicenseContext` | Lexichord.Abstractions (v0.0.4c) | KnowledgeContextStrategy (runtime tier checks) |
| `ITokenCounter` | Lexichord.Abstractions (v0.6.1b) | ContextStrategyBase (token estimation) |
| `KnowledgeContextOptions` | Lexichord.Abstractions (v0.6.6e) | KnowledgeContextStrategy (provider configuration) |
| `KnowledgeContext` | Lexichord.Abstractions (v0.6.6e) | KnowledgeContextStrategy (provider result) |
| `ContextFormat` | Lexichord.Abstractions (v0.6.6e) | KnowledgeContextConfig (output format) |

### No New NuGet Packages

All dependencies are existing project references. No new NuGet packages required.

---

## Known Limitations

1. **No direct axiom/relationship access** — The strategy delegates entirely to `IKnowledgeContextProvider`, which means it cannot independently query axioms or relationships. The provider's pipeline handles this internally.
2. **Static agent configurations** — Agent-specific configs are compiled into the strategy. Adding new agent types requires code changes, not runtime configuration.
3. **No document mention scoring** — The relevance scoring uses the provider's built-in ranker rather than the spec's weighted algorithm (semantic + mention + type + recency). This is because the `IKnowledgeContextProvider` encapsulates the ranking pipeline.
4. **Entity type case sensitivity** — Entity type filtering uses case-sensitive string matching (matching the `EntitySearchQuery.EntityTypes` contract).
