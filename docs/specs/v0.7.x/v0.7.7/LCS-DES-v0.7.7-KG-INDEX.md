# LCS-DES-077-KG-INDEX: Validation Workflows (CKVS Phase 4d)

## Document Control

| Field | Value |
| :--- | :--- |
| **Spec ID** | LCS-DES-077-KG-INDEX |
| **System Breakdown** | LCS-SBD-077-KG |
| **Version** | v0.7.7 |
| **Codename** | Validation Workflows Index (CKVS Phase 4d) |
| **Estimated Hours** | 23 (total) |
| **Status** | Draft |
| **Last Updated** | 2026-01-31 |

---

## 1. Overview

The **Validation Workflows (CKVS Phase 4d)** system provides a comprehensive framework for executing complex validation, gating, and synchronization workflows within the Lexichord platform. This index document links all sub-specifications and provides architectural context.

---

## 2. Specification Structure

### 2.1 Sub-Specification Map

| Spec ID | Title | Hours | Focus Area |
| :------ | :---- | :---: | :--------- |
| [LCS-DES-077-KG-e](./LCS-DES-077-KG-e.md) | Validation Step Types | 4 | Core validation step execution |
| [LCS-DES-077-KG-f](./LCS-DES-077-KG-f.md) | Gating Step Type | 3 | Conditional workflow blocking |
| [LCS-DES-077-KG-g](./LCS-DES-077-KG-g.md) | Sync Step Type | 3 | Document-graph synchronization |
| [LCS-DES-077-KG-h](./LCS-DES-077-KG-h.md) | Pre-built Workflows | 4 | Template workflows (3 primary) |
| [LCS-DES-077-KG-i](./LCS-DES-077-KG-i.md) | Workflow Metrics | 4 | Telemetry and health scoring |
| [LCS-DES-077-KG-j](./LCS-DES-077-KG-j.md) | CI/CD Integration | 5 | Headless workflow execution |

---

## 3. Architecture Overview

### 3.1 System Components

```
┌─────────────────────────────────────────────────────────────┐
│                    Validation Workflows (v0.7.7)             │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌─────────────────────────────────────────────────────┐   │
│  │              IWorkflowEngine (Core)                 │   │
│  │  - Orchestrates multi-step workflow execution      │   │
│  │  - Manages step ordering and dependencies         │   │
│  │  - Handles errors and branching                   │   │
│  └─────────────────────────────────────────────────────┘   │
│                          ▲                                   │
│                          │                                   │
│   ┌──────────────────────┼──────────────────────┐           │
│   │                      │                      │           │
│   v                      v                      v           │
│ ┌─────────────────┐ ┌──────────────────┐ ┌──────────────┐ │
│ │ Validation Step │ │ Gating Step      │ │ Sync Step    │ │
│ │ (LCS-DES-077-e) │ │ (LCS-DES-077-f)  │ │ (LCS-077-g)  │ │
│ │                 │ │                  │ │              │ │
│ │ Types:          │ │ - Evaluates      │ │ - Direction  │ │
│ │ - Schema        │ │   expressions    │ │ - Conflicts  │ │
│ │ - Consistency   │ │ - Blocks on fail │ │ - Logging    │ │
│ │ - References    │ │ - Branching      │ │              │ │
│ │ - Grammar       │ │                  │ │              │ │
│ │ - Custom        │ │                  │ │              │ │
│ └─────────────────┘ └──────────────────┘ └──────────────┘ │
│   │                                                         │
│   └──────────────────┬──────────────────┐                  │
│                      │                  │                  │
│                      v                  v                  │
│              ┌──────────────┐   ┌──────────────────┐      │
│              │ Metrics      │   │ Notifications    │      │
│              │ (077-i)      │   │ System           │      │
│              │              │   │                  │      │
│              │ - Tracking   │   │ - User alerts    │      │
│              │ - Health     │   │ - Admin reports  │      │
│              │ - Trending   │   │                  │      │
│              └──────────────┘   └──────────────────┘      │
│                                                               │
└─────────────────────────────────────────────────────────────┘
        ▲                              ▲
        │                              │
        │                              │
  ┌─────┴──────────────┐      ┌────────┴──────────┐
  │ Pre-built Workflows │      │ CI/CD Integration  │
  │ (LCS-DES-077-h)     │      │ (LCS-DES-077-j)    │
  │                     │      │                    │
  │ 1. On-Save          │      │ - CLI Command      │
  │ 2. Pre-Publish      │      │ - API Endpoint     │
  │ 3. Nightly Health   │      │ - Exit Codes       │
  └─────────────────────┘      │ - Output Formats   │
                               └────────────────────┘
```

### 3.2 Step Type Hierarchy

```
IWorkflowStep (Base Interface)
├── IValidationWorkflowStep (LCS-DES-077-KG-e)
│   ├── StepType: ValidationStepType
│   │   ├── Schema
│   │   ├── CrossReference
│   │   ├── Consistency
│   │   ├── Grammar
│   │   ├── KnowledgeGraphAlignment
│   │   └── Metadata
│   ├── FailureAction: ValidationFailureAction
│   │   ├── Halt
│   │   ├── Continue
│   │   ├── Branch
│   │   └── Notify
│   └── FailureSeverity: ValidationFailureSeverity
│       ├── Info
│       ├── Warning
│       ├── Error
│       └── Critical
│
├── IGatingWorkflowStep (LCS-DES-077-KG-f)
│   ├── ConditionExpression
│   ├── FailureMessage
│   ├── BranchPath
│   └── RequireAll
│
└── ISyncWorkflowStep (LCS-DES-077-KG-g)
    ├── Direction: SyncDirection
    │   ├── DocumentToGraph
    │   ├── GraphToDocument
    │   └── Bidirectional
    ├── ConflictStrategy: ConflictStrategy
    │   ├── PreferDocument
    │   ├── PreferGraph
    │   ├── PreferNewer
    │   ├── Merge
    │   ├── FailOnConflict
    │   └── Manual
    └── SkipIfValidationFailed
```

### 3.3 Data Flow

```
[Document Event]
     │
     ├─ on-save ─────────────────┐
     ├─ pre-publish ─────────────┼─> [Load Workflow]
     ├─ scheduled-nightly ───────┤
     ├─ manual ──────────────────┤
     └─ ci-trigger ──────────────┘
                    │
                    v
           [WorkflowRegistry]
                    │
        ┌───────────┴──────────────┐
        │                          │
        v                          v
    [Load Document]         [Load Steps]
        │                          │
        v                          v
    [Document]             [Step Configuration]
        │                          │
        └───────────┬──────────────┘
                    │
                    v
            [WorkflowContext]
                    │
                    v
        [IWorkflowEngine.ExecuteAsync]
                    │
        ┌───────────┼───────────┐
        │           │           │
        v           v           v
    [Step 1]    [Step 2]    [Step N]
   Validate    Gate/Sync   Notify
        │           │           │
        └───────────┼───────────┘
                    │
                    v
        [Collect Step Results]
                    │
        ┌───────────┼──────────────┐
        │           │              │
        v           v              v
    [Metrics]  [Results]   [Notifications]
                    │
                    v
            [Return Result]
```

---

## 4. Interface Dependencies

### 4.1 Core Dependencies

```csharp
// From v0.7.7 Workflow Engine
IWorkflowEngine ExecuteAsync(WorkflowContext ctx)
IWorkflowStep Base interface for all steps

// From v0.7.5-KG Unified Validation
IUnifiedValidationService
├── ValidateAsync(ValidationRequest)
├── ValidateSyncAsync(ValidationRequest)
└── GetRulesForTypeAsync(ValidationStepType)

// From v0.7.6-KG Sync Service
ISyncService
├── SyncAsync(SyncRequest)
└── GetSyncHistoryAsync(DocumentId)

// From v0.1.x Notification
INotificationService
└── NotifyAsync(Notification)

// From v0.8.x CLI Framework
ICommand
└── ExecuteAsync()
```

### 4.2 Data Flow Between Steps

```
[Validation Step]
        │
        ├─ Returns: ValidationStepResult
        │   ├─ IsValid
        │   ├─ Errors: IReadOnlyList<ValidationError>
        │   ├─ Warnings: IReadOnlyList<ValidationWarning>
        │   └─ ItemsWithIssues
        │
        v
[Gating Step] (optional)
        │
        ├─ Input: Previous ValidationStepResult
        │ ├─ Evaluates: validation_count(error), etc.
        │ └─ Decision: Pass/Block/Branch
        │
        v
[Sync Step] (optional)
        │
        └─ Input: Document + ValidationContext
          ├─ Syncs: DocumentToGraph or GraphToDocument
          └─ Returns: SyncStepResult
                ├─ ItemsSynced
                ├─ ConflictsDetected
                └─ Changes: IReadOnlyList<SyncChange>
```

---

## 5. Workflow Template Architecture

### 5.1 Pre-built Workflows

Three primary workflows are defined as YAML templates:

#### On-Save Validation (LCS-DES-077-KG-h)

**Purpose:** Real-time feedback during editing

**Steps:**
1. Schema Validation (5s timeout, halt on error)
2. Consistency Check (10s timeout, continue)
3. Reference Validation (5s timeout, continue)
4. Notify User (with summary)

**License:** WriterPro+

---

#### Pre-Publish Gate (LCS-DES-077-KG-h)

**Purpose:** Quality gate before publication

**Steps:**
1. Schema Validation (full, halt)
2. Grammar Check (15s, continue)
3. Consistency Check (15s, continue)
4. Knowledge Graph Alignment (20s, continue)
5. Reference Validation (halt)
6. Publication Gate (expression: `validation_count(error) == 0`)
7. Sync to Graph (DocumentToGraph)
8. Notify Stakeholders

**License:** Teams+

---

#### Nightly Health Check (LCS-DES-077-KG-h)

**Purpose:** Workspace-wide health assessment

**Steps:**
1. Workspace Scan (60s, include drafts)
2. Batch Schema Validation (120s, async)
3. Batch Consistency Check (120s, async)
4. Batch Reference Validation (120s, async)
5. Knowledge Graph Health (120s, async)
6. Generate Health Report (PDF)
7. Notify Admins

**License:** Teams+

---

### 5.2 Workflow Registry

```csharp
// Access workflows through registry
IWorkflowRegistry registry;

// Get pre-built workflow
var workflow = await registry.GetWorkflowAsync("pre-publish-gate");

// List all available workflows
var all = await registry.ListWorkflowsAsync();

// Register custom workflow
await registry.RegisterWorkflowAsync(customWorkflow);
```

---

## 6. Metrics and Observability (LCS-DES-077-KG-i)

### 6.1 Metrics Collection Points

```
Workflow Execution
├─ WorkflowExecutionMetrics
│  ├─ ExecutionTime (ms)
│  ├─ TotalErrors / TotalWarnings
│  ├─ GatesBlocked / GatesPassed
│  ├─ ConflictsDetected / Resolved
│  └─ Status: Success|PartialSuccess|Failed|Cancelled|TimedOut
│
├─ ValidationStepMetrics (per step)
│  ├─ StepType (schema, consistency, etc.)
│  ├─ Passed (bool)
│  ├─ ExecutionTimeMs
│  ├─ ErrorCount / WarningCount
│  └─ ItemsValidated / ItemsWithIssues
│
├─ GatingDecisionMetrics
│  ├─ GateId
│  ├─ Passed (bool)
│  ├─ ConflictExpression
│  └─ Decision rationale
│
└─ SyncOperationMetrics
   ├─ Direction (D2G|G2D|Bi)
   ├─ ItemsSynced
   ├─ ConflictsDetected / Resolved
   └─ Changes tracked
```

### 6.2 Health Score Calculation

```
Workspace Health Score = Base (Pass Rate) + Adjustment (Errors)
├─ Pass Rate: Percentage of documents passing validation
│  └─ 0-80 points
├─ Error Freedom: Inverse of error count
│  └─ 0-20 points
└─ Total: 0-100

Document Health Score = Base (Success) - ErrorPenalty - WarningPenalty
├─ Success: 80 points
├─ Failure: 30 points
├─ Error Penalty: -5 per error (max -30)
├─ Warning Penalty: -2 per warning (max -20)
└─ Total: 0-100

Trend: Improving | Stable | Declining
└─ Based on period-over-period comparison
```

---

## 7. CI/CD Integration (LCS-DES-077-KG-j)

### 7.1 CLI Command

```bash
# Basic usage
lexichord validate <workflow-id> \
  --workspace <id-or-key> \
  --document <id-or-path> \
  --timeout 300

# With CI metadata
lexichord validate pre-publish-gate \
  --workspace $WORKSPACE_KEY \
  --document docs/api.md \
  --ci-system github \
  --build-id $GITHUB_RUN_ID \
  --commit $GITHUB_SHA \
  --branch $GITHUB_REF \
  --output sarif

# With options
lexichord validate on-save-validation \
  --workspace $WORKSPACE_KEY \
  --document api.md \
  --fail-on-warnings \
  --max-warnings 5 \
  --output junit \
  --json-output results.json
```

### 7.2 Exit Codes

```
0   = Success
1   = Validation failed (errors/warnings)
2   = Invalid input
3   = Execution error
124 = Timeout
127 = Fatal error
```

### 7.3 Output Formats

- `json`: Structured JSON results
- `junit`: JUnit XML (for CI system integration)
- `sarif`: SARIF JSON (GitHub code scanning)
- `markdown`: Markdown summary
- `html`: HTML report

### 7.4 GitHub Actions Example

```yaml
- name: Validate Documentation
  run: |
    lexichord validate pre-publish-gate \
      --workspace ${{ secrets.WORKSPACE }} \
      --document ./docs/api.md \
      --output sarif \
      --json-output results.json

- name: Upload Results
  uses: github/codeql-action/upload-sarif@v1
  with:
    sarif_file: results.json
```

---

## 8. License Gating Summary

| Tier | On-Save | Pre-Publish | Nightly | CLI | Custom |
| :--- | :---: | :---: | :---: | :---: | :---: |
| Core | ❌ | ❌ | ❌ | ❌ | ❌ |
| WriterPro | ✅ | ❌ | ❌ | ❌ | ❌ |
| Teams | ✅ | ✅ | ✅ | ✅ | ❌ |
| Enterprise | ✅ | ✅ | ✅ | ✅ | ✅ |

---

## 9. Error Handling Strategy

### 9.1 Step-Level Errors

```
Validation Step Error
├─ Configuration Error → Return validation errors
├─ Service Unavailable → Log, return failed result
├─ Timeout → Cancel, return timeout result
├─ Cancellation → Propagate OperationCanceledException
└─ Unexpected Error → Log, return error result

Gating Step Error
├─ Expression Parse Error → Return evaluation error
├─ Unknown Expression → Return false
├─ Missing Properties → Return false
└─ Timeout → Cancel, return timeout error

Sync Step Error
├─ Service Unavailable → Return failure
├─ Conflicts Unresolvable → Collect unresolved
├─ Timeout → Cancel, return timeout
└─ Sync Error → Return failure with message
```

### 9.2 Workflow-Level Recovery

```
[Workflow Execution]
     │
     └─> [Step Fails]
              │
              ├─ FailureAction: Halt → Stop immediately
              ├─ FailureAction: Continue → Log, continue
              ├─ FailureAction: Branch → Take alternate path
              └─ FailureAction: Notify → Log, notify, continue
```

---

## 10. Performance Targets

| Component | Target | Rationale |
| :--------- | :------ | :--------- |
| Validation Step | < 30s | User feedback in on-save |
| Gating Evaluation | < 10s | Lightweight expression eval |
| Sync Operation | < 60s | Graph operations may be slow |
| Full Pre-Publish Workflow | < 5min | Quality gate before publication |
| Nightly Batch Validation | < 2 hours | Large workspace scans |
| Health Score Calculation | < 500ms | Dashboard responsiveness |
| CLI Startup | < 1s | Developer experience |

---

## 11. Integration Points

### 11.1 With IWorkflowEngine (v0.7.7)

```csharp
// Register step handlers with workflow engine
engine.RegisterStepHandler(typeof(ValidationWorkflowStep));
engine.RegisterStepHandler(typeof(GatingWorkflowStep));
engine.RegisterStepHandler(typeof(SyncWorkflowStep));

// Execute workflow
var result = await engine.ExecuteAsync(context);
```

### 11.2 With IUnifiedValidationService (v0.7.5-KG)

```csharp
// Validation step uses validation service
var result = await validationService.ValidateAsync(request);

// Step collects validation errors/warnings
var errors = result.Errors;
var warnings = result.Warnings;
```

### 11.3 With ISyncService (v0.7.6-KG)

```csharp
// Sync step coordinates synchronization
var syncResult = await syncService.SyncAsync(syncRequest);

// Handles conflicts per strategy
var unresolved = syncResult.UnresolvedConflicts;
```

### 11.4 With INotificationService (v0.1.x)

```csharp
// Steps can trigger notifications
await notificationService.NotifyAsync(notification);

// Gate blocks can notify users
// Metrics aggregation notifies admins
```

---

## 12. Development Checklist

### 12.1 Implementation Order

- [x] LCS-DES-077-KG-e: Validation Step Types
  - Core validation step interface and implementation
  - Step type enumeration
  - Failure action handling
  - Integration with validation service

- [x] LCS-DES-077-KG-f: Gating Step Type
  - Condition expression parser
  - Evaluator for expressions
  - Branching logic
  - Gate blocking implementation

- [x] LCS-DES-077-KG-g: Sync Step Type
  - Sync direction and conflict strategy enums
  - Integration with sync service
  - Conflict tracking and resolution

- [x] LCS-DES-077-KG-h: Pre-built Workflows
  - YAML workflow definitions
  - Workflow registry implementation
  - Loader for embedded resources
  - License enforcement

- [x] LCS-DES-077-KG-i: Workflow Metrics
  - Metrics collector interface
  - Health score calculator
  - Time-series data storage
  - Aggregation and reporting

- [x] LCS-DES-077-KG-j: CI/CD Integration
  - CLI command implementation
  - CI pipeline service
  - Multiple output format support
  - Exit code mapping

### 12.2 Testing Matrix

| Component | Unit | Integration | E2E |
| :--------- | :---: | :---: | :---: |
| Validation Step | ✓ | ✓ | ✓ |
| Gating Step | ✓ | ✓ | ✓ |
| Sync Step | ✓ | ✓ | ✓ |
| Workflows | - | ✓ | ✓ |
| Metrics | ✓ | ✓ | - |
| CLI/CI | ✓ | ✓ | ✓ |

---

## 13. Deployment Considerations

### 13.1 Version Compatibility

```
v0.7.7 Requires:
├─ IWorkflowEngine (v0.7.7)
├─ IUnifiedValidationService (v0.7.5-KG or higher)
├─ ISyncService (v0.7.6-KG or higher)
├─ INotificationService (v0.1.x)
├─ CLI Framework (v0.8.x)
└─ Database (for metrics storage)
```

### 13.2 Database Schema

```sql
-- Metrics tables
CREATE TABLE WorkflowExecutionMetrics (
    ExecutionId UNIQUEIDENTIFIER PRIMARY KEY,
    WorkflowId NVARCHAR(255),
    WorkspaceId UNIQUEIDENTIFIER,
    DocumentId UNIQUEIDENTIFIER,
    Success BIT,
    ExecutedAt DATETIME2,
    TotalExecutionTimeMs INT,
    TotalErrors INT,
    TotalWarnings INT,
    ...
);

CREATE TABLE ValidationStepMetrics (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    ExecutionId UNIQUEIDENTIFIER FOREIGN KEY,
    StepId NVARCHAR(255),
    Passed BIT,
    ExecutionTimeMs INT,
    ErrorCount INT,
    WarningCount INT,
    ...
);

-- Indexes for performance
CREATE INDEX idx_workspace_date ON WorkflowExecutionMetrics
    (WorkspaceId, ExecutedAt DESC);
CREATE INDEX idx_document ON WorkflowExecutionMetrics
    (DocumentId, ExecutedAt DESC);
```

---

## 14. Documentation References

### 14.1 Related Specifications

- **LCS-DES-071-KG**: Knowledge Graph Core Architecture
- **LCS-DES-075-KG**: Unified Validation Service (v0.7.5-KG)
- **LCS-DES-076-KG**: Sync Service (v0.7.6-KG)
- **LCS-DES-077-KG-a through d**: Previous CKVS phases

### 14.2 API Documentation

Each specification includes:
- C# interface definitions
- Data type records
- Implementation classes
- Factory patterns
- Example usage

---

## 15. Changelog

| Version | Date | Author | Changes |
| :------ | :--- | :----- | :------ |
| 1.0 | 2026-01-31 | Lead Architect | Index creation |
| 1.0 | 2026-01-31 | Lead Architect | All 6 sub-specs created |

---

## 16. Quick Reference

### File Locations

```
/specs/v0.7.x/v0.7.7/
├── LCS-DES-077-KG-e.md          (Validation Steps)
├── LCS-DES-077-KG-f.md          (Gating)
├── LCS-DES-077-KG-g.md          (Sync)
├── LCS-DES-077-KG-h.md          (Pre-built Workflows)
├── LCS-DES-077-KG-i.md          (Metrics)
├── LCS-DES-077-KG-j.md          (CI/CD)
└── LCS-DES-077-KG-INDEX.md      (This file)

/src/Lexichord.Workflows/
├── Validation/
│   ├── Steps/
│   │   ├── IValidationWorkflowStep.cs
│   │   ├── ValidationWorkflowStep.cs
│   │   ├── IGatingWorkflowStep.cs
│   │   ├── GatingWorkflowStep.cs
│   │   ├── ISyncWorkflowStep.cs
│   │   └── SyncWorkflowStep.cs
│   ├── Templates/
│   │   ├── PrebuiltWorkflows.cs
│   │   └── WorkflowRegistry.cs
│   ├── Metrics/
│   │   ├── WorkflowMetricsCollector.cs
│   │   └── HealthScoreCalculator.cs
│   └── CI/
│       ├── CIPipelineService.cs
│       └── WorkflowExecutionRequest.cs

/src/Lexichord.Cli/
└── Commands/
    ├── ValidateCommand.cs
    └── WorkflowStatusCommand.cs

/src/Resources/Workflows/
├── on-save-validation.yaml
├── pre-publish-gate.yaml
└── nightly-health-check.yaml
```

### Key Interfaces

- `IValidationWorkflowStep` → Validation execution
- `IGatingWorkflowStep` → Conditional blocking
- `ISyncWorkflowStep` → Document-graph sync
- `IWorkflowRegistry` → Workflow discovery
- `IWorkflowMetricsCollector` → Telemetry
- `ICIPipelineService` → Headless execution

### Configuration Examples

```yaml
# Workflow with validation + gate + sync
id: custom-publish-flow
name: "Custom Publish Flow"
steps:
  - id: validate
    type: validation
    step_type: schema
    failure_action: halt
  - id: gate
    type: gating
    condition_expression: "validation_count(error) == 0"
    failure_action: halt
  - id: sync
    type: sync
    direction: document_to_graph
    skip_if_validation_failed: true
```

---

This index provides complete navigation and architectural context for the Validation Workflows (CKVS Phase 4d) system. Each sub-specification stands alone but integrates through the workflow engine, metrics collector, and CLI interface.
