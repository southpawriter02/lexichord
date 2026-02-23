# LCS-CL-v0.7.7e — Validation Step Types

**Version:** v0.7.7e  
**Feature ID:** AGT-077e  
**Date:** 2026-02-23  
**Module:** `Lexichord.Modules.Agents`  

---

## Summary

Introduces unified validation step types for the Validation Workflows system (CKVS Phase 4d). Defines the `IValidationWorkflowStep` interface, `ValidationWorkflowStep` implementation, and `ValidationWorkflowStepFactory` for consistent execution of validation rules within the `IWorkflowEngine`. Includes step type classification (7 types), failure action handling (Halt, Continue, Branch, Notify), severity tracking (Info, Warning, Error, Critical), timeout enforcement, and async support.

---

## New Files

| File | Type | Description |
|:-----|:-----|:------------|
| `Workflows/Validation/ValidationStepType.cs` | Enum | 7 step types: Schema, CrossReference, Consistency, Custom, Grammar, KnowledgeGraphAlignment, Metadata |
| `Workflows/Validation/ValidationFailureAction.cs` | Enum | 4 failure actions: Halt, Continue, Branch, Notify |
| `Workflows/Validation/ValidationFailureSeverity.cs` | Enum | 4 severity levels: Info, Warning, Error, Critical |
| `Workflows/Validation/ValidationTrigger.cs` | Enum | 6 trigger types: Manual, OnSave, PrePublish, ScheduledNightly, PreWorkflow, Custom |
| `Workflows/Validation/ValidationRule.cs` | Record | 8-property rule definition with type, severity, and config |
| `Workflows/Validation/ValidationStepResult.cs` | Record | 10-property step result with errors, warnings, metrics |
| `Workflows/Validation/ValidationWorkflowStepOptions.cs` | Record | 7-property factory configuration with defaults |
| `Workflows/Validation/ValidationWorkflowContext.cs` | Record | 10-property execution context with document and trigger info |
| `Workflows/Validation/IWorkflowStep.cs` | Interface | Base step interface: identity, ordering, timeout, execute, validate |
| `Workflows/Validation/WorkflowStepResult.cs` | Record | Base step result: StepId, Success, Message, Data |
| `Workflows/Validation/IValidationWorkflowStep.cs` | Interface | Extended step interface: StepType, Options, FailureAction, FailureSeverity, IsAsync |
| `Workflows/Validation/ValidationWorkflowStep.cs` | Implementation | Full `IValidationWorkflowStep` with timeout, logging, configuration validation |
| `Workflows/Validation/ValidationWorkflowStepFactory.cs` | Factory | Creates steps with injected `IUnifiedValidationService` and `ILoggerFactory` |

## Modified Files

| File | Change |
|:-----|:-------|
| `AgentsModule.cs` | DI: `ValidationWorkflowStepFactory` (Singleton) |

---

## DI Registrations

| Registration | Lifetime |
|:-------------|:---------|
| `ValidationWorkflowStepFactory` | Singleton |

---

## Key Implementation Details

### Step Type Classification (spec §3.1)
- 7 types covering schema, cross-reference, consistency, custom, grammar, KG alignment, and metadata validation
- License gated: WriterPro (Schema, Grammar), Teams (all types), Enterprise (unlimited rules)

### Failure Handling (spec §3.2–§3.3)
- Halt: stops workflow execution immediately
- Continue: records failure but proceeds to next step
- Branch/Notify: alternate path or alert-and-continue

### Timeout & Cancellation (spec §4.1)
- Linked `CancellationTokenSource` with configurable `TimeoutMs` (default 30s)
- Graceful cancellation returns `WorkflowStepResult` with `Success=false`

### Adaptations from Spec
- Placed in `Lexichord.Modules.Agents/Workflows/Validation/` (spec references `Lexichord.Workflows/`)
- `ValidationWorkflowContext` replaces spec's `Document`/`ValidationContext` pair for self-contained context
- Default rule generation when no explicit rules configured (future phases wire to service)

---

## Test Results

| Suite | Passed | Failed | Skipped |
|:------|-------:|-------:|--------:|
| v0.7.7e tests | 10 | 0 | 0 |
| Full regression | 11,108 | 0 | 33 |

**Unit Tests (10):**
- Factory: CreateStep_ValidConfiguration, CreateStep_NoOptions_UsesDefaults
- Configuration: ValidateConfiguration_InvalidSettings
- Execution: ExecuteValidation_Passes, FailsWithHalt, FailsWithContinue, Timeout, Cancelled
- Rules: GetValidationRules_ReturnsDefaultRule
- Lifecycle: DisabledStep_SkipsExecution
