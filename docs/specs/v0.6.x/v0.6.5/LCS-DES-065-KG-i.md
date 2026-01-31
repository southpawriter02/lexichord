# LCS-DES-065-KG-i: Validation Result Aggregator

## Document Control

| Field | Value |
| :--- | :--- |
| **Spec ID** | LCS-DES-065-KG-i |
| **System Breakdown** | LCS-SBD-065-KG |
| **Version** | v0.6.5 |
| **Codename** | Validation Result Aggregator (CKVS Phase 3a) |
| **Estimated Hours** | 4 |
| **Status** | Draft |
| **Last Updated** | 2026-01-31 |

---

## 1. Overview

### 1.1 Purpose

The **Validation Result Aggregator** combines validation findings from all validators into a unified result. It handles deduplication, severity normalization, fix suggestion consolidation, and produces a coherent validation report that can be presented to users.

### 1.2 Key Responsibilities

- Aggregate findings from multiple validators
- Deduplicate similar findings
- Normalize severity levels
- Consolidate fix suggestions
- Compute overall validation status
- Provide summary statistics
- Support filtering and sorting

### 1.3 Module Location

```
src/
  Lexichord.KnowledgeGraph/
    Validation/
      Aggregation/
        IResultAggregator.cs
        ResultAggregator.cs
        FindingDeduplicator.cs
        FixConsolidator.cs
```

---

## 2. Interface Definitions

### 2.1 Result Aggregator Interface

```csharp
namespace Lexichord.KnowledgeGraph.Validation.Aggregation;

/// <summary>
/// Aggregates validation findings into a unified result.
/// </summary>
public interface IResultAggregator
{
    /// <summary>
    /// Aggregates findings into a validation result.
    /// </summary>
    /// <param name="findings">Findings from all validators.</param>
    /// <param name="duration">Total validation duration.</param>
    /// <param name="options">Validation options for filtering.</param>
    /// <param name="claimsValidated">Number of claims validated.</param>
    /// <param name="entitiesValidated">Number of entities validated.</param>
    /// <returns>Aggregated validation result.</returns>
    ValidationResult Aggregate(
        IEnumerable<ValidationFinding> findings,
        TimeSpan duration,
        ValidationOptions options,
        int claimsValidated = 0,
        int entitiesValidated = 0);

    /// <summary>
    /// Aggregates findings with validator failure information.
    /// </summary>
    ValidationResult Aggregate(
        IEnumerable<ValidationFinding> findings,
        IEnumerable<ValidatorFailure> failures,
        TimeSpan duration,
        ValidationOptions options,
        int claimsValidated = 0,
        int entitiesValidated = 0);

    /// <summary>
    /// Filters findings based on criteria.
    /// </summary>
    IReadOnlyList<ValidationFinding> FilterFindings(
        IReadOnlyList<ValidationFinding> findings,
        FindingFilter filter);

    /// <summary>
    /// Groups findings by various criteria.
    /// </summary>
    IReadOnlyDictionary<string, IReadOnlyList<ValidationFinding>> GroupFindings(
        IReadOnlyList<ValidationFinding> findings,
        FindingGroupBy groupBy);
}
```

### 2.2 Finding Deduplicator Interface

```csharp
/// <summary>
/// Deduplicates validation findings.
/// </summary>
public interface IFindingDeduplicator
{
    /// <summary>
    /// Removes duplicate findings from a collection.
    /// </summary>
    /// <param name="findings">Findings to deduplicate.</param>
    /// <returns>Deduplicated findings.</returns>
    IReadOnlyList<ValidationFinding> Deduplicate(
        IEnumerable<ValidationFinding> findings);

    /// <summary>
    /// Checks if two findings are duplicates.
    /// </summary>
    bool AreDuplicates(ValidationFinding a, ValidationFinding b);
}
```

### 2.3 Fix Consolidator Interface

```csharp
/// <summary>
/// Consolidates fix suggestions from multiple findings.
/// </summary>
public interface IFixConsolidator
{
    /// <summary>
    /// Consolidates overlapping fixes into a coherent set.
    /// </summary>
    /// <param name="findings">Findings with suggested fixes.</param>
    /// <returns>Consolidated fix set.</returns>
    IReadOnlyList<ConsolidatedFix> ConsolidateFixes(
        IReadOnlyList<ValidationFinding> findings);

    /// <summary>
    /// Creates a "Fix All" action for multiple fixes.
    /// </summary>
    FixAllAction CreateFixAllAction(
        IReadOnlyList<ConsolidatedFix> fixes);
}
```

---

## 3. Data Types

### 3.1 Finding Filter

```csharp
/// <summary>
/// Filter criteria for validation findings.
/// </summary>
public record FindingFilter
{
    /// <summary>
    /// Minimum severity to include.
    /// </summary>
    public ValidationSeverity? MinSeverity { get; init; }

    /// <summary>
    /// Validators to include.
    /// </summary>
    public IReadOnlySet<string>? ValidatorNames { get; init; }

    /// <summary>
    /// Finding codes to include.
    /// </summary>
    public IReadOnlySet<string>? Codes { get; init; }

    /// <summary>
    /// Entity types to include.
    /// </summary>
    public IReadOnlySet<string>? EntityTypes { get; init; }

    /// <summary>
    /// Whether to include only fixable findings.
    /// </summary>
    public bool? FixableOnly { get; init; }

    /// <summary>
    /// Text span range to include.
    /// </summary>
    public TextSpan? LocationRange { get; init; }
}
```

### 3.2 Finding Group By

```csharp
/// <summary>
/// Grouping criteria for findings.
/// </summary>
public enum FindingGroupBy
{
    /// <summary>Group by validator name.</summary>
    Validator,

    /// <summary>Group by severity.</summary>
    Severity,

    /// <summary>Group by finding code.</summary>
    Code,

    /// <summary>Group by related entity.</summary>
    Entity,

    /// <summary>Group by document location.</summary>
    Location
}
```

### 3.3 Consolidated Fix

```csharp
/// <summary>
/// A consolidated fix combining multiple related fixes.
/// </summary>
public record ConsolidatedFix
{
    /// <summary>
    /// Unique fix ID.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Fix description.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Findings this fix addresses.
    /// </summary>
    public required IReadOnlyList<ValidationFinding> AffectedFindings { get; init; }

    /// <summary>
    /// Text edits to apply.
    /// </summary>
    public IReadOnlyList<TextEdit> Edits { get; init; } = [];

    /// <summary>
    /// Overall confidence in this fix.
    /// </summary>
    public float Confidence { get; init; }

    /// <summary>
    /// Whether fix can be auto-applied.
    /// </summary>
    public bool CanAutoApply { get; init; }

    /// <summary>
    /// Number of findings this fix resolves.
    /// </summary>
    public int FindingsResolved => AffectedFindings.Count;
}

/// <summary>
/// A text edit operation.
/// </summary>
public record TextEdit
{
    /// <summary>
    /// Span to replace.
    /// </summary>
    public required TextSpan Span { get; init; }

    /// <summary>
    /// New text.
    /// </summary>
    public required string NewText { get; init; }
}
```

### 3.4 Fix All Action

```csharp
/// <summary>
/// Action to fix multiple issues at once.
/// </summary>
public record FixAllAction
{
    /// <summary>
    /// Description of what "Fix All" will do.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// All fixes to apply.
    /// </summary>
    public required IReadOnlyList<ConsolidatedFix> Fixes { get; init; }

    /// <summary>
    /// Total findings that will be resolved.
    /// </summary>
    public int TotalFindingsResolved { get; init; }

    /// <summary>
    /// Whether all fixes can be auto-applied.
    /// </summary>
    public bool AllAutoApplicable { get; init; }

    /// <summary>
    /// Warnings about the fix all action.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];
}
```

### 3.5 Validation Summary

```csharp
/// <summary>
/// Summary statistics for validation results.
/// </summary>
public record ValidationSummary
{
    /// <summary>
    /// Total findings count.
    /// </summary>
    public int TotalFindings { get; init; }

    /// <summary>
    /// Findings by severity.
    /// </summary>
    public required IReadOnlyDictionary<ValidationSeverity, int> BySeverity { get; init; }

    /// <summary>
    /// Findings by validator.
    /// </summary>
    public required IReadOnlyDictionary<string, int> ByValidator { get; init; }

    /// <summary>
    /// Findings by code.
    /// </summary>
    public required IReadOnlyDictionary<string, int> ByCode { get; init; }

    /// <summary>
    /// Number of fixable findings.
    /// </summary>
    public int FixableFindings { get; init; }

    /// <summary>
    /// Number of auto-fixable findings.
    /// </summary>
    public int AutoFixableFindings { get; init; }
}
```

---

## 4. Implementation

### 4.1 Result Aggregator Implementation

```csharp
/// <summary>
/// Aggregates validation findings into a unified result.
/// </summary>
public class ResultAggregator : IResultAggregator
{
    private readonly IFindingDeduplicator _deduplicator;
    private readonly IFixConsolidator _fixConsolidator;
    private readonly ILogger<ResultAggregator> _logger;

    public ResultAggregator(
        IFindingDeduplicator deduplicator,
        IFixConsolidator fixConsolidator,
        ILogger<ResultAggregator> logger)
    {
        _deduplicator = deduplicator;
        _fixConsolidator = fixConsolidator;
        _logger = logger;
    }

    public ValidationResult Aggregate(
        IEnumerable<ValidationFinding> findings,
        TimeSpan duration,
        ValidationOptions options,
        int claimsValidated = 0,
        int entitiesValidated = 0)
    {
        return Aggregate(findings, [], duration, options, claimsValidated, entitiesValidated);
    }

    public ValidationResult Aggregate(
        IEnumerable<ValidationFinding> findings,
        IEnumerable<ValidatorFailure> failures,
        TimeSpan duration,
        ValidationOptions options,
        int claimsValidated = 0,
        int entitiesValidated = 0)
    {
        var findingsList = findings.ToList();

        // Apply deduplication
        var deduplicated = _deduplicator.Deduplicate(findingsList);

        // Apply severity filter
        var filtered = deduplicated
            .Where(f => f.Severity <= options.MinSeverity)
            .ToList();

        // Sort by severity, then location
        var sorted = filtered
            .OrderBy(f => f.Severity)
            .ThenBy(f => f.Location?.Start ?? int.MaxValue)
            .ToList();

        // Limit findings if needed
        var limited = options.MaxFindings > 0
            ? sorted.Take(options.MaxFindings).ToList()
            : sorted;

        // Compute status
        var status = ComputeStatus(limited, failures.ToList());

        // Get validators that ran
        var validatorsRun = limited
            .Select(f => f.ValidatorName)
            .Distinct()
            .ToList();

        _logger.LogDebug(
            "Aggregated {Original} findings into {Final} (deduplicated: {Dedup}, filtered: {Filtered})",
            findingsList.Count,
            limited.Count,
            deduplicated.Count,
            filtered.Count);

        return new ValidationResult
        {
            Status = status,
            Findings = limited,
            Duration = duration,
            ClaimsValidated = claimsValidated,
            EntitiesValidated = entitiesValidated,
            ValidatorsRun = validatorsRun,
            ValidatorFailures = failures.ToList()
        };
    }

    public IReadOnlyList<ValidationFinding> FilterFindings(
        IReadOnlyList<ValidationFinding> findings,
        FindingFilter filter)
    {
        var result = findings.AsEnumerable();

        if (filter.MinSeverity.HasValue)
        {
            result = result.Where(f => f.Severity <= filter.MinSeverity.Value);
        }

        if (filter.ValidatorNames != null && filter.ValidatorNames.Count > 0)
        {
            result = result.Where(f => filter.ValidatorNames.Contains(f.ValidatorName));
        }

        if (filter.Codes != null && filter.Codes.Count > 0)
        {
            result = result.Where(f => filter.Codes.Contains(f.Code));
        }

        if (filter.EntityTypes != null && filter.EntityTypes.Count > 0)
        {
            result = result.Where(f =>
                f.RelatedEntity != null &&
                filter.EntityTypes.Contains(f.RelatedEntity.Type));
        }

        if (filter.FixableOnly == true)
        {
            result = result.Where(f => f.SuggestedFix != null);
        }

        if (filter.LocationRange != null)
        {
            result = result.Where(f =>
                f.Location != null &&
                f.Location.Start >= filter.LocationRange.Start &&
                f.Location.End <= filter.LocationRange.End);
        }

        return result.ToList();
    }

    public IReadOnlyDictionary<string, IReadOnlyList<ValidationFinding>> GroupFindings(
        IReadOnlyList<ValidationFinding> findings,
        FindingGroupBy groupBy)
    {
        return groupBy switch
        {
            FindingGroupBy.Validator => findings
                .GroupBy(f => f.ValidatorName)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<ValidationFinding>)g.ToList()),

            FindingGroupBy.Severity => findings
                .GroupBy(f => f.Severity.ToString())
                .ToDictionary(g => g.Key, g => (IReadOnlyList<ValidationFinding>)g.ToList()),

            FindingGroupBy.Code => findings
                .GroupBy(f => f.Code)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<ValidationFinding>)g.ToList()),

            FindingGroupBy.Entity => findings
                .Where(f => f.RelatedEntity != null)
                .GroupBy(f => f.RelatedEntity!.Name)
                .ToDictionary(g => g.Key, g => (IReadOnlyList<ValidationFinding>)g.ToList()),

            FindingGroupBy.Location => GroupByLocation(findings),

            _ => throw new ArgumentException($"Unknown groupBy: {groupBy}")
        };
    }

    private ValidationStatus ComputeStatus(
        IReadOnlyList<ValidationFinding> findings,
        IReadOnlyList<ValidatorFailure> failures)
    {
        if (failures.Any())
        {
            return ValidationStatus.Incomplete;
        }

        var hasErrors = findings.Any(f => f.Severity == ValidationSeverity.Error);
        var hasWarnings = findings.Any(f => f.Severity == ValidationSeverity.Warning);

        if (hasErrors)
        {
            return ValidationStatus.Invalid;
        }

        if (hasWarnings)
        {
            return ValidationStatus.ValidWithWarnings;
        }

        return ValidationStatus.Valid;
    }

    private IReadOnlyDictionary<string, IReadOnlyList<ValidationFinding>> GroupByLocation(
        IReadOnlyList<ValidationFinding> findings)
    {
        // Group by "section" based on location ranges
        var withLocation = findings.Where(f => f.Location != null).ToList();

        // Simple grouping by line ranges
        return withLocation
            .GroupBy(f => GetLocationGroup(f.Location!))
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ValidationFinding>)g.ToList());
    }

    private string GetLocationGroup(TextSpan span)
    {
        // Group into 50-line sections
        var startSection = span.Start / 50;
        return $"Lines {startSection * 50}-{(startSection + 1) * 50}";
    }
}
```

### 4.2 Finding Deduplicator Implementation

```csharp
/// <summary>
/// Deduplicates validation findings.
/// </summary>
public class FindingDeduplicator : IFindingDeduplicator
{
    public IReadOnlyList<ValidationFinding> Deduplicate(
        IEnumerable<ValidationFinding> findings)
    {
        var result = new List<ValidationFinding>();
        var seen = new List<ValidationFinding>();

        foreach (var finding in findings)
        {
            var isDuplicate = false;

            foreach (var existing in seen)
            {
                if (AreDuplicates(finding, existing))
                {
                    isDuplicate = true;
                    break;
                }
            }

            if (!isDuplicate)
            {
                result.Add(finding);
                seen.Add(finding);
            }
        }

        return result;
    }

    public bool AreDuplicates(ValidationFinding a, ValidationFinding b)
    {
        // Same code and validator
        if (a.Code != b.Code || a.ValidatorName != b.ValidatorName)
        {
            return false;
        }

        // Same or overlapping location
        if (a.Location != null && b.Location != null)
        {
            if (LocationsOverlap(a.Location, b.Location))
            {
                return true;
            }
        }

        // Same entity and claim
        if (a.RelatedEntity?.Id == b.RelatedEntity?.Id &&
            a.RelatedClaim?.Id == b.RelatedClaim?.Id)
        {
            // Check message similarity
            return MessagesSimilar(a.Message, b.Message);
        }

        return false;
    }

    private bool LocationsOverlap(TextSpan a, TextSpan b)
    {
        return a.Start <= b.End && b.Start <= a.End;
    }

    private bool MessagesSimilar(string a, string b)
    {
        // Simple similarity check
        if (string.Equals(a, b, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check if one contains the other
        if (a.Contains(b, StringComparison.OrdinalIgnoreCase) ||
            b.Contains(a, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
```

### 4.3 Fix Consolidator Implementation

```csharp
/// <summary>
/// Consolidates fix suggestions from multiple findings.
/// </summary>
public class FixConsolidator : IFixConsolidator
{
    public IReadOnlyList<ConsolidatedFix> ConsolidateFixes(
        IReadOnlyList<ValidationFinding> findings)
    {
        var withFixes = findings.Where(f => f.SuggestedFix != null).ToList();
        var consolidated = new List<ConsolidatedFix>();
        var processed = new HashSet<Guid>();

        foreach (var finding in withFixes)
        {
            if (processed.Contains(finding.Id))
                continue;

            // Find related findings with similar fixes
            var related = FindRelatedFixFindings(finding, withFixes, processed);
            related.Add(finding);

            foreach (var r in related)
            {
                processed.Add(r.Id);
            }

            var fix = CreateConsolidatedFix(related);
            consolidated.Add(fix);
        }

        return consolidated
            .OrderByDescending(f => f.Confidence)
            .ThenByDescending(f => f.FindingsResolved)
            .ToList();
    }

    public FixAllAction CreateFixAllAction(IReadOnlyList<ConsolidatedFix> fixes)
    {
        var autoApplicable = fixes.Where(f => f.CanAutoApply).ToList();
        var warnings = new List<string>();

        if (autoApplicable.Count < fixes.Count)
        {
            var manualCount = fixes.Count - autoApplicable.Count;
            warnings.Add($"{manualCount} fix(es) require manual review and will be skipped");
        }

        // Check for overlapping edits
        var allEdits = fixes.SelectMany(f => f.Edits).ToList();
        var hasOverlaps = CheckForOverlappingEdits(allEdits);
        if (hasOverlaps)
        {
            warnings.Add("Some fixes have overlapping text ranges and may conflict");
        }

        return new FixAllAction
        {
            Description = $"Apply {autoApplicable.Count} fixes to resolve " +
                          $"{autoApplicable.Sum(f => f.FindingsResolved)} findings",
            Fixes = autoApplicable,
            TotalFindingsResolved = autoApplicable.Sum(f => f.FindingsResolved),
            AllAutoApplicable = autoApplicable.Count == fixes.Count,
            Warnings = warnings
        };
    }

    private List<ValidationFinding> FindRelatedFixFindings(
        ValidationFinding finding,
        IReadOnlyList<ValidationFinding> allFindings,
        HashSet<Guid> processed)
    {
        var related = new List<ValidationFinding>();

        foreach (var other in allFindings)
        {
            if (other.Id == finding.Id || processed.Contains(other.Id))
                continue;

            if (AreFixesRelated(finding.SuggestedFix!, other.SuggestedFix!))
            {
                related.Add(other);
            }
        }

        return related;
    }

    private bool AreFixesRelated(ValidationFix a, ValidationFix b)
    {
        // Same replacement text
        if (a.ReplacementText != null && a.ReplacementText == b.ReplacementText)
        {
            return true;
        }

        // Overlapping spans
        if (a.ReplaceSpan != null && b.ReplaceSpan != null)
        {
            return SpansOverlap(a.ReplaceSpan, b.ReplaceSpan);
        }

        // Similar descriptions
        if (string.Equals(a.Description, b.Description, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private ConsolidatedFix CreateConsolidatedFix(List<ValidationFinding> findings)
    {
        var fixes = findings.Select(f => f.SuggestedFix!).ToList();
        var edits = fixes
            .Where(f => f.ReplaceSpan != null && f.ReplacementText != null)
            .Select(f => new TextEdit
            {
                Span = f.ReplaceSpan!,
                NewText = f.ReplacementText!
            })
            .ToList();

        return new ConsolidatedFix
        {
            Description = fixes.First().Description,
            AffectedFindings = findings,
            Edits = edits,
            Confidence = fixes.Average(f => f.Confidence),
            CanAutoApply = fixes.All(f => f.CanAutoApply)
        };
    }

    private bool SpansOverlap(TextSpan a, TextSpan b)
    {
        return a.Start <= b.End && b.Start <= a.End;
    }

    private bool CheckForOverlappingEdits(IReadOnlyList<TextEdit> edits)
    {
        for (int i = 0; i < edits.Count; i++)
        {
            for (int j = i + 1; j < edits.Count; j++)
            {
                if (SpansOverlap(edits[i].Span, edits[j].Span))
                {
                    return true;
                }
            }
        }
        return false;
    }
}
```

---

## 5. Validation Summary Generator

```csharp
/// <summary>
/// Generates summary statistics from validation results.
/// </summary>
public static class ValidationSummaryGenerator
{
    public static ValidationSummary Generate(ValidationResult result)
    {
        var findings = result.Findings;

        return new ValidationSummary
        {
            TotalFindings = findings.Count,
            BySeverity = findings
                .GroupBy(f => f.Severity)
                .ToDictionary(g => g.Key, g => g.Count()),
            ByValidator = findings
                .GroupBy(f => f.ValidatorName)
                .ToDictionary(g => g.Key, g => g.Count()),
            ByCode = findings
                .GroupBy(f => f.Code)
                .ToDictionary(g => g.Key, g => g.Count()),
            FixableFindings = findings.Count(f => f.SuggestedFix != null),
            AutoFixableFindings = findings.Count(f => f.SuggestedFix?.CanAutoApply == true)
        };
    }
}
```

---

## 6. Error Handling

| Error | Handling Strategy |
| :---- | :---------------- |
| Empty findings list | Return valid status with empty results |
| Null findings | Filter out null entries |
| Invalid filter criteria | Log warning, ignore invalid criteria |
| Fix consolidation conflict | Keep separate, add warning |

---

## 7. Testing Requirements

### 7.1 Unit Tests

| Test Case | Description |
| :-------- | :---------- |
| `Aggregate_ComputesCorrectStatus` | Status based on findings |
| `Aggregate_DeduplicatesFindings` | Removes duplicates |
| `Aggregate_FiltersBySeverity` | Respects min severity |
| `Aggregate_LimitsFindings` | Respects max findings |
| `FilterFindings_AppliesAllCriteria` | Filter combinations |
| `GroupFindings_ByValidator` | Groups correctly |
| `ConsolidateFixes_MergesRelated` | Fix consolidation |

### 7.2 Integration Tests

| Test Case | Description |
| :-------- | :---------- |
| `ResultAggregator_EndToEnd` | Full aggregation flow |
| `FixConsolidator_WithRealFindings` | Real fix merging |

---

## 8. Performance Considerations

- **Streaming Aggregation:** Process findings as they arrive
- **Lazy Grouping:** Compute groups on demand
- **Efficient Deduplication:** Use hash-based lookup
- **Parallel Processing:** Consolidate fixes in parallel

---

## 9. License Gating

| Tier | Access |
| :--- | :----- |
| Core | Not available |
| WriterPro | Basic aggregation |
| Teams | Full aggregation + fix consolidation |
| Enterprise | Full + custom grouping |

---

## 10. Changelog

| Version | Date | Author | Changes |
| :------ | :--- | :----- | :------ |
| 1.0 | 2026-01-31 | Lead Architect | Initial creation |

---
