# Changelog: v0.4.5g - Entity Abstraction Layer

**Release Date:** 2026-02-01
**Status:** Implemented
**Specification:** [LCS-DES-045-KG-c](../../specs/v0.4.x/v0.4.5/LCS-DES-v0.4.5-KG-c.md)

---

## Summary

Implements the Entity Abstraction Layer for the Knowledge Graph subsystem (CKVS Phase 1). This version adds the `IEntityExtractor` and `IEntityExtractionPipeline` abstraction interfaces, four extraction records (`EntityMention`, `ExtractionContext`, `ExtractionResult`, `AggregatedEntity`), three built-in regex-based extractors (`EndpointExtractor`, `ParameterExtractor`, `ConceptExtractor`), a `MentionAggregator` for entity deduplication, and an `EntityExtractionPipeline` that orchestrates extractors with priority ordering, error isolation, confidence filtering, mention deduplication, and entity aggregation.

---

## Changes

### Added

#### Lexichord.Abstractions/Contracts/

| File                             | Type      | Description                                                                      |
| :------------------------------- | :-------- | :------------------------------------------------------------------------------- |
| `ExtractionRecords.cs`           | Record    | EntityMention with type, value, offsets, confidence, properties, computed Length  |
| `ExtractionRecords.cs`           | Record    | ExtractionContext with DocumentId, MinConfidence, TargetEntityTypes, DiscoveryMode |
| `ExtractionRecords.cs`           | Record    | ExtractionResult with Mentions, AggregatedEntities, Duration, computed AverageConfidence |
| `ExtractionRecords.cs`           | Record    | AggregatedEntity with CanonicalValue, MergedProperties, SourceDocuments          |
| `IEntityExtractor.cs`            | Interface | IEntityExtractor with SupportedTypes, Priority, ExtractAsync                     |
| `IEntityExtractionPipeline.cs`   | Interface | IEntityExtractionPipeline with Register, ExtractAllAsync, ExtractFromChunksAsync |

#### Lexichord.Modules.Knowledge/

| File                                                     | Type           | Description                                                                  |
| :------------------------------------------------------- | :------------- | :--------------------------------------------------------------------------- |
| `Extraction/Extractors/EndpointExtractor.cs`             | Internal       | Regex-based API endpoint detection (3 patterns, confidence 0.7–1.0)          |
| `Extraction/Extractors/ParameterExtractor.cs`            | Internal       | Parameter detection from paths, queries, code, JSON (5 patterns, 0.6–1.0)   |
| `Extraction/Extractors/ConceptExtractor.cs`              | Internal       | Domain term detection (definitions, acronyms, glossary, capitalized terms)   |
| `Extraction/MentionAggregator.cs`                        | Internal       | Groups mentions by (type, normalizedValue) into AggregatedEntity records     |
| `Extraction/EntityExtractionPipeline.cs`                 | Implementation | IEntityExtractionPipeline with priority ordering, deduplication, aggregation |

#### Lexichord.Tests.Unit/

| File                                                            | Tests | Coverage                                                            |
| :-------------------------------------------------------------- | ----: | :------------------------------------------------------------------ |
| `Abstractions/Knowledge/ExtractionRecordsTests.cs`              |    25 | Record defaults, equality, computed properties, with-expressions    |
| `Modules/Knowledge/EndpointExtractorTests.cs`                   |    27 | Method+path, code block, standalone, false positives, normalization |
| `Modules/Knowledge/ParameterExtractorTests.cs`                  |    22 | Definitions, inline code, path/query params, JSON, deduplication   |
| `Modules/Knowledge/ConceptExtractorTests.cs`                    |    21 | Defined terms, acronyms, glossary, discovery mode, filtering       |
| `Modules/Knowledge/MentionAggregatorTests.cs`                   |    14 | Grouping, canonical values, property merging, ordering             |
| `Modules/Knowledge/EntityExtractionPipelineTests.cs`            |    19 | Multi-extractor, dedup, confidence filter, error isolation, chunks |

### Modified

| File                                          | Change                                                           |
| :-------------------------------------------- | :--------------------------------------------------------------- |
| `KnowledgeModule.cs`                          | Added EntityExtractionPipeline singleton DI registration (v0.4.5g), registers 3 built-in extractors |

---

## Technical Details

### Extraction Patterns

#### EndpointExtractor (Priority 100)

| Pattern             | Confidence | Example Matches                              |
| :------------------ | ---------: | :------------------------------------------- |
| HTTP method + path  |        1.0 | `GET /users/{id}`, `POST /api/v1/orders`     |
| Code block path     |        0.9 | `endpoint: /api/orders`, `url = "/users"`    |
| Standalone path     |        0.7 | `/users`, `/api/v1/orders`                   |

False positive filters: file extensions, Unix system paths (`/usr/`, `/etc/`, etc.), short paths (< 4 chars).

#### ParameterExtractor (Priority 90)

| Pattern              | Confidence | Example Matches                              |
| :------------------- | ---------: | :------------------------------------------- |
| Explicit definition  |        1.0 | `parameter limit`, `param offset (integer)`  |
| Path parameter       |       0.95 | `{userId}`, `{orderId}`                      |
| Inline code          |        0.9 | `` `limit` parameter ``, `` `email` field `` |
| Query parameter      |       0.85 | `?page=1`, `&limit=20`                       |
| JSON property        |        0.6 | `"email": "test@..."`, `"age": 25`           |

Meta-property filter: `type`, `name`, `id`, `version`, `description`, `title`, `status`, `code`, `message`, `error`, `data`, `result`, `success`, `count`, `total`, `items`, `value`, `key`.

#### ConceptExtractor (Priority 50)

| Pattern              | Confidence | Example Matches                                            |
| :------------------- | ---------: | :--------------------------------------------------------- |
| Acronym + definition |       0.95 | `API (Application Programming Interface)`                  |
| Defined term         |        0.9 | `called Rate Limiting`, `known as OAuth`                   |
| Glossary entry       |       0.85 | `**Rate Limiting**: Controls request frequency`            |
| Capitalized term     |        0.5 | `Service Mesh`, `Access Control` (discovery mode only)     |

Common word/phrase filters applied to all patterns.

### Pipeline Execution Flow

1. Filter extractors by `TargetEntityTypes` (if specified).
2. Run each extractor in priority order (descending).
3. Filter mentions by `MinConfidence` threshold.
4. Tag mentions with `ExtractorName` for traceability.
5. Deduplicate overlapping mentions (higher confidence wins).
6. Aggregate into `AggregatedEntity` records via `MentionAggregator`.

### Aggregation Algorithm

1. Group mentions by `{EntityType}::{NormalizedValue}` (case-insensitive).
2. Select highest-confidence mention as canonical representative.
3. Merge properties from all mentions (first-seen wins per key).
4. Collect distinct chunk IDs for provenance tracking.
5. Order by mention count descending, then max confidence descending.

---

## Verification

```bash
# Build Knowledge module
dotnet build src/Lexichord.Modules.Knowledge
# Result: Build succeeded

# Run v0.4.5g unit tests
dotnet test tests/Lexichord.Tests.Unit --filter "Feature=v0.4.5g"
# Result: 128 tests passed

# Run full Knowledge Graph regression
dotnet test tests/Lexichord.Tests.Unit --filter "Feature=v0.4.5e|Feature=v0.4.5f|Feature=v0.4.5g"
# Result: All tests passed
```

---

## Test Coverage

| Category                                            | Tests |
| :-------------------------------------------------- | ----: |
| EntityMention defaults/equality/computed/with        |     8 |
| ExtractionContext defaults/properties/equality       |     3 |
| ExtractionResult defaults/AverageConfidence          |     4 |
| AggregatedEntity defaults/properties/equality        |     4 |
| ExtractionRecords total                              |    19 |
| EndpointExtractor method+path (7 HTTP methods)       |     9 |
| EndpointExtractor code block paths                   |     4 |
| EndpointExtractor standalone paths                   |     2 |
| EndpointExtractor false positive filtering           |     4 |
| EndpointExtractor path normalization                 |     5 |
| EndpointExtractor deduplication                      |     1 |
| EndpointExtractor empty/whitespace/metadata          |     5 |
| ParameterExtractor explicit definitions              |     3 |
| ParameterExtractor inline code parameters            |     2 |
| ParameterExtractor path parameters                   |     2 |
| ParameterExtractor query parameters                  |     2 |
| ParameterExtractor JSON properties                   |     3 |
| ParameterExtractor dedup/empty/metadata              |     5 |
| ConceptExtractor defined terms                       |     3 |
| ConceptExtractor acronyms                            |     2 |
| ConceptExtractor glossary entries                    |     2 |
| ConceptExtractor capitalized terms (discovery mode)  |     3 |
| ConceptExtractor validation/filtering                |     4 |
| ConceptExtractor dedup/empty/metadata                |     4 |
| MentionAggregator single/multi grouping              |     4 |
| MentionAggregator canonical value/properties         |     2 |
| MentionAggregator source documents                   |     3 |
| MentionAggregator ordering/empty/case-insensitive    |     3 |
| Pipeline extractor registration/ordering             |     3 |
| Pipeline multi-extractor combining                   |     1 |
| Pipeline mention deduplication                       |     2 |
| Pipeline confidence/type filtering                   |     2 |
| Pipeline error isolation                             |     1 |
| Pipeline ExtractFromChunksAsync                      |     4 |
| Pipeline aggregation/statistics/tagging              |     3 |
| Pipeline empty/duration                              |     2 |
| **Total**                                            | **128** |

---

## Dependencies

- v0.4.5f: ISchemaRegistry (optional schema validation in ExtractionContext)
- v0.4.5e: KnowledgeEntity, KnowledgeRelationship (graph node/edge records)
- v0.4.3a: TextChunk, ChunkMetadata (chunk-based extraction)
- v0.0.3b: ILogger<T> (structured logging)

## Dependents

- v0.4.7+: Entity Browser (uses extraction results for display)
- v0.5.5: Entity Linking (uses extractors for NLU-based recognition)

---

## Related Documents

- [LCS-DES-045-KG-c](../../specs/v0.4.x/v0.4.5/LCS-DES-v0.4.5-KG-c.md) - Design specification
- [LCS-SBD-045-KG §4.3](../../specs/v0.4.x/v0.4.5/LCS-SBD-v0.4.5-KG.md#43-v045g-entity-abstraction-layer) - Scope breakdown
- [LCS-DES-045-KG-INDEX](../../specs/v0.4.x/v0.4.5/LCS-DES-v0.4.5-KG-INDEX.md) - Knowledge Graph specs index
- [LCS-CL-v0.4.5f](./LCS-CL-v0.4.5f.md) - Previous version (Schema Registry Service)
