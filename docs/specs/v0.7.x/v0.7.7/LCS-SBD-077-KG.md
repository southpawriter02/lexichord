# LCS-SBD-077-KG: Scope Overview — Validation Workflows

## Document Control

| Field            | Value                                                        |
| :--------------- | :----------------------------------------------------------- |
| **Document ID**  | LCS-SBD-077-KG                                               |
| **Version**      | v0.7.7                                                       |
| **Codename**     | Validation Workflows (CKVS Phase 4d)                         |
| **Status**       | Draft                                                        |
| **Last Updated** | 2026-01-31                                                   |
| **Owner**        | Lead Architect                                               |
| **Depends On**   | v0.7.7 (Agent Workflows), v0.7.5-KG (Unified Validation), v0.7.6-KG (Sync Service) |

---

## 1. Executive Summary

### 1.1 The Vision

**v0.7.7-KG** delivers **Validation Workflows** — automated pipelines that integrate CKVS validation into the agent workflow system. Users can define workflows like:

- "On document save → Validate → Auto-fix style issues → Flag knowledge errors"
- "Before publish → Full validation → Block if errors → Notify on warnings"
- "Nightly → Validate all docs → Generate health report → Alert on regressions"

This completes the CKVS integration by embedding validation into automated processes.

### 1.2 Business Value

- **Automation:** Validation happens automatically, not manually.
- **Publication Gates:** Prevent invalid content from publishing.
- **Quality Metrics:** Track documentation health over time.
- **Team Workflows:** Consistent validation across team members.
- **CI/CD Integration:** Validation as part of build pipeline.

### 1.3 Success Criteria

1. Validation steps available in workflow designer.
2. Pre-built workflows for common scenarios.
3. Workflow triggers: on-save, on-publish, scheduled.
4. Conditional branching based on validation results.
5. Integration with notification systems.
6. Published doc errors reduced >70% via workflows.

---

## 2. Relationship to Existing v0.7.7

The existing v0.7.7 spec covers **Agent Workflows** for chaining agent operations. Validation Workflows:

- **New Step Types:** Validation, sync, and gating steps.
- **Shared Designer:** Same workflow designer UI.
- **Same Engine:** Uses workflow execution engine.

---

## 3. Key Deliverables

### 3.1 Sub-Parts

| Sub-Part | Title | Description | Est. Hours |
|:---------|:------|:------------|:-----------|
| v0.7.7e | Validation Step Types | Steps for unified validation | 4 |
| v0.7.7f | Gating Step Type | Block workflow on validation failure | 3 |
| v0.7.7g | Sync Step Type | Trigger doc-graph sync in workflow | 3 |
| v0.7.7h | Pre-built Workflows | Common validation workflow templates | 4 |
| v0.7.7i | Workflow Metrics | Track validation outcomes over time | 4 |
| v0.7.7j | CI/CD Integration | CLI for headless workflow execution | 5 |
| **Total** | | | **23 hours** |

### 3.2 Key Interfaces

```csharp
/// <summary>
/// Validation step for agent workflows.
/// </summary>
public class ValidationWorkflowStep : IWorkflowStep
{
    public string StepType => "Validation";

    /// <summary>Validation options.</summary>
    public UnifiedValidationOptions Options { get; init; }

    /// <summary>Action on validation failure.</summary>
    public ValidationFailureAction FailureAction { get; init; }

    /// <summary>Minimum severity to trigger failure.</summary>
    public UnifiedSeverity FailureSeverity { get; init; } = UnifiedSeverity.Error;

    public async Task<WorkflowStepResult> ExecuteAsync(
        WorkflowContext context,
        CancellationToken ct = default)
    {
        var validation = await _validationService.ValidateAsync(
            context.Document, Options, ct);

        context.SetVariable("validation_result", validation);

        var failures = validation.Issues
            .Count(i => i.Severity <= FailureSeverity);

        if (failures > 0)
        {
            return FailureAction switch
            {
                ValidationFailureAction.Halt => WorkflowStepResult.Halt(
                    $"Validation failed: {failures} issues"),
                ValidationFailureAction.Continue => WorkflowStepResult.Continue(),
                ValidationFailureAction.Branch => WorkflowStepResult.Branch("on_failure"),
                _ => throw new InvalidOperationException()
            };
        }

        return WorkflowStepResult.Continue();
    }
}

public enum ValidationFailureAction
{
    Halt,       // Stop workflow
    Continue,   // Continue anyway (log warning)
    Branch      // Take alternate path
}

/// <summary>
/// Sync step for triggering doc-graph sync.
/// </summary>
public class SyncWorkflowStep : IWorkflowStep
{
    public string StepType => "Sync";

    /// <summary>Sync direction.</summary>
    public SyncDirection Direction { get; init; } = SyncDirection.DocumentToGraph;

    /// <summary>Conflict resolution strategy.</summary>
    public ConflictResolutionStrategy ConflictStrategy { get; init; }

    public async Task<WorkflowStepResult> ExecuteAsync(
        WorkflowContext context,
        CancellationToken ct = default);
}

public enum SyncDirection
{
    DocumentToGraph,
    GraphToDocument,
    Bidirectional
}

/// <summary>
/// Gating step that blocks based on condition.
/// </summary>
public class GatingWorkflowStep : IWorkflowStep
{
    public string StepType => "Gate";

    /// <summary>Condition expression (e.g., "validation_result.ErrorCount == 0").</summary>
    public required string Condition { get; init; }

    /// <summary>Message shown when gate fails.</summary>
    public string? FailureMessage { get; init; }

    public async Task<WorkflowStepResult> ExecuteAsync(
        WorkflowContext context,
        CancellationToken ct = default);
}
```

### 3.3 Workflow Designer Integration

```
┌────────────────────────────────────────────────────────────────┐
│ Workflow: Pre-Publish Validation                               │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│ ┌─────────────┐     ┌─────────────┐     ┌─────────────┐       │
│ │   Trigger   │────▶│  Validate   │────▶│    Gate     │       │
│ │  On Publish │     │    Full     │     │  No Errors  │       │
│ └─────────────┘     └─────────────┘     └──────┬──────┘       │
│                                                │               │
│                           ┌────────────────────┼────────────┐ │
│                           │                    │            │ │
│                           ▼                    ▼            │ │
│                    ┌─────────────┐     ┌─────────────┐      │ │
│                    │   Notify    │     │   Publish   │      │ │
│                    │   Errors    │     │   Success   │      │ │
│                    └─────────────┘     └─────────────┘      │ │
│                                                              │ │
├──────────────────────────────────────────────────────────────┤ │
│ Step Types:                                                   │ │
│ [Validate] [Sync] [Gate] [Notify] [Publish] [Custom]         │ │
└────────────────────────────────────────────────────────────────┘
```

---

## 4. Pre-Built Workflows

### 4.1 On-Save Validation

```yaml
id: on-save-validation
name: "Validate on Save"
description: "Validates document and auto-fixes style issues"
trigger:
  type: document_saved

steps:
  - type: Validation
    id: validate
    options:
      include: [Style, Grammar, Knowledge]
    failure_action: Continue

  - type: AutoFix
    id: fix_style
    condition: "steps.validate.result.AutoFixableCount > 0"
    categories: [Style, Grammar]

  - type: Notify
    id: notify_errors
    condition: "steps.validate.result.ErrorCount > 0"
    channel: toast
    message: "Document has {{steps.validate.result.ErrorCount}} validation errors"
```

### 4.2 Pre-Publish Gate

```yaml
id: pre-publish-gate
name: "Publication Gate"
description: "Blocks publish if validation fails"
trigger:
  type: pre_publish

steps:
  - type: Sync
    id: sync_graph
    direction: DocumentToGraph

  - type: Validation
    id: full_validation
    options:
      include: [Style, Grammar, Knowledge]
      mode: Full
    failure_action: Branch

  - type: Gate
    id: error_gate
    condition: "steps.full_validation.result.ErrorCount == 0"
    on_failure:
      - type: Notify
        channel: modal
        message: "Cannot publish: {{steps.full_validation.result.ErrorCount}} errors found"
        actions:
          - label: "View Issues"
            action: open_issues_panel
          - label: "Cancel"
            action: abort

  - type: Publish
    id: do_publish
    condition: "steps.error_gate.passed"
```

### 4.3 Nightly Health Check

```yaml
id: nightly-health-check
name: "Nightly Documentation Health"
description: "Validates all docs and generates report"
trigger:
  type: scheduled
  cron: "0 2 * * *"  # 2 AM daily

steps:
  - type: ForEach
    id: validate_all
    collection: all_documents
    steps:
      - type: Validation
        options:
          include: [Style, Grammar, Knowledge]
          mode: Full

  - type: Aggregate
    id: summarize
    input: steps.validate_all.results
    output:
      total_docs: "count()"
      total_errors: "sum(ErrorCount)"
      total_warnings: "sum(WarningCount)"
      health_score: "(total_docs - error_docs) / total_docs * 100"

  - type: Report
    id: generate_report
    template: "health-report"
    data: steps.summarize.output
    output: "reports/health-{{date}}.md"

  - type: Notify
    id: alert_regressions
    condition: "steps.summarize.output.health_score < 90"
    channel: email
    recipients: [docs-team@company.com]
    message: "Documentation health dropped to {{health_score}}%"
```

---

## 5. CLI for CI/CD

```bash
# Run validation workflow in CI
lexichord workflow run pre-publish-gate --document ./docs/api-reference.md

# Output:
# ✓ Sync: 3 entities updated
# ✓ Validation: 0 errors, 2 warnings
# ✓ Gate: Passed
# ✓ Ready to publish

# Exit code: 0 (success) or 1 (gate failed)

# Run health check
lexichord workflow run nightly-health-check --workspace ./docs

# Output:
# Validating 47 documents...
# ✓ Complete: 45 healthy, 2 with issues
# Health score: 95.7%
# Report: reports/health-2026-01-31.md
```

---

## 6. Metrics Dashboard

```
┌────────────────────────────────────────────────────────────────┐
│ Documentation Health Dashboard                                 │
├────────────────────────────────────────────────────────────────┤
│                                                                │
│ Health Score: 94.2% ▲ (+2.1% from last week)                  │
│                                                                │
│ ┌──────────────────────────────────────────────────────────┐  │
│ │                   Health Trend (30 days)                 │  │
│ │  100% ─────────────────────────────────────────────────  │  │
│ │   95% ─────────╭───────────────────────────╮──────────  │  │
│ │   90% ─────────╯                           ╰──────────  │  │
│ │   85% ─────────────────────────────────────────────────  │  │
│ │        Jan 1  Jan 8  Jan 15  Jan 22  Jan 29             │  │
│ └──────────────────────────────────────────────────────────┘  │
│                                                                │
│ Issue Breakdown:                                               │
│ ├── Style: 23 warnings                                        │
│ ├── Grammar: 8 warnings                                       │
│ ├── Knowledge: 2 errors, 5 warnings                          │
│ └── Total: 2 errors, 36 warnings                             │
│                                                                │
│ Recent Workflow Runs:                                         │
│ ├── pre-publish-gate: 15 runs, 14 passed (93%)               │
│ ├── on-save-validation: 234 runs, auto-fixed 89 issues        │
│ └── nightly-health: 7 runs, 0 regressions                    │
│                                                                │
└────────────────────────────────────────────────────────────────┘
```

---

## 7. Dependencies

| Component | Source | Usage |
|:----------|:-------|:------|
| `IWorkflowEngine` | v0.7.7 | Workflow execution |
| `IUnifiedValidationService` | v0.7.5-KG | Validation step |
| `ISyncService` | v0.7.6-KG | Sync step |
| `INotificationService` | v0.1.x | Notifications |
| CLI Framework | v0.8.x | CI/CD integration |

---

## 8. License Gating

| Tier | Validation Workflows |
|:-----|:---------------------|
| Core | Not available |
| WriterPro | On-save validation only |
| Teams | All workflows + custom |
| Enterprise | Full + CI/CD + metrics |

---

## 9. Performance Targets

| Metric | Target | Measurement |
|:-------|:-------|:------------|
| Workflow trigger latency | <100ms | P95 timing |
| Single doc workflow | <5s | P95 timing |
| Full workspace validation | <5min | P95 timing |
| CI exit time | <30s | P95 timing |

---

## 10. Success Metrics

| Metric | Target | Rationale |
|:-------|:-------|:----------|
| Published errors | -70% | Validation catches issues pre-publish |
| Validation adoption | >80% | Teams enable workflows |
| Auto-fix rate | >50% | Issues fixed without manual work |
| Health score | >95% | Consistent quality across docs |

---

## 11. Risks & Mitigations

| Risk | Mitigation |
|:-----|:-----------|
| Workflow overhead slows publishing | Async validation, fast paths |
| Gate too strict, blocks valid content | Configurable thresholds |
| CI integration complexity | Pre-built GitHub Actions |
| Metric gaming | Multiple quality dimensions |

---

## 12. CKVS Integration Complete

With v0.7.7-KG, the CKVS integration is complete:

| Phase | Version | Component | Status |
|:------|:--------|:----------|:-------|
| 1 | v0.4.5-KG | Graph Foundation | ✓ |
| 1 | v0.4.6-KG | Axiom Store | ✓ |
| 1 | v0.4.7-KG | Entity Browser | ✓ |
| 2 | v0.5.5-KG | Entity Linking | ✓ |
| 2 | v0.5.6-KG | Claim Extraction | ✓ |
| 3 | v0.6.5-KG | Validation Engine | ✓ |
| 3 | v0.6.6-KG | Knowledge-Aware Co-pilot | ✓ |
| 4 | v0.7.2-KG | Graph Context Strategy | ✓ |
| 4 | v0.7.5-KG | Unified Validation | ✓ |
| 4 | v0.7.6-KG | Sync Service | ✓ |
| 4 | v0.7.7-KG | Validation Workflows | ✓ |

**Total CKVS Effort:** ~265 hours (~7 person-months)
**Target:** Publication errors reduced 70%+

---
