# Lexichord Scope Breakdown: v0.8.9 — Persistent Agent Memory Fabric

**Version:** v0.8.9
**Codename:** The Rememberer
**Theme:** Cross-session memory with temporal awareness and continuous learning
**Prerequisites:** v0.8.8 (Publisher hardened), v0.5.9 (Semantic Deduplication), v0.7.9 (Contextual Compression)

---

## Executive Summary

v0.8.9 introduces the **Persistent Agent Memory Fabric**, a cross-session memory layer enabling AI agents to build, retain, and refine personal knowledge over time. Inspired by cognitive science, this system supports distinct memory types (semantic, episodic, procedural), temporal awareness, salience-based retrieval, and background memory consolidation.

---

## Problem Statement

Current AI agents are largely stateless between sessions:
- Each conversation starts fresh with no memory of past interactions
- Users must re-explain preferences, context, and project details
- Agents can't learn from past successes or mistakes
- No concept of "when" or "why" something was learned

**Impact:**
- Repetitive context-setting wastes user time
- Agents make the same mistakes across sessions
- No improvement in agent performance over time
- Lost opportunity for personalization and adaptation

---

## Core Concepts

### Memory Types

Inspired by cognitive science, the fabric supports distinct memory categories:

```
┌─────────────────────────────────────────────────────────────┐
│                      Memory Fabric                           │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌─────────────────┐  ┌─────────────────┐                   │
│  │ SEMANTIC        │  │ EPISODIC        │                   │
│  │ MEMORY          │  │ MEMORY          │                   │
│  │                 │  │                 │                   │
│  │ Facts & concepts│  │ Specific events │                   │
│  │ "User prefers   │  │ "On Jan 15, we  │                   │
│  │  dark mode"     │  │  debugged the   │                   │
│  │                 │  │  auth module"   │                   │
│  └─────────────────┘  └─────────────────┘                   │
│                                                              │
│  ┌─────────────────┐  ┌─────────────────┐                   │
│  │ PROCEDURAL      │  │ WORKING         │                   │
│  │ MEMORY          │  │ MEMORY          │                   │
│  │                 │  │                 │                   │
│  │ How to do things│  │ Current session │                   │
│  │ "To deploy,     │  │ context (temp)  │                   │
│  │  run make prod" │  │                 │                   │
│  └─────────────────┘  └─────────────────┘                   │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

| Type | Contains | Retention | Example |
|------|----------|-----------|---------|
| **Semantic** | Facts, preferences, concepts | Long-term | "The project uses PostgreSQL" |
| **Episodic** | Specific events, conversations | Long-term | "Last week we refactored auth" |
| **Procedural** | Skills, workflows, how-tos | Long-term | "To run tests: npm test" |
| **Working** | Current session context | Session only | Active task state |

### Temporal Indexing

Every memory has temporal metadata enabling queries like:
- "What did we discuss last week?"
- "What's changed since I last worked on this?"
- "What am I most confident about regarding X?"

### Memory Salience

Not all memories are equal. Salience scoring determines retrieval priority:

```
salience = f(recency, frequency, importance, relevance)

Where:
- recency: Time decay since last access
- frequency: How often accessed
- importance: Explicitly marked or inferred significance
- relevance: Embedding similarity to current context
```

---

## Feature Breakdown

### v0.8.9a: Memory Data Model
**Goal:** Define the core data structures for the memory fabric.

**Deliverables:**
- `MemoryType` enum (Semantic, Episodic, Procedural, Working)
- `Memory` record with content, embedding, and metadata
- `TemporalMetadata` record for time-based tracking
- `ProvenanceInfo` record for source attribution
- `MemoryLink` record for inter-memory relationships

**Interface:**
```csharp
public enum MemoryType
{
    Semantic,   // Facts and concepts
    Episodic,   // Specific events
    Procedural, // How to do things
    Working     // Current session (not persisted long-term)
}

public record Memory(
    string Id,
    MemoryType Type,
    string Content,
    float[] Embedding,
    TemporalMetadata Temporal,
    ProvenanceInfo Provenance,
    float CurrentSalience,
    IReadOnlyList<string> LinkedMemoryIds);

public record TemporalMetadata(
    DateTimeOffset CreatedAt,
    DateTimeOffset LastAccessed,
    int AccessCount,
    DateTimeOffset? LastReinforced,
    IReadOnlyList<float> ConfidenceTrajectory);

public record ProvenanceInfo(
    string? SourceConversationId,
    string? LearningContext,
    string? SourceDocumentId);

public record MemoryLink(
    string FromMemoryId,
    string ToMemoryId,
    MemoryLinkType LinkType,
    float Strength);

public enum MemoryLinkType
{
    Related,
    DerivedFrom,
    Contradicts,
    Supersedes,
    Reinforces
}
```

---

### v0.8.9b: Memory Storage Schema
**Goal:** Create persistent storage for memories with efficient retrieval.

**Deliverables:**
- `memories` table with vector embeddings
- `memory_confidence_history` for trajectory tracking
- `memory_links` for relationship graph
- Indexes for temporal, salience, and vector queries

**Schema:**
```sql
CREATE TABLE memories (
    id UUID PRIMARY KEY,
    memory_type TEXT NOT NULL, -- 'semantic', 'episodic', 'procedural'
    content TEXT NOT NULL,
    embedding VECTOR(1536) NOT NULL,

    -- Temporal metadata
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_accessed_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    access_count INTEGER NOT NULL DEFAULT 0,
    last_reinforced_at TIMESTAMPTZ,

    -- Salience
    current_salience REAL NOT NULL DEFAULT 0.5,
    importance_score REAL NOT NULL DEFAULT 0.5,

    -- Provenance
    source_conversation_id TEXT,
    learning_context TEXT,
    source_document_id UUID,

    -- Status
    status TEXT NOT NULL DEFAULT 'active', -- 'active', 'archived', 'superseded'
    superseded_by UUID REFERENCES memories(id),

    -- Scoping
    project_id UUID,
    user_id TEXT NOT NULL
);

CREATE TABLE memory_confidence_history (
    id UUID PRIMARY KEY,
    memory_id UUID NOT NULL REFERENCES memories(id) ON DELETE CASCADE,
    confidence REAL NOT NULL,
    recorded_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE memory_links (
    id UUID PRIMARY KEY,
    from_memory_id UUID NOT NULL REFERENCES memories(id) ON DELETE CASCADE,
    to_memory_id UUID NOT NULL REFERENCES memories(id) ON DELETE CASCADE,
    link_type TEXT NOT NULL,
    strength REAL NOT NULL DEFAULT 0.5,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(from_memory_id, to_memory_id, link_type)
);

-- Indexes
CREATE INDEX memories_embedding_idx ON memories
    USING ivfflat (embedding vector_cosine_ops);

CREATE INDEX memories_temporal_idx ON memories
    (created_at, last_accessed_at);

CREATE INDEX memories_salience_idx ON memories
    (current_salience DESC) WHERE status = 'active';

CREATE INDEX memories_type_idx ON memories
    (memory_type, user_id) WHERE status = 'active';

CREATE INDEX memories_user_project_idx ON memories
    (user_id, project_id) WHERE status = 'active';
```

**Interface:**
```csharp
public interface IMemoryStore
{
    Task<Memory> StoreAsync(Memory memory, CancellationToken ct);
    Task<Memory?> GetByIdAsync(string memoryId, CancellationToken ct);
    Task<IReadOnlyList<Memory>> GetByTypeAsync(
        MemoryType type,
        string userId,
        int limit = 50,
        CancellationToken ct = default);
    Task UpdateSalienceAsync(string memoryId, float newSalience, CancellationToken ct);
    Task RecordAccessAsync(string memoryId, CancellationToken ct);
    Task ArchiveAsync(string memoryId, CancellationToken ct);
    Task SupersedeAsync(string memoryId, string replacementId, CancellationToken ct);
}
```

---

### v0.8.9c: Memory Encoder
**Goal:** Process incoming information into structured memories.

**Deliverables:**
- `IMemoryEncoder` for creating memories from content
- Automatic memory type classification
- Embedding generation for semantic search
- Provenance extraction from context

**Interface:**
```csharp
public interface IMemoryEncoder
{
    Task<Memory> EncodeAsync(
        string content,
        MemoryContext context,
        CancellationToken ct = default);

    Task<MemoryType> ClassifyTypeAsync(
        string content,
        CancellationToken ct = default);
}

public record MemoryContext(
    string UserId,
    string? ConversationId,
    string? ProjectId,
    string? SourceDocumentId,
    string? LearningContext);
```

**Memory Type Classification Rules:**
```csharp
private static readonly Dictionary<MemoryType, string[]> TypeIndicators = new()
{
    [MemoryType.Semantic] = new[]
    {
        "is", "are", "uses", "prefers", "requires", "the", "always", "never"
    },
    [MemoryType.Episodic] = new[]
    {
        "yesterday", "last week", "on", "when we", "that time", "remember when"
    },
    [MemoryType.Procedural] = new[]
    {
        "to do", "how to", "steps to", "run", "execute", "first", "then", "finally"
    }
};
```

---

### v0.8.9d: Salience Calculator
**Goal:** Compute memory salience for retrieval prioritization.

**Deliverables:**
- `ISalienceCalculator` for multi-factor scoring
- Configurable weights for each factor
- Time decay functions for recency
- Importance inference from content

**Interface:**
```csharp
public interface ISalienceCalculator
{
    float CalculateSalience(Memory memory, SalienceContext context);
    Task UpdateSalienceAsync(string memoryId, CancellationToken ct);
    Task DecayAllSalienceAsync(TimeSpan timePeriod, CancellationToken ct);
}

public record SalienceContext(
    string? CurrentQuery,
    float[]? QueryEmbedding,
    DateTimeOffset CurrentTime);

public record SalienceWeights(
    float RecencyWeight = 0.25f,
    float FrequencyWeight = 0.20f,
    float ImportanceWeight = 0.25f,
    float RelevanceWeight = 0.30f);
```

**Salience Formula:**
```csharp
public float CalculateSalience(Memory memory, SalienceContext context)
{
    var recency = CalculateRecencyScore(memory.Temporal.LastAccessed, context.CurrentTime);
    var frequency = CalculateFrequencyScore(memory.Temporal.AccessCount);
    var importance = memory.Temporal.ConfidenceTrajectory.LastOrDefault()
                     ?? memory.CurrentSalience;
    var relevance = context.QueryEmbedding != null
        ? CosineSimilarity(memory.Embedding, context.QueryEmbedding)
        : 0.5f;

    return _weights.RecencyWeight * recency
         + _weights.FrequencyWeight * frequency
         + _weights.ImportanceWeight * importance
         + _weights.RelevanceWeight * relevance;
}

private float CalculateRecencyScore(DateTimeOffset lastAccess, DateTimeOffset now)
{
    var daysSinceAccess = (now - lastAccess).TotalDays;
    // Exponential decay: half-life of 7 days
    return (float)Math.Exp(-daysSinceAccess / 7.0);
}

private float CalculateFrequencyScore(int accessCount)
{
    // Logarithmic scaling to prevent runaway frequency
    return (float)Math.Min(1.0, Math.Log(accessCount + 1) / Math.Log(100));
}
```

---

### v0.8.9e: Memory Retriever
**Goal:** Recall relevant memories using multiple retrieval strategies.

**Deliverables:**
- `IMemoryRetriever` for multi-modal recall
- Semantic similarity search
- Temporal range queries
- Type-filtered retrieval
- Salience-weighted ranking

**Interface:**
```csharp
public interface IMemoryRetriever
{
    Task<IReadOnlyList<Memory>> RecallAsync(
        string query,
        RecallOptions options,
        CancellationToken ct = default);

    Task<IReadOnlyList<Memory>> RecallTemporalAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        string? topicFilter = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<Memory>> RecallByTypeAsync(
        MemoryType type,
        int limit = 10,
        CancellationToken ct = default);
}

public record RecallOptions(
    string UserId,
    int MaxResults = 10,
    MemoryType? TypeFilter = null,
    float MinSalience = 0.0f,
    RecallMode Mode = RecallMode.Relevant,
    string? ProjectId = null);

public enum RecallMode
{
    Relevant,    // Most relevant to query (embedding similarity + salience)
    Recent,      // Most recently accessed
    Important,   // Highest salience
    Temporal,    // Within time range
    Frequent     // Most frequently accessed
}
```

**Retrieval Query (Relevant Mode):**
```sql
WITH salience_adjusted AS (
    SELECT
        m.*,
        (1 - (m.embedding <=> @query_embedding)) * 0.6 +
        m.current_salience * 0.4 AS combined_score
    FROM memories m
    WHERE m.user_id = @user_id
      AND m.status = 'active'
      AND (@type_filter IS NULL OR m.memory_type = @type_filter)
      AND (@project_id IS NULL OR m.project_id = @project_id)
      AND m.current_salience >= @min_salience
)
SELECT * FROM salience_adjusted
ORDER BY combined_score DESC
LIMIT @max_results;
```

---

### v0.8.9f: Memory Fabric Service
**Goal:** Orchestrate the complete memory lifecycle.

**Deliverables:**
- `IMemoryFabric` as the main API
- Memory creation with automatic encoding
- Reinforcement and correction workflows
- Integration with existing services

**Interface:**
```csharp
public interface IMemoryFabric
{
    /// <summary>
    /// Store a new memory, automatically classifying type.
    /// </summary>
    Task<Memory> RememberAsync(
        string content,
        MemoryContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Recall memories relevant to a query.
    /// </summary>
    Task<IReadOnlyList<Memory>> RecallAsync(
        string query,
        RecallOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Recall memories from a specific time period.
    /// </summary>
    Task<IReadOnlyList<Memory>> RecallTemporalAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        string? topicFilter = null,
        string? userId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Reinforce a memory (increase salience).
    /// </summary>
    Task ReinforceMemoryAsync(
        string memoryId,
        ReinforcementReason reason,
        CancellationToken ct = default);

    /// <summary>
    /// Correct or update a memory.
    /// </summary>
    Task<Memory> UpdateMemoryAsync(
        string memoryId,
        string correctedContent,
        string correctionReason,
        CancellationToken ct = default);

    /// <summary>
    /// Link two related memories.
    /// </summary>
    Task LinkMemoriesAsync(
        string fromMemoryId,
        string toMemoryId,
        MemoryLinkType linkType,
        float strength = 0.5f,
        CancellationToken ct = default);
}

public enum ReinforcementReason
{
    ExplicitUserConfirmation,
    SuccessfulApplication,
    RepeatedAccess,
    UserCorrection
}
```

---

### v0.8.9g: Memory Consolidator
**Goal:** Background process for memory organization and optimization.

**Deliverables:**
- `IMemoryConsolidator` for batch processing
- Pattern extraction from episodic to semantic
- Salience decay and pruning
- Memory deduplication (uses v0.5.9)
- Contradiction detection

**Interface:**
```csharp
public interface IMemoryConsolidator
{
    Task ConsolidateAsync(
        ConsolidationOptions options,
        CancellationToken ct = default);

    Task<ConsolidationReport> RunCycleAsync(
        string userId,
        CancellationToken ct = default);
}

public record ConsolidationOptions(
    string? UserId = null,
    TimeSpan? MaxAge = null,
    bool ExtractPatterns = true,
    bool DecaySalience = true,
    bool PruneArchived = true,
    bool DetectContradictions = true);

public record ConsolidationReport(
    int MemoriesProcessed,
    int PatternsExtracted,
    int MemoriesArchived,
    int ContradictionsFound,
    int DuplicatesMerged,
    TimeSpan Duration);
```

**Consolidation Cycle:**
```
┌─────────────────────────────────────────────────────────────┐
│                   Consolidation Cycle                        │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  1. REPLAY                                                   │
│     Review recent episodic memories                          │
│     Identify patterns and recurring themes                   │
│                                                              │
│  2. EXTRACT                                                  │
│     Convert patterns to semantic memories                    │
│     "I noticed we always run tests before deploying"         │
│     → Create procedural memory about deployment workflow     │
│                                                              │
│  3. STRENGTHEN                                               │
│     Boost salience of frequently-accessed memories           │
│     Reinforce memories that led to successful outcomes       │
│                                                              │
│  4. LINK                                                     │
│     Connect related memories                                 │
│     Build knowledge graph structure                          │
│                                                              │
│  5. PRUNE                                                    │
│     Archive low-salience memories                            │
│     Merge redundant semantic memories (uses v0.5.9)          │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

---

### v0.8.9h: Memory Capture Integration
**Goal:** Integrate memory creation into existing Lexichord workflows.

**Deliverables:**
- Conversation analysis for memory extraction
- User correction capture
- Configuration change tracking
- Workflow observation for procedural memories

**Memory Capture Points:**
```csharp
public interface IMemoryCaptureService
{
    Task CaptureFromConversationAsync(
        ConversationTurn turn,
        CancellationToken ct = default);

    Task CaptureUserCorrectionAsync(
        string originalContent,
        string correctedContent,
        string context,
        CancellationToken ct = default);

    Task CaptureConfigurationChangeAsync(
        string settingKey,
        object oldValue,
        object newValue,
        CancellationToken ct = default);

    Task CaptureWorkflowPatternAsync(
        IReadOnlyList<UserAction> actions,
        CancellationToken ct = default);
}
```

**Capture Triggers:**
| Trigger | Memory Type | Example |
|---------|-------------|---------|
| User correction | Semantic | "Actually, the API uses OAuth, not API keys" |
| Successful search confirmation | Reinforcement | User confirms result was helpful |
| Configuration change | Semantic | User sets preferences |
| Conversation milestone | Episodic | Major decision made, task completed |
| Repeated action pattern | Procedural | Workflow observation |

---

### v0.8.9i: Hardening & Metrics
**Goal:** Ensure production readiness with testing and observability.

**Deliverables:**
- Unit tests for all memory components
- Integration tests with realistic scenarios
- Performance benchmarks
- Consolidation quality tests

**Success Metrics:**
| Metric | Target |
|--------|--------|
| Memory Recall Precision | >0.85 |
| Salience Ranking Accuracy | >0.80 |
| Consolidation Pattern Accuracy | >0.75 |
| Memory Store Latency (read) | <50ms |
| Memory Store Latency (write) | <100ms |
| Consolidation Cycle Duration | <30s per 1000 memories |
| Storage per Memory | <2KB average |

**Fidelity Testing:**
```csharp
public interface IMemoryFidelityEvaluator
{
    Task<FidelityScore> EvaluateRecallAsync(
        string query,
        IReadOnlyList<Memory> expectedMemories,
        IReadOnlyList<Memory> actualMemories,
        CancellationToken ct = default);

    Task<float> EvaluateConsolidationQualityAsync(
        IReadOnlyList<Memory> episodicMemories,
        IReadOnlyList<Memory> extractedSemanticMemories,
        CancellationToken ct = default);
}

public record FidelityScore(
    float Precision,
    float Recall,
    float F1Score,
    float SalienceCorrelation);
```

---

## Dependencies

| Component | Source Version | Usage |
|-----------|----------------|-------|
| `IEmbeddingService` | v0.4.4a | Memory embedding generation |
| `IChatCompletionService` | v0.6.1a | Memory type classification, pattern extraction |
| `IDeduplicationService` | v0.5.9d | Consolidation deduplication |
| `IContextCompressor` | v0.7.9d | Working memory compression |
| `IChunkRepository` | v0.4.1c | Vector storage patterns |
| `IMediator` | v0.0.7a | Event publishing |
| pgvector | v0.4.1a | Vector similarity search |

---

## MediatR Events

| Event | Description |
|-------|-------------|
| `MemoryCreatedEvent` | New memory stored |
| `MemoryReinforcedEvent` | Memory salience increased |
| `MemoryUpdatedEvent` | Memory content corrected |
| `MemoryArchivedEvent` | Memory moved to archive |
| `MemorySupersededEvent` | Memory replaced by newer version |
| `MemoriesLinkedEvent` | Relationship created between memories |
| `ConsolidationCompletedEvent` | Background consolidation finished |
| `PatternExtractedEvent` | Semantic memory created from episodic patterns |
| `MemoryContradictionDetectedEvent` | Conflicting memories found |

---

## License Gating

| Feature | Core | WriterPro | Teams | Enterprise |
|---------|------|-----------|-------|------------|
| Basic memory storage | — | ✓ | ✓ | ✓ |
| Memory recall (semantic) | — | ✓ | ✓ | ✓ |
| Temporal queries | — | ✓ | ✓ | ✓ |
| Memory reinforcement | — | ✓ | ✓ | ✓ |
| Automatic consolidation | — | — | ✓ | ✓ |
| Pattern extraction | — | — | ✓ | ✓ |
| Cross-project memories | — | — | — | ✓ |
| Memory analytics | — | — | — | ✓ |

---

## Integration with Existing Systems

### Parallel System Architecture

The Memory Fabric operates alongside Lexichord's RAG, not replacing it:

```
┌─────────────────────────────────────────────────────────────┐
│                     Lexichord Application                    │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌─────────────────────┐    ┌─────────────────────┐         │
│  │  RAG System         │    │  Memory Fabric      │         │
│  │  (existing)         │    │  (new)              │         │
│  │                     │    │                     │         │
│  │  Document chunks    │    │  Agent memories     │         │
│  │  BM25 + vector      │    │  User preferences   │         │
│  │  Citation engine    │    │  Learned patterns   │         │
│  │  Snippets           │    │  Past interactions  │         │
│  └──────────┬──────────┘    └──────────┬──────────┘         │
│             │                          │                     │
│             └────────────┬─────────────┘                     │
│                          ▼                                   │
│             ┌─────────────────────────┐                      │
│             │  Unified Context        │                      │
│             │  Assembler (v0.7.2)     │                      │
│             │                         │                      │
│             │  Combines document      │                      │
│             │  knowledge + agent      │                      │
│             │  memory for context     │                      │
│             └─────────────────────────┘                      │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### Shared Infrastructure

Both systems share:
- PostgreSQL database (separate tables)
- pgvector for embeddings
- Common embedding model
- Context Assembler for unified retrieval

---

## Open Questions

1. **Memory Scope**: Per-user? Per-project? Global?
2. **Privacy**: How to handle sensitive information in memories?
3. **Forgetting**: Should users be able to explicitly delete memories? GDPR implications?
4. **Sharing**: Can memories be shared between users/agents?
5. **Conflicts**: When memory contradicts retrieved documents, which wins?
6. **Bootstrapping**: How to handle cold-start with no memories?

---

## Migration Path

1. **v0.8.9a-b**: Deploy memory schema (non-breaking, new tables)
2. **v0.8.9c-d**: Enable memory encoding and salience (background processing)
3. **v0.8.9e-f**: Activate memory retrieval and fabric API
4. **v0.8.9g**: Enable consolidation (off-peak hours initially)
5. **v0.8.9h**: Integrate capture points
6. **v0.8.9i**: Monitor metrics, tune salience weights

---

## Timeline Estimate

| Phase | Sub-versions | Relative Effort |
|-------|--------------|-----------------|
| Data Model & Storage | v0.8.9a-b | 20% |
| Encoding & Salience | v0.8.9c-d | 20% |
| Retrieval & Fabric | v0.8.9e-f | 25% |
| Consolidation | v0.8.9g | 20% |
| Integration & Hardening | v0.8.9h-i | 15% |
