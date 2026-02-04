# Design Proposal: Contextual Compression Engine

A system for intelligently compressing conversational context while preserving semantic fidelity and enabling on-demand expansion.

---

## Problem Statement

Long-running AI conversations and complex agent workflows accumulate context that exceeds LLM context window limits. Naive truncation loses critical information. Simple summarization flattens nuance. We need a compression system that:

1. Reduces token usage without losing actionable context
2. Preserves decision points, commitments, and unresolved questions
3. Allows selective expansion when deeper recall is needed
4. Works transparently as middleware in existing pipelines

---

## Core Concepts

### Hierarchical Compression Levels

```
┌─────────────────────────────────────────────────────────────┐
│ Level 0: Full Transcript                                    │
│ Complete conversation history, all messages verbatim        │
├─────────────────────────────────────────────────────────────┤
│ Level 1: Detailed Summary                                   │
│ Condensed narrative preserving key exchanges and outcomes   │
├─────────────────────────────────────────────────────────────┤
│ Level 2: Brief Summary                                      │
│ High-level overview: goals, decisions made, current state   │
├─────────────────────────────────────────────────────────────┤
│ Level 3: Topic Tags                                         │
│ Keywords and entity mentions for retrieval                  │
└─────────────────────────────────────────────────────────────┘
```

Each level is stored independently. Retrieval can start at any level and expand downward as needed.

### Anchor Points

Certain elements are **never compressed away**, regardless of level:

- **Commitments**: "I will...", "You should...", action items
- **Decisions**: Choices made between alternatives
- **Unresolved Questions**: Open loops that need follow-up
- **Critical Facts**: User-provided data, configurations, preferences
- **Corrections**: "Actually...", "I was wrong about..."

Anchor points are tagged during compression and always included in the compressed output.

### Expansion Tokens

Compressed summaries contain special markers indicating expandable regions:

```
[Discussed authentication options →L1:auth_discussion]
```

When the agent encounters a marker and needs more detail, it triggers retrieval of the Level 1 content for that section.

---

## Proposed Architecture

### Components

```
┌─────────────────────────────────────────────────────────────┐
│                    Compression Engine                        │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │  Segmenter  │  │ Summarizer  │  │  Anchor Extractor   │  │
│  │             │  │             │  │                     │  │
│  │ Splits conv │  │ LLM-based   │  │ Identifies critical │  │
│  │ into chunks │  │ compression │  │ preserve-always     │  │
│  └─────────────┘  └─────────────┘  └─────────────────────┘  │
│                                                              │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │   Storage   │  │  Expander   │  │   Token Budgeter    │  │
│  │             │  │             │  │                     │  │
│  │ Multi-level │  │ On-demand   │  │ Allocates context   │  │
│  │ cache/DB    │  │ retrieval   │  │ window dynamically  │  │
│  └─────────────┘  └─────────────┘  └─────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

### Data Flow

1. **Ingestion**: New messages appended to Level 0
2. **Segmentation**: Conversation split into topical chunks (by turn count, topic shift, or time)
3. **Compression**: Each chunk summarized to Levels 1, 2, 3 with anchors preserved
4. **Budget Allocation**: Based on current task, allocate tokens across levels
5. **Context Assembly**: Combine compressed history + expanded relevant sections
6. **Expansion Trigger**: Classifier detects when compressed info is insufficient, fetches more

---

## Interface Design

### Core Interface

```csharp
public interface IContextCompressor
{
    /// <summary>
    /// Compress a conversation segment to specified level.
    /// </summary>
    Task<CompressedSegment> CompressAsync(
        ConversationSegment segment,
        CompressionLevel targetLevel,
        CancellationToken ct = default);

    /// <summary>
    /// Expand a compressed segment to a more detailed level.
    /// </summary>
    Task<CompressedSegment> ExpandAsync(
        string segmentId,
        CompressionLevel targetLevel,
        CancellationToken ct = default);

    /// <summary>
    /// Assemble context within a token budget.
    /// </summary>
    Task<AssembledContext> AssembleContextAsync(
        string conversationId,
        int tokenBudget,
        ContextAssemblyOptions options,
        CancellationToken ct = default);
}
```

### Supporting Types

```csharp
public enum CompressionLevel
{
    Full = 0,
    Detailed = 1,
    Brief = 2,
    Tags = 3
}

public record CompressedSegment(
    string SegmentId,
    CompressionLevel Level,
    string Content,
    IReadOnlyList<AnchorPoint> Anchors,
    IReadOnlyList<ExpansionMarker> ExpansionMarkers,
    int TokenCount);

public record AnchorPoint(
    AnchorType Type,
    string Content,
    int OriginalPosition);

public enum AnchorType
{
    Commitment,
    Decision,
    UnresolvedQuestion,
    CriticalFact,
    Correction
}
```

---

## Integration Points for Lexichord

### Where It Fits

```
User Query
    │
    ▼
┌──────────────────┐
│  Query Analyzer  │  (existing)
└────────┬─────────┘
         │
         ▼
┌──────────────────┐     ┌──────────────────────────┐
│  RAG Retrieval   │────▶│  Context Compressor      │
│  (chunks, BM25)  │     │  (compress conv history) │
└────────┬─────────┘     └────────────┬─────────────┘
         │                            │
         └───────────┬────────────────┘
                     ▼
              ┌─────────────┐
              │ LLM Request │
              │ (assembled  │
              │  context)   │
              └─────────────┘
```

### No Schema Changes Required

- Compression storage can use existing PostgreSQL with new tables
- Or standalone SQLite file for conversation-specific compression cache
- RAG chunk storage remains unchanged

---

## Implementation Phases

### Phase 1: Basic Compression
- Implement Segmenter (split by message count)
- Implement Summarizer (single-level, LLM-based)
- Store compressed versions alongside full history

### Phase 2: Hierarchical Levels
- Add multi-level compression
- Implement anchor extraction
- Add expansion markers to summaries

### Phase 3: Dynamic Assembly
- Token budget allocation
- Expansion trigger classifier
- Context assembly with mixed compression levels

### Phase 4: Optimization
- Caching strategies
- Incremental compression (only new segments)
- Compression quality metrics

---

## Open Questions

1. **Segmentation Strategy**: Fixed turn count vs. topic detection vs. time-based?
2. **Anchor Classification**: Rule-based patterns vs. LLM classification?
3. **Expansion Triggering**: Explicit markers vs. embedding similarity vs. LLM self-assessment?
4. **Storage Location**: Alongside conversation in app DB vs. separate cache?
5. **Multi-conversation**: Should compression be per-conversation or cross-conversation?

---

## Success Metrics

- **Compression Ratio**: Tokens reduced at each level
- **Fidelity Score**: Can agent answer questions about compressed history accurately?
- **Expansion Hit Rate**: How often does expansion retrieve useful additional context?
- **Latency Impact**: Added latency from compression/expansion operations
