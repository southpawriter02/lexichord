# v0.7.7i — Workflow Metrics (Validation Workflows — CKVS Phase 4d)

**Released:** 2026-02-26
**Specification:** LCS-DES-077-KG-i
**Test Count:** 28 (all passing)

---

## Overview

This sub-part delivers the metrics collection, health scoring, and reporting infrastructure for validation workflows. It tracks execution outcomes, step-level results, gating decisions, and sync operations, then aggregates them into workspace and document health scores.

## New Types

### Interfaces (namespace: `Lexichord.Modules.Agents.Workflows.Validation.Metrics`)

| Interface                   | Methods | Purpose                                                    |
| --------------------------- | ------- | ---------------------------------------------------------- |
| `IWorkflowMetricsCollector` | 7       | Records telemetry, queries metrics, computes health scores |
| `IMetricsStore`             | 10      | Persistence layer for metrics records                      |
| `IHealthScoreCalculator`    | 2       | Workspace and document health score formulas               |

### Implementations

| Class                      | Implements                  | Description                                                                        |
| -------------------------- | --------------------------- | ---------------------------------------------------------------------------------- |
| `WorkflowMetricsCollector` | `IWorkflowMetricsCollector` | Fire-and-forget recording, background health recalculation, statistics aggregation |
| `HealthScoreCalculator`    | `IHealthScoreCalculator`    | Workspace: PassRate×80 + ErrorFreedom; Document: BaseScore - Penalties             |
| `InMemoryMetricsStore`     | `IMetricsStore`             | ConcurrentBag-based stub with LINQ filtering                                       |

### Records & Enums (MetricsDataTypes.cs)

| Type                       | Kind   | Properties/Values                                               |
| -------------------------- | ------ | --------------------------------------------------------------- |
| `WorkflowExecutionMetrics` | Record | 18 properties                                                   |
| `ValidationStepMetrics`    | Record | 13 properties                                                   |
| `GatingDecisionMetrics`    | Record | 7 properties                                                    |
| `SyncOperationMetrics`     | Record | 8 properties                                                    |
| `WorkspaceHealthScore`     | Record | 11 properties                                                   |
| `DocumentHealthScore`      | Record | 12 properties                                                   |
| `MetricsQuery`             | Record | 9 properties                                                    |
| `MetricsReport`            | Record | 7 properties                                                    |
| `MetricsStatistics`        | Record | 10 properties                                                   |
| `ErrorTypeFrequency`       | Record | 3 properties                                                    |
| `ValidationCategoryScore`  | Record | 3 properties                                                    |
| `HealthRecommendation`     | Record | 4 properties                                                    |
| `MetricsExecutionStatus`   | Enum   | 5 values (Success, PartialSuccess, Failed, Cancelled, TimedOut) |
| `HealthTrend`              | Enum   | 3 values (Improving, Stable, Declining)                         |
| `DocumentValidationState`  | Enum   | 5 values                                                        |
| `MetricsAggregation`       | Enum   | 5 values (Raw, Hourly, Daily, Weekly, Monthly)                  |
| `HealthRecommendationType` | Enum   | 6 values                                                        |

## Design Decisions

### MetricsExecutionStatus vs WorkflowExecutionStatus

The spec defines a `WorkflowExecutionStatus` enum, but v0.7.7b already has one with different values (Pending/Running/Completed vs Success/PartialSuccess/TimedOut). Created `MetricsExecutionStatus` to avoid collision while preserving the spec's outcome-focused semantics.

### ValidationWorkflowTrigger Reuse

The spec references `WorkflowTrigger` which doesn't exist. Reused `ValidationWorkflowTrigger` from v0.7.7h since both operate in the same validation workflow domain.

## Health Score Formulas

### Workspace (§4.2)

```
baseScore = PassRate × 80         // 0–80 points
errorFreedom = 20 - (totalErrors / 10)  // 0–20 points, clamped
healthScore = clamp(baseScore + errorFreedom, 0, 100)
```

### Document (§4.2)

```
baseScore = success ? 80 : 30
errorPenalty = min(30, errors × 5)
warningPenalty = min(20, warnings × 2)
healthScore = clamp(baseScore - errorPenalty - warningPenalty, 0, 100)
```

## Unit Tests

| Test Class                      | Count | Description                                                                        |
| ------------------------------- | ----- | ---------------------------------------------------------------------------------- |
| `HealthScoreCalculatorTests`    | 9     | Score formulas, edge cases, recommendations, validation states                     |
| `WorkflowMetricsCollectorTests` | 9     | Recording delegation, fire-and-forget errors, report generation, health delegation |
| `WorkflowMetricsDataTypeTests`  | 10    | Enum completeness (5 enums), record defaults (5 records)                           |

## Files Changed

### New Files (10)

- `src/Lexichord.Modules.Agents/Workflows/Validation/Metrics/MetricsDataTypes.cs`
- `src/Lexichord.Modules.Agents/Workflows/Validation/Metrics/IWorkflowMetricsCollector.cs`
- `src/Lexichord.Modules.Agents/Workflows/Validation/Metrics/IMetricsStore.cs`
- `src/Lexichord.Modules.Agents/Workflows/Validation/Metrics/IHealthScoreCalculator.cs`
- `src/Lexichord.Modules.Agents/Workflows/Validation/Metrics/WorkflowMetricsCollector.cs`
- `src/Lexichord.Modules.Agents/Workflows/Validation/Metrics/HealthScoreCalculator.cs`
- `src/Lexichord.Modules.Agents/Workflows/Validation/Metrics/InMemoryMetricsStore.cs`
- `tests/Lexichord.Tests.Unit/Modules/Agents/Workflows/HealthScoreCalculatorTests.cs`
- `tests/Lexichord.Tests.Unit/Modules/Agents/Workflows/WorkflowMetricsCollectorTests.cs`
- `tests/Lexichord.Tests.Unit/Modules/Agents/Workflows/WorkflowMetricsDataTypeTests.cs`

### Modified Files (1)

- `docs/changelogs/CHANGELOG.md` — Added v0.7.7i entry
