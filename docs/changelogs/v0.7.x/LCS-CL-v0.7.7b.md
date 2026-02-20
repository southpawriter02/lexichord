# LCS-CL-v0.7.7b — Workflow Engine

| Field        | Value                                                                         |
|:-------------|:------------------------------------------------------------------------------|
| **Version**  | v0.7.7b                                                                       |
| **Codename** | The Execution Engine                                                          |
| **Date**     | 2026-02-19                                                                    |
| **Swimlane** | Agents                                                                        |
| **Spec**     | [LCS-DES-v0.7.7b](../../specs/v0.7.x/v0.7.7/LCS-DES-v0.7.7b.md)            |

---

## Summary

Introduces the **Workflow Engine** — the runtime that executes workflow definitions
created by the Workflow Designer (v0.7.7a). Supports sequential agent execution with
conditional logic, data passing between steps via shared variables, cancellation
support, per-step timeouts, aggregated usage metrics, and MediatR event publishing
for real-time UI progress updates.

---

## Added

### Interfaces
- **`IWorkflowEngine`** — Core engine contract with `ExecuteAsync`, `ExecuteStreamingAsync`, `ValidateExecution`, and `EstimateTokens`
- **`IExpressionEvaluator`** — Sandboxed expression evaluation for step conditions with `Evaluate<T>`, `IsValid`, and `GetReferencedVariables`

### Records & Enums
- **`WorkflowExecutionContext`** — Execution context (document path, selection, initial variables, options)
- **`WorkflowExecutionOptions`** — Execution behavior (StopOnFirstFailure, StepTimeout, MaxRetries, DryRun)
- **`WorkflowExecutionResult`** — Full execution result (success, status, step results, final output, usage)
- **`WorkflowExecutionStatus`** — Pending, Running, Completed, Failed, Cancelled, PartialSuccess
- **`WorkflowStepExecutionResult`** — Per-step result (output, duration, usage, error)
- **`WorkflowStepStatus`** — Pending, Running, Completed, Failed, Skipped, Cancelled
- **`WorkflowUsageMetrics`** — Aggregated token counts with `Empty`, `Add`, `WithSkipped` helpers
- **`AgentUsageMetrics`** — Per-step token counts
- **`WorkflowExecutionValidation`** — Pre-flight validation result
- **`WorkflowTokenEstimate` / `StepTokenEstimate`** — Token usage estimation records

### Implementations
- **`WorkflowEngine`** — Full engine implementation with sequential step execution, condition evaluation (Always, PreviousSuccess, PreviousFailed, Expression), output mapping, failure handling, cancellation, and metrics aggregation
- **`ExpressionEvaluator`** — DynamicExpresso-based evaluator with built-in functions (`count`, `any`, `isEmpty`, `contains`)
- **`ExpressionEvaluationException`** — Custom exception for evaluation failures

### Events (MediatR)
- **`WorkflowStartedEvent`** — Published when execution begins
- **`WorkflowStepStartedEvent`** — Published before each step
- **`WorkflowStepCompletedEvent`** — Published after each step
- **`WorkflowCompletedEvent`** — Published on execution completion
- **`WorkflowCancelledEvent`** — Published on cancellation

### Dependencies
- **DynamicExpresso.Core 2.16.1** — Sandboxed expression evaluation for workflow step conditions

---

## Verification

| Metric | Result |
|:-------|:-------|
| v0.7.7b Tests | 13 passed |
| Full Regression | 11,061 passed, 33 skipped, 0 failed |
| Build Warnings | 0 new (1 pre-existing Avalonia warning) |
