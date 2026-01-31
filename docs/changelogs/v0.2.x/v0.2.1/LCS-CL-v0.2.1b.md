# LCS-CL-021b: Rule Object Model

## Document Control

| Field            | Value             |
| :--------------- | :---------------- |
| **Document ID**  | LCS-CL-021b       |
| **Version**      | v0.2.1b           |
| **Date**         | 2026-01-29        |
| **Status**       | Complete          |
| **Parent Spec**  | LCS-DES-021b      |
| **Feature Name** | Rule Object Model |
| **Module**       | Style             |

---

## Summary

Expanded the stub domain types from v0.2.1a into full implementations with pattern matching and violation detection. The `StyleRule` record now supports async pattern matching via `FindViolationsAsync()` with lazy regex compilation and ReDoS protection. The `StyleViolation` record provides complete position information for editor integration. The `StyleSheet` record supports rule merging for "extends: default" semantics.

---

## Changes Made

### Enums Updated

| Enum                | Previous Values                                                  | New Values (Spec-Compliant)                                                               |
| :------------------ | :--------------------------------------------------------------- | :---------------------------------------------------------------------------------------- |
| `RuleCategory`      | Vocabulary, Grammar, Punctuation, Consistency, Structure, Custom | Terminology (0), Formatting (1), Syntax (2)                                               |
| `ViolationSeverity` | Hint (0), Suggestion (1), Warning (2), Error (3)                 | Error (0), Warning (1), Info (2), Hint (3)                                                |
| `PatternType`       | Literal, Regex, WordList, Custom                                 | Regex (0), Literal (1), LiteralIgnoreCase (2), StartsWith (3), EndsWith (4), Contains (5) |

### StyleRule Record

| Method/Property         | Description                                                         |
| :---------------------- | :------------------------------------------------------------------ |
| `FindViolationsAsync()` | Pattern matching with position calculation; returns violations list |
| `Disable()`             | Creates disabled copy                                               |
| `Enable()`              | Creates enabled copy                                                |
| `WithSeverity()`        | Creates copy with new severity                                      |
| `WithPattern()`         | Creates copy with new pattern and type                              |

**Implementation Details:**

- Lazy regex compilation via `Lazy<Regex?>` field
- 100ms regex timeout for ReDoS protection
- Line/column position calculation (1-indexed)
- Support for all 6 pattern types

### StyleViolation Record

| Property/Method           | Description                                       |
| :------------------------ | :------------------------------------------------ |
| `Length`                  | Computed: `EndOffset - StartOffset`               |
| `GetSurroundingContext()` | Shows match with configurable prefix/suffix chars |
| `WithSeverity()`          | Creates copy with new severity                    |
| `WithMessage()`           | Creates copy with new message                     |

### StyleSheet Record

| Method                 | Description                                    |
| :--------------------- | :--------------------------------------------- |
| `Empty`                | Static singleton for null object pattern       |
| `GetEnabledRules()`    | Filters to enabled rules only                  |
| `GetRulesByCategory()` | Filters by category                            |
| `GetRulesBySeverity()` | Filters by severity threshold                  |
| `FindRuleById()`       | Case-insensitive lookup                        |
| `MergeWith()`          | Combines with base sheet (this overrides base) |
| `DisableRule()`        | Creates copy with rule disabled                |
| `SetRuleSeverity()`    | Creates copy with rule severity changed        |
| `HasBaseSheet`         | Whether `Extends` is set                       |
| `EnabledRuleCount`     | Count of enabled rules                         |

---

## Files Modified

| File                                                           | Change                                           |
| :------------------------------------------------------------- | :----------------------------------------------- |
| `src/Lexichord.Abstractions/Contracts/StyleDomainTypes.cs`     | Full rewrite with spec-compliant implementations |
| `src/Lexichord.Modules.Style/Services/YamlStyleSheetLoader.cs` | Updated to use constructor syntax                |
| `tests/Lexichord.Tests.Unit/Modules/Style/StyleEngineTests.cs` | Updated to use constructor syntax                |

## Files Created

| File                                                                    | Purpose                    |
| :---------------------------------------------------------------------- | :------------------------- |
| `tests/Lexichord.Tests.Unit/Abstractions/Domain/EnumTests.cs`           | Enum value verification    |
| `tests/Lexichord.Tests.Unit/Abstractions/Domain/StyleRuleTests.cs`      | Pattern matching tests     |
| `tests/Lexichord.Tests.Unit/Abstractions/Domain/StyleViolationTests.cs` | Position calculation tests |
| `tests/Lexichord.Tests.Unit/Abstractions/Domain/StyleSheetTests.cs`     | Merge and filtering tests  |

---

## Test Results

```
Test summary: total: 1407, failed: 0, succeeded: 1379, skipped: 28
```

New tests added:

- `EnumTests`: 7 tests (enum values, ordering)
- `StyleRuleTests`: 14 tests (pattern matching, positions, helper methods)
- `StyleViolationTests`: 9 tests (length, context, helper methods)
- `StyleSheetTests`: 14 tests (merge, filtering, rule management)

---

## Verification Commands

```bash
# Build
dotnet build

# Run all Style-related tests
dotnet test tests/Lexichord.Tests.Unit --filter "FullyQualifiedName~Style"

# Run domain object tests
dotnet test tests/Lexichord.Tests.Unit --filter "FullyQualifiedName~Domain"
```

---

## Acceptance Criteria Status

| #   | Criterion                                          | Status |
| :-- | :------------------------------------------------- | :----- |
| 1   | `RuleCategory` has Terminology, Formatting, Syntax | ✅     |
| 2   | `ViolationSeverity` has Error, Warning, Info, Hint | ✅     |
| 3   | `PatternType` has 6 values per spec                | ✅     |
| 4   | `StyleRule` is an immutable record                 | ✅     |
| 5   | `FindViolationsAsync` returns correct positions    | ✅     |
| 6   | Regex patterns compile lazily and are cached       | ✅     |
| 7   | Disabled rules return no violations                | ✅     |
| 8   | `StyleViolation` includes all position information | ✅     |
| 9   | `GetSurroundingContext` shows match in context     | ✅     |
| 10  | `StyleSheet` is an immutable record                | ✅     |
| 11  | `StyleSheet.Empty` is available as default         | ✅     |
| 12  | `MergeWith` correctly combines rules               | ✅     |
