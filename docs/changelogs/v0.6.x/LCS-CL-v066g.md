# v0.6.6g — Post-Generation Validator

**Released:** 2026-02-10
**Feature:** Co-pilot Agent (CKVS Phase 3b)
**Spec:** [LCS-DES-v0.6.6-KG-g](../../specs/v0.6.x/v0.6.6/LCS-DES-v0.6.6-KG-g.md)

## Summary

Implements post-generation validation of LLM-generated content against the Knowledge Graph. The validator extracts claims from generated content, validates them, detects hallucinations (unknown entities, contradictory property values), computes a composite validation score, and optionally auto-fixes content. Returns a `PostValidationResult` with findings, hallucinations, suggested fixes, verified entities, and a user-facing message.

## New Files

### Abstractions (`Lexichord.Abstractions`)

| File | Description |
|------|-------------|
| `IPostGenerationValidator.cs` | Validator interface: `ValidateAsync(content, context, request)` and `ValidateAndFixAsync(content, context, request)` |
| `IHallucinationDetector.cs` | Detector interface: `DetectAsync(content, context)` |
| `PostValidationResult.cs` | Result record with `IsValid`, `Status`, `Findings`, `Hallucinations`, `SuggestedFixes`, `CorrectedContent`, `VerifiedEntities`, `ExtractedClaims`, `ValidationScore`, `UserMessage`, and `Valid()`/`Inconclusive()` factories |
| `PostValidationStatus.cs` | Enum: `Valid`, `ValidWithWarnings`, `Invalid`, `Inconclusive` |
| `HallucinationFinding.cs` | Finding record with `ClaimText`, `Location`, `Confidence`, `Type`, `SuggestedCorrection` |
| `HallucinationType.cs` | Enum: `UnknownEntity`, `ContradictoryValue`, `UnsupportedRelationship`, `UnverifiableFact` |
| `ValidationFix.cs` | Fix record with `Description`, `ReplaceSpan`, `ReplacementText`, `Confidence`, `CanAutoApply` |

### Implementations (`Lexichord.Modules.Knowledge`)

| File | Description |
|------|-------------|
| `PostGenerationValidator.cs` | Orchestrator: entity verification → claim extraction → claim validation → hallucination detection → score computation → status → fix generation → user message |
| `HallucinationDetector.cs` | Pattern-based detection: contradiction detection via regex, Levenshtein distance for closest-match suggestions |

## Tests

| File | Tests |
|------|-------|
| `PostGenerationValidatorTests.cs` | 12 tests: empty content, clean content, hallucinations, warnings, validation errors, claim extraction failure (inconclusive), hallucination detector failure (graceful degradation), entity verification, validate-and-fix, factory methods |
| `HallucinationDetectorTests.cs` | 9 tests: empty content, empty context, no contradictions, contradictory value detection, matching value, Levenshtein distance, closest match, no close match |

## Spec Deviations

| Spec | Actual | Reason |
|------|--------|--------|
| `CopilotRequest` | `AgentRequest` | `CopilotRequest` does not exist; `AgentRequest` (v0.6.6a) is the correct type |
| `IEntityLinkingService` | Direct name matching | Service not yet implemented (v0.5.5 planned); entity verification via case-insensitive `string.Contains` |
| `IEntityRecognizer` | Regex-based detection | Service not yet implemented; contradiction detection via regex patterns |
| `IValidationEngine.ValidateClaimsAsync()` | `ValidateDocumentAsync()` | `ValidateClaimsAsync` does not exist on `IValidationEngine` |
| `Lexichord.KnowledgeGraph` | `Lexichord.Modules.Knowledge` | Spec namespace does not exist in project structure |
| `ValidationFinding.SuggestedFix` (type `ValidationFix`) | `ValidationFinding.SuggestedFix` (string) | Existing `ValidationFinding` uses `string? SuggestedFix`, not a `ValidationFix` record; fixes are generated separately |
