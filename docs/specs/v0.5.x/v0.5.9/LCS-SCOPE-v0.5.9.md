# Lexichord Scope Breakdown: v0.5.9 — Semantic Memory Deduplication

**Version:** v0.5.9
**Codename:** The Consolidator
**Theme:** Knowledge deduplication and canonical record management
**Prerequisites:** v0.5.8 (Hardening complete), RAG infrastructure stable

---

## Executive Summary

v0.5.9 introduces **Semantic Memory Deduplication**, a system that identifies semantically equivalent chunks, merges duplicates into canonical records, and detects contradictions for resolution. This reduces storage waste, improves retrieval precision, and eliminates redundant information from search results.

---

## Problem Statement

As knowledge bases grow, redundant information accumulates:
- Same facts stated differently across documents
- Updated information coexisting with outdated versions
- Contradictory statements from different sources
- Slight variations consuming storage and confusing retrieval

**Impact:**
- Wasted storage and compute on duplicate embeddings
- Retrieval noise returning multiple versions of the same info
- Contradictions surfacing conflicting information to users
- Context bloat in RAG pipelines

---

## Feature Breakdown

### v0.5.9a: Similarity Detection Infrastructure
**Goal:** Establish the foundation for detecting potential duplicates during ingestion.

**Deliverables:**
- `ISimilarityDetector` interface for finding similar existing chunks
- Configurable similarity threshold (default: 0.90)
- Batch similarity query optimization using pgvector
- Logging of potential duplicates without automatic action

**Interface:**
```csharp
public interface ISimilarityDetector
{
    Task<IReadOnlyList<SimilarityMatch>> FindSimilarAsync(
        float[] embedding,
        float threshold = 0.90f,
        int maxResults = 5,
        CancellationToken ct = default);
}

public record SimilarityMatch(
    Guid ChunkId,
    float SimilarityScore,
    string ContentPreview);
```

**Schema Changes:** None (uses existing embedding index)

---

### v0.5.9b: Relationship Classification
**Goal:** Determine the relationship type between similar chunks.

**Deliverables:**
- `IRelationshipClassifier` for determining chunk relationships
- LLM-based classification for ambiguous cases
- Rule-based fast-path for high-confidence matches (>0.95)
- Classification result caching

**Relationship Types:**
| Type | Description | Action |
|------|-------------|--------|
| `Equivalent` | Same meaning, different words | Merge to canonical |
| `Complementary` | Related info that adds detail | Link, don't merge |
| `Contradictory` | Conflicting statements | Flag for resolution |
| `Superseding` | Newer version of old info | Replace, archive old |
| `Subset` | One contains the other | Keep superset only |

**Interface:**
```csharp
public interface IRelationshipClassifier
{
    Task<RelationshipClassification> ClassifyAsync(
        Chunk chunkA,
        Chunk chunkB,
        CancellationToken ct = default);
}

public record RelationshipClassification(
    RelationshipType Type,
    float Confidence,
    string? Explanation);
```

---

### v0.5.9c: Canonical Record Management
**Goal:** Create and manage canonical records representing unique facts.

**Deliverables:**
- `canonical_records` table linking to authoritative chunk
- `chunk_variants` table for tracking merged duplicates
- `ICanonicalManager` service for CRUD operations
- Provenance tracking (source documents, timestamps)

**Schema:**
```sql
CREATE TABLE canonical_records (
    id UUID PRIMARY KEY,
    canonical_chunk_id UUID NOT NULL REFERENCES chunks(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    merge_count INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE chunk_variants (
    id UUID PRIMARY KEY,
    canonical_record_id UUID NOT NULL REFERENCES canonical_records(id),
    variant_chunk_id UUID NOT NULL REFERENCES chunks(id),
    relationship_type TEXT NOT NULL,
    similarity_score REAL NOT NULL,
    merged_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(variant_chunk_id)
);

CREATE TABLE chunk_provenance (
    id UUID PRIMARY KEY,
    chunk_id UUID NOT NULL REFERENCES chunks(id),
    source_document_id UUID REFERENCES documents(id),
    source_location TEXT,
    ingested_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    verified_at TIMESTAMPTZ
);
```

**Interface:**
```csharp
public interface ICanonicalManager
{
    Task<CanonicalRecord> CreateCanonicalAsync(Chunk chunk, CancellationToken ct);
    Task MergeIntoCanonicalAsync(Guid canonicalId, Chunk variant, RelationshipType type, float similarity, CancellationToken ct);
    Task<CanonicalRecord?> GetCanonicalForChunkAsync(Guid chunkId, CancellationToken ct);
    Task<IReadOnlyList<Chunk>> GetVariantsAsync(Guid canonicalId, CancellationToken ct);
}
```

---

### v0.5.9d: Deduplication Service
**Goal:** Orchestrate the full deduplication pipeline during ingestion.

**Deliverables:**
- `IDeduplicationService` as the main entry point
- Integration with chunk ingestion pipeline
- Configurable auto-merge threshold (default: 0.95 + LLM confirmation)
- Manual review queue for ambiguous cases

**Interface:**
```csharp
public interface IDeduplicationService
{
    Task<DeduplicationResult> ProcessChunkAsync(
        Chunk newChunk,
        DeduplicationOptions options,
        CancellationToken ct = default);

    Task<IReadOnlyList<DuplicateCandidate>> FindDuplicatesAsync(
        Chunk chunk,
        float similarityThreshold = 0.85f,
        CancellationToken ct = default);
}

public record DeduplicationResult(
    string CanonicalChunkId,
    DeduplicationAction ActionTaken,
    string? MergedFromId,
    IReadOnlyList<string> LinkedChunkIds);

public enum DeduplicationAction
{
    StoredAsNew,
    MergedIntoExisting,
    LinkedToExisting,
    FlaggedAsContradiction,
    QueuedForReview
}

public record DeduplicationOptions(
    float AutoMergeThreshold = 0.95f,
    bool RequireLLMConfirmation = true,
    bool EnableContradictionDetection = true);
```

---

### v0.5.9e: Contradiction Detection & Resolution
**Goal:** Identify and manage conflicting information across documents.

**Deliverables:**
- `contradictions` table for tracking unresolved conflicts
- `IContradictionService` for detection and resolution workflow
- Notification system for flagged contradictions
- Resolution UI in admin panel

**Schema:**
```sql
CREATE TABLE contradictions (
    id UUID PRIMARY KEY,
    chunk_a_id UUID NOT NULL REFERENCES chunks(id),
    chunk_b_id UUID NOT NULL REFERENCES chunks(id),
    conflict_description TEXT NOT NULL,
    status TEXT NOT NULL DEFAULT 'pending',
    resolution_notes TEXT,
    resolved_by TEXT,
    detected_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    resolved_at TIMESTAMPTZ
);
```

**Interface:**
```csharp
public interface IContradictionService
{
    Task<Contradiction> FlagContradictionAsync(Chunk a, Chunk b, string description, CancellationToken ct);
    Task<IReadOnlyList<Contradiction>> GetPendingContradictionsAsync(CancellationToken ct);
    Task ResolveContradictionAsync(Guid contradictionId, ContradictionResolution resolution, CancellationToken ct);
}

public record ContradictionResolution(
    ResolutionAction Action,
    Guid? PreferredChunkId,
    string Notes);

public enum ResolutionAction { KeepBoth, KeepA, KeepB, ArchiveBoth, Merge }
```

---

### v0.5.9f: Retrieval Integration
**Goal:** Modify search to return only canonical records, eliminating duplicates.

**Deliverables:**
- Modified `IChunkRepository.SearchSimilarAsync` to respect canonicals
- Option to include/exclude variants in results
- Provenance display in search results UI
- "Show Variants" expansion in result cards

**Modified Query:**
```sql
SELECT DISTINCT ON (COALESCE(cr.id, c.id))
    c.*,
    cr.id as canonical_record_id,
    (SELECT COUNT(*) FROM chunk_variants cv WHERE cv.canonical_record_id = cr.id) as variant_count
FROM chunks c
LEFT JOIN chunk_variants cv ON cv.variant_chunk_id = c.id
LEFT JOIN canonical_records cr ON cr.id = cv.canonical_record_id OR cr.canonical_chunk_id = c.id
WHERE c.embedding <=> @query_embedding < @threshold
  AND c.id NOT IN (SELECT variant_chunk_id FROM chunk_variants)
ORDER BY COALESCE(cr.id, c.id), c.embedding <=> @query_embedding;
```

---

### v0.5.9g: Batch Retroactive Deduplication
**Goal:** Process existing chunks to find and merge historical duplicates.

**Deliverables:**
- Background job for retroactive deduplication
- Progress tracking and resumability
- Dry-run mode for preview without changes
- Statistics and reporting

**Interface:**
```csharp
public interface IBatchDeduplicationJob
{
    Task<BatchDeduplicationResult> ExecuteAsync(
        BatchDeduplicationOptions options,
        IProgress<BatchProgress>? progress = null,
        CancellationToken ct = default);
}

public record BatchDeduplicationOptions(
    Guid? ProjectId = null,
    bool DryRun = false,
    float SimilarityThreshold = 0.90f,
    int BatchSize = 100);

public record BatchDeduplicationResult(
    int ChunksProcessed,
    int DuplicatesFound,
    int MergedCount,
    int ContradictionsFound,
    TimeSpan Duration);
```

---

### v0.5.9h: Hardening & Metrics
**Goal:** Ensure production readiness with testing and observability.

**Deliverables:**
- Unit tests for all deduplication components
- Integration tests with realistic corpus
- Performance benchmarks (target: <50ms overhead per chunk)
- Metrics dashboard (dedup rate, storage savings, contradiction rate)

**Success Metrics:**
| Metric | Target |
|--------|--------|
| Deduplication Rate | 10-30% of incoming chunks merged |
| Storage Savings | 15-25% reduction in total chunks |
| Retrieval Precision | <5% redundant results |
| False Positive Rate | <1% incorrectly merged |
| Processing Overhead | <50ms per chunk |

---

## Dependencies

| Component | Source Version | Usage |
|-----------|----------------|-------|
| `IChunkRepository` | v0.4.1c | Chunk storage and retrieval |
| `IEmbeddingService` | v0.4.4a | Similarity computation |
| `IChatCompletionService` | v0.6.1a | LLM classification (optional) |
| `IMediator` | v0.0.7a | Event publishing |
| pgvector | v0.4.1a | Vector similarity search |

---

## MediatR Events

| Event | Description |
|-------|-------------|
| `ChunkDeduplicatedEvent` | Chunk merged into existing canonical |
| `CanonicalRecordCreatedEvent` | New canonical record established |
| `ContradictionDetectedEvent` | Conflicting information found |
| `ContradictionResolvedEvent` | Contradiction manually resolved |
| `BatchDeduplicationCompletedEvent` | Retroactive job finished |

---

## License Gating

| Feature | Core | WriterPro | Teams | Enterprise |
|---------|------|-----------|-------|------------|
| Basic deduplication | — | ✓ | ✓ | ✓ |
| Contradiction detection | — | ✓ | ✓ | ✓ |
| Batch retroactive dedup | — | — | ✓ | ✓ |
| Deduplication analytics | — | — | ✓ | ✓ |

---

## Migration Path

1. **v0.5.9a-b**: Deploy similarity detection (non-breaking, logging only)
2. **v0.5.9c**: Run schema migration for canonical tables
3. **v0.5.9d-e**: Enable deduplication service with conservative thresholds
4. **v0.5.9f**: Switch retrieval to canonical-aware queries
5. **v0.5.9g**: Run batch job on existing data (off-peak hours)
6. **v0.5.9h**: Monitor metrics, tune thresholds

---

## Open Questions

1. **Cross-project deduplication**: Should dedup operate within project or globally?
2. **User override**: Can users force-keep duplicates for specific use cases?
3. **Update semantics**: When source doc updates, how to handle existing canonicals?
4. **LLM cost**: Is per-chunk LLM classification affordable, or batch/sample?

---

## Timeline Estimate

| Phase | Sub-versions | Relative Effort |
|-------|--------------|-----------------|
| Infrastructure | v0.5.9a-b | 25% |
| Core Logic | v0.5.9c-e | 40% |
| Integration | v0.5.9f-g | 25% |
| Hardening | v0.5.9h | 10% |
