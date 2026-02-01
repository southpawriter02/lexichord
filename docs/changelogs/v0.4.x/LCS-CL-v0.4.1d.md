# Changelog - v0.4.1d: Dapper Repository Implementation

**Release Date:** 2026-02-01

## Overview

Implements the Dapper-based data access layer for the RAG (Retrieval-Augmented Generation) subsystem, providing high-performance database operations for document indexing and vector similarity search.

## New Files

### `src/Lexichord.Modules.RAG/`

| File                           | Description                                                  |
| ------------------------------ | ------------------------------------------------------------ |
| `Lexichord.Modules.RAG.csproj` | Module project with Dapper and Pgvector dependencies         |
| `RAGModule.cs`                 | Module registration with VectorTypeHandler and repository DI |
| `Data/VectorTypeHandler.cs`    | Custom Dapper type handler for pgvector VECTOR type          |
| `Data/DocumentRepository.cs`   | Full IDocumentRepository implementation with CRUD ops        |
| `Data/ChunkRepository.cs`      | IChunkRepository with vector similarity search               |

### `tests/Lexichord.Tests.Unit/Modules/RAG/`

| File                         | Description                                        |
| ---------------------------- | -------------------------------------------------- |
| `VectorTypeHandlerTests.cs`  | 10 tests for SetValue/Parse operations             |
| `DocumentRepositoryTests.cs` | Constructor validation tests                       |
| `ChunkRepositoryTests.cs`    | Constructor + embedding dimension validation tests |

## Key Design Decisions

### VectorTypeHandler

- Converts `float[]` ↔ `Pgvector.Vector` for database operations
- Handles null bidirectionally (null → DBNull, DBNull → null)
- Registered globally with `SqlMapper.AddTypeHandler()` during module init

### Repository Pattern

- Follows existing `TerminologyRepository` pattern from Style module
- Uses `IDbConnectionFactory` for connection management
- `await using` ensures proper connection disposal
- All queries use parameterized SQL to prevent injection
- Comprehensive debug-level logging for troubleshooting

### Vector Similarity Search

- Uses pgvector's `<=>` operator for cosine distance
- Similarity score = `1 - cosine_distance`
- Validates 1536 embedding dimensions before database call
- Optional project scoping via JOIN with documents table
- Performance warning logs for queries >100ms

## Dependencies

- `Dapper` 2.1.35
- `Pgvector` 0.3.0

## Test Results

```
Total tests: 128 (RAG-related)
     Passed: 127
    Skipped: 1
```

## Verification Commands

```bash
# Build module
dotnet build src/Lexichord.Modules.RAG/Lexichord.Modules.RAG.csproj

# Run unit tests
dotnet test tests/Lexichord.Tests.Unit --filter "FullyQualifiedName~RAG"
```
