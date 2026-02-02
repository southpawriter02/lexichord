# Changelog v0.4.7b - Manual Indexing Controls

**Released:** 2026-02-02  
**Specification:** [LCS-DES-v0.4.7b](../specs/v0.4.x/v0.4.7/LCS-DES-v0.4.7b.md)

## Summary

v0.4.7b introduces manual indexing controls to the Index Status View, enabling users to manage their indexed document corpus through the Settings interface. Users can now re-index individual documents, remove documents from the index, and trigger a full corpus re-index.

## Added

### Interfaces (Lexichord.Abstractions)

- **[IIndexManagementService](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Abstractions/Contracts/RAG/IIndexManagementService.cs)**: Interface for manual index management operations
    - `ReindexDocumentAsync(documentId)`: Re-indexes a single document
    - `RemoveFromIndexAsync(documentId)`: Removes a document and its chunks
    - `ReindexAllAsync(progress)`: Re-indexes all documents with progress reporting

- **[IndexManagementResult](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Abstractions/Contracts/RAG/IndexManagementResult.cs)**: Immutable result record for operations
    - Factory methods: `SuccessSingle`, `FailureSingle`, `NotFound`, `Bulk`

### Services (Lexichord.Modules.RAG)

- **[IndexManagementService](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Modules.RAG/Services/IndexManagementService.cs)**: Implementation of `IIndexManagementService`
    - Dependencies: `IDocumentRepository`, `IChunkRepository`, `DocumentIndexingPipeline`, `IMediator`
    - Exhaustive logging for all operations
    - Proper cancellation token propagation

### MediatR Events (Telemetry)

- **[IndexManagementEvents](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Modules.RAG/Indexing/IndexManagementEvents.cs)**:
    - `DocumentReindexedEvent(DocumentId, FilePath, ChunkCount, Duration)`
    - `DocumentRemovedFromIndexEvent(DocumentId, FilePath)`
    - `AllDocumentsReindexedEvent(SuccessCount, FailedCount, TotalDuration)`

### ViewModel Commands

- **IndexStatusViewModel** enhanced with:
    - `ReindexDocumentCommand`: Re-indexes selected document
    - `RemoveFromIndexCommand`: Removes selected document (with confirmation)
    - `ReindexAllCommand`: Re-indexes all documents (with confirmation)
    - `ConfirmationDelegate`: Delegate for wiring confirmation dialogs
    - `SelectedDocument`: Property for context menu binding
    - `ProgressPercent`: Property for bulk operation progress
    - `LastOperationMessage`: Property for operation feedback

## Modified

- **[RAGModule.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Modules.RAG/RAGModule.cs)**:
    - Added DI registration for `IIndexManagementService`
    - Updated module version to 0.4.7

## Tests

- **[IndexManagementServiceTests](file:///Users/ryan/Documents/GitHub/lexichord/tests/Lexichord.Tests.Unit/Modules/RAG/Services/IndexManagementServiceTests.cs)**:
    - Constructor null-guard tests (5 tests)
    - `ReindexDocumentAsync` tests: success, not found, file not found, event publishing
    - `RemoveFromIndexAsync` tests: success, not found, delete order, event publishing
    - `ReindexAllAsync` tests: empty result, progress reporting, partial failures, event publishing
    - Cancellation token tests

## Dependencies

| Component               | Dependency               | Version |
| ----------------------- | ------------------------ | ------- |
| IIndexManagementService | IDocumentRepository      | v0.4.1c |
| IIndexManagementService | IChunkRepository         | v0.4.1c |
| IIndexManagementService | DocumentIndexingPipeline | v0.4.4d |
| IndexStatusViewModel    | IIndexStatusService      | v0.4.7a |
| IndexStatusViewModel    | IIndexManagementService  | v0.4.7b |

## Breaking Changes

None. This is a backward-compatible feature addition.

## Migration Notes

No migration required. The new commands are available in the Index Status View immediately after upgrade.
