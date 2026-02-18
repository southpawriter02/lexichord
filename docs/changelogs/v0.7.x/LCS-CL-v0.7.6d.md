# Changelog: v0.7.6d — Document Comparison

**Feature ID:** AGT-076d
**Version:** 0.7.6d
**Date:** 2026-02-18
**Status:** ✅ Complete

---

## Overview

Implements Document Comparison for the Summarizer Agent feature set, enabling semantic analysis of changes between two document versions using a hybrid DiffPlex + LLM approach. The system categorizes changes by type (Added, Removed, Modified, Restructured, Clarified, Formatting, Correction, Terminology) and scores their significance (Critical, High, Medium, Low). This is the fourth and final sub-part of v0.7.6 "The Summarizer Agent."

The implementation adds `IDocumentComparer` interface with methods for file-based comparison (`CompareAsync()`), content-based comparison (`CompareContentAsync()`), git version comparison (`CompareWithGitVersionAsync()`), and pure text diff (`GetTextDiff()`); `ChangeCategory` enum with 8 change types; `ChangeSignificance` enum with 4 significance levels and `FromScore()` extension method; `LineRange` record for tracking affected line ranges; `DocumentChange` record with category, section, description, significance score, and optional diff text; `ComparisonOptions` record for filtering, grouping, and output configuration; `ComparisonResult` record with summary, changes, magnitude, and computed statistics; `DocumentComparer` implementation integrating `IChatCompletionService`, `IFileService`, and DiffPlex for hybrid analysis; `ComparisonViewModel` and `ChangeCardViewModel` for interactive UI; `ComparisonView` and `ChangeCard` Avalonia views; MediatR events for started/completed/failed lifecycle; and 164 unit tests with 100% pass rate.

---

## What's New

### ChangeCategory Enum

Change classification types:
- **Namespace:** `Lexichord.Abstractions.Agents.DocumentComparison`
- **Values:**
  - `Added` (0) — New content that didn't exist before
  - `Removed` (1) — Content that was deleted
  - `Modified` (2) — Content that was changed but retains same purpose
  - `Restructured` (3) — Content moved or reorganized
  - `Clarified` (4) — Content rewritten for better clarity
  - `Formatting` (5) — Only formatting/presentation changed
  - `Correction` (6) — Error or mistake fixed
  - `Terminology` (7) — Terminology or naming changed

### ChangeSignificance Enum

Significance levels with score ranges:
- **Namespace:** `Lexichord.Abstractions.Agents.DocumentComparison`
- **Values:**
  - `Low` (0) — Score range 0.0-0.3, minor changes
  - `Medium` (1) — Score range 0.3-0.6, moderate changes
  - `High` (2) — Score range 0.6-0.8, significant changes
  - `Critical` (3) — Score range 0.8-1.0, critical changes
- **Extension Methods:**
  - `FromScore(double)` — Maps score to significance level
  - `GetMinimumScore()` — Returns lower bound of score range
  - `GetDisplayLabel()` — Returns human-readable label

### LineRange Record

Line number range tracking:
- **Namespace:** `Lexichord.Abstractions.Agents.DocumentComparison`
- **Properties:**
  - `Start` — First line number (1-based)
  - `End` — Last line number (1-based)
- **Computed Properties:**
  - `LineCount` — Number of lines in range (End - Start + 1)
  - `IsSingleLine` — Whether Start equals End
  - `IsValid` — Whether range is valid (Start > 0 and End >= Start)
- **Methods:**
  - `Validate()` — Throws `ArgumentException` if invalid
  - `Contains(int)` — Checks if line number is within range
  - `Overlaps(LineRange)` — Checks if ranges share lines
  - `ToString()` — Returns "line N" or "lines N-M"

### DocumentChange Record

Individual change record:
- **Namespace:** `Lexichord.Abstractions.Agents.DocumentComparison`
- **Properties:**
  - `Category` — Change type (ChangeCategory)
  - `Section` — Document section affected (nullable)
  - `Description` — Human-readable change description
  - `Significance` — Score from 0.0 to 1.0
  - `OriginalText` — Text before change (nullable)
  - `NewText` — Text after change (nullable)
  - `Impact` — Description of change impact (nullable)
  - `OriginalLineRange` — Lines in original (nullable)
  - `NewLineRange` — Lines in new (nullable)
  - `RelatedChangeIndices` — Indices of related changes
- **Computed Properties:**
  - `SignificanceLevel` — `ChangeSignificance.FromScore(Significance)`
  - `HasDiff` — Whether OriginalText or NewText is present
- **Methods:**
  - `Validate()` — Validates significance range and line ranges

### ComparisonOptions Record

Comparison configuration:
- **Namespace:** `Lexichord.Abstractions.Agents.DocumentComparison`
- **Properties:**
  - `SignificanceThreshold` — Minimum score to include (default 0.2)
  - `IncludeFormattingChanges` — Include formatting changes (default false)
  - `GroupBySection` — Group output by section (default true)
  - `MaxChanges` — Maximum changes to return (default 20)
  - `FocusSections` — Sections to focus on (nullable)
  - `IncludeTextDiff` — Include raw text diff (default false)
  - `IdentifyRelatedChanges` — Link related changes (default true)
  - `OriginalVersionLabel` — Label for original (nullable)
  - `NewVersionLabel` — Label for new version (nullable)
  - `MaxResponseTokens` — Max LLM tokens (default 4096)
- **Methods:**
  - `Validate()` — Validates all option ranges
  - `WithLabels(original, new)` — Creates copy with labels
- **Static Properties:**
  - `Default` — Shared default options instance

### ComparisonResult Record

Comparison output:
- **Namespace:** `Lexichord.Abstractions.Agents.DocumentComparison`
- **Properties:**
  - `Summary` — Overall summary of changes
  - `Changes` — List of DocumentChange items
  - `ChangeMagnitude` — Overall change magnitude (0.0-1.0)
  - `AffectedSections` — Sections with changes
  - `Usage` — Token usage metrics
  - `ComparedAt` — Comparison timestamp
  - `OriginalPath` — Original document path (nullable)
  - `NewPath` — New document path (nullable)
  - `TextDiff` — Raw text diff if requested (nullable)
- **Computed Properties:**
  - `AreIdentical` — No changes detected
  - `ChangeCount` — Number of changes
  - `AdditionCount` — Count of Added changes
  - `DeletionCount` — Count of Removed changes
  - `ModificationCount` — Count of Modified changes
  - `CriticalCount`, `HighCount`, `MediumCount`, `LowCount`
- **Factory Methods:**
  - `Identical(summary?)` — Creates no-changes result
  - `Failed(errorMessage)` — Creates error result

### IDocumentComparer Interface

Main service contract:
- **Namespace:** `Lexichord.Abstractions.Agents.DocumentComparison`
- **Methods:**
  - `CompareAsync(originalPath, newPath, options?, ct)` — Compare two files
  - `CompareContentAsync(originalContent, newContent, options?, ct)` — Compare text content
  - `CompareWithGitVersionAsync(currentPath, gitRef, options?, ct)` — Compare with git revision
  - `GetTextDiff(originalContent, newContent)` — Pure text diff without LLM

### DocumentComparer Implementation

Core comparer implementing `IDocumentComparer`:
- **Namespace:** `Lexichord.Modules.Agents.DocumentComparison`
- **Dependencies:**
  - `IChatCompletionService` — LLM communication
  - `IPromptRenderer` — Prompt template rendering
  - `IPromptTemplateRepository` — Template storage
  - `IFileService` — File operations
  - `ILicenseContext` — License verification
  - `IMediator` — Event publishing
  - `ILogger` — Diagnostic logging
- **Features:**
  - Hybrid DiffPlex + LLM semantic analysis
  - 3-catch error pattern (user cancel, timeout, generic)
  - JSON response parsing with category/significance extraction
  - Significance filtering based on threshold
  - License gating for WriterPro tier
- **License Gating:** Requires WriterPro tier

### MediatR Events

Lifecycle events for observability:
- `DocumentComparisonStartedEvent(OriginalPath?, NewPath?, OriginalCharacterCount, NewCharacterCount, DateTime Timestamp)` — Published before comparison
- `DocumentComparisonCompletedEvent(OriginalPath?, NewPath?, ChangeCount, ChangeMagnitude, Duration, DateTime Timestamp)` — Published on success with `AreIdentical` and `MagnitudePercentage` computed properties
- `DocumentComparisonFailedEvent(OriginalPath?, NewPath?, ErrorMessage, DateTime Timestamp)` — Published on failure

### ComparisonViewModel

ViewModel for Comparison Panel UI:
- **Namespace:** `Lexichord.Modules.Agents.DocumentComparison.ViewModels`
- **Base Class:** `ObservableObject` (CommunityToolkit.Mvvm)
- **Lifetime:** Transient (per panel instance)
- **Observable Properties:**
  - `OriginalDocumentPath`, `NewDocumentPath`
  - `Result` (ComparisonResult)
  - `IsLoading`, `ErrorMessage`
  - `ChangeCards` (ObservableCollection<ChangeCardViewModel>)
  - `CriticalChanges`, `HighChanges`, `MediumChanges`, `LowChanges` (filtered lists)
- **Commands:**
  - `CompareCommand` — Executes comparison
  - `RefreshCommand` — Re-runs comparison
  - `CopyDiffCommand` — Copies diff to clipboard
  - `ExportReportCommand` — Exports comparison report
  - `CloseCommand` — Closes panel
- **Events:** `CloseRequested`

### ChangeCardViewModel

Wrapper for change display:
- **Namespace:** `Lexichord.Modules.Agents.DocumentComparison.ViewModels`
- **Properties:**
  - `Change` — Wrapped DocumentChange
  - `IsExpanded` — Expanded state
  - `OriginalVersionLabel`, `NewVersionLabel` — Version labels
- **Computed Properties:**
  - `CategoryIcon` — Icon for change category
  - `CategoryColor` — Color for category badge
  - `SignificanceColor` — Color for significance level
  - `ExpandCollapseIcon` — Chevron icon based on state
  - `HasDiffContent` — Whether diff can be shown
  - `ShowDiffArrow` — Whether to show expand arrow
- **Commands:**
  - `ToggleExpandCommand` — Toggles expanded state

### ComparisonView

Avalonia panel for comparison display:
- **Namespace:** `Lexichord.Modules.Agents.DocumentComparison.Views`
- **Layout:**
  - Header: Document paths, version labels, action buttons
  - Summary: Overall change summary with magnitude bar
  - Change Groups: Changes grouped by significance level
  - Footer: Status and timing information
- **Keyboard Shortcuts:**
  - `Escape` — Close panel
  - `Ctrl+R` — Refresh comparison
  - `Ctrl+C` — Copy diff to clipboard
  - `Ctrl+E` — Export report

### ChangeCard View

Card component for change display:
- **Namespace:** `Lexichord.Modules.Agents.DocumentComparison.Views`
- **Features:**
  - Category badge with icon and color
  - Significance indicator
  - Expandable diff view
  - Section and impact display
  - Keyboard accessibility (Enter/Space to toggle)

### DI Registration

Added `DocumentComparisonServiceCollectionExtensions` with `AddDocumentComparisonPipeline()`:
```csharp
services.AddSingleton<IDocumentComparer, DocumentComparer>();
services.AddTransient<ComparisonViewModel>();
services.AddTransient<ChangeCardViewModel>();
```

Updated `AgentsModule.RegisterServices()` with `services.AddDocumentComparisonPipeline()` call. Initialization verification confirms `IDocumentComparer` service availability.

---

## Files Created

### Abstractions (7 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Abstractions/Agents/DocumentComparison/ChangeCategory.cs` | Enum | 8 change type categories |
| `src/Lexichord.Abstractions/Agents/DocumentComparison/ChangeSignificance.cs` | Enum | 4 significance levels with extensions |
| `src/Lexichord.Abstractions/Agents/DocumentComparison/LineRange.cs` | Record | Line number range tracking |
| `src/Lexichord.Abstractions/Agents/DocumentComparison/DocumentChange.cs` | Record | Individual change data |
| `src/Lexichord.Abstractions/Agents/DocumentComparison/ComparisonOptions.cs` | Record | Comparison configuration |
| `src/Lexichord.Abstractions/Agents/DocumentComparison/ComparisonResult.cs` | Record | Comparison output |
| `src/Lexichord.Abstractions/Agents/DocumentComparison/IDocumentComparer.cs` | Interface | Main service contract |

### Implementation (12 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Modules.Agents/DocumentComparison/DocumentComparer.cs` | Class | Core IDocumentComparer implementation |
| `src/Lexichord.Modules.Agents/DocumentComparison/Events/DocumentComparisonStartedEvent.cs` | Record | Comparison started event |
| `src/Lexichord.Modules.Agents/DocumentComparison/Events/DocumentComparisonCompletedEvent.cs` | Record | Comparison completed event |
| `src/Lexichord.Modules.Agents/DocumentComparison/Events/DocumentComparisonFailedEvent.cs` | Record | Comparison failed event |
| `src/Lexichord.Modules.Agents/DocumentComparison/ViewModels/ComparisonViewModel.cs` | Class | Panel orchestration ViewModel |
| `src/Lexichord.Modules.Agents/DocumentComparison/ViewModels/ChangeCardViewModel.cs` | Class | Change card display wrapper |
| `src/Lexichord.Modules.Agents/DocumentComparison/Views/ComparisonView.axaml` | XAML | Panel UI layout |
| `src/Lexichord.Modules.Agents/DocumentComparison/Views/ComparisonView.axaml.cs` | Class | Panel code-behind |
| `src/Lexichord.Modules.Agents/DocumentComparison/Views/ChangeCard.axaml` | XAML | Change card component |
| `src/Lexichord.Modules.Agents/DocumentComparison/Views/ChangeCard.axaml.cs` | Class | Card code-behind |
| `src/Lexichord.Modules.Agents/Extensions/DocumentComparisonServiceCollectionExtensions.cs` | Class | DI registration extension |
| `src/Lexichord.Modules.Agents/Resources/Prompts/document-comparer.yaml` | YAML | Mustache prompt template |

### Tests (9 files)

| File | Test Count | Description |
|:-----|:----------:|:------------|
| `tests/Lexichord.Tests.Unit/Modules/Agents/DocumentComparison/ChangeSignificanceTests.cs` | 18 | FromScore mapping, GetMinimumScore, GetDisplayLabel |
| `tests/Lexichord.Tests.Unit/Modules/Agents/DocumentComparison/ChangeCategoryTests.cs` | 12 | Enum values and parsing |
| `tests/Lexichord.Tests.Unit/Modules/Agents/DocumentComparison/LineRangeTests.cs` | 28 | Construction, validation, Contains, Overlaps, ToString |
| `tests/Lexichord.Tests.Unit/Modules/Agents/DocumentComparison/ComparisonOptionsTests.cs` | 25 | Defaults, validation, WithLabels |
| `tests/Lexichord.Tests.Unit/Modules/Agents/DocumentComparison/DocumentComparerTests.cs` | 35 | ComparisonResult factories, computed properties |
| `tests/Lexichord.Tests.Unit/Modules/Agents/DocumentComparison/DocumentComparisonEventsTests.cs` | 46 | Started, Completed, Failed event tests |

---

## Files Modified

| File | Changes |
|:-----|:--------|
| `src/Lexichord.Abstractions/Constants/FeatureCodes.cs` | Added `DocumentComparison = "Feature.DocumentComparison"` constant |
| `src/Lexichord.Modules.Agents/AgentsModule.cs` | Added `services.AddDocumentComparisonPipeline()` and init verification |
| `docs/changelogs/CHANGELOG.md` | Added v0.7.6d entry |

---

## Technical Details

### Hybrid Comparison Approach

**DiffPlex Integration:**
- Uses `InlineDiffBuilder` for word-level text diff
- Generates unified diff format for raw text comparison
- Provides context for LLM semantic analysis

**LLM Semantic Analysis:**
- Receives text diff as input along with original and new content
- Classifies changes into 8 categories
- Scores significance on 0.0-1.0 scale
- Identifies related changes across document
- Generates human-readable change descriptions

### JSON Response Parsing

LLM response format:
```json
{
  "summary": "Overall change description",
  "change_magnitude": 0.45,
  "affected_sections": ["Introduction", "Methods"],
  "changes": [
    {
      "category": "Modified",
      "section": "Introduction",
      "description": "Updated project scope",
      "significance": 0.7,
      "original_text": "...",
      "new_text": "...",
      "impact": "Affects project timeline"
    }
  ]
}
```

### Significance Threshold Filtering

Changes are filtered based on `SignificanceThreshold`:
- Default 0.2 excludes trivial changes
- Set to 0.0 to include all changes
- Set to 0.6+ to show only High/Critical changes

### Git Integration

`CompareWithGitVersionAsync()` implementation:
1. Execute `git show {ref}:{path}` to get historical content
2. Load current file content via `IFileService`
3. Delegate to `CompareContentAsync()`

---

## License Gating

All v0.7.6d comparison features require **WriterPro** tier:
- Feature Code: `FeatureCodes.DocumentComparison`
- Lower tiers receive `ComparisonResult.Failed("Upgrade to WriterPro to use document comparison.")`

---

## Testing

### Test Categories

- **Unit Tests:** 164 tests across 9 test files
- **Traits:** `[Trait("Category", "Unit")]`, `[Trait("SubPart", "v0.7.6d")]`

### Test Coverage

| Component | Test File | Coverage |
|:----------|:----------|:---------|
| ChangeSignificance | ChangeSignificanceTests.cs | FromScore, GetMinimumScore, GetDisplayLabel |
| ChangeCategory | ChangeCategoryTests.cs | Enum values, parsing |
| LineRange | LineRangeTests.cs | Construction, validation, methods |
| ComparisonOptions | ComparisonOptionsTests.cs | Defaults, validation, factories |
| ComparisonResult | DocumentComparerTests.cs | Factories, computed properties |
| Events | DocumentComparisonEventsTests.cs | Started, Completed, Failed |

### Running Tests

```bash
# Run v0.7.6d tests only
dotnet test --filter "SubPart=v0.7.6d"

# Run all v0.7.6 tests
dotnet test --filter "SubPart~v0.7.6"
```

---

## Dependencies

### New Dependencies

None — uses existing project dependencies:
- `DiffPlex` for text diff generation (already in project from v0.7.4c)
- `CommunityToolkit.Mvvm` for ViewModels
- `System.Text.Json` for JSON parsing

### Existing Dependencies Used

- `MediatR` for event publishing
- `Avalonia` for UI components
- `YamlDotNet` for prompt templates

---

## Migration Notes

No breaking changes. New functionality is additive.

---

## See Also

- [v0.7.6a Changelog](LCS-CL-v0.7.6a.md) — Summarization Modes
- [v0.7.6b Changelog](LCS-CL-v0.7.6b.md) — Metadata Extraction
- [v0.7.6c Changelog](LCS-CL-v0.7.6c.md) — Export Formats
- [v0.7.6 Specification](../../specs/v0.7.x/v0.7.6/LCS-SBD-v0.7.6.md) — Full feature specification
