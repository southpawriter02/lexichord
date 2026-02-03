# LCS-CL-053d: Context Preview UI

**Version:** v0.5.3d
**Date:** 2026-02
**Status:** ✅ Complete

## Summary

Implemented the Context Preview UI for the RAG module's search results. This feature adds expandable context previews showing surrounding chunks and heading breadcrumbs, with license gating for Writer Pro users. The implementation includes a new ViewModel for expansion state management, custom Avalonia controls for breadcrumb and context display, and comprehensive unit tests.

## Changes

### Constants (`Lexichord.Abstractions`)

| File                   | Change                                              |
| ---------------------- | --------------------------------------------------- |
| `FeatureCodes.cs`      | Added `ContextExpansion` feature code constant      |

### ViewModels (`Lexichord.Modules.RAG`)

| File                                 | Change                                                           |
| ------------------------------------ | ---------------------------------------------------------------- |
| `IContextPreviewViewModelFactory.cs` | New factory interface for creating ContextPreviewViewModel       |
| `ContextPreviewViewModelFactory.cs`  | Factory implementation with TextChunk to Chunk conversion        |
| `ContextPreviewViewModel.cs`         | New ViewModel managing expand/collapse state and license gating  |
| `SearchResultItemViewModel.cs`       | Added optional `ContextPreview` property and factory parameter   |

### Controls (`Lexichord.Modules.RAG`)

| File                      | Change                                                     |
| ------------------------- | ---------------------------------------------------------- |
| `BreadcrumbControl.cs`    | New TemplatedControl for displaying heading breadcrumbs    |
| `ContextChunkControl.cs`  | New TemplatedControl for context chunk display with muting |

### Converters (`Lexichord.Modules.RAG`)

| File                        | Change                                                  |
| --------------------------- | ------------------------------------------------------- |
| `LicenseTooltipConverter.cs`| New converter for license-aware expand button tooltips  |

### Views (`Lexichord.Modules.RAG`)

| File                          | Change                                                    |
| ----------------------------- | --------------------------------------------------------- |
| `SearchResultItemView.axaml`  | Added context preview panel, breadcrumb, expand button    |
| `SearchResultItemView.axaml.cs` | Added UpgradePromptRequestedEvent for cross-module dialog |

### Module (`Lexichord.Modules.RAG`)

| File           | Change                                                          |
| -------------- | --------------------------------------------------------------- |
| `RAGModule.cs` | Registered `IContextPreviewViewModelFactory` in DI container    |

### Tests (`Lexichord.Tests.Unit`)

| File                                     | Tests                                       |
| ---------------------------------------- | ------------------------------------------- |
| `ContextPreviewViewModelTests.cs`        | 32 tests for ViewModel behavior             |
| `ContextPreviewViewModelFactoryTests.cs` | 12 tests for factory behavior               |

## Key Features

### ContextPreviewViewModel

- **Expand/Collapse State**: `IsExpanded`, `IsLoading` properties with `ToggleExpandedCommand`
- **License Gating**: Checks `ILicenseContext` for WriterPro+ tier and ContextExpansion feature
- **Upgrade Prompt**: `ShowUpgradePromptRequested` flag for View to display upgrade dialog
- **Caching**: Caches fetched `ExpandedChunk` to avoid redundant service calls
- **Error Handling**: Sets `ErrorMessage` on service failure with retry capability
- **Computed Properties**: `ExpandButtonIcon`, `ExpandButtonText`, `ShowLockIcon`, `HasBreadcrumb`

### ContextPreviewViewModelFactory

- **TextChunk Conversion**: Converts `SearchHit.Chunk` (TextChunk) to RAG `Chunk` type
- **Deterministic ID**: Generates consistent chunk ID from DocumentId + ChunkIndex for caching
- **Dependency Injection**: Provides `IContextExpansionService`, `ILicenseContext`, and logger to ViewModels

### BreadcrumbControl

- **Styled Property**: `Breadcrumb` (string?) for breadcrumb text
- **Computed Property**: `HasBreadcrumb` for visibility binding
- **Visual Style**: Bookmark icon with muted text

### ContextChunkControl

- **Styled Properties**: `ChunkContent` (string?), `IsMuted` (bool), `MaxLines` (int)
- **Opacity Constants**: `MutedOpacity` (0.7), `NormalOpacity` (1.0)
- **Computed Properties**: `ContentOpacity`, `HasContent`

### LicenseTooltipConverter

- **Licensed**: Returns "Expand to show context"
- **Unlicensed**: Returns "Upgrade to Writer Pro to expand context"

### SearchResultItemView Updates

- **Breadcrumb Display**: Shows heading trail below document title
- **Collapsed State**: Existing preview with HighlightedTextBlock
- **Expanded State**: Before chunks → Core chunk (highlighted) → After chunks
- **Loading State**: ProgressBar with "Loading context..." text
- **Error State**: Warning banner with error message
- **Expand Button**: Lock icon for unlicensed users, toggle text/icon

### Cross-Module Communication

- **Routed Event**: `UpgradePromptRequestedEvent` bubbles up from RAG module
- **Event Args**: `UpgradePromptRequestedEventArgs` with feature code
- **Host Handling**: Parent window handles event to show `UpgradePromptDialog`

## Architecture Notes

### Type Conversion

The `SearchHit` record contains a `TextChunk` with `ChunkMetadata`, while `IContextExpansionService.ExpandAsync()` expects a RAG `Chunk`. The factory performs this conversion:

```
TextChunk                   →  Chunk
  .Content                     .Content
  .StartOffset                 .StartOffset
  .EndOffset                   .EndOffset
  .Metadata.Index              .ChunkIndex
  .Metadata.Heading            .Heading
  .Metadata.Level              .HeadingLevel
  (Document.Id)                .DocumentId
  (generated)                  .Id (deterministic GUID)
```

### Deterministic Chunk ID

Since `TextChunk` doesn't carry a database ID, the factory generates one by XORing the document ID bytes with the chunk index. This ensures:
- Same document + index always produces the same ID
- Consistent caching in `IContextExpansionService`
- No collisions within a document

## Dependencies

- `IContextExpansionService` (v0.5.3a)
- `IHeadingHierarchyService` (v0.5.3c)
- `ExpandedChunk` record (v0.5.3a)
- `ILicenseContext` (v0.0.4c)
- `FeatureCodes` constants
- `CommunityToolkit.Mvvm` attributes

## Test Coverage

### ContextPreviewViewModelTests (32 tests)

- Constructor validation (null parameters)
- Initial state verification (license, breadcrumb)
- License gating (upgrade prompt for unlicensed)
- Expand/collapse behavior
- Caching (skip service when cached)
- Error handling (set error message)
- Computed properties (icons, text, lock)
- RefreshContext command
- AcknowledgeUpgradePrompt method
- RefreshLicenseState method

### ContextPreviewViewModelFactoryTests (12 tests)

- Constructor null validation
- Create with valid SearchHit
- Create with null SearchHit
- Heading transfer from metadata
- License state from context
- Independent instance creation
- Deterministic ID consistency

## Migration Notes

None required. This is a new feature addition with no breaking changes.

## Related Documents

- [LCS-DES-v0.5.3-INDEX.md](../../specs/v0.5.x/v0.5.3/LCS-DES-v0.5.3-INDEX.md) - v0.5.3 sub-parts index
- [LCS-SBD-v0.5.3.md](../../specs/v0.5.x/v0.5.3/LCS-SBD-v0.5.3.md) - v0.5.3 specification
- [LCS-CL-053a.md](LCS-CL-053a.md) - Context Expansion Service
- [LCS-CL-053c.md](LCS-CL-053c.md) - Heading Hierarchy Service
