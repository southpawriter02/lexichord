# Changelog: v0.7.6c — Export Formats

**Feature ID:** AGT-076c
**Version:** 0.7.6c
**Date:** 2026-02-17
**Status:** ✅ Complete

---

## Overview

Implements Export Formats for the Summarizer Agent feature set, enabling multi-destination export of generated summaries and extracted metadata. Users can export summaries to five destinations: Summary Panel UI, YAML Frontmatter, standalone Markdown File, system Clipboard, and inline cursor position (InlineInsert). The feature includes intelligent frontmatter merging, summary caching with content-hash invalidation, and callout-formatted inline insertions. This is the third sub-part of v0.7.6 "The Summarizer Agent."

The implementation adds `ISummaryExporter` interface with methods for destination-based export, frontmatter updates, and summary caching; `ExportDestination` enum with 5 export targets; `FrontmatterFields` flags enum for selective field inclusion; `SummaryExportOptions` and `SummaryExportResult` records for input/output modeling; `CachedSummary` record for persistent summary storage; `SummaryExporter` implementation integrating with `IFileService`, `IEditorService`, `IClipboardService`, and `IMediator`; `SummaryCacheService` with hybrid IMemoryCache + JSON file persistence; `SummaryPanelViewModel` and `SummaryPanelView` for interactive summary display; MediatR events for started/completed/failed/panel-opened lifecycle; and 160+ unit tests with 100% pass rate.

---

## What's New

### ExportDestination Enum

Export target destinations:
- **Namespace:** `Lexichord.Abstractions.Agents.SummaryExport`
- **Values:**
  - `Panel` (0) — Display in Summary Panel UI
  - `Frontmatter` (1) — Inject into YAML frontmatter block
  - `File` (2) — Create standalone .summary.md file
  - `Clipboard` (3) — Copy to system clipboard
  - `InlineInsert` (4) — Insert at cursor position

### FrontmatterFields Flags Enum

Selective frontmatter field inclusion:
- **Namespace:** `Lexichord.Abstractions.Agents.SummaryExport`
- **Attribute:** `[Flags]`
- **Values:**
  - `None` (0) — No fields selected
  - `Abstract` (1) — Include summary abstract/text
  - `Tags` (2) — Include suggested tags
  - `KeyTerms` (4) — Include key terms with importance
  - `ReadingTime` (8) — Include estimated reading time
  - `Category` (16) — Include primary category
  - `Audience` (32) — Include target audience
  - `GeneratedAt` (64) — Include generation timestamp
  - `All` (127) — Include all fields (default)

### SummaryExportOptions Record

Configuration for export operations:
- **Namespace:** `Lexichord.Abstractions.Agents.SummaryExport`
- **Properties:**
  - `Destination` — Target export destination; default `Panel`
  - `OutputPath` (nullable) — File path for File exports; auto-generates if null
  - `Fields` — Frontmatter fields to include; default `All`
  - `Overwrite` — Replace existing content; default true
  - `IncludeMetadata` — Include metadata in File exports; default true
  - `IncludeSourceReference` — Include source path/timestamp; default true
  - `ExportTemplate` (nullable) — Custom Mustache template for File exports
  - `ClipboardAsMarkdown` — Preserve Markdown in clipboard; default true
  - `UseCalloutBlock` — Wrap inline inserts in callout; default true
  - `CalloutType` — Callout type (info/note/tip/warning); default "info"
- **Methods:**
  - `Validate()` — Validates options for configured destination
- **Factory Methods:**
  - `ForDestination(destination)` — Creates defaults for destination

### SummaryExportResult Record

Outcome of export operations:
- **Namespace:** `Lexichord.Abstractions.Agents.SummaryExport`
- **Properties:**
  - `Success` — Whether export succeeded
  - `Destination` — Target destination
  - `OutputPath` (nullable) — Output file path for File exports
  - `ErrorMessage` (nullable) — Error details if failed
  - `BytesWritten` (nullable) — Bytes written for file operations
  - `CharactersWritten` (nullable) — Characters for clipboard/inline
  - `DidOverwrite` — Whether existing content was replaced
  - `ExportedAt` — UTC timestamp of export completion
- **Factory Methods:**
  - `Succeeded(destination, outputPath?)` — Creates successful result
  - `Failed(destination, errorMessage)` — Creates failed result

### CachedSummary Record

Cached summary entry with metadata:
- **Namespace:** `Lexichord.Abstractions.Agents.SummaryExport`
- **Properties:**
  - `DocumentPath` (required) — Source document path
  - `ContentHash` (required) — SHA256 hash for change detection
  - `Summary` (required) — Cached SummarizationResult
  - `Metadata` (nullable) — Cached DocumentMetadata
  - `CachedAt` — UTC timestamp of cache creation
  - `ExpiresAt` — UTC expiration time; default +7 days
- **Computed Properties:**
  - `IsExpired` — Whether cache entry has expired
  - `Age` — Time elapsed since caching
- **Factory Methods:**
  - `Create(documentPath, contentHash, summary, metadata?, expirationDays?)` — Creates cache entry with validation

### IClipboardService Interface

General clipboard abstraction:
- **Namespace:** `Lexichord.Abstractions.Contracts`
- **Methods:**
  - `SetTextAsync(text, ct)` — Sets clipboard text content
  - `GetTextAsync(ct)` — Gets clipboard text content
  - `ClearAsync(ct)` — Clears clipboard contents
  - `ContainsTextAsync(ct)` — Checks if clipboard contains text

### ISummaryExporter Interface

Main service contract for summary export:
- **Namespace:** `Lexichord.Abstractions.Agents.SummaryExport`
- **Methods:**
  - `ExportAsync(summary, sourceDocumentPath, options, ct)` — Exports summary to destination
  - `ExportMetadataAsync(metadata, sourceDocumentPath, options, ct)` — Exports metadata only (Frontmatter)
  - `UpdateFrontmatterAsync(documentPath, summary?, metadata?, ct)` — Merges into existing frontmatter
  - `GetCachedSummaryAsync(documentPath, ct)` — Retrieves valid cached summary
  - `CacheSummaryAsync(documentPath, summary, metadata?, ct)` — Stores summary in cache
  - `ClearCacheAsync(documentPath, ct)` — Removes cached summary
  - `ShowInPanelAsync(summary, metadata?, sourceDocumentPath)` — Opens Summary Panel UI

### ISummaryCacheService Interface

Internal caching service:
- **Namespace:** `Lexichord.Modules.Agents.SummaryExport.Services`
- **Methods:**
  - `GetAsync(documentPath, ct)` — Gets cached summary if valid
  - `SetAsync(documentPath, summary, metadata?, ct)` — Stores summary in cache
  - `ClearAsync(documentPath, ct)` — Removes cached summary
  - `ComputeContentHash(content)` — Computes SHA256 hash

### SummaryExporter Implementation

Core exporter implementing `ISummaryExporter`:
- **Namespace:** `Lexichord.Modules.Agents.SummaryExport`
- **Dependencies:**
  - `IFileService` — File read/write operations
  - `IEditorService` — Cursor positioning, text insertion
  - `IClipboardService` — Clipboard operations
  - `ISummaryCacheService` — Cache operations
  - `ILicenseContext` — Feature gating
  - `IMediator` — Event publishing
  - `ILogger` — Diagnostic logging
- **Export Handlers:**
  - Panel: Caches summary, publishes `SummaryPanelOpenedEvent`
  - Frontmatter: Parses/merges YAML, preserves user fields
  - File: Generates formatted Markdown with optional template
  - Clipboard: Copies with optional Markdown stripping
  - InlineInsert: Inserts at cursor with callout wrapping
- **License Gating:** Requires WriterPro tier
- **Error Handling:** 3-catch pattern (user cancel, timeout, generic)

### SummaryCacheService Implementation

Hybrid caching with memory + file persistence:
- **Namespace:** `Lexichord.Modules.Agents.SummaryExport.Services`
- **Storage:**
  - Memory: IMemoryCache with 4-hour sliding expiration
  - File: `.lexichord/cache/summaries/{hash}.json`
- **Cache Invalidation:**
  - Content hash mismatch (document modified)
  - Expiration time passed (default 7 days)
  - Manual clear via `ClearAsync()`
- **Content Hashing:** SHA256 with `sha256:` prefix

### ClipboardService Implementation

Avalonia clipboard wrapper:
- **Namespace:** `Lexichord.Modules.Agents.SummaryExport.Services`
- **Features:**
  - UI thread marshalling via `Dispatcher.UIThread`
  - Cross-platform support (Windows, macOS, Linux)
  - Cancellation token support
  - Comprehensive logging

### MediatR Events

Lifecycle events for observability:
- `SummaryExportStartedEvent(Destination, DocumentPath?, Timestamp)` — Published before export
- `SummaryExportedEvent(Destination, DocumentPath?, OutputPath?, BytesWritten?, CharactersWritten?, DidOverwrite, Duration, Timestamp)` — Published on success
- `SummaryExportFailedEvent(Destination, DocumentPath?, ErrorMessage, Timestamp)` — Published on failure
- `SummaryPanelOpenedEvent(DocumentPath, Mode, HasMetadata, WasCached, Timestamp)` — Published when panel opens

### SummaryPanelViewModel

ViewModel for Summary Panel UI:
- **Namespace:** `Lexichord.Modules.Agents.SummaryExport.ViewModels`
- **Base Class:** `ObservableObject` (CommunityToolkit.Mvvm)
- **Lifetime:** Transient (per panel instance)
- **Observable Properties:**
  - Document: `DocumentPath`, `DocumentName`
  - Content: `Summary`, `Metadata`, `SelectedMode`
  - State: `IsLoading`, `HasContent`, `HasMetadata`, `ErrorMessage`
  - Display: `GenerationInfo`, `CompressionDisplay`, `ReadingTimeDisplay`, `ComplexityDisplay`, `AudienceDisplay`, `CategoryDisplay`, `TagsDisplay`
- **Collections:**
  - `KeyTerms` (ObservableCollection<KeyTermViewModel>)
  - `AvailableModes` (IReadOnlyList<SummarizationMode>)
- **Commands:**
  - `RefreshCommand` — Regenerates summary
  - `CopySummaryCommand` — Copies to clipboard
  - `AddToFrontmatterCommand` — Adds to document frontmatter
  - `ExportFileCommand` — Exports to .summary.md file
  - `CopyKeyTermsCommand` — Copies key terms to clipboard
  - `ClearCacheCommand` — Clears cached summary
  - `CloseCommand` — Closes panel
- **Events:** `CloseRequested`

### KeyTermViewModel

Wrapper for key term display:
- **Namespace:** `Lexichord.Modules.Agents.SummaryExport.ViewModels`
- **Properties:**
  - `Term` — The term text
  - `Importance` — Importance score (0.0-1.0)
  - `ImportanceLevel` — Scaled 1-5 for dot visualization
  - `FilledDots` / `EmptyDots` — Dot strings for importance display

### SummaryPanelView

Avalonia panel for summary display:
- **Namespace:** `Lexichord.Modules.Agents.SummaryExport.Views`
- **Layout:**
  - Header: Document name, mode badge, refresh/close buttons
  - Content: Summary text, compression info
  - Metadata: Reading time, complexity, audience, category
  - Key Terms: Chip display with importance dots
  - Tags: Tag list display
  - Actions: Copy, Frontmatter, Export buttons
- **Keyboard Shortcuts:**
  - `Ctrl+R` — Refresh
  - `Ctrl+C` — Copy to clipboard
  - `Ctrl+S` — Export to file
  - `Escape` — Close panel

### KeyTermChip View

Chip component for key term display:
- **Namespace:** `Lexichord.Modules.Agents.SummaryExport.Views`
- **Features:**
  - Term text display
  - Filled/empty dot importance visualization
  - Hover tooltip with importance percentage

### DI Registration

Added `SummaryExportServiceCollectionExtensions` with `AddSummaryExportPipeline()`:
```csharp
services.AddSingleton<ISummaryCacheService, SummaryCacheService>();
services.AddSingleton<IClipboardService, ClipboardService>();
services.AddSingleton<ISummaryExporter, SummaryExporter>();
services.AddTransient<SummaryPanelViewModel>();
services.AddTransient<KeyTermViewModel>();
```

Updated `AgentsModule.RegisterServices()` with `services.AddSummaryExportPipeline()` call. Initialization verification confirms `ISummaryExporter` and `IClipboardService` service availability.

---

## Files Created

### Abstractions (7 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Abstractions/Agents/SummaryExport/ExportDestination.cs` | Enum | 5 export destination targets |
| `src/Lexichord.Abstractions/Agents/SummaryExport/FrontmatterFields.cs` | Flags Enum | Selective field inclusion |
| `src/Lexichord.Abstractions/Agents/SummaryExport/SummaryExportOptions.cs` | Record | Export configuration |
| `src/Lexichord.Abstractions/Agents/SummaryExport/SummaryExportResult.cs` | Record | Export outcome |
| `src/Lexichord.Abstractions/Agents/SummaryExport/CachedSummary.cs` | Record | Cache entry with metadata |
| `src/Lexichord.Abstractions/Agents/SummaryExport/ISummaryExporter.cs` | Interface | Main export service contract |
| `src/Lexichord.Abstractions/Contracts/IClipboardService.cs` | Interface | General clipboard abstraction |

### Implementation (14 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Modules.Agents/SummaryExport/SummaryExporter.cs` | Class | Core ISummaryExporter implementation |
| `src/Lexichord.Modules.Agents/SummaryExport/Services/ClipboardService.cs` | Class | Avalonia clipboard wrapper |
| `src/Lexichord.Modules.Agents/SummaryExport/Services/SummaryCacheService.cs` | Class | Hybrid cache implementation |
| `src/Lexichord.Modules.Agents/SummaryExport/Events/SummaryExportStartedEvent.cs` | Record | Export started event |
| `src/Lexichord.Modules.Agents/SummaryExport/Events/SummaryExportedEvent.cs` | Record | Export completed event |
| `src/Lexichord.Modules.Agents/SummaryExport/Events/SummaryExportFailedEvent.cs` | Record | Export failed event |
| `src/Lexichord.Modules.Agents/SummaryExport/Events/SummaryPanelOpenedEvent.cs` | Record | Panel opened event |
| `src/Lexichord.Modules.Agents/SummaryExport/ViewModels/SummaryPanelViewModel.cs` | Class | Panel orchestration ViewModel |
| `src/Lexichord.Modules.Agents/SummaryExport/ViewModels/KeyTermViewModel.cs` | Class | Key term display wrapper |
| `src/Lexichord.Modules.Agents/SummaryExport/Views/SummaryPanelView.axaml` | XAML | Panel UI layout |
| `src/Lexichord.Modules.Agents/SummaryExport/Views/SummaryPanelView.axaml.cs` | Class | Panel code-behind |
| `src/Lexichord.Modules.Agents/SummaryExport/Views/KeyTermChip.axaml` | XAML | Key term chip component |
| `src/Lexichord.Modules.Agents/SummaryExport/Views/KeyTermChip.axaml.cs` | Class | Chip code-behind |
| `src/Lexichord.Modules.Agents/Extensions/SummaryExportServiceCollectionExtensions.cs` | Class | DI registration extension |

### Tests (8 files)

| File | Test Count | Description |
|:-----|:----------:|:------------|
| `tests/Lexichord.Tests.Unit/Modules/Agents/SummaryExport/SummaryExportOptionsTests.cs` | 25 | Options validation and defaults |
| `tests/Lexichord.Tests.Unit/Modules/Agents/SummaryExport/SummaryExportResultTests.cs` | 15 | Factory methods and properties |
| `tests/Lexichord.Tests.Unit/Modules/Agents/SummaryExport/CachedSummaryTests.cs` | 18 | Cache entry validation |
| `tests/Lexichord.Tests.Unit/Modules/Agents/SummaryExport/FrontmatterFieldsTests.cs` | 12 | Flags enum operations |
| `tests/Lexichord.Tests.Unit/Modules/Agents/SummaryExport/ClipboardServiceTests.cs` | 10 | Constructor and validation |
| `tests/Lexichord.Tests.Unit/Modules/Agents/SummaryExport/SummaryCacheServiceTests.cs` | 20 | Cache operations |
| `tests/Lexichord.Tests.Unit/Modules/Agents/SummaryExport/SummaryExporterTests.cs` | 35 | Export logic and events |
| `tests/Lexichord.Tests.Unit/Modules/Agents/SummaryExport/SummaryPanelViewModelTests.cs` | 25 | ViewModel behavior |

---

## Files Modified

| File | Changes |
|:-----|:--------|
| `src/Lexichord.Abstractions/Constants/FeatureCodes.cs` | Added `SummaryExport = "Feature.SummaryExport"` constant |
| `src/Lexichord.Modules.Agents/AgentsModule.cs` | Added `services.AddSummaryExportPipeline()` and init verification |
| `docs/changelogs/CHANGELOG.md` | Added v0.7.6c entry |

---

## Technical Details

### Cache Storage

**Memory Cache:**
- Provider: `IMemoryCache`
- Key Format: `summary_cache_{sha256(normalized_path)}`
- Expiration: 4-hour sliding expiration

**File Cache:**
- Location: `.lexichord/cache/summaries/{hash}.json`
- Format: JSON with snake_case naming
- Expiration: 7-day absolute expiration

### Content Hashing

SHA256 hash of document content:
```csharp
var bytes = Encoding.UTF8.GetBytes(content);
var hashBytes = SHA256.HashData(bytes);
var hashString = Convert.ToHexString(hashBytes).ToLowerInvariant();
return $"sha256:{hashString}";
```

### Frontmatter Merge Algorithm

1. Parse existing frontmatter (if present)
2. Preserve user-defined fields
3. Add/update `summary` section with:
   - `text`, `mode`, `word_count`, `compression_ratio`
   - `generated_at`, `model`, `items` (if applicable)
4. Add/update `metadata` section with:
   - `reading_time_minutes`, `category`, `target_audience`
   - `tags`, `key_terms`, `complexity_score`, `document_type`
5. Serialize back to YAML
6. Write: `---\n{yaml}---\n{body}`

### File Export Template Placeholders

| Placeholder | Description |
|:------------|:------------|
| `{{document_title}}` | Source document name |
| `{{source_name}}` | Source file name |
| `{{source_path}}` | Full path to source |
| `{{generated_at}}` | Generation timestamp |
| `{{model}}` | LLM model used |
| `{{summary}}` | Summary text |
| `{{reading_time}}` | Reading time in minutes |
| `{{complexity}}` | Complexity score |
| `{{document_type}}` | Detected document type |
| `{{target_audience}}` | Inferred audience |
| `{{category}}` | Primary category |

### Callout Format (InlineInsert)

GitHub/Obsidian callout syntax:
```markdown
> [!info] Summary
> • First key point
> • Second key point
> • Third key point
```

---

## License Gating

All v0.7.6c export features require **WriterPro** tier:
- Feature Code: `FeatureCodes.SummaryExport`
- Lower tiers receive `SummaryExportResult.Failed(destination, "Upgrade to WriterPro to use export features.")`

---

## Testing

### Test Categories

- **Unit Tests:** 160+ tests across 8 test files
- **Traits:** `[Trait("Category", "Unit")]`, `[Trait("SubPart", "v0.7.6c")]`

### Test Coverage

| Component | Test File | Coverage |
|:----------|:----------|:---------|
| SummaryExportOptions | SummaryExportOptionsTests.cs | Defaults, validation, factories |
| SummaryExportResult | SummaryExportResultTests.cs | Factories, timestamps |
| CachedSummary | CachedSummaryTests.cs | Creation, expiration, age |
| FrontmatterFields | FrontmatterFieldsTests.cs | Flag combinations |
| ClipboardService | ClipboardServiceTests.cs | Constructor, validation |
| SummaryCacheService | SummaryCacheServiceTests.cs | Get/Set/Clear, hashing |
| SummaryExporter | SummaryExporterTests.cs | All destinations, events |
| SummaryPanelViewModel | SummaryPanelViewModelTests.cs | Commands, state |

### Running Tests

```bash
# Run v0.7.6c tests only
dotnet test --filter "SubPart=v0.7.6c"

# Run all v0.7.6 tests
dotnet test --filter "SubPart~v0.7.6"
```

---

## Dependencies

### New Dependencies

None — uses existing project dependencies:
- `YamlDotNet` for frontmatter serialization
- `Microsoft.Extensions.Caching.Memory` for IMemoryCache
- `CommunityToolkit.Mvvm` for ViewModels

### Existing Dependencies Used

- `MediatR` for event publishing
- `Avalonia` for UI components
- `System.Text.Json` for cache file serialization

---

## Migration Notes

No breaking changes. New functionality is additive.

---

## See Also

- [v0.7.6a Changelog](LCS-CL-v0.7.6a.md) — Summarization Modes
- [v0.7.6b Changelog](LCS-CL-v0.7.6b.md) — Metadata Extraction
- [v0.7.6 Specification](../../specs/v0.7.x/v0.7.6/LCS-SBD-v0.7.6.md) — Full feature specification
