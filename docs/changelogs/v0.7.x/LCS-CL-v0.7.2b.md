# Changelog: v0.7.2b — Built-in Context Strategies

**Feature ID:** CTX-072b
**Version:** 0.7.2b
**Date:** 2026-02-14
**Status:** ✅ Complete

---

## Overview

Implements 6 concrete context strategy classes that gather contextual information from different sources (document content, selected text, cursor position, heading hierarchy, RAG search results, and style rules) for AI agents. Populates the `ContextStrategyFactory` registrations and DI container with these implementations, completing the strategy layer of the Context Assembler system.

This sub-part builds upon v0.7.2a's abstraction layer (interfaces, records, base class, factory) and delivers the actual data-gathering logic that powers intelligent context assembly.

---

## What's New

### DocumentContextStrategy

Full document content provider with smart truncation:
- **StrategyId:** `document`
- **Priority:** 100 (Critical) — Document content is essential for most agent tasks
- **MaxTokens:** 4000
- **License:** WriterPro+
- **Logic:** Two-tier document access via `IEditorService.GetDocumentByPath()` with fallback to `GetDocumentText()` for active document. Enhanced line-by-line truncation respects heading and paragraph boundaries.
- **Dependencies:** `IEditorService`, `ITokenCounter`, `ILogger<T>`

### SelectionContextStrategy

User-selected text with surrounding paragraph context:
- **StrategyId:** `selection`
- **Priority:** 80 (High) — Selection represents explicit user focus
- **MaxTokens:** 1000
- **License:** WriterPro+
- **Logic:** Wraps selected text with `<<SELECTED_TEXT>>` / `<</SELECTED_TEXT>>` markers. Includes surrounding paragraphs from the document when cursor position is available.
- **Dependencies:** `IEditorService`, `ITokenCounter`, `ILogger<T>`

### CursorContextStrategy

Text window around cursor position with marker:
- **StrategyId:** `cursor`
- **Priority:** 80 (High)
- **MaxTokens:** 500
- **License:** WriterPro+
- **Logic:** Extracts configurable text window centered on cursor (default 500 chars), expands to word boundaries, inserts `▌` cursor marker at exact position. Configurable via `WindowSize` hint.
- **Dependencies:** `IEditorService`, `ITokenCounter`, `ILogger<T>`

### HeadingContextStrategy

Document heading hierarchy (outline):
- **StrategyId:** `heading`
- **Priority:** 70 (Medium + 10)
- **MaxTokens:** 300
- **License:** WriterPro+
- **Logic:** Resolves file path to document via `IDocumentRepository.GetByFilePathAsync()`, builds heading tree via `IHeadingHierarchyService.BuildHeadingTreeAsync()`, formats as indented outline. Optionally includes breadcrumb for current cursor location.
- **Dependencies:** `IHeadingHierarchyService`, `IDocumentRepository`, `ITokenCounter`, `ILogger<T>`
- **Note:** Uses `Guid.Empty` for projectId, matching `DocumentIndexingPipeline` pattern.

### RAGContextStrategy

Semantically related documentation via search:
- **StrategyId:** `rag`
- **Priority:** 60 (Medium)
- **MaxTokens:** 2000
- **License:** Teams+
- **Logic:** Uses selected text (preferred) or document file name as search query. Configurable `TopK` (default 3) and `MinScore` (default 0.7) via hints. Gracefully handles `FeatureNotLicensedException` and general exceptions; re-throws `OperationCanceledException`.
- **Dependencies:** `ISemanticSearchService`, `ITokenCounter`, `ILogger<T>`

### StyleContextStrategy

Active style rules for consistency guidance:
- **StrategyId:** `style`
- **Priority:** 50 (Optional + 30)
- **MaxTokens:** 1000
- **License:** Teams+
- **Logic:** Retrieves active `StyleSheet` from `IStyleEngine`, filters rules by agent type (editor→Syntax/Terminology, simplifier→Formatting/Syntax, tuning→Terminology, others→all), formats grouped by `RuleCategory`.
- **Dependencies:** `IStyleEngine`, `ITokenCounter`, `ILogger<T>`
- **Note:** No document, selection, or cursor required — style rules apply workspace-wide.

---

## Factory & DI Updates

### ContextStrategyFactory Registrations

Populated the static `_registrations` dictionary with 6 entries:
- `["document"]` → `(typeof(DocumentContextStrategy), LicenseTier.WriterPro)`
- `["selection"]` → `(typeof(SelectionContextStrategy), LicenseTier.WriterPro)`
- `["cursor"]` → `(typeof(CursorContextStrategy), LicenseTier.WriterPro)`
- `["heading"]` → `(typeof(HeadingContextStrategy), LicenseTier.WriterPro)`
- `["rag"]` → `(typeof(RAGContextStrategy), LicenseTier.Teams)`
- `["style"]` → `(typeof(StyleContextStrategy), LicenseTier.Teams)`

### DI Registration

Added transient registrations for all 6 strategies in `AddContextStrategies()`:
- Strategies are registered as `Transient` (lightweight, stateless, fresh per resolution)
- Factory remains `Singleton` for application-wide strategy management

---

## Files Changed

### New Files (12)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/.../Strategies/DocumentContextStrategy.cs` | Implementation | Document content strategy |
| `src/.../Strategies/SelectionContextStrategy.cs` | Implementation | Selected text strategy |
| `src/.../Strategies/CursorContextStrategy.cs` | Implementation | Cursor window strategy |
| `src/.../Strategies/HeadingContextStrategy.cs` | Implementation | Heading hierarchy strategy |
| `src/.../Strategies/RAGContextStrategy.cs` | Implementation | Semantic search strategy |
| `src/.../Strategies/StyleContextStrategy.cs` | Implementation | Style rules strategy |
| `tests/.../Strategies/DocumentContextStrategyTests.cs` | Tests | 12 tests |
| `tests/.../Strategies/SelectionContextStrategyTests.cs` | Tests | 8 tests |
| `tests/.../Strategies/CursorContextStrategyTests.cs` | Tests | 18 tests |
| `tests/.../Strategies/HeadingContextStrategyTests.cs` | Tests | 14 tests |
| `tests/.../Strategies/RAGContextStrategyTests.cs` | Tests | 16 tests |
| `tests/.../Strategies/StyleContextStrategyTests.cs` | Tests | 17 tests |

### Modified Files (3)

| File | Changes |
|:-----|:--------|
| `src/.../Context/ContextStrategyFactory.cs` | Populated `_registrations` dictionary, added `using Strategies`, updated XML docs |
| `src/.../Extensions/AgentsServiceCollectionExtensions.cs` | Added 6 transient strategy registrations in `AddContextStrategies()` |
| `tests/.../Context/ContextStrategyFactoryTests.cs` | Updated tests for real strategy IDs, tier filtering, DI resolution |

---

## Spec-to-Codebase Adaptations

The design spec (LCS-DES-072b) referenced several interfaces that differ from the actual codebase. These adaptations were made:

| Spec Reference | Actual Codebase | Adaptation |
|:---|:---|:---|
| `IEditorService.GetContentAsync(path, ct)` | `IEditorService.GetDocumentByPath(path)` (sync) | Used sync access with `Task.FromResult` |
| `IDocumentStructureService` | `IHeadingHierarchyService` | Used `BuildHeadingTreeAsync()` with `HeadingNode` |
| `IStyleRuleRepository` + `IStyleProfile` | `IStyleEngine` | Used `GetActiveStyleSheet()` → `GetEnabledRules()` |
| `SearchOptions(TopK: 3)` constructor | Property init syntax | Used `new SearchOptions { TopK = 3 }` |
| `SearchHit.DocumentId`, `.DocumentTitle` | `SearchHit.Document.FilePath`, `.Score` | Accessed via navigation properties |
| Selection Priority = 90 (spec) | Priority = 80 (High) | Aligned with `StrategyPriority.High` constant |

---

## Testing

### Test Summary

| Test File | Tests | Key Coverage |
|:----------|:-----:|:-------------|
| DocumentContextStrategyTests | 12 | Constructor, properties, document retrieval, fallback, truncation |
| SelectionContextStrategyTests | 8 | Selection markers, surrounding context, no-selection handling |
| CursorContextStrategyTests | 18 | Window extraction, word boundary expansion, cursor marker, relevance |
| HeadingContextStrategyTests | 14 | Path resolution, tree formatting, breadcrumb, nested headings |
| RAGContextStrategyTests | 16 | Search query, results formatting, exception handling, cancellation |
| StyleContextStrategyTests | 17 | Rule filtering by agent, category formatting, empty sheet handling |
| ContextStrategyFactoryTests (updated) | 22 | Tier filtering with real IDs, DI resolution, strategy counts |
| **Total** | **107** | New v0.7.2b tests + updated v0.7.2a tests |

### Test Traits

All new tests use: `[Trait("Category", "Unit")]`, `[Trait("SubPart", "v0.7.2b")]`

---

## Design Decisions

1. **Sync strategies use `Task.FromResult`** — `IEditorService` is synchronous, but `IContextStrategy.GatherAsync` is async. Wrapping with `Task.FromResult` avoids unnecessary thread pool usage while maintaining the async contract.

2. **`TruncateSmartly` in DocumentContextStrategy** — Enhanced line-by-line truncation (vs base class paragraph truncation) because document content benefits from heading-aware breaking points.

3. **Agent-specific rule filtering in StyleContextStrategy** — Different agents benefit from different rule categories. The switch expression provides targeted guidance without overloading agents with irrelevant rules.

4. **`Guid.Empty` for projectId** — Following existing `DocumentIndexingPipeline` pattern until proper `IProjectContext` is introduced.

5. **Transient DI lifetime for strategies** — Strategies are lightweight and stateless; transient lifetime ensures fresh logger/service references per resolution cycle.

---

## Dependencies

### Consumed (from existing modules)

| Interface | Module | Used By |
|:----------|:-------|:--------|
| `IEditorService` | Lexichord.Modules.Editor | Document, Selection, Cursor |
| `IHeadingHierarchyService` | Lexichord.Abstractions | Heading |
| `IDocumentRepository` | Lexichord.Abstractions | Heading |
| `ISemanticSearchService` | Lexichord.Abstractions | RAG |
| `IStyleEngine` | Lexichord.Abstractions | Style |
| `ITokenCounter` | Lexichord.Abstractions | All strategies (via base) |
| `ILicenseContext` | Lexichord.Host | Factory (tier gating) |

### No New NuGet Packages

All dependencies are existing project references.

---

## Known Limitations

1. **Heading strategy requires indexed document** — Documents not in the RAG index will not produce heading context.
2. **Cursor estimation is approximate** — `EstimateChunkIndex` uses ~1000 chars/chunk for breadcrumb lookup.
3. **No strategy configuration UI** — Strategy MaxTokens and hints are code-defined; runtime configuration deferred to v0.7.2c orchestrator.
4. **Selection priority adjusted** — Spec listed 90, implementation uses 80 (StrategyPriority.High) to match the standard priority constant.
