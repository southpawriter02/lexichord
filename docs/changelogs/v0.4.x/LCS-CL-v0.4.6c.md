# Changelog: v0.4.6c - Source Navigation

**Release Date:** 2026-02-01
**Status:** Implemented
**Specification:** [LCS-DES-046c](../../specs/v0.4.x/v0.4.6/LCS-DES-v0.4.6c.md)

---

## Summary

Implements Source Navigation for the Reference Panel, enabling users to click search results to navigate directly to the matching text in the source document. Adds `IReferenceNavigationService` abstraction, `ReferenceNavigationService` implementation that bridges the RAG module with the editor's existing navigation infrastructure, `ReferenceNavigatedEvent` for telemetry, and `HighlightStyle` enum for editor highlight categorization.

---

## Changes

### Added

#### Lexichord.Abstractions/Contracts/Editor/

| File               | Type | Description                                                  |
| :----------------- | :--- | :----------------------------------------------------------- |
| `HighlightStyle.cs` | Enum | Text highlight styles for editor: SearchResult, Error, Warning, Reference |

#### Lexichord.Abstractions/Contracts/

| File                             | Type      | Description                                                                    |
| :------------------------------- | :-------- | :----------------------------------------------------------------------------- |
| `IReferenceNavigationService.cs` | Interface | Contract for navigating from search results to source documents with two methods |

#### Lexichord.Modules.RAG/Events/

| File                          | Type   | Description                                                          |
| :---------------------------- | :----- | :------------------------------------------------------------------- |
| `ReferenceNavigatedEvent.cs` | Record | MediatR INotification with DocumentPath, Offset, Length, Score, Timestamp |

#### Lexichord.Modules.RAG/Services/

| File                            | Type           | Description                                                              |
| :------------------------------ | :------------- | :----------------------------------------------------------------------- |
| `ReferenceNavigationService.cs` | Implementation | Bridges SearchHit to IEditorService + IEditorNavigationService for navigation |

#### Lexichord.Tests.Unit/Modules/RAG/Services/

| File                                  | Tests | Coverage                                                              |
| :------------------------------------ | ----: | :-------------------------------------------------------------------- |
| `ReferenceNavigationServiceTests.cs` |    19 | Input validation, document opening, navigation delegation, telemetry, error handling, constructor validation |

### Modified

| File                    | Change                                                                        |
| :---------------------- | :---------------------------------------------------------------------------- |
| `RAGModule.cs`          | Added v0.4.6c DI registration for IReferenceNavigationService; version → 0.4.6 |
| `ReferenceViewModel.cs` | Injected IReferenceNavigationService; replaced placeholder OnNavigateToResult with async navigation |

---

## Technical Details

### Architectural Adaptation

The design specification described phantom `IEditorService` methods (`IsDocumentOpenAsync`, `ScrollToOffsetAsync`, `SelectTextAsync`, `HighlightRangeAsync`, `SetCursorPositionAsync`) that do not exist in the codebase. The implementation adapts the spec to use the actual APIs:

- **`IEditorService.GetDocumentByPath(path)`** — checks if document is open
- **`IEditorService.OpenDocumentAsync(path)`** — opens closed documents
- **`IEditorNavigationService.NavigateToOffsetAsync(documentId, offset, length, ct)`** — handles activation, scrolling, caret positioning, and highlighting

### Navigation Flow

1. Validate SearchHit (null checks, path validation)
2. Look up document by path via `IEditorService.GetDocumentByPath`
3. If not open, open via `IEditorService.OpenDocumentAsync`
4. Get `DocumentId` from `IManuscriptViewModel`
5. Delegate to `IEditorNavigationService.NavigateToOffsetAsync`
6. On success, publish `ReferenceNavigatedEvent` via `IMediator`

### Error Handling

| Scenario              | Behavior                  |
| :-------------------- | :------------------------ |
| Null hit              | Return false, log warning |
| Missing document path | Return false, log warning |
| Document open fails   | Return false, log warning |
| File not found        | Return false, log warning |
| Invalid offset        | Clamp to 0, proceed       |
| Editor service error  | Return false, log error   |
| Cancellation          | Return false, log debug   |

### ReferenceViewModel Integration

- `IReferenceNavigationService` injected via constructor (v0.4.6c)
- `OnNavigateToResult(SearchHit)` now uses `async void` fire-and-forget pattern
- Error handling wraps navigation call with try/catch

---

## Verification

```bash
# Build RAG module
dotnet build src/Lexichord.Modules.RAG
# Result: Build succeeded, 0 errors

# Run v0.4.6c tests
dotnet test tests/Lexichord.Tests.Unit --filter "Feature=v0.4.6c"
# Result: 19 tests passed

# Run full RAG regression
dotnet test tests/Lexichord.Tests.Unit --filter "FullyQualifiedName~Modules.RAG"
# Result: 416 tests passed, 0 failures

# Run full test suite
dotnet test tests/Lexichord.Tests.Unit
# Result: 4451 passed, 33 skipped, 0 failures
```

---

## Test Coverage

| Category                                 | Tests |
| :--------------------------------------- | ----: |
| NavigateToHitAsync null/invalid input    |     2 |
| NavigateToHitAsync document opening      |     2 |
| NavigateToHitAsync navigation delegation |     3 |
| NavigateToHitAsync telemetry events      |     2 |
| NavigateToHitAsync error handling        |     1 |
| NavigateToOffsetAsync input validation   |     3 |
| NavigateToOffsetAsync error handling     |     2 |
| Constructor validation                   |     4 |
| **Total**                                | **19** |

---

## Dependencies

- v0.1.3a: IEditorService (document open/lookup)
- v0.2.6b: IEditorNavigationService (offset navigation, highlighting)
- v0.0.7a: IMediator (event publishing)
- v0.4.5a: SearchHit, TextChunk, Document (search result records)
- v0.4.6a: ReferenceViewModel (search panel orchestration)
- v0.4.6b: SearchResultItemViewModel (result item display)

## Dependents

- v0.4.6d: Search History (uses same panel infrastructure)
- v0.4.7: Index Manager (navigation patterns)

---

## Related Documents

- [LCS-DES-046c](../../specs/v0.4.x/v0.4.6/LCS-DES-v0.4.6c.md) - Design specification
- [LCS-CL-v0.4.6b](./LCS-CL-v0.4.6b.md) - Previous version (Search Result Item)
