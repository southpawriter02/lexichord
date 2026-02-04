# Design Proposal: Persistent Agent Memory Fabric

A cross-session memory layer enabling AI agents to build, retain, and refine personal knowledge over time with temporal awareness.

---

## Problem Statement

Current AI agents are largely stateless between sessions:

- Each conversation starts fresh with no memory of past interactions
- Users must re-explain preferences, context, and project details
- Agents can't learn from past successes or mistakes
- No concept of "when" or "why" something was learned

We need a memory system that gives agents:

1. **Persistence**: Memory survives across sessions
2. **Temporal awareness**: Understanding of when things were learned and how beliefs evolved
3. **Structured recall**: Different memory types for different purposes
4. **Continuous learning**: Improvement based on experience

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
| **Episodic** | Specific events, conversations | Long-term | "Last week we refactored the auth module" |
| **Procedural** | Skills, workflows, how-tos | Long-term | "To run tests: npm test" |
| **Working** | Current session context | Session only | Active task state |

### Temporal Indexing

Every memory has temporal metadata:

```
Memory {
    content: "User prefers verbose logging"
    
    temporal: {
        created_at: 2026-01-15T10:30:00Z
        last_accessed: 2026-02-01T14:22:00Z
        access_count: 7
        last_reinforced: 2026-01-28T09:15:00Z
        confidence_trajectory: [0.6, 0.7, 0.85, 0.9]
    }
    
    provenance: {
        source_conversation: "abc-123"
        learning_context: "User corrected default log level"
    }
}
```

This enables queries like:
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

Low-salience memories fade (deprioritized, not deleted). High-salience memories surface readily.

---

## Proposed Architecture

### System Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    Agent Application                         │
│  ┌─────────────────────────────────────────────────────┐    │
│  │                   Memory Fabric                      │    │
│  ├─────────────────────────────────────────────────────┤    │
│  │                                                      │    │
│  │  ┌──────────────┐  ┌──────────────┐                 │    │
│  │  │ Memory       │  │ Temporal     │                 │    │
│  │  │ Encoder      │  │ Index        │                 │    │
│  │  │              │  │              │                 │    │
│  │  │ Structures & │  │ When/how     │                 │    │
│  │  │ embeds new   │  │ memories     │                 │    │
│  │  │ memories     │  │ relate in    │                 │    │
│  │  │              │  │ time         │                 │    │
│  │  └──────────────┘  └──────────────┘                 │    │
│  │                                                      │    │
│  │  ┌──────────────┐  ┌──────────────┐                 │    │
│  │  │ Retriever    │  │ Consolidator │                 │    │
│  │  │              │  │              │                 │    │
│  │  │ Salience-    │  │ Background   │                 │    │
│  │  │ weighted     │  │ process that │                 │    │
│  │  │ recall       │  │ strengthens  │                 │    │
│  │  │              │  │ & prunes     │                 │    │
│  │  └──────────────┘  └──────────────┘                 │    │
│  │                                                      │    │
│  └─────────────────────────────────────────────────────┘    │
│                            │                                 │
│                            ▼                                 │
│  ┌─────────────────────────────────────────────────────┐    │
│  │              Memory Store                            │    │
│  │  PostgreSQL + pgvector / SQLite + FAISS             │    │
│  └─────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘
```

### Components

#### Memory Encoder
- Classifies incoming information by memory type
- Generates embeddings for semantic search
- Extracts temporal context
- Links to related existing memories

#### Temporal Index
- Maintains timeline of memory creation/access
- Enables time-range queries
- Tracks confidence evolution

#### Retriever
- Multi-factor ranking (salience scoring)
- Supports different retrieval modes:
  - Relevant to current context
  - Recent memories
  - Memories about specific topics
  - Memories from specific time periods

#### Consolidator
- Background process (like "sleep")
- Strengthens frequently-accessed memories
- Identifies patterns across episodic memories → creates semantic memories
- Prunes low-salience memories (archive, don't delete)

---

## Interface Design

### Core Interface

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
        CancellationToken ct = default);

    /// <summary>
    /// Reinforce a memory (increase salience).
    /// </summary>
    Task ReinforcememoryAsync(
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
    /// Run consolidation process.
    /// </summary>
    Task ConsolidateAsync(
        ConsolidationOptions options,
        CancellationToken ct = default);
}
```

### Supporting Types

```csharp
public record Memory(
    string Id,
    MemoryType Type,
    string Content,
    float[] Embedding,
    TemporalMetadata Temporal,
    ProvenanceInfo Provenance,
    float CurrentSalience,
    IReadOnlyList<string> LinkedMemoryIds);

public enum MemoryType
{
    Semantic,   // Facts and concepts
    Episodic,   // Specific events
    Procedural, // How to do things
    Working     // Current session (not persisted long-term)
}

public record TemporalMetadata(
    DateTimeOffset CreatedAt,
    DateTimeOffset LastAccessed,
    int AccessCount,
    DateTimeOffset? LastReinforced,
    IReadOnlyList<float> ConfidenceTrajectory);

public record RecallOptions(
    int MaxResults = 10,
    MemoryType? TypeFilter = null,
    float MinSalience = 0.0f,
    RecallMode Mode = RecallMode.Relevant);

public enum RecallMode
{
    Relevant,    // Most relevant to query
    Recent,      // Most recently accessed
    Important,   // Highest salience
    Temporal     // Within time range
}

public enum ReinforcementReason
{
    ExplicitUserConfirmation,
    SuccessfulApplication,
    RepeatedAccess,
    UserCorrection  // Also reinforces, but with updated content
}
```

---

## Schema Design

### Core Tables

```sql
-- Main memory storage
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
    
    -- Status
    status TEXT NOT NULL DEFAULT 'active', -- 'active', 'archived', 'superseded'
    superseded_by UUID REFERENCES memories(id)
);

-- Confidence trajectory over time
CREATE TABLE memory_confidence_history (
    id UUID PRIMARY KEY,
    memory_id UUID NOT NULL REFERENCES memories(id),
    confidence REAL NOT NULL,
    recorded_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Links between related memories
CREATE TABLE memory_links (
    id UUID PRIMARY KEY,
    from_memory_id UUID NOT NULL REFERENCES memories(id),
    to_memory_id UUID NOT NULL REFERENCES memories(id),
    link_type TEXT NOT NULL, -- 'related', 'derived_from', 'contradicts', 'supersedes'
    strength REAL NOT NULL DEFAULT 0.5,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Index for vector similarity search
CREATE INDEX memories_embedding_idx ON memories 
    USING ivfflat (embedding vector_cosine_ops);

-- Index for temporal queries
CREATE INDEX memories_temporal_idx ON memories (created_at, last_accessed_at);

-- Index for salience-based retrieval
CREATE INDEX memories_salience_idx ON memories (current_salience DESC) 
    WHERE status = 'active';
```

---

## Integration with Lexichord

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
│             │  Unified Retrieval      │                      │
│             │  Layer                  │                      │
│             │                         │                      │
│             │  Combines document      │                      │
│             │  knowledge + agent      │                      │
│             │  memory for context     │                      │
│             └─────────────────────────┘                      │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

### Shared Infrastructure

Both systems can share:
- PostgreSQL database (separate schemas or tables)
- pgvector for embeddings
- Common embedding model

### Memory Capture Points

Where memories get created in Lexichord:

1. **User corrections**: "Actually, the API uses OAuth, not API keys"
   → Semantic memory about the project

2. **Successful searches**: When user confirms a result was helpful
   → Reinforces related memories

3. **Configuration changes**: User sets preferences
   → Semantic memory of preferences

4. **Conversation milestones**: Major decisions, completed tasks
   → Episodic memories

5. **Workflow observations**: Repeated action patterns
   → Procedural memories

---

## Implementation Phases

### Phase 1: Basic Storage & Recall
- Implement core Memory table
- Basic embedding-based retrieval
- Simple salience scoring (recency only)
- Manual memory creation API

### Phase 2: Automatic Memory Extraction
- Conversation analysis for memory-worthy content
- Classification into memory types
- Provenance tracking

### Phase 3: Temporal Features
- Time-range queries
- Confidence trajectory tracking
- "What's changed" queries

### Phase 4: Consolidation
- Background consolidation process
- Pattern extraction from episodic → semantic
- Salience decay and pruning

### Phase 5: Advanced Features
- Memory linking and relationships
- Contradiction detection
- Superseding logic
- Cross-user memory sharing (opt-in)

---

## Consolidation Process

The "sleep" cycle that improves memory organization:

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
│     Merge redundant semantic memories                        │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

---

## Open Questions

1. **Memory Scope**: Per-user? Per-project? Global?
2. **Privacy**: How to handle sensitive information in memories?
3. **Forgetting**: Should users be able to explicitly delete memories? GDPR implications?
4. **Sharing**: Can memories be shared between users/agents?
5. **Conflicts**: When memory contradicts retrieved documents, which wins?
6. **Bootstrapping**: How to handle cold-start with no memories?

---

## Success Metrics

- **Recall Accuracy**: Are retrieved memories actually relevant?
- **Learning Curve**: Does agent performance improve over sessions?
- **User Corrections**: Decreasing rate of "you forgot that..." moments
- **Memory Utilization**: % of memories that get accessed vs. archived
- **Consolidation Quality**: Are extracted patterns accurate and useful?
- **User Satisfaction**: Perceived "intelligence" improvement over time
