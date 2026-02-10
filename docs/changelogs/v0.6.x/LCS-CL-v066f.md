# v0.6.6f — Pre-Generation Validator

**Released:** 2026-02-10
**Feature:** Co-pilot Agent (CKVS Phase 3b)
**Spec:** [LCS-DES-v0.6.6-KG-f](../../specs/v0.6.x/v0.6.6/LCS-DES-v0.6.6-KG-f.md)

## Summary

Implements pre-generation validation of knowledge context and user requests before LLM generation. The validator checks context consistency (duplicate entities, property conflicts, dangling relationships), request-context alignment, and axiom rule compliance. Blocking issues prevent generation; warnings are surfaced but allow it to proceed.

## New Files

### Abstractions (`Lexichord.Abstractions`)

| File | Description |
|------|-------------|
| `IPreGenerationValidator.cs` | Validator interface: `ValidateAsync(request, context)` and `CanProceedAsync(request, context)` |
| `IContextConsistencyChecker.cs` | Consistency checker interface: `CheckConsistency(context)` and `CheckRequestConsistency(request, context)` |
| `PreValidationResult.cs` | Aggregated result record with `CanProceed`, `Issues`, `BlockingIssues`, `Warnings`, `SuggestedModifications`, `UserMessage`, and `Pass()`/`Block()` factories |
| `ContextIssue.cs` | Issue record with `Code`, `Message`, `Severity`, optional `RelatedEntity`/`RelatedAxiom`/`Resolution` |
| `ContextIssueSeverity.cs` | Enum: `Error` (blocks), `Warning` (allows), `Info` (advisory) |
| `ContextIssueCodes.cs` | Static constants: `PREVAL_CONFLICTING_ENTITIES`, `PREVAL_MISSING_ENTITY`, `PREVAL_AXIOM_VIOLATION`, `PREVAL_STALE_CONTEXT`, `PREVAL_EMPTY_CONTEXT`, `PREVAL_REQUEST_CONFLICT`, `PREVAL_AMBIGUOUS_REQUEST`, `PREVAL_UNSUPPORTED_TYPE` |
| `ContextModification.cs` | Advisory modification record with `Type`, `Description`, `EntityToAdd`, `EntityIdToRemove` |
| `ContextModificationType.cs` | Enum: `AddEntity`, `RemoveEntity`, `UpdateEntity`, `RefreshContext` |

### Implementations (`Lexichord.Modules.Knowledge`)

| File | Description |
|------|-------------|
| `PreGenerationValidator.cs` | Orchestrator: empty context check → consistency → request alignment → axiom compliance → aggregate |
| `ContextConsistencyChecker.cs` | Structural checks: duplicate entities (case-insensitive), boolean property conflicts, dangling relationship endpoints, ambiguous requests |

## Tests

| File | Tests |
|------|-------|
| `PreGenerationValidatorTests.cs` | 12 tests: empty context, clean pass, issue aggregation, error blocking, axiom violation/satisfaction, graceful degradation, CanProceedAsync delegation, static factory methods |
| `ContextConsistencyCheckerTests.cs` | 10 tests: empty context, unique entities, duplicate detection, boolean property conflicts, non-boolean exclusion, dangling/valid relationships, empty request, valid request, null guards |

## Spec Deviations

| Spec | Actual | Reason |
|------|--------|--------|
| `CopilotRequest` | `AgentRequest` | `CopilotRequest` does not exist; `AgentRequest` (v0.6.6a) is the correct type |
| `AxiomSeverity.Must` | `AxiomSeverity.Error` | `Must` is not a valid enum value; `Error` blocks operations |
| `PropertyRule`/`PropertyOperator` | `AxiomRule`/`AxiomConstraintType` | Spec references non-existent types; adapted to actual axiom data model |
| `Lexichord.KnowledgeGraph` | `Lexichord.Modules.Knowledge` | Spec namespace does not exist in project structure |
