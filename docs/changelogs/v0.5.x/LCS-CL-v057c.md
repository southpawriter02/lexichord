# Changelog: v0.5.7c — Preview Pane

**Date:** 2026-02-03
**Status:** Complete
**Feature:** Split-View Preview Pane (LCS-DES-v0.5.7c)
**License Gate:** Writer Pro (soft gate via `RAG-PREVIEW-PANE`)

## Summary

Implements a split-view preview pane adjacent to search results, displaying expanded context for selected hits with heading breadcrumbs and quick actions.

## New Components

### Data Contracts — `Lexichord.Modules.RAG/Models/`

| File | Type | Purpose |
|------|------|---------|
| `PreviewOptions.cs` | Record | Configuration: LinesBefore/After, IncludeBreadcrumb |
| `PreviewContent.cs` | Record | Preview display data with context sections |

### Service Layer — `Lexichord.Modules.RAG/Services/`

| File | Type | Purpose |
|------|------|---------|
| `IPreviewContentBuilder.cs` | Interface | Contract for preview content building |
| `PreviewContentBuilder.cs` | Singleton | Coordinates context expansion + snippet extraction |

### ViewModel Layer — `Lexichord.Modules.RAG/ViewModels/`

| File | Type | Purpose |
|------|------|---------|
| `PreviewPaneViewModel.cs` | Transient | Async loading, visibility toggle, clipboard commands |

## DI Registration

Added to `RAGModule.cs`:
- `IPreviewContentBuilder` → `PreviewContentBuilder` (singleton)
- `PreviewPaneViewModel` (transient)

## Dependencies

| Upstream | Purpose |
|----------|---------|
| `IContextExpansionService` | Sibling chunk retrieval + headings |
| `ISnippetService` | Highlight span extraction |
| `IEditorService` | Open document at line |
| `ILicenseContext` | Feature gating |

## Unit Tests

| Test Class | Count | Coverage |
|------------|-------|----------|
| `PreviewContentBuilderTests.cs` | 10 | Constructor, BuildAsync, breadcrumb |
| `PreviewPaneViewModelTests.cs` | 16 | Constructor, commands, license, state |
| **Total** | **26** | |

## Verification

```bash
# Build
dotnet build src/Lexichord.Modules.RAG

# Test
dotnet test tests/Lexichord.Tests.Unit --filter "Feature=v0.5.7c"
```

## Related Documents

- **Spec:** [LCS-DES-v0.5.7c.md](file:///Users/ryan/Documents/GitHub/lexichord/docs/specs/v0.5.x/v0.5.7/LCS-DES-v0.5.7c.md)
- **v0.5.7b:** [LCS-CL-v057b.md](./LCS-CL-v057b.md)
