# Changelog: v0.5.7d — Search Result Actions

**Date:** 2026-02-03
**Status:** Complete
**Feature:** Search Result Actions (LCS-DES-v0.5.7d)
**License Gate:** Writer Pro (soft gate for export/citation copy via `RAG-SEARCH-ACTIONS`)

## Summary

Implements bulk actions on search results: copy (text, citation, markdown, JSON), export (JSON, CSV, Markdown, BibTeX), and open-all documents. Export and citation-formatted copy are gated to Writer Pro tier.

## New Components

### Abstractions — `Lexichord.Abstractions/Contracts/RAG/`

| File | Type | Purpose |
|------|------|---------|
| `ISearchActionsService.cs` | Interface + Records | Service contract with 7 supporting types |

**Types defined in interface file:**
- `SearchActionCopyFormat` — Enum: PlainText, CitationFormatted, Markdown, Json
- `SearchActionExportFormat` — Enum: Json, Csv, Markdown, BibTeX
- `SearchExportOptions` — Record: format, path, include flags
- `SearchActionResult` — Record: Success, ItemCount, Elapsed, ErrorMessage
- `SearchExportResult` — Record: extends with OutputPath, BytesWritten
- `SearchOpenAllResult` — Record: extends with OpenedCount, SkippedCount, FailedPaths
- `SearchResultSet` + `SearchResultGroup` — Grouped result containers

### Service Layer — `Lexichord.Modules.RAG/Services/`

| File | Type | Purpose |
|------|------|---------|
| `SearchActionsService.cs` | Singleton | Copy, export, open-all operations |

### Events — `Lexichord.Modules.RAG/Events/`

| File | Type | Purpose |
|------|------|---------|
| `SearchResultsExportedEvent.cs` | MediatR Notification | Export telemetry (format, count, path, duration) |

## DI Registration

Added to `RAGModule.cs`:
- `ISearchActionsService` → `SearchActionsService` (singleton)

## Dependencies

| Upstream | Purpose |
|----------|---------|
| `ICitationService` | Formatted citation generation |
| `IEditorService` | Open documents at path |
| `ILicenseContext` | Feature gating (`RAG-SEARCH-ACTIONS`) |
| `IMediator` | Event publishing |

## Unit Tests

| Test Class | Count | Coverage |
|------------|-------|----------|
| `SearchActionsServiceTests.cs` | 24 | Constructor, copy, export, open-all, license |

## Verification

```bash
# Build
dotnet build src/Lexichord.Modules.RAG

# Test
dotnet test tests/Lexichord.Tests.Unit --filter "Feature=v0.5.7d"
```

## Related Documents

- **Spec:** [LCS-DES-v0.5.7d.md](file:///Users/ryan/Documents/GitHub/lexichord/docs/specs/v0.5.x/v0.5.7/LCS-DES-v0.5.7d.md)
- **v0.5.7c:** [LCS-CL-v057c.md](./LCS-CL-v057c.md)
