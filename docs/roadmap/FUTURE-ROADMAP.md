# Lexichord Future Roadmap — CKVS Beyond v0.10.x

This document captures CKVS features planned for future versions beyond v0.10.x. These represent advanced capabilities that build upon the foundation established in Phases 1-5.

**Status:** Planning / Research
**Target:** v0.11.x and beyond
**Last Updated:** 2026-01-31

---

## Overview

The following features have been identified as valuable extensions to the Canonical Knowledge Validation System but are not yet scheduled for implementation. They are organized by category and prioritized based on user demand and strategic value.

---

## 1. Temporal Knowledge (High Priority)

**Problem:** The current CKVS cannot represent time-varying facts. For example, "CEO of Acme was Smith from 2020-2023, then Jones from 2024-present" cannot be modeled.

**Proposed Solution:**

```csharp
public record TemporalFact
{
    public Guid FactId { get; init; }
    public Guid EntityId { get; init; }
    public string PropertyName { get; init; }
    public object Value { get; init; }
    public DateTimeOffset ValidFrom { get; init; }
    public DateTimeOffset? ValidTo { get; init; }
    public TemporalType Type { get; init; }
}

public enum TemporalType
{
    ValidTime,      // When fact is true in the real world
    TransactionTime // When fact was recorded in the system
}

public interface ITemporalQueryService
{
    Task<IReadOnlyList<TemporalFact>> GetFactHistoryAsync(
        Guid entityId,
        string propertyName,
        CancellationToken ct = default);

    Task<object?> GetFactAtTimeAsync(
        Guid entityId,
        string propertyName,
        DateTimeOffset pointInTime,
        CancellationToken ct = default);
}
```

**Use Cases:**
- Track organizational changes over time
- Historical API versioning documentation
- Regulatory compliance with temporal requirements

**Estimated Effort:** 35-45 hours
**Dependencies:** v0.10.1-KG (Graph Versioning)

---

## 2. Confidence Scoring for Claims (High Priority)

**Problem:** All claims extracted from documents are treated equally, regardless of source reliability or extraction confidence.

**Proposed Solution:**

```csharp
public record ScoredClaim
{
    public Guid ClaimId { get; init; }
    public string ClaimText { get; init; }
    public float ExtractionConfidence { get; init; }  // How confident the extractor is
    public float SourceReliability { get; init; }     // How reliable the source document is
    public float CorroborationScore { get; init; }    // How many other sources agree
    public float OverallConfidence { get; init; }     // Weighted combination
    public IReadOnlyList<ConfidenceSignal> Signals { get; init; }
}

public record ConfidenceSignal
{
    public string SignalType { get; init; }
    public float Weight { get; init; }
    public float Score { get; init; }
    public string? Explanation { get; init; }
}

public interface IClaimConfidenceService
{
    Task<float> CalculateConfidenceAsync(Guid claimId, CancellationToken ct = default);
    Task<IReadOnlyList<ScoredClaim>> GetClaimsByConfidenceAsync(
        float minConfidence,
        CancellationToken ct = default);
}
```

**Confidence Signals:**
- Extraction model confidence score
- Document authority (official docs vs. notes)
- Claim corroboration (multiple sources)
- Recency (newer = more confident)
- Author expertise level

**Use Cases:**
- Prioritize validation of low-confidence claims
- Filter knowledge graph by reliability
- Highlight uncertain information in UI

**Estimated Effort:** 25-35 hours
**Dependencies:** v0.5.6-KG (Claim Extraction)

---

## 3. External Knowledge Integration (Medium Priority)

**Problem:** CKVS operates in isolation without connection to external knowledge bases that could enrich or validate documentation.

**Proposed Solution:**

```csharp
public interface IExternalKnowledgeConnector
{
    string ConnectorId { get; }
    string DisplayName { get; }

    Task<IReadOnlyList<ExternalEntity>> SearchAsync(
        string query,
        int limit = 10,
        CancellationToken ct = default);

    Task<ExternalEntity?> GetByIdAsync(
        string externalId,
        CancellationToken ct = default);

    Task<IReadOnlyList<ExternalFact>> GetFactsAboutAsync(
        string externalId,
        CancellationToken ct = default);
}

public record ExternalEntity
{
    public string ExternalId { get; init; }
    public string Source { get; init; }  // "wikidata", "dbpedia", etc.
    public string Label { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<string> Aliases { get; init; }
    public string? Url { get; init; }
}

// Built-in connectors
public class WikidataConnector : IExternalKnowledgeConnector { }
public class DBpediaConnector : IExternalKnowledgeConnector { }
public class SchemaOrgConnector : IExternalKnowledgeConnector { }
```

**Features:**
- Link local entities to Wikidata/DBpedia entries
- Enrich entities with external properties
- Cross-reference claims against authoritative sources
- Import standard vocabularies (Schema.org, etc.)

**Use Cases:**
- Technical documentation linked to official standards
- Company entities linked to public records
- Terminology aligned with industry ontologies

**Estimated Effort:** 50-60 hours
**Dependencies:** v0.10.5-KG (Import/Export), v0.10.3-KG (Entity Resolution)

---

## 4. Active Learning / Feedback Loop (Medium Priority)

**Problem:** When users correct validation errors or disambiguation choices, the system doesn't learn from these corrections.

**Proposed Solution:**

```csharp
public interface IFeedbackLearningService
{
    Task RecordCorrectionAsync(
        CorrectionFeedback feedback,
        CancellationToken ct = default);

    Task<ModelUpdateResult> RetrainAsync(
        RetrainOptions options,
        CancellationToken ct = default);

    Task<IReadOnlyList<LearningInsight>> GetInsightsAsync(
        CancellationToken ct = default);
}

public record CorrectionFeedback
{
    public CorrectionType Type { get; init; }
    public Guid OriginalId { get; init; }
    public object OriginalValue { get; init; }
    public object CorrectedValue { get; init; }
    public string? UserExplanation { get; init; }
    public DateTimeOffset CorrectedAt { get; init; }
    public string CorrectedBy { get; init; }
}

public enum CorrectionType
{
    EntityExtraction,
    ClaimExtraction,
    Disambiguation,
    ValidationFalsePositive,
    ValidationFalseNegative,
    RelationshipExtraction
}
```

**Learning Targets:**
- Entity extraction patterns
- Claim extraction rules
- Disambiguation preferences
- Validation rule thresholds
- Relationship inference weights

**Use Cases:**
- Reduce false positives over time
- Adapt to domain-specific terminology
- Improve extraction accuracy

**Estimated Effort:** 60-80 hours
**Dependencies:** v0.10.3-KG (Entity Resolution), v0.6.5-KG (Validation Engine)

---

## 5. Deep Provenance Chain (Medium Priority)

**Problem:** Claims track source documents, but there's no deep lineage showing who created an entity, what document first established a fact, and the full audit trail of changes.

**Proposed Solution:**

```csharp
public record ProvenanceRecord
{
    public Guid RecordId { get; init; }
    public Guid TargetId { get; init; }
    public ProvenanceTargetType TargetType { get; init; }
    public ProvenanceAction Action { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public string? ActorId { get; init; }
    public string? ActorType { get; init; }  // "user", "system", "sync", "inference"
    public Guid? SourceDocumentId { get; init; }
    public int? SourceLineNumber { get; init; }
    public Guid? CausedById { get; init; }  // Previous provenance record
    public JsonDocument? Metadata { get; init; }
}

public interface IProvenanceService
{
    Task<ProvenanceChain> GetProvenanceAsync(
        Guid entityId,
        ProvenanceOptions options,
        CancellationToken ct = default);

    Task<IReadOnlyList<ProvenanceRecord>> GetRootSourcesAsync(
        Guid factId,
        CancellationToken ct = default);

    Task<ProvenanceReport> GenerateReportAsync(
        Guid entityId,
        ReportOptions options,
        CancellationToken ct = default);
}
```

**Tracked Events:**
- Entity first creation
- Property additions/modifications
- Relationship establishment
- Claim attribution
- Validation actions
- User corrections

**Use Cases:**
- Compliance audits
- Source attribution
- Change accountability
- Impact analysis

**Estimated Effort:** 40-50 hours
**Dependencies:** v0.10.1-KG (Graph Versioning)

---

## 6. Multi-Language Support (Low Priority)

**Problem:** No explicit handling of entities/claims in multiple languages or cross-language entity resolution.

**Proposed Solution:**

```csharp
public record LocalizedEntity
{
    public Guid EntityId { get; init; }
    public string DefaultLanguage { get; init; }
    public IReadOnlyDictionary<string, LocalizedProperties> Translations { get; init; }
}

public record LocalizedProperties
{
    public string LanguageCode { get; init; }  // ISO 639-1
    public string Label { get; init; }
    public string? Description { get; init; }
    public IReadOnlyDictionary<string, string>? AdditionalProperties { get; init; }
}

public interface IMultiLanguageService
{
    Task<LocalizedEntity> GetLocalizedAsync(
        Guid entityId,
        string languageCode,
        CancellationToken ct = default);

    Task AddTranslationAsync(
        Guid entityId,
        string languageCode,
        LocalizedProperties properties,
        CancellationToken ct = default);

    Task<IReadOnlyList<Guid>> FindCrossLanguageMatchesAsync(
        Guid entityId,
        CancellationToken ct = default);
}
```

**Features:**
- Store entity labels/descriptions in multiple languages
- Cross-language entity resolution
- Language-specific validation rules
- Multilingual search

**Use Cases:**
- International documentation teams
- Localized product documentation
- Multi-market API docs

**Estimated Effort:** 45-55 hours
**Dependencies:** v0.10.3-KG (Entity Resolution)

---

## 7. Public API / SDK (Low Priority)

**Problem:** No documented REST/GraphQL API for external tools to query or update the knowledge graph programmatically.

**Proposed Solution:**

```csharp
// REST API Endpoints
// GET    /api/v1/entities
// GET    /api/v1/entities/{id}
// POST   /api/v1/entities
// PUT    /api/v1/entities/{id}
// DELETE /api/v1/entities/{id}
// GET    /api/v1/entities/{id}/relationships
// GET    /api/v1/entities/{id}/claims
// POST   /api/v1/query (CKVS-QL)
// GET    /api/v1/search?q={query}
// POST   /api/v1/validate
// GET    /api/v1/graph/export
// POST   /api/v1/graph/import

// GraphQL Schema
type Entity {
    id: ID!
    type: String!
    name: String!
    properties: JSON
    relationships(type: String): [Relationship!]!
    claims: [Claim!]!
}

type Query {
    entity(id: ID!): Entity
    entities(type: String, limit: Int): [Entity!]!
    search(query: String!): SearchResult!
    path(from: ID!, to: ID!, maxDepth: Int): [Path!]!
}

type Mutation {
    createEntity(input: EntityInput!): Entity!
    updateEntity(id: ID!, input: EntityInput!): Entity!
    deleteEntity(id: ID!): Boolean!
    mergeEntities(primary: ID!, secondary: [ID!]!): Entity!
}
```

**SDK Features:**
- .NET SDK with strongly-typed clients
- Python SDK for data science workflows
- TypeScript SDK for web integrations
- CLI tool for scripting

**Use Cases:**
- CI/CD integration beyond workflows
- Custom dashboards and reports
- Third-party tool integration
- Automated documentation pipelines

**Estimated Effort:** 70-90 hours
**Dependencies:** All v0.10.x features

---

## 8. Knowledge Graph Analytics (Future)

**Problem:** No analytics dashboards for understanding knowledge graph health, growth, and usage patterns.

**Proposed Features:**
- Graph growth over time
- Entity type distribution
- Relationship density metrics
- Orphan entity detection
- Claim coverage analysis
- Validation trend tracking
- User activity patterns

**Estimated Effort:** 40-50 hours

---

## 9. Collaborative Knowledge Editing (Future)

**Problem:** No real-time collaboration features for teams editing the knowledge graph simultaneously.

**Proposed Features:**
- Real-time presence indicators
- Concurrent edit conflict resolution
- Entity-level locking
- Change notifications
- Comment threads on entities

**Estimated Effort:** 60-80 hours

---

## Priority Summary

| Feature | Priority | Est. Hours | Target |
|:--------|:---------|:-----------|:-------|
| Temporal Knowledge | High | 35-45 | v0.11.1 |
| Confidence Scoring | High | 25-35 | v0.11.2 |
| External Knowledge | Medium | 50-60 | v0.11.3 |
| Active Learning | Medium | 60-80 | v0.11.4 |
| Deep Provenance | Medium | 40-50 | v0.11.5 |
| Multi-Language | Low | 45-55 | v0.12.x |
| Public API/SDK | Low | 70-90 | v0.12.x |
| Analytics | Future | 40-50 | v0.13.x |
| Collaboration | Future | 60-80 | v0.13.x |

**Total Future Roadmap:** ~425-545 hours (~11-14 person-months)

---

## Contributing

Feature requests and feedback welcome! Please submit issues with:
- Clear use case description
- Expected user benefit
- Rough implementation thoughts
- Priority suggestion

---
