# Changelog: v0.7.5e — Unified Issue Model

**Feature ID:** AGT-075e
**Version:** 0.7.5e
**Date:** 2026-02-16
**Status:** Complete

---

## Overview

Implements the Unified Issue Model for the Unified Validation feature, providing a normalized representation for validation findings from different sources (Style Linter, Grammar Linter, CKVS Validation Engine). This is the fifth sub-part of v0.7.5 "The Tuning Agent" and enables consistent processing of issues regardless of their source.

The implementation adds:
- `IssueCategory` — Unified categorization (Style, Grammar, Knowledge, Structure, Custom)
- `FixType` — Fix operation types (Replacement, Insertion, Deletion, Rewrite, NoFix)
- `UnifiedFix` — Location-aware fix representation with confidence scoring
- `UnifiedIssue` — Normalized issue record with fixes, severity, and source tracking
- `SeverityMapper` — Maps source-specific severities to UnifiedSeverity
- `UnifiedIssueFactory` — Factory methods for creating issues from various sources
- Full unit test coverage (107 tests)

---

## What's New

### IssueCategory Enum

Unified categorization for validation issues:
- **Namespace:** `Lexichord.Abstractions.Contracts.Validation`
- **Values:**
  - `Style = 0` — Style guide violations
  - `Grammar = 1` — Grammar/spelling issues
  - `Knowledge = 2` — CKVS validation findings
  - `Structure = 3` — Document structure issues
  - `Custom = 4` — User-defined rules

### FixType Enum

Types of fix operations:
- **Namespace:** `Lexichord.Abstractions.Contracts.Validation`
- **Values:**
  - `Replacement = 0` — Replace OldText with NewText
  - `Insertion = 1` — Insert NewText at position
  - `Deletion = 2` — Delete text at position
  - `Rewrite = 3` — AI-generated rewrite
  - `NoFix = 4` — No automatic fix available

### UnifiedFix Record

Location-aware fix representation:
- **Namespace:** `Lexichord.Abstractions.Contracts.Validation`
- **Properties:**
  - `FixId` — Unique identifier (Guid)
  - `Location` — TextSpan for fix position
  - `OldText` — Original text (nullable)
  - `NewText` — Replacement text (nullable)
  - `Type` — FixType enum value
  - `Description` — Human-readable description
  - `Confidence` — Score from 0.0 to 1.0
  - `CanAutoApply` — Whether fix can be auto-applied
- **Computed Properties:**
  - `IsHighConfidence` — True when confidence >= 0.8
  - `HasChanges` — True when OldText differs from NewText
- **Factory Methods:**
  - `Replacement()` — Create replacement fix
  - `Insertion()` — Create insertion fix
  - `Deletion()` — Create deletion fix
  - `Rewrite()` — Create AI rewrite fix
  - `NoFixAvailable()` — Create no-fix placeholder

### UnifiedIssue Record

Normalized validation issue:
- **Namespace:** `Lexichord.Abstractions.Contracts.Validation`
- **Properties:**
  - `IssueId` — Unique identifier (Guid)
  - `SourceId` — Original source identifier (rule ID, code)
  - `Category` — IssueCategory enum value
  - `Severity` — UnifiedSeverity from v0.6.5j
  - `Message` — Human-readable description
  - `Location` — TextSpan for issue position
  - `OriginalText` — Text at issue location (nullable)
  - `Fixes` — List of UnifiedFix (sorted by confidence)
  - `SourceType` — "StyleLinter", "GrammarLinter", "Validation"
  - `OriginalSource` — Original source object (nullable)
- **Computed Properties:**
  - `HasFixes` — True when non-NoFix fixes exist
  - `BestFix` — Highest confidence fix (nullable)
  - `HasHighConfidenceFix` — True when best fix >= 0.8
  - `CanAutoFix` — True when best fix is auto-applicable
  - `SeverityOrder` — Numeric order for sorting
  - `IsFromStyleLinter`, `IsFromGrammarLinter`, `IsFromValidation`
- **Methods:**
  - `WithFixes()` — Create copy with new fixes
  - `WithAdditionalFix()` — Add fix maintaining sort order
- **Static Properties:**
  - `Empty` — Empty issue placeholder

### SeverityMapper Static Class

Maps source-specific severities:
- **Namespace:** `Lexichord.Abstractions.Contracts.Validation`
- **Methods:**
  - `FromViolationSeverity()` — ViolationSeverity → UnifiedSeverity
  - `FromValidationSeverity()` — ValidationSeverity → UnifiedSeverity
  - `FromDeviationPriority()` — DeviationPriority → UnifiedSeverity
  - `ToViolationSeverity()` — UnifiedSeverity → ViolationSeverity
  - `ToValidationSeverity()` — UnifiedSeverity → ValidationSeverity
  - `ToDeviationPriority()` — UnifiedSeverity → DeviationPriority
- **Mapping Logic:**
  - ViolationSeverity: Direct 1:1 mapping (same values)
  - ValidationSeverity: Info=0→Info(2), Warning=1→Warning(1), Error=2→Error(0)
  - DeviationPriority: Critical→Error, High→Warning, Normal→Info, Low→Hint

### UnifiedIssueFactory Static Class

Factory methods for creating issues:
- **Namespace:** `Lexichord.Abstractions.Contracts.Validation`
- **Methods:**
  - `FromStyleDeviation()` — Convert StyleDeviation to UnifiedIssue
  - `FromStyleViolation()` — Convert StyleViolation to UnifiedIssue
  - `FromUnifiedFinding()` — Convert UnifiedFinding to UnifiedIssue
  - `FromValidationFinding()` — Convert ValidationFinding to UnifiedIssue
  - `FromDeviations()` — Batch convert StyleDeviations
  - `FromViolations()` — Batch convert StyleViolations
  - `FromFindings()` — Batch convert UnifiedFindings

---

## Spec Adaptations

The design spec (LCS-DES-v0.7.5-KG-e.md) references several APIs that don't match the actual codebase:

1. **TextSpan Namespace** — Spec proposes `Lexichord.Abstractions.Contracts.Text.TextSpan` with LineNumber?/ColumnNumber?. Adapted to reuse existing `Editor.TextSpan` (Start, Length).

2. **Type Names** — Spec references `LintViolation`, `LintSeverity`, `ValidationIssue`. Actual types: `StyleViolation`, `ViolationSeverity`, `ValidationFinding`.

3. **Existing v0.6.5j Types** — Spec doesn't account for existing `UnifiedFinding`, `UnifiedFix`, `FindingCategory` in `Knowledge.Validation.Integration`. Created parallel types in new namespace to avoid breaking changes.

4. **Namespace** — Created new `Lexichord.Abstractions.Contracts.Validation` as spec proposes, coexisting with `Knowledge.Validation.Integration`.

---

## Files Created

### Contracts — Enums (2 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Abstractions/Contracts/Validation/IssueCategory.cs` | Enum | Issue categories |
| `src/Lexichord.Abstractions/Contracts/Validation/FixType.cs` | Enum | Fix operation types |

### Contracts — Records (2 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Abstractions/Contracts/Validation/UnifiedFix.cs` | Record | Location-aware fix |
| `src/Lexichord.Abstractions/Contracts/Validation/UnifiedIssue.cs` | Record | Normalized issue |

### Contracts — Helpers (2 files)

| File | Type | Description |
|:-----|:-----|:------------|
| `src/Lexichord.Abstractions/Contracts/Validation/SeverityMapper.cs` | Static Class | Severity conversion |
| `src/Lexichord.Abstractions/Contracts/Validation/UnifiedIssueFactory.cs` | Static Class | Issue factories |

### Tests (3 files)

| File | Tests | Description |
|:-----|:-----:|:------------|
| `tests/Lexichord.Tests.Unit/Abstractions/Validation/UnifiedIssueTests.cs` | 35 | Record and enum tests |
| `tests/Lexichord.Tests.Unit/Abstractions/Validation/SeverityMapperTests.cs` | 28 | Severity mapping tests |
| `tests/Lexichord.Tests.Unit/Abstractions/Validation/UnifiedIssueFactoryTests.cs` | 44 | Factory method tests |

---

## Testing

### Test Summary

| Test File | Tests | Key Coverage |
|:----------|:-----:|:-------------|
| UnifiedIssueTests | 35 | IssueCategory, FixType, UnifiedFix, UnifiedIssue records |
| SeverityMapperTests | 28 | All severity mapping methods and round-trips |
| UnifiedIssueFactoryTests | 44 | All factory methods, null handling, batch conversion |
| **Total v0.7.5e** | **107** | All v0.7.5e functionality |

### Test Groups

| Group | Tests | Description |
|:------|:-----:|:------------|
| IssueCategory enum | 2 | Values, default |
| FixType enum | 2 | Values, default |
| UnifiedFix factory | 5 | Replacement, Insertion, Deletion, Rewrite, NoFix |
| UnifiedFix properties | 3 | IsHighConfidence, HasChanges |
| UnifiedIssue properties | 8 | HasFixes, BestFix, CanAutoFix, SeverityOrder, source types |
| UnifiedIssue methods | 3 | WithFixes, WithAdditionalFix, Empty |
| SeverityMapper.FromViolationSeverity | 4 | Each severity level |
| SeverityMapper.FromValidationSeverity | 3 | Each severity level |
| SeverityMapper.FromDeviationPriority | 4 | Each priority level |
| SeverityMapper.ToViolationSeverity | 4 | Each severity level |
| SeverityMapper.ToValidationSeverity | 4 | Each severity level (including Hint→Info) |
| SeverityMapper.ToDeviationPriority | 4 | Each severity level |
| SeverityMapper round-trips | 8 | ViolationSeverity, DeviationPriority |
| Factory.FromStyleDeviation | 6 | Conversion, priority mapping, suggestions |
| Factory.FromStyleViolation | 4 | Conversion, severity mapping, suggestions |
| Factory.FromUnifiedFinding | 5 | Conversion, category/source mapping |
| Factory.FromValidationFinding | 4 | Conversion, severity mapping |
| Factory batch methods | 4 | FromDeviations, FromViolations, FromFindings |
| Factory null handling | 4 | ArgumentNullException tests |

### Test Traits

All tests use: `[Trait("Category", "Unit")]`, `[Trait("SubPart", "v0.7.5e")]`

---

## Design Decisions

1. **New Namespace for Coexistence** — Created `Lexichord.Abstractions.Contracts.Validation` rather than modifying existing v0.6.5j types in `Knowledge.Validation.Integration` to avoid breaking changes.

2. **Reuse Editor.TextSpan** — Used existing `Editor.TextSpan(int Start, int Length)` instead of creating new `Text.TextSpan` with optional LineNumber/ColumnNumber.

3. **Reuse UnifiedSeverity** — Used existing `UnifiedSeverity` enum from v0.6.5j rather than creating a new severity type.

4. **Using Aliases in Tests** — Added `using UnifiedFix = Lexichord.Abstractions.Contracts.Validation.UnifiedFix;` to disambiguate from `Knowledge.Validation.Integration.UnifiedFix`.

5. **Factory Pattern** — Used static factory class rather than constructors for creating `UnifiedIssue` from different source types, enabling proper mapping and validation.

6. **Confidence-Based Sorting** — `UnifiedIssue.Fixes` is expected to be sorted by confidence (highest first), with `BestFix` returning the first non-NoFix entry.

7. **No DI Registration** — All types are pure data contracts and static helpers; no services requiring DI registration.

---

## Dependencies

### Consumed (from existing modules)

| Type | Namespace | Version |
|:-----|:----------|:--------|
| `TextSpan` | `Editor` | v0.6.7b |
| `UnifiedSeverity` | `Knowledge.Validation.Integration` | v0.6.5j |
| `UnifiedFinding` | `Knowledge.Validation.Integration` | v0.6.5j |
| `FindingSource` | `Knowledge.Validation.Integration` | v0.6.5j |
| `FindingCategory` | `Knowledge.Validation.Integration` | v0.6.5j |
| `StyleDeviation` | `Agents` | v0.7.5a |
| `DeviationPriority` | `Agents` | v0.7.5a |
| `StyleViolation` | `StyleDomainTypes` | v0.2.1b |
| `ViolationSeverity` | `StyleDomainTypes` | v0.2.1b |
| `StyleRule` | `StyleDomainTypes` | v0.2.1b |
| `RuleCategory` | `StyleDomainTypes` | v0.2.1b |
| `ValidationFinding` | `Knowledge.Validation` | v0.6.5e |
| `ValidationSeverity` | `Knowledge.Validation` | v0.6.5e |

### Produced (new types)

| Type | Consumers |
|:-----|:----------|
| `UnifiedIssue` | Tuning Agent (v0.7.5f+), Quick Fix Panel |
| `UnifiedFix` | Fix application logic, Tuning Panel |
| `IssueCategory` | Filtering, grouping in UI |
| `FixType` | Fix application logic |
| `SeverityMapper` | Conversion utilities |
| `UnifiedIssueFactory` | Issue creation from any source |

---

## Build & Test Results

```
Build:     Succeeded (0 errors, 0 warnings in Abstractions)
v0.7.5e:   107 passed, 0 failed
Full suite: 5512 passed, 0 failed, 27 skipped
```
