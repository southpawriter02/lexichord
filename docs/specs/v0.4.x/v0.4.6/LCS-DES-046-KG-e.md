# LCS-DES-046-KG-e: Axiom Data Model

## 1. Metadata & Categorization

| Field | Value |
| :--- | :--- |
| **Document ID** | LCS-DES-046-KG-e |
| **Feature ID** | KG-046e |
| **Feature Name** | Axiom Data Model |
| **Target Version** | v0.4.6e |
| **Module Scope** | `Lexichord.Abstractions.Contracts` |
| **Swimlane** | Memory |
| **License Tier** | WriterPro (read), Teams (full) |
| **Feature Gate Key** | `knowledge.axioms.enabled` |
| **Status** | Draft |
| **Last Updated** | 2026-01-31 |

---

## 2. Executive Summary

### 2.1 The Requirement

Axioms are foundational truths that constrain what can exist in the Knowledge Graph. Before we can store, load, or query axioms, we need a well-defined data model that captures the structure of axiom rules, their constraints, and the violations they can produce.

### 2.2 The Proposed Solution

Define a comprehensive set of record types and enums that represent:

- **Axiom**: A named rule with one or more constraints.
- **AxiomRule**: A single constraint within an axiom.
- **AxiomViolation**: A violation detected during validation.
- **AxiomConstraintType**: The types of constraints supported.

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

**Upstream Modules:**
- `Lexichord.Abstractions` — Core record types
- v0.4.5-KG: `KnowledgeEntity`, `KnowledgeRelationship` — Target types

**NuGet Packages:**
- None (pure data model)

### 3.2 Module Placement

```
Lexichord.Abstractions/
├── Contracts/
│   ├── Knowledge/
│   │   ├── Axiom.cs
│   │   ├── AxiomRule.cs
│   │   ├── AxiomViolation.cs
│   │   ├── AxiomConstraintType.cs
│   │   ├── AxiomSeverity.cs
│   │   └── AxiomValidationResult.cs
```

### 3.3 Licensing Behavior

- **Load Behavior:** [x] Always — Records are value types, no licensing on model itself
- **Fallback Experience:** All tiers can reference types; validation logic is gated elsewhere

---

## 4. Data Contract (The API)

### 4.1 Axiom Record

```csharp
namespace Lexichord.Abstractions.Contracts.Knowledge;

/// <summary>
/// A foundational axiom (immutable domain rule) that content must satisfy.
/// Axioms define what is always true about entities and relationships.
/// </summary>
public record Axiom
{
    /// <summary>
    /// Unique axiom identifier (e.g., "endpoint-must-have-method").
    /// Must be lowercase with hyphens, no spaces.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Human-readable axiom name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Detailed description of what this axiom enforces.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Entity or relationship type this axiom applies to.
    /// Must match a type defined in the Schema Registry.
    /// </summary>
    public required string TargetType { get; init; }

    /// <summary>
    /// Whether this axiom targets entities or relationships.
    /// </summary>
    public AxiomTargetKind TargetKind { get; init; } = AxiomTargetKind.Entity;

    /// <summary>
    /// The rules that define this axiom's constraints.
    /// All rules must pass for the axiom to be satisfied.
    /// </summary>
    public required IReadOnlyList<AxiomRule> Rules { get; init; }

    /// <summary>
    /// Severity of violations. Error blocks save, Warning allows.
    /// </summary>
    public AxiomSeverity Severity { get; init; } = AxiomSeverity.Error;

    /// <summary>
    /// Category for grouping axioms in UI.
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Tags for filtering axioms.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Whether this axiom is enabled.
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Source file path (if loaded from YAML).
    /// </summary>
    public string? SourceFile { get; init; }

    /// <summary>
    /// When the axiom was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Axiom schema version for compatibility.
    /// </summary>
    public string SchemaVersion { get; init; } = "1.0";
}

/// <summary>
/// Target kind for axioms.
/// </summary>
public enum AxiomTargetKind
{
    /// <summary>Axiom applies to entities.</summary>
    Entity,

    /// <summary>Axiom applies to relationships.</summary>
    Relationship,

    /// <summary>Axiom applies to claims.</summary>
    Claim
}

/// <summary>
/// Severity of axiom violations.
/// </summary>
public enum AxiomSeverity
{
    /// <summary>Must fix before save/publish. Blocks operation.</summary>
    Error,

    /// <summary>Should fix but doesn't block.</summary>
    Warning,

    /// <summary>Informational suggestion.</summary>
    Info
}
```

### 4.2 AxiomRule Record

```csharp
namespace Lexichord.Abstractions.Contracts.Knowledge;

/// <summary>
/// A single rule within an axiom that defines a constraint.
/// </summary>
public record AxiomRule
{
    /// <summary>
    /// Property name this rule applies to (if property-based).
    /// </summary>
    public string? Property { get; init; }

    /// <summary>
    /// Properties this rule applies to (for multi-property constraints).
    /// </summary>
    public IReadOnlyList<string>? Properties { get; init; }

    /// <summary>
    /// The type of constraint.
    /// </summary>
    public required AxiomConstraintType Constraint { get; init; }

    /// <summary>
    /// Expected value(s) for equality/one_of constraints.
    /// </summary>
    public IReadOnlyList<object>? Values { get; init; }

    /// <summary>
    /// Minimum value for range constraints.
    /// </summary>
    public object? Min { get; init; }

    /// <summary>
    /// Maximum value for range constraints.
    /// </summary>
    public object? Max { get; init; }

    /// <summary>
    /// Regex pattern for pattern constraints.
    /// </summary>
    public string? Pattern { get; init; }

    /// <summary>
    /// Minimum count for cardinality constraints.
    /// </summary>
    public int? MinCount { get; init; }

    /// <summary>
    /// Maximum count for cardinality constraints.
    /// </summary>
    public int? MaxCount { get; init; }

    /// <summary>
    /// Conditional clause (when to apply this rule).
    /// </summary>
    public AxiomCondition? When { get; init; }

    /// <summary>
    /// Custom error message for this rule.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Related entity/relationship type for reference constraints.
    /// </summary>
    public string? ReferenceType { get; init; }
}

/// <summary>
/// Types of constraints that axiom rules can enforce.
/// </summary>
public enum AxiomConstraintType
{
    /// <summary>Property must be present and non-null.</summary>
    Required,

    /// <summary>Property must be one of the specified values.</summary>
    OneOf,

    /// <summary>Property must not be any of the specified values.</summary>
    NotOneOf,

    /// <summary>Property value must be within range [min, max].</summary>
    Range,

    /// <summary>Property must match regex pattern.</summary>
    Pattern,

    /// <summary>Collection property must have count within bounds.</summary>
    Cardinality,

    /// <summary>Properties cannot both have values (mutually exclusive).</summary>
    NotBoth,

    /// <summary>If one property has value, the other must too.</summary>
    RequiresTogether,

    /// <summary>Property must equal specified value.</summary>
    Equals,

    /// <summary>Property must not equal specified value.</summary>
    NotEquals,

    /// <summary>Property must be unique across all entities of this type.</summary>
    Unique,

    /// <summary>Property must reference an existing entity.</summary>
    ReferenceExists,

    /// <summary>Property value must be valid for its declared type.</summary>
    TypeValid,

    /// <summary>Custom validation via expression.</summary>
    Custom
}

/// <summary>
/// Conditional clause for when to apply an axiom rule.
/// </summary>
public record AxiomCondition
{
    /// <summary>
    /// Property to check for condition.
    /// </summary>
    public required string Property { get; init; }

    /// <summary>
    /// Operator for comparison.
    /// </summary>
    public ConditionOperator Operator { get; init; } = ConditionOperator.Equals;

    /// <summary>
    /// Value to compare against.
    /// </summary>
    public required object Value { get; init; }
}

/// <summary>
/// Operators for axiom conditions.
/// </summary>
public enum ConditionOperator
{
    Equals,
    NotEquals,
    Contains,
    StartsWith,
    EndsWith,
    GreaterThan,
    LessThan,
    IsNull,
    IsNotNull
}
```

### 4.3 AxiomViolation Record

```csharp
namespace Lexichord.Abstractions.Contracts.Knowledge;

/// <summary>
/// A violation detected when validating against an axiom.
/// </summary>
public record AxiomViolation
{
    /// <summary>
    /// Unique violation identifier.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The axiom that was violated.
    /// </summary>
    public required Axiom Axiom { get; init; }

    /// <summary>
    /// The specific rule within the axiom that failed.
    /// </summary>
    public required AxiomRule ViolatedRule { get; init; }

    /// <summary>
    /// Entity ID that caused the violation (if entity-based).
    /// </summary>
    public Guid? EntityId { get; init; }

    /// <summary>
    /// Relationship ID that caused the violation (if relationship-based).
    /// </summary>
    public Guid? RelationshipId { get; init; }

    /// <summary>
    /// Claim ID that caused the violation (if claim-based).
    /// </summary>
    public Guid? ClaimId { get; init; }

    /// <summary>
    /// Property name where violation occurred.
    /// </summary>
    public string? PropertyName { get; init; }

    /// <summary>
    /// Actual value that caused the violation.
    /// </summary>
    public object? ActualValue { get; init; }

    /// <summary>
    /// Expected value(s) based on the constraint.
    /// </summary>
    public object? ExpectedValue { get; init; }

    /// <summary>
    /// Human-readable violation message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Severity inherited from axiom.
    /// </summary>
    public AxiomSeverity Severity { get; init; }

    /// <summary>
    /// Document ID where violation occurred (if applicable).
    /// </summary>
    public Guid? DocumentId { get; init; }

    /// <summary>
    /// Text position in document (if applicable).
    /// </summary>
    public TextSpan? Location { get; init; }

    /// <summary>
    /// Suggested fix for the violation.
    /// </summary>
    public AxiomFix? SuggestedFix { get; init; }

    /// <summary>
    /// When the violation was detected.
    /// </summary>
    public DateTimeOffset DetectedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Suggested fix for an axiom violation.
/// </summary>
public record AxiomFix
{
    /// <summary>
    /// Description of the fix.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Property to modify.
    /// </summary>
    public string? PropertyName { get; init; }

    /// <summary>
    /// Suggested new value.
    /// </summary>
    public object? NewValue { get; init; }

    /// <summary>
    /// Confidence that this fix is correct (0.0-1.0).
    /// </summary>
    public float Confidence { get; init; }

    /// <summary>
    /// Whether this fix can be auto-applied.
    /// </summary>
    public bool CanAutoApply { get; init; }
}

/// <summary>
/// Text span in a document.
/// </summary>
public record TextSpan
{
    /// <summary>Starting character offset.</summary>
    public int Start { get; init; }

    /// <summary>Ending character offset.</summary>
    public int End { get; init; }

    /// <summary>Line number (1-based).</summary>
    public int Line { get; init; }

    /// <summary>Column number (1-based).</summary>
    public int Column { get; init; }

    /// <summary>Length of the span.</summary>
    public int Length => End - Start;
}
```

### 4.4 AxiomValidationResult Record

```csharp
namespace Lexichord.Abstractions.Contracts.Knowledge;

/// <summary>
/// Result of validating against axioms.
/// </summary>
public record AxiomValidationResult
{
    /// <summary>
    /// Whether validation passed (no errors).
    /// </summary>
    public bool IsValid => !Violations.Any(v => v.Severity == AxiomSeverity.Error);

    /// <summary>
    /// All violations found.
    /// </summary>
    public required IReadOnlyList<AxiomViolation> Violations { get; init; }

    /// <summary>
    /// Number of axioms evaluated.
    /// </summary>
    public int AxiomsEvaluated { get; init; }

    /// <summary>
    /// Number of rules evaluated.
    /// </summary>
    public int RulesEvaluated { get; init; }

    /// <summary>
    /// Validation duration.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Violations grouped by severity.
    /// </summary>
    public IReadOnlyDictionary<AxiomSeverity, IReadOnlyList<AxiomViolation>> BySeverity =>
        Violations.GroupBy(v => v.Severity)
                  .ToDictionary(g => g.Key, g => (IReadOnlyList<AxiomViolation>)g.ToList());

    /// <summary>
    /// Error count.
    /// </summary>
    public int ErrorCount => Violations.Count(v => v.Severity == AxiomSeverity.Error);

    /// <summary>
    /// Warning count.
    /// </summary>
    public int WarningCount => Violations.Count(v => v.Severity == AxiomSeverity.Warning);

    /// <summary>
    /// Info count.
    /// </summary>
    public int InfoCount => Violations.Count(v => v.Severity == AxiomSeverity.Info);

    /// <summary>
    /// Creates an empty (valid) result.
    /// </summary>
    public static AxiomValidationResult Valid(int axiomsEvaluated = 0, int rulesEvaluated = 0) =>
        new()
        {
            Violations = Array.Empty<AxiomViolation>(),
            AxiomsEvaluated = axiomsEvaluated,
            RulesEvaluated = rulesEvaluated
        };

    /// <summary>
    /// Creates a result with violations.
    /// </summary>
    public static AxiomValidationResult WithViolations(
        IEnumerable<AxiomViolation> violations,
        int axiomsEvaluated = 0,
        int rulesEvaluated = 0) =>
        new()
        {
            Violations = violations.ToList(),
            AxiomsEvaluated = axiomsEvaluated,
            RulesEvaluated = rulesEvaluated
        };
}
```

---

## 5. Usage Examples

### 5.1 Creating an Axiom Programmatically

```csharp
var axiom = new Axiom
{
    Id = "endpoint-must-have-method",
    Name = "Endpoint Method Required",
    Description = "Every endpoint must specify exactly one HTTP method",
    TargetType = "Endpoint",
    TargetKind = AxiomTargetKind.Entity,
    Severity = AxiomSeverity.Error,
    Category = "API Documentation",
    Tags = new[] { "api", "endpoint", "required" },
    Rules = new[]
    {
        new AxiomRule
        {
            Property = "method",
            Constraint = AxiomConstraintType.Required,
            ErrorMessage = "Endpoint must specify an HTTP method"
        },
        new AxiomRule
        {
            Property = "method",
            Constraint = AxiomConstraintType.OneOf,
            Values = new object[] { "GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS" },
            ErrorMessage = "Method must be a valid HTTP method"
        }
    }
};
```

### 5.2 Creating a Violation

```csharp
var violation = new AxiomViolation
{
    Axiom = axiom,
    ViolatedRule = axiom.Rules[0],
    EntityId = Guid.Parse("..."),
    PropertyName = "method",
    ActualValue = null,
    ExpectedValue = "non-null value",
    Message = "Endpoint 'GET /users' missing required property 'method'",
    Severity = AxiomSeverity.Error,
    SuggestedFix = new AxiomFix
    {
        Description = "Add method property",
        PropertyName = "method",
        NewValue = "GET",
        Confidence = 0.9f,
        CanAutoApply = true
    }
};
```

---

## 6. YAML Serialization Format

```yaml
# Example axiom YAML (parsed by AxiomLoader)
axiom_version: "1.0"
name: "API Documentation Axioms"

axioms:
  - id: "endpoint-must-have-method"
    name: "Endpoint Method Required"
    description: "Every endpoint must specify exactly one HTTP method"
    target_type: Endpoint
    target_kind: Entity
    severity: error
    category: "API Documentation"
    tags: [api, endpoint, required]
    rules:
      - property: method
        constraint: required
        error_message: "Endpoint must specify an HTTP method"
      - property: method
        constraint: one_of
        values: [GET, POST, PUT, PATCH, DELETE, HEAD, OPTIONS]

  - id: "parameter-required-no-default"
    name: "Required Parameter Cannot Have Default"
    target_type: Parameter
    severity: warning
    rules:
      - constraint: not_both
        properties: [required, default_value]
        when:
          property: required
          operator: equals
          value: true
```

---

## 7. Unit Testing Requirements

```csharp
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.6e")]
public class AxiomDataModelTests
{
    [Fact]
    public void Axiom_WithRequiredProperties_CreatesSuccessfully()
    {
        // Arrange & Act
        var axiom = new Axiom
        {
            Id = "test-axiom",
            Name = "Test Axiom",
            TargetType = "TestEntity",
            Rules = new[] { new AxiomRule { Constraint = AxiomConstraintType.Required, Property = "name" } }
        };

        // Assert
        axiom.Id.Should().Be("test-axiom");
        axiom.IsEnabled.Should().BeTrue();
        axiom.Severity.Should().Be(AxiomSeverity.Error);
    }

    [Fact]
    public void AxiomValidationResult_WithNoViolations_IsValid()
    {
        // Arrange & Act
        var result = AxiomValidationResult.Valid(axiomsEvaluated: 5);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorCount.Should().Be(0);
    }

    [Fact]
    public void AxiomValidationResult_WithErrors_IsNotValid()
    {
        // Arrange
        var violation = CreateErrorViolation();

        // Act
        var result = AxiomValidationResult.WithViolations(new[] { violation });

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorCount.Should().Be(1);
    }

    [Fact]
    public void AxiomValidationResult_WithWarningsOnly_IsValid()
    {
        // Arrange
        var violation = CreateWarningViolation();

        // Act
        var result = AxiomValidationResult.WithViolations(new[] { violation });

        // Assert
        result.IsValid.Should().BeTrue();
        result.WarningCount.Should().Be(1);
    }

    [Fact]
    public void TextSpan_Length_CalculatesCorrectly()
    {
        // Arrange
        var span = new TextSpan { Start = 10, End = 25, Line = 1, Column = 10 };

        // Act & Assert
        span.Length.Should().Be(15);
    }
}
```

---

## 8. Acceptance Criteria (QA)

| # | Criterion |
| :- | :-------- |
| 1 | `Axiom` record can be instantiated with required properties. |
| 2 | `AxiomRule` supports all defined constraint types. |
| 3 | `AxiomViolation` captures all context needed for display. |
| 4 | `AxiomValidationResult.IsValid` returns false only for errors. |
| 5 | `AxiomCondition` supports all defined operators. |
| 6 | `AxiomFix` captures auto-apply capability. |
| 7 | All enums have complete value coverage. |
| 8 | Records are immutable (init-only properties). |

---

## 9. Deliverable Checklist

| # | Deliverable | Status |
| :- | :---------- | :----- |
| 1 | `Axiom` record | [ ] |
| 2 | `AxiomRule` record | [ ] |
| 3 | `AxiomViolation` record | [ ] |
| 4 | `AxiomValidationResult` record | [ ] |
| 5 | `AxiomFix` record | [ ] |
| 6 | `AxiomCondition` record | [ ] |
| 7 | `TextSpan` record | [ ] |
| 8 | `AxiomSeverity` enum | [ ] |
| 9 | `AxiomTargetKind` enum | [ ] |
| 10 | `AxiomConstraintType` enum | [ ] |
| 11 | `ConditionOperator` enum | [ ] |
| 12 | Unit tests | [ ] |

---

## 10. Changelog Entry

```markdown
### Added (v0.4.6e)

- `Axiom` record for foundational domain rules
- `AxiomRule` record for constraint definitions
- `AxiomViolation` record for validation findings
- `AxiomValidationResult` record for aggregated results
- `AxiomConstraintType` enum with 14 constraint types
- Support for conditional axiom rules via `AxiomCondition`
- `AxiomFix` for suggested violation fixes
```

---
