# CKVS Integration Strategy for Lexichord Roadmap

## Executive Summary

This document outlines the integration strategy for incorporating the Canonical Knowledge Validation System (CKVS) into the existing Lexichord roadmap (v0.4.x through v0.7.x). The approach favors **incremental integration** over creating separate CKVS-only versions, minimizing technical debt and leveraging existing infrastructure.

**Decision**: Integrate CKVS features progressively across v0.4.x-v0.7.x using version sub-releases (e.g., v0.4.5-v0.4.7 for CKVS Foundation).

---

## Integration Principles

1. **Augment, Don't Replace**: CKVS enhances existing features rather than duplicating them
2. **Leverage Infrastructure**: Use PostgreSQL, agent framework, and prompt templates already built
3. **Unified Validation**: Merge style linting and knowledge validation into coherent system
4. **Deferred Worldbuilding**: Focus on technical documentation; save worldbuilding extensions for post-v1.0

---

## Phase-by-Version Mapping

### v0.4.x: The Archive → CKVS Foundation (Integrated)

**Existing Plan**: Vector storage, document ingestion, semantic search
**CKVS Addition**: Knowledge Graph infrastructure, basic schema, ontology

#### v0.4.5: Knowledge Graph Foundation (NEW)
*Extends v0.4.4 (Embedder)*

**Components Added:**
- **[KG-01] Graph Database Integration**: Add Neo4j Docker container alongside PostgreSQL
  - Schema: Technical documentation entities (Product, Component, Endpoint, Parameter, Response)
  - Migration: `Migration_005_KnowledgeGraph.cs` - connection config, basic metadata tables
  - Repository: `IKnowledgeGraphRepository` interface, Neo4j Cypher implementation

- **[KG-02] Schema Registry Service**: Define and enforce entity/relationship types
  - Schema YAML definitions in `.lexichord/knowledge/schema/`
  - Schema validation on entity creation
  - Version tracking for schema evolution

- **[KG-03] Entity Abstraction Layer**: Bridge between Markdown content and graph nodes
  - `IEntityExtractor` interface for identifying entities in text
  - Basic extractors: Endpoint, Parameter, Concept
  - Confidence scoring for extraction quality

**Dependencies on Prior Versions:**
- `IDbConnectionFactory` (v0.0.5b) - pattern for graph connection
- `IDocumentRepository` (v0.4.1c) - link documents to graph entities
- `FluentMigrator` (v0.0.5c) - schema management pattern

**License Gating:** Knowledge Graph features require WriterPro tier

**Timeline:** 2-3 weeks (parallel to v0.4.5-v0.4.6 work)

---

#### v0.4.6: Axiom Store (NEW)
*Runs parallel to existing v0.4.6 (Reference Panel)*

**Components Added:**
- **[KG-04] Axiom Storage**: PostgreSQL table for foundational truths
  - Table: `axioms` (id, content, scope, authority_source, created_at)
  - `IAxiomRepository` for CRUD operations
  - Axiom categories: Architectural, Business Rules, API Contracts

- **[KG-05] Axiom Management UI**: Settings panel for managing axioms
  - List view of active axioms with edit/delete
  - "Add Axiom" form with scope selector
  - Inheritance visualization (which entities are constrained)

**Integration Point:** Axioms can reference style rules from v0.2.x, creating unified governance

**Timeline:** 1 week

---

#### v0.4.7: Entity-Document Linking (MODIFIED)
*Replaces planned "Index Manager" with knowledge-aware version*

**Components Modified:**
- **[KG-06] Knowledge-Aware Indexing**: Extend document indexing to populate graph
  - During chunk ingestion (v0.4.2), extract entities and create graph nodes
  - Store `document_id → entity_ids` mapping
  - Bidirectional sync: Document changes update graph, graph changes flag documents

- **[KG-07] Entity Browser**: New panel showing knowledge graph visually
  - Node/edge graph visualization (using vis.js or similar)
  - Click entity to see all referencing documents
  - Filter by entity type, search by property

**Replaces:** Original "Index Status View" becomes "Knowledge & Index Status"

**Timeline:** 2 weeks

---

### v0.5.x: The Retrieval → CKVS Extraction (Integrated)

**Existing Plan**: Hybrid search, citations, context expansion
**CKVS Addition**: NLU pipeline, entity linking, claim extraction

#### v0.5.5: Entity Linking & Recognition (MODIFIED)
*Extends v0.5.4 (Query Understanding) with entity-aware search*

**Components Added:**
- **[NLU-01] Entity Recognizer**: Identify entity mentions in queries and text
  - NER using domain-specific training (fine-tuned transformer or SpaCy)
  - Recognize: Endpoints, Parameters, Concepts from knowledge graph
  - Confidence thresholds: >0.8 for auto-link, <0.8 request human clarification

- **[NLU-02] Entity Linker**: Resolve mentions to canonical graph nodes
  - Candidate generation from graph (fuzzy match on names/aliases)
  - Disambiguation using context (surrounding text, document topic)
  - `IEntityLinker` interface with `Link(mention, context) → EntityReference`

**Integration Point:** Enhances existing semantic search (v0.5.1) with structured entity knowledge

**Timeline:** 2-3 weeks

---

#### v0.5.6: Claim Extraction (MODIFIED)
*Extends v0.5.6 (Snippet Generation) with formal claim parsing*

**Components Added:**
- **[NLU-03] Claim Extractor**: Parse prose statements into formal propositions
  - Extract from Markdown: "The /users endpoint accepts a limit parameter with default 100"
  - Output: `Endpoint('/users') ACCEPTS Parameter('limit'); Parameter('limit') HAS_DEFAULT 100`
  - Claim types: Existence, Property, Relationship, Constraint
  - `IClaimExtractor` interface with `Extract(text, linked_entities) → List[FormalClaim]`

- **[NLU-04] Claim Storage**: PostgreSQL table for extracted claims
  - Table: `claims` (id, document_id, chunk_id, claim_type, subject, predicate, object, confidence)
  - Link claims to both source document and graph entities
  - Track extraction provenance for debugging

**Integration Point:** Claims feed into validation engine (v0.6.x) and enhance search relevance

**Timeline:** 2 weeks

---

### v0.6.x: The Conductors → CKVS Validation Engine (Integrated)

**Existing Plan**: LLM gateway, agents, prompt templates
**CKVS Addition**: Validation orchestration, consistency checking

#### v0.6.5: Validation Engine Foundation (NEW)
*Runs parallel to v0.6.5 (Streaming)*

**Components Added:**
- **[VAL-01] Schema Validator**: Verify claims conform to ontological schema
  - Check entity types exist before creating relationships
  - Validate property constraints (type, required fields)
  - Immediate feedback on structural errors

- **[VAL-02] Axiom Validator**: Check claims against foundational axioms
  - Pattern matching: Does claim contradict established axiom?
  - Logical inference: Derive constraints from axiom scope
  - Severity levels: Error (must fix), Warning (review recommended)

- **[VAL-03] Consistency Checker**: Detect conflicts between derived content
  - Compare extracted claims across documents
  - Identify contradictions: "Parameter X defaults to 100" vs "Parameter X defaults to 50"
  - Queue conflicts for human resolution

**Integration Point:** Validates content generated by Co-pilot (v0.6.6) before acceptance

**License Gating:** Full validation requires Teams tier (WriterPro gets basic schema checking)

**Timeline:** 3 weeks

---

#### v0.6.6: Knowledge-Aware Co-pilot (MODIFIED)
*Enhances existing Co-pilot Agent with graph context*

**Components Modified:**
- **[VAL-04] Validation API for Agents**: Expose validation to content generation
  - `IValidationService.Validate(content, context) → ValidationResult`
  - Pre-generation: Query graph for constraints before generating
  - Post-generation: Validate claims before showing to user
  - Explain violations: "This conflicts with axiom X which states..."

- **[VAL-05] Graph-Aware Context Assembly**: Include knowledge graph in prompts
  - Query graph for entities related to user's query
  - Inject canonical definitions, relationships, constraints
  - Reduces hallucination by grounding responses in known facts

**Integration Point:** Co-pilot now validates its own output against knowledge graph before presenting

**Timeline:** 2 weeks (integrated with v0.6.6 work)

---

### v0.7.x: The Specialists → CKVS Content Agents (Merged)

**Existing Plan**: Specialized agents (Editor, Simplifier, etc.)
**CKVS Addition**: Knowledge-aware generation, synchronization

**Key Decision**: **Do NOT create separate CKVS agents**. Instead, enhance existing agents with knowledge awareness.

#### v0.7.2: Knowledge-Aware Context Assembler (MODIFIED)
*Extends v0.7.2 (Context Assembler) to include graph queries*

**Components Added:**
- **[CTX-01] Graph Context Strategy**: New `IContextStrategy` for knowledge graph
  - Query graph for entities mentioned in selection/cursor context
  - Retrieve entity properties, relationships, applicable axioms
  - Format as structured context for prompt injection
  - Priority: High (knowledge should influence generation)

**Integration:** Existing `IContextOrchestrator` now includes knowledge graph alongside RAG, style, document context

**Timeline:** 1 week

---

#### v0.7.5: Validation-Enhanced Tuning Agent (MODIFIED)
*Extends Tuning Agent to fix both style AND knowledge violations*

**Components Modified:**
- **[AGT-01] Unified Violation Handling**: Combine linting and CKVS validation
  - `IViolationAggregator` merges style violations (v0.2.x) + knowledge violations (v0.6.5)
  - Single "Fix All" workflow addresses both types
  - Prioritize: Knowledge violations (errors) > Style violations (warnings)

- **[AGT-02] Knowledge-Grounded Rewriting**: Tuning Agent uses graph for corrections
  - Query graph for canonical values when fixing violations
  - Ensure corrected text aligns with axioms
  - Validate rewritten content before applying

**Integration:** Tuning Agent becomes the unified enforcement point for all governance (style + knowledge)

**Timeline:** 2 weeks (integrated with v0.7.5 work)

---

#### v0.7.6: Synchronization Service (NEW)
*Runs parallel to Summarizer Agent*

**Components Added:**
- **[SYNC-01] Document-Graph Sync**: Bidirectional consistency maintenance
  - **Document → Graph**: Extract claims from saved documents, propose graph updates
  - **Graph → Knowledge**: When entities change, identify affected documents
  - `ISyncCoordinator` orchestrates change detection and propagation

- **[SYNC-02] Conflict Resolution Workflow**: Handle contradictions between sources
  - Detect: Graph says "Parameter X defaults to 100", Document says "defaults to 50"
  - Present to human: Show both claims with sources, request decision
  - Apply: Update graph and/or document based on resolution
  - Track patterns for learning

**Integration:** Uses existing `IMediator` events from document saves (v0.1.4c) to trigger sync

**Timeline:** 2-3 weeks

---

#### v0.7.7: Knowledge-Aware Workflows (MODIFIED)
*Enhances existing Agent Workflows with validation steps*

**Components Modified:**
- **[WF-01] Validation Workflow Step**: Add validation as workflow stage
  - New step type: `ValidationStep` that runs `IValidationService`
  - Conditional execution: Proceed only if validation passes
  - Output mapping: Pass violations to subsequent agents for fixing

**Example Workflow:**
```yaml
workflow_id: "knowledge-validated-generation"
steps:
  - step_id: "generate"
    agent_id: "api-documentation"
  - step_id: "validate"
    type: "validation"
    fail_on_error: false
  - step_id: "fix_violations"
    agent_id: "tuning"
    condition: "validation.violations.Count > 0"
```

**Timeline:** 1 week (minimal addition to v0.7.7)

---

## Deferred Features

The following CKVS components are **deferred to post-v1.0** (v0.10.x or later):

### Deferred to v0.10.x+
1. **Worldbuilding Extensions** (CKVS Part V, Section 13)
   - Character, Location, Event entities
   - Perspectival knowledge (character beliefs vs. objective truth)
   - Timeline validation
   - *Rationale*: Technical documentation is v1.0 focus; worldbuilding is separate domain

2. **Advanced NLU** (CKVS Phase 4)
   - Production-grade entity recognition (fine-tuned models)
   - Coreference resolution across documents
   - Multi-lingual support
   - *Rationale*: Can launch with rule-based/LLM-assisted extraction, optimize later

3. **Version Branching** (CKVS Phase 4)
   - Knowledge graph versioning per product version
   - Temporal queries ("what was true in v2.1?")
   - *Rationale*: Adds complexity; can be added once core system proven

4. **Visual Knowledge Browser** (CKVS Section 4.6)
   - Full graph visualization with exploration
   - Interactive entity relationship mapping
   - *Rationale*: v0.4.7 provides basic version; full UI can wait

---

## Dependencies Summary

| CKVS Component | Depends On (Existing Roadmap) | Enables (Future Features) |
|:---------------|:------------------------------|:--------------------------|
| Knowledge Graph (v0.4.5) | PostgreSQL (v0.0.5), FluentMigrator (v0.0.5c) | All CKVS features |
| Axiom Store (v0.4.6) | ISettingsService (v0.1.6a) | Validation Engine (v0.6.5) |
| Entity Linking (v0.5.5) | Semantic Search (v0.5.1), NER models | Claim Extraction (v0.5.6) |
| Claim Extraction (v0.5.6) | Entity Linking (v0.5.5), Markdown Parser (v0.1.3b) | Validation Engine (v0.6.5) |
| Validation Engine (v0.6.5) | Claims (v0.5.6), Axioms (v0.4.6), Graph (v0.4.5) | Agent validation (v0.6.6+) |
| Sync Service (v0.7.6) | Document events (v0.1.4c), Validation (v0.6.5) | Automated consistency |

---

## License Gating Strategy

| Feature | Core | WriterPro | Teams | Enterprise |
|:--------|:-----|:----------|:------|:-----------|
| Knowledge Graph (read-only) | — | ✓ | ✓ | ✓ |
| Entity Browser | — | ✓ | ✓ | ✓ |
| Axiom Management | — | ✓ | ✓ | ✓ |
| Basic Schema Validation | — | ✓ | ✓ | ✓ |
| Full Validation (Axiom + Consistency) | — | — | ✓ | ✓ |
| Knowledge-Aware Agents | — | — | ✓ | ✓ |
| Document-Graph Sync | — | — | ✓ | ✓ |
| Conflict Resolution Workflows | — | — | ✓ | ✓ |
| Custom Ontology Schemas | — | — | — | ✓ |

---

## Implementation Timeline

Assuming 2-week sprints and parallel workstreams:

### Q2 2026 (Months 4-6)
- **v0.4.5-v0.4.7**: CKVS Foundation (Knowledge Graph, Axioms, Entity Browser)
- **Parallel**: Complete existing v0.4.x RAG features
- **Total**: 6-8 weeks

### Q3 2026 (Months 7-9)
- **v0.5.5-v0.5.6**: CKVS Extraction (Entity Linking, Claim Extraction)
- **Parallel**: Complete existing v0.5.x Retrieval features
- **Total**: 4-6 weeks

### Q4 2026 (Months 10-12)
- **v0.6.5-v0.6.6**: CKVS Validation (Validation Engine, Agent Integration)
- **Parallel**: Complete existing v0.6.x Conductors features
- **Total**: 5-7 weeks

### Q1 2027 (Months 13-15)
- **v0.7.2, v0.7.5-v0.7.7**: CKVS Agents (Knowledge Context, Sync, Workflows)
- **Parallel**: Complete existing v0.7.x Specialists features
- **Total**: 6-8 weeks

**Total CKVS Development Time**: ~21-29 weeks (5-7 months) integrated across existing roadmap

---

## Risk Mitigation

### Technical Risks

1. **Graph Database Performance at Scale**
   - *Risk*: Neo4j may struggle with 10K+ entities
   - *Mitigation*:
     - Implement caching layer for frequent queries
     - Use read replicas for query-heavy operations
     - Benchmark early (v0.4.5) with synthetic data

2. **Dual Knowledge Stores Confusion** (RAG vs. Knowledge Graph)
   - *Risk*: Users confused about when to use semantic search vs. entity search
   - *Mitigation*:
     - Unified search interface that queries both
     - Clear documentation on use cases: RAG for prose, Graph for structured facts
     - Eventual goal: RAG chunks link to graph entities

3. **Validation False Positives**
   - *Risk*: Overzealous validation blocks legitimate edge cases
   - *Mitigation*:
     - Confidence thresholds with "Review" state (not just Valid/Invalid)
     - User feedback loop to improve patterns
     - "Suppress Rule" option for known exceptions

### Process Risks

1. **Roadmap Scope Creep**
   - *Risk*: Adding CKVS delays v1.0 Gold
   - *Mitigation*:
     - Strict feature gating: Defer worldbuilding and advanced NLU
     - Parallel development: CKVS work doesn't block existing features
     - MVP mindset: Launch with rule-based extraction, improve later

2. **Team Bandwidth**
   - *Risk*: Not enough developers to handle parallel workstreams
   - *Mitigation*:
     - Prioritize: CKVS Foundation first, other phases can follow
     - Consider phased rollout: v0.4.5-v0.5.6 in current roadmap, v0.6.5+ in v0.10.x
     - Use v0.x.9 suffix if needed to isolate CKVS work

---

## Alternative Approaches (Rejected)

### Alternative A: v0.10.x Clean Slate
**Proposal**: Complete v0.1.x-v0.9.x as planned, then do CKVS in v0.10.x-v0.13.x

**Rejected Because:**
- Technical debt: Retrofitting CKVS into existing style/agent systems is harder than integrating incrementally
- Duplicated effort: Would need to refactor v0.7.x agents to become knowledge-aware later
- User experience: Knowledge validation feels like missing feature in v1.0 if deferred

### Alternative B: Dedicated CKVS Versions
**Proposal**: Use v0.4.9x, v0.5.9x, v0.6.9x, v0.7.9x for CKVS-only releases

**Rejected Because:**
- Fragmentation: Creates two parallel feature tracks that are hard to merge
- Confusion: Users won't understand x.9 naming convention
- Integration risk: Late merging of CKVS with RAG/Agents could cause conflicts

### Alternative C: CKVS as Separate Module
**Proposal**: Build CKVS as optional add-on module, separate from core

**Rejected Because:**
- Core value prop: Knowledge validation is central to Lexichord's mission, not optional
- Tight coupling: CKVS needs deep integration with agents, style system, and content generation
- Maintenance burden: Keeping separate module compatible with core versions is costly

---

## Success Criteria

### v0.4.x Milestone (CKVS Foundation)
- [ ] Neo4j container runs alongside PostgreSQL with <100ms avg query latency
- [ ] 5 entity types defined (Product, Component, Endpoint, Parameter, Response)
- [ ] Can manually create 100 entities and query relationships
- [ ] Entity Browser shows graph visualization with clickable nodes
- [ ] 10 foundational axioms entered and stored

### v0.5.x Milestone (CKVS Extraction)
- [ ] Entity recognizer achieves >80% precision/recall on test corpus
- [ ] Claim extractor processes 100-page doc in <30 seconds
- [ ] Extracted claims correctly link to graph entities >85% of time
- [ ] Human-in-the-loop clarification UI works for ambiguous entities

### v0.6.x Milestone (CKVS Validation)
- [ ] Validation engine detects 95% of known axiom violations in test set
- [ ] Validation latency <500ms for typical document (100 claims)
- [ ] Co-pilot generates content that passes validation >90% of time
- [ ] Validation explanations rated "helpful" by >80% of beta testers

### v0.7.x Milestone (CKVS Agents & Sync)
- [ ] Document-graph sync detects changes and flags affected docs in <5 minutes
- [ ] Conflict resolution workflow presents conflicts with sufficient context for decision
- [ ] Knowledge-aware Tuning Agent resolves 80% of violations without human intervention
- [ ] Workflows with validation steps reduce published errors by >70%

---

## Appendix: Spec File Naming Convention

To integrate CKVS into existing spec structure, use this naming:

```
docs/specs/v0.4.x/v0.4.5/
  LCS-SBD-045.md         # (Existing: Embedder)
  LCS-SBD-045-KG.md      # (New: Knowledge Graph Foundation)
  LCS-DES-045-KG-a.md    # Graph Database Integration
  LCS-DES-045-KG-b.md    # Schema Registry Service
  LCS-DES-045-KG-c.md    # Entity Abstraction Layer
  LCS-DES-045-KG-INDEX.md

docs/specs/v0.5.x/v0.5.5/
  LCS-SBD-055.md         # (Existing: Filter System)
  LCS-SBD-055-NLU.md     # (New: Entity Linking)
  LCS-DES-055-NLU-a.md   # Entity Recognizer
  LCS-DES-055-NLU-b.md   # Entity Linker
  LCS-DES-055-NLU-INDEX.md
```

Suffix `-KG` for Knowledge Graph components, `-NLU` for Natural Language Understanding, `-VAL` for Validation.

---

## Appendix: Revised v0.4.x Roadmap Snippet

Here's how v0.4.x roadmap would be modified:

```markdown
## v0.4.5: The Embedder (Existing) + Knowledge Graph Foundation (NEW)

### Existing Components (v0.4.5a-d): Vector Generation
[...existing embedder implementation...]

### Knowledge Graph Components (v0.4.5e-g): CKVS Foundation

*   **v0.4.5e:** **Graph Database Integration.** Add Neo4j to Docker Compose:
    *   Docker image: `neo4j:5.x-community` with APOC plugin
    *   Connection via Bolt protocol (port 7687)
    *   Health check on startup
    *   Migration: `Migration_005_KnowledgeGraphMeta.cs` for connection config table

*   **v0.4.5f:** **Schema Registry Service.** Define ontological schema:
    *   Schema YAML in `.lexichord/knowledge/schema/technical-docs.yaml`
    *   Entity types: Product, Component, Endpoint, Parameter, Response
    *   Relationship types: CONTAINS, EXPOSES, ACCEPTS, RETURNS, REQUIRES
    *   `ISchemaRegistry` interface for validation

*   **v0.4.5g:** **Basic Entity Extraction.** Identify entities in Markdown:
    *   Regex-based extractor for API endpoints (`/users`, `POST /auth`)
    *   Parameter name extraction from code blocks
    *   `IEntityExtractor.Extract(text) → List[EntityMention]`
    *   Confidence scoring: Regex matches = 1.0, heuristics = 0.7

[...continue with v0.4.6...]
```

---

## Conclusion

This integration strategy balances ambition with pragmatism:

1. **Incremental**: CKVS features integrate across v0.4.x-v0.7.x, avoiding big-bang risk
2. **Synergistic**: CKVS enhances RAG, agents, and style systems rather than competing
3. **Focused**: Defers worldbuilding and advanced features to stay on v1.0 timeline
4. **Tested**: Each phase has clear milestones before proceeding to next

**Recommendation**: Proceed with this plan. Begin with v0.4.5 Knowledge Graph Foundation as soon as v0.4.4 (Embedder) completes. Track progress against CKVS success criteria at each milestone.

**Next Steps**:
1. Review and approve this integration strategy
2. Create detailed specs for v0.4.5e-g (Knowledge Graph components)
3. Update project timeline with parallel CKVS workstream
4. Allocate resources: 1 developer for CKVS Foundation (v0.4.5-v0.4.7)
