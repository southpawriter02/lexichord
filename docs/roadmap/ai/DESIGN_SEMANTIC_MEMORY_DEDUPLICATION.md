# Design Proposal: Semantic Memory Deduplication

A knowledge management system that identifies semantically equivalent information, merges duplicates, and maintains a single source of truth.

---

## Problem Statement

Knowledge bases accumulate redundant information over time:

- Same fact stated differently across documents
- Updated information coexisting with outdated versions
- Contradictory statements from different sources
- Slight variations consuming storage and confusing retrieval

This leads to:
- **Wasted storage and compute** on duplicate embeddings
- **Retrieval noise** returning multiple versions of the same info
- **Contradictions** surfacing conflicting information to users
- **Context bloat** in RAG pipelines

---

## Core Concepts

### Semantic Equivalence Classes

Instead of treating each chunk as independent, group chunks into equivalence classes:

```
┌─────────────────────────────────────────────────────────────┐
│                    Equivalence Class                         │
├─────────────────────────────────────────────────────────────┤
│  Canonical Record (single source of truth)                  │
│  ├── "The API rate limit is 100 requests per minute"        │
│                                                              │
│  Variants (linked, not stored separately for retrieval)     │
│  ├── "Rate limiting: 100 req/min"                           │
│  ├── "You can make up to 100 API calls each minute"         │
│  └── "API throttling set to 100/min"                        │
│                                                              │
│  Provenance                                                  │
│  ├── Source: api_docs.md (line 47)                          │
│  ├── Source: faq.md (line 123)                              │
│  └── Last verified: 2026-01-15                              │
└─────────────────────────────────────────────────────────────┘
```

### Relationship Types

Not all similar content is duplicate. The system must distinguish:

| Relationship | Description | Action |
|--------------|-------------|--------|
| **Equivalent** | Same meaning, different words | Merge to canonical |
| **Complementary** | Related info that adds detail | Link, don't merge |
| **Contradictory** | Conflicting statements | Flag for resolution |
| **Superseding** | Newer version of old info | Replace, archive old |
| **Subset** | One contains the other | Keep superset only |

### Canonical Record Selection

When merging equivalents, choose the canonical based on:

1. **Recency**: Newer sources preferred
2. **Authority**: Primary docs over secondary
3. **Completeness**: More detailed version preferred
4. **Clarity**: Better-written version preferred

---

## Proposed Architecture

### Components

```
┌─────────────────────────────────────────────────────────────┐
│                 Deduplication Pipeline                       │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌─────────────────┐    ┌─────────────────┐                 │
│  │  Ingestion Hook │───▶│ Similarity      │                 │
│  │                 │    │ Detector        │                 │
│  │ Intercepts new  │    │                 │                 │
│  │ chunks before   │    │ Finds potential │                 │
│  │ storage         │    │ duplicates      │                 │
│  └─────────────────┘    └────────┬────────┘                 │
│                                  │                           │
│                                  ▼                           │
│  ┌─────────────────┐    ┌─────────────────┐                 │
│  │ Canonical       │◀───│ Relationship    │                 │
│  │ Manager         │    │ Classifier      │                 │
│  │                 │    │                 │                 │
│  │ Merges, links,  │    │ LLM determines  │                 │
│  │ manages records │    │ equiv/contra/   │                 │
│  │                 │    │ complement      │                 │
│  └─────────────────┘    └─────────────────┘                 │
│                                                              │
│  ┌─────────────────┐    ┌─────────────────┐                 │
│  │ Contradiction   │    │ Provenance      │                 │
│  │ Resolver        │    │ Tracker         │                 │
│  │                 │    │                 │                 │
│  │ Flags/resolves  │    │ Maintains       │                 │
│  │ conflicts       │    │ source links    │                 │
│  └─────────────────┘    └─────────────────┘                 │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### Data Flow

```
New Chunk Ingested
        │
        ▼
┌───────────────────┐
│ Generate Embedding │
└─────────┬─────────┘
          │
          ▼
┌───────────────────┐     ┌─────────────────────┐
│ Query for Similar │────▶│ Candidates Found?   │
│ Existing Chunks   │     │                     │
└───────────────────┘     └──────────┬──────────┘
                                     │
                    ┌────────────────┼────────────────┐
                    │ No             │ Yes            │
                    ▼                ▼                │
          ┌─────────────┐   ┌─────────────────┐      │
          │ Store as    │   │ Classify        │      │
          │ New Record  │   │ Relationship    │      │
          └─────────────┘   └────────┬────────┘      │
                                     │               │
                    ┌────────────────┼───────────────┤
                    │                │               │
            Equivalent        Complementary    Contradictory
                    │                │               │
                    ▼                ▼               ▼
          ┌─────────────┐   ┌─────────────┐  ┌─────────────┐
          │ Merge into  │   │ Link to     │  │ Flag for    │
          │ Canonical   │   │ Existing    │  │ Resolution  │
          └─────────────┘   └─────────────┘  └─────────────┘
```

---

## Interface Design

### Core Interface

```csharp
public interface IDeduplicationService
{
    /// <summary>
    /// Process a new chunk, deduplicating if necessary.
    /// Returns the canonical chunk ID (may be existing or new).
    /// </summary>
    Task<DeduplicationResult> ProcessChunkAsync(
        Chunk newChunk,
        DeduplicationOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Find potential duplicates of a chunk without processing.
    /// </summary>
    Task<IReadOnlyList<DuplicateCandidate>> FindDuplicatesAsync(
        Chunk chunk,
        float similarityThreshold = 0.85f,
        CancellationToken ct = default);

    /// <summary>
    /// Get all contradictions pending resolution.
    /// </summary>
    Task<IReadOnlyList<Contradiction>> GetPendingContradictionsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Resolve a contradiction by choosing which version to keep.
    /// </summary>
    Task ResolveContradictionAsync(
        string contradictionId,
        ContradictionResolution resolution,
        CancellationToken ct = default);
}
```

### Supporting Types

```csharp
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
    FlaggedAsContradiction
}

public record DuplicateCandidate(
    string ChunkId,
    float SimilarityScore,
    RelationshipType PredictedRelationship,
    string ContentPreview);

public enum RelationshipType
{
    Equivalent,
    Complementary,
    Contradictory,
    Superseding,
    Subset
}

public record Contradiction(
    string ContradictionId,
    Chunk ChunkA,
    Chunk ChunkB,
    string ConflictDescription,
    DateTimeOffset DetectedAt);
```

---

## Schema Extensions for Lexichord

### New Tables

```sql
-- Tracks equivalence classes
CREATE TABLE canonical_records (
    id UUID PRIMARY KEY,
    canonical_chunk_id UUID NOT NULL REFERENCES chunks(id),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Links variant chunks to their canonical
CREATE TABLE chunk_variants (
    id UUID PRIMARY KEY,
    canonical_record_id UUID NOT NULL REFERENCES canonical_records(id),
    variant_chunk_id UUID NOT NULL REFERENCES chunks(id),
    relationship_type TEXT NOT NULL, -- 'equivalent', 'complementary', 'subset'
    similarity_score REAL NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(variant_chunk_id)
);

-- Tracks unresolved contradictions
CREATE TABLE contradictions (
    id UUID PRIMARY KEY,
    chunk_a_id UUID NOT NULL REFERENCES chunks(id),
    chunk_b_id UUID NOT NULL REFERENCES chunks(id),
    conflict_description TEXT NOT NULL,
    status TEXT NOT NULL DEFAULT 'pending', -- 'pending', 'resolved', 'ignored'
    resolution_notes TEXT,
    detected_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    resolved_at TIMESTAMPTZ
);

-- Provenance tracking
CREATE TABLE chunk_provenance (
    id UUID PRIMARY KEY,
    chunk_id UUID NOT NULL REFERENCES chunks(id),
    source_document_id UUID REFERENCES documents(id),
    source_location TEXT, -- line number, section, etc.
    ingested_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    verified_at TIMESTAMPTZ
);
```

### Modified Retrieval Query

Instead of retrieving all matching chunks, retrieve only canonicals:

```sql
-- Before: returns duplicates
SELECT * FROM chunks 
WHERE embedding <=> $query_embedding < 0.3;

-- After: returns unique canonical records
SELECT DISTINCT ON (COALESCE(cr.id, c.id))
    c.*,
    cr.id as canonical_record_id
FROM chunks c
LEFT JOIN chunk_variants cv ON cv.variant_chunk_id = c.id
LEFT JOIN canonical_records cr ON cr.id = cv.canonical_record_id
WHERE c.embedding <=> $query_embedding < 0.3
ORDER BY COALESCE(cr.id, c.id), c.embedding <=> $query_embedding;
```

---

## Implementation Phases

### Phase 1: Similarity Detection
- Add embedding similarity check during ingestion
- Flag potential duplicates above threshold (e.g., 0.90)
- Log but don't automatically merge

### Phase 2: Relationship Classification
- Implement LLM-based classifier for relationship types
- Auto-merge clear equivalents (>0.95 similarity + LLM confirms)
- Queue ambiguous cases for review

### Phase 3: Canonical Management
- Create canonical record structure
- Modify retrieval to respect canonicals
- Implement provenance tracking

### Phase 4: Contradiction Handling
- Build contradiction detection
- Create resolution UI/workflow
- Implement superseding logic for updates

### Phase 5: Batch Processing
- Implement retroactive deduplication for existing data
- Background job for periodic re-analysis
- Metrics and reporting

---

## Integration Strategy for Lexichord

### Ingestion Pipeline Modification

```
Current:
Document → Chunker → Embedder → Store in chunks table

With Deduplication:
Document → Chunker → Embedder → DeduplicationService → Store/Merge/Link
```

### Minimal Initial Integration

Start with a simple wrapper around `IChunkRepository`:

```csharp
public class DeduplicatingChunkRepository : IChunkRepository
{
    private readonly IChunkRepository _inner;
    private readonly IDeduplicationService _dedup;

    public async Task<Chunk> AddAsync(Chunk chunk, CancellationToken ct)
    {
        var result = await _dedup.ProcessChunkAsync(chunk, ct);
        
        if (result.ActionTaken == DeduplicationAction.StoredAsNew)
        {
            return await _inner.AddAsync(chunk, ct);
        }
        
        // Return existing canonical chunk
        return await _inner.GetByIdAsync(result.CanonicalChunkId, ct);
    }
}
```

---

## Open Questions

1. **Similarity Threshold**: What embedding distance qualifies as "potential duplicate"?
2. **LLM Cost**: Is per-chunk LLM classification affordable, or batch/sample?
3. **User Control**: Should users be able to force-keep duplicates?
4. **Cross-Document**: Deduplicate across all docs or within document scope?
5. **Update Semantics**: When source doc updates, how to handle existing canonicals?

---

## Success Metrics

- **Deduplication Rate**: % of incoming chunks merged vs. stored new
- **Storage Savings**: Reduction in total chunks stored
- **Retrieval Precision**: Fewer redundant results in search
- **Contradiction Detection**: % of conflicts caught automatically
- **False Positive Rate**: Incorrectly merged distinct information
