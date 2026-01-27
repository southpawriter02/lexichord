# LCS-SBD-048: Scope Breakdown — The Hardening

## Document Control

| Field            | Value                                    |
| :--------------- | :--------------------------------------- |
| **Document ID**  | LCS-SBD-048                              |
| **Version**      | v0.4.8                                   |
| **Codename**     | The Hardening (Performance & Testing)    |
| **Status**       | Draft                                    |
| **Last Updated** | 2026-01-27                               |
| **Owner**        | Lead Architect                           |

---

## 1. Executive Summary

### 1.1 Purpose

v0.4.8 ensures the RAG system is production-ready through comprehensive testing, performance benchmarking, and optimization. This version establishes quality baselines and adds an embedding cache to reduce API costs.

### 1.2 Success Metrics

| Metric | Target |
| :----- | :----- |
| Unit test coverage for RAG module | ≥ 80% |
| Integration tests pass rate | 100% |
| Search latency (10K chunks) | < 200ms |
| Chunking throughput (100KB doc) | < 100ms |
| Cache hit rate for repeated queries | ≥ 90% |

### 1.3 Estimated Effort

| Sub-Part | Description | Hours |
| :------- | :---------- | :---- |
| v0.4.8a | Unit Test Suite | 12 |
| v0.4.8b | Integration Tests | 10 |
| v0.4.8c | Performance Benchmarks | 8 |
| v0.4.8d | Embedding Cache | 10 |
| **Total** | | **40** |

---

## 2. Sub-Part Specifications

### 2.1 v0.4.8a: Unit Test Suite

**Goal:** Comprehensive unit tests for all RAG components.

**Tasks:**

1. Create `ChunkingStrategyTests` for all chunking strategies
2. Create `EmbeddingServiceTests` with mocked HTTP
3. Create `SearchServiceTests` for score calculation and filtering
4. Create `TokenCounterTests` for counting accuracy
5. Create `IngestionServiceTests` for pipeline logic
6. Achieve ≥80% code coverage

**Definition of Done:**

- [ ] All chunking strategies have tests
- [ ] Embedding service tested with mock responses
- [ ] Search scoring logic verified
- [ ] Token counting edge cases covered
- [ ] Coverage report generated

---

### 2.2 v0.4.8b: Integration Tests

**Goal:** Test end-to-end flows in realistic scenarios.

**Tasks:**

1. Create test fixture with PostgreSQL + pgvector
2. Test document ingestion → chunk creation → embedding storage
3. Test search query → embedding → vector search → results
4. Test change detection → re-indexing
5. Test deletion cascade (document → chunks)
6. Test concurrent ingestion

**Definition of Done:**

- [ ] Integration test project configured
- [ ] Test containers running PostgreSQL with pgvector
- [ ] Full ingestion pipeline tested
- [ ] Search roundtrip verified
- [ ] Change detection working
- [ ] Concurrent access safe

---

### 2.3 v0.4.8c: Performance Benchmarks

**Goal:** Establish performance baselines and identify bottlenecks.

**Tasks:**

1. Create BenchmarkDotNet project
2. Benchmark chunking strategies
3. Benchmark token counting
4. Benchmark vector search queries
5. Benchmark full indexing pipeline
6. Document baseline metrics

**Definition of Done:**

- [ ] Benchmark project configured
- [ ] Chunking < 100ms for 100KB
- [ ] Search < 200ms for 10K chunks
- [ ] Indexing > 10 docs/minute
- [ ] Memory < 50MB for 1K docs
- [ ] Baseline report generated

---

### 2.4 v0.4.8d: Embedding Cache

**Goal:** Cache embeddings locally to reduce API costs and latency.

**Tasks:**

1. Design cache schema (SQLite)
2. Implement `IEmbeddingCache` interface
3. Implement `SqliteEmbeddingCache`
4. Key by content hash (SHA-256)
5. Implement LRU eviction
6. Add configuration options
7. Integrate with `EmbeddingService`

**Definition of Done:**

- [ ] Cache stores embeddings by content hash
- [ ] Cache hit returns stored embedding
- [ ] LRU eviction when size limit reached
- [ ] Configuration for enable/disable and max size
- [ ] Cache statistics available
- [ ] Unit tests verify cache behavior

---

## 3. Implementation Checklist

| # | Task | Sub-Part | Status |
| :- | :--- | :------- | :----- |
| 1 | Create ChunkingStrategyTests | v0.4.8a | [ ] |
| 2 | Create EmbeddingServiceTests | v0.4.8a | [ ] |
| 3 | Create SearchServiceTests | v0.4.8a | [ ] |
| 4 | Create TokenCounterTests | v0.4.8a | [ ] |
| 5 | Create IngestionServiceTests | v0.4.8a | [ ] |
| 6 | Generate coverage report | v0.4.8a | [ ] |
| 7 | Configure test containers | v0.4.8b | [ ] |
| 8 | Test ingestion pipeline | v0.4.8b | [ ] |
| 9 | Test search roundtrip | v0.4.8b | [ ] |
| 10 | Test change detection | v0.4.8b | [ ] |
| 11 | Test deletion cascade | v0.4.8b | [ ] |
| 12 | Test concurrent ingestion | v0.4.8b | [ ] |
| 13 | Create benchmark project | v0.4.8c | [ ] |
| 14 | Benchmark chunking | v0.4.8c | [ ] |
| 15 | Benchmark search | v0.4.8c | [ ] |
| 16 | Benchmark indexing | v0.4.8c | [ ] |
| 17 | Document baselines | v0.4.8c | [ ] |
| 18 | Design cache schema | v0.4.8d | [ ] |
| 19 | Implement IEmbeddingCache | v0.4.8d | [ ] |
| 20 | Implement SqliteEmbeddingCache | v0.4.8d | [ ] |
| 21 | Add LRU eviction | v0.4.8d | [ ] |
| 22 | Add configuration | v0.4.8d | [ ] |
| 23 | Integrate with EmbeddingService | v0.4.8d | [ ] |
| 24 | Write cache unit tests | v0.4.8d | [ ] |

---

## 4. Dependency Matrix

### 4.1 Required Interfaces (Upstream)

| Interface | Source | Usage |
| :-------- | :----- | :---- |
| `IChunkingStrategy` | v0.4.3a | Test target |
| `IEmbeddingService` | v0.4.4a | Test target |
| `ISemanticSearchService` | v0.4.5a | Test target |
| `IIngestionService` | v0.4.2a | Test target |
| `IDocumentRepository` | v0.4.1c | Integration tests |
| `IChunkRepository` | v0.4.1c | Integration tests |

### 4.2 New Interfaces Introduced

| Interface | Purpose |
| :-------- | :------ |
| `IEmbeddingCache` | Local embedding storage |

### 4.3 NuGet Packages

| Package | Version | Purpose |
| :------ | :------ | :------ |
| `Microsoft.Data.Sqlite` | 9.x | Cache storage |
| `BenchmarkDotNet` | 0.14.x | Performance benchmarks |
| `Testcontainers.PostgreSql` | 3.x | Integration tests |
| `coverlet.collector` | 6.x | Coverage reporting |

---

## 5. Risks & Mitigations

| Risk | Impact | Mitigation |
| :--- | :----- | :--------- |
| Flaky integration tests | Medium | Use deterministic test data |
| Benchmark variance | Low | Run multiple iterations |
| Cache corruption | Medium | Validate on read, rebuild on error |
| SQLite locking issues | Medium | Use connection pooling |

---

## 6. User Stories

| ID | Role | Story | Priority |
| :- | :--- | :---- | :------- |
| US-048-1 | Developer | Run unit tests for RAG module | Must Have |
| US-048-2 | Developer | Run integration tests against real DB | Must Have |
| US-048-3 | Developer | View performance benchmarks | Should Have |
| US-048-4 | Writer | Benefit from faster repeated searches | Should Have |
| US-048-5 | Writer | Configure embedding cache size | Could Have |

---

## 7. Use Cases

### UC-048-1: Run Unit Tests

**Preconditions:** Development environment set up.

**Flow:**

1. Developer runs `dotnet test --filter Category=Unit`
2. System executes all unit tests
3. System reports pass/fail results
4. System generates coverage report

**Postconditions:** Test results and coverage available.

---

### UC-048-2: Run Integration Tests

**Preconditions:** Docker installed, test containers available.

**Flow:**

1. Developer runs `dotnet test --filter Category=Integration`
2. System starts PostgreSQL container with pgvector
3. System executes integration tests
4. System tears down containers

**Postconditions:** Integration tests verified against real database.

---

### UC-048-3: Use Embedding Cache

**Preconditions:** Cache enabled in settings.

**Flow:**

1. User searches for "project management"
2. System checks cache for query embedding
3. Cache miss → System calls API, stores result
4. User searches same query again
5. Cache hit → System uses cached embedding

**Postconditions:** Subsequent searches faster, no API call.

---

## 8. Unit Testing Requirements

```csharp
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.8a")]
public class FixedSizeChunkingStrategyTests
{
    [Theory]
    [InlineData(100, 0, 10)]  // 100 chars, no overlap, expect 10 chunks
    [InlineData(100, 50, 19)] // 100 chars, 50 overlap, expect more chunks
    public void Split_VariousSettings_ReturnsExpectedChunkCount(
        int chunkSize, int overlap, int expectedMinChunks)
    {
        var strategy = new FixedSizeChunkingStrategy();
        var content = new string('a', 1000);
        var options = new ChunkingOptions
        {
            MaxChunkSize = chunkSize,
            Overlap = overlap
        };

        var chunks = strategy.Split(content, options);

        chunks.Should().HaveCountGreaterOrEqualTo(expectedMinChunks);
    }
}

[Trait("Category", "Integration")]
[Trait("Feature", "v0.4.8b")]
public class IngestionPipelineIntegrationTests : IClassFixture<PostgresFixture>
{
    [Fact]
    public async Task IngestFile_CreatesDocumentAndChunks()
    {
        // Arrange
        var testFile = CreateTestFile("# Test\n\nSome content.");

        // Act
        var result = await _ingestionService.IngestFileAsync(testFile);

        // Assert
        result.Success.Should().BeTrue();

        var doc = await _documentRepo.GetByPathAsync(testFile);
        doc.Should().NotBeNull();

        var chunks = await _chunkRepo.GetByDocumentAsync(doc!.Id);
        chunks.Should().NotBeEmpty();
    }
}
```

---

## 9. Observability & Logging

| Level | Source | Message |
| :---- | :----- | :------ |
| Information | BenchmarkRunner | "Benchmark complete: {Name} = {Duration}ms" |
| Debug | EmbeddingCache | "Cache hit for hash: {Hash}" |
| Debug | EmbeddingCache | "Cache miss for hash: {Hash}" |
| Information | EmbeddingCache | "Cache eviction: removed {Count} entries" |
| Warning | EmbeddingCache | "Cache corruption detected, rebuilding" |

---

## 10. Acceptance Criteria (QA)

| # | Criterion | Status |
| :- | :-------- | :----- |
| 1 | Unit test suite achieves ≥80% coverage | [ ] |
| 2 | All unit tests pass | [ ] |
| 3 | Integration tests pass with real PostgreSQL | [ ] |
| 4 | Chunking benchmark < 100ms for 100KB | [ ] |
| 5 | Search benchmark < 200ms for 10K chunks | [ ] |
| 6 | Embedding cache stores by content hash | [ ] |
| 7 | Cache hit skips API call | [ ] |
| 8 | LRU eviction respects size limit | [ ] |
| 9 | Cache can be disabled via configuration | [ ] |

---

## 11. Verification Commands

```bash
# Run unit tests with coverage
dotnet test tests/Lexichord.Modules.RAG.Tests \
  --filter Category=Unit \
  --collect:"XPlat Code Coverage"

# Run integration tests
dotnet test tests/Lexichord.Modules.RAG.IntegrationTests \
  --filter Category=Integration

# Run benchmarks
dotnet run -c Release --project benchmarks/Lexichord.Benchmarks -- --filter "*RAG*"

# View coverage report
reportgenerator -reports:coverage.cobertura.xml -targetdir:coveragereport
```

---

## 12. Deferred Features

| Feature | Reason | Target Version |
| :------ | :----- | :------------- |
| Distributed cache | Complexity | v0.6.x |
| Cache warming | Low priority | v0.5.x |
| A/B testing framework | Out of scope | v0.7.x |
| Load testing | Requires infrastructure | v0.6.x |

---

## 13. Related Documents

| Document | Relationship |
| :------- | :----------- |
| [LCS-DES-048-INDEX](./LCS-DES-048-INDEX.md) | Design specification index |
| [LCS-SBD-047](../v0.4.7/LCS-SBD-047.md) | Predecessor (Index Manager) |
| [roadmap-v0.4.x](../roadmap-v0.4.x.md) | Version roadmap |

---

## 14. Changelog Entry

```markdown
### v0.4.8 - The Hardening

#### Added
- Comprehensive unit test suite for RAG module (≥80% coverage)
- Integration tests with PostgreSQL test containers
- Performance benchmarks using BenchmarkDotNet
- Local embedding cache using SQLite
- `IEmbeddingCache` interface for cache abstraction
- LRU eviction policy for cache size management
- Cache configuration options (`EmbeddingCacheEnabled`, `EmbeddingCacheMaxSizeMB`)

#### Performance
- Established baseline: Chunking < 100ms for 100KB
- Established baseline: Search < 200ms for 10K chunks
- Established baseline: Indexing > 10 docs/minute
- Cache reduces repeat query latency by ~95%

#### Developer Experience
- Coverage reports generated automatically
- Benchmark reports available
```

---

## 15. Revision History

| Version | Date       | Author         | Changes                    |
| :------ | :--------- | :------------- | :------------------------- |
| 0.1     | 2026-01-27 | Lead Architect | Initial draft              |

---
