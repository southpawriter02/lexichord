# Changelog: v0.6.5g — Axiom Validator

- **Date:** 2026-02-08
- **Version:** 0.6.5g
- **Author:** AI Assistant
- **Status:** Complete
- **Spec:** [LCS-DES-v0.6.5-KG-g](../../../specs/v0.6.x/v0.6.5/LCS-DES-v0.6.5-KG-g.md)

## Summary

Implements the Axiom Validator that bridges the v0.4.6h axiom evaluation system (`IAxiomStore` + `IAxiomEvaluator`) with the v0.6.5e Validation Orchestrator pipeline. Creates `IAxiomValidatorService`, `AxiomFindingCodes`, `AxiomMatcher`, and `AxiomValidatorService` components. The service implements `IValidator` for pipeline integration and exposes direct entity-level axiom validation methods.

## Changes by Module

### Lexichord.Abstractions

| File | Change |
|:---|:---|
| `AxiomFindingCodes.cs` | [NEW] Static class with 12 `const string` finding codes (`AXIOM_*` prefix) |
| `IAxiomValidatorService.cs` | [NEW] Extends `IValidator` with `ValidateEntityAsync`, `ValidateEntitiesAsync`, `GetApplicableAxiomsAsync` |

### Lexichord.Modules.Knowledge

| File | Change |
|:---|:---|
| `AxiomMatcher.cs` | [NEW] Static helper filtering axioms by `IsEnabled`, `TargetKind` (Entity), and `TargetType` (case-insensitive), ordered by severity ascending |
| `AxiomValidatorService.cs` | [NEW] `IValidator` bridge — extracts entities from context metadata, matches axioms via `AxiomMatcher`, evaluates via `IAxiomEvaluator`, converts `AxiomViolation` → `ValidationFinding` with severity and constraint-to-code mapping |
| `KnowledgeModule.cs` | [MODIFIED] Added DI registrations for `AxiomValidatorService` and `IAxiomValidatorService` |

### Lexichord.Tests.Unit

| File | Tests |
|:---|:---|
| `AxiomValidatorServiceTests.cs` | 21 tests — identity properties (4), pipeline integration (2), axiom matching (1), violation detection (1), severity mapping (2), multi-axiom aggregation (1), batch validation (1), applicable axiom query (1), constructor guard (1), constraint-to-code mapping (7 theory cases) |

## Verification Results

- **Build:** `Lexichord.Abstractions`, `Lexichord.Modules.Knowledge`, `Lexichord.Tests.Unit` — all 0 errors, 0 warnings
- **Tests:** 21 new tests, all passing

## Key Design Decisions

| Decision | Rationale |
|:---|:---|
| Fully qualified `Axiom` type references | Folder namespace `Validators.Axiom` collides with the `Axiom` record type; resolved via FQN |
| Test folder named `AxiomValidator/` | Avoids breaking existing `AxiomDataModelTests.cs` which uses bare `Axiom` type |
| `IAxiomEvaluator.Evaluate` (synchronous) | Reuses existing v0.4.6h evaluator API; `ValidateEntityAsync` wraps result in `Task.FromResult` |
| `LicenseTier.Teams` required | Matches `IAxiomStore.ValidateEntity` license gating |
