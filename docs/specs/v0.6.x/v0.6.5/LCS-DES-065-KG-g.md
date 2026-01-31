# LCS-DES-065-KG-g: Axiom Validator

## Document Control

| Field | Value |
| :--- | :--- |
| **Spec ID** | LCS-DES-065-KG-g |
| **System Breakdown** | LCS-SBD-065-KG |
| **Version** | v0.6.5 |
| **Codename** | Axiom Validator (CKVS Phase 3a) |
| **Estimated Hours** | 8 |
| **Status** | Draft |
| **Last Updated** | 2026-01-31 |

---

## 1. Overview

### 1.1 Purpose

The **Axiom Validator** validates claims against the foundational domain rules stored in the Axiom Store. It enforces property constraints, relationship validity, cardinality rules, and business logic axioms to ensure documentation is semantically correct.

### 1.2 Key Responsibilities

- Validate claims against applicable axioms
- Evaluate property constraint axioms (equality, range, cardinality)
- Check relationship axioms (valid from/to types)
- Enforce business rule axioms (domain-specific logic)
- Generate fix suggestions for axiom violations
- Support conditional axiom evaluation

### 1.3 Module Location

```
src/
  Lexichord.KnowledgeGraph/
    Validation/
      Validators/
        Axiom/
          IAxiomValidator.cs
          AxiomValidator.cs
          AxiomMatcher.cs
          AxiomEvaluator.cs
```

---

## 2. Interface Definitions

### 2.1 Axiom Validator Interface

```csharp
namespace Lexichord.KnowledgeGraph.Validation.Validators.Axiom;

/// <summary>
/// Validates claims against axioms from the Axiom Store.
/// </summary>
public interface IAxiomValidator : IValidator
{
    /// <summary>
    /// Validates a single claim against all applicable axioms.
    /// </summary>
    /// <param name="claim">Claim to validate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Axiom validation findings.</returns>
    Task<IReadOnlyList<ValidationFinding>> ValidateClaimAsync(
        Claim claim,
        CancellationToken ct = default);

    /// <summary>
    /// Validates multiple claims in batch.
    /// </summary>
    /// <param name="claims">Claims to validate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>All axiom validation findings.</returns>
    Task<IReadOnlyList<ValidationFinding>> ValidateClaimsAsync(
        IReadOnlyList<Claim> claims,
        CancellationToken ct = default);

    /// <summary>
    /// Gets applicable axioms for a claim.
    /// </summary>
    /// <param name="claim">Claim to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Axioms that apply to this claim.</returns>
    Task<IReadOnlyList<Axiom>> GetApplicableAxiomsAsync(
        Claim claim,
        CancellationToken ct = default);
}
```

### 2.2 Axiom Matcher Interface

```csharp
/// <summary>
/// Matches claims to applicable axioms.
/// </summary>
public interface IAxiomMatcher
{
    /// <summary>
    /// Finds all axioms that apply to a claim.
    /// </summary>
    /// <param name="claim">Claim to match.</param>
    /// <param name="axioms">Available axioms.</param>
    /// <returns>Matching axioms.</returns>
    IReadOnlyList<Axiom> FindMatchingAxioms(
        Claim claim,
        IReadOnlyList<Axiom> axioms);

    /// <summary>
    /// Checks if an axiom applies to a claim.
    /// </summary>
    /// <param name="axiom">Axiom to check.</param>
    /// <param name="claim">Claim to check against.</param>
    /// <returns>True if axiom applies.</returns>
    bool DoesAxiomApply(Axiom axiom, Claim claim);
}
```

### 2.3 Axiom Evaluator Interface

```csharp
/// <summary>
/// Evaluates axiom rules against claims.
/// </summary>
public interface IAxiomEvaluator
{
    /// <summary>
    /// Evaluates an axiom against a claim.
    /// </summary>
    /// <param name="axiom">Axiom to evaluate.</param>
    /// <param name="claim">Claim to evaluate against.</param>
    /// <param name="context">Evaluation context.</param>
    /// <returns>Evaluation result.</returns>
    Task<AxiomEvaluationResult> EvaluateAsync(
        Axiom axiom,
        Claim claim,
        AxiomEvaluationContext context);
}

/// <summary>
/// Context for axiom evaluation.
/// </summary>
public record AxiomEvaluationContext
{
    /// <summary>
    /// All claims in the document (for cross-claim checks).
    /// </summary>
    public IReadOnlyList<Claim> AllClaims { get; init; } = [];

    /// <summary>
    /// Linked entities (for reference resolution).
    /// </summary>
    public IReadOnlyList<LinkedEntity> LinkedEntities { get; init; } = [];

    /// <summary>
    /// Graph repository for entity lookups.
    /// </summary>
    public IGraphRepository? GraphRepository { get; init; }
}

/// <summary>
/// Result of axiom evaluation.
/// </summary>
public record AxiomEvaluationResult
{
    /// <summary>
    /// Whether the axiom was satisfied.
    /// </summary>
    public bool IsSatisfied { get; init; }

    /// <summary>
    /// Violation message if not satisfied.
    /// </summary>
    public string? ViolationMessage { get; init; }

    /// <summary>
    /// Suggested fix for violation.
    /// </summary>
    public ValidationFix? SuggestedFix { get; init; }

    /// <summary>
    /// Additional context about the evaluation.
    /// </summary>
    public IReadOnlyDictionary<string, object>? EvaluationDetails { get; init; }
}
```

---

## 3. Data Types

### 3.1 Axiom Finding Codes

```csharp
/// <summary>
/// Axiom validation finding codes.
/// </summary>
public static class AxiomFindingCodes
{
    public const string AxiomViolation = "AXIOM_VIOLATION";
    public const string PropertyConstraint = "AXIOM_PROPERTY_CONSTRAINT";
    public const string RelationshipInvalid = "AXIOM_RELATIONSHIP_INVALID";
    public const string CardinalityViolation = "AXIOM_CARDINALITY";
    public const string BusinessRuleViolation = "AXIOM_BUSINESS_RULE";
    public const string ConditionalViolation = "AXIOM_CONDITIONAL";
    public const string TypeMismatch = "AXIOM_TYPE_MISMATCH";
    public const string MutualExclusionViolation = "AXIOM_MUTUAL_EXCLUSION";
    public const string DependencyViolation = "AXIOM_DEPENDENCY";
}
```

### 3.2 Axiom Rule Types

```csharp
/// <summary>
/// Types of axiom rules for evaluation.
/// </summary>
public enum AxiomRuleType
{
    /// <summary>Property must equal a value.</summary>
    PropertyEquals,

    /// <summary>Property must not equal a value.</summary>
    PropertyNotEquals,

    /// <summary>Property must be in range.</summary>
    PropertyRange,

    /// <summary>Property must match pattern.</summary>
    PropertyPattern,

    /// <summary>Relationship must have valid types.</summary>
    RelationshipTypes,

    /// <summary>Cardinality constraint (min/max).</summary>
    Cardinality,

    /// <summary>Properties are mutually exclusive.</summary>
    MutualExclusion,

    /// <summary>Property depends on another property.</summary>
    Dependency,

    /// <summary>Custom expression.</summary>
    Expression,

    /// <summary>Cross-claim consistency.</summary>
    CrossClaim
}
```

---

## 4. Implementation

### 4.1 Axiom Validator Implementation

```csharp
/// <summary>
/// Axiom validator implementation.
/// </summary>
public class AxiomValidator : IAxiomValidator
{
    private readonly IAxiomStore _axiomStore;
    private readonly IAxiomMatcher _matcher;
    private readonly IAxiomEvaluator _evaluator;
    private readonly IGraphRepository _graphRepository;
    private readonly ILogger<AxiomValidator> _logger;

    public string Name => "AxiomValidator";
    public int Priority => 20; // Run after schema validator
    public LicenseTier RequiredTier => LicenseTier.Teams;
    public bool SupportsStreaming => true;

    public AxiomValidator(
        IAxiomStore axiomStore,
        IAxiomMatcher matcher,
        IAxiomEvaluator evaluator,
        IGraphRepository graphRepository,
        ILogger<AxiomValidator> logger)
    {
        _axiomStore = axiomStore;
        _matcher = matcher;
        _evaluator = evaluator;
        _graphRepository = graphRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ValidationFinding>> ValidateAsync(
        ValidationContext context,
        CancellationToken ct = default)
    {
        if (context.Claims.Count == 0)
        {
            _logger.LogDebug("No claims to validate against axioms");
            return [];
        }

        return await ValidateClaimsAsync(context.Claims, ct);
    }

    public async Task<IReadOnlyList<ValidationFinding>> ValidateClaimAsync(
        Claim claim,
        CancellationToken ct = default)
    {
        var findings = new List<ValidationFinding>();

        var applicableAxioms = await GetApplicableAxiomsAsync(claim, ct);
        _logger.LogDebug(
            "Found {Count} applicable axioms for claim {ClaimId}",
            applicableAxioms.Count,
            claim.Id);

        var evalContext = new AxiomEvaluationContext
        {
            AllClaims = [claim],
            GraphRepository = _graphRepository
        };

        foreach (var axiom in applicableAxioms)
        {
            var result = await _evaluator.EvaluateAsync(axiom, claim, evalContext);

            if (!result.IsSatisfied)
            {
                findings.Add(CreateFinding(axiom, claim, result));
            }
        }

        return findings;
    }

    public async Task<IReadOnlyList<ValidationFinding>> ValidateClaimsAsync(
        IReadOnlyList<Claim> claims,
        CancellationToken ct = default)
    {
        var findings = new List<ValidationFinding>();

        // Get all axioms once
        var allAxioms = await _axiomStore.GetAllAxiomsAsync(ct);

        var evalContext = new AxiomEvaluationContext
        {
            AllClaims = claims,
            GraphRepository = _graphRepository
        };

        foreach (var claim in claims)
        {
            var applicableAxioms = _matcher.FindMatchingAxioms(claim, allAxioms);

            foreach (var axiom in applicableAxioms)
            {
                ct.ThrowIfCancellationRequested();

                var result = await _evaluator.EvaluateAsync(axiom, claim, evalContext);

                if (!result.IsSatisfied)
                {
                    findings.Add(CreateFinding(axiom, claim, result));
                }
            }
        }

        return findings;
    }

    public async Task<IReadOnlyList<Axiom>> GetApplicableAxiomsAsync(
        Claim claim,
        CancellationToken ct = default)
    {
        var allAxioms = await _axiomStore.GetAllAxiomsAsync(ct);
        return _matcher.FindMatchingAxioms(claim, allAxioms);
    }

    public async IAsyncEnumerable<ValidationFinding> ValidateStreamingAsync(
        ValidationContext context,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var allAxioms = await _axiomStore.GetAllAxiomsAsync(ct);

        var evalContext = new AxiomEvaluationContext
        {
            AllClaims = context.Claims,
            LinkedEntities = context.LinkedEntities,
            GraphRepository = _graphRepository
        };

        foreach (var claim in context.Claims)
        {
            var applicableAxioms = _matcher.FindMatchingAxioms(claim, allAxioms);

            foreach (var axiom in applicableAxioms)
            {
                var result = await _evaluator.EvaluateAsync(axiom, claim, evalContext);

                if (!result.IsSatisfied)
                {
                    yield return CreateFinding(axiom, claim, result);
                }
            }
        }
    }

    private ValidationFinding CreateFinding(
        Axiom axiom,
        Claim claim,
        AxiomEvaluationResult result)
    {
        return new ValidationFinding
        {
            ValidatorName = Name,
            Code = GetFindingCode(axiom),
            Message = result.ViolationMessage ?? $"Axiom '{axiom.Name}' violated",
            Severity = MapSeverity(axiom.Severity),
            RelatedClaim = claim,
            ViolatedAxiom = axiom,
            SuggestedFix = result.SuggestedFix,
            Metadata = result.EvaluationDetails
        };
    }

    private string GetFindingCode(Axiom axiom)
    {
        return axiom.Type switch
        {
            AxiomType.PropertyConstraint => AxiomFindingCodes.PropertyConstraint,
            AxiomType.RelationshipConstraint => AxiomFindingCodes.RelationshipInvalid,
            AxiomType.CardinalityConstraint => AxiomFindingCodes.CardinalityViolation,
            AxiomType.MutualExclusion => AxiomFindingCodes.MutualExclusionViolation,
            AxiomType.Dependency => AxiomFindingCodes.DependencyViolation,
            _ => AxiomFindingCodes.AxiomViolation
        };
    }

    private ValidationSeverity MapSeverity(AxiomSeverity severity)
    {
        return severity switch
        {
            AxiomSeverity.Must => ValidationSeverity.Error,
            AxiomSeverity.Should => ValidationSeverity.Warning,
            AxiomSeverity.May => ValidationSeverity.Info,
            _ => ValidationSeverity.Warning
        };
    }
}
```

### 4.2 Axiom Matcher Implementation

```csharp
/// <summary>
/// Matches claims to applicable axioms.
/// </summary>
public class AxiomMatcher : IAxiomMatcher
{
    public IReadOnlyList<Axiom> FindMatchingAxioms(
        Claim claim,
        IReadOnlyList<Axiom> axioms)
    {
        return axioms
            .Where(a => DoesAxiomApply(a, claim))
            .OrderByDescending(a => a.Priority)
            .ToList();
    }

    public bool DoesAxiomApply(Axiom axiom, Claim claim)
    {
        // Check if axiom applies to this claim's subject type
        if (!string.IsNullOrEmpty(axiom.AppliesToType))
        {
            var subjectType = claim.Subject.EntityType;
            if (!string.Equals(subjectType, axiom.AppliesToType, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // Check if axiom applies to this predicate
        if (!string.IsNullOrEmpty(axiom.AppliesToPredicate))
        {
            if (!string.Equals(claim.Predicate, axiom.AppliesToPredicate, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // Check scope constraints
        if (axiom.ScopeConstraints != null)
        {
            foreach (var constraint in axiom.ScopeConstraints)
            {
                if (!EvaluateScopeConstraint(constraint, claim))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private bool EvaluateScopeConstraint(ScopeConstraint constraint, Claim claim)
    {
        return constraint.Type switch
        {
            ScopeConstraintType.SubjectProperty =>
                HasProperty(claim.Subject, constraint.PropertyName, constraint.PropertyValue),

            ScopeConstraintType.ObjectProperty when claim.Object.EntityValue != null =>
                HasProperty(claim.Object.EntityValue, constraint.PropertyName, constraint.PropertyValue),

            ScopeConstraintType.PredicatePattern =>
                Regex.IsMatch(claim.Predicate, constraint.Pattern ?? ".*"),

            _ => true
        };
    }

    private bool HasProperty(ClaimEntity entity, string? propertyName, string? expectedValue)
    {
        if (string.IsNullOrEmpty(propertyName)) return true;

        // Would need to look up entity properties from graph
        // For now, return true (optimistic matching)
        return true;
    }
}

/// <summary>
/// Scope constraint for axiom matching.
/// </summary>
public record ScopeConstraint
{
    public ScopeConstraintType Type { get; init; }
    public string? PropertyName { get; init; }
    public string? PropertyValue { get; init; }
    public string? Pattern { get; init; }
}

public enum ScopeConstraintType
{
    SubjectProperty,
    ObjectProperty,
    PredicatePattern
}
```

### 4.3 Axiom Evaluator Implementation

```csharp
/// <summary>
/// Evaluates axiom rules against claims.
/// </summary>
public class AxiomEvaluator : IAxiomEvaluator
{
    private readonly IGraphRepository _graphRepository;
    private readonly ILogger<AxiomEvaluator> _logger;

    public AxiomEvaluator(
        IGraphRepository graphRepository,
        ILogger<AxiomEvaluator> logger)
    {
        _graphRepository = graphRepository;
        _logger = logger;
    }

    public async Task<AxiomEvaluationResult> EvaluateAsync(
        Axiom axiom,
        Claim claim,
        AxiomEvaluationContext context)
    {
        return axiom.Type switch
        {
            AxiomType.PropertyConstraint => await EvaluatePropertyConstraintAsync(axiom, claim, context),
            AxiomType.RelationshipConstraint => await EvaluateRelationshipConstraintAsync(axiom, claim, context),
            AxiomType.CardinalityConstraint => await EvaluateCardinalityAsync(axiom, claim, context),
            AxiomType.MutualExclusion => EvaluateMutualExclusion(axiom, claim, context),
            AxiomType.Dependency => await EvaluateDependencyAsync(axiom, claim, context),
            AxiomType.Expression => EvaluateExpression(axiom, claim, context),
            _ => new AxiomEvaluationResult { IsSatisfied = true }
        };
    }

    private async Task<AxiomEvaluationResult> EvaluatePropertyConstraintAsync(
        Axiom axiom,
        Claim claim,
        AxiomEvaluationContext context)
    {
        var rule = axiom.PropertyRule;
        if (rule == null) return new AxiomEvaluationResult { IsSatisfied = true };

        // Get entity from graph to check properties
        var entity = await _graphRepository.GetEntityByIdAsync(claim.Subject.EntityId);
        if (entity == null)
        {
            return new AxiomEvaluationResult
            {
                IsSatisfied = true // Can't validate without entity
            };
        }

        var propertyValue = entity.Properties.GetValueOrDefault(rule.PropertyName);

        var isSatisfied = rule.Operator switch
        {
            PropertyOperator.Equals => Equals(propertyValue, rule.ExpectedValue),
            PropertyOperator.NotEquals => !Equals(propertyValue, rule.ExpectedValue),
            PropertyOperator.GreaterThan => Compare(propertyValue, rule.ExpectedValue) > 0,
            PropertyOperator.LessThan => Compare(propertyValue, rule.ExpectedValue) < 0,
            PropertyOperator.Contains => propertyValue?.ToString()?.Contains(rule.ExpectedValue?.ToString() ?? "") ?? false,
            PropertyOperator.Matches => Regex.IsMatch(propertyValue?.ToString() ?? "", rule.ExpectedValue?.ToString() ?? ""),
            PropertyOperator.IsSet => propertyValue != null,
            PropertyOperator.IsNotSet => propertyValue == null,
            _ => true
        };

        if (!isSatisfied)
        {
            return new AxiomEvaluationResult
            {
                IsSatisfied = false,
                ViolationMessage = $"Property '{rule.PropertyName}' {rule.Operator} '{rule.ExpectedValue}' " +
                                   $"but actual value is '{propertyValue}'",
                SuggestedFix = CreatePropertyFix(rule),
                EvaluationDetails = new Dictionary<string, object>
                {
                    ["propertyName"] = rule.PropertyName,
                    ["expectedValue"] = rule.ExpectedValue ?? "null",
                    ["actualValue"] = propertyValue ?? "null"
                }
            };
        }

        return new AxiomEvaluationResult { IsSatisfied = true };
    }

    private async Task<AxiomEvaluationResult> EvaluateRelationshipConstraintAsync(
        Axiom axiom,
        Claim claim,
        AxiomEvaluationContext context)
    {
        var rule = axiom.RelationshipRule;
        if (rule == null) return new AxiomEvaluationResult { IsSatisfied = true };

        // Check if subject type is allowed
        if (rule.AllowedFromTypes != null && rule.AllowedFromTypes.Count > 0)
        {
            if (!rule.AllowedFromTypes.Contains(claim.Subject.EntityType, StringComparer.OrdinalIgnoreCase))
            {
                return new AxiomEvaluationResult
                {
                    IsSatisfied = false,
                    ViolationMessage = $"Relationship '{claim.Predicate}' not allowed from type " +
                                       $"'{claim.Subject.EntityType}'. Allowed: {string.Join(", ", rule.AllowedFromTypes)}"
                };
            }
        }

        // Check if object type is allowed (for entity objects)
        if (rule.AllowedToTypes != null && rule.AllowedToTypes.Count > 0 && claim.Object.EntityValue != null)
        {
            if (!rule.AllowedToTypes.Contains(claim.Object.EntityValue.EntityType, StringComparer.OrdinalIgnoreCase))
            {
                return new AxiomEvaluationResult
                {
                    IsSatisfied = false,
                    ViolationMessage = $"Relationship '{claim.Predicate}' not allowed to type " +
                                       $"'{claim.Object.EntityValue.EntityType}'. Allowed: {string.Join(", ", rule.AllowedToTypes)}"
                };
            }
        }

        return new AxiomEvaluationResult { IsSatisfied = true };
    }

    private async Task<AxiomEvaluationResult> EvaluateCardinalityAsync(
        Axiom axiom,
        Claim claim,
        AxiomEvaluationContext context)
    {
        var rule = axiom.CardinalityRule;
        if (rule == null) return new AxiomEvaluationResult { IsSatisfied = true };

        // Count claims with same subject and predicate
        var count = context.AllClaims.Count(c =>
            c.Subject.EntityId == claim.Subject.EntityId &&
            c.Predicate == claim.Predicate);

        if (rule.MinCount.HasValue && count < rule.MinCount)
        {
            return new AxiomEvaluationResult
            {
                IsSatisfied = false,
                ViolationMessage = $"Entity '{claim.Subject.SurfaceForm}' has {count} '{claim.Predicate}' " +
                                   $"relationships but minimum is {rule.MinCount}",
                EvaluationDetails = new Dictionary<string, object>
                {
                    ["actualCount"] = count,
                    ["minCount"] = rule.MinCount.Value
                }
            };
        }

        if (rule.MaxCount.HasValue && count > rule.MaxCount)
        {
            return new AxiomEvaluationResult
            {
                IsSatisfied = false,
                ViolationMessage = $"Entity '{claim.Subject.SurfaceForm}' has {count} '{claim.Predicate}' " +
                                   $"relationships but maximum is {rule.MaxCount}",
                EvaluationDetails = new Dictionary<string, object>
                {
                    ["actualCount"] = count,
                    ["maxCount"] = rule.MaxCount.Value
                }
            };
        }

        return new AxiomEvaluationResult { IsSatisfied = true };
    }

    private AxiomEvaluationResult EvaluateMutualExclusion(
        Axiom axiom,
        Claim claim,
        AxiomEvaluationContext context)
    {
        var rule = axiom.MutualExclusionRule;
        if (rule == null) return new AxiomEvaluationResult { IsSatisfied = true };

        // Find claims for same subject with mutually exclusive predicates
        var relatedClaims = context.AllClaims
            .Where(c => c.Subject.EntityId == claim.Subject.EntityId)
            .Where(c => rule.ExclusivePredicates.Contains(c.Predicate))
            .ToList();

        if (relatedClaims.Count > 1)
        {
            var conflicting = relatedClaims
                .Select(c => c.Predicate)
                .Distinct()
                .ToList();

            if (conflicting.Count > 1)
            {
                return new AxiomEvaluationResult
                {
                    IsSatisfied = false,
                    ViolationMessage = $"Mutually exclusive predicates found for '{claim.Subject.SurfaceForm}': " +
                                       $"{string.Join(", ", conflicting)}"
                };
            }
        }

        return new AxiomEvaluationResult { IsSatisfied = true };
    }

    private async Task<AxiomEvaluationResult> EvaluateDependencyAsync(
        Axiom axiom,
        Claim claim,
        AxiomEvaluationContext context)
    {
        var rule = axiom.DependencyRule;
        if (rule == null) return new AxiomEvaluationResult { IsSatisfied = true };

        // Check if dependent claim exists
        var dependentExists = context.AllClaims.Any(c =>
            c.Subject.EntityId == claim.Subject.EntityId &&
            c.Predicate == rule.RequiredPredicate);

        if (rule.MustExist && !dependentExists)
        {
            return new AxiomEvaluationResult
            {
                IsSatisfied = false,
                ViolationMessage = $"Claim '{claim.Predicate}' requires '{rule.RequiredPredicate}' to exist " +
                                   $"for entity '{claim.Subject.SurfaceForm}'"
            };
        }

        if (rule.MustNotExist && dependentExists)
        {
            return new AxiomEvaluationResult
            {
                IsSatisfied = false,
                ViolationMessage = $"Claim '{claim.Predicate}' cannot coexist with '{rule.RequiredPredicate}' " +
                                   $"for entity '{claim.Subject.SurfaceForm}'"
            };
        }

        return new AxiomEvaluationResult { IsSatisfied = true };
    }

    private AxiomEvaluationResult EvaluateExpression(
        Axiom axiom,
        Claim claim,
        AxiomEvaluationContext context)
    {
        // Expression evaluation would use a simple expression language
        // For now, return satisfied
        return new AxiomEvaluationResult { IsSatisfied = true };
    }

    private ValidationFix CreatePropertyFix(PropertyConstraintRule rule)
    {
        return new ValidationFix
        {
            Description = $"Set '{rule.PropertyName}' to '{rule.ExpectedValue}'",
            Confidence = rule.Operator == PropertyOperator.Equals ? 0.8f : 0.5f,
            CanAutoApply = false
        };
    }

    private int Compare(object? a, object? b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return -1;
        if (b == null) return 1;

        try
        {
            var da = Convert.ToDouble(a);
            var db = Convert.ToDouble(b);
            return da.CompareTo(db);
        }
        catch
        {
            return string.Compare(a.ToString(), b?.ToString(), StringComparison.Ordinal);
        }
    }
}
```

---

## 5. Axiom Rule Definitions

### 5.1 Property Constraint Rule

```csharp
/// <summary>
/// Rule for property value constraints.
/// </summary>
public record PropertyConstraintRule
{
    public required string PropertyName { get; init; }
    public PropertyOperator Operator { get; init; }
    public object? ExpectedValue { get; init; }
}

public enum PropertyOperator
{
    Equals,
    NotEquals,
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual,
    Contains,
    Matches,
    IsSet,
    IsNotSet
}
```

### 5.2 Relationship Constraint Rule

```csharp
/// <summary>
/// Rule for relationship type constraints.
/// </summary>
public record RelationshipConstraintRule
{
    public IReadOnlyList<string>? AllowedFromTypes { get; init; }
    public IReadOnlyList<string>? AllowedToTypes { get; init; }
    public bool RequiresBidirectional { get; init; }
}
```

### 5.3 Cardinality Rule

```csharp
/// <summary>
/// Rule for relationship cardinality.
/// </summary>
public record CardinalityRule
{
    public int? MinCount { get; init; }
    public int? MaxCount { get; init; }
}
```

---

## 6. Error Handling

| Error | Handling Strategy |
| :---- | :---------------- |
| Axiom store unavailable | Log warning, skip axiom validation |
| Entity not found in graph | Skip property checks, log info |
| Invalid axiom expression | Log error, return satisfied |
| Circular dependency | Detect cycles, return error |

---

## 7. Testing Requirements

### 7.1 Unit Tests

| Test Case | Description |
| :-------- | :---------- |
| `ValidateClaim_PropertyEquals` | Property equality axiom |
| `ValidateClaim_PropertyRange` | Property range axiom |
| `ValidateClaim_RelationshipTypes` | Relationship type constraints |
| `ValidateClaim_Cardinality` | Cardinality constraints |
| `ValidateClaim_MutualExclusion` | Mutually exclusive predicates |
| `ValidateClaim_Dependency` | Dependency axioms |
| `AxiomMatcher_FindsApplicableAxioms` | Correct matching |

### 7.2 Integration Tests

| Test Case | Description |
| :-------- | :---------- |
| `AxiomValidator_WithAxiomStore` | Integration with store |
| `AxiomValidator_WithGraphRepository` | Entity lookups work |

---

## 8. Performance Considerations

- **Axiom Caching:** Cache axioms from store
- **Batch Entity Lookups:** Prefetch entities for claims
- **Index Axioms by Type:** Quick lookup by entity type
- **Early Exit:** Stop after max findings

---

## 9. License Gating

| Tier | Access |
| :--- | :----- |
| Core | Not available |
| WriterPro | Not available |
| Teams | Full axiom validation |
| Enterprise | Custom axioms + validation |

---

## 10. Changelog

| Version | Date | Author | Changes |
| :------ | :--- | :----- | :------ |
| 1.0 | 2026-01-31 | Lead Architect | Initial creation |

---
