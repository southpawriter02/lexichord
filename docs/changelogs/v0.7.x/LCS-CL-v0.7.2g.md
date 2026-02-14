# Changelog: v0.7.2g — Knowledge Context Formatter

**Feature ID:** CTX-072g
**Version:** 0.7.2g
**Date:** 2026-02-14
**Status:** ✅ Complete

---

## Overview

Enhances the Knowledge Context Formatter with metadata-rich output, token budget enforcement, and improved formatting across all four output formats. Extends the existing `IKnowledgeContextFormatter` interface (v0.6.6e) with `FormatWithMetadata()` returning a `FormattedContext` record, and `TruncateToTokenBudget()` for token budget management. Adds `ITokenCounter` integration for accurate token estimation, YAML header/escaping/severity, Markdown entity grouping by type, entity name resolution for relationships, JSON null suppression, and Plain text section headers.

This sub-part builds upon v0.6.6e's basic formatting and v0.7.2f's Entity Relevance Scorer, providing enhanced context serialization for all specialist agents consuming knowledge context.

---

## What's New

### FormattedContext Record

Formatted knowledge context with metadata for token budget management:
- **Namespace:** `Lexichord.Abstractions.Contracts.Knowledge.Copilot`
- **Pattern:** `sealed record` with `init` setters for `with`-expression composition
- **Properties:** `Content` (required string), `TokenCount` (int), `Format` (ContextFormat), `EntityCount`, `RelationshipCount`, `AxiomCount` (int), `WasTruncated` (bool)
- **Static:** `Empty` property returning zero-count context with Markdown format
- **Purpose:** Wraps the raw formatted string with metadata about content for informed budget allocation decisions

### IKnowledgeContextFormatter Interface Extensions

Two new methods added to the existing v0.6.6e interface:
- **`FormatWithMetadata(entities, relationships?, axioms?, ContextFormatOptions)`** → `FormattedContext`: Formats data and returns rich metadata including token count, item counts, and format used
- **`TruncateToTokenBudget(FormattedContext, int maxTokens)`** → `FormattedContext`: Truncates content at clean boundaries with 5% safety buffer and truncation marker

### KnowledgeContextFormatter Implementation Enhancements

Enhanced internal sealed class with:
- **Constructor:** Added optional `ITokenCounter?` parameter for accurate token estimation (falls back to char/4 heuristic when null)
- **`FormatWithMetadata()`:** Builds entity ID→name lookup dictionary, delegates to format methods, returns `FormattedContext` with complete metadata
- **`TruncateToTokenBudget()`:** Character-to-token ratio calculation, 5% safety buffer, clean newline boundary truncation, `[Context truncated due to token limit]` marker
- **`EstimateTokens()`:** Enhanced to use `ITokenCounter.CountTokens()` when available
- **`FormatContext()`:** Now delegates to `FormatWithMetadata().Content` for unified pipeline
- **Entity Name Resolution:** `ResolveEntityName()` resolves relationship GUIDs to entity names via lookup dictionary, falls back to short GUID prefix for unknown entities

### Format-Specific Enhancements

**YAML:**
- `# Knowledge Context` header comment
- `EscapeYaml()` for string values (quotes, newlines)
- `FormatPropertyValue()` type-aware formatting (strings quoted, bools lowercase, null literal)
- Axiom `severity:` field
- Entity name resolution for relationships (quoted and escaped)

**Markdown:**
- Entity grouping by type with `**TypeName**` headers
- Entity name resolution for relationships (`{from} **{type}** {to}`)
- Axiom severity display: `**{name}** ({severity}): {description}`

**JSON:**
- `JsonIgnoreCondition.WhenWritingNull` for compact output (null collections omitted)
- Axiom `severity` field (string representation)
- Entity name resolution for relationships

**Plain:**
- `KNOWLEDGE CONTEXT:` top-level header
- `RELATIONSHIPS:` section header
- `RULES:` section header
- Entity name resolution for relationships

---

## DI Registration

### KnowledgeModule.RegisterServices() Update

Replaced direct type registration with factory lambda:
```csharp
services.AddSingleton<IKnowledgeContextFormatter>(sp =>
    new KnowledgeContextFormatter(
        sp.GetRequiredService<ILogger<KnowledgeContextFormatter>>(),
        sp.GetService<ITokenCounter>()));
```

Optional `ITokenCounter?` resolution — when not registered, falls back to char/4 heuristic.

### KnowledgeContextProvider Integration

Updated both `GetContextAsync()` and `GetContextForEntitiesAsync()` to use `FormatWithMetadata()` instead of separate `FormatContext()` + `EstimateTokens()` calls. Uses `formattedResult.Content` and `formattedResult.TokenCount` from the returned `FormattedContext`.

---

## Files Changed

### New Files (3)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/.../Contracts/Knowledge/Copilot/FormattedContext.cs` | Record | Formatted context with metadata |
| `tests/.../Copilot/KnowledgeContextFormatterEnhancedTests.cs` | Tests | 19 unit tests for v0.7.2g |
| `tests/.../Copilot/FormattedContextTests.cs` | Tests | 5 unit tests for FormattedContext record |

### Modified Files (5)

| File | Changes |
|:-----|:--------|
| `src/.../Contracts/Knowledge/Copilot/IKnowledgeContextFormatter.cs` | Added v0.7.2g version tag, `FormatWithMetadata()`, `TruncateToTokenBudget()` |
| `src/.../Copilot/Context/KnowledgeContextFormatter.cs` | Major enhancement: ITokenCounter?, FormatWithMetadata, TruncateToTokenBudget, enhanced format methods, helper methods |
| `src/.../KnowledgeModule.cs` | Factory lambda for IKnowledgeContextFormatter with optional ITokenCounter? |
| `src/.../Copilot/Context/KnowledgeContextProvider.cs` | Use FormatWithMetadata() in both methods |
| `tests/.../Copilot/KnowledgeContextFormatterTests.cs` | Updated constructor reflection (null ITokenCounter?), assertions for v0.7.2g output |

### Modified Test Files (1)

| File | Changes |
|:-----|:--------|
| `tests/.../Copilot/KnowledgeContextProviderTests.cs` | Fixed constructor reflection (pass null scorer for v0.7.2f), updated mocks for FormatWithMetadata() |

---

## Spec-to-Codebase Adaptations

| Spec Reference | Actual Codebase | Adaptation |
|:---|:---|:---|
| `Lexichord.KnowledgeGraph.Context.Formatting` | `Lexichord.Modules.Knowledge.Copilot.Context` | Module namespace convention |
| New `IKnowledgeContextFormatter` | Extend existing v0.6.6e interface | Interface already exists, add new methods |
| `ITokenEstimator` / `SimpleTokenEstimator` | `ITokenCounter?` (v0.6.1b, optional) | Avoid ghost dependency — use existing interface |
| `KnowledgeContextConfig` parameter | `ContextFormatOptions` parameter | Use existing type from v0.6.6e |
| `rel.FromEntityName` / `rel.ToEntityName` | Entity ID → name dictionary lookup | `KnowledgeRelationship` only has GUID fields |
| `config.IncludeProperties` | `ContextFormatOptions.MaxPropertiesPerEntity` | Use `.Take(maxPropertiesPerEntity)` |
| `public class` | `internal sealed class` | Codebase convention |
| Moq test framework | NSubstitute + FluentAssertions | Codebase standard for Knowledge module tests |
| `[Trait("Version", ...)]` | `[Trait("SubPart", "v0.7.2g")]` | Codebase trait convention |
| Reflection `Activator.CreateInstance(type, logger)` | `Activator.CreateInstance(type, logger, null)` | Constructor accepts optional ITokenCounter? |

---

## Testing

### Test Summary

| Test File | Tests | Key Coverage |
|:----------|:-----:|:-------------|
| FormattedContextTests | 5 | Empty property, default values, with-expression, all properties, record equality |
| KnowledgeContextFormatterEnhancedTests | 19 | FormatWithMetadata (YAML, Markdown, JSON, Plain), metadata, truncation, entity name resolution |
| KnowledgeContextFormatterTests (updated) | 7 | v0.6.6e backward compatibility with v0.7.2g output |
| KnowledgeContextProviderTests (updated) | 5 | Provider integration with FormatWithMetadata |
| **Total new** | **24** | All v0.7.2g tests |
| **Total updated** | **12** | v0.6.6e tests updated for compatibility |

### Test Traits

All new tests use: `[Trait("Category", "Unit")]`, `[Trait("SubPart", "v0.7.2g")]`

---

## Design Decisions

1. **Extend Existing Interface, Not Replace** — `IKnowledgeContextFormatter` already exists from v0.6.6e. Added `FormatWithMetadata()` and `TruncateToTokenBudget()` as new methods, keeping `FormatContext()` and `EstimateTokens()` for backward compatibility. `FormatContext()` now delegates to `FormatWithMetadata().Content` for unified output.

2. **Optional ITokenCounter** — Not all deployments have a tokenizer configured. When `ITokenCounter` is null, the formatter falls back to the char/4 heuristic. This maintains zero-disruption deployment.

3. **Entity Name Resolution via Dictionary** — `KnowledgeRelationship` only has `FromEntityId`/`ToEntityId` GUIDs. The formatter builds an entity ID → name lookup from the provided entity list. Unknown IDs (entities not in the current context set) fall back to a short GUID prefix for readability.

4. **5% Safety Buffer for Truncation** — Token estimation is inherently approximate. The 5% buffer in `TruncateToTokenBudget()` accounts for estimation variance, ensuring the truncated content stays within the actual token budget.

5. **Clean Boundary Truncation** — Truncation prefers newline boundaries (within 80% of target length) to avoid mid-line cuts that would produce malformed output. The `[Context truncated due to token limit]` marker signals to the LLM that context is incomplete.

6. **Singleton Lifetime** — The formatter is stateless (ITokenCounter is read-only, no mutable state). Factory lambda in DI registration optionally resolves `ITokenCounter`.

7. **JSON Null Suppression** — `JsonIgnoreCondition.WhenWritingNull` produces cleaner JSON output when relationships or axioms are null, reducing noise for LLM consumption.

---

## Dependencies

### Consumed (from existing modules)

| Interface | Module | Used By |
|:----------|:-------|:--------|
| `ITokenCounter` | Lexichord.Abstractions (v0.6.1b) | KnowledgeContextFormatter (accurate token estimation, optional) |
| `KnowledgeEntity` | Lexichord.Abstractions (v0.4.5e) | KnowledgeContextFormatter (entity formatting and name resolution) |
| `KnowledgeRelationship` | Lexichord.Abstractions (v0.4.5e) | KnowledgeContextFormatter (relationship formatting) |
| `Axiom` | Lexichord.Abstractions (v0.4.6e) | KnowledgeContextFormatter (axiom formatting with severity) |
| `ContextFormatOptions` | Lexichord.Abstractions (v0.6.6e) | KnowledgeContextFormatter (format configuration) |
| `ContextFormat` | Lexichord.Abstractions (v0.6.6e) | FormattedContext (format metadata) |

### No New NuGet Packages

All dependencies are existing project references. `System.Text.Json` and `Microsoft.Extensions.Logging.Abstractions` were already referenced.

---

## Known Limitations

1. **Entity name resolution scope** — Only entities in the current context set can be resolved to names. Relationship endpoints referencing entities outside the context fall back to short GUID prefixes.
2. **Static token estimation fallback** — When `ITokenCounter` is unavailable, the char/4 heuristic may be inaccurate for non-English content or highly structured output formats.
3. **No format-specific truncation** — `TruncateToTokenBudget()` uses character-based truncation regardless of the output format. This may produce invalid YAML or JSON if truncated mid-structure. However, the truncation marker signals this to the consumer.
4. **Axiom severity as string** — JSON output uses `Severity.ToString()` producing the enum name (e.g., "Error", "Warning"). No numeric severity mapping is provided.
