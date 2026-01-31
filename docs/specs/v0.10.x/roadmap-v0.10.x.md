# Lexichord Roadmap (v0.10.1 - v0.10.5)

In v0.9.x, we completed "The Premiere" — the final hardening phase with user profiles, licensing, auto-updates, and security safeguards. In v0.10.x, we deliver **Advanced Knowledge Graph** capabilities that extend CKVS beyond validation into a comprehensive knowledge management platform.

**Architectural Note:** This version focuses entirely on CKVS Phase 5, building upon the foundation established in Phases 1-4 (v0.4.x through v0.7.x). All features extend the `Lexichord.Knowledge` module.

**Total Sub-Parts:** 31 distinct implementation steps across 5 versions.
**Total Estimated Hours:** 214 hours (~5.4 person-months)

---

## Version Overview

| Version | Codename | Focus | Est. Hours |
|:--------|:---------|:------|:-----------|
| v0.10.1-KG | Knowledge Graph Versioning | Time-travel, snapshots, branching | 44 |
| v0.10.2-KG | Inference Engine | Automated reasoning, derived facts | 43 |
| v0.10.3-KG | Entity Resolution | Disambiguation, deduplication | 38 |
| v0.10.4-KG | Graph Visualization & Search | Visual explorer, CKVS-QL, semantic search | 44 |
| v0.10.5-KG | Knowledge Import/Export | OWL, RDF, JSON-LD interoperability | 45 |

---

## v0.10.1-KG: Knowledge Graph Versioning

**Goal:** Track changes to the knowledge graph over time, enabling rollback, time-travel queries, and branching for experimental changes.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.10.1e | Version Store | 8 |
| v0.10.1f | Change Tracking | 6 |
| v0.10.1g | Time-Travel Queries | 8 |
| v0.10.1h | Snapshot Manager | 6 |
| v0.10.1i | Branch/Merge | 10 |
| v0.10.1j | Version History UI | 6 |

### Key Interfaces

```csharp
public interface IGraphVersionService
{
    Task<GraphVersion> GetCurrentVersionAsync(CancellationToken ct = default);
    Task<IGraphSnapshot> GetSnapshotAsync(GraphVersionRef versionRef, CancellationToken ct = default);
    Task<GraphSnapshot> CreateSnapshotAsync(string name, string? description = null, CancellationToken ct = default);
    Task<RollbackResult> RollbackAsync(GraphVersionRef targetVersion, RollbackOptions options, CancellationToken ct = default);
    Task<IReadOnlyList<GraphChange>> GetHistoryAsync(HistoryQuery query, CancellationToken ct = default);
}

public interface IGraphBranchService
{
    Task<GraphBranch> CreateBranchAsync(string branchName, string? description = null, CancellationToken ct = default);
    Task SwitchBranchAsync(string branchName, CancellationToken ct = default);
    Task<MergeResult> MergeBranchAsync(string sourceBranch, MergeOptions options, CancellationToken ct = default);
}
```

### License Gating

| Tier | Features |
|:-----|:---------|
| Core | Not available |
| WriterPro | View history (30 days) |
| Teams | Full versioning + rollback |
| Enterprise | Branching + unlimited history |

---

## v0.10.2-KG: Inference Engine

**Goal:** Automatically derive new facts from existing knowledge through forward-chaining inference rules.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.10.2e | Inference Rule Language | 8 |
| v0.10.2f | Rule Compiler | 6 |
| v0.10.2g | Forward Chainer | 10 |
| v0.10.2h | Incremental Inference | 8 |
| v0.10.2i | Provenance Tracker | 6 |
| v0.10.2j | Inference UI | 5 |

### Key Interfaces

```csharp
public interface IInferenceEngine
{
    Task<InferenceResult> InferAsync(InferenceOptions options, CancellationToken ct = default);
    Task<InferenceResult> InferIncrementalAsync(IReadOnlyList<GraphChange> changes, CancellationToken ct = default);
    Task<DerivationExplanation?> ExplainAsync(Guid factId, CancellationToken ct = default);
}
```

### Rule DSL Example

```
RULE "Service Dependency Detection"
WHEN
    ?service1 -[CALLS]-> ?endpoint
    ?endpoint -[DEFINED_IN]-> ?service2
    ?service1 != ?service2
THEN
    DERIVE ?service1 -[DEPENDS_ON]-> ?service2
```

### License Gating

| Tier | Features |
|:-----|:---------|
| Core | Not available |
| WriterPro | Built-in rules only |
| Teams | Custom rules (up to 50) |
| Enterprise | Unlimited rules + API |

---

## v0.10.3-KG: Entity Resolution

**Goal:** Handle disambiguation for uncertain entity matches and deduplicate entities representing the same concept.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.10.3e | Disambiguation Service | 6 |
| v0.10.3f | Duplicate Detector | 8 |
| v0.10.3g | Entity Merger | 8 |
| v0.10.3h | Resolution Learning | 6 |
| v0.10.3i | Bulk Resolution UI | 6 |
| v0.10.3j | Resolution Audit | 4 |

### Key Interfaces

```csharp
public interface IDisambiguationService
{
    Task<DisambiguationResult> GetCandidatesAsync(EntityMention mention, DisambiguationOptions options, CancellationToken ct = default);
    Task RecordChoiceAsync(Guid mentionId, Guid chosenEntityId, DisambiguationFeedback feedback, CancellationToken ct = default);
}

public interface IDuplicateDetector
{
    Task<DuplicateScanResult> ScanForDuplicatesAsync(DuplicateScanOptions options, CancellationToken ct = default);
    Task<IReadOnlyList<DuplicateCandidate>> FindDuplicatesOfAsync(Guid entityId, CancellationToken ct = default);
}

public interface IEntityMerger
{
    Task<MergePreview> PreviewMergeAsync(Guid primaryEntityId, IReadOnlyList<Guid> secondaryEntityIds, MergeOptions options, CancellationToken ct = default);
    Task<MergeResult> MergeAsync(Guid primaryEntityId, IReadOnlyList<Guid> secondaryEntityIds, MergeOptions options, CancellationToken ct = default);
}
```

### License Gating

| Tier | Features |
|:-----|:---------|
| Core | Not available |
| WriterPro | Disambiguation only |
| Teams | Full + bulk operations |
| Enterprise | Learning + API |

---

## v0.10.4-KG: Graph Visualization & Search

**Goal:** Provide interactive visualization, path-finding, structured queries, and semantic search for the knowledge graph.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.10.4e | Graph Renderer | 10 |
| v0.10.4f | Path Finder | 6 |
| v0.10.4g | Query Language (CKVS-QL) | 10 |
| v0.10.4h | Semantic Search | 8 |
| v0.10.4i | Graph Export | 4 |
| v0.10.4j | Search UI | 6 |

### Key Interfaces

```csharp
public interface IGraphRenderer
{
    Task<GraphVisualization> RenderAsync(GraphRenderRequest request, CancellationToken ct = default);
    Task<GraphVisualization> RenderNeighborhoodAsync(Guid entityId, int depth, NeighborhoodOptions options, CancellationToken ct = default);
    Task<byte[]> ExportAsync(GraphVisualization visualization, ExportFormat format, ExportOptions options, CancellationToken ct = default);
}

public interface IGraphQueryService
{
    Task<QueryResult> QueryAsync(string query, QueryOptions options, CancellationToken ct = default);
    Task<IReadOnlyList<QuerySuggestion>> GetSuggestionsAsync(string partialQuery, int cursorPosition, CancellationToken ct = default);
}

public interface ISemanticGraphSearch
{
    Task<SemanticSearchResult> SearchAsync(string query, SemanticSearchOptions options, CancellationToken ct = default);
    Task<IReadOnlyList<SimilarEntity>> FindSimilarAsync(Guid entityId, int limit = 10, CancellationToken ct = default);
}
```

### CKVS-QL Examples

```sql
-- Find all authenticated endpoints
FIND Entity WHERE type = "Endpoint" AND requiresAuth = true

-- Path between services
FIND PATH FROM "UserService" TO "PaymentGateway" MAX 5

-- Service dependency count
FIND e1 -[DEPENDS_ON]-> e2 WHERE e1.type = "Service"
GROUP BY e1.name COUNT AS deps ORDER BY deps DESC
```

### License Gating

| Tier | Features |
|:-----|:---------|
| Core | Basic search |
| WriterPro | Visualization + path finding |
| Teams | Full + CKVS-QL |
| Enterprise | Export + API |

---

## v0.10.5-KG: Knowledge Import/Export

**Goal:** Enable interoperability with standard ontology formats (OWL, RDF, JSON-LD, SKOS).

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.10.5e | Format Parsers | 10 |
| v0.10.5f | Schema Mapper | 8 |
| v0.10.5g | Import Engine | 8 |
| v0.10.5h | Export Engine | 8 |
| v0.10.5i | Validation Layer | 6 |
| v0.10.5j | Import/Export UI | 5 |

### Key Interfaces

```csharp
public interface IKnowledgeImporter
{
    Task<ImportPreview> PreviewAsync(Stream content, ImportFormat format, ImportOptions options, CancellationToken ct = default);
    Task<ImportResult> ImportAsync(Stream content, ImportFormat format, ImportOptions options, SchemaMapping? mapping = null, CancellationToken ct = default);
}

public interface IKnowledgeExporter
{
    Task ExportAsync(Stream output, ExportFormat format, ExportOptions options, CancellationToken ct = default);
}

public interface ISchemaMappingService
{
    Task<SchemaMapping> DetectMappingsAsync(Stream content, ImportFormat format, CancellationToken ct = default);
}
```

### Supported Formats

| Format | Import | Export |
|:-------|:-------|:-------|
| OWL/XML | ✓ | ✓ |
| RDF/XML | ✓ | ✓ |
| Turtle | ✓ | ✓ |
| JSON-LD | ✓ | ✓ |
| SKOS | ✓ | ✓ |
| CSV | ✓ | ✓ |

### License Gating

| Tier | Features |
|:-----|:---------|
| Core | CSV export only |
| WriterPro | JSON-LD |
| Teams | All formats |
| Enterprise | Scheduled exports + API |

---

## Dependencies on Prior Versions

| Component | Source | Usage in v0.10.x |
|:----------|:-------|:-----------------|
| `IGraphRepository` | v0.4.5e | Core graph operations |
| `IAxiomStore` | v0.4.6-KG | Rule storage |
| `IEntityBrowser` | v0.4.7-KG | Entity management |
| `IEntityLinkingService` | v0.5.5-KG | Entity linking |
| `IValidationEngine` | v0.6.5-KG | Import validation |
| `ISyncService` | v0.7.6-KG | Version triggers |
| `IRagService` | v0.4.3 | Vector embeddings |

---

## MediatR Events Introduced

| Event | Version | Description |
|:------|:--------|:------------|
| `GraphVersionCreatedEvent` | v0.10.1 | New graph version created |
| `GraphRolledBackEvent` | v0.10.1 | Graph rolled back to previous version |
| `BranchCreatedEvent` | v0.10.1 | New graph branch created |
| `BranchMergedEvent` | v0.10.1 | Branch merged |
| `InferenceCompletedEvent` | v0.10.2 | Inference run completed |
| `FactsDerivedEvent` | v0.10.2 | New facts derived |
| `EntityDisambiguatedEvent` | v0.10.3 | Entity mention disambiguated |
| `EntitiesMergedEvent` | v0.10.3 | Duplicate entities merged |
| `KnowledgeImportedEvent` | v0.10.5 | Knowledge imported from file |
| `KnowledgeExportedEvent` | v0.10.5 | Knowledge exported to file |

---

## NuGet Packages Introduced

| Package | Version | Purpose |
|:--------|:--------|:--------|
| `dotNetRDF` | 3.x | RDF/OWL parsing and serialization |
| `Cytoscape.NET` | 1.x | Graph visualization |
| `Superpower` | 3.x | CKVS-QL parser |

---

## What This Enables

With v0.10.x complete, CKVS becomes a full-featured knowledge management platform:

- **Audit & Compliance:** Full history of all knowledge changes
- **Experimentation:** Branch knowledge for what-if scenarios
- **Intelligence:** Automatically discover relationships through inference
- **Quality:** No duplicate or ambiguous entities
- **Discovery:** Visual exploration and powerful search
- **Interoperability:** Work with industry-standard formats

---

## Total CKVS Investment

| Phase | Versions | Hours |
|:------|:---------|:------|
| Phase 1: Foundation | v0.4.5 - v0.4.7 | ~75 |
| Phase 2: Intelligence | v0.5.5 - v0.5.6 | ~50 |
| Phase 3: Validation | v0.6.5 - v0.6.6 | ~57 |
| Phase 4: Integration | v0.7.2, v0.7.5 - v0.7.7 | ~91 |
| Phase 5: Advanced | v0.10.1 - v0.10.5 | ~214 |
| **Total** | | **~487 hours (~12 person-months)** |

---
