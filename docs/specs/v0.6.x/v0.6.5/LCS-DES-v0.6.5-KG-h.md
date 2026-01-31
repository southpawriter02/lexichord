# LCS-DES-065-KG-h: Consistency Checker

## Document Control

| Field | Value |
| :--- | :--- |
| **Spec ID** | LCS-DES-065-KG-h |
| **System Breakdown** | LCS-SBD-065-KG |
| **Version** | v0.6.5 |
| **Codename** | Consistency Checker (CKVS Phase 3a) |
| **Estimated Hours** | 8 |
| **Status** | Draft |
| **Last Updated** | 2026-01-31 |

---

## 1. Overview

### 1.1 Purpose

The **Consistency Checker** detects contradictions between new claims and existing knowledge in the Knowledge Graph. It identifies conflicting property values, contradictory relationships, and temporal inconsistencies to ensure documentation remains coherent.

### 1.2 Key Responsibilities

- Detect contradictions between new and existing claims
- Identify conflicting property values for the same entity
- Find contradictory relationships
- Handle temporal/versioned claim conflicts
- Leverage claim diff service for semantic comparison
- Generate resolution suggestions for conflicts

### 1.3 Module Location

```
src/
  Lexichord.KnowledgeGraph/
    Validation/
      Validators/
        Consistency/
          IConsistencyChecker.cs
          ConsistencyChecker.cs
          ConflictDetector.cs
          ContradictionResolver.cs
```

---

## 2. Interface Definitions

### 2.1 Consistency Checker Interface

```csharp
namespace Lexichord.KnowledgeGraph.Validation.Validators.Consistency;

/// <summary>
/// Checks consistency between new claims and existing knowledge.
/// </summary>
public interface IConsistencyChecker : IValidator
{
    /// <summary>
    /// Checks a claim for conflicts with existing knowledge.
    /// </summary>
    /// <param name="claim">Claim to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Consistency findings.</returns>
    Task<IReadOnlyList<ValidationFinding>> CheckClaimConsistencyAsync(
        Claim claim,
        CancellationToken ct = default);

    /// <summary>
    /// Checks multiple claims for conflicts.
    /// </summary>
    /// <param name="claims">Claims to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>All consistency findings.</returns>
    Task<IReadOnlyList<ValidationFinding>> CheckClaimsConsistencyAsync(
        IReadOnlyList<Claim> claims,
        CancellationToken ct = default);

    /// <summary>
    /// Gets existing claims that may conflict with a new claim.
    /// </summary>
    /// <param name="claim">New claim to check.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Potentially conflicting claims.</returns>
    Task<IReadOnlyList<Claim>> GetPotentialConflictsAsync(
        Claim claim,
        CancellationToken ct = default);
}
```

### 2.2 Conflict Detector Interface

```csharp
/// <summary>
/// Detects specific types of conflicts between claims.
/// </summary>
public interface IConflictDetector
{
    /// <summary>
    /// Detects if two claims conflict.
    /// </summary>
    /// <param name="newClaim">New claim being validated.</param>
    /// <param name="existingClaim">Existing claim from knowledge base.</param>
    /// <returns>Conflict result.</returns>
    ConflictResult DetectConflict(Claim newClaim, Claim existingClaim);
}

/// <summary>
/// Result of conflict detection.
/// </summary>
public record ConflictResult
{
    /// <summary>
    /// Whether a conflict was detected.
    /// </summary>
    public bool HasConflict { get; init; }

    /// <summary>
    /// Type of conflict detected.
    /// </summary>
    public ConflictType ConflictType { get; init; }

    /// <summary>
    /// Confidence in conflict detection (0-1).
    /// </summary>
    public float Confidence { get; init; }

    /// <summary>
    /// Description of the conflict.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Suggested resolution.
    /// </summary>
    public ConflictResolution? SuggestedResolution { get; init; }
}

/// <summary>
/// Types of conflicts.
/// </summary>
public enum ConflictType
{
    /// <summary>No conflict detected.</summary>
    None,

    /// <summary>Direct value contradiction.</summary>
    ValueContradiction,

    /// <summary>Conflicting property values.</summary>
    PropertyConflict,

    /// <summary>Contradictory relationship.</summary>
    RelationshipContradiction,

    /// <summary>Temporal inconsistency.</summary>
    TemporalConflict,

    /// <summary>Cardinality violation.</summary>
    CardinalityConflict,

    /// <summary>Semantic contradiction (meaning conflicts).</summary>
    SemanticContradiction
}
```

### 2.3 Contradiction Resolver Interface

```csharp
/// <summary>
/// Suggests resolutions for detected conflicts.
/// </summary>
public interface IContradictionResolver
{
    /// <summary>
    /// Suggests a resolution for a conflict.
    /// </summary>
    /// <param name="newClaim">New claim.</param>
    /// <param name="existingClaim">Existing claim.</param>
    /// <param name="conflictType">Type of conflict.</param>
    /// <returns>Suggested resolution.</returns>
    ConflictResolution SuggestResolution(
        Claim newClaim,
        Claim existingClaim,
        ConflictType conflictType);
}

/// <summary>
/// Suggested resolution for a conflict.
/// </summary>
public record ConflictResolution
{
    /// <summary>
    /// Resolution strategy.
    /// </summary>
    public ResolutionStrategy Strategy { get; init; }

    /// <summary>
    /// Human-readable description.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Confidence in this resolution.
    /// </summary>
    public float Confidence { get; init; }

    /// <summary>
    /// Validation fix for the new claim.
    /// </summary>
    public ValidationFix? Fix { get; init; }

    /// <summary>
    /// Whether resolution can be auto-applied.
    /// </summary>
    public bool CanAutoApply { get; init; }
}

/// <summary>
/// Resolution strategies for conflicts.
/// </summary>
public enum ResolutionStrategy
{
    /// <summary>Keep the new claim, supersede existing.</summary>
    AcceptNew,

    /// <summary>Keep the existing claim, reject new.</summary>
    KeepExisting,

    /// <summary>Mark both as valid in different contexts.</summary>
    Contextualize,

    /// <summary>Merge claims into unified statement.</summary>
    Merge,

    /// <summary>Require manual resolution.</summary>
    ManualReview,

    /// <summary>Version the existing claim as historical.</summary>
    VersionExisting
}
```

---

## 3. Data Types

### 3.1 Consistency Finding Codes

```csharp
/// <summary>
/// Consistency validation finding codes.
/// </summary>
public static class ConsistencyFindingCodes
{
    public const string ConsistencyConflict = "CONSISTENCY_CONFLICT";
    public const string ValueContradiction = "CONSISTENCY_VALUE_CONTRADICTION";
    public const string PropertyConflict = "CONSISTENCY_PROPERTY_CONFLICT";
    public const string RelationshipConflict = "CONSISTENCY_RELATIONSHIP_CONFLICT";
    public const string TemporalConflict = "CONSISTENCY_TEMPORAL_CONFLICT";
    public const string SemanticConflict = "CONSISTENCY_SEMANTIC_CONFLICT";
    public const string DuplicateClaim = "CONSISTENCY_DUPLICATE";
}
```

### 3.2 Consistency Finding

```csharp
/// <summary>
/// Extended validation finding for consistency issues.
/// </summary>
public record ConsistencyFinding : ValidationFinding
{
    /// <summary>
    /// The existing claim that conflicts.
    /// </summary>
    public Claim? ExistingClaim { get; init; }

    /// <summary>
    /// Source document of existing claim.
    /// </summary>
    public Guid? ExistingClaimDocumentId { get; init; }

    /// <summary>
    /// Conflict type.
    /// </summary>
    public ConflictType ConflictType { get; init; }

    /// <summary>
    /// Conflict confidence.
    /// </summary>
    public float ConflictConfidence { get; init; }

    /// <summary>
    /// Suggested resolution.
    /// </summary>
    public ConflictResolution? Resolution { get; init; }
}
```

---

## 4. Implementation

### 4.1 Consistency Checker Implementation

```csharp
/// <summary>
/// Consistency checker implementation.
/// </summary>
public class ConsistencyChecker : IConsistencyChecker
{
    private readonly IClaimRepository _claimRepository;
    private readonly IConflictDetector _conflictDetector;
    private readonly IContradictionResolver _resolver;
    private readonly IClaimDiffService _diffService;
    private readonly ILogger<ConsistencyChecker> _logger;

    public string Name => "ConsistencyChecker";
    public int Priority => 30; // Run after axiom validator
    public LicenseTier RequiredTier => LicenseTier.Teams;
    public bool SupportsStreaming => false; // Requires full context

    public ConsistencyChecker(
        IClaimRepository claimRepository,
        IConflictDetector conflictDetector,
        IContradictionResolver resolver,
        IClaimDiffService diffService,
        ILogger<ConsistencyChecker> logger)
    {
        _claimRepository = claimRepository;
        _conflictDetector = conflictDetector;
        _resolver = resolver;
        _diffService = diffService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ValidationFinding>> ValidateAsync(
        ValidationContext context,
        CancellationToken ct = default)
    {
        if (context.Claims.Count == 0)
        {
            _logger.LogDebug("No claims to check for consistency");
            return [];
        }

        return await CheckClaimsConsistencyAsync(context.Claims, ct);
    }

    public async Task<IReadOnlyList<ValidationFinding>> CheckClaimConsistencyAsync(
        Claim claim,
        CancellationToken ct = default)
    {
        var findings = new List<ValidationFinding>();

        // Get potentially conflicting claims
        var potentialConflicts = await GetPotentialConflictsAsync(claim, ct);

        _logger.LogDebug(
            "Found {Count} potential conflicts for claim {ClaimId}",
            potentialConflicts.Count,
            claim.Id);

        foreach (var existingClaim in potentialConflicts)
        {
            var conflict = _conflictDetector.DetectConflict(claim, existingClaim);

            if (conflict.HasConflict)
            {
                var resolution = _resolver.SuggestResolution(
                    claim, existingClaim, conflict.ConflictType);

                findings.Add(CreateConsistencyFinding(
                    claim, existingClaim, conflict, resolution));
            }
        }

        // Check for semantic contradictions using diff service
        var semanticConflicts = await CheckSemanticConflictsAsync(claim, potentialConflicts, ct);
        findings.AddRange(semanticConflicts);

        return findings;
    }

    public async Task<IReadOnlyList<ValidationFinding>> CheckClaimsConsistencyAsync(
        IReadOnlyList<Claim> claims,
        CancellationToken ct = default)
    {
        var findings = new List<ValidationFinding>();

        // Check each claim against existing knowledge
        foreach (var claim in claims)
        {
            ct.ThrowIfCancellationRequested();
            var claimFindings = await CheckClaimConsistencyAsync(claim, ct);
            findings.AddRange(claimFindings);
        }

        // Also check for internal consistency (conflicts within the new claims)
        var internalConflicts = CheckInternalConsistency(claims);
        findings.AddRange(internalConflicts);

        return findings;
    }

    public async Task<IReadOnlyList<Claim>> GetPotentialConflictsAsync(
        Claim claim,
        CancellationToken ct = default)
    {
        // Search for claims about the same entity
        var query = new ClaimSearchQuery
        {
            SubjectEntityId = claim.Subject.EntityId,
            Predicates = [claim.Predicate],
            ExcludeClaimId = claim.Id
        };

        var results = await _claimRepository.SearchAsync(query, ct);

        // Also search for claims with same predicate but potentially related objects
        if (claim.Object.EntityValue != null)
        {
            var relatedQuery = new ClaimSearchQuery
            {
                SubjectEntityId = claim.Object.EntityValue.EntityId,
                ExcludeClaimId = claim.Id
            };
            var related = await _claimRepository.SearchAsync(relatedQuery, ct);
            results = results.Concat(related).Distinct().ToList();
        }

        return results;
    }

    public async IAsyncEnumerable<ValidationFinding> ValidateStreamingAsync(
        ValidationContext context,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // Consistency checking doesn't support true streaming
        // as it needs full context. Yield all at once.
        var findings = await ValidateAsync(context, ct);
        foreach (var finding in findings)
        {
            yield return finding;
        }
    }

    private async Task<IReadOnlyList<ValidationFinding>> CheckSemanticConflictsAsync(
        Claim newClaim,
        IReadOnlyList<Claim> existingClaims,
        CancellationToken ct)
    {
        var findings = new List<ValidationFinding>();

        foreach (var existing in existingClaims)
        {
            // Use claim diff service for semantic comparison
            var diff = await _diffService.CompareSingleAsync(newClaim, existing, ct);

            if (diff.ChangeType == ClaimChangeType.Modified &&
                diff.SemanticSimilarity < 0.5f) // Low similarity = potential contradiction
            {
                findings.Add(new ConsistencyFinding
                {
                    ValidatorName = Name,
                    Code = ConsistencyFindingCodes.SemanticConflict,
                    Message = $"Semantic conflict detected: new claim about '{newClaim.Subject.SurfaceForm}' " +
                              $"may contradict existing knowledge",
                    Severity = ValidationSeverity.Warning,
                    RelatedClaim = newClaim,
                    ExistingClaim = existing,
                    ConflictType = ConflictType.SemanticContradiction,
                    ConflictConfidence = 1.0f - diff.SemanticSimilarity
                });
            }
        }

        return findings;
    }

    private IReadOnlyList<ValidationFinding> CheckInternalConsistency(
        IReadOnlyList<Claim> claims)
    {
        var findings = new List<ValidationFinding>();

        // Group claims by subject
        var claimsBySubject = claims.GroupBy(c => c.Subject.EntityId);

        foreach (var group in claimsBySubject)
        {
            var subjectClaims = group.ToList();

            // Check for conflicting predicates within same subject
            var predicateGroups = subjectClaims.GroupBy(c => c.Predicate);

            foreach (var predGroup in predicateGroups)
            {
                var predClaims = predGroup.ToList();
                if (predClaims.Count <= 1) continue;

                // Check if multiple claims have conflicting objects
                for (int i = 0; i < predClaims.Count; i++)
                {
                    for (int j = i + 1; j < predClaims.Count; j++)
                    {
                        var conflict = _conflictDetector.DetectConflict(
                            predClaims[i], predClaims[j]);

                        if (conflict.HasConflict)
                        {
                            findings.Add(new ConsistencyFinding
                            {
                                ValidatorName = Name,
                                Code = ConsistencyFindingCodes.ConsistencyConflict,
                                Message = $"Internal conflict: document contains contradictory claims " +
                                          $"about '{predClaims[i].Subject.SurfaceForm}' {predClaims[i].Predicate}",
                                Severity = ValidationSeverity.Error,
                                RelatedClaim = predClaims[i],
                                ExistingClaim = predClaims[j],
                                ConflictType = conflict.ConflictType,
                                ConflictConfidence = conflict.Confidence
                            });
                        }
                    }
                }
            }
        }

        return findings;
    }

    private ConsistencyFinding CreateConsistencyFinding(
        Claim newClaim,
        Claim existingClaim,
        ConflictResult conflict,
        ConflictResolution resolution)
    {
        return new ConsistencyFinding
        {
            ValidatorName = Name,
            Code = GetFindingCode(conflict.ConflictType),
            Message = conflict.Description ?? $"Conflict with existing claim",
            Severity = conflict.Confidence > 0.8f
                ? ValidationSeverity.Error
                : ValidationSeverity.Warning,
            RelatedClaim = newClaim,
            ExistingClaim = existingClaim,
            ExistingClaimDocumentId = existingClaim.Evidence.DocumentId,
            ConflictType = conflict.ConflictType,
            ConflictConfidence = conflict.Confidence,
            Resolution = resolution,
            SuggestedFix = resolution.Fix
        };
    }

    private string GetFindingCode(ConflictType type)
    {
        return type switch
        {
            ConflictType.ValueContradiction => ConsistencyFindingCodes.ValueContradiction,
            ConflictType.PropertyConflict => ConsistencyFindingCodes.PropertyConflict,
            ConflictType.RelationshipContradiction => ConsistencyFindingCodes.RelationshipConflict,
            ConflictType.TemporalConflict => ConsistencyFindingCodes.TemporalConflict,
            ConflictType.SemanticContradiction => ConsistencyFindingCodes.SemanticConflict,
            _ => ConsistencyFindingCodes.ConsistencyConflict
        };
    }
}
```

### 4.2 Conflict Detector Implementation

```csharp
/// <summary>
/// Detects specific types of conflicts between claims.
/// </summary>
public class ConflictDetector : IConflictDetector
{
    public ConflictResult DetectConflict(Claim newClaim, Claim existingClaim)
    {
        // Same subject and predicate?
        if (newClaim.Subject.EntityId != existingClaim.Subject.EntityId)
        {
            return new ConflictResult { HasConflict = false, ConflictType = ConflictType.None };
        }

        if (newClaim.Predicate != existingClaim.Predicate)
        {
            // Check for semantic predicate conflicts
            return CheckPredicateConflict(newClaim, existingClaim);
        }

        // Same subject and predicate - check objects
        return CheckObjectConflict(newClaim, existingClaim);
    }

    private ConflictResult CheckObjectConflict(Claim newClaim, Claim existingClaim)
    {
        var newObj = newClaim.Object;
        var existingObj = existingClaim.Object;

        // Both are literals
        if (newObj.LiteralValue != null && existingObj.LiteralValue != null)
        {
            if (!ObjectsMatch(newObj.LiteralValue, existingObj.LiteralValue))
            {
                return new ConflictResult
                {
                    HasConflict = true,
                    ConflictType = ConflictType.ValueContradiction,
                    Confidence = 0.9f,
                    Description = $"Conflicting values: '{newObj.LiteralValue}' vs '{existingObj.LiteralValue}' " +
                                  $"for {newClaim.Subject.SurfaceForm} {newClaim.Predicate}"
                };
            }
        }

        // Both are entity references
        if (newObj.EntityValue != null && existingObj.EntityValue != null)
        {
            if (newObj.EntityValue.EntityId != existingObj.EntityValue.EntityId)
            {
                // Check if predicate allows multiple objects
                if (IsSingleValuePredicate(newClaim.Predicate))
                {
                    return new ConflictResult
                    {
                        HasConflict = true,
                        ConflictType = ConflictType.RelationshipContradiction,
                        Confidence = 0.85f,
                        Description = $"Conflicting relationships: {newClaim.Subject.SurfaceForm} " +
                                      $"{newClaim.Predicate} points to different entities"
                    };
                }
            }
        }

        // One literal, one entity
        if ((newObj.LiteralValue != null) != (existingObj.LiteralValue != null))
        {
            return new ConflictResult
            {
                HasConflict = true,
                ConflictType = ConflictType.ValueContradiction,
                Confidence = 0.7f,
                Description = "Object type mismatch: one is literal, other is entity reference"
            };
        }

        // Objects match - no conflict
        return new ConflictResult { HasConflict = false, ConflictType = ConflictType.None };
    }

    private ConflictResult CheckPredicateConflict(Claim newClaim, Claim existingClaim)
    {
        // Check for known contradictory predicate pairs
        var contradictions = new Dictionary<string, string[]>
        {
            ["IS_REQUIRED"] = ["IS_OPTIONAL"],
            ["IS_DEPRECATED"] = ["IS_ACTIVE", "IS_CURRENT"],
            ["ACCEPTS"] = ["REJECTS"],
            ["SUPPORTS"] = ["DOES_NOT_SUPPORT"]
        };

        foreach (var pair in contradictions)
        {
            if (newClaim.Predicate == pair.Key &&
                pair.Value.Contains(existingClaim.Predicate))
            {
                return new ConflictResult
                {
                    HasConflict = true,
                    ConflictType = ConflictType.RelationshipContradiction,
                    Confidence = 0.95f,
                    Description = $"Contradictory predicates: '{newClaim.Predicate}' vs '{existingClaim.Predicate}'"
                };
            }

            if (existingClaim.Predicate == pair.Key &&
                pair.Value.Contains(newClaim.Predicate))
            {
                return new ConflictResult
                {
                    HasConflict = true,
                    ConflictType = ConflictType.RelationshipContradiction,
                    Confidence = 0.95f,
                    Description = $"Contradictory predicates: '{newClaim.Predicate}' vs '{existingClaim.Predicate}'"
                };
            }
        }

        return new ConflictResult { HasConflict = false, ConflictType = ConflictType.None };
    }

    private bool ObjectsMatch(object a, object b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;

        // Try numeric comparison
        try
        {
            var da = Convert.ToDouble(a);
            var db = Convert.ToDouble(b);
            return Math.Abs(da - db) < 0.0001;
        }
        catch { }

        // String comparison
        return string.Equals(a.ToString(), b.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private bool IsSingleValuePredicate(string predicate)
    {
        // Predicates that typically have single values
        var singleValue = new[]
        {
            "HAS_METHOD", "HAS_TYPE", "HAS_STATUS", "RETURNS",
            "IS_REQUIRED", "IS_DEPRECATED", "HAS_DEFAULT"
        };

        return singleValue.Contains(predicate, StringComparer.OrdinalIgnoreCase);
    }
}
```

### 4.3 Contradiction Resolver Implementation

```csharp
/// <summary>
/// Suggests resolutions for detected conflicts.
/// </summary>
public class ContradictionResolver : IContradictionResolver
{
    public ConflictResolution SuggestResolution(
        Claim newClaim,
        Claim existingClaim,
        ConflictType conflictType)
    {
        return conflictType switch
        {
            ConflictType.ValueContradiction => ResolveValueContradiction(newClaim, existingClaim),
            ConflictType.PropertyConflict => ResolvePropertyConflict(newClaim, existingClaim),
            ConflictType.RelationshipContradiction => ResolveRelationshipConflict(newClaim, existingClaim),
            ConflictType.TemporalConflict => ResolveTemporalConflict(newClaim, existingClaim),
            ConflictType.SemanticContradiction => ResolveSemanticConflict(newClaim, existingClaim),
            _ => new ConflictResolution
            {
                Strategy = ResolutionStrategy.ManualReview,
                Description = "Manual review required to resolve this conflict",
                Confidence = 0.0f,
                CanAutoApply = false
            }
        };
    }

    private ConflictResolution ResolveValueContradiction(Claim newClaim, Claim existingClaim)
    {
        // Prefer newer claim if from more recent document
        if (newClaim.Evidence.Timestamp > existingClaim.Evidence.Timestamp)
        {
            return new ConflictResolution
            {
                Strategy = ResolutionStrategy.AcceptNew,
                Description = $"Accept new value '{newClaim.Object.LiteralValue}' as more recent",
                Confidence = 0.7f,
                CanAutoApply = false,
                Fix = new ValidationFix
                {
                    Description = "Update existing documentation to match",
                    Confidence = 0.7f,
                    CanAutoApply = false
                }
            };
        }

        return new ConflictResolution
        {
            Strategy = ResolutionStrategy.ManualReview,
            Description = $"Conflicting values: '{newClaim.Object.LiteralValue}' vs " +
                          $"'{existingClaim.Object.LiteralValue}'. Manual review needed.",
            Confidence = 0.5f,
            CanAutoApply = false
        };
    }

    private ConflictResolution ResolvePropertyConflict(Claim newClaim, Claim existingClaim)
    {
        return new ConflictResolution
        {
            Strategy = ResolutionStrategy.ManualReview,
            Description = "Property values conflict. Review both documents to determine correct value.",
            Confidence = 0.3f,
            CanAutoApply = false
        };
    }

    private ConflictResolution ResolveRelationshipConflict(Claim newClaim, Claim existingClaim)
    {
        // Check if this could be a versioning issue
        if (IsVersioningScenario(newClaim, existingClaim))
        {
            return new ConflictResolution
            {
                Strategy = ResolutionStrategy.VersionExisting,
                Description = "Mark existing claim as historical and accept new claim as current",
                Confidence = 0.6f,
                CanAutoApply = false
            };
        }

        return new ConflictResolution
        {
            Strategy = ResolutionStrategy.ManualReview,
            Description = "Contradictory relationships detected. Review and update documentation.",
            Confidence = 0.4f,
            CanAutoApply = false
        };
    }

    private ConflictResolution ResolveTemporalConflict(Claim newClaim, Claim existingClaim)
    {
        return new ConflictResolution
        {
            Strategy = ResolutionStrategy.VersionExisting,
            Description = "Create versioned history: mark existing as historical, accept new as current",
            Confidence = 0.8f,
            CanAutoApply = true
        };
    }

    private ConflictResolution ResolveSemanticConflict(Claim newClaim, Claim existingClaim)
    {
        return new ConflictResolution
        {
            Strategy = ResolutionStrategy.Contextualize,
            Description = "Claims may be valid in different contexts. Consider adding context qualifiers.",
            Confidence = 0.5f,
            CanAutoApply = false
        };
    }

    private bool IsVersioningScenario(Claim newClaim, Claim existingClaim)
    {
        // Check if subject mentions version
        var versionPattern = @"v\d+|version\s*\d+|V\d+";
        var newHasVersion = Regex.IsMatch(newClaim.Subject.SurfaceForm, versionPattern, RegexOptions.IgnoreCase);
        var existingHasVersion = Regex.IsMatch(existingClaim.Subject.SurfaceForm, versionPattern, RegexOptions.IgnoreCase);

        return newHasVersion || existingHasVersion;
    }
}
```

---

## 5. Error Handling

| Error | Handling Strategy |
| :---- | :---------------- |
| Claim repository unavailable | Log warning, skip consistency check |
| Diff service fails | Fall back to simple comparison |
| Too many potential conflicts | Limit to most relevant |
| Circular conflict detection | Track visited claims |

---

## 6. Testing Requirements

### 6.1 Unit Tests

| Test Case | Description |
| :-------- | :---------- |
| `CheckClaim_ValueContradiction` | Detects value conflicts |
| `CheckClaim_RelationshipContradiction` | Detects relationship conflicts |
| `CheckClaim_NoConflict` | Returns empty for consistent claims |
| `CheckClaims_InternalConsistency` | Detects internal conflicts |
| `ConflictDetector_SameSubjectDifferentValue` | Correct conflict type |
| `ContradictionResolver_SuggestsResolution` | Resolution suggestions |

### 6.2 Integration Tests

| Test Case | Description |
| :-------- | :---------- |
| `ConsistencyChecker_WithClaimRepository` | Integration with repository |
| `ConsistencyChecker_WithDiffService` | Semantic conflict detection |

---

## 7. Performance Considerations

- **Claim Indexing:** Index claims by subject entity for fast lookup
- **Batch Queries:** Fetch potential conflicts in batches
- **Caching:** Cache recent consistency checks
- **Limit Scope:** Only check most likely conflicts first
- **Async Execution:** Run conflict checks concurrently

---

## 8. License Gating

| Tier | Access |
| :--- | :----- |
| Core | Not available |
| WriterPro | Not available |
| Teams | Full consistency checking |
| Enterprise | Full + custom conflict rules |

---

## 9. Changelog

| Version | Date | Author | Changes |
| :------ | :--- | :----- | :------ |
| 1.0 | 2026-01-31 | Lead Architect | Initial creation |

---
