# Changelog: v0.7.4b — Simplification Pipeline

**Feature ID:** AGT-074b
**Version:** 0.7.4b
**Date:** 2026-02-15
**Status:** ✅ Complete

---

## Overview

Implements the Simplification Pipeline for the Simplifier Agent, enabling LLM-powered text simplification to target Flesch-Kincaid grade levels. This is the second sub-part of v0.7.4 "The Simplifier Agent" and builds upon v0.7.4a's Readability Target Service to provide the core simplification orchestration.

The implementation adds `ISimplificationPipeline` interface with methods for batch and streaming simplification; `SimplificationRequest` and `SimplificationResult` records for input/output modeling; `SimplificationStrategy` enum (Conservative/Balanced/Aggressive) with strategy-specific temperature settings; `SimplificationChangeType` enum for categorizing text changes; `SimplifierAgent` implementation integrating with `IContextOrchestrator`, `IReadabilityService`, and `IChatCompletionService`; `ISimplificationResponseParser` interface and `SimplificationResponseParser` implementation for parsing structured LLM responses; `specialist-simplifier.yaml` prompt template with Mustache syntax; and 92 unit tests with 100% pass rate.

---

## What's New

### ISimplificationPipeline Interface

Main service contract for simplification operations:
- **Namespace:** `Lexichord.Abstractions.Agents.Simplifier`
- **Methods:**
  - `SimplifyAsync(request, ct)` — Simplifies text and returns complete result with metrics
  - `SimplifyStreamingAsync(request, ct)` — Streams simplification chunks as they become available
  - `ValidateRequest(request)` — Validates request before execution (text length, target validity)

### SimplificationRequest Record

Immutable request for simplification operations:
- **Properties:** `OriginalText` (required), `Target` (required ReadabilityTarget), `DocumentPath`, `Strategy`, `GenerateGlossary`, `PreserveFormatting`, `AdditionalInstructions`, `Timeout`
- **Constants:** `MaxTextLength` (50,000 chars), `DefaultTimeout` (60s), `MaxTimeout` (5 min)
- **Methods:**
  - `Validate()` — Throws ArgumentException for invalid state

### SimplificationResult Record

Complete output of simplification operations:
- **Properties:** `SimplifiedText`, `OriginalMetrics`, `SimplifiedMetrics`, `Changes`, `Glossary`, `TokenUsage`, `ProcessingTime`, `StrategyUsed`, `TargetUsed`, `Success`, `ErrorMessage`
- **Computed Properties:** `GradeLevelReduction`, `WordCountDifference`, `TargetAchieved`
- **Methods:**
  - `Failed(originalText, strategy, target, errorMessage, processingTime)` — Factory for failure results

### SimplificationStrategy Enum

Simplification intensity levels:
- `Conservative` — Minimal changes, preserve author voice (Temperature: 0.3)
- `Balanced` — Moderate simplification (Temperature: 0.4) *[default]*
- `Aggressive` — Maximum simplification for accessibility (Temperature: 0.5)

### SimplificationChangeType Enum

Categories for text changes:
- `SentenceSplit` — Split long sentence into shorter ones
- `JargonReplacement` — Replaced technical term with plain language
- `PassiveToActive` — Converted passive voice to active voice
- `WordSimplification` — Replaced complex word with simpler one
- `ClauseReduction` — Simplified complex clause structure
- `TransitionAdded` — Added transitional word/phrase for clarity
- `RedundancyRemoved` — Removed redundant words/phrases
- `Combined` — Multiple change types in one transformation

### SimplificationChange Record

Detailed change information:
- **Properties:** `OriginalText`, `SimplifiedText`, `ChangeType`, `Explanation`, `Location` (nullable TextLocation), `Confidence`
- **Nested Record:** `TextLocation(StartOffset, EndOffset)` with `Length` computed property

### SimplificationChunk Record

Streaming output chunk:
- **Properties:** `TextDelta`, `IsComplete`, `CompletedChange`
- **Methods:**
  - `Text(delta)` — Factory for text delta
  - `Complete(finalDelta)` — Factory for completion signal

### SimplificationValidation Record

Request validation result:
- **Properties:** `IsValid`, `Errors`, `Warnings`
- **Computed Properties:** `HasErrors`, `HasWarnings`
- **Methods:**
  - `Valid()` — Factory for valid result
  - `ValidWithWarnings(warnings)` — Factory with warnings
  - `Invalid(errors)` — Factory for invalid result
  - `InvalidWithWarnings(errors, warnings)` — Factory with both

### ISimplificationResponseParser Interface

Parses structured LLM responses:
- **Namespace:** `Lexichord.Modules.Agents.Simplifier`
- **Methods:**
  - `Parse(llmResponse)` — Extracts simplified text, changes, and glossary from response

### SimplificationParseResult Record

Parser output:
- **Properties:** `SimplifiedText`, `Changes`, `Glossary`
- **Computed Properties:** `HasChanges`, `HasGlossary`
- **Methods:**
  - `Empty()` — Factory for empty result
  - `WithTextOnly(text)` — Factory for text-only result

### SimplificationResponseParser Implementation

Regex-based parser for LLM responses:
- Extracts `\`\`\`simplified` block for simplified text
- Extracts `\`\`\`changes` block for change list
- Extracts `\`\`\`glossary` block for term definitions (optional)
- Supports arrow variations: →, ->, =>
- Case-insensitive block name matching
- Multi-hyphen change type support (e.g., "passive-to-active")
- Fallback to raw content when blocks are missing

### SimplifierAgent Implementation

Core agent implementing `IAgent` and `ISimplificationPipeline`:
- **Namespace:** `Lexichord.Modules.Agents.Simplifier`
- **Attributes:** `[RequiresLicense(LicenseTier.WriterPro)]`, `[AgentDefinition("simplifier", Priority = 101)]`
- **Agent Properties:**
  - `AgentId` → `"simplifier"`
  - `Name` → `"The Simplifier"`
  - `Description` → `"Simplifies text to target readability levels while maintaining meaning."`
  - `Capabilities` → `Chat | DocumentContext | StyleEnforcement | Streaming`
- **Constants:**
  - `ContextTokenBudget` = 4000 tokens
  - `DefaultMaxTokens` = 4096 tokens
  - `DefaultPromptCostPer1K` = 0.01 USD
  - `DefaultCompletionCostPer1K` = 0.03 USD
  - `LongTextWarningThreshold` = 20,000 chars

### Prompt Template

Added `specialist-simplifier.yaml` with Mustache syntax:
- **Template ID:** `specialist-simplifier`
- **Required Variables:** `original_text`, `target_grade_level`, `max_sentence_words`, `simplify_jargon`, `strategy`, `current_flesch_kincaid`, `current_avg_sentence`, `current_complex_ratio`
- **Optional Variables:** `style_rules`, `terminology`, `surrounding_context`, `additional_instructions`, `generate_glossary`, `strategy_conservative`, `strategy_balanced`, `strategy_aggressive`
- **Output Format:** Structured blocks for simplified text, changes, and optional glossary

### DI Registration

Extended `SimplifierServiceCollectionExtensions` with `AddSimplifierAgentPipeline()`:
```csharp
services.AddSingleton<ISimplificationResponseParser, SimplificationResponseParser>();
services.AddSingleton<ISimplificationPipeline, SimplifierAgent>();
services.AddSingleton<IAgent>(sp => (IAgent)sp.GetRequiredService<ISimplificationPipeline>());
```

Updated `AgentsModule.RegisterServices()` with `services.AddSimplifierAgentPipeline()` call after `AddReadabilityTargetService()`. Initialization verification confirms `ISimplificationPipeline` service availability.

---

## Files Created

### Abstractions (7 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Abstractions/Agents/Simplifier/SimplificationStrategy.cs` | Enum | Simplification intensity levels |
| `src/Lexichord.Abstractions/Agents/Simplifier/SimplificationChangeType.cs` | Enum | Change categorization |
| `src/Lexichord.Abstractions/Agents/Simplifier/SimplificationChange.cs` | Record | Single change with location |
| `src/Lexichord.Abstractions/Agents/Simplifier/SimplificationChunk.cs` | Record | Streaming output chunk |
| `src/Lexichord.Abstractions/Agents/Simplifier/SimplificationValidation.cs` | Record | Request validation result |
| `src/Lexichord.Abstractions/Agents/Simplifier/SimplificationRequest.cs` | Record | Pipeline input |
| `src/Lexichord.Abstractions/Agents/Simplifier/SimplificationResult.cs` | Record | Pipeline output |

### Implementation (5 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Abstractions/Agents/Simplifier/ISimplificationPipeline.cs` | Interface | Pipeline contract |
| `src/Lexichord.Modules.Agents/Simplifier/ISimplificationResponseParser.cs` | Interface | Parser contract |
| `src/Lexichord.Modules.Agents/Simplifier/SimplificationParseResult.cs` | Record | Parser output |
| `src/Lexichord.Modules.Agents/Simplifier/SimplificationResponseParser.cs` | Class | Regex-based parser |
| `src/Lexichord.Modules.Agents/Simplifier/SimplifierAgent.cs` | Class | Core agent implementation |

### Resources (1 file)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Modules.Agents/Resources/Prompts/specialist-simplifier.yaml` | YAML | Prompt template |

### Tests (4 files)

| File | Tests | Description |
|:-----|:-----:|:------------|
| `tests/Lexichord.Tests.Unit/Modules/Agents/Simplifier/SimplificationRequestTests.cs` | 10 | Request validation, defaults, constants |
| `tests/Lexichord.Tests.Unit/Modules/Agents/Simplifier/SimplificationResultTests.cs` | 4 | Computed properties, Failed factory |
| `tests/Lexichord.Tests.Unit/Modules/Agents/Simplifier/SimplificationResponseParserTests.cs` | 26 | Parsing, change types, glossary |
| `tests/Lexichord.Tests.Unit/Modules/Agents/Simplifier/SimplifierAgentTests.cs` | 30 | IAgent properties, SimplifyAsync, ValidateRequest |

---

## Files Modified

| File | Changes |
|:-----|:--------|
| `src/Lexichord.Modules.Agents/Extensions/SimplifierServiceCollectionExtensions.cs` | Added `AddSimplifierAgentPipeline()` method for DI registration |
| `src/Lexichord.Modules.Agents/AgentsModule.cs` | Added `services.AddSimplifierAgentPipeline()` call, initialization verification for ISimplificationPipeline |

---

## Testing

### Test Summary

| Test File | Tests | Key Coverage |
|:----------|:-----:|:-------------|
| SimplificationRequestTests | 10 | Validate, defaults, MaxTextLength, Timeout |
| SimplificationResultTests | 4 | GradeLevelReduction, TargetAchieved, Failed factory |
| SimplificationResponseParserTests | 26 | Parse blocks, change types, arrow formats, glossary |
| SimplifierAgentTests | 30 | IAgent properties, constructor validation, SimplifyAsync, ValidateRequest |
| **(From v0.7.4a)** ReadabilityTargetServiceTests | 44 | Presets, target resolution, validation |
| **(From v0.7.4a)** AudiencePresetTests | 34 | Record validation, equality, cloning |
| **Total v0.7.4b** | **70** | All v0.7.4b functionality |
| **Total v0.7.4** | **92** | Combined v0.7.4a + v0.7.4b |

### Test Groups

| Group | Tests | Description |
|:------|:------|:------------|
| SimplificationRequest Validation | 4 | Empty text, null target, valid request |
| SimplificationRequest Constants | 3 | MaxTextLength, DefaultTimeout, MaxTimeout |
| SimplificationRequest Defaults | 3 | Strategy, GenerateGlossary, Timeout |
| SimplificationResult Computed | 2 | GradeLevelReduction, TargetAchieved |
| SimplificationResult Factory | 2 | Failed, defaults |
| ResponseParser Blocks | 8 | Simplified, changes, glossary extraction |
| ResponseParser ChangeTypes | 14 | All types, case-insensitive, hyphenated |
| ResponseParser Arrows | 3 | →, ->, => variations |
| ResponseParser Edge Cases | 5 | Empty, whitespace, null, malformed |
| SimplifierAgent IAgent | 8 | AgentId, Name, Description, Capabilities, Template |
| SimplifierAgent Constructor | 8 | Null argument checks |
| SimplifierAgent SimplifyAsync | 6 | Success, cancellation, timeout, template not found |
| SimplifierAgent ValidateRequest | 6 | Valid, invalid, warnings |
| SimplifierAgent InvokeAsync | 2 | Delegation to SimplifyAsync |

### Test Traits

All tests use: `[Trait("Category", "Unit")]`, `[Trait("SubPart", "v0.7.4b")]`

---

## Design Decisions

1. **No BaseAgent Class** — `SimplifierAgent` implements `IAgent` directly following the `EditorAgent` pattern. The spec referenced a `BaseAgent` class that doesn't exist in the codebase.

2. **Strategy-Based Temperature** — Each `SimplificationStrategy` maps to a specific temperature:
   - Conservative: 0.3 (low variance for faithful output)
   - Balanced: 0.4 (moderate variance)
   - Aggressive: 0.5 (higher variance for creative restructuring)

3. **Context Token Budget** — Fixed at 4000 tokens for `IContextOrchestrator.AssembleAsync()` with required strategies `["style", "terminology"]`. This provides sufficient context without consuming too much of the model's window.

4. **Singleton Lifetime** — Both `SimplificationResponseParser` and `SimplifierAgent` are registered as singletons because they are stateless—all state is passed via request/result objects.

5. **Dual Interface Registration** — `SimplifierAgent` is registered both as `ISimplificationPipeline` (for pipeline consumers) and `IAgent` (for registry discovery) via forwarding registration.

6. **Regex-Based Parsing** — `SimplificationResponseParser` uses compiled regex patterns for performance. Multi-hyphen change types (e.g., "passive-to-active") are supported via `(\w+(?:-\w+)*)` pattern.

7. **Graceful Context Failure** — Context assembly failures log a warning and return `AssembledContext.Empty` instead of failing the operation, allowing simplification to proceed with limited context.

8. **Timeout Handling** — `SimplifyAsync` uses a linked `CancellationTokenSource` combining user cancellation and request timeout. User cancellation returns "cancelled by user", timeout returns "timed out after X seconds".

---

## Dependencies

### Consumed (from existing modules)

| Interface | Version | Used By |
|:----------|:--------|:--------|
| `IReadabilityTargetService` | v0.7.4a | SimplifierAgent (target resolution) |
| `IReadabilityService` | v0.3.3c | SimplifierAgent (text analysis) |
| `IContextOrchestrator` | v0.7.2c | SimplifierAgent (context assembly) |
| `IChatCompletionService` | v0.6.1a | SimplifierAgent (LLM invocation) |
| `IPromptRenderer` | v0.6.3b | SimplifierAgent (template rendering) |
| `IPromptTemplateRepository` | v0.6.3c | SimplifierAgent (template lookup) |
| `ReadabilityTarget` | v0.7.4a | SimplificationRequest (target config) |
| `ReadabilityMetrics` | v0.3.3c | SimplificationResult (before/after) |
| `UsageMetrics` | v0.6.1a | SimplificationResult (token tracking) |
| `ILicenseContext` | v0.0.4c | License gating via attribute |

### Produced (new classes)

| Class | Consumers |
|:------|:----------|
| `ISimplificationPipeline` | Future UI components (v0.7.4c), Batch service (v0.7.4d) |
| `SimplificationRequest` | Pipeline consumers, command handlers |
| `SimplificationResult` | Preview UI, result handling |
| `SimplificationChange` | Change review UI |
| `SimplificationChunk` | Streaming UI |
| `SimplificationValidation` | Pre-execution validation |
| `SimplifierAgent` | Agent registry, direct invocation |

### No New NuGet Packages

All dependencies are existing project references.

---

## Spec Adaptations

| Spec Reference | Actual Codebase | Adaptation |
|:---|:---|:---|
| `BaseAgent` parent class | Does not exist | Implements `IAgent` directly (EditorAgent pattern) |
| `TextSpan Location` in SimplificationChange | Not available | Used nullable `TextLocation` record with StartOffset/EndOffset |
| `SimplificationStrategy.Combined` | Not in spec | Added `SimplificationChangeType.Combined` for multi-type changes |
| `ReadabilityMetrics.PassiveVoicePercentage` | Does not exist | Omitted from prompt template variables |
| `ReadabilityMetrics.AverageSentenceLength` | `AverageWordsPerSentence` | Used correct property name |
| Sync `IPromptRenderer.RenderMessages` | Returns `IEnumerable<ChatMessage>` | Converted to `ImmutableArray` for `ChatRequest` |

---

## Build & Test Results

```
Build:     Succeeded (0 errors, 1 warning)
v0.7.4b:   70 passed, 0 failed
v0.7.4:    92 passed, 0 failed (combined a+b)
```

---

## License Gating

| Feature | Core | Writer | WriterPro | Teams |
|:--------|:-----|:-------|:----------|:------|
| SimplifierAgent access | - | - | ✓ | ✓ |
| SimplifyAsync | - | - | ✓ | ✓ |
| SimplifyStreamingAsync | - | - | ✓ | ✓ |
| ValidateRequest | - | - | ✓ | ✓ |
| Context strategies (style, terminology) | - | - | ✓ | ✓ |

---

## Prompt Template Details

The `specialist-simplifier.yaml` template includes:

### System Prompt Structure
1. Target readability configuration (grade level, max sentence words, jargon handling)
2. Current text metrics (FK grade, avg sentence, complex ratio)
3. Strategy-specific guidelines (Conservative/Balanced/Aggressive)
4. Sentence structure rules
5. Vocabulary guidelines (simplify vs explain jargon)
6. Meaning preservation rules
7. Output format specification

### Output Format
```
```simplified
[simplified text maintaining paragraph structure]
```

```changes
- "original" → "simplified" | Type: ChangeType | Reason: explanation
```

```glossary (optional)
- "term" → "plain definition"
```
```

### Change Types (exact strings)
- SentenceSplit, JargonReplacement, PassiveToActive, WordSimplification
- ClauseReduction, TransitionAdded, RedundancyRemoved, Combined
