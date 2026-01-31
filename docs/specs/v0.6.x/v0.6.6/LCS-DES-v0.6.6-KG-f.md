# LCS-DES-066-KG-f: Pre-Generation Validator

## Document Control

| Field | Value |
| :--- | :--- |
| **Spec ID** | LCS-DES-066-KG-f |
| **System Breakdown** | LCS-SBD-066-KG |
| **Version** | v0.6.6 |
| **Codename** | Pre-Generation Validator (CKVS Phase 3b) |
| **Estimated Hours** | 4 |
| **Status** | Draft |
| **Last Updated** | 2026-01-31 |

---

## 1. Overview

### 1.1 Purpose

The **Pre-Generation Validator** validates the knowledge context and user request before sending to the LLM. It ensures the Co-pilot doesn't generate content based on contradictory or invalid context.

### 1.2 Key Responsibilities

- Validate knowledge context for internal consistency
- Check user request against current document state
- Detect potential contradictions before generation
- Block generation if critical errors exist
- Provide pre-generation warnings to user

### 1.3 Module Location

```
src/
  Lexichord.KnowledgeGraph/
    Copilot/
      Validation/
        IPreGenerationValidator.cs
        PreGenerationValidator.cs
        ContextConsistencyChecker.cs
```

---

## 2. Interface Definitions

### 2.1 Pre-Generation Validator Interface

```csharp
namespace Lexichord.KnowledgeGraph.Copilot.Validation;

/// <summary>
/// Validates context and request before LLM generation.
/// </summary>
public interface IPreGenerationValidator
{
    /// <summary>
    /// Validates context before generation.
    /// </summary>
    Task<PreValidationResult> ValidateAsync(
        CopilotRequest request,
        KnowledgeContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Quick check if generation should proceed.
    /// </summary>
    Task<bool> CanProceedAsync(
        CopilotRequest request,
        KnowledgeContext context,
        CancellationToken ct = default);
}
```

### 2.2 Context Consistency Checker Interface

```csharp
/// <summary>
/// Checks knowledge context for internal consistency.
/// </summary>
public interface IContextConsistencyChecker
{
    /// <summary>
    /// Checks context entities for conflicts.
    /// </summary>
    IReadOnlyList<ContextIssue> CheckConsistency(KnowledgeContext context);

    /// <summary>
    /// Checks if request conflicts with context.
    /// </summary>
    IReadOnlyList<ContextIssue> CheckRequestConsistency(
        CopilotRequest request,
        KnowledgeContext context);
}
```

---

## 3. Data Types

### 3.1 Pre-Validation Result

```csharp
/// <summary>
/// Result of pre-generation validation.
/// </summary>
public record PreValidationResult
{
    /// <summary>Whether generation can proceed.</summary>
    public bool CanProceed { get; init; }

    /// <summary>Issues found during validation.</summary>
    public required IReadOnlyList<ContextIssue> Issues { get; init; }

    /// <summary>Blocking issues that prevent generation.</summary>
    public IReadOnlyList<ContextIssue> BlockingIssues =>
        Issues.Where(i => i.Severity == ContextIssueSeverity.Error).ToList();

    /// <summary>Warnings that don't block generation.</summary>
    public IReadOnlyList<ContextIssue> Warnings =>
        Issues.Where(i => i.Severity == ContextIssueSeverity.Warning).ToList();

    /// <summary>Suggested modifications to context.</summary>
    public IReadOnlyList<ContextModification>? SuggestedModifications { get; init; }

    /// <summary>Message to show user.</summary>
    public string? UserMessage { get; init; }

    /// <summary>Creates a passing result.</summary>
    public static PreValidationResult Pass() => new()
    {
        CanProceed = true,
        Issues = []
    };

    /// <summary>Creates a blocking result.</summary>
    public static PreValidationResult Block(IReadOnlyList<ContextIssue> issues, string message) => new()
    {
        CanProceed = false,
        Issues = issues,
        UserMessage = message
    };
}
```

### 3.2 Context Issue

```csharp
/// <summary>
/// An issue found in the knowledge context.
/// </summary>
public record ContextIssue
{
    /// <summary>Issue code.</summary>
    public required string Code { get; init; }

    /// <summary>Issue message.</summary>
    public required string Message { get; init; }

    /// <summary>Issue severity.</summary>
    public ContextIssueSeverity Severity { get; init; }

    /// <summary>Related entity if applicable.</summary>
    public KnowledgeEntity? RelatedEntity { get; init; }

    /// <summary>Related axiom if applicable.</summary>
    public Axiom? RelatedAxiom { get; init; }

    /// <summary>Suggested resolution.</summary>
    public string? Resolution { get; init; }
}

public enum ContextIssueSeverity
{
    Error,   // Blocks generation
    Warning, // Warns but allows
    Info     // Informational
}
```

### 3.3 Context Issue Codes

```csharp
public static class ContextIssueCodes
{
    public const string ConflictingEntities = "PREVAL_CONFLICTING_ENTITIES";
    public const string MissingRequiredEntity = "PREVAL_MISSING_ENTITY";
    public const string AxiomViolation = "PREVAL_AXIOM_VIOLATION";
    public const string StaleContext = "PREVAL_STALE_CONTEXT";
    public const string EmptyContext = "PREVAL_EMPTY_CONTEXT";
    public const string RequestConflict = "PREVAL_REQUEST_CONFLICT";
    public const string AmbiguousRequest = "PREVAL_AMBIGUOUS_REQUEST";
    public const string UnsupportedEntityType = "PREVAL_UNSUPPORTED_TYPE";
}
```

### 3.4 Context Modification

```csharp
/// <summary>
/// Suggested modification to context.
/// </summary>
public record ContextModification
{
    public ContextModificationType Type { get; init; }
    public string Description { get; init; } = "";
    public KnowledgeEntity? EntityToAdd { get; init; }
    public Guid? EntityIdToRemove { get; init; }
}

public enum ContextModificationType
{
    AddEntity,
    RemoveEntity,
    UpdateEntity,
    RefreshContext
}
```

---

## 4. Implementation

### 4.1 Pre-Generation Validator

```csharp
public class PreGenerationValidator : IPreGenerationValidator
{
    private readonly IContextConsistencyChecker _consistencyChecker;
    private readonly IValidationEngine _validationEngine;
    private readonly ILogger<PreGenerationValidator> _logger;

    public PreGenerationValidator(
        IContextConsistencyChecker consistencyChecker,
        IValidationEngine validationEngine,
        ILogger<PreGenerationValidator> logger)
    {
        _consistencyChecker = consistencyChecker;
        _validationEngine = validationEngine;
        _logger = logger;
    }

    public async Task<PreValidationResult> ValidateAsync(
        CopilotRequest request,
        KnowledgeContext context,
        CancellationToken ct = default)
    {
        var issues = new List<ContextIssue>();

        // Check for empty context
        if (context.Entities.Count == 0)
        {
            issues.Add(new ContextIssue
            {
                Code = ContextIssueCodes.EmptyContext,
                Message = "No knowledge context available for this request",
                Severity = ContextIssueSeverity.Warning,
                Resolution = "Generation will proceed without domain knowledge"
            });
        }

        // Check context consistency
        var consistencyIssues = _consistencyChecker.CheckConsistency(context);
        issues.AddRange(consistencyIssues);

        // Check request against context
        var requestIssues = _consistencyChecker.CheckRequestConsistency(request, context);
        issues.AddRange(requestIssues);

        // Check axiom compliance of context
        if (context.Axioms?.Count > 0)
        {
            var axiomIssues = await CheckAxiomComplianceAsync(context, ct);
            issues.AddRange(axiomIssues);
        }

        // Determine if we can proceed
        var blockingIssues = issues.Where(i => i.Severity == ContextIssueSeverity.Error).ToList();
        var canProceed = blockingIssues.Count == 0;

        _logger.LogDebug(
            "Pre-validation: {IssueCount} issues, canProceed={CanProceed}",
            issues.Count, canProceed);

        return new PreValidationResult
        {
            CanProceed = canProceed,
            Issues = issues,
            UserMessage = canProceed
                ? null
                : $"Cannot generate: {string.Join("; ", blockingIssues.Select(i => i.Message))}"
        };
    }

    public async Task<bool> CanProceedAsync(
        CopilotRequest request,
        KnowledgeContext context,
        CancellationToken ct = default)
    {
        var result = await ValidateAsync(request, context, ct);
        return result.CanProceed;
    }

    private async Task<IReadOnlyList<ContextIssue>> CheckAxiomComplianceAsync(
        KnowledgeContext context,
        CancellationToken ct)
    {
        var issues = new List<ContextIssue>();

        // Validate entities against their axioms
        foreach (var entity in context.Entities)
        {
            var applicableAxioms = context.Axioms?
                .Where(a => a.AppliesToType == entity.Type)
                .ToList() ?? [];

            foreach (var axiom in applicableAxioms)
            {
                if (!EntitySatisfiesAxiom(entity, axiom))
                {
                    issues.Add(new ContextIssue
                    {
                        Code = ContextIssueCodes.AxiomViolation,
                        Message = $"Entity '{entity.Name}' violates axiom '{axiom.Name}'",
                        Severity = axiom.Severity == AxiomSeverity.Must
                            ? ContextIssueSeverity.Error
                            : ContextIssueSeverity.Warning,
                        RelatedEntity = entity,
                        RelatedAxiom = axiom,
                        Resolution = $"Axiom rule: {axiom.Description}"
                    });
                }
            }
        }

        return issues;
    }

    private bool EntitySatisfiesAxiom(KnowledgeEntity entity, Axiom axiom)
    {
        // Simplified axiom check - real implementation would use AxiomEvaluator
        if (axiom.PropertyRule == null) return true;

        var rule = axiom.PropertyRule;
        var value = entity.Properties.GetValueOrDefault(rule.PropertyName);

        return rule.Operator switch
        {
            PropertyOperator.IsSet => value != null,
            PropertyOperator.IsNotSet => value == null,
            PropertyOperator.Equals => Equals(value, rule.ExpectedValue),
            _ => true
        };
    }
}
```

### 4.2 Context Consistency Checker

```csharp
public class ContextConsistencyChecker : IContextConsistencyChecker
{
    public IReadOnlyList<ContextIssue> CheckConsistency(KnowledgeContext context)
    {
        var issues = new List<ContextIssue>();

        // Check for duplicate entities
        var duplicates = context.Entities
            .GroupBy(e => e.Name.ToLowerInvariant())
            .Where(g => g.Count() > 1);

        foreach (var dup in duplicates)
        {
            issues.Add(new ContextIssue
            {
                Code = ContextIssueCodes.ConflictingEntities,
                Message = $"Multiple entities with name '{dup.Key}' in context",
                Severity = ContextIssueSeverity.Warning,
                Resolution = "Consider which entity is most relevant"
            });
        }

        // Check for conflicting property values
        issues.AddRange(CheckPropertyConflicts(context.Entities));

        // Check relationship consistency
        if (context.Relationships != null)
        {
            issues.AddRange(CheckRelationshipConsistency(
                context.Entities, context.Relationships));
        }

        return issues;
    }

    public IReadOnlyList<ContextIssue> CheckRequestConsistency(
        CopilotRequest request,
        KnowledgeContext context)
    {
        var issues = new List<ContextIssue>();

        // Check if request references entities not in context
        var requestTerms = ExtractTerms(request.Query);
        var contextEntityNames = context.Entities
            .Select(e => e.Name.ToLowerInvariant())
            .ToHashSet();

        // Look for potential entity references in request
        foreach (var term in requestTerms)
        {
            if (LooksLikeEntityReference(term) && !contextEntityNames.Contains(term))
            {
                issues.Add(new ContextIssue
                {
                    Code = ContextIssueCodes.MissingRequiredEntity,
                    Message = $"Request mentions '{term}' but no matching entity in context",
                    Severity = ContextIssueSeverity.Info,
                    Resolution = "Entity may need to be added to knowledge graph"
                });
            }
        }

        // Check for ambiguous requests
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            issues.Add(new ContextIssue
            {
                Code = ContextIssueCodes.AmbiguousRequest,
                Message = "Request is empty or unclear",
                Severity = ContextIssueSeverity.Error
            });
        }

        return issues;
    }

    private IEnumerable<ContextIssue> CheckPropertyConflicts(
        IReadOnlyList<KnowledgeEntity> entities)
    {
        // Check if same-type entities have conflicting property values
        var byType = entities.GroupBy(e => e.Type);

        foreach (var typeGroup in byType)
        {
            var entitiesOfType = typeGroup.ToList();
            if (entitiesOfType.Count < 2) continue;

            // Find properties that exist in multiple entities
            var allProps = entitiesOfType
                .SelectMany(e => e.Properties.Keys)
                .Distinct();

            foreach (var prop in allProps)
            {
                var values = entitiesOfType
                    .Where(e => e.Properties.ContainsKey(prop))
                    .Select(e => (Entity: e, Value: e.Properties[prop]))
                    .ToList();

                // Check for obvious conflicts (e.g., "required: true" vs "required: false")
                if (values.Count > 1)
                {
                    var distinctValues = values
                        .Select(v => v.Value?.ToString())
                        .Distinct()
                        .ToList();

                    if (distinctValues.Count > 1 && IsLikelyConflict(prop, distinctValues))
                    {
                        yield return new ContextIssue
                        {
                            Code = ContextIssueCodes.ConflictingEntities,
                            Message = $"Property '{prop}' has conflicting values across {typeGroup.Key} entities",
                            Severity = ContextIssueSeverity.Warning
                        };
                    }
                }
            }
        }
    }

    private IEnumerable<ContextIssue> CheckRelationshipConsistency(
        IReadOnlyList<KnowledgeEntity> entities,
        IReadOnlyList<KnowledgeRelationship> relationships)
    {
        var entityIds = entities.Select(e => e.Id).ToHashSet();

        foreach (var rel in relationships)
        {
            // Check if both endpoints exist in context
            if (!entityIds.Contains(rel.FromEntityId))
            {
                yield return new ContextIssue
                {
                    Code = ContextIssueCodes.MissingRequiredEntity,
                    Message = $"Relationship references missing entity: {rel.FromEntityName}",
                    Severity = ContextIssueSeverity.Warning
                };
            }

            if (!entityIds.Contains(rel.ToEntityId))
            {
                yield return new ContextIssue
                {
                    Code = ContextIssueCodes.MissingRequiredEntity,
                    Message = $"Relationship references missing entity: {rel.ToEntityName}",
                    Severity = ContextIssueSeverity.Warning
                };
            }
        }
    }

    private IReadOnlyList<string> ExtractTerms(string query)
    {
        return query.ToLowerInvariant()
            .Split([' ', ',', '.', '/', '-', '_'], StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 2)
            .ToList();
    }

    private bool LooksLikeEntityReference(string term)
    {
        // Heuristics for entity-like terms
        return term.StartsWith("/") || // Path-like
               term.Contains("_") ||    // Snake_case
               char.IsUpper(term[0]);   // PascalCase
    }

    private bool IsLikelyConflict(string propertyName, List<string?> values)
    {
        // Boolean-like properties with different values are conflicts
        var boolProps = new[] { "required", "optional", "deprecated", "enabled", "disabled" };
        return boolProps.Any(p => propertyName.Contains(p, StringComparison.OrdinalIgnoreCase));
    }
}
```

---

## 5. Error Handling

| Error | Handling Strategy |
| :---- | :---------------- |
| Validation service unavailable | Allow generation with warning |
| Empty context | Warning, proceed without knowledge |
| Blocking issues | Return result with error message |
| Timeout | Allow generation, log warning |

---

## 6. Testing Requirements

| Test Case | Description |
| :-------- | :---------- |
| `Validate_EmptyContext_ReturnsWarning` | Empty context warning |
| `Validate_ConflictingEntities_ReturnsWarning` | Detects conflicts |
| `Validate_AxiomViolation_BlocksGeneration` | Blocks on MUST axiom |
| `Validate_CleanContext_Passes` | Clean context passes |
| `CheckRequestConsistency_MissingEntity` | Missing entity detected |

---

## 7. License Gating

| Tier | Access |
| :--- | :----- |
| Core | Not available |
| WriterPro | Basic validation |
| Teams | Full pre-validation |
| Enterprise | Full + custom rules |

---

## 8. Changelog

| Version | Date | Author | Changes |
| :------ | :--- | :----- | :------ |
| 1.0 | 2026-01-31 | Lead Architect | Initial creation |
