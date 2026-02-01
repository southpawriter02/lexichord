# LCS-CL-v0.4.1c: Repository Abstractions

**Version**: v0.4.1c  
**Parent**: [LCS-DES-v0.4.1c](../../specs/v0.4.x/v0.4.1/LCS-DES-v0.4.1c.md)  
**Date**: 2026-01-31  
**Status**: ✅ Complete

## Summary

Implemented `IDocumentRepository` and `IChunkRepository` interfaces in `Lexichord.Abstractions` for the RAG subsystem's vector storage layer. These interfaces define the data access contracts for document lifecycle management and vector similarity search.

## New Files

| File                                   | Description                                                                         |
| -------------------------------------- | ----------------------------------------------------------------------------------- |
| `Contracts/RAG/DocumentStatus.cs`      | Enum defining document lifecycle states (Pending, Indexing, Indexed, Failed, Stale) |
| `Contracts/RAG/Document.cs`            | Record representing an indexed document with metadata                               |
| `Contracts/RAG/Chunk.cs`               | Record representing a document chunk with embedding vector                          |
| `Contracts/RAG/ChunkSearchResult.cs`   | Record wrapping chunk with similarity score                                         |
| `Contracts/RAG/IDocumentRepository.cs` | Repository interface for document CRUD operations                                   |
| `Contracts/RAG/IChunkRepository.cs`    | Repository interface for chunk storage and vector search                            |

## Unit Tests

| Test File                             | Coverage                                                       |
| ------------------------------------- | -------------------------------------------------------------- |
| `DocumentStatusTests.cs`              | Enum value verification, string parsing                        |
| `DocumentRecordTests.cs`              | Record equality, with-expressions, CreatePending factory       |
| `ChunkRecordTests.cs`                 | Record equality, computed properties, CreateWithoutEmbedding   |
| `ChunkSearchResultRecordTests.cs`     | Threshold properties (IsHighConfidence, MeetsMinimumThreshold) |
| `IDocumentRepositoryContractTests.cs` | Interface contract: 8 methods with async signatures            |
| `IChunkRepositoryContractTests.cs`    | Interface contract: 4 methods including SearchSimilarAsync     |

## Key Design Decisions

1. **Immutable Records**: All data types use C# records for thread-safe, value-equality semantics.
2. **Factory Methods**: `Document.CreatePending` and `Chunk.CreateWithoutEmbedding` ensure correct initial state.
3. **Nullable Embeddings**: `Chunk.Embedding` is nullable to support pre-embedding phase.
4. **CancellationToken**: All async methods accept optional cancellation tokens for graceful shutdown.
5. **Computed Properties**: `ChunkSearchResult.IsHighConfidence` and `MeetsMinimumThreshold` for convenience filtering.

## Dependencies

- Builds on v0.4.1b schema migration (Documents/Chunks tables)
- Required by v0.4.1d (Health Check) and later RAG implementations

## Verification

- ✅ Build: `dotnet build src/Lexichord.Abstractions` - Success
- ✅ Tests: 108 total (107 passed, 1 skipped - Windows-only vault test)
