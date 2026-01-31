# LCS-DES-065-KG-j: Linter Integration

## Document Control

| Field | Value |
| :--- | :--- |
| **Spec ID** | LCS-DES-065-KG-j |
| **System Breakdown** | LCS-SBD-065-KG |
| **Version** | v0.6.5 |
| **Codename** | Linter Integration (CKVS Phase 3a) |
| **Estimated Hours** | 4 |
| **Status** | Draft |
| **Last Updated** | 2026-01-31 |

---

## 1. Overview

### 1.1 Purpose

The **Linter Integration** module bridges the CKVS Validation Engine with Lexichord's existing style and grammar linter (v0.3.x). It provides a unified view of all findings, consistent severity levels, and a combined fix workflow for users.

### 1.2 Key Responsibilities

- Integrate validation findings with linter findings
- Normalize severity levels across both systems
- Provide unified "Fix All" workflow
- Support combined filtering and sorting
- Enable single panel display for all issues

### 1.3 Module Location

```
src/
  Lexichord.KnowledgeGraph/
    Validation/
      Integration/
        ILinterIntegration.cs
        LinterIntegration.cs
        UnifiedFindingAdapter.cs
        CombinedFixWorkflow.cs
```

---

## 2. Interface Definitions

### 2.1 Linter Integration Interface

```csharp
namespace Lexichord.KnowledgeGraph.Validation.Integration;

/// <summary>
/// Integrates CKVS validation with Lexichord's style linter.
/// </summary>
public interface ILinterIntegration
{
    /// <summary>
    /// Gets combined findings from validation and linter.
    /// </summary>
    Task<UnifiedFindingResult> GetUnifiedFindingsAsync(
        Document document,
        UnifiedFindingOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Applies a fix from either validation or linter.
    /// </summary>
    Task<FixResult> ApplyUnifiedFixAsync(
        UnifiedFix fix,
        Document document,
        CancellationToken ct = default);

    /// <summary>
    /// Applies all auto-fixable issues.
    /// </summary>
    Task<FixAllResult> ApplyAllFixesAsync(
        Document document,
        FixAllOptions options,
        CancellationToken ct = default);
}
```

### 2.2 Unified Finding Adapter Interface

```csharp
/// <summary>
/// Adapts findings from different sources to unified format.
/// </summary>
public interface IUnifiedFindingAdapter
{
    /// <summary>
    /// Converts a validation finding to unified format.
    /// </summary>
    UnifiedFinding FromValidationFinding(ValidationFinding finding);

    /// <summary>
    /// Converts a linter finding to unified format.
    /// </summary>
    UnifiedFinding FromLinterFinding(LinterFinding finding);

    /// <summary>
    /// Normalizes severity across sources.
    /// </summary>
    UnifiedSeverity NormalizeSeverity(object sourceSeverity, FindingSource source);
}
```

### 2.3 Combined Fix Workflow Interface

```csharp
/// <summary>
/// Manages combined fix workflow for validation and linter issues.
/// </summary>
public interface ICombinedFixWorkflow
{
    /// <summary>
    /// Validates that fixes don't conflict.
    /// </summary>
    FixConflictResult CheckForConflicts(
        IReadOnlyList<UnifiedFix> fixes);

    /// <summary>
    /// Orders fixes for safe application.
    /// </summary>
    IReadOnlyList<UnifiedFix> OrderFixesForApplication(
        IReadOnlyList<UnifiedFix> fixes);

    /// <summary>
    /// Applies fixes in correct order.
    /// </summary>
    Task<FixApplicationResult> ApplyOrderedFixesAsync(
        IReadOnlyList<UnifiedFix> fixes,
        Document document,
        CancellationToken ct = default);
}
```

---

## 3. Data Types

### 3.1 Unified Finding

```csharp
/// <summary>
/// A finding that can come from validation or linter.
/// </summary>
public record UnifiedFinding
{
    /// <summary>Unique finding ID.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Finding source.</summary>
    public FindingSource Source { get; init; }

    /// <summary>Unified severity.</summary>
    public UnifiedSeverity Severity { get; init; }

    /// <summary>Finding code.</summary>
    public required string Code { get; init; }

    /// <summary>Human-readable message.</summary>
    public required string Message { get; init; }

    /// <summary>Location in document.</summary>
    public TextSpan? Location { get; init; }

    /// <summary>Category for grouping.</summary>
    public FindingCategory Category { get; init; }

    /// <summary>Suggested fix if available.</summary>
    public UnifiedFix? SuggestedFix { get; init; }

    /// <summary>Original validation finding (if from validation).</summary>
    public ValidationFinding? OriginalValidationFinding { get; init; }

    /// <summary>Original linter finding (if from linter).</summary>
    public LinterFinding? OriginalLinterFinding { get; init; }
}

/// <summary>
/// Source of a finding.
/// </summary>
public enum FindingSource
{
    /// <summary>From CKVS validation engine.</summary>
    Validation,

    /// <summary>From style linter.</summary>
    StyleLinter,

    /// <summary>From grammar linter.</summary>
    GrammarLinter
}

/// <summary>
/// Unified severity levels.
/// </summary>
public enum UnifiedSeverity
{
    /// <summary>Critical error blocking publication.</summary>
    Error = 0,

    /// <summary>Warning that should be addressed.</summary>
    Warning = 1,

    /// <summary>Informational suggestion.</summary>
    Info = 2,

    /// <summary>Hint or style suggestion.</summary>
    Hint = 3
}

/// <summary>
/// Category for grouping findings.
/// </summary>
public enum FindingCategory
{
    /// <summary>Schema validation issues.</summary>
    Schema,

    /// <summary>Axiom/rule violations.</summary>
    Axiom,

    /// <summary>Consistency/contradiction issues.</summary>
    Consistency,

    /// <summary>Style guide violations.</summary>
    Style,

    /// <summary>Grammar issues.</summary>
    Grammar,

    /// <summary>Spelling issues.</summary>
    Spelling
}
```

### 3.2 Unified Fix

```csharp
/// <summary>
/// A fix that can come from validation or linter.
/// </summary>
public record UnifiedFix
{
    /// <summary>Unique fix ID.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Fix source.</summary>
    public FindingSource Source { get; init; }

    /// <summary>Fix description.</summary>
    public required string Description { get; init; }

    /// <summary>Text edits to apply.</summary>
    public IReadOnlyList<TextEdit> Edits { get; init; } = [];

    /// <summary>Fix confidence (0-1).</summary>
    public float Confidence { get; init; }

    /// <summary>Whether fix can be auto-applied.</summary>
    public bool CanAutoApply { get; init; }

    /// <summary>Finding this fix addresses.</summary>
    public Guid FindingId { get; init; }
}
```

### 3.3 Unified Finding Result

```csharp
/// <summary>
/// Combined results from validation and linter.
/// </summary>
public record UnifiedFindingResult
{
    /// <summary>All unified findings.</summary>
    public required IReadOnlyList<UnifiedFinding> Findings { get; init; }

    /// <summary>Validation-specific findings count.</summary>
    public int ValidationCount { get; init; }

    /// <summary>Linter-specific findings count.</summary>
    public int LinterCount { get; init; }

    /// <summary>Overall status.</summary>
    public UnifiedStatus Status { get; init; }

    /// <summary>Summary by category.</summary>
    public IReadOnlyDictionary<FindingCategory, int> ByCategory { get; init; } =
        new Dictionary<FindingCategory, int>();

    /// <summary>Summary by severity.</summary>
    public IReadOnlyDictionary<UnifiedSeverity, int> BySeverity { get; init; } =
        new Dictionary<UnifiedSeverity, int>();
}

public enum UnifiedStatus
{
    /// <summary>All checks passed.</summary>
    Pass,

    /// <summary>Passed with warnings.</summary>
    PassWithWarnings,

    /// <summary>Failed with errors.</summary>
    Fail
}
```

### 3.4 Unified Finding Options

```csharp
/// <summary>
/// Options for unified finding retrieval.
/// </summary>
public record UnifiedFindingOptions
{
    /// <summary>Whether to include validation findings.</summary>
    public bool IncludeValidation { get; init; } = true;

    /// <summary>Whether to include linter findings.</summary>
    public bool IncludeLinter { get; init; } = true;

    /// <summary>Minimum severity to include.</summary>
    public UnifiedSeverity MinSeverity { get; init; } = UnifiedSeverity.Hint;

    /// <summary>Categories to include (null = all).</summary>
    public IReadOnlySet<FindingCategory>? Categories { get; init; }

    /// <summary>Maximum findings to return.</summary>
    public int MaxFindings { get; init; } = 200;

    /// <summary>Validation options.</summary>
    public ValidationOptions? ValidationOptions { get; init; }

    /// <summary>Linter options.</summary>
    public LinterOptions? LinterOptions { get; init; }
}
```

---

## 4. Implementation

### 4.1 Linter Integration Implementation

```csharp
/// <summary>
/// Integrates CKVS validation with Lexichord's style linter.
/// </summary>
public class LinterIntegration : ILinterIntegration
{
    private readonly IValidationEngine _validationEngine;
    private readonly ILinterService _linterService;
    private readonly IUnifiedFindingAdapter _adapter;
    private readonly ICombinedFixWorkflow _fixWorkflow;
    private readonly ILogger<LinterIntegration> _logger;

    public LinterIntegration(
        IValidationEngine validationEngine,
        ILinterService linterService,
        IUnifiedFindingAdapter adapter,
        ICombinedFixWorkflow fixWorkflow,
        ILogger<LinterIntegration> logger)
    {
        _validationEngine = validationEngine;
        _linterService = linterService;
        _adapter = adapter;
        _fixWorkflow = fixWorkflow;
        _logger = logger;
    }

    public async Task<UnifiedFindingResult> GetUnifiedFindingsAsync(
        Document document,
        UnifiedFindingOptions options,
        CancellationToken ct = default)
    {
        var findings = new List<UnifiedFinding>();

        // Run validation and linter in parallel
        var validationTask = options.IncludeValidation
            ? _validationEngine.ValidateDocumentAsync(
                document,
                options.ValidationOptions ?? new ValidationOptions(),
                ct)
            : Task.FromResult<ValidationResult?>(null);

        var linterTask = options.IncludeLinter
            ? _linterService.LintDocumentAsync(
                document,
                options.LinterOptions ?? new LinterOptions(),
                ct)
            : Task.FromResult<LinterResult?>(null);

        await Task.WhenAll(validationTask, linterTask);

        var validationResult = await validationTask;
        var linterResult = await linterTask;

        // Convert validation findings
        if (validationResult != null)
        {
            foreach (var finding in validationResult.Findings)
            {
                var unified = _adapter.FromValidationFinding(finding);
                findings.Add(unified);
            }
        }

        // Convert linter findings
        if (linterResult != null)
        {
            foreach (var finding in linterResult.Findings)
            {
                var unified = _adapter.FromLinterFinding(finding);
                findings.Add(unified);
            }
        }

        // Apply filters
        var filtered = ApplyFilters(findings, options);

        // Sort by severity, then location
        var sorted = filtered
            .OrderBy(f => f.Severity)
            .ThenBy(f => f.Location?.Start ?? int.MaxValue)
            .Take(options.MaxFindings)
            .ToList();

        _logger.LogDebug(
            "Unified findings: {Validation} validation + {Linter} linter = {Total} total",
            validationResult?.Findings.Count ?? 0,
            linterResult?.Findings.Count ?? 0,
            sorted.Count);

        return new UnifiedFindingResult
        {
            Findings = sorted,
            ValidationCount = validationResult?.Findings.Count ?? 0,
            LinterCount = linterResult?.Findings.Count ?? 0,
            Status = ComputeStatus(sorted),
            ByCategory = sorted.GroupBy(f => f.Category)
                .ToDictionary(g => g.Key, g => g.Count()),
            BySeverity = sorted.GroupBy(f => f.Severity)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    public async Task<FixResult> ApplyUnifiedFixAsync(
        UnifiedFix fix,
        Document document,
        CancellationToken ct = default)
    {
        try
        {
            // Apply text edits
            var content = document.Content;

            // Sort edits in reverse order to preserve positions
            var sortedEdits = fix.Edits
                .OrderByDescending(e => e.Span.Start)
                .ToList();

            foreach (var edit in sortedEdits)
            {
                content = content.Substring(0, edit.Span.Start) +
                          edit.NewText +
                          content.Substring(edit.Span.End);
            }

            return new FixResult
            {
                Success = true,
                NewContent = content,
                EditsApplied = fix.Edits.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply fix {FixId}", fix.Id);
            return new FixResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<FixAllResult> ApplyAllFixesAsync(
        Document document,
        FixAllOptions options,
        CancellationToken ct = default)
    {
        // Get all findings
        var result = await GetUnifiedFindingsAsync(document, new UnifiedFindingOptions
        {
            IncludeValidation = options.IncludeValidation,
            IncludeLinter = options.IncludeLinter
        }, ct);

        // Collect all auto-applicable fixes
        var fixes = result.Findings
            .Where(f => f.SuggestedFix != null && f.SuggestedFix.CanAutoApply)
            .Select(f => f.SuggestedFix!)
            .ToList();

        // Check for conflicts
        var conflictResult = _fixWorkflow.CheckForConflicts(fixes);
        if (conflictResult.HasConflicts && !options.ForceApply)
        {
            return new FixAllResult
            {
                Success = false,
                FixesApplied = 0,
                Conflicts = conflictResult.Conflicts
            };
        }

        // Order and apply fixes
        var orderedFixes = _fixWorkflow.OrderFixesForApplication(fixes);
        var applicationResult = await _fixWorkflow.ApplyOrderedFixesAsync(
            orderedFixes, document, ct);

        return new FixAllResult
        {
            Success = applicationResult.Success,
            FixesApplied = applicationResult.FixesApplied,
            NewContent = applicationResult.NewContent,
            Warnings = applicationResult.Warnings
        };
    }

    private List<UnifiedFinding> ApplyFilters(
        List<UnifiedFinding> findings,
        UnifiedFindingOptions options)
    {
        var result = findings.AsEnumerable();

        // Severity filter
        result = result.Where(f => f.Severity >= options.MinSeverity);

        // Category filter
        if (options.Categories != null && options.Categories.Count > 0)
        {
            result = result.Where(f => options.Categories.Contains(f.Category));
        }

        return result.ToList();
    }

    private UnifiedStatus ComputeStatus(IReadOnlyList<UnifiedFinding> findings)
    {
        if (findings.Any(f => f.Severity == UnifiedSeverity.Error))
        {
            return UnifiedStatus.Fail;
        }

        if (findings.Any(f => f.Severity == UnifiedSeverity.Warning))
        {
            return UnifiedStatus.PassWithWarnings;
        }

        return UnifiedStatus.Pass;
    }
}
```

### 4.2 Unified Finding Adapter Implementation

```csharp
/// <summary>
/// Adapts findings from different sources to unified format.
/// </summary>
public class UnifiedFindingAdapter : IUnifiedFindingAdapter
{
    public UnifiedFinding FromValidationFinding(ValidationFinding finding)
    {
        return new UnifiedFinding
        {
            Id = finding.Id,
            Source = FindingSource.Validation,
            Severity = NormalizeSeverity(finding.Severity, FindingSource.Validation),
            Code = finding.Code,
            Message = finding.Message,
            Location = finding.Location,
            Category = MapValidationCategory(finding),
            SuggestedFix = finding.SuggestedFix != null
                ? new UnifiedFix
                {
                    Source = FindingSource.Validation,
                    Description = finding.SuggestedFix.Description,
                    Edits = finding.SuggestedFix.ReplaceSpan != null
                        ? [new TextEdit
                        {
                            Span = finding.SuggestedFix.ReplaceSpan,
                            NewText = finding.SuggestedFix.ReplacementText ?? ""
                        }]
                        : [],
                    Confidence = finding.SuggestedFix.Confidence,
                    CanAutoApply = finding.SuggestedFix.CanAutoApply,
                    FindingId = finding.Id
                }
                : null,
            OriginalValidationFinding = finding
        };
    }

    public UnifiedFinding FromLinterFinding(LinterFinding finding)
    {
        return new UnifiedFinding
        {
            Id = Guid.NewGuid(),
            Source = finding.LinterType == LinterType.Style
                ? FindingSource.StyleLinter
                : FindingSource.GrammarLinter,
            Severity = NormalizeSeverity(finding.Severity, FindingSource.StyleLinter),
            Code = finding.RuleId,
            Message = finding.Message,
            Location = finding.Location,
            Category = MapLinterCategory(finding),
            SuggestedFix = finding.Fix != null
                ? new UnifiedFix
                {
                    Source = FindingSource.StyleLinter,
                    Description = finding.Fix.Description,
                    Edits = finding.Fix.Edits.Select(e => new TextEdit
                    {
                        Span = e.Span,
                        NewText = e.NewText
                    }).ToList(),
                    Confidence = finding.Fix.Confidence,
                    CanAutoApply = finding.Fix.CanAutoApply,
                    FindingId = Guid.NewGuid()
                }
                : null,
            OriginalLinterFinding = finding
        };
    }

    public UnifiedSeverity NormalizeSeverity(object sourceSeverity, FindingSource source)
    {
        if (source == FindingSource.Validation && sourceSeverity is ValidationSeverity vs)
        {
            return vs switch
            {
                ValidationSeverity.Error => UnifiedSeverity.Error,
                ValidationSeverity.Warning => UnifiedSeverity.Warning,
                ValidationSeverity.Info => UnifiedSeverity.Info,
                ValidationSeverity.Hint => UnifiedSeverity.Hint,
                _ => UnifiedSeverity.Info
            };
        }

        if (sourceSeverity is LinterSeverity ls)
        {
            return ls switch
            {
                LinterSeverity.Error => UnifiedSeverity.Error,
                LinterSeverity.Warning => UnifiedSeverity.Warning,
                LinterSeverity.Suggestion => UnifiedSeverity.Info,
                _ => UnifiedSeverity.Hint
            };
        }

        return UnifiedSeverity.Info;
    }

    private FindingCategory MapValidationCategory(ValidationFinding finding)
    {
        if (finding.ValidatorName == "SchemaValidator")
            return FindingCategory.Schema;
        if (finding.ValidatorName == "AxiomValidator")
            return FindingCategory.Axiom;
        if (finding.ValidatorName == "ConsistencyChecker")
            return FindingCategory.Consistency;

        return FindingCategory.Schema;
    }

    private FindingCategory MapLinterCategory(LinterFinding finding)
    {
        return finding.LinterType switch
        {
            LinterType.Style => FindingCategory.Style,
            LinterType.Grammar => FindingCategory.Grammar,
            LinterType.Spelling => FindingCategory.Spelling,
            _ => FindingCategory.Style
        };
    }
}
```

### 4.3 Combined Fix Workflow Implementation

```csharp
/// <summary>
/// Manages combined fix workflow for validation and linter issues.
/// </summary>
public class CombinedFixWorkflow : ICombinedFixWorkflow
{
    public FixConflictResult CheckForConflicts(IReadOnlyList<UnifiedFix> fixes)
    {
        var conflicts = new List<FixConflict>();

        for (int i = 0; i < fixes.Count; i++)
        {
            for (int j = i + 1; j < fixes.Count; j++)
            {
                if (FixesConflict(fixes[i], fixes[j]))
                {
                    conflicts.Add(new FixConflict
                    {
                        Fix1 = fixes[i],
                        Fix2 = fixes[j],
                        ConflictType = "Overlapping text ranges"
                    });
                }
            }
        }

        return new FixConflictResult
        {
            HasConflicts = conflicts.Count > 0,
            Conflicts = conflicts
        };
    }

    public IReadOnlyList<UnifiedFix> OrderFixesForApplication(
        IReadOnlyList<UnifiedFix> fixes)
    {
        // Sort by position in reverse order to preserve positions
        return fixes
            .OrderByDescending(f => f.Edits.FirstOrDefault()?.Span.Start ?? 0)
            .ToList();
    }

    public async Task<FixApplicationResult> ApplyOrderedFixesAsync(
        IReadOnlyList<UnifiedFix> fixes,
        Document document,
        CancellationToken ct = default)
    {
        var content = document.Content;
        var applied = 0;
        var warnings = new List<string>();

        foreach (var fix in fixes)
        {
            try
            {
                foreach (var edit in fix.Edits.OrderByDescending(e => e.Span.Start))
                {
                    content = content.Substring(0, edit.Span.Start) +
                              edit.NewText +
                              content.Substring(edit.Span.End);
                }
                applied++;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to apply fix: {ex.Message}");
            }
        }

        return new FixApplicationResult
        {
            Success = applied > 0,
            FixesApplied = applied,
            NewContent = content,
            Warnings = warnings
        };
    }

    private bool FixesConflict(UnifiedFix a, UnifiedFix b)
    {
        foreach (var editA in a.Edits)
        {
            foreach (var editB in b.Edits)
            {
                if (SpansOverlap(editA.Span, editB.Span))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool SpansOverlap(TextSpan a, TextSpan b)
    {
        return a.Start < b.End && b.Start < a.End;
    }
}
```

---

## 5. Error Handling

| Error | Handling Strategy |
| :---- | :---------------- |
| Validation service unavailable | Return linter-only results |
| Linter service unavailable | Return validation-only results |
| Fix application fails | Log warning, continue with next fix |
| Conflicting fixes | Report conflicts, skip conflicting fixes |

---

## 6. Testing Requirements

### 6.1 Unit Tests

| Test Case | Description |
| :-------- | :---------- |
| `GetUnifiedFindings_CombinesBothSources` | Merges validation + linter |
| `GetUnifiedFindings_FiltersBySeverity` | Severity filter works |
| `ApplyUnifiedFix_AppliesEdits` | Text edits applied correctly |
| `CheckForConflicts_DetectsOverlaps` | Conflicting fixes detected |
| `OrderFixesForApplication_ReverseOrder` | Fixes ordered correctly |

### 7.2 Integration Tests

| Test Case | Description |
| :-------- | :---------- |
| `LinterIntegration_EndToEnd` | Full integration flow |
| `ApplyAllFixes_WithRealDocument` | Real document fix application |

---

## 8. License Gating

| Tier | Access |
| :--- | :----- |
| Core | Linter only |
| WriterPro | Linter + schema validation |
| Teams | Full unified integration |
| Enterprise | Full + custom rules |

---

## 9. Changelog

| Version | Date | Author | Changes |
| :------ | :--- | :----- | :------ |
| 1.0 | 2026-01-31 | Lead Architect | Initial creation |

---
