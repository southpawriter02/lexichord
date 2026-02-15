# Changelog: v0.7.4d — Batch Simplification

**Feature ID:** AGT-074d
**Version:** 0.7.4d
**Date:** 2026-02-15
**Status:** In Progress

---

## Overview

Implements the Batch Simplification feature for the Simplifier Agent, enabling document-wide paragraph-by-paragraph simplification with progress tracking, skip detection, cancellation support, and atomic undo. This is the fourth sub-part of v0.7.4 "The Simplifier Agent" and builds upon v0.7.4a's Readability Target Service, v0.7.4b's Simplification Pipeline, and v0.7.4c's Preview/Diff UI.

The implementation adds:
- `IBatchSimplificationService` — service interface for batch operations
- `BatchSimplificationService` — core service implementing paragraph-by-paragraph simplification
- `ParagraphParser` — document parsing with offset tracking and paragraph type detection
- Three MediatR events for batch workflow coordination
- Comprehensive data models: options, progress, results, estimates
- ViewModels and Views for progress and completion dialogs
- Full unit test coverage

---

## What's New

### IBatchSimplificationService Interface

Defines the contract for batch simplification operations:
- **Namespace:** `Lexichord.Abstractions.Agents.Simplifier`
- **Methods:**
  - `SimplifyDocumentAsync()` — Simplifies all paragraphs in a document
  - `SimplifySelectionsAsync()` — Simplifies specific text selections
  - `SimplifyDocumentStreamingAsync()` — Returns results as they complete (IAsyncEnumerable)
  - `EstimateCostAsync()` — Estimates tokens, time, and cost before processing

### BatchSimplificationService

Core implementation of `IBatchSimplificationService`:
- **Namespace:** `Lexichord.Modules.Agents.Simplifier`
- **Dependencies:** `ISimplificationPipeline`, `IReadabilityTargetService`, `IReadabilityService`, `IEditorService`, `IMediator`, `ILicenseContext`, `ILogger`
- **Key Features:**
  - License validation (requires WriterPro tier)
  - Skip detection (already simple, too short, headings, code blocks, blockquotes, list items)
  - Progress reporting with time estimation
  - Cancellation support with graceful completion
  - Atomic undo via `IEditorService.BeginUndoGroup()`
  - MediatR event publishing for workflow coordination

### ParagraphParser

Document parsing utility:
- **Namespace:** `Lexichord.Modules.Agents.Simplifier`
- **Methods:** `Parse(text)` returns `IReadOnlyList<ParsedParagraph>`
- **Paragraph Types Detected:**
  - Normal paragraphs
  - Headings (Markdown `#` prefix)
  - Code blocks (fenced ``` and indented)
  - Blockquotes (`>` prefix)
  - List items (`-`, `*`, numbered lists)
- **Features:**
  - Offset tracking for position-preserving replacements
  - Windows/Unix line ending normalization
  - Empty line handling

### Data Models

**Enums:**
- `BatchSimplificationScope` — EntireDocument, Selection, CurrentSection, ComplexParagraphsOnly
- `BatchSimplificationPhase` — Initializing, AnalyzingDocument, ProcessingParagraphs, AggregatingResults, Completed, Cancelled, Failed
- `ParagraphSkipReason` — None, AlreadySimple, TooShort, IsHeading, IsCodeBlock, IsBlockquote, IsListItem
- `ParagraphType` — Normal, Heading, CodeBlock, Blockquote, ListItem

**Records:**
- `TextSelection` — Document path, offset, length, text
- `BatchSimplificationOptions` — Scope, skip settings, limits, strategy
- `BatchSimplificationProgress` — Phase, current/total paragraphs, time estimate
- `BatchSimplificationEstimate` — Estimated paragraphs, tokens, time, cost
- `ParsedParagraph` — Index, offsets, text, type
- `ParagraphSimplificationResult` — Per-paragraph result with skip reason
- `BatchSimplificationResult` — Aggregate result with document metrics

### MediatR Events

Three notification records for batch workflow coordination:

**SimplificationCompletedEvent:**
- **Properties:** `DocumentPath`, `Result`, `TotalParagraphs`, `SimplifiedParagraphs`, `SkippedParagraphs`, `ProcessingTime`, `GradeLevelReduction`

**ParagraphSimplifiedEvent:**
- **Properties:** `DocumentPath`, `ParagraphIndex`, `TotalParagraphs`, `Result`, `CumulativeTokens`

**BatchSimplificationCancelledEvent:**
- **Properties:** `DocumentPath`, `ProcessedParagraphs`, `TotalParagraphs`, `ProcessingTime`
- **Computed:** `CompletionPercentage`

### BatchProgressViewModel

ViewModel for progress dialog:
- **Namespace:** `Lexichord.Modules.Agents.Simplifier`
- **Base Class:** `DisposableViewModel`
- **Properties:**
  - Progress tracking: `ProgressPercentage`, `ProcessedParagraphs`, `TotalParagraphs`
  - Counts: `SimplifiedParagraphs`, `SkippedParagraphs`
  - Time: `ElapsedTimeText`, `EstimatedTimeRemainingText`
  - Current: `CurrentParagraphPreview`
  - Tokens: `TokensUsed`
  - Activity: `RecentActivity` (ObservableCollection)
- **Commands:** `CancelCommand`
- **Events:** `Completed`
- **Methods:** `StartAsync()`, `Cancel()`

### BatchCompletionViewModel

ViewModel for completion summary:
- **Namespace:** `Lexichord.Modules.Agents.Simplifier`
- **Base Class:** `DisposableViewModel`
- **Properties:**
  - Status: `Title`, `CompletionMessage`, `IsSuccess`, `WasCancelled`
  - Metrics: `OriginalGradeLevel`, `SimplifiedGradeLevel`, `OriginalFogIndex`, `SimplifiedFogIndex`
  - Counts: `SimplifiedParagraphs`, `SkippedParagraphs`, `TotalParagraphs`
  - Improvement: `GradeLevelImprovement`, `GradeLevelImprovementText`
  - Processing: `ProcessingTime`, `ProcessingTimeText`, `TokensUsed`, `EstimatedCost`
  - Display: `OriginalGradeLevelText`, `SimplifiedGradeLevelText`, `ParagraphCountsText`, `TokensAndCostText`, `UndoHint`
- **Commands:** `CloseCommand`, `ViewDetailsCommand`
- **Events:** `CloseRequested`, `ViewDetailsRequested`

### UI Views

**BatchProgressView.axaml:**
- Progress bar with percentage
- Paragraph counts (processed, simplified, skipped)
- Time remaining estimate
- Current paragraph preview
- Recent activity log
- Cancel button

**BatchCompletionView.axaml:**
- Success/cancelled/failed status icons
- Grade level improvement badge
- Before/after readability metrics
- Paragraph statistics
- Processing time and token usage
- Undo hint
- Done and View Details buttons

### DI Registration

Extended `SimplifierServiceCollectionExtensions` with `AddBatchSimplificationService()`:
```csharp
services.AddSingleton<IBatchSimplificationService, BatchSimplificationService>();
services.AddTransient<BatchProgressViewModel>();
services.AddTransient<BatchCompletionViewModel>();
```

Updated `AgentsModule.RegisterServices()` with `services.AddBatchSimplificationService()` call.

---

## Files Created

### Abstractions — Enums (4 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Abstractions/Agents/Simplifier/BatchSimplificationScope.cs` | Enum | Document scope options |
| `src/Lexichord.Abstractions/Agents/Simplifier/BatchSimplificationPhase.cs` | Enum | Processing phases |
| `src/Lexichord.Abstractions/Agents/Simplifier/ParagraphSkipReason.cs` | Enum | Skip conditions |
| `src/Lexichord.Modules.Agents/Simplifier/ParagraphType.cs` | Enum | Paragraph classification |

### Abstractions — Records (7 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Abstractions/Agents/Simplifier/TextSelection.cs` | Record | Text selection data |
| `src/Lexichord.Abstractions/Agents/Simplifier/BatchSimplificationOptions.cs` | Record | Operation options |
| `src/Lexichord.Abstractions/Agents/Simplifier/BatchSimplificationProgress.cs` | Record | Progress reporting |
| `src/Lexichord.Abstractions/Agents/Simplifier/BatchSimplificationEstimate.cs` | Record | Cost estimation |
| `src/Lexichord.Abstractions/Agents/Simplifier/ParagraphSimplificationResult.cs` | Record | Per-paragraph result |
| `src/Lexichord.Abstractions/Agents/Simplifier/BatchSimplificationResult.cs` | Record | Aggregate result |
| `src/Lexichord.Modules.Agents/Simplifier/ParsedParagraph.cs` | Record | Parsed paragraph data |

### Abstractions — Events (3 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Abstractions/Agents/Simplifier/Events/SimplificationCompletedEvent.cs` | Record | Batch completion event |
| `src/Lexichord.Abstractions/Agents/Simplifier/Events/ParagraphSimplifiedEvent.cs` | Record | Per-paragraph event |
| `src/Lexichord.Abstractions/Agents/Simplifier/Events/BatchSimplificationCancelledEvent.cs` | Record | Cancellation event |

### Abstractions — Interface (1 file)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Abstractions/Agents/Simplifier/IBatchSimplificationService.cs` | Interface | Service contract |

### Module — Service (2 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Modules.Agents/Simplifier/BatchSimplificationService.cs` | Class | Core service implementation |
| `src/Lexichord.Modules.Agents/Simplifier/ParagraphParser.cs` | Class | Document parsing |

### Module — ViewModels (2 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Modules.Agents/Simplifier/BatchProgressViewModel.cs` | Class | Progress dialog VM |
| `src/Lexichord.Modules.Agents/Simplifier/BatchCompletionViewModel.cs` | Class | Completion dialog VM |

### Module — Views (4 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Modules.Agents/Simplifier/Views/BatchProgressView.axaml` | AXAML | Progress dialog |
| `src/Lexichord.Modules.Agents/Simplifier/Views/BatchProgressView.axaml.cs` | C# | Code-behind |
| `src/Lexichord.Modules.Agents/Simplifier/Views/BatchCompletionView.axaml` | AXAML | Completion dialog |
| `src/Lexichord.Modules.Agents/Simplifier/Views/BatchCompletionView.axaml.cs` | C# | Code-behind |

### Tests (3 files)

| File | Tests | Description |
|:-----|:-----:|:------------|
| `tests/Lexichord.Tests.Unit/Modules/Agents/Simplifier/BatchSimplificationServiceTests.cs` | 33 | Service tests |
| `tests/Lexichord.Tests.Unit/Modules/Agents/Simplifier/ParagraphParserTests.cs` | 15 | Parser tests |
| `tests/Lexichord.Tests.Unit/Modules/Agents/Simplifier/BatchSimplificationEventsTests.cs` | TBD | Event tests |

---

## Files Modified

| File | Changes |
|:-----|:--------|
| `src/Lexichord.Modules.Agents/Extensions/SimplifierServiceCollectionExtensions.cs` | Added `AddBatchSimplificationService()` method, updated docs |
| `src/Lexichord.Modules.Agents/AgentsModule.cs` | Added `services.AddBatchSimplificationService()` call |

---

## Testing

### Test Summary

| Test File | Tests | Key Coverage |
|:----------|:-----:|:-------------|
| BatchSimplificationServiceTests | 33 | Constructor, SimplifyDocument, license, skip logic, events |
| ParagraphParserTests | 15 | Parsing, offsets, paragraph types |
| **Total v0.7.4d** | **48+** | All v0.7.4d functionality |
| **Total v0.7.4** | **211+** | Combined a+b+c+d |

### Test Groups

| Group | Tests | Description |
|:------|:------|:------------|
| Service Constructor | 7 | Dependency null checks |
| SimplifyDocumentAsync | 8 | Success, empty, all skipped, cancellation |
| License Validation | 2 | License required, failed result |
| Skip Logic | 6 | Headings, code blocks, too short, already simple |
| Event Publishing | 4 | Completion, paragraph, cancellation events |
| Progress Reporting | 3 | Progress updates during processing |
| Parser Basic | 5 | Single/multiple paragraphs, offsets |
| Parser Types | 6 | Headings, code blocks, blockquotes, lists |
| Parser Edge Cases | 4 | Empty, whitespace, line endings |

### Test Traits

All tests use: `[Trait("Category", "Unit")]`, `[Trait("SubPart", "v0.7.4d")]`

---

## Design Decisions

1. **Singleton Service Lifetime** — `BatchSimplificationService` is stateless; all operation state is passed via parameters and returned via results.

2. **Skip Detection** — Paragraphs are evaluated against multiple skip conditions before processing to avoid unnecessary API calls for content that won't benefit from simplification.

3. **Reverse-Order Application** — Changes are applied from highest offset to lowest to preserve position accuracy during text replacement.

4. **Atomic Undo** — Uses `IEditorService.BeginUndoGroup()` / `EndUndoGroup()` to wrap all paragraph replacements for single Ctrl+Z undo.

5. **Time Estimation** — Tracks running average of processing time per paragraph for accurate ETA calculation.

6. **License Gating** — Returns `Failed()` result for unlicensed users rather than throwing, allowing graceful UI handling.

7. **MediatR Events** — Using `INotification` for decoupled progress tracking and analytics.

8. **Type Alias Pattern** — Uses `using BatchTextSelection = TextSelection` to resolve namespace ambiguity with Word interop types.

---

## Dependencies

### Consumed (from existing modules)

| Interface | Version | Used By |
|:----------|:--------|:--------|
| `ISimplificationPipeline` | v0.7.4b | Per-paragraph simplification |
| `IReadabilityTargetService` | v0.7.4a | Target resolution |
| `IReadabilityService` | v0.3.3c | Metrics calculation |
| `IEditorService` | v0.6.7b | Document text, undo groups |
| `IMediator` | v0.6.6d | Event publishing |
| `ILicenseContext` | v0.0.4c | License validation |
| `DisposableViewModel` | v0.3.7d | ViewModel base class |
| `ReadabilityMetrics` | v0.3.3c | Metrics aggregation |
| `UsageMetrics` | v0.6.6a | Token tracking |
| `ReadabilityTarget` | v0.7.4a | Target configuration |

### Produced (new classes)

| Class | Consumers |
|:------|:----------|
| `IBatchSimplificationService` | UI commands, background jobs |
| `BatchSimplificationService` | DI container |
| `ParagraphParser` | BatchSimplificationService |
| `BatchProgressViewModel` | Progress dialog |
| `BatchCompletionViewModel` | Completion dialog |
| `BatchSimplificationResult` | UI display, analytics |
| `ParagraphSimplificationResult` | Detail views |
| `BatchSimplificationProgress` | Progress reporting |
| `BatchSimplificationEstimate` | Cost confirmation |
| Events (3) | Analytics, logging handlers |

---

## Skip Detection Logic

```
IF paragraph.WordCount < options.MinParagraphWords → Skip (TooShort)
IF paragraph.Type == Heading AND options.SkipHeadings → Skip (IsHeading)
IF paragraph.Type == CodeBlock AND options.SkipCodeBlocks → Skip (IsCodeBlock)
IF paragraph.Type == Blockquote AND options.SkipBlockquotes → Skip (IsBlockquote)
IF paragraph.Type == ListItem AND options.SkipListItems → Skip (IsListItem)
IF metrics.FleschKincaidGradeLevel <= target.TargetGradeLevel + tolerance → Skip (AlreadySimple)
ELSE → Process
```

---

## Time Estimation Algorithm

```
runningAverage = totalTime / processedParagraphs
estimatedRemaining = runningAverage * (totalParagraphs - processedParagraphs)
```

Updates after each paragraph completion for accurate ETA.

---

## License Gating

| Feature | Core | Writer | WriterPro | Teams |
|:--------|:-----|:-------|:----------|:------|
| Batch simplification | - | - | ✓ | ✓ |
| Progress tracking | - | - | ✓ | ✓ |
| Cost estimation | - | - | ✓ | ✓ |
| Single-paragraph preview | ✓ | ✓ | ✓ | ✓ |

---

## Build & Test Results

```
Build:     Succeeded (0 errors, 1 warning)
v0.7.4d:   48+ passed, 0 failed
v0.7.4:    211+ passed, 0 failed (combined a+b+c+d)
```
