# LCS-CL-056e: Changelog — Claim Data Model

## Metadata

| Field           | Value                        |
| :-------------- | :--------------------------- |
| **Document ID** | LCS-CL-056e                  |
| **Version**     | v0.5.6e                      |
| **Title**       | Claim Data Model             |
| **Module**      | `Lexichord.Abstractions`     |
| **Date**        | 2026-02-04                   |
| **Status**      | Completed                    |

---

## Summary

Implements the core data contracts for the Claim Extraction pipeline. Claims are structured assertions extracted from prose following subject-predicate-object triple structure. This sub-part defines records for claims, entities, objects, evidence, predicates, and extraction results.

---

## New Files

### Contracts (Lexichord.Abstractions/Contracts/Knowledge/Claims/)

- `Claim.cs`
  - Main claim record with subject, predicate, object, confidence, evidence, and validation status
  - License-gated: WriterPro tier (FeatureCode: CLM-01)
  - Includes `ToCanonicalForm()` method for deduplication

- `ClaimEntity.cs`
  - Entity reference within claims (subject or object position)
  - Factory methods: `Unresolved()`, `Resolved()`
  - Links to `KnowledgeEntity` for graph integration

- `ClaimObject.cs`
  - Claim object (entity reference or literal value)
  - Factory methods: `FromEntity()`, `FromString()`, `FromInt()`, `FromDecimal()`, `FromBool()`
  - Includes `ToCanonicalForm()` method

- `ClaimEvidence.cs`
  - Provenance record linking claims to source text
  - Properties: Sentence, Context, Offsets, Section, ChunkId, ExtractionMethod

- `ClaimPredicate.cs`
  - Static class with 26 standard predicate constants
  - Categories: Structural, Functional, Descriptive, Lifecycle
  - Includes `GetInverse()` method for bidirectional navigation

- `ClaimValidationStatus.cs`
  - Enum: Pending, Valid, Invalid, Conflict, Inconclusive

- `ClaimObjectType.cs`
  - Enum: Entity, Literal

- `ClaimExtractionMethod.cs`
  - Enum: PatternRule, SemanticRoleLabeling, DependencyParsing, LLMExtraction, Manual

- `ClaimRelation.cs`
  - Record with `ClaimRelationType` enum (DerivedFrom, Supports, Contradicts, etc.)
  - Properties: RelatedClaimId, RelationType, Confidence, Metadata

- `ClaimValidationMessage.cs`
  - Record with `ClaimMessageSeverity` enum (Info, Warning, Error, Critical)
  - Properties: Code, Message, AffectedFields, SuggestedFix

- `ClaimExtractionResult.cs`
  - Result container with Claims list, Stats, FailedSentences
  - Static `Empty` property for failed operations

- `ClaimExtractionStats.cs`
  - Aggregate metrics: TotalSentences, TotalClaims, ByPredicate counts

---

## Unit Tests

| Test File                 | Test Count |
| :------------------------ | :--------- |
| `ClaimDataModelTests.cs`  | 23         |
| **Total**                 | **23**     |

### Test Coverage

- **Claim**: Creation, ID auto-generation, canonical form (entity/literal objects)
- **ClaimEntity**: Unresolved/Resolved factory methods, null validation, linking confidence
- **ClaimObject**: All factory methods, type discrimination, canonical form, null handling
- **ClaimPredicate**: Inverse lookup, null for no-inverse predicates, All collection
- **ClaimExtractionResult**: Empty property, HasClaims/HasFailures flags
- **ClaimEvidence**: Length computation
- **ClaimRelation**: Default confidence

---

## API Additions

### Records

```csharp
public record Claim
{
    Guid Id { get; init; }
    ClaimEntity Subject { get; init; }
    string Predicate { get; init; }
    ClaimObject Object { get; init; }
    float Confidence { get; init; }
    ClaimEvidence? Evidence { get; init; }
    ClaimValidationStatus ValidationStatus { get; init; }
    IReadOnlyList<ClaimValidationMessage> ValidationMessages { get; init; }
    IReadOnlyList<ClaimRelation> RelatedClaims { get; init; }
    string ToCanonicalForm();
}

public record ClaimEntity
{
    Guid? EntityId { get; init; }
    string EntityType { get; init; }
    string SurfaceForm { get; init; }
    string NormalizedForm { get; init; }
    bool IsResolved { get; }
    static ClaimEntity Unresolved(string surfaceForm, string entityType, int startOffset, int endOffset);
    static ClaimEntity Resolved(string surfaceForm, KnowledgeEntity entity, float confidence, int startOffset, int endOffset);
}

public record ClaimObject
{
    ClaimObjectType Type { get; init; }
    ClaimEntity? Entity { get; init; }
    string? LiteralValue { get; init; }
    string? LiteralType { get; init; }
    string? Unit { get; init; }
    string ToCanonicalForm();
    static ClaimObject FromEntity(ClaimEntity entity);
    static ClaimObject FromString(string value);
    static ClaimObject FromInt(int value, string? unit = null);
    static ClaimObject FromDecimal(decimal value, string? unit = null);
    static ClaimObject FromBool(bool value);
}
```

### Enums

```csharp
public enum ClaimValidationStatus { Pending, Valid, Invalid, Conflict, Inconclusive }
public enum ClaimObjectType { Entity, Literal }
public enum ClaimExtractionMethod { PatternRule, SemanticRoleLabeling, DependencyParsing, LLMExtraction, Manual }
public enum ClaimRelationType { DerivedFrom, Supports, Contradicts, Updates, Supersedes, PartOf, EvidenceOf }
public enum ClaimMessageSeverity { Info, Warning, Error, Critical }
```

### Static Class

```csharp
public static class ClaimPredicate
{
    // Structural
    const string CONTAINS, BELONGS_TO, REFERENCES, REFERENCED_BY;
    // Functional
    const string ACCEPTS, ACCEPTED_BY, RETURNS, RETURNED_BY, REQUIRES, REQUIRED_BY, CALLS, CALLED_BY, THROWS, THROWN_BY;
    // Descriptive
    const string HAS_PROPERTY, HAS_DEFAULT, HAS_TYPE, HAS_CONSTRAINT, HAS_DESCRIPTION, HAS_EXAMPLE;
    // Lifecycle
    const string IS_DEPRECATED, REPLACED_BY, REPLACES, ALIAS_OF, EXTENDS, EXTENDED_BY;

    static IReadOnlyList<string> All { get; }
    static string? GetInverse(string predicate);
}
```

---

## Dependencies

| Dependency         | Version | Purpose                  |
| :----------------- | :------ | :----------------------- |
| `KnowledgeEntity`  | v0.4.5e | Entity resolution target |
| `RequiresLicense`  | v0.0.7  | License gating attribute |

---

## Notes

- All records use C# `required` modifier for mandatory fields
- Claims use ISO 8601 UTC timestamps (`DateTimeOffset`)
- `ToCanonicalForm()` enables claim deduplication by comparing normalized representations
- Predicate inverse mapping supports bidirectional relationship navigation
- License gating: WriterPro tier for type access, Teams tier for full validation features

---

## Document History

| Version | Date       | Author    | Changes                |
| :------ | :--------- | :-------- | :--------------------- |
| 1.0     | 2026-02-04 | Assistant | Initial implementation |
