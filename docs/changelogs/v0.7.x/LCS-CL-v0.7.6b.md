# Changelog: v0.7.6b — Metadata Extraction

**Feature ID:** AGT-076b
**Version:** 0.7.6b
**Date:** 2026-02-17
**Status:** ✅ Complete

---

## Overview

Implements Metadata Extraction for the Summarizer Agent feature set, enabling automated extraction of structured metadata from documents. The agent extracts key terms with importance scoring, high-level concepts, tag suggestions aligned with existing workspace taxonomy, reading time estimates, complexity scores, and document type classification. This is the second sub-part of v0.7.6 "The Summarizer Agent."

The implementation adds `IMetadataExtractor` interface with methods for file-based and content-based extraction, tag suggestion, reading time calculation, and key term extraction; `DocumentType` enum with 15 classification types; `KeyTerm` record with importance scoring and technical indicators; `MetadataExtractionOptions` and `DocumentMetadata` records for input/output modeling; `MetadataExtractor` implementation integrating with `IPromptRenderer`, `IFileService`, and `IChatCompletionService`; `metadata-extractor.yaml` prompt template with Mustache syntax; MediatR events for started/completed/failed lifecycle; and 121 unit tests with 100% pass rate.

---

## What's New

### DocumentType Enum

Document classification categories:
- **Namespace:** `Lexichord.Abstractions.Agents.MetadataExtraction`
- **Values:**
  - `Unknown` (0) — Default for unclassified documents
  - `Article` (1) — Blog posts, news articles, opinion pieces
  - `Tutorial` (2) — Step-by-step instructional content
  - `HowTo` (3) — Task-focused procedural guides
  - `Reference` (4) — Technical reference documentation
  - `APIDocumentation` (5) — API specifications and references
  - `Specification` (6) — Technical specifications
  - `Report` (7) — Analytical reports, research findings
  - `Whitepaper` (8) — In-depth technical or policy documents
  - `Proposal` (9) — Business proposals, project proposals
  - `Meeting` (10) — Meeting notes, minutes
  - `Notes` (11) — General notes, informal documentation
  - `Readme` (12) — README files, project overviews
  - `Changelog` (13) — Version changelogs, release notes
  - `Other` (14) — Miscellaneous documents

### KeyTerm Record

Extracted term with metadata:
- **Namespace:** `Lexichord.Abstractions.Agents.MetadataExtraction`
- **Properties:**
  - `Term` (required) — The extracted term or phrase
  - `Importance` (0.0-1.0) — Relevance score; default 0.5
  - `Frequency` — Occurrence count in document; default 1
  - `IsTechnical` — Whether term is domain-specific; default false
  - `Definition` (nullable) — Brief definition if available
  - `Category` (nullable) — Grouping category
  - `RelatedTerms` (nullable) — Associated terms
- **Factory Methods:**
  - `Create(term, importance, frequency)` — Creates technical term
  - `CreateSimple(term)` — Creates non-technical term with defaults

### MetadataExtractionOptions Record

Immutable configuration for extraction requests:
- **Properties:**
  - `MaxKeyTerms` (1-50, default 10) — Maximum key terms to extract
  - `MaxConcepts` (1-20, default 5) — Maximum high-level concepts
  - `MaxTags` (1-20, default 5) — Maximum tag suggestions
  - `MinimumTermImportance` (0.0-1.0, default 0.3) — Term importance threshold
  - `WordsPerMinute` (100-400, default 200) — Reading speed for time calculation
  - `ExistingTags` (nullable) — Workspace tags for matching
  - `ExtractNamedEntities` (default true) — Extract people, organizations, products
  - `InferAudience` (default true) — Infer target audience
  - `CalculateComplexity` (default true) — Calculate complexity score
  - `DetectDocumentType` (default true) — Classify document type
  - `IncludeDefinitions` (default false) — Include term definitions
  - `MaxResponseTokens` (default 2048) — LLM response token limit
- **Methods:**
  - `Validate()` — Throws ArgumentException for invalid state
- **Static Properties:**
  - `Default` — Standard configuration with all defaults

### DocumentMetadata Record

Complete output of extraction operations:
- **Properties:**
  - `Title` (nullable) — Inferred document title
  - `Description` (nullable) — Brief document summary
  - `KeyTerms` (required) — Extracted key terms with importance
  - `Concepts` (required) — High-level concepts
  - `SuggestedTags` (required) — Workspace-aligned tag suggestions
  - `ReadingTimeMinutes` — Estimated reading time
  - `ComplexityScore` (1-10) — Document complexity rating
  - `WordCount` — Total word count
  - `DocumentType` — Classified document type
  - `TargetAudience` (nullable) — Inferred audience
  - `NamedEntities` (nullable) — Extracted named entities
  - `Usage` (required) — UsageMetrics for token tracking
  - `ExtractedAt` — UTC timestamp of extraction
  - `Success` — Whether extraction succeeded
  - `ErrorMessage` (nullable) — Error details if failed
- **Factory Methods:**
  - `Failed(errorMessage, wordCount)` — Creates failed result with zero metrics
  - `CreateMinimal(keyTerms, concepts, suggestedTags, readingTimeMinutes, complexityScore, wordCount, usage)` — Creates minimal successful result

### IMetadataExtractor Interface

Main service contract for metadata extraction:
- **Namespace:** `Lexichord.Abstractions.Agents.MetadataExtraction`
- **Extends:** `IAgent`
- **Methods:**
  - `ExtractAsync(documentPath, options, ct)` — Extracts metadata from file path via IFileService
  - `ExtractFromContentAsync(content, options, ct)` — Extracts metadata from provided content
  - `SuggestTagsAsync(content, existingTags, maxSuggestions, ct)` — Suggests tags aligned with existing taxonomy
  - `CalculateReadingTime(content, wordsPerMinute)` — Calculates reading time (algorithmic, no LLM)
  - `ExtractKeyTermsAsync(content, maxTerms, ct)` — Extracts key terms only (lightweight)
  - `GetDefaultOptions()` — Returns default extraction options

### MetadataExtractor Implementation

Core agent implementing `IAgent` and `IMetadataExtractor`:
- **Namespace:** `Lexichord.Modules.Agents.MetadataExtraction`
- **Attributes:** `[RequiresLicense(LicenseTier.WriterPro, FeatureCode = FeatureCodes.MetadataExtraction)]`, `[AgentDefinition("metadata-extractor", Priority = 103)]`
- **Agent Properties:**
  - `AgentId` → `"metadata-extractor"`
  - `Name` → `"The Metadata Extractor"`
  - `Description` → `"Extracts structured metadata from documents including key terms, concepts, tags, reading time, complexity score, and document classification."`
  - `Capabilities` → `Chat | DocumentContext | Summarization | StructureAnalysis`
- **Constants:**
  - `TemplateId` = "metadata-extractor"
  - `KeyTermsTemplateId` = "metadata-extractor-keyterms"
  - `DefaultPromptCostPer1K` = 0.01 USD
  - `DefaultCompletionCostPer1K` = 0.03 USD
  - `DefaultTimeout` = 60 seconds

### Reading Time Algorithm (spec §5.2)

Algorithmic calculation without LLM:
```
baseMinutes = wordCount / wordsPerMinute
+ codeBlocks * 0.5          // +30 sec per code block
+ tables * 0.5              // +30 sec per table (rows/3)
+ images * 0.2              // +12 sec per image
* 1.10 if avgSentence > 25  // +10% for complex sentences
* 1.20 if techDensity > 10% // +20% for technical content
minimum = 1 minute
```

### Complexity Scoring Algorithm (spec §5.3)

Document complexity scoring (1-10):
```
base = 5
+1 if technicalDensity > 15%
-1 if technicalDensity < 5%
+1 if avgWordsPerSentence > 25
-1 if avgWordsPerSentence < 12
+0.5 if hasNestedHeadings && hasTables
-0.5 if simpleStructure
clamp(1, 10)
```

### Tag Matching Algorithm (spec §5.4)

Tag suggestion with existing taxonomy alignment:
1. **Exact Match:** Prefer tags that exactly match existing workspace tags
2. **Fuzzy Match:** Levenshtein distance > 0.8 similarity threshold
3. **Novel Tags:** Add new tags (lowercase-hyphenated) if room remains

### MediatR Events

Lifecycle events for observability:
- `MetadataExtractionStartedEvent(CharacterCount, DocumentPath?, Timestamp)` — Published before LLM invocation
- `MetadataExtractionCompletedEvent(DocumentType, KeyTermCount, ComplexityScore, Duration, Timestamp)` — Published on success
- `MetadataExtractionFailedEvent(ErrorMessage, DocumentPath?, Timestamp)` — Published on failure

### Prompt Template

Added `metadata-extractor.yaml` with Mustache syntax:
- **Template ID:** `metadata-extractor`
- **Required Variables:** `document_content`, `max_key_terms`, `max_concepts`, `max_tags`
- **Optional Variables:** `extract_named_entities`, `infer_audience`, `existing_tags`
- **Output Format:** JSON with key_terms, concepts, suggested_tags, document_type, target_audience, named_entities

### DI Registration

Added `MetadataExtractionServiceCollectionExtensions` with `AddMetadataExtractionPipeline()`:
```csharp
services.AddSingleton<IMetadataExtractor, MetadataExtractor>();
services.AddSingleton<IAgent>(sp => (IAgent)sp.GetRequiredService<IMetadataExtractor>());
```

Updated `AgentsModule.RegisterServices()` with `services.AddMetadataExtractionPipeline()` call. Initialization verification confirms `IMetadataExtractor` service availability and logs agent details.

---

## Files Created

### Abstractions (5 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Abstractions/Agents/MetadataExtraction/DocumentType.cs` | Enum | 15 document classification types |
| `src/Lexichord.Abstractions/Agents/MetadataExtraction/KeyTerm.cs` | Record | Term with importance scoring |
| `src/Lexichord.Abstractions/Agents/MetadataExtraction/MetadataExtractionOptions.cs` | Record | Configuration with validation |
| `src/Lexichord.Abstractions/Agents/MetadataExtraction/DocumentMetadata.cs` | Record | Result with metrics and Failed factory |
| `src/Lexichord.Abstractions/Agents/MetadataExtraction/IMetadataExtractor.cs` | Interface | Extraction contract extending IAgent |

### Implementation (2 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Modules.Agents/MetadataExtraction/MetadataExtractor.cs` | Class | Core agent implementation |
| `src/Lexichord.Modules.Agents/Extensions/MetadataExtractionServiceCollectionExtensions.cs` | Class | DI registration extension |

### Events (3 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Modules.Agents/MetadataExtraction/Events/MetadataExtractionStartedEvent.cs` | Record | Lifecycle start event |
| `src/Lexichord.Modules.Agents/MetadataExtraction/Events/MetadataExtractionCompletedEvent.cs` | Record | Lifecycle completion event |
| `src/Lexichord.Modules.Agents/MetadataExtraction/Events/MetadataExtractionFailedEvent.cs` | Record | Lifecycle failure event |

### Resources (1 file)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Modules.Agents/Resources/Prompts/metadata-extractor.yaml` | YAML | Prompt template |

### Tests (5 files)

| File | Tests | Description |
|:-----|:-----:|:------------|
| `tests/Lexichord.Tests.Unit/Modules/Agents/MetadataExtraction/MetadataExtractionOptionsTests.cs` | 36 | Validation, defaults, ranges |
| `tests/Lexichord.Tests.Unit/Modules/Agents/MetadataExtraction/KeyTermTests.cs` | 24 | Factory methods, properties, equality |
| `tests/Lexichord.Tests.Unit/Modules/Agents/MetadataExtraction/DocumentMetadataTests.cs` | 23 | Failed factory, properties, timestamps |
| `tests/Lexichord.Tests.Unit/Modules/Agents/MetadataExtraction/ReadingTimeTests.cs` | 16 | Calculations, adjustments, clamping |
| `tests/Lexichord.Tests.Unit/Modules/Agents/MetadataExtraction/DocumentTypeTests.cs` | 22 | Enum values, parsing, defaults |

---

## Files Modified

| File | Changes |
|:-----|:--------|
| `src/Lexichord.Abstractions/Constants/FeatureCodes.cs` | Added `MetadataExtraction = "Feature.MetadataExtraction"` constant in new `#region Summarizer Agent Features (v0.7.6)` |
| `src/Lexichord.Modules.Agents/AgentsModule.cs` | Added `services.AddMetadataExtractionPipeline()` call, initialization verification for IMetadataExtractor |

---

## Testing

### Test Summary

| Test File | Tests | Key Coverage |
|:----------|:-----:|:-------------|
| MetadataExtractionOptionsTests | 36 | Default values, MaxKeyTerms/MaxConcepts/MaxTags validation, MinimumTermImportance validation, WordsPerMinute validation |
| KeyTermTests | 24 | Create factory, CreateSimple factory, property defaults, technical flags |
| DocumentMetadataTests | 23 | Failed factory, CreateMinimal factory, timestamp accuracy, empty collections |
| ReadingTimeTests | 16 | Basic calculation, WPM clamping, code block adjustment, table adjustment, image adjustment, complexity multipliers |
| DocumentTypeTests | 22 | Enum values, numeric values, default value, string parsing, case-insensitive parsing |
| **Total v0.7.6b** | **121** | All v0.7.6b functionality |

### Test Groups

| Group | Tests | Description |
|:------|:------|:------------|
| MetadataExtractionOptions Defaults | 1 | All default values correct |
| MetadataExtractionOptions MaxKeyTerms | 8 | In-range and out-of-range validation (0, -1, 51, 100) |
| MetadataExtractionOptions MaxConcepts | 8 | In-range and out-of-range validation (0, -1, 21, 100) |
| MetadataExtractionOptions MaxTags | 8 | In-range and out-of-range validation (0, -1, 21, 100) |
| MetadataExtractionOptions MinImportance | 5 | In-range (0, 0.3, 0.5, 1) and out-of-range (-0.1, 1.1) |
| MetadataExtractionOptions WordsPerMinute | 8 | In-range and out-of-range validation (99, 401, 1000) |
| KeyTerm Create Factory | 6 | Standard creation, importance, frequency, technical flag |
| KeyTerm CreateSimple Factory | 4 | Simple creation, defaults |
| KeyTerm Properties | 8 | All properties, nullable values |
| KeyTerm Equality | 6 | Value equality, hash codes |
| DocumentMetadata Failed | 5 | Error message, word count, timestamps, null validation |
| DocumentMetadata CreateMinimal | 4 | Minimal success, required fields |
| DocumentMetadata Properties | 10 | All properties, collections, defaults |
| DocumentMetadata Timestamps | 4 | UTC timestamps, accuracy |
| ReadingTime Basic | 4 | Empty, null, whitespace, short content |
| ReadingTime Calculation | 4 | 200 words, 400 words, rounding up |
| ReadingTime WPM | 4 | Slower, faster, clamping below, clamping above |
| ReadingTime Adjustments | 4 | Code blocks, multiple code blocks, inline code, tables, images |
| DocumentType Values | 2 | Expected count, numeric values |
| DocumentType Parsing | 4 | String parsing, case-insensitive, invalid strings |
| DocumentType Defaults | 1 | Default is Unknown |

### Test Traits

All tests use: `[Trait("Category", "Unit")]`, `[Trait("SubPart", "v0.7.6b")]`

---

## Design Decisions

1. **No BaseAgent Class** — `MetadataExtractor` implements `IAgent` directly following the `SummarizerAgent`, `EditorAgent`, and `SimplifierAgent` pattern. The spec referenced a `BaseAgent` class that doesn't exist in the codebase.

2. **IFileService over IDocumentService** — Spec references `IDocumentService` which doesn't exist. Uses `IFileService.Exists()` and `IFileService.LoadAsync()` to read documents.

3. **Singleton Lifetime** — `MetadataExtractor` is registered as a singleton because it is stateless — all state is passed via request/result objects.

4. **Dual Interface Registration** — `MetadataExtractor` is registered both as `IMetadataExtractor` (for typed consumers) and `IAgent` (for registry discovery) via forwarding registration.

5. **Algorithmic Reading Time** — `CalculateReadingTime()` is implemented algorithmically without LLM calls, enabling fast repeated calculations. Uses regex patterns for code block, table, and image detection.

6. **Levenshtein Distance for Tag Matching** — Custom implementation of Levenshtein distance algorithm for fuzzy tag matching with 0.8 similarity threshold.

7. **JSON Response Parsing** — Uses System.Text.Json with case-insensitive property matching and trailing comma tolerance. Malformed responses trigger fallback extraction with regex patterns.

8. **3-Catch Error Pattern** — User cancellation → timeout → generic exception, following the SummarizerAgent pattern for consistent error handling.

9. **Safe Event Publishing** — `PublishFailedEventSafe()` helper method catches exceptions during event publishing in catch blocks to prevent masking original errors.

10. **StructureAnalysis Capability** — Adds `AgentCapabilities.StructureAnalysis` flag in addition to Chat, DocumentContext, and Summarization to reflect document type classification capability.

11. **Technical Term Detection** — Maintains a HashSet of 48 common technical indicators (api, sdk, async, class, function, etc.) for technical density calculation.

---

## Dependencies

### Consumed (from existing modules)

| Interface | Version | Used By |
|:----------|:--------|:--------|
| `IChatCompletionService` | v0.6.1a | MetadataExtractor (LLM invocation) |
| `IPromptRenderer` | v0.6.3b | MetadataExtractor (template rendering) |
| `IPromptTemplateRepository` | v0.6.3c | MetadataExtractor (template lookup) |
| `IFileService` | v0.1.4b | MetadataExtractor (document loading) |
| `UsageMetrics` | v0.6.1a | DocumentMetadata (token tracking) |
| `ILicenseContext` | v0.0.4c | License gating via attribute |
| `IMediator` | v0.0.7a | Event publishing |
| `AgentCapabilities` | v0.6.6a | Agent capability flags |
| `ChatOptions` | v0.6.1a | LLM request configuration |
| `ChatRequest` | v0.6.1a | LLM request assembly |

### Produced (new classes)

| Class | Consumers |
|:------|:----------|
| `IMetadataExtractor` | Command handlers needing typed access |
| `DocumentType` | Options, results, events |
| `KeyTerm` | DocumentMetadata, result processing |
| `MetadataExtractionOptions` | Pipeline consumers |
| `DocumentMetadata` | UI components, result handling |
| `MetadataExtractor` | Agent registry, direct invocation |
| `MetadataExtractionStartedEvent` | UI components, analytics |
| `MetadataExtractionCompletedEvent` | UI components, analytics |
| `MetadataExtractionFailedEvent` | Error tracking, analytics |

### No New NuGet Packages

All dependencies are existing project references.

---

## Spec Adaptations

| Spec Reference | Actual Codebase | Adaptation |
|:---|:---|:---|
| `BaseAgent` parent class | Does not exist | Implements `IAgent` directly (SummarizerAgent pattern) |
| `IDocumentService` | Does not exist | Uses `IFileService.LoadAsync()` for document loading |
| `IPromptRenderer.RenderAsync()` | `RenderMessages()` method | Uses RenderMessages + ChatRequest pattern |
| `AgentCapabilities.Analysis` | Does not exist | Uses `StructureAnalysis` capability flag |
| `Lexichord.Modules.Agents.Abstractions` namespace | `Lexichord.Abstractions.Agents.MetadataExtraction` | Follows existing project namespace conventions |
| Events in Abstractions | Events in `Modules.Agents.MetadataExtraction.Events` | Follows SummarizerAgent event location pattern |

---

## Build & Test Results

```
Build:     Succeeded (0 errors, 1 warning - Avalonia deprecation)
v0.7.6b:   121 passed, 0 failed
```

---

## License Gating

| Feature | Core | Writer | WriterPro | Teams |
|:--------|:-----|:-------|:----------|:------|
| MetadataExtractor access | - | - | ✓ | ✓ |
| ExtractAsync | - | - | ✓ | ✓ |
| ExtractFromContentAsync | - | - | ✓ | ✓ |
| SuggestTagsAsync | - | - | ✓ | ✓ |
| CalculateReadingTime | - | - | ✓ | ✓ |
| ExtractKeyTermsAsync | - | - | ✓ | ✓ |
| GetDefaultOptions | - | - | ✓ | ✓ |

---

## Prompt Template Details

The `metadata-extractor.yaml` template includes:

### System Prompt Structure
1. Role definition as expert metadata extraction specialist
2. Core extraction guidelines (accuracy, relevance, taxonomy alignment)
3. Key term extraction with importance scoring instructions
4. Concept identification guidelines
5. Tag suggestion rules with existing taxonomy preference
6. Document type classification criteria

### JSON Output Schema
```json
{
  "title": "Inferred document title",
  "description": "Brief document summary",
  "key_terms": [
    {
      "term": "string",
      "importance": 0.0-1.0,
      "frequency": number,
      "is_technical": boolean,
      "definition": "optional",
      "category": "optional"
    }
  ],
  "concepts": ["string"],
  "suggested_tags": ["string"],
  "document_type": "enum value",
  "target_audience": "string",
  "named_entities": {
    "people": ["string"],
    "organizations": ["string"],
    "products": ["string"]
  }
}
```

### Variable Binding
- `document_content` — Full document text
- `max_key_terms` — Maximum terms to extract
- `max_concepts` — Maximum concepts to identify
- `max_tags` — Maximum tags to suggest
- `extract_named_entities` — Whether to extract NER
- `infer_audience` — Whether to infer audience
- `existing_tags` — Workspace tags for matching (comma-separated)
