# Changelog: v0.4.6a - Reference Panel View

**Release Date:** 2026-02-01
**Status:** Implemented
**Specification:** [LCS-DES-046-RP-a](../../specs/v0.4.x/v0.4.6/LCS-DES-v0.4.6a.md)

---

## Summary

Implements the Reference Panel View, the user-facing interface for semantic search in the RAG subsystem. This version adds the `ISearchHistoryService` abstraction, `SearchHistoryService` for thread-safe query history management, `ReferenceViewModel` for search orchestration, `SearchResultItemViewModel` for result display, and `ReferenceView.axaml` with virtualized results, license gating, and keyboard accessibility.

---

## Changes

### Added

#### Lexichord.Abstractions/Contracts/RAG/

| File                       | Type      | Description                                                    |
| :------------------------- | :-------- | :------------------------------------------------------------- |
| `ISearchHistoryService.cs` | Interface | Contract for search query history with Add, Get, Clear methods |

#### Lexichord.Modules.RAG/Search/

| File                      | Type           | Description                                                             |
| :------------------------ | :------------- | :---------------------------------------------------------------------- |
| `SearchHistoryService.cs` | Implementation | Thread-safe LinkedList-based history with O(1) operations, max 10 items |

#### Lexichord.Modules.RAG/ViewModels/

| File                           | Type      | Description                                                   |
| :----------------------------- | :-------- | :------------------------------------------------------------ |
| `ReferenceViewModel.cs`        | ViewModel | Search orchestration, history, license gating, error handling |
| `SearchResultItemViewModel.cs` | ViewModel | Wraps SearchHit for UI display with formatted properties      |

#### Lexichord.Modules.RAG/Views/

| File                     | Type        | Description                                                  |
| :----------------------- | :---------- | :----------------------------------------------------------- |
| `ReferenceView.axaml`    | UserControl | Search UI with autocomplete, virtualized results, status bar |
| `ReferenceView.axaml.cs` | Code-behind | Keyboard (Enter to search) and pointer event handlers        |

#### Lexichord.Tests.Unit/Modules/RAG/Search/

| File                           | Tests | Coverage                                                |
| :----------------------------- | ----: | :------------------------------------------------------ |
| `SearchHistoryServiceTests.cs` |    20 | Add, get, clear, deduplication, eviction, thread safety |

### Modified

| File                           | Change                                                               |
| :----------------------------- | :------------------------------------------------------------------- |
| `RAGModule.cs`                 | Added v0.4.6 section: ISearchHistoryService, ReferenceViewModel DI   |
| `Lexichord.Modules.RAG.csproj` | Added Avalonia 11.2.3, CommunityToolkit.Mvvm 8.4.0, version to 0.4.6 |

---

## Technical Details

### SearchHistoryService

- **Data Structure**: LinkedList + Dictionary for O(1) add, remove, and lookup
- **Deduplication**: Case-insensitive; re-adding moves query to front
- **Eviction**: FIFO when exceeding MaxHistorySize (default 10)
- **Thread Safety**: Lock-based synchronization

### ReferenceViewModel

- **License Gating**: Uses `SearchLicenseGuard.IsSearchAvailable`
- **Search Execution**: Async with cancellation support
- **History Integration**: Auto-refreshes history after each search

### ReferenceView.axaml

- **Virtualization**: `VirtualizingStackPanel` for efficient rendering
- **Accessibility**: `AutomationProperties.Name` on interactive elements
- **Keyboard Navigation**: Enter key triggers search

---

## Verification

```bash
# Build RAG module
dotnet build src/Lexichord.Modules.RAG
# Result: Build succeeded

# Run SearchHistoryService tests
dotnet test tests/Lexichord.Tests.Unit --filter "FullyQualifiedName~SearchHistoryServiceTests"
# Result: 20 tests passed

# Run full RAG regression
dotnet test tests/Lexichord.Tests.Unit --filter "FullyQualifiedName~Modules.RAG"
# Result: 360 tests passed, 0 failures
```

---

## Test Coverage

| Category                             |  Tests |
| :----------------------------------- | -----: |
| MaxHistorySize default               |      1 |
| AddQuery null/whitespace handling    |      4 |
| AddQuery valid/trimming              |      3 |
| AddQuery ordering (newest first)     |      1 |
| AddQuery duplicate deduplication     |      1 |
| AddQuery eviction on max size        |      1 |
| GetRecentQueries empty/zero/negative |      3 |
| GetRecentQueries limited count       |      1 |
| GetRecentQueries exceeds history     |      1 |
| ClearHistory operations              |      3 |
| Thread safety concurrent operations  |      2 |
| **Total**                            | **20** |

---

## Dependencies

- v0.4.5b: SearchLicenseGuard (license gating)
- v0.4.5c: ISemanticSearchService (search execution)
- v0.4.3a: SearchHit, SearchResult (result records)
- CommunityToolkit.Mvvm 8.4.0 (ObservableObject, RelayCommand)
- Avalonia 11.2.3 (UI framework)

## Dependents

- v0.4.6b: SearchResultItemView (result card styling)
- v0.4.6c: IReferenceNavigationService (result navigation)
- v0.4.6d: Shell region registration

---

## Related Documents

- [LCS-DES-046-RP-a](../../specs/v0.4.x/v0.4.6/LCS-DES-v0.4.6a.md) - Design specification
- [LCS-CL-v0.4.5g](./LCS-CL-v0.4.5g.md) - Previous version (Entity Abstraction Layer)
