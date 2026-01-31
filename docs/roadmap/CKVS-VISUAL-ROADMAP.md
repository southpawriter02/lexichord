# CKVS Integration Visual Roadmap

## Overview

This document provides a visual timeline of CKVS (Canonical Knowledge Validation System) integration into the Lexichord roadmap. CKVS features are integrated incrementally across v0.4.x through v0.7.x, running in parallel with existing planned features.

---

## Timeline Overview

```
2026                                              2027
Q2 (Apr-Jun)     Q3 (Jul-Sep)     Q4 (Oct-Dec)     Q1 (Jan-Mar)
|----------------|----------------|----------------|----------------|
     v0.4.x           v0.5.x           v0.6.x           v0.7.x

  ┌─────────────────────────────────────────────────────────────────┐
  │                    EXISTING ROADMAP                             │
  │  RAG/Vector    │  Retrieval   │  Agents/LLM  │  Specialists     │
  │  Foundation    │  Advanced    │  Gateway     │  Workflows       │
  └─────────────────────────────────────────────────────────────────┘

  ┌─────────────────────────────────────────────────────────────────┐
  │                    CKVS INTEGRATION                             │
  │  Foundation    │  Extraction  │  Validation  │  Sync/Context    │
  │  Graph+Schema  │  NLU+Claims  │  Engine      │  Agents          │
  └─────────────────────────────────────────────────────────────────┘
```

---

## Phase 1: CKVS Foundation (Q2 2026)

### Parallel Development: v0.4.x

```mermaid
gantt
    title v0.4.x Development Timeline
    dateFormat  YYYY-MM-DD
    section Existing RAG
    v0.4.1 Vector Foundation     :done, rag1, 2026-04-01, 2w
    v0.4.2 Watcher               :done, rag2, after rag1, 2w
    v0.4.3 Splitter              :done, rag3, after rag2, 2w
    v0.4.4 Embedder              :active, rag4, after rag3, 2w
    v0.4.5 Searcher              :rag5, after rag4, 2w
    v0.4.6 Reference Panel       :rag6, after rag5, 2w
    v0.4.7 Index Manager         :rag7, after rag6, 2w
    v0.4.8 Hardening             :rag8, after rag7, 2w

    section CKVS Foundation
    v0.4.5e Neo4j Integration    :ckvs1, 2026-04-22, 2w
    v0.4.5f Schema Registry      :ckvs2, after ckvs1, 1w
    v0.4.5g Entity Extraction    :ckvs3, after ckvs2, 2w
    v0.4.6 Axiom Store           :ckvs4, after ckvs3, 1w
    v0.4.7 Entity Browser        :ckvs5, after ckvs4, 2w
```

### Component Dependencies

```mermaid
graph LR
    subgraph "Existing v0.4.x"
        V41[v0.4.1<br/>Vector Foundation]
        V42[v0.4.2<br/>Watcher]
        V43[v0.4.3<br/>Splitter]
        V44[v0.4.4<br/>Embedder]
        V45[v0.4.5<br/>Searcher]
    end

    subgraph "CKVS v0.4.x"
        KG[v0.4.5e<br/>Graph DB]
        SR[v0.4.5f<br/>Schema Registry]
        EE[v0.4.5g<br/>Entity Extraction]
        AX[v0.4.6<br/>Axiom Store]
        EB[v0.4.7<br/>Entity Browser]
    end

    V41 --> V42 --> V43 --> V44 --> V45
    V41 -.->|DB Pattern| KG
    V43 -.->|Chunks| EE
    KG --> SR --> EE
    KG --> AX
    KG --> EB
    SR --> EB

    style KG fill:#22c55e
    style SR fill:#22c55e
    style EE fill:#22c55e
    style AX fill:#22c55e
    style EB fill:#22c55e
```

### v0.4.x Deliverables Summary

| Version | Existing Feature | CKVS Addition | Integration Point |
|:--------|:-----------------|:--------------|:------------------|
| v0.4.5 | Semantic Search | Neo4j + Schema + Extraction | Parallel development |
| v0.4.6 | Reference Panel | Axiom Store | Settings integration |
| v0.4.7 | Index Manager | Entity Browser | Same panel, different tab |
| v0.4.8 | Hardening | — | Test coverage |

---

## Phase 2: CKVS Extraction (Q3 2026)

### Parallel Development: v0.5.x

```mermaid
gantt
    title v0.5.x Development Timeline
    dateFormat  YYYY-MM-DD
    section Existing Retrieval
    v0.5.1 Hybrid Search         :ret1, 2026-07-01, 2w
    v0.5.2 Citation Engine       :ret2, after ret1, 2w
    v0.5.3 Context Expansion     :ret3, after ret2, 2w
    v0.5.4 Query Understanding   :ret4, after ret3, 2w
    v0.5.5 Filter System         :ret5, after ret4, 2w
    v0.5.6 Snippet Generation    :ret6, after ret5, 2w

    section CKVS Extraction
    v0.5.5 Entity Linking        :nlu1, 2026-08-12, 3w
    v0.5.6 Claim Extraction      :nlu2, after nlu1, 2w
```

### Component Integration

```mermaid
graph TB
    subgraph "v0.5.x Retrieval"
        HS[Hybrid Search]
        CE[Citation Engine]
        CX[Context Expansion]
        QU[Query Understanding]
    end

    subgraph "CKVS Extraction"
        ER[Entity Recognizer<br/>NER/Fine-tuned]
        EL[Entity Linker<br/>Graph Resolution]
        CL[Claim Extractor<br/>Proposition Parser]
        CS[Claim Storage<br/>PostgreSQL]
    end

    subgraph "Foundation (v0.4.x)"
        KG[(Knowledge Graph)]
        SR[Schema Registry]
        EE[Entity Extraction]
    end

    QU -->|Enhances| ER
    EE -->|Bootstrap| ER
    KG -->|Candidates| EL
    ER --> EL
    EL --> CL
    CL --> CS

    style ER fill:#3b82f6
    style EL fill:#3b82f6
    style CL fill:#3b82f6
    style CS fill:#3b82f6
```

### v0.5.x Deliverables Summary

| Version | Existing Feature | CKVS Addition | Integration Point |
|:--------|:-----------------|:--------------|:------------------|
| v0.5.4 | Query Understanding | — | NER foundation |
| v0.5.5 | Filter System | Entity Linking | Entity-aware search |
| v0.5.6 | Snippet Generation | Claim Extraction | Claim-enhanced snippets |

---

## Phase 3: CKVS Validation (Q4 2026)

### Parallel Development: v0.6.x

```mermaid
gantt
    title v0.6.x Development Timeline
    dateFormat  YYYY-MM-DD
    section Existing Agents
    v0.6.1 LLM Gateway           :llm1, 2026-10-01, 2w
    v0.6.2 Prompt Registry       :llm2, after llm1, 2w
    v0.6.3 Response Parser       :llm3, after llm2, 2w
    v0.6.4 Token Manager         :llm4, after llm3, 2w
    v0.6.5 Streaming             :llm5, after llm4, 2w
    v0.6.6 Co-pilot Agent        :llm6, after llm5, 3w

    section CKVS Validation
    v0.6.5 Validation Engine     :val1, 2026-11-12, 3w
    v0.6.6 Agent Integration     :val2, after val1, 2w
```

### Validation Engine Architecture

```mermaid
graph TB
    subgraph "Content Input"
        DOC[Document]
        GEN[Generated Content]
    end

    subgraph "CKVS Validation Engine"
        SV[Schema Validator<br/>Type Checking]
        AV[Axiom Validator<br/>Truth Checking]
        CV[Consistency Checker<br/>Conflict Detection]
        VO[Validation Orchestrator]
    end

    subgraph "Knowledge Sources"
        KG[(Knowledge Graph)]
        AX[(Axiom Store)]
        CL[(Claim Store)]
    end

    subgraph "Output"
        VR[Validation Result]
        FIX[Fix Suggestions]
    end

    DOC --> VO
    GEN --> VO
    VO --> SV --> KG
    VO --> AV --> AX
    VO --> CV --> CL
    SV --> VR
    AV --> VR
    CV --> VR
    VR --> FIX

    style SV fill:#f59e0b
    style AV fill:#f59e0b
    style CV fill:#f59e0b
    style VO fill:#f59e0b
```

### v0.6.x Deliverables Summary

| Version | Existing Feature | CKVS Addition | Integration Point |
|:--------|:-----------------|:--------------|:------------------|
| v0.6.5 | Streaming | Validation Engine | Parallel development |
| v0.6.6 | Co-pilot Agent | Graph-Aware Context | Pre/post validation |

---

## Phase 4: CKVS Agents & Sync (Q1 2027)

### Parallel Development: v0.7.x

```mermaid
gantt
    title v0.7.x Development Timeline
    dateFormat  YYYY-MM-DD
    section Existing Specialists
    v0.7.1 Agent Registry        :agt1, 2027-01-01, 2w
    v0.7.2 Context Assembler     :agt2, after agt1, 2w
    v0.7.3 Editor Agent          :agt3, after agt2, 2w
    v0.7.4 Simplifier Agent      :agt4, after agt3, 2w
    v0.7.5 Tuning Agent          :agt5, after agt4, 2w
    v0.7.6 Summarizer Agent      :agt6, after agt5, 2w
    v0.7.7 Agent Workflows       :agt7, after agt6, 2w

    section CKVS Integration
    v0.7.2 Graph Context         :ctx1, 2027-01-15, 1w
    v0.7.5 Unified Validation    :uv1, 2027-02-12, 2w
    v0.7.6 Sync Service          :sync1, after uv1, 3w
    v0.7.7 Validation Workflows  :vwf1, after sync1, 1w
```

### Knowledge-Aware Agent Architecture

```mermaid
graph TB
    subgraph "Context Assembly"
        DOC[Document Context]
        RAG[RAG Context]
        STY[Style Context]
        KNO[Knowledge Context<br/>CKVS Addition]
    end

    subgraph "Agent Layer"
        CO[Context Orchestrator]
        ED[Editor Agent]
        TU[Tuning Agent]
        SU[Summarizer Agent]
    end

    subgraph "CKVS Services"
        GQ[Graph Query]
        VS[Validation Service]
        SS[Sync Service]
    end

    DOC --> CO
    RAG --> CO
    STY --> CO
    KNO --> CO
    GQ --> KNO

    CO --> ED
    CO --> TU
    CO --> SU

    ED -->|Validate Output| VS
    TU -->|Validate Output| VS
    SU -->|Validate Output| VS

    ED -->|Update Graph| SS

    style KNO fill:#ec4899
    style GQ fill:#ec4899
    style VS fill:#ec4899
    style SS fill:#ec4899
```

### v0.7.x Deliverables Summary

| Version | Existing Feature | CKVS Addition | Integration Point |
|:--------|:-----------------|:--------------|:------------------|
| v0.7.2 | Context Assembler | Graph Context Strategy | New context source |
| v0.7.5 | Tuning Agent | Unified Validation | Combined fix workflow |
| v0.7.6 | Summarizer Agent | Sync Service | Bidirectional sync |
| v0.7.7 | Agent Workflows | Validation Steps | Workflow integration |

---

## Full Integration Timeline

```
                    2026                                    2027
        Apr    May    Jun    Jul    Aug    Sep    Oct    Nov    Dec    Jan    Feb    Mar
        ├──────┼──────┼──────┼──────┼──────┼──────┼──────┼──────┼──────┼──────┼──────┤

EXISTING │◄─────── v0.4.x RAG ────────►│◄──── v0.5.x Retrieval ────►│◄─── v0.6.x Agents ───►│◄──── v0.7.x Specialists ─────►│
        │ Vector  Watcher  Splitter   │ Hybrid  Citation  Context  │ LLM Gateway  Co-pilot  │ Editor  Tuning  Workflows    │
        │ DB      Ingest   Chunking   │ Search  Engine    Expand   │ Prompt Reg   Agent     │ Agent   Agent   Agent        │

CKVS    │◄─── Foundation ───►│        │◄─── Extraction ──►│       │◄── Validation ─►│      │◄────── Sync & Context ──────►│
        │ Neo4j    Schema    Entity   │ Entity   Claim    │       │ Schema  Axiom   Co-    │ Graph   Unified  Sync   Valid │
        │ Graph    Registry  Extract  │ Linking  Extract  │       │ Valid   Valid   pilot  │ Context Valid    Svc    WF    │
        │                    │        │                   │       │                        │                               │
        ├──────┬──────┬──────┼────────┼───────┬───────────┼───────┼──────┬─────────┬──────┼───────┬───────┬───────┬───────┤
        v0.4.5e v0.4.5f v0.4.5g       v0.5.5  v0.5.6            v0.6.5  v0.6.6         v0.7.2  v0.7.5  v0.7.6  v0.7.7
```

---

## Resource Allocation

### Team Structure

```mermaid
pie title Development Effort by Phase
    "CKVS Foundation (v0.4.x)" : 39
    "CKVS Extraction (v0.5.x)" : 30
    "CKVS Validation (v0.6.x)" : 35
    "CKVS Agents (v0.7.x)" : 40
```

### Recommended Team Allocation

| Phase | Timeline | CKVS FTE | Existing FTE | Notes |
|:------|:---------|:---------|:-------------|:------|
| Foundation | Q2 2026 | 1 | 2 | Parallel with RAG |
| Extraction | Q3 2026 | 1 | 2 | Extends Query Understanding |
| Validation | Q4 2026 | 1.5 | 1.5 | Integrated with Co-pilot |
| Agents | Q1 2027 | 1 | 2 | Enhances existing agents |

**Total CKVS Effort**: ~144 hours (~4 person-months)
**Total Timeline**: 12 months (Q2 2026 - Q1 2027)

---

## Risk Timeline

```mermaid
graph LR
    subgraph "Q2 - Low Risk"
        R1[Neo4j Learning Curve]
        R2[Schema Design Iteration]
    end

    subgraph "Q3 - Medium Risk"
        R3[NER Model Quality]
        R4[Entity Resolution Accuracy]
    end

    subgraph "Q4 - High Risk"
        R5[Validation False Positives]
        R6[Performance at Scale]
    end

    subgraph "Q1 - Medium Risk"
        R7[Sync Complexity]
        R8[Agent Integration]
    end

    R1 --> R3 --> R5 --> R7
    R2 --> R4 --> R6 --> R8

    style R5 fill:#ef4444
    style R6 fill:#ef4444
```

### Risk Mitigation Schedule

| Risk | Phase | Mitigation | Owner |
|:-----|:------|:-----------|:------|
| Neo4j Performance | Q2 | Early benchmarking, HNSW indexing | Backend Lead |
| NER Quality | Q3 | LLM fallback for low-confidence | ML Engineer |
| False Positives | Q4 | Confidence thresholds, user feedback | Product |
| Sync Complexity | Q1 | Event-driven architecture, idempotent ops | Backend Lead |

---

## Success Milestones

### Quarterly Checkpoints

```mermaid
timeline
    title CKVS Integration Milestones

    section Q2 2026
        Foundation Complete : Neo4j running
                           : Schema loaded
                           : 100+ entities stored
                           : Entity Browser works

    section Q3 2026
        Extraction Working : Entity linking >80% accuracy
                          : Claim extraction <30s/100 pages
                          : Human review UI functional

    section Q4 2026
        Validation Live : 95% axiom violation detection
                       : Co-pilot passes validation >90%
                       : Validation <500ms per doc

    section Q1 2027
        Full Integration : Doc-graph sync <5min lag
                        : Unified fix workflow
                        : Published errors down 70%
```

---

## Deferred to Post-v1.0

The following CKVS features are **not included** in this roadmap and are deferred to v0.10.x or later:

| Feature | Reason | Target |
|:--------|:-------|:-------|
| Worldbuilding Extensions | Out of scope for tech docs v1.0 | v0.10.x |
| Character/Location Entities | Worldbuilding feature | v0.10.x |
| Timeline Validation | Worldbuilding feature | v0.10.x |
| Advanced NLU (Fine-tuned Models) | Can launch with rules+LLM | v0.11.x |
| Multi-lingual Support | English-first for v1.0 | v0.11.x |
| Knowledge Graph Versioning | Post-launch optimization | v0.12.x |
| Visual Graph Designer | Advanced tooling | v0.12.x |

---

## Related Documents

- [CKVS Integration Strategy](./CKVS-INTEGRATION-STRATEGY.md) — Detailed integration plan
- [Roadmap v0.4.x](./roadmap-v0.4.x.md) — RAG/Archive phase details
- [Roadmap v0.5.x](./roadmap-v0.5.x.md) — Retrieval phase details
- [Roadmap v0.6.x](./roadmap-v0.6.x.md) — Agents phase details
- [Roadmap v0.7.x](./roadmap-v0.7.x.md) — Specialists phase details
- [v0.4.5-KG Specs](../specs/v0.4.x/v0.4.5/LCS-DES-045-KG-INDEX.md) — Detailed specs index

---

## Document History

| Date | Author | Changes |
|:-----|:-------|:--------|
| 2026-01-31 | Lead Architect | Initial visual roadmap creation |

---
