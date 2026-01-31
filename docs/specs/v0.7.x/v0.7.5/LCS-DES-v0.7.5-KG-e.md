# LCS-DES-075-KG-e: Design Specification — Unified Issue Model

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `VAL-075-KG-e` | Unified validation sub-part e |
| **Feature Name** | `Unified Issue Model` | Common issue record for all validators |
| **Target Version** | `v0.7.5e` | Fifth sub-part of v0.7.5-KG |
| **Module Scope** | `Lexichord.Modules.Validation` | Validation module |
| **Swimlane** | `Validation` | Core validation vertical |
| **License Tier** | `Core` | Available across all tiers |
| **Feature Gate Key** | `FeatureFlags.Validation.UnifiedIssues` | Unified validation feature |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-31` | |
| **Parent Document** | [LCS-SBD-075-KG](./LCS-SBD-075-KG.md) | Unified Validation scope |
| **Scope Breakdown** | [LCS-SBD-075-KG S3.1](./LCS-SBD-075-KG.md#31-sub-parts) | e = Issue Model |

---

## 2. Executive Summary

### 2.1 The Requirement

CKVS v0.7.5-KG requires a unified issue representation to normalize findings from three distinct validators:

- **Style Linter** (v0.3.x): Style violations with `LintSeverity` enum
- **Grammar Linter** (v0.3.x): Grammar errors with suggestion-based severity
- **CKVS Validation Engine** (v0.6.5-KG): Knowledge/axiom violations with `ValidationSeverity` enum

Each validator uses different data structures and severity models. Consumers (UI panels, fix orchestrators, reporting tools) need a single, unified representation to avoid translation logic scatter.

### 2.2 The Proposed Solution

Define a unified issue model with:

1. **UnifiedIssue record:** Common structure for all issue types
2. **IssueCategory enum:** Identifies source validator (Style, Grammar, Knowledge, Structure, Custom)
3. **UnifiedSeverity enum:** Four-level unified severity (Error, Warning, Info, Hint)
4. **Severity mappers:** Convert each validator's severity → UnifiedSeverity
5. **Location + Fix support:** Optional text spans and fix suggestions

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Dependencies

| Interface | Source Version | Purpose |
| :--- | :--- | :--- |
| `LintViolation` | v0.3.x | Style linter violation structure |
| `LintSeverity` | v0.3.x | Style linter severity enum |
| `GrammarError` | v0.3.x | Grammar linter error structure |
| `ValidationIssue` | v0.6.5-KG | CKVS validation issue structure |
| `ValidationSeverity` | v0.6.5-KG | CKVS validation severity enum |
| `TextSpan` | v0.1.x | Document location representation |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| (None required) | | Unified model uses only existing abstractions |

### 3.2 Licensing Behavior

- **Load Behavior:** Always available
  - UnifiedIssue record is a data contract, no licensing gate
  - Validators themselves are gated; filtering happens upstream
  - Core users see unified style issues only
  - WriterPro and above see all issue categories

---

## 4. Data Contract (The API)

### 4.1 Primary Data Type

```csharp
namespace Lexichord.Abstractions.Contracts.Validation;

/// <summary>
/// Represents a single issue detected by any validator in the unified validation system.
/// Normalizes findings from Style Linter, Grammar Linter, and CKVS Validation Engine
/// into a common structure for display and fix orchestration.
/// </summary>
public record UnifiedIssue
{
    /// <summary>
    /// Unique identifier for this issue instance.
    /// </summary>
    public required Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Category identifying the source validator type.
    /// </summary>
    public required IssueCategory Category { get; init; }

    /// <summary>
    /// Human-readable name of the validator that reported this issue.
    /// Examples: "Style Linter", "Grammar Checker", "Knowledge Engine".
    /// </summary>
    public required string ValidatorName { get; init; }

    /// <summary>
    /// Issue code or rule ID from the source validator.
    /// Examples: "STYLE_001", "AXIOM_MISSING_DESCRIPTION", "PASSIVE_VOICE".
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Unified severity across all validators.
    /// Normalized from source validator's severity model.
    /// </summary>
    public required UnifiedSeverity Severity { get; init; }

    /// <summary>
    /// Human-readable message describing the issue.
    /// Should be actionable and specific.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Optional location in the document (line/column or start/end offset).
    /// May be null for document-level issues (e.g., missing required sections).
    /// </summary>
    public TextSpan? Location { get; init; }

    /// <summary>
    /// Optional suggested fix for this issue.
    /// If present, indicates the issue is (potentially) auto-fixable.
    /// </summary>
    public UnifiedFix? Fix { get; init; }

    /// <summary>
    /// Additional context or metadata from the source validator.
    /// Structure varies by category; may include rule details, conflict info, etc.
    /// Consumers may cast to known types based on Category.
    /// </summary>
    public object? Context { get; init; }

    /// <summary>
    /// Whether this issue is a duplicate (detected by multiple validators).
    /// Used by aggregator to mark redundant findings.
    /// </summary>
    public bool IsDuplicate { get; init; } = false;

    /// <summary>
    /// IDs of other issues this one is a duplicate of.
    /// Useful for grouping related findings.
    /// </summary>
    public IReadOnlyList<Guid> DuplicateOf { get; init; } = Array.Empty<Guid>();

    /// <summary>
    /// Timestamp when this issue was detected.
    /// </summary>
    public DateTimeOffset DetectedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Whether the issue is currently suppressed in the UI.
    /// Used for dismissing issues without deleting them.
    /// </summary>
    public bool IsSuppressed { get; init; } = false;

    /// <summary>
    /// Optional rule severity from source validator (before normalization).
    /// Preserved for audit/reporting purposes.
    /// </summary>
    public string? OriginalSeverity { get; init; }
}
```

### 4.2 Issue Category Enum

```csharp
namespace Lexichord.Abstractions.Contracts.Validation;

/// <summary>
/// Categorizes the source of a unified issue.
/// Used for filtering, grouping, and routing to fix appliers.
/// </summary>
public enum IssueCategory
{
    /// <summary>
    /// Style violation detected by Style Linter (v0.3.x).
    /// Includes formatting, naming conventions, readability issues.
    /// </summary>
    Style = 1,

    /// <summary>
    /// Grammar or spelling error detected by Grammar Linter (v0.3.x).
    /// Includes spelling, punctuation, tense, voice issues.
    /// </summary>
    Grammar = 2,

    /// <summary>
    /// Knowledge/axiom violation detected by CKVS Validation Engine (v0.6.5-KG).
    /// Includes missing endpoints, parameter conflicts, schema mismatches.
    /// </summary>
    Knowledge = 3,

    /// <summary>
    /// Structural validation issue (e.g., required sections missing).
    /// May come from schema or document structure validators.
    /// </summary>
    Structure = 4,

    /// <summary>
    /// Issue from a custom or third-party validator.
    /// Used for extensibility.
    /// </summary>
    Custom = 5
}
```

### 4.3 Unified Severity Enum

```csharp
namespace Lexichord.Abstractions.Contracts.Validation;

/// <summary>
/// Four-level unified severity model across all validators.
/// Provides a common severity scale regardless of source.
/// </summary>
public enum UnifiedSeverity
{
    /// <summary>
    /// Critical issue that must be fixed before publication.
    /// Maps from: LintSeverity.Error, ValidationSeverity.Error.
    /// Blocks publishing and has visual prominence in UI.
    /// </summary>
    Error = 1,

    /// <summary>
    /// Important issue that should be fixed but does not block publishing.
    /// Maps from: LintSeverity.Warning, ValidationSeverity.Warning, Grammar suggestions treated as warnings.
    /// Shown in UI with warning styling.
    /// </summary>
    Warning = 2,

    /// <summary>
    /// Informational suggestion for improvement.
    /// Maps from: LintSeverity.Information, ValidationSeverity.Info.
    /// Often collapsed by default in UI.
    /// </summary>
    Info = 3,

    /// <summary>
    /// Optional enhancement or best practice tip.
    /// Maps from: ValidationSeverity.Hint, Grammar optional corrections.
    /// May be hidden by default; shown on demand.
    /// </summary>
    Hint = 4
}
```

### 4.4 Unified Fix Record

```csharp
namespace Lexichord.Abstractions.Contracts.Validation;

/// <summary>
/// Represents a suggested fix for a unified issue.
/// May be applied automatically or require user confirmation.
/// </summary>
public record UnifiedFix
{
    /// <summary>
    /// Unique identifier for this fix.
    /// </summary>
    public required Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Type of fix: Replacement, Insertion, Deletion, etc.
    /// </summary>
    public required FixType Type { get; init; }

    /// <summary>
    /// The location in the document where the fix applies.
    /// For replacements/deletions, the span to replace.
    /// For insertions, the location to insert at.
    /// </summary>
    public required TextSpan Location { get; init; }

    /// <summary>
    /// The new text to insert or replacement text.
    /// Empty for deletions.
    /// </summary>
    public required string NewText { get; init; }

    /// <summary>
    /// Original text before the fix (for reference/undo).
    /// </summary>
    public required string OldText { get; init; }

    /// <summary>
    /// Whether this fix can be applied automatically.
    /// False for fixes requiring human judgment.
    /// </summary>
    public bool CanAutoApply { get; init; } = true;

    /// <summary>
    /// Human-readable description of what the fix does.
    /// Example: "Replace with active voice".
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Optional confidence score (0.0 to 1.0) for AI-generated fixes.
    /// 1.0 = high confidence, < 0.8 = may need review.
    /// </summary>
    public double? Confidence { get; init; }

    /// <summary>
    /// Source fix applier that created this fix.
    /// Used for tracking and debugging.
    /// </summary>
    public string? ApplierName { get; init; }
}
```

### 4.5 Fix Type Enum

```csharp
namespace Lexichord.Abstractions.Contracts.Validation;

/// <summary>
/// Type of textual modification a fix performs.
/// </summary>
public enum FixType
{
    /// <summary>Replace text in a span with new text.</summary>
    Replacement = 1,

    /// <summary>Insert text at a location.</summary>
    Insertion = 2,

    /// <summary>Delete text in a span.</summary>
    Deletion = 3,

    /// <summary>Move text from one location to another.</summary>
    Move = 4,

    /// <summary>Non-textual fix (e.g., metadata change).</summary>
    Metadata = 5
}
```

### 4.6 TextSpan Type (Reference)

```csharp
namespace Lexichord.Abstractions.Contracts.Text;

/// <summary>
/// Represents a location in text (existing type, referenced here for completeness).
/// Used by all validators for issue location.
/// </summary>
public record TextSpan
{
    /// <summary>
    /// Zero-based character offset where the span starts.
    /// </summary>
    public required int Start { get; init; }

    /// <summary>
    /// Zero-based character offset where the span ends (exclusive).
    /// </summary>
    public required int End { get; init; }

    /// <summary>
    /// Convenience property for span length.
    /// </summary>
    public int Length => End - Start;

    /// <summary>
    /// One-based line number (for display purposes).
    /// </summary>
    public int? LineNumber { get; init; }

    /// <summary>
    /// One-based column number (for display purposes).
    /// </summary>
    public int? ColumnNumber { get; init; }
}
```

---

## 5. Severity Mapping Table

### 5.1 Style Linter Mapping

| Source Enum | Source Value | Target Enum | Target Value | Rationale |
| :--- | :--- | :--- | :--- | :--- |
| `LintSeverity` | Error | `UnifiedSeverity` | Error | Direct mapping |
| `LintSeverity` | Warning | `UnifiedSeverity` | Warning | Direct mapping |
| `LintSeverity` | Information | `UnifiedSeverity` | Info | Direct mapping |
| `LintSeverity` | Hint | `UnifiedSeverity` | Hint | Direct mapping |

### 5.2 Grammar Linter Mapping

| Source Enum | Source Value | Target Enum | Target Value | Rationale |
| :--- | :--- | :--- | :--- | :--- |
| `GrammarErrorType` | Error | `UnifiedSeverity` | Error | Grammatical errors block publishing |
| `GrammarErrorType` | Suggestion | `UnifiedSeverity` | Info | Grammar suggestions are informational |
| `GrammarErrorType` | Note | `UnifiedSeverity` | Hint | Grammar notes are optional hints |

### 5.3 CKVS Validation Engine Mapping

| Source Enum | Source Value | Target Enum | Target Value | Rationale |
| :--- | :--- | :--- | :--- | :--- |
| `ValidationSeverity` | Error | `UnifiedSeverity` | Error | Axiom/schema errors block publishing |
| `ValidationSeverity` | Warning | `UnifiedSeverity` | Warning | Validation warnings should be addressed |
| `ValidationSeverity` | Info | `UnifiedSeverity` | Info | Informational validation notes |
| `ValidationSeverity` | Hint | `UnifiedSeverity` | Hint | Optional hints from validator |

---

## 6. Implementation

### 6.1 Severity Mapper Helper

```csharp
namespace Lexichord.Modules.Validation.Internals;

/// <summary>
/// Helper class for converting validator-specific severities to unified severity.
/// </summary>
public static class SeverityMapper
{
    /// <summary>
    /// Maps Style Linter severity to unified severity.
    /// </summary>
    public static UnifiedSeverity FromLintSeverity(LintSeverity severity) =>
        severity switch
        {
            LintSeverity.Error => UnifiedSeverity.Error,
            LintSeverity.Warning => UnifiedSeverity.Warning,
            LintSeverity.Information => UnifiedSeverity.Info,
            LintSeverity.Hint => UnifiedSeverity.Hint,
            _ => UnifiedSeverity.Info
        };

    /// <summary>
    /// Maps Grammar Linter error type to unified severity.
    /// </summary>
    public static UnifiedSeverity FromGrammarError(GrammarErrorType errorType) =>
        errorType switch
        {
            GrammarErrorType.Error => UnifiedSeverity.Error,
            GrammarErrorType.Suggestion => UnifiedSeverity.Info,
            GrammarErrorType.Note => UnifiedSeverity.Hint,
            _ => UnifiedSeverity.Info
        };

    /// <summary>
    /// Maps CKVS Validation Engine severity to unified severity.
    /// </summary>
    public static UnifiedSeverity FromValidationSeverity(ValidationSeverity severity) =>
        severity switch
        {
            ValidationSeverity.Error => UnifiedSeverity.Error,
            ValidationSeverity.Warning => UnifiedSeverity.Warning,
            ValidationSeverity.Info => UnifiedSeverity.Info,
            ValidationSeverity.Hint => UnifiedSeverity.Hint,
            _ => UnifiedSeverity.Info
        };
}
```

### 6.2 Issue Factory Methods

```csharp
namespace Lexichord.Modules.Validation.Internals;

/// <summary>
/// Factory methods for creating UnifiedIssue instances from various source types.
/// </summary>
public static class UnifiedIssueFactory
{
    /// <summary>
    /// Creates a UnifiedIssue from a Style Linter violation.
    /// </summary>
    public static UnifiedIssue FromLintViolation(
        LintViolation violation,
        StyleRule rule,
        string? suggestedFix = null)
    {
        var fix = suggestedFix != null
            ? new UnifiedFix
            {
                Id = Guid.NewGuid(),
                Type = FixType.Replacement,
                Location = violation.Location,
                NewText = suggestedFix,
                OldText = violation.ViolatedText,
                CanAutoApply = true,
                Description = rule.Explanation,
                ApplierName = "Style Linter",
                Confidence = 0.95
            }
            : null;

        return new UnifiedIssue
        {
            Id = Guid.NewGuid(),
            Category = IssueCategory.Style,
            ValidatorName = "Style Linter",
            Code = rule.RuleId,
            Severity = SeverityMapper.FromLintSeverity(violation.Severity),
            Message = violation.Message,
            Location = violation.Location,
            Fix = fix,
            Context = new { Rule = rule, Violation = violation },
            DetectedAt = DateTimeOffset.UtcNow,
            OriginalSeverity = violation.Severity.ToString()
        };
    }

    /// <summary>
    /// Creates a UnifiedIssue from a Grammar Linter error.
    /// </summary>
    public static UnifiedIssue FromGrammarError(
        GrammarError error,
        string? suggestedReplacement = null)
    {
        var fix = suggestedReplacement != null
            ? new UnifiedFix
            {
                Id = Guid.NewGuid(),
                Type = FixType.Replacement,
                Location = error.Location,
                NewText = suggestedReplacement,
                OldText = error.OriginalText,
                CanAutoApply = error.ErrorType != GrammarErrorType.Suggestion,
                Description = error.Description,
                ApplierName = "Grammar Linter",
                Confidence = error.Confidence ?? 0.9
            }
            : null;

        return new UnifiedIssue
        {
            Id = Guid.NewGuid(),
            Category = IssueCategory.Grammar,
            ValidatorName = "Grammar Linter",
            Code = error.RuleCode,
            Severity = SeverityMapper.FromGrammarError(error.ErrorType),
            Message = error.Description,
            Location = error.Location,
            Fix = fix,
            Context = error,
            DetectedAt = DateTimeOffset.UtcNow,
            OriginalSeverity = error.ErrorType.ToString()
        };
    }

    /// <summary>
    /// Creates a UnifiedIssue from a CKVS Validation Engine issue.
    /// </summary>
    public static UnifiedIssue FromValidationIssue(
        ValidationIssue issue,
        UnifiedFix? fix = null)
    {
        return new UnifiedIssue
        {
            Id = Guid.NewGuid(),
            Category = IssueCategory.Knowledge,
            ValidatorName = "CKVS Validation Engine",
            Code = issue.ViolationCode,
            Severity = SeverityMapper.FromValidationSeverity(issue.Severity),
            Message = issue.Message,
            Location = issue.Location,
            Fix = fix,
            Context = issue,
            DetectedAt = DateTimeOffset.UtcNow,
            OriginalSeverity = issue.Severity.ToString()
        };
    }
}
```

---

## 7. Error Handling

### 7.1 Invalid Severity Mapping

**Scenario:** Source validator uses unknown severity value.

**Handling:**
- Fall back to `UnifiedSeverity.Info`
- Log warning with severity value
- Preserve original severity in `OriginalSeverity` field

**Code:**
```csharp
public static UnifiedSeverity FromLintSeverity(LintSeverity severity)
{
    var mapped = severity switch { /* ... */ _ => UnifiedSeverity.Info };

    if (mapped == UnifiedSeverity.Info && severity.ToString() != nameof(LintSeverity.Information))
    {
        _logger.LogWarning(
            "Unknown LintSeverity encountered: {Severity}, falling back to Info",
            severity);
    }

    return mapped;
}
```

### 7.2 Missing Location

**Scenario:** Issue has no specific location in document.

**Handling:**
- `Location` property is nullable
- UI treats null location as document-level issue
- Cannot auto-apply fixes without location

### 7.3 Invalid Fix Data

**Scenario:** Suggested fix has invalid location or text.

**Handling:**
- Set `CanAutoApply = false`
- Log warning with issue ID and fix details
- Preserve fix for manual review

---

## 8. Testing

### 8.1 Unit Tests

```csharp
[TestClass]
public class UnifiedIssueTests
{
    [TestMethod]
    public void UnifiedIssue_HasUniqueId()
    {
        var issue1 = new UnifiedIssue { /* ... */ };
        var issue2 = new UnifiedIssue { /* ... */ };

        Assert.AreNotEqual(issue1.Id, issue2.Id);
    }

    [TestMethod]
    public void SeverityMapper_MapsLintSeverityCorrectly()
    {
        Assert.AreEqual(
            UnifiedSeverity.Error,
            SeverityMapper.FromLintSeverity(LintSeverity.Error));

        Assert.AreEqual(
            UnifiedSeverity.Warning,
            SeverityMapper.FromLintSeverity(LintSeverity.Warning));
    }

    [TestMethod]
    public void SeverityMapper_UnknownSeverity_DefaultsToInfo()
    {
        var unknown = (LintSeverity)999;

        var result = SeverityMapper.FromLintSeverity(unknown);

        Assert.AreEqual(UnifiedSeverity.Info, result);
    }

    [TestMethod]
    public void UnifiedIssueFactory_FromLintViolation_CreatesValidIssue()
    {
        var violation = new LintViolation { /* ... */ };
        var rule = new StyleRule { /* ... */ };

        var issue = UnifiedIssueFactory.FromLintViolation(violation, rule, "fix text");

        Assert.IsNotNull(issue);
        Assert.AreEqual(IssueCategory.Style, issue.Category);
        Assert.AreEqual("Style Linter", issue.ValidatorName);
        Assert.IsNotNull(issue.Fix);
        Assert.AreEqual("fix text", issue.Fix.NewText);
    }

    [TestMethod]
    public void UnifiedIssueFactory_FromGrammarError_CreatesValidIssue()
    {
        var error = new GrammarError { /* ... */ };

        var issue = UnifiedIssueFactory.FromGrammarError(error, "replacement");

        Assert.IsNotNull(issue);
        Assert.AreEqual(IssueCategory.Grammar, issue.Category);
        Assert.AreEqual("Grammar Linter", issue.ValidatorName);
    }

    [TestMethod]
    public void UnifiedFix_Replacement_HasCorrectType()
    {
        var fix = new UnifiedFix { Type = FixType.Replacement };

        Assert.AreEqual(FixType.Replacement, fix.Type);
    }

    [TestMethod]
    public void TextSpan_Length_CalculatedCorrectly()
    {
        var span = new TextSpan { Start = 10, End = 25 };

        Assert.AreEqual(15, span.Length);
    }
}
```

### 8.2 Integration Tests

```csharp
[TestClass]
public class UnifiedIssueIntegrationTests
{
    [TestMethod]
    public void Aggregator_CreatesUnifiedIssues_FromMultipleSources()
    {
        // Arrange: violations from different validators
        var styleLintResult = CreateMockStyleLintResult();
        var grammarResult = CreateMockGrammarResult();
        var validationResult = CreateMockValidationResult();

        // Act: aggregate all
        var issues = new List<UnifiedIssue>();
        issues.AddRange(styleLintResult.Select(/* factory */));
        issues.AddRange(grammarResult.Select(/* factory */));
        issues.AddRange(validationResult.Select(/* factory */));

        // Assert: unified representation
        Assert.IsTrue(issues.Any(i => i.Category == IssueCategory.Style));
        Assert.IsTrue(issues.Any(i => i.Category == IssueCategory.Grammar));
        Assert.IsTrue(issues.Any(i => i.Category == IssueCategory.Knowledge));
    }
}
```

---

## 9. Security & Safety

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| Null reference | Low | All required fields use `required` keyword |
| Invalid severity | Low | Default to Info with warning log |
| Malformed location | Low | TextSpan validation in aggregator |
| Memory exhaustion | Medium | Limited by parent aggregator (max issue count) |

---

## 10. Acceptance Criteria

### 10.1 Functional Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | Style linter violation | Creating UnifiedIssue | Category = Style, correct severity mapped |
| 2 | Grammar error | Creating UnifiedIssue | Category = Grammar, correct severity mapped |
| 3 | Validation issue | Creating UnifiedIssue | Category = Knowledge, correct severity mapped |
| 4 | Issue with unknown severity | Creating UnifiedIssue | Defaults to Info, OriginalSeverity preserved |
| 5 | Issue with fix suggestion | Creating UnifiedIssue | Fix populated, CanAutoApply set correctly |
| 6 | Document-level issue | Creating UnifiedIssue | Location = null, still valid |
| 7 | Multiple issues | Grouping by severity | Correctly separated into Error/Warning/Info/Hint |

### 10.2 Performance Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 8 | 1000 violations | Creating 1000 UnifiedIssues | Completes in < 50ms |
| 9 | Factory method call | Creating UnifiedIssue | No allocations beyond record itself |

### 10.3 Code Quality Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 10 | UnifiedIssue record | Serialization | JSON-serializable, all fields represented |
| 11 | TextSpan with line/column | Display | Can be formatted as "Line X, Column Y" |

---

## 11. Deliverable Checklist

| # | Deliverable | Status |
| :--- | :--- | :--- |
| 1 | `UnifiedIssue` record | [ ] |
| 2 | `IssueCategory` enum | [ ] |
| 3 | `UnifiedSeverity` enum | [ ] |
| 4 | `UnifiedFix` record | [ ] |
| 5 | `FixType` enum | [ ] |
| 6 | `SeverityMapper` helper class | [ ] |
| 7 | `UnifiedIssueFactory` factory methods | [ ] |
| 8 | Unit tests for all types | [ ] |
| 9 | Integration tests with real validators | [ ] |
| 10 | Documentation with examples | [ ] |
| 11 | JSON serialization support | [ ] |

---

## 12. Verification Commands

```bash
# Run unit tests for unified issue model
dotnet test --filter "Version=v0.7.5e" --logger "console;verbosity=detailed"

# Run integration tests
dotnet test --filter "Category=Integration&Version=v0.7.5e"

# Verify serialization
dotnet build && dotnet run --project tests/Lexichord.Tests.Validation -- --test-serialization UnifiedIssue

# Manual verification:
# 1. Create UnifiedIssue from each validator type
# 2. Verify all required fields populated
# 3. Verify severity mapping correct
# 4. Verify JSON serialization round-trips
# 5. Test with null Location and Fix
```

---

## Document History

| Version | Date | Author | Changes |
| :--- | :--- | :--- | :--- |
| 1.0 | 2026-01-31 | Lead Architect | Initial draft |
