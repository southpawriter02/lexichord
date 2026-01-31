# LCS-DES-v0.10.5-KG-e: Design Specification — Validation Layer

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `KG-105-e` | Knowledge Import/Export sub-part e |
| **Feature Name** | `Validation Layer` | Validate imports against CKVS rules |
| **Target Version** | `v0.10.5e` | Fifth sub-part of v0.10.5-KG |
| **Module Scope** | `Lexichord.Modules.CKVS.Interop` | CKVS Interoperability module |
| **Swimlane** | `Knowledge Management` | Knowledge vertical |
| **License Tier** | `Teams` | Teams and above |
| **Feature Gate Key** | `FeatureFlags.CKVS.KnowledgeInterop` | Knowledge interoperability feature flag |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-31` | |
| **Parent Document** | [LCS-SBD-v0.10.5-KG](./LCS-SBD-v0.10.5-KG.md) | Knowledge Import/Export scope |
| **Scope Breakdown** | [LCS-SBD-v0.10.5-KG S2.1](./LCS-SBD-v0.10.5-KG.md#21-sub-parts) | e = Validation Layer |

---

## 2. Executive Summary

### 2.1 The Requirement

The Validation Layer ensures that imported data conforms to CKVS schema, axioms, and business rules before being committed to the knowledge graph. This prevents:

- Invalid entity types
- Missing required properties
- Circular relationships
- Axiom violations
- Schema constraint violations

### 2.2 The Proposed Solution

Implement a comprehensive validation framework with:

1. **IImportValidator interface:** Pluggable validation strategies
2. **Constraint validation:** Enforce CKVS schema rules
3. **Axiom validation:** Check imported data against axiom rules
4. **Detailed error reporting:** Categorized, actionable validation errors
5. **Validation caching:** Avoid redundant checks

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Dependencies

| Component | Source Version | Purpose |
| :--- | :--- | :--- |
| `IGraphRepository` | v0.4.5e | Query existing data for conflict detection |
| `IAxiomStore` | v0.4.6-KG | Retrieve axioms and schema constraints |
| `IValidationEngine` | v0.6.5-KG | Core validation logic |
| Format Parsers | v0.10.5a | Understand format-specific constraints |
| Schema Mapper | v0.10.5b | Validate mappings before application |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| (None) | | Uses existing dependencies |

### 3.2 Licensing Behavior

- **Core Tier:** No validation beyond syntax
- **WriterPro Tier:** Basic schema validation
- **Teams Tier:** Full validation (schema, axioms, relationships)
- **Enterprise Tier:** Teams tier + advanced analytics

---

## 4. Data Contract (The API)

### 4.1 IImportValidator Interface

```csharp
namespace Lexichord.Modules.CKVS.Interop.Contracts;

/// <summary>
/// Validates imported data against CKVS schema and axioms.
/// Provides detailed error and warning information.
/// </summary>
public interface IImportValidator
{
    /// <summary>
    /// Validates a single entity against CKVS constraints.
    /// </summary>
    Task<EntityValidationResult> ValidateEntityAsync(
        ImportEntity entity,
        SchemaMapping mapping,
        CancellationToken ct = default);

    /// <summary>
    /// Validates all entities in a batch.
    /// </summary>
    Task<BatchValidationResult> ValidateEntitiesAsync(
        IEnumerable<ImportEntity> entities,
        SchemaMapping mapping,
        CancellationToken ct = default);

    /// <summary>
    /// Validates relationships before creation.
    /// </summary>
    Task<RelationshipValidationResult> ValidateRelationshipAsync(
        ImportRelationship relationship,
        SchemaMapping mapping,
        CancellationToken ct = default);

    /// <summary>
    /// Validates entire import against axioms and rules.
    /// </summary>
    Task<AxiomValidationResult> ValidateAxiomsAsync(
        IEnumerable<ImportEntity> entities,
        IEnumerable<ImportRelationship> relationships,
        CancellationToken ct = default);

    /// <summary>
    /// Validates schema mapping compatibility.
    /// </summary>
    Task<MappingValidationResult> ValidateMappingAsync(
        SchemaMapping mapping,
        CancellationToken ct = default);
}

/// <summary>
/// Result of validating a single entity.
/// </summary>
public record EntityValidationResult
{
    /// <summary>
    /// Whether entity is valid.
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Entity being validated.
    /// </summary>
    public required ImportEntity Entity { get; init; }

    /// <summary>
    /// Validation errors that prevent import.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; init; } = [];

    /// <summary>
    /// Non-blocking warnings.
    /// </summary>
    public IReadOnlyList<ValidationWarning> Warnings { get; init; } = [];

    /// <summary>
    /// Severity level of worst issue.
    /// </summary>
    public ValidationSeverity MaxSeverity { get; init; }

    /// <summary>
    /// Timestamp of validation.
    /// </summary>
    public DateTimeOffset ValidatedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Validation error with detailed information.
/// </summary>
public record ValidationError
{
    /// <summary>
    /// Error code for programmatic handling.
    /// </summary>
    public required string ErrorCode { get; init; }

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Path to problematic field (e.g., "properties.name").
    /// </summary>
    public string? FieldPath { get; init; }

    /// <summary>
    /// The value that caused the error.
    /// </summary>
    public object? ProblematicValue { get; init; }

    /// <summary>
    /// Suggested fix or constraint information.
    /// </summary>
    public string? Suggestion { get; init; }

    /// <summary>
    /// Line number in source if available.
    /// </summary>
    public int? SourceLine { get; init; }

    /// <summary>
    /// Error category.
    /// </summary>
    public ErrorCategory Category { get; init; }
}

/// <summary>
/// Non-blocking validation warning.
/// </summary>
public record ValidationWarning
{
    /// <summary>
    /// Warning code.
    /// </summary>
    public required string WarningCode { get; init; }

    /// <summary>
    /// Warning message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Path to field with warning.
    /// </summary>
    public string? FieldPath { get; init; }

    /// <summary>
    /// Suggested action.
    /// </summary>
    public string? Recommendation { get; init; }
}

/// <summary>
/// Categories of validation errors.
/// </summary>
public enum ErrorCategory
{
    /// <summary>Entity type not recognized.</summary>
    UnknownType,

    /// <summary>Required property missing.</summary>
    MissingRequired,

    /// <summary>Property value violates constraint.</summary>
    ConstraintViolation,

    /// <summary>Data type mismatch.</summary>
    TypeMismatch,

    /// <summary>Relationship integrity issue.</summary>
    RelationshipError,

    /// <summary>Axiom rule violation.</summary>
    AxiomViolation,

    /// <summary>Entity already exists (conflict).</summary>
    Conflict,

    /// <summary>Invalid mapping.</summary>
    MappingError,

    /// <summary>Other validation issue.</summary>
    Other
}

/// <summary>
/// Severity levels for validation issues.
/// </summary>
public enum ValidationSeverity
{
    /// <summary>Informational message only.</summary>
    Info = 0,

    /// <summary>Warning - non-blocking.</summary>
    Warning = 1,

    /// <summary>Error - prevents import.</summary>
    Error = 2,

    /// <summary>Critical error - stops entire import.</summary>
    Critical = 3
}

/// <summary>
/// Batch validation result.
/// </summary>
public record BatchValidationResult
{
    /// <summary>
    /// Overall validation status.
    /// </summary>
    public required ValidationStatus Status { get; init; }

    /// <summary>
    /// Total entities validated.
    /// </summary>
    public required int TotalEntities { get; init; }

    /// <summary>
    /// Valid entities count.
    /// </summary>
    public required int ValidCount { get; init; }

    /// <summary>
    /// Entities with errors.
    /// </summary>
    public required int ErrorCount { get; init; }

    /// <summary>
    /// Entities with only warnings.
    /// </summary>
    public required int WarningCount { get; init; }

    /// <summary>
    /// Results by entity.
    /// </summary>
    public IReadOnlyList<EntityValidationResult> Results { get; init; } = [];

    /// <summary>
    /// Aggregated errors.
    /// </summary>
    public IReadOnlyList<ValidationError> AllErrors { get; init; } = [];

    /// <summary>
    /// Aggregated warnings.
    /// </summary>
    public IReadOnlyList<ValidationWarning> AllWarnings { get; init; } = [];

    /// <summary>
    /// Validation duration.
    /// </summary>
    public TimeSpan Duration { get; init; }
}

/// <summary>
/// Relationship validation result.
/// </summary>
public record RelationshipValidationResult
{
    /// <summary>
    /// Whether relationship is valid.
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Relationship being validated.
    /// </summary>
    public required ImportRelationship Relationship { get; init; }

    /// <summary>
    /// Validation errors.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; init; } = [];

    /// <summary>
    /// Validation warnings.
    /// </summary>
    public IReadOnlyList<ValidationWarning> Warnings { get; init; } = [];
}

/// <summary>
/// Axiom validation result.
/// </summary>
public record AxiomValidationResult
{
    /// <summary>
    /// Whether all axioms are satisfied.
    /// </summary>
    public required bool AllAxiomsSatisfied { get; init; }

    /// <summary>
    /// Total axioms checked.
    /// </summary>
    public required int TotalAxiomsChecked { get; init; }

    /// <summary>
    /// Satisfied axioms count.
    /// </summary>
    public required int SatisfiedCount { get; init; }

    /// <summary>
    /// Violated axioms count.
    /// </summary>
    public required int ViolatedCount { get; init; }

    /// <summary>
    /// Axiom violations detected.
    /// </summary>
    public IReadOnlyList<AxiomViolation> Violations { get; init; } = [];

    /// <summary>
    /// Validation duration.
    /// </summary>
    public TimeSpan Duration { get; init; }
}

/// <summary>
/// Axiom rule violation detail.
/// </summary>
public record AxiomViolation
{
    /// <summary>
    /// Axiom rule that was violated.
    /// </summary>
    public required string AxiomRule { get; init; }

    /// <summary>
    /// Description of violation.
    /// </summary>
    public required string ViolationMessage { get; init; }

    /// <summary>
    /// Entity IDs involved in violation.
    /// </summary>
    public IReadOnlyList<string> InvolvedEntityIds { get; init; } = [];

    /// <summary>
    /// Recommended resolution.
    /// </summary>
    public string? Resolution { get; init; }
}

/// <summary>
/// Schema mapping validation result.
/// </summary>
public record MappingValidationResult
{
    /// <summary>
    /// Whether mapping is valid.
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Validation errors.
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; init; } = [];

    /// <summary>
    /// Validation warnings.
    /// </summary>
    public IReadOnlyList<ValidationWarning> Warnings { get; init; } = [];

    /// <summary>
    /// Coverage percentage of source schema.
    /// </summary>
    public float MappingCoverage { get; init; }

    /// <summary>
    /// Unmapped type count.
    /// </summary>
    public int UnmappedTypeCount { get; init; }
}

/// <summary>
/// Overall validation status.
/// </summary>
public enum ValidationStatus
{
    /// <summary>All entities valid.</summary>
    Valid,

    /// <summary>Some entities have warnings but no errors.</summary>
    ValidWithWarnings,

    /// <summary>Some entities have errors.</summary>
    Invalid,

    /// <summary>Validation failed due to internal error.</summary>
    ValidationFailed
}
```

---

## 5. Implementation Strategy

### 5.1 Validation Pipeline

```
ValidateEntitiesAsync Request
    ↓
[Phase 1] Pre-validation
    • Check mapping validity
    • Verify CKVS compatibility
    • Prepare validation rules
    ↓
[Phase 2] Schema Validation
    • Check entity type exists
    • Verify required properties present
    • Validate property types
    • Check cardinality constraints
    ↓
[Phase 3] Constraint Validation
    • Validate property values
    • Check unique constraints
    • Verify pattern constraints
    • Check range constraints
    ↓
[Phase 4] Relationship Validation
    • Check relationship types exist
    • Verify source/target types
    • Validate multiplicity
    ↓
[Phase 5] Axiom Validation
    • Check import against axiom rules
    • Detect inconsistencies
    • Verify derived fact consistency
    ↓
[Phase 6] Compile Results
    • Aggregate errors/warnings
    • Determine overall status
    • Generate report
    ↓
BatchValidationResult
```

### 5.2 Validation Caching

```csharp
private readonly Dictionary<string, EntityValidationResult> _validationCache;

public async Task<EntityValidationResult> ValidateEntityAsync(
    ImportEntity entity, SchemaMapping mapping, CancellationToken ct)
{
    var cacheKey = $"{entity.Id}:{mapping.MappingId}";

    if (_validationCache.TryGetValue(cacheKey, out var cached))
        return cached;

    var result = await PerformValidationAsync(entity, mapping, ct);
    _validationCache[cacheKey] = result;

    return result;
}
```

---

## 6. Error Handling

### 6.1 Missing Required Property

**Scenario:** Entity missing a property marked as required in schema.

**Handling:**
- ValidationError with ErrorCode "MISSING_REQUIRED"
- FieldPath indicates missing property
- Suggestion provides property definition
- Status becomes Invalid

**Code:**
```csharp
if (!entity.Properties.ContainsKey(requiredProperty.Name))
{
    errors.Add(new ValidationError
    {
        ErrorCode = "MISSING_REQUIRED",
        Message = $"Required property '{requiredProperty.Name}' is missing",
        FieldPath = requiredProperty.Name,
        Suggestion = $"Property is required by schema: {requiredProperty.Description}",
        Category = ErrorCategory.MissingRequired
    });
}
```

### 6.2 Type Constraint Violation

**Scenario:** Property value does not match declared type.

**Handling:**
- ValidationError with ErrorCode "TYPE_MISMATCH"
- ProblematicValue shows actual value
- Suggestion shows expected type
- Error is critical

**Code:**
```csharp
if (!IsValidType(entity.Properties[prop], schema.Properties[prop].Type))
{
    errors.Add(new ValidationError
    {
        ErrorCode = "TYPE_MISMATCH",
        Message = $"Property '{prop}' has wrong type",
        ProblematicValue = entity.Properties[prop],
        FieldPath = prop,
        Category = ErrorCategory.TypeMismatch
    });
}
```

### 6.3 Axiom Violation

**Scenario:** Imported data violates an axiom rule.

**Handling:**
- AxiomValidationResult with violation details
- Suggests how to resolve
- Includes related entities
- May allow conditional import

**Code:**
```csharp
var violations = await _axiomStore.CheckAsync(entities, relationships);
result = new AxiomValidationResult
{
    AllAxiomsSatisfied = violations.Count == 0,
    Violations = violations.Select(v => new AxiomViolation
    {
        AxiomRule = v.RuleName,
        ViolationMessage = v.Message,
        InvolvedEntityIds = v.EntityIds,
        Resolution = SuggestResolution(v)
    }).ToList()
};
```

---

## 7. Performance Considerations

| Operation | Target | Implementation |
| :--- | :--- | :--- |
| Validate 100 entities | <1s | Batch processing, caching |
| Validate 1K entities | <5s | Parallel validation per type |
| Axiom validation | <2s | Pre-compiled rules |
| Schema check | <100ms | Cached schema |
| Mapping validation | <500ms | Structure analysis |

---

## 8. Security & Safety

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| Invalid data acceptance | Critical | Comprehensive validation checks |
| Performance DoS | Medium | Timeout on validation |
| Memory exhaustion | Medium | Stream validation for large batches |
| Axiom bypass | High | Multiple validation layers |

---

## 9. License Gating

| Tier | Validation Support |
| :--- | :--- |
| Core | Syntax only |
| WriterPro | Schema validation |
| Teams | Full validation (schema + axioms) |
| Enterprise | Teams + advanced analytics |

---

## 10. Acceptance Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | Entity with valid properties | ValidateEntityAsync | Returns EntityValidationResult.IsValid = true |
| 2 | Entity missing required property | ValidateEntityAsync | Returns error with ErrorCode "MISSING_REQUIRED" |
| 3 | Property with wrong type | ValidateEntityAsync | Returns error with ErrorCode "TYPE_MISMATCH" |
| 4 | 1000 entities | ValidateEntitiesAsync | Completes in <5s |
| 5 | Data violating axiom | ValidateAxiomsAsync | Returns violation with AxiomRule and Message |
| 6 | Valid mapping | ValidateMappingAsync | IsValid = true, coverage > 90% |
| 7 | Incomplete mapping | ValidateMappingAsync | IsValid = true but coverage < 90% with warnings |
| 8 | Batch with mix of valid/invalid | ValidateEntitiesAsync | Status = Invalid, ErrorCount reflects invalid entities |
| 9 | Relationship with bad target | ValidateRelationshipAsync | Returns error with Category = RelationshipError |
| 10 | Large batch | ValidateEntitiesAsync | Uses parallel processing, respects timeout |

---

## 11. Changelog

| Version | Date | Changes |
| :--- | :--- | :--- |
| 1.0 | 2026-01-31 | Initial draft - IImportValidator, entity/relationship/axiom validation, error categorization |

---

**Parent Document:** [LCS-DES-v0.10.5-KG-INDEX](./LCS-DES-v0.10.5-KG-INDEX.md)
**Previous Part:** [LCS-DES-v0.10.5-KG-d](./LCS-DES-v0.10.5-KG-d.md)
**Next Part:** [LCS-DES-v0.10.5-KG-f](./LCS-DES-v0.10.5-KG-f.md)
