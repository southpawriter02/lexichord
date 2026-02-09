# LCS-CL-v065j: Linter Integration

| Field       | Value                            |
| :---------- | :------------------------------- |
| **Date**    | 2026-02-08                       |
| **Version** | v0.6.5j                          |
| **Author**  | Documentation Agent              |
| **Status**  | ✅ Implemented                   |
| **Spec**    | LCS-DES-065-KG-j                |

## 1. Summary

Implemented the Linter Integration — bridges the CKVS Validation Engine (v0.6.5e) with Lexichord's style linter (v0.3.x), providing a unified view of all findings, consistent severity normalization, and a combined fix workflow (CKVS Phase 3a).

## 2. Changes

### 2.1 Abstractions — Data Contracts & Interfaces
- **New `Contracts/Knowledge/Validation/Integration/` namespace** (13 files):
  - `FindingSource.cs`: Enum — `Validation`, `StyleLinter`, `GrammarLinter`.
  - `UnifiedSeverity.cs`: Enum — `Error`, `Warning`, `Info`, `Hint`.
  - `FindingCategory.cs`: Enum — `Schema`, `Axiom`, `Consistency`, `Style`, `Grammar`, `Spelling`.
  - `UnifiedStatus.cs`: Enum — `Pass`, `PassWithWarnings`, `Fail`.
  - `UnifiedFinding.cs`: Record bridging `ValidationFinding`/`StyleViolation` to a common shape. Retains original references.
  - `UnifiedFix.cs`: Record with `ReplacementText`, `Confidence`, `CanAutoApply`.
  - `UnifiedFindingResult.cs`: Aggregated result with `ByCategory`/`BySeverity` summaries and `Empty()` factory.
  - `UnifiedFindingOptions.cs`: Options record — source toggles, severity threshold, category filter, max findings.
  - `FixConflictResult.cs` + `FixConflict`: Conflict detection records.
  - `FixApplicationResult.cs`: Fix application outcome with `Success()`/`Empty()` factories.
  - `ILinterIntegration.cs`: Main integration interface — `GetUnifiedFindingsAsync()`, `ApplyAllFixesAsync()`.
  - `IUnifiedFindingAdapter.cs`: Finding adapter — typed `NormalizeSeverity()` overloads.
  - `ICombinedFixWorkflow.cs`: Fix workflow — `CheckForConflicts()`, `OrderFixesForApplication()`.

### 2.2 Knowledge Module — Service Implementations
- **New `Validation/Integration/` namespace** (3 files):
  - `UnifiedFindingAdapter.cs`: Maps `ValidationFinding`→`UnifiedFinding` (ValidatorId→Category, SuggestedFix→UnifiedFix@0.5) and `StyleViolation`→`UnifiedFinding` (→Style category, Suggestion→UnifiedFix@0.7).
  - `CombinedFixWorkflow.cs`: Detects conflicts (same FindingId), orders fixes by FindingId.
  - `LinterIntegration.cs`: Parallel `Task.WhenAll` orchestration of `IValidationEngine`+`IStyleEngine`, severity/category filtering, sorting, maxFindings cap, status computation, resilient error handling.

### 2.3 DI Registration
- **Modified `KnowledgeModule.cs`**: Registered `UnifiedFindingAdapter`, `CombinedFixWorkflow`, and `LinterIntegration` as singletons. `ILinterIntegration` forwarded to `LinterIntegration`.

### 2.4 Testing
- **New `UnifiedFindingAdapterTests.cs`**: 13 tests — severity/category mapping, fix creation, property path pass-through.
- **New `CombinedFixWorkflowTests.cs`**: 8 tests — conflict detection, fix ordering.
- **New `LinterIntegrationTests.cs`**: 17 tests — parallel execution, filtering, sorting, status computation, error resilience, fix application.

### 2.5 Spec-to-Codebase Adaptations
- `Document` parameter → `(string documentId, string content)` pair.
- `ILinterService`/`LinterFinding` → `IStyleEngine`/`StyleViolation`.
- `TextSpan` → `string PropertyPath`.
- `NormalizeSeverity(object, FindingSource)` → typed overloads.

## 3. Verification Results

| Area                | Result | Notes                                              |
| :------------------ | :----: | :------------------------------------------------- |
| **Build**           |  PASS  | Abstractions + Knowledge + Tests: 0 errors         |
| **Unit Tests**      |  PASS  | 38/38 new integration tests passed                 |
| **Regression**      |  PASS  | 7960 passed, 1 pre-existing failure, 33 skipped    |

## 4. Next Steps
- Wire `ILinterIntegration` into the Problems Panel UI (`IProblemsPanelViewModel`).
- Add grammar linter adapter when grammar linter is implemented.
