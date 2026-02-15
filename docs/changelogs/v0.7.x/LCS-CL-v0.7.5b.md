# Changelog: v0.7.5b — Automatic Fix Suggestions

**Feature ID:** AGT-075b
**Version:** 0.7.5b
**Date:** 2026-02-15
**Status:** Complete

---

## Overview

Implements the Automatic Fix Suggestions for the Tuning Agent, building upon v0.7.5a's Style Deviation Scanner. This is the second sub-part of v0.7.5 "The Tuning Agent" and provides AI-powered fix generation for detected style deviations.

The implementation adds:
- `IFixSuggestionGenerator` — interface for fix generation, batch processing, regeneration, and validation
- `FixSuggestionGenerator` — core service orchestrating LLM calls with DiffPlex integration
- `FixSuggestion` — comprehensive suggestion record with confidence, quality, diff, and alternatives
- `FixValidationResult` — validation status with semantic similarity scoring
- `DiffGenerator` — DiffPlex wrapper for unified and HTML diff generation
- `FixValidator` — re-linting and semantic similarity validation
- Mustache prompt template for rule-aware fix generation
- Full unit test coverage

---

## What's New

### IFixSuggestionGenerator Interface

Defines the contract for AI-powered fix suggestion generation:
- **Namespace:** `Lexichord.Abstractions.Contracts.Agents`
- **Methods:**
  - `GenerateFixAsync()` — Generates fix for single deviation with options
  - `GenerateFixesAsync()` — Batch processing with parallel execution
  - `RegenerateFixAsync()` — Regenerates with user guidance
  - `ValidateFixAsync()` — Validates fix resolves violation without introducing new ones

### FixSuggestionGenerator

Core implementation of `IFixSuggestionGenerator`:
- **Namespace:** `Lexichord.Modules.Agents.Tuning`
- **Dependencies:** `IChatCompletionService`, `IPromptRenderer`, `IPromptTemplateRepository`, `IStyleEngine`, `ILicenseContext`, `DiffGenerator`, `FixValidator`, `ILogger`
- **Key Features:**
  - License validation (requires WriterPro tier)
  - Prompt template rendering with rule context
  - JSON response parsing with alternatives extraction
  - Semaphore-based parallel generation (configurable MaxParallelism)
  - Optional fix validation via re-linting
  - Diff generation for visual comparison
  - Semantic similarity scoring

### FixSuggestion Record

Comprehensive fix suggestion data:
- **Namespace:** `Lexichord.Abstractions.Contracts.Agents`
- **Properties:**
  - `SuggestionId` — Unique identifier (Guid)
  - `DeviationId` — Link to source deviation
  - `OriginalText` — Text being fixed
  - `SuggestedText` — AI-generated replacement
  - `Explanation` — Human-readable explanation
  - `Confidence` — LLM confidence score (0.0-1.0)
  - `QualityScore` — Validation quality score (0.0-1.0)
  - `Diff` — TextDiff with operations
  - `TokenUsage` — LLM token metrics
  - `GenerationTime` — Processing duration
  - `Alternatives` — List of alternative suggestions
  - `IsValidated` — Whether validation was performed
  - `ValidationResult` — Validation details (nullable)
  - `ErrorMessage` — Error details on failure (nullable)
  - `Success` — Whether generation succeeded
- **Computed Properties:**
  - `IsHighConfidence` — True when confidence ≥0.8, quality ≥0.7, and validated
- **Factory Methods:**
  - `Failed()` — Creates failed suggestion with error message
  - `LicenseRequired()` — Creates license-required placeholder

### AlternativeSuggestion Record

Alternative fix option:
- **Namespace:** `Lexichord.Abstractions.Contracts.Agents`
- **Properties:**
  - `SuggestedText` — Alternative replacement text
  - `Explanation` — Why this alternative might be preferred
  - `Confidence` — Confidence score for alternative

### FixGenerationOptions Record

Configurable generation behavior:
- **Namespace:** `Lexichord.Abstractions.Contracts.Agents`
- **Properties:**
  - `MaxAlternatives = 2` — Maximum alternative suggestions
  - `IncludeExplanations = true` — Include explanation text
  - `MinConfidence = 0.7` — Minimum confidence threshold
  - `ValidateFixes = true` — Validate via re-linting
  - `Tone` — TonePreference (nullable)
  - `PreserveVoice = true` — Preserve author's voice
  - `MaxParallelism = 5` — Batch parallel limit
  - `UseLearningContext = true` — Use learning loop data

### FixValidationResult Record

Validation status and details:
- **Namespace:** `Lexichord.Abstractions.Contracts.Agents`
- **Properties:**
  - `ResolvesViolation` — Whether original violation is fixed
  - `IntroducesNewViolations` — Whether new violations were created
  - `NewViolations` — List of new violations (nullable)
  - `SemanticSimilarity` — Similarity score (0.0-1.0)
  - `Status` — ValidationStatus enum value
  - `Message` — Human-readable status message
- **Factory Methods:**
  - `Valid()` — Creates valid result with similarity
  - `Invalid()` — Creates invalid result with message
  - `Failed()` — Creates failed validation result

### TextDiff Record

Structured diff representation:
- **Namespace:** `Lexichord.Abstractions.Contracts.Agents`
- **Properties:**
  - `Operations` — List of DiffOperation
  - `UnifiedDiff` — Unified diff format string
  - `HtmlDiff` — HTML with CSS classes
- **Computed Properties:**
  - `Additions` — Count of additions
  - `Deletions` — Count of deletions
  - `Unchanged` — Count of unchanged
  - `TotalChanges` — Total change count
  - `HasChanges` — Whether any changes exist
- **Static Properties:**
  - `Empty` — Empty diff instance

### DiffOperation Record

Single diff operation:
- **Namespace:** `Lexichord.Abstractions.Contracts.Agents`
- **Properties:**
  - `Type` — DiffType (Unchanged, Addition, Deletion)
  - `Text` — Operation text content
  - `StartIndex` — Position in result
  - `Length` — Text length

### Enums

Three new enums for fix generation:

**TonePreference:**
- `Neutral = 0` — No tone adjustment
- `Formal = 1` — Professional, formal tone
- `Casual = 2` — Conversational, informal tone
- `Technical = 3` — Technical, precise language
- `Simplified = 4` — Simple, accessible language

**ValidationStatus:**
- `Valid = 0` — Fix is valid and safe to apply
- `ValidWithWarnings = 1` — Valid but with semantic concerns
- `Invalid = 2` — Fix doesn't resolve or introduces issues
- `ValidationFailed = 3` — Validation process failed

**DiffType:**
- `Unchanged = 0` — Unchanged text
- `Addition = 1` — Added text
- `Deletion = 2` — Deleted text

### DiffGenerator Helper

DiffPlex wrapper for diff generation:
- **Namespace:** `Lexichord.Modules.Agents.Tuning`
- **Dependencies:** `ILogger`
- **Methods:**
  - `GenerateDiff()` — Generates TextDiff from original and suggested text
- **Features:**
  - Uses `InlineDiffBuilder` for word-level diffing
  - Generates unified diff format
  - Generates HTML with CSS classes (`diff-add`, `diff-del`, `diff-unchanged`)
  - Returns `TextDiff.Empty` on error or identical text

### FixValidator Helper

Fix validation via re-linting:
- **Namespace:** `Lexichord.Modules.Agents.Tuning`
- **Dependencies:** `IStyleEngine`, `ILogger`
- **Methods:**
  - `ValidateAsync()` — Validates fix suggestion
- **Features:**
  - Applies fix to surrounding context
  - Re-runs style analysis on fixed text
  - Checks if original violation is resolved
  - Checks for new violations in fix area
  - Calculates semantic similarity (Jaccard + length ratio)
  - Returns appropriate ValidationStatus

### Semantic Similarity Algorithm

Calculates text similarity using:
1. **Jaccard Similarity (70% weight):** Word-level intersection/union
2. **Length Ratio (30% weight):** min(len)/max(len)
3. **Threshold:** < 0.7 returns `ValidWithWarnings`

### Prompt Template

Mustache-based YAML template (`tuning-agent-fix.yaml`):
- **Template ID:** `tuning-agent-fix`
- **System Prompt Variables:**
  - `rule_id`, `rule_name`, `rule_description`, `rule_category`
  - `violation_severity`, `violation_message`
  - `rule_examples`, `linter_suggested_fix` (optional)
  - `preserve_voice`, `tone` (optional)
  - `learning_context`, `accepted_patterns`, `rejected_patterns` (optional)
  - `max_alternatives`
- **User Prompt Variables:**
  - `original_text` — Text to rewrite
  - `surrounding_context` (optional) — Context for understanding
  - `user_guidance` (optional) — User feedback for regeneration
- **Output Format:** JSON with `suggested_text`, `explanation`, `confidence`, `alternatives`

### DI Registration

Extended `TuningServiceCollectionExtensions` with `AddFixSuggestionGenerator()`:
```csharp
services.AddSingleton<DiffGenerator>();
services.AddSingleton<FixValidator>();
services.AddSingleton<IFixSuggestionGenerator, FixSuggestionGenerator>();
```

Updated `AgentsModule.RegisterServices()` with `services.AddFixSuggestionGenerator()` call.

---

## Files Created

### Abstractions — Enums (3 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Abstractions/Contracts/Agents/TonePreference.cs` | Enum | Tone preferences |
| `src/Lexichord.Abstractions/Contracts/Agents/ValidationStatus.cs` | Enum | Validation status |
| `src/Lexichord.Abstractions/Contracts/Agents/DiffType.cs` | Enum | Diff operation types |

### Abstractions — Records (6 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Abstractions/Contracts/Agents/DiffOperation.cs` | Record | Single diff operation |
| `src/Lexichord.Abstractions/Contracts/Agents/TextDiff.cs` | Record | Structured diff |
| `src/Lexichord.Abstractions/Contracts/Agents/AlternativeSuggestion.cs` | Record | Alternative fix |
| `src/Lexichord.Abstractions/Contracts/Agents/FixValidationResult.cs` | Record | Validation result |
| `src/Lexichord.Abstractions/Contracts/Agents/FixGenerationOptions.cs` | Record | Generation options |
| `src/Lexichord.Abstractions/Contracts/Agents/FixSuggestion.cs` | Record | Core suggestion |

### Abstractions — Interface (1 file)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Abstractions/Contracts/Agents/IFixSuggestionGenerator.cs` | Interface | Generator contract |

### Module — Services (3 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Modules.Agents/Tuning/FixSuggestionGenerator.cs` | Class | Core implementation |
| `src/Lexichord.Modules.Agents/Tuning/DiffGenerator.cs` | Class | Diff helper |
| `src/Lexichord.Modules.Agents/Tuning/FixValidator.cs` | Class | Validation helper |

### Module — Prompt Template (1 file)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Modules.Agents/Resources/Prompts/tuning-agent-fix.yaml` | YAML | Fix generation prompt |

### Tests (1 file)

| File | Tests | Description |
|:-----|:-----:|:------------|
| `tests/Lexichord.Tests.Unit/Modules/Agents/Tuning/FixSuggestionGeneratorTests.cs` | 40 | Generator tests |

---

## Files Modified

| File | Changes |
|:-----|:--------|
| `src/Lexichord.Modules.Agents/Extensions/TuningServiceCollectionExtensions.cs` | Added `AddFixSuggestionGenerator()` method |
| `src/Lexichord.Modules.Agents/AgentsModule.cs` | Added `services.AddFixSuggestionGenerator()` call and init verification |

---

## Testing

### Test Summary

| Test File | Tests | Key Coverage |
|:----------|:-----:|:-------------|
| FixSuggestionGeneratorTests | 40 | Constructor, generate, batch, regenerate, validate, diff, records |
| **Total v0.7.5b** | **40** | All v0.7.5b functionality |

### Test Groups

| Group | Tests | Description |
|:------|:------|:------------|
| Constructor | 7 | Dependency null checks |
| GenerateFixAsync | 8 | Success, license check, template missing, validation |
| GenerateFixesAsync | 4 | Batch, empty input, parallelism |
| RegenerateFixAsync | 3 | User guidance, null checks |
| ValidateFixAsync | 3 | Valid/invalid fix, null checks |
| DiffGenerator | 5 | Identical, different, unified, HTML |
| FixValidator | 4 | No violations, still present, similarity |
| Record Tests | 6 | Factory methods, computed properties |

### Test Traits

All tests use: `[Trait("Category", "Unit")]`, `[Trait("SubPart", "v0.7.5b")]`

---

## Design Decisions

1. **Singleton Service Lifetime** — `FixSuggestionGenerator`, `DiffGenerator`, and `FixValidator` are registered as singletons for shared state and efficient resource usage.

2. **Nullable ILearningLoopService** — The learning loop service (v0.7.5d) is not yet implemented, so it's accepted as nullable with null checks.

3. **Sync IPromptTemplateRepository** — Uses the synchronous `GetTemplate()` method rather than async, matching the existing API.

4. **DiffPlex Integration** — Uses `InlineDiffBuilder` for word-level diffing, consistent with v0.7.4c's simplifier preview.

5. **Jaccard + Length Ratio** — Semantic similarity combines word overlap (70%) with length ratio (30%) for balanced assessment.

6. **Parallel Batch Processing** — Uses `SemaphoreSlim` with configurable `MaxParallelism` (default 5) for controlled concurrent generation.

7. **License Gating** — Returns `LicenseRequired()` factory method result for unlicensed users rather than throwing.

8. **Public Helper Classes** — `DiffGenerator` and `FixValidator` are public (not internal) to support constructor injection in `FixSuggestionGenerator`.

---

## Dependencies

### Consumed (from existing modules)

| Interface | Version | Used By |
|:----------|:--------|:--------|
| `IChatCompletionService` | v0.5.1a | LLM calls |
| `IPromptRenderer` | v0.5.2a | Template rendering |
| `IPromptTemplateRepository` | v0.5.2b | Template retrieval |
| `IStyleEngine` | v0.2.1a | Re-linting validation |
| `ILicenseContext` | v0.0.4c | License validation |
| `StyleDeviation` | v0.7.5a | Input deviation data |
| `StyleViolation` | v0.2.1b | Violation data |
| `UsageMetrics` | v0.5.1a | Token tracking |
| `DiffPlex` | 1.7.2 | Diff generation |

### Produced (new interfaces/classes)

| Class | Consumers |
|:------|:----------|
| `IFixSuggestionGenerator` | Tuning Agent (v0.7.5c+), UI commands |
| `FixSuggestion` | Review UI, apply commands |
| `FixGenerationOptions` | API callers |
| `FixValidationResult` | Quality assessment |
| `TextDiff` | Diff display |
| `DiffOperation` | Diff details |
| `AlternativeSuggestion` | Alternative selection |
| `TonePreference` | Generation options |
| `ValidationStatus` | Validation results |
| `DiffType` | Diff operations |
| `DiffGenerator` | Internal diff generation |
| `FixValidator` | Internal validation |

---

## Validation Logic

```
1. Apply suggested fix to surrounding context
2. Re-run IStyleEngine.AnalyzeAsync() on fixed text
3. Check if original violation is still present
   → If present: return Invalid("Fix does not resolve the original violation")
4. Check for new violations in fix area
   → If found: return Invalid with NewViolations list
5. Calculate semantic similarity
   → If < 0.7: return ValidWithWarnings("Fix may significantly alter the original meaning")
6. Return Valid(semanticSimilarity)
```

---

## License Gating

| Feature | Core | Writer | WriterPro | Teams |
|:--------|:-----|:-------|:----------|:------|
| Fix suggestion generation | - | - | ✓ | ✓ |
| Batch generation | - | - | ✓ | ✓ |
| User-guided regeneration | - | - | ✓ | ✓ |
| Fix validation | - | - | ✓ | ✓ |

---

## Build & Test Results

```
Build:     Succeeded (0 errors, 1 warning - unrelated Avalonia)
v0.7.5b:   40 passed, 0 failed
```

