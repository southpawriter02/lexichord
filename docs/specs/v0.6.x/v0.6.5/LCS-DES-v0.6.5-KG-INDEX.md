# LCS-DES-065-KG-INDEX: Validation Engine — Design Specifications Index

## Document Control

| Field | Value |
| :--- | :--- |
| **Index ID** | LCS-DES-065-KG-INDEX |
| **System Breakdown** | LCS-SBD-065-KG |
| **Version** | v0.6.5 |
| **Codename** | Validation Engine (CKVS Phase 3a) |
| **Status** | Draft |
| **Last Updated** | 2026-01-31 |

---

## 1. Overview

This index catalogs all design specifications for the **Validation Engine** component of CKVS Phase 3a. The Validation Engine is the core component where all CKVS infrastructure converges to provide actionable validation feedback to technical writers.

---

## 2. Sub-Part Specifications

| Spec ID | Title | Est. Hours | Description |
| :------ | :---- | :--------- | :---------- |
| [LCS-DES-065-KG-e](LCS-DES-065-KG-e.md) | Validation Orchestrator | 6 | Central coordination point for all validators |
| [LCS-DES-065-KG-f](LCS-DES-065-KG-f.md) | Schema Validator | 5 | Validates entities against type schemas |
| [LCS-DES-065-KG-g](LCS-DES-065-KG-g.md) | Axiom Validator | 8 | Validates claims against axiom rules |
| [LCS-DES-065-KG-h](LCS-DES-065-KG-h.md) | Consistency Checker | 8 | Detects contradictions with existing knowledge |
| [LCS-DES-065-KG-i](LCS-DES-065-KG-i.md) | Validation Result Aggregator | 4 | Combines results, deduplicates, suggests fixes |
| [LCS-DES-065-KG-j](LCS-DES-065-KG-j.md) | Linter Integration | 4 | Unified workflow with Lexichord style linter |
| **Total** | | **35** | |

---

## 3. Architecture Diagram

```mermaid
graph TB
    subgraph "Input Layer"
        DOC[Document]
        CLM[Extracted Claims]
        ENT[Linked Entities]
    end

    subgraph "Orchestration Layer"
        VE[ValidationEngine<br/>LCS-DES-065-KG-e]
        VR[ValidatorRegistry]
        VP[ValidationPipeline]
    end

    subgraph "Validation Layer"
        SV[SchemaValidator<br/>LCS-DES-065-KG-f]
        AV[AxiomValidator<br/>LCS-DES-065-KG-g]
        CC[ConsistencyChecker<br/>LCS-DES-065-KG-h]
    end

    subgraph "Aggregation Layer"
        RA[ResultAggregator<br/>LCS-DES-065-KG-i]
        FD[FindingDeduplicator]
        FC[FixConsolidator]
    end

    subgraph "Integration Layer"
        LI[LinterIntegration<br/>LCS-DES-065-KG-j]
        UFA[UnifiedFindingAdapter]
        CFW[CombinedFixWorkflow]
    end

    subgraph "Knowledge Sources"
        SR[(Schema Registry)]
        AX[(Axiom Store)]
        CS[(Claim Store)]
        KG[(Knowledge Graph)]
    end

    subgraph "Output"
        VRS[ValidationResult]
        URS[UnifiedFindingResult]
        FAA[FixAllAction]
    end

    DOC --> VE
    CLM --> VE
    ENT --> VE

    VE --> VR
    VE --> VP
    VR --> SV
    VR --> AV
    VR --> CC

    SV --> SR
    AV --> AX
    CC --> CS
    CC --> KG

    SV --> RA
    AV --> RA
    CC --> RA

    RA --> FD
    RA --> FC
    RA --> VRS

    VRS --> LI
    LI --> UFA
    LI --> CFW
    LI --> URS
    LI --> FAA

    style VE fill:#f59e0b
    style SV fill:#3b82f6
    style AV fill:#3b82f6
    style CC fill:#3b82f6
    style RA fill:#10b981
    style LI fill:#8b5cf6
```

---

## 4. Validation Flow

```mermaid
sequenceDiagram
    participant U as User
    participant VE as ValidationEngine
    participant VR as ValidatorRegistry
    participant SV as SchemaValidator
    participant AV as AxiomValidator
    participant CC as ConsistencyChecker
    participant RA as ResultAggregator
    participant LI as LinterIntegration

    U->>VE: ValidateDocumentAsync(doc, options)
    VE->>VR: GetValidatorsForMode(mode, license)
    VR-->>VE: [validators]

    par Parallel Validation
        VE->>SV: ValidateAsync(context)
        SV-->>VE: schema findings
    and
        VE->>AV: ValidateAsync(context)
        AV-->>VE: axiom findings
    and
        VE->>CC: ValidateAsync(context)
        CC-->>VE: consistency findings
    end

    VE->>RA: Aggregate(findings, duration)
    RA-->>VE: ValidationResult

    VE->>LI: GetUnifiedFindingsAsync(doc)
    LI-->>VE: UnifiedFindingResult

    VE-->>U: Combined Results + Fixes
```

---

## 5. Key Interfaces

### 5.1 IValidationEngine

```csharp
public interface IValidationEngine
{
    Task<ValidationResult> ValidateDocumentAsync(
        Document document,
        ValidationOptions options,
        CancellationToken ct = default);

    Task<ValidationResult> ValidateClaimsAsync(
        IReadOnlyList<Claim> claims,
        ValidationOptions options,
        CancellationToken ct = default);

    Task<ValidationResult> ValidateGeneratedContentAsync(
        string content,
        GenerationContext context,
        CancellationToken ct = default);

    IAsyncEnumerable<ValidationFinding> ValidateStreamingAsync(
        string content,
        ValidationOptions options,
        CancellationToken ct = default);
}
```

### 5.2 IValidator

```csharp
public interface IValidator
{
    string Name { get; }
    int Priority { get; }
    LicenseTier RequiredTier { get; }
    bool SupportsStreaming { get; }

    Task<IReadOnlyList<ValidationFinding>> ValidateAsync(
        ValidationContext context,
        CancellationToken ct = default);

    IAsyncEnumerable<ValidationFinding> ValidateStreamingAsync(
        ValidationContext context,
        CancellationToken ct = default);
}
```

### 5.3 ILinterIntegration

```csharp
public interface ILinterIntegration
{
    Task<UnifiedFindingResult> GetUnifiedFindingsAsync(
        Document document,
        UnifiedFindingOptions options,
        CancellationToken ct = default);

    Task<FixResult> ApplyUnifiedFixAsync(
        UnifiedFix fix,
        Document document,
        CancellationToken ct = default);

    Task<FixAllResult> ApplyAllFixesAsync(
        Document document,
        FixAllOptions options,
        CancellationToken ct = default);
}
```

---

## 6. Validation Modes

| Mode | Latency Target | Validators | Use Case |
| :--- | :------------- | :--------- | :------- |
| **RealTime** | <100ms | Schema only | As user types |
| **OnSave** | <500ms | Schema + Axiom | Document save |
| **Full** | <5s | All validators | Pre-publish |
| **Streaming** | Per-chunk | Schema + Axiom | LLM generation |

---

## 7. Finding Severity Levels

| Severity | Description | Example |
| :------- | :---------- | :------ |
| **Error** | Blocks publication | Missing required property |
| **Warning** | Should be addressed | Axiom violation (SHOULD) |
| **Info** | Informational note | Deprecated entity usage |
| **Hint** | Style suggestion | Consider adding description |

---

## 8. Validation Finding Codes

### Schema Validator Codes

| Code | Description |
| :--- | :---------- |
| `SCHEMA_REQUIRED_PROPERTY` | Required property missing |
| `SCHEMA_TYPE_MISMATCH` | Property type incorrect |
| `SCHEMA_INVALID_ENUM` | Invalid enum value |
| `SCHEMA_CONSTRAINT` | Constraint violated |
| `SCHEMA_PATTERN_MISMATCH` | Pattern not matched |

### Axiom Validator Codes

| Code | Description |
| :--- | :---------- |
| `AXIOM_VIOLATION` | General axiom violation |
| `AXIOM_PROPERTY_CONSTRAINT` | Property constraint violated |
| `AXIOM_RELATIONSHIP_INVALID` | Invalid relationship types |
| `AXIOM_CARDINALITY` | Cardinality constraint violated |
| `AXIOM_MUTUAL_EXCLUSION` | Mutually exclusive properties |

### Consistency Checker Codes

| Code | Description |
| :--- | :---------- |
| `CONSISTENCY_CONFLICT` | General conflict |
| `CONSISTENCY_VALUE_CONTRADICTION` | Conflicting values |
| `CONSISTENCY_PROPERTY_CONFLICT` | Property value conflict |
| `CONSISTENCY_RELATIONSHIP_CONFLICT` | Relationship contradiction |
| `CONSISTENCY_SEMANTIC_CONFLICT` | Semantic contradiction |

---

## 9. Performance Targets

| Metric | Target | Measurement |
| :----- | :----- | :---------- |
| Real-time validation | <100ms | P95 timing |
| On-save validation | <500ms | P95 timing |
| Full validation | <5s | P95 timing |
| Axiom violation recall | >95% | Test corpus |
| False positive rate | <10% | Human evaluation |

---

## 10. Dependencies

| Component | From | Description |
| :-------- | :--- | :---------- |
| `ISchemaRegistry` | v0.4.5f | Entity type schemas |
| `IAxiomStore` | v0.4.6-KG | Axiom rules |
| `IClaimRepository` | v0.5.6-KG | Existing claims |
| `IGraphRepository` | v0.4.5e | Entity data |
| `IClaimDiffService` | v0.5.6-KG | Semantic comparison |
| `ILinterService` | v0.3.x | Style/grammar linter |

---

## 11. License Gating Summary

| Component | Core | WriterPro | Teams | Enterprise |
| :-------- | :--- | :-------- | :---- | :--------- |
| ValidationEngine | ❌ | Schema only | Full | Full |
| SchemaValidator | ❌ | ✅ | ✅ | ✅ |
| AxiomValidator | ❌ | ❌ | ✅ | ✅ |
| ConsistencyChecker | ❌ | ❌ | ✅ | ✅ |
| LinterIntegration | Linter only | Partial | Full | Full + Custom |

---

## 12. What This Enables

- **v0.6.6 Co-pilot:** Pre/post validation for AI-generated content
- **v0.7.5 Unified Validation:** Combined style + knowledge validation
- **Publication Gates:** Block publishing of invalid documentation
- **Continuous Monitoring:** Track documentation health over time

---

## 13. Changelog

| Version | Date | Author | Changes |
| :------ | :--- | :----- | :------ |
| 1.0 | 2026-01-31 | Lead Architect | Initial creation |

---
