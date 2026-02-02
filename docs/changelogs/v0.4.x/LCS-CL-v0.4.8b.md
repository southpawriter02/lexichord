# LCS-CL-v0.4.8b: Integration Test Infrastructure

## Document Control

| Field           | Value                                         |
| :-------------- | :-------------------------------------------- |
| **Document ID** | LCS-CL-048b                                   |
| **Version**     | v0.4.8b                                       |
| **Title**       | Integration Tests (Testcontainers + pgvector) |
| **Date**        | 2026-02-02                                    |
| **Author**      | Assistant                                     |

---

## Summary

Implemented end-to-end integration test suite for the RAG module using Testcontainers with PostgreSQL + pgvector. All 14 tests pass with full database roundtrip verification.

---

## Changes

### Added

- **PostgresRagFixture** (`tests/.../PostgresRagFixture.cs`): Test fixture for RAG integration tests
    - Uses `pgvector/pgvector:pg16` Docker image via Testcontainers
    - Implements `VectorEnabledConnectionFactory` with `UseVector()` for pgvector type mapping
    - Registers `VectorTypeHandler` with Dapper for embedding serialization
    - Provides Respawn-based database reset between tests
    - Configures mock `IEmbeddingService` with deterministic embeddings (SHA256-based)

- **IngestionPipelineTests** (`tests/.../IngestionPipelineTests.cs`): 4 tests
    - `IndexDocument_CreatesDocumentAndChunks`: Verifies pipeline creates document and chunks
    - `IndexDocument_StoresCorrectChunkCount`: Validates chunk count matches document record
    - `IndexDocument_SetsIndexedStatus`: Confirms status transitions to Indexed
    - `IndexDocument_ReindexUpdatesDocument`: Tests content change detection

- **SearchRoundtripTests** (`tests/.../SearchRoundtripTests.cs`): 6 tests
    - `Search_ReturnsRelevantResults`: Verifies search finds indexed content
    - `Search_RespectsTopK`: Tests TopK limiting
    - `Search_FiltersByDocument`: Tests document-scoped search
    - `Search_IncludesSearchDuration`: Validates timing metadata
    - `Search_ResultsIncludeChunkContent`: Confirms content is returned
    - `Search_ResultsHaveRelevanceScores`: Tests relevance score calculation

- **ChangeDetectionTests** (`tests/.../ChangeDetectionTests.cs`): 2 tests
    - `IndexDocument_SameContent_ProducesSameHash`: Hash stability for unchanged content
    - `IndexDocument_ChangedContent_ProducesDifferentHash`: Hash changes with content

- **DeletionCascadeTests** (`tests/.../DeletionCascadeTests.cs`): 2 tests
    - Tests document deletion cascade to chunks

### Fixed

- **DocumentIndexingPipeline**: Updated hash computation on reindex
    - Now computes new hash for every indexing operation
    - Compares with existing document hash before update
    - Updates document record when content changes

- **PostgresRagFixture Schema**: Changed `status` column from INTEGER to TEXT
    - Aligns with `DocumentRepository.AddAsync` using `ToString()` for DocumentStatus

- **DI Registration**: Fixed chunking strategy registration order
    - `MarkdownHeaderChunkingStrategy` depends on concrete `ParagraphChunkingStrategy` and `FixedSizeChunkingStrategy`
    - Registered concrete types before interfaces to satisfy dependencies

---

## Test Summary

| Category         |  Tests | Status  |
| :--------------- | -----: | :------ |
| Ingestion        |      4 | ✅ Pass |
| Search Roundtrip |      6 | ✅ Pass |
| Change Detection |      2 | ✅ Pass |
| Deletion Cascade |      2 | ✅ Pass |
| **Total**        | **14** | ✅ Pass |

---

## Technical Details

### pgvector Configuration

```csharp
// Fixture creates vector-enabled data source
var dataSourceBuilder = new NpgsqlDataSourceBuilder(ConnectionString);
dataSourceBuilder.UseVector(); // Enable pgvector type mapping
var dataSource = dataSourceBuilder.Build();

// Register Dapper type handler
SqlMapper.AddTypeHandler(new VectorTypeHandler());
```

### Deterministic Embeddings

```csharp
// Uses SHA256 hash for reproducible test embeddings
private static float[] GenerateDeterministicEmbedding(string text)
{
    var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
    var embedding = new float[1536];
    for (int i = 0; i < 1536; i++)
    {
        embedding[i] = (float)hash[i % hash.Length] / 255f;
    }
    // Normalize to unit vector...
    return embedding;
}
```

---

## Acceptance Criteria

| #   | Criterion                            | Status |
| :-- | :----------------------------------- | :----- |
| 1   | Testcontainers with pgvector working | ✅     |
| 2   | Ingestion pipeline tests pass        | ✅     |
| 3   | Search roundtrip tests pass          | ✅     |
| 4   | Change detection tests pass          | ✅     |
| 5   | Deletion cascade tests pass          | ✅     |
| 6   | Database reset between tests         | ✅     |
| 7   | All 14 integration tests pass        | ✅     |

---

## Files Changed

| File                                           | Change                           |
| :--------------------------------------------- | :------------------------------- |
| `tests/.../Lexichord.Tests.Integration.csproj` | Added Respawn, Dapper references |
| `tests/.../RAG/Fixtures/PostgresRagFixture.cs` | Added                            |
| `tests/.../RAG/IngestionPipelineTests.cs`      | Added                            |
| `tests/.../RAG/SearchRoundtripTests.cs`        | Added                            |
| `tests/.../RAG/ChangeDetectionTests.cs`        | Added                            |
| `tests/.../RAG/DeletionCascadeTests.cs`        | Added                            |
| `src/.../Indexing/DocumentIndexingPipeline.cs` | Fixed hash update on reindex     |
