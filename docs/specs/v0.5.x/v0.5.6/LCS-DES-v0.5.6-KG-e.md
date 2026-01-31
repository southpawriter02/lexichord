# LCS-DES-056-KG-e: Claim Data Model

## 1. Metadata & Categorization

| Field | Value |
| :--- | :--- |
| **Document ID** | LCS-DES-056-KG-e |
| **Feature ID** | KG-056e |
| **Feature Name** | Claim Data Model |
| **Target Version** | v0.5.6e |
| **Module Scope** | `Lexichord.Abstractions.Contracts.Knowledge` |
| **Swimlane** | Memory |
| **License Tier** | WriterPro (types), Teams (full) |
| **Feature Gate Key** | `knowledge.claims.enabled` |
| **Status** | Draft |
| **Last Updated** | 2026-01-31 |

---

## 2. Executive Summary

### 2.1 The Requirement

Claims are machine-readable assertions extracted from prose that can be validated against the Knowledge Graph and Axiom Store. Before we can extract, store, or validate claims, we need a well-defined data model that captures the subject-predicate-object triple structure along with provenance and confidence metadata.

### 2.2 The Proposed Solution

Define a comprehensive set of record types and enums that represent:

- **Claim**: A subject-predicate-object assertion with evidence
- **ClaimEntity**: An entity reference in a claim (subject or object)
- **ClaimObject**: The object of a claim (entity or literal)
- **ClaimEvidence**: Source text linking claim to document
- **ClaimPredicate**: Standard predicates for technical documentation

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

**Upstream Modules:**
- `Lexichord.Abstractions` — Core record types
- v0.5.5-KG: `LinkedEntity` — Entity linking results
- v0.4.5-KG: `KnowledgeEntity` — Graph entities

**NuGet Packages:**
- None (pure data model)

### 3.2 Module Placement

```
Lexichord.Abstractions/
├── Contracts/
│   ├── Knowledge/
│   │   ├── Claims/
│   │   │   ├── Claim.cs
│   │   │   ├── ClaimEntity.cs
│   │   │   ├── ClaimObject.cs
│   │   │   ├── ClaimEvidence.cs
│   │   │   ├── ClaimPredicate.cs
│   │   │   ├── ClaimValidationStatus.cs
│   │   │   └── ClaimExtractionResult.cs
```

### 3.3 Licensing Behavior

- **Load Behavior:** [x] Always — Records are value types, no licensing on model itself
- **Fallback Experience:** All tiers can reference types; extraction logic is gated elsewhere

---

## 4. Data Contract (The API)

### 4.1 Claim Record

```csharp
namespace Lexichord.Abstractions.Contracts.Knowledge.Claims;

/// <summary>
/// A claim (assertion) extracted from text.
/// Claims represent subject-predicate-object triples that can be validated.
/// </summary>
public record Claim
{
    /// <summary>
    /// Unique claim identifier.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The subject entity (who/what the claim is about).
    /// </summary>
    public required ClaimEntity Subject { get; init; }

    /// <summary>
    /// The predicate (relationship type).
    /// Use standard predicates from ClaimPredicate where possible.
    /// </summary>
    public required string Predicate { get; init; }

    /// <summary>
    /// The object (value, entity, or literal).
    /// </summary>
    public required ClaimObject Object { get; init; }

    /// <summary>
    /// Confidence score (0.0-1.0) from extraction.
    /// </summary>
    public float Confidence { get; init; } = 1.0f;

    /// <summary>
    /// Project ID containing this claim.
    /// </summary>
    public Guid ProjectId { get; init; }

    /// <summary>
    /// Source document ID.
    /// </summary>
    public Guid DocumentId { get; init; }

    /// <summary>
    /// Source sentence/paragraph evidence.
    /// </summary>
    public required ClaimEvidence Evidence { get; init; }

    /// <summary>
    /// Validation status against axioms and graph.
    /// </summary>
    public ClaimValidationStatus ValidationStatus { get; init; } = ClaimValidationStatus.Pending;

    /// <summary>
    /// Validation messages (errors, warnings).
    /// </summary>
    public IReadOnlyList<ClaimValidationMessage>? ValidationMessages { get; init; }

    /// <summary>
    /// Whether this claim has been reviewed by a human.
    /// </summary>
    public bool IsReviewed { get; init; }

    /// <summary>
    /// Reviewer notes (if reviewed).
    /// </summary>
    public string? ReviewNotes { get; init; }

    /// <summary>
    /// Claims this claim is related to (e.g., derived from, contradicts).
    /// </summary>
    public IReadOnlyList<ClaimRelation>? RelatedClaims { get; init; }

    /// <summary>
    /// Additional metadata from extraction.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// When the claim was extracted.
    /// </summary>
    public DateTimeOffset ExtractedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When the claim was last updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; init; }

    /// <summary>
    /// Whether the claim is active (not superseded or deleted).
    /// </summary>
    public bool IsActive { get; init; } = true;

    /// <summary>
    /// Version number for optimistic concurrency.
    /// </summary>
    public int Version { get; init; } = 1;

    /// <summary>
    /// Returns a canonical string form of the claim.
    /// </summary>
    public string ToCanonicalForm() =>
        $"({Subject.SurfaceForm}, {Predicate}, {Object.ToCanonicalForm()})";
}

/// <summary>
/// Validation status of a claim.
/// </summary>
public enum ClaimValidationStatus
{
    /// <summary>Not yet validated.</summary>
    Pending,

    /// <summary>Claim is valid (consistent with graph and axioms).</summary>
    Valid,

    /// <summary>Claim is invalid (violates axioms).</summary>
    Invalid,

    /// <summary>Claim conflicts with other claims.</summary>
    Conflict,

    /// <summary>Claim could not be validated (missing context).</summary>
    Inconclusive
}

/// <summary>
/// Validation message for a claim.
/// </summary>
public record ClaimValidationMessage
{
    /// <summary>Severity level.</summary>
    public ClaimMessageSeverity Severity { get; init; }

    /// <summary>Message code.</summary>
    public required string Code { get; init; }

    /// <summary>Human-readable message.</summary>
    public required string Message { get; init; }

    /// <summary>Related axiom ID (if axiom violation).</summary>
    public string? AxiomId { get; init; }

    /// <summary>Related claim ID (if conflict).</summary>
    public Guid? ConflictingClaimId { get; init; }
}

public enum ClaimMessageSeverity
{
    Info,
    Warning,
    Error
}

/// <summary>
/// Relation between claims.
/// </summary>
public record ClaimRelation
{
    /// <summary>Related claim ID.</summary>
    public required Guid RelatedClaimId { get; init; }

    /// <summary>Type of relation.</summary>
    public required ClaimRelationType RelationType { get; init; }

    /// <summary>Confidence in the relation.</summary>
    public float Confidence { get; init; } = 1.0f;
}

public enum ClaimRelationType
{
    /// <summary>This claim is derived from another.</summary>
    DerivedFrom,

    /// <summary>This claim supports another.</summary>
    Supports,

    /// <summary>This claim contradicts another.</summary>
    Contradicts,

    /// <summary>This claim supersedes another.</summary>
    Supersedes,

    /// <summary>This claim is equivalent to another.</summary>
    EquivalentTo
}
```

### 4.2 ClaimEntity Record

```csharp
namespace Lexichord.Abstractions.Contracts.Knowledge.Claims;

/// <summary>
/// Subject or object entity reference in a claim.
/// </summary>
public record ClaimEntity
{
    /// <summary>
    /// Linked graph entity ID (if resolved).
    /// </summary>
    public Guid? EntityId { get; init; }

    /// <summary>
    /// Entity type (e.g., "Endpoint", "Parameter", "Schema").
    /// </summary>
    public required string EntityType { get; init; }

    /// <summary>
    /// Surface form (as appeared in text).
    /// </summary>
    public required string SurfaceForm { get; init; }

    /// <summary>
    /// Normalized form (for matching).
    /// </summary>
    public string NormalizedForm => SurfaceForm.ToLowerInvariant().Trim();

    /// <summary>
    /// The linked entity (if resolved and loaded).
    /// </summary>
    public KnowledgeEntity? ResolvedEntity { get; init; }

    /// <summary>
    /// Whether this entity is resolved to a graph entity.
    /// </summary>
    public bool IsResolved => EntityId.HasValue;

    /// <summary>
    /// Linking confidence (if resolved).
    /// </summary>
    public float? LinkingConfidence { get; init; }

    /// <summary>
    /// Character offset in source document.
    /// </summary>
    public int StartOffset { get; init; }

    /// <summary>
    /// End character offset.
    /// </summary>
    public int EndOffset { get; init; }

    /// <summary>
    /// Creates an unresolved entity reference.
    /// </summary>
    public static ClaimEntity Unresolved(string surfaceForm, string entityType) =>
        new()
        {
            SurfaceForm = surfaceForm,
            EntityType = entityType
        };

    /// <summary>
    /// Creates a resolved entity reference.
    /// </summary>
    public static ClaimEntity Resolved(
        KnowledgeEntity entity,
        string surfaceForm,
        float confidence = 1.0f) =>
        new()
        {
            EntityId = entity.Id,
            EntityType = entity.EntityType,
            SurfaceForm = surfaceForm,
            ResolvedEntity = entity,
            LinkingConfidence = confidence
        };
}
```

### 4.3 ClaimObject Record

```csharp
namespace Lexichord.Abstractions.Contracts.Knowledge.Claims;

/// <summary>
/// Object of a claim (can be entity or literal value).
/// </summary>
public record ClaimObject
{
    /// <summary>
    /// Whether object is an entity or literal.
    /// </summary>
    public ClaimObjectType Type { get; init; }

    /// <summary>
    /// Entity reference (if Type is Entity).
    /// </summary>
    public ClaimEntity? Entity { get; init; }

    /// <summary>
    /// Literal value (if Type is Literal).
    /// </summary>
    public object? LiteralValue { get; init; }

    /// <summary>
    /// Literal data type (e.g., "string", "int", "bool", "datetime").
    /// </summary>
    public string? LiteralType { get; init; }

    /// <summary>
    /// Unit for numeric values (e.g., "requests/minute", "bytes").
    /// </summary>
    public string? Unit { get; init; }

    /// <summary>
    /// Returns canonical string form.
    /// </summary>
    public string ToCanonicalForm() => Type switch
    {
        ClaimObjectType.Entity => Entity?.SurfaceForm ?? "[unresolved]",
        ClaimObjectType.Literal => LiteralValue?.ToString() ?? "null",
        _ => "[unknown]"
    };

    /// <summary>
    /// Creates an entity object.
    /// </summary>
    public static ClaimObject FromEntity(ClaimEntity entity) =>
        new() { Type = ClaimObjectType.Entity, Entity = entity };

    /// <summary>
    /// Creates a literal string object.
    /// </summary>
    public static ClaimObject FromString(string value) =>
        new() { Type = ClaimObjectType.Literal, LiteralValue = value, LiteralType = "string" };

    /// <summary>
    /// Creates a literal boolean object.
    /// </summary>
    public static ClaimObject FromBool(bool value) =>
        new() { Type = ClaimObjectType.Literal, LiteralValue = value, LiteralType = "bool" };

    /// <summary>
    /// Creates a literal integer object.
    /// </summary>
    public static ClaimObject FromInt(int value, string? unit = null) =>
        new() { Type = ClaimObjectType.Literal, LiteralValue = value, LiteralType = "int", Unit = unit };

    /// <summary>
    /// Creates a literal decimal object.
    /// </summary>
    public static ClaimObject FromDecimal(decimal value, string? unit = null) =>
        new() { Type = ClaimObjectType.Literal, LiteralValue = value, LiteralType = "decimal", Unit = unit };
}

/// <summary>
/// Type of claim object.
/// </summary>
public enum ClaimObjectType
{
    /// <summary>Object is an entity reference.</summary>
    Entity,

    /// <summary>Object is a literal value.</summary>
    Literal
}
```

### 4.4 ClaimEvidence Record

```csharp
namespace Lexichord.Abstractions.Contracts.Knowledge.Claims;

/// <summary>
/// Evidence linking claim to source text.
/// </summary>
public record ClaimEvidence
{
    /// <summary>
    /// Source sentence text.
    /// </summary>
    public required string Sentence { get; init; }

    /// <summary>
    /// Broader context (paragraph or surrounding sentences).
    /// </summary>
    public string? Context { get; init; }

    /// <summary>
    /// Character offset in document.
    /// </summary>
    public int StartOffset { get; init; }

    /// <summary>
    /// End character offset.
    /// </summary>
    public int EndOffset { get; init; }

    /// <summary>
    /// Length of the evidence span.
    /// </summary>
    public int Length => EndOffset - StartOffset;

    /// <summary>
    /// Line number in document (1-based).
    /// </summary>
    public int? LineNumber { get; init; }

    /// <summary>
    /// Section/heading containing this evidence.
    /// </summary>
    public string? Section { get; init; }

    /// <summary>
    /// Chunk ID containing this evidence.
    /// </summary>
    public Guid? ChunkId { get; init; }

    /// <summary>
    /// Extraction method used.
    /// </summary>
    public ClaimExtractionMethod ExtractionMethod { get; init; }

    /// <summary>
    /// Pattern ID (if rule-based extraction).
    /// </summary>
    public string? PatternId { get; init; }
}

/// <summary>
/// Method used to extract a claim.
/// </summary>
public enum ClaimExtractionMethod
{
    /// <summary>Pattern-based rule matching.</summary>
    PatternRule,

    /// <summary>Semantic role labeling.</summary>
    SemanticRoleLabeling,

    /// <summary>Dependency parsing.</summary>
    DependencyParsing,

    /// <summary>LLM-based extraction.</summary>
    LLMExtraction,

    /// <summary>Manual entry.</summary>
    Manual
}
```

### 4.5 ClaimPredicate Constants

```csharp
namespace Lexichord.Abstractions.Contracts.Knowledge.Claims;

/// <summary>
/// Standard claim predicates for technical documentation.
/// </summary>
public static class ClaimPredicate
{
    // API/Endpoint predicates
    public const string ACCEPTS = "ACCEPTS";
    public const string RETURNS = "RETURNS";
    public const string REQUIRES = "REQUIRES";
    public const string PRODUCES = "PRODUCES";
    public const string CONSUMES = "CONSUMES";

    // Property predicates
    public const string HAS_PROPERTY = "HAS_PROPERTY";
    public const string HAS_TYPE = "HAS_TYPE";
    public const string HAS_DEFAULT = "HAS_DEFAULT";
    public const string HAS_VALUE = "HAS_VALUE";
    public const string HAS_FORMAT = "HAS_FORMAT";

    // Containment predicates
    public const string CONTAINS = "CONTAINS";
    public const string PART_OF = "PART_OF";
    public const string BELONGS_TO = "BELONGS_TO";

    // Lifecycle predicates
    public const string IS_DEPRECATED = "IS_DEPRECATED";
    public const string REPLACED_BY = "REPLACED_BY";
    public const string INTRODUCED_IN = "INTRODUCED_IN";

    // Relationship predicates
    public const string RELATED_TO = "RELATED_TO";
    public const string DEPENDS_ON = "DEPENDS_ON";
    public const string IMPLEMENTS = "IMPLEMENTS";
    public const string EXTENDS = "EXTENDS";

    // Constraint predicates
    public const string MUST_BE = "MUST_BE";
    public const string CANNOT_BE = "CANNOT_BE";
    public const string VALID_VALUES = "VALID_VALUES";
    public const string MIN_VALUE = "MIN_VALUE";
    public const string MAX_VALUE = "MAX_VALUE";

    /// <summary>
    /// All standard predicates.
    /// </summary>
    public static readonly IReadOnlyList<string> All = new[]
    {
        ACCEPTS, RETURNS, REQUIRES, PRODUCES, CONSUMES,
        HAS_PROPERTY, HAS_TYPE, HAS_DEFAULT, HAS_VALUE, HAS_FORMAT,
        CONTAINS, PART_OF, BELONGS_TO,
        IS_DEPRECATED, REPLACED_BY, INTRODUCED_IN,
        RELATED_TO, DEPENDS_ON, IMPLEMENTS, EXTENDS,
        MUST_BE, CANNOT_BE, VALID_VALUES, MIN_VALUE, MAX_VALUE
    };

    /// <summary>
    /// Gets the inverse predicate (if applicable).
    /// </summary>
    public static string? GetInverse(string predicate) => predicate switch
    {
        ACCEPTS => null,
        RETURNS => null,
        REQUIRES => null,
        CONTAINS => PART_OF,
        PART_OF => CONTAINS,
        DEPENDS_ON => null,
        IMPLEMENTS => null,
        EXTENDS => null,
        _ => null
    };
}
```

### 4.6 ClaimExtractionResult Record

```csharp
namespace Lexichord.Abstractions.Contracts.Knowledge.Claims;

/// <summary>
/// Result of claim extraction from a document.
/// </summary>
public record ClaimExtractionResult
{
    /// <summary>
    /// Extracted claims.
    /// </summary>
    public required IReadOnlyList<Claim> Claims { get; init; }

    /// <summary>
    /// Document ID processed.
    /// </summary>
    public Guid DocumentId { get; init; }

    /// <summary>
    /// Processing duration.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Extraction statistics.
    /// </summary>
    public ClaimExtractionStats Stats { get; init; } = new();

    /// <summary>
    /// Sentences that couldn't be processed.
    /// </summary>
    public IReadOnlyList<string>? FailedSentences { get; init; }

    /// <summary>
    /// Creates an empty result.
    /// </summary>
    public static ClaimExtractionResult Empty => new()
    {
        Claims = Array.Empty<Claim>()
    };
}

/// <summary>
/// Statistics about claim extraction.
/// </summary>
public record ClaimExtractionStats
{
    public int TotalSentences { get; init; }
    public int SentencesWithClaims { get; init; }
    public int TotalClaims { get; init; }
    public int PatternMatches { get; init; }
    public int SRLExtractions { get; init; }
    public int DuplicatesRemoved { get; init; }
    public float AverageConfidence { get; init; }
    public IReadOnlyDictionary<string, int>? ClaimsByPredicate { get; init; }
}
```

---

## 5. Usage Examples

### 5.1 Creating Claims Programmatically

```csharp
// Claim: "GET /users accepts a limit parameter"
var claim1 = new Claim
{
    Subject = new ClaimEntity
    {
        EntityId = endpointId,
        EntityType = "Endpoint",
        SurfaceForm = "GET /users"
    },
    Predicate = ClaimPredicate.ACCEPTS,
    Object = ClaimObject.FromEntity(new ClaimEntity
    {
        EntityId = parameterId,
        EntityType = "Parameter",
        SurfaceForm = "limit"
    }),
    Evidence = new ClaimEvidence
    {
        Sentence = "The GET /users endpoint accepts a limit parameter.",
        StartOffset = 100,
        EndOffset = 147,
        ExtractionMethod = ClaimExtractionMethod.PatternRule,
        PatternId = "accepts_parameter"
    },
    Confidence = 0.9f,
    DocumentId = docId
};

// Claim: "Rate limiting is 100 requests per minute"
var claim2 = new Claim
{
    Subject = new ClaimEntity
    {
        EntityType = "Concept",
        SurfaceForm = "Rate limiting"
    },
    Predicate = ClaimPredicate.HAS_VALUE,
    Object = ClaimObject.FromInt(100, "requests/minute"),
    Evidence = new ClaimEvidence
    {
        Sentence = "Rate limiting is set to 100 requests per minute.",
        StartOffset = 200,
        EndOffset = 248,
        ExtractionMethod = ClaimExtractionMethod.SemanticRoleLabeling
    },
    Confidence = 0.85f,
    DocumentId = docId
};
```

### 5.2 Querying Claims

```csharp
// Find all claims about an endpoint
var endpointClaims = claims.Where(c =>
    c.Subject.EntityId == endpointId ||
    (c.Object.Type == ClaimObjectType.Entity && c.Object.Entity?.EntityId == endpointId));

// Find all deprecation claims
var deprecations = claims.Where(c =>
    c.Predicate == ClaimPredicate.IS_DEPRECATED &&
    c.Object.Type == ClaimObjectType.Literal &&
    (bool)c.Object.LiteralValue! == true);

// Find conflicting claims
var conflicts = claims.Where(c =>
    c.ValidationStatus == ClaimValidationStatus.Conflict);
```

---

## 6. Unit Testing Requirements

```csharp
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.6e")]
public class ClaimDataModelTests
{
    [Fact]
    public void Claim_WithRequiredProperties_CreatesSuccessfully()
    {
        // Arrange & Act
        var claim = new Claim
        {
            Subject = ClaimEntity.Unresolved("GET /users", "Endpoint"),
            Predicate = ClaimPredicate.ACCEPTS,
            Object = ClaimObject.FromEntity(ClaimEntity.Unresolved("limit", "Parameter")),
            Evidence = new ClaimEvidence
            {
                Sentence = "Test sentence.",
                ExtractionMethod = ClaimExtractionMethod.PatternRule
            }
        };

        // Assert
        claim.Id.Should().NotBeEmpty();
        claim.ValidationStatus.Should().Be(ClaimValidationStatus.Pending);
        claim.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Claim_ToCanonicalForm_ReturnsCorrectFormat()
    {
        // Arrange
        var claim = CreateTestClaim("GET /users", ClaimPredicate.ACCEPTS, "limit");

        // Act
        var canonical = claim.ToCanonicalForm();

        // Assert
        canonical.Should().Be("(GET /users, ACCEPTS, limit)");
    }

    [Fact]
    public void ClaimEntity_Resolved_HasEntityId()
    {
        // Arrange
        var entity = new KnowledgeEntity
        {
            Id = Guid.NewGuid(),
            Name = "GET /users",
            EntityType = "Endpoint"
        };

        // Act
        var claimEntity = ClaimEntity.Resolved(entity, "users endpoint", 0.95f);

        // Assert
        claimEntity.IsResolved.Should().BeTrue();
        claimEntity.EntityId.Should().Be(entity.Id);
        claimEntity.LinkingConfidence.Should().Be(0.95f);
    }

    [Fact]
    public void ClaimEntity_Unresolved_HasNoEntityId()
    {
        // Act
        var claimEntity = ClaimEntity.Unresolved("something", "Unknown");

        // Assert
        claimEntity.IsResolved.Should().BeFalse();
        claimEntity.EntityId.Should().BeNull();
    }

    [Fact]
    public void ClaimObject_FromEntity_CreatesEntityType()
    {
        // Arrange
        var entity = ClaimEntity.Unresolved("limit", "Parameter");

        // Act
        var obj = ClaimObject.FromEntity(entity);

        // Assert
        obj.Type.Should().Be(ClaimObjectType.Entity);
        obj.Entity.Should().Be(entity);
    }

    [Fact]
    public void ClaimObject_FromInt_CreatesLiteralWithUnit()
    {
        // Act
        var obj = ClaimObject.FromInt(100, "requests/minute");

        // Assert
        obj.Type.Should().Be(ClaimObjectType.Literal);
        obj.LiteralValue.Should().Be(100);
        obj.LiteralType.Should().Be("int");
        obj.Unit.Should().Be("requests/minute");
    }

    [Fact]
    public void ClaimPredicate_GetInverse_ReturnsCorrectInverse()
    {
        // Assert
        ClaimPredicate.GetInverse(ClaimPredicate.CONTAINS).Should().Be(ClaimPredicate.PART_OF);
        ClaimPredicate.GetInverse(ClaimPredicate.PART_OF).Should().Be(ClaimPredicate.CONTAINS);
        ClaimPredicate.GetInverse(ClaimPredicate.ACCEPTS).Should().BeNull();
    }
}
```

---

## 7. Acceptance Criteria (QA)

| # | Criterion |
| :- | :-------- |
| 1 | `Claim` record can be instantiated with required properties. |
| 2 | `ClaimEntity` supports both resolved and unresolved forms. |
| 3 | `ClaimObject` supports entity and literal types. |
| 4 | `ClaimEvidence` captures source text and position. |
| 5 | `ClaimPredicate` defines all standard predicates. |
| 6 | `ToCanonicalForm()` returns consistent string representation. |
| 7 | Validation status enum covers all states. |
| 8 | Claim relations support contradiction tracking. |
| 9 | Records are immutable (init-only properties). |
| 10 | Factory methods create correct object types. |

---

## 8. Deliverable Checklist

| # | Deliverable | Status |
| :- | :---------- | :----- |
| 1 | `Claim` record | [ ] |
| 2 | `ClaimEntity` record | [ ] |
| 3 | `ClaimObject` record | [ ] |
| 4 | `ClaimEvidence` record | [ ] |
| 5 | `ClaimValidationStatus` enum | [ ] |
| 6 | `ClaimValidationMessage` record | [ ] |
| 7 | `ClaimRelation` record | [ ] |
| 8 | `ClaimObjectType` enum | [ ] |
| 9 | `ClaimExtractionMethod` enum | [ ] |
| 10 | `ClaimPredicate` constants | [ ] |
| 11 | `ClaimExtractionResult` record | [ ] |
| 12 | `ClaimExtractionStats` record | [ ] |
| 13 | Unit tests | [ ] |

---

## 9. Changelog Entry

```markdown
### Added (v0.5.6e)

- `Claim` record for subject-predicate-object assertions
- `ClaimEntity` record for entity references in claims
- `ClaimObject` record supporting entity and literal objects
- `ClaimEvidence` for source text provenance
- `ClaimPredicate` constants for standard predicates
- `ClaimValidationStatus` for tracking validation state
- `ClaimRelation` for claim-to-claim relationships
- Factory methods for common claim patterns
```

---
