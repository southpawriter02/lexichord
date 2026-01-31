# LCS-DES-v0.10.2-KG-f: Design Specification — Inference UI

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `KG-102-f` | Inference Engine sub-part f |
| **Feature Name** | `Inference UI` | Configure rules and view derived facts |
| **Target Version** | `v0.10.2f` | Sixth sub-part of v0.10.2-KG |
| **Module Scope** | `Lexichord.Modules.CKVS.Inference` | Inference module |
| **Swimlane** | `Knowledge Graph` | Knowledge Graph vertical |
| **License Tier** | `Teams` / `Enterprise` | Custom rules require Teams+ |
| **Feature Gate Key** | `FeatureFlags.CKVS.InferenceEngine` | Inference engine feature flag |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-31` | |
| **Parent Document** | [LCS-SBD-v0.10.2-KG](./LCS-SBD-v0.10.2-KG.md) | Inference Engine scope |
| **Scope Breakdown** | [LCS-SBD-v0.10.2-KG S2.1](./LCS-SBD-v0.10.2-KG.md#21-sub-parts) | f = Inference UI |

---

## 2. Executive Summary

### 2.1 The Requirement

Users need UI/API to:

1. **Manage rules:** Create, edit, delete, enable/disable inference rules
2. **Test rules:** Validate syntax and dry-run rules without persisting facts
3. **View derivations:** Browse derived facts and see which rules created them
4. **Understand explanations:** Modal/panel showing derivation chains
5. **Monitor performance:** See inference execution times and fact counts
6. **Configure settings:** Control inference depth, iteration limits, and scope

### 2.2 The Proposed Solution

Implement REST API and UI components for:

1. **Rule Management API:** CRUD operations on InferenceRule
2. **Derived Facts API:** Query and filter derived facts by rule/type
3. **Explanation API:** Retrieve derivation explanations
4. **Inference Control API:** Start/stop/dry-run inference
5. **Rule Editor:** Rich text editor with syntax highlighting and validation
6. **Derived Facts Browser:** Table view with filtering, sorting, explanations
7. **Metrics Dashboard:** Inference execution metrics and health

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Dependencies

| Component | Source Version | Purpose |
| :--- | :--- | :--- |
| `IInferenceEngine` | v0.10.2c | Execute and control inference |
| `IProvenanceTracker` | v0.10.2e | Generate explanations |
| `IAxiomStore` | v0.4.6-KG | Persist rules |
| `IGraphRepository` | v0.4.5e | Query derived facts |
| `IValidationEngine` | v0.6.5-KG | Validate rule syntax |
| `IProfileService` | v0.9.1 | User identity for audit |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| FluentValidation | 12.x | Rule validation |
| (None else required) | | Minimal dependencies |

### 3.2 Licensing Behavior

- **WriterPro:** Read-only view of built-in rules, no UI editing
- **Teams:** Full rule management (create, edit, delete up to 50), editing UI
- **Enterprise:** Unlimited rules, advanced configuration, API access

---

## 4. Data Contract (The API)

### 4.1 Rule Management API

```csharp
namespace Lexichord.Modules.CKVS.Inference.Services;

/// <summary>
/// API for managing inference rules (CRUD operations).
/// </summary>
public interface IInferenceRuleService
{
    /// <summary>
    /// Lists all available rules (filtered by license tier).
    /// </summary>
    Task<IReadOnlyList<InferenceRule>> ListRulesAsync(
        InferenceRuleFilter? filter = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a single rule by ID.
    /// </summary>
    Task<InferenceRule?> GetRuleAsync(
        Guid ruleId,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a new custom inference rule.
    /// Requires Teams license tier.
    /// </summary>
    Task<InferenceRule> CreateRuleAsync(
        CreateInferenceRuleRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Updates an existing rule.
    /// Built-in rules cannot be modified.
    /// </summary>
    Task<InferenceRule> UpdateRuleAsync(
        Guid ruleId,
        UpdateInferenceRuleRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a rule.
    /// Built-in rules cannot be deleted.
    /// </summary>
    Task DeleteRuleAsync(
        Guid ruleId,
        CancellationToken ct = default);

    /// <summary>
    /// Enables or disables a rule without deleting it.
    /// </summary>
    Task<InferenceRule> SetRuleEnabledAsync(
        Guid ruleId,
        bool enabled,
        CancellationToken ct = default);

    /// <summary>
    /// Validates rule syntax without persisting.
    /// </summary>
    Task<RuleValidationResult> ValidateRuleAsync(
        string ruleText,
        CancellationToken ct = default);

    /// <summary>
    /// Dry-runs a rule against current graph without persisting derived facts.
    /// </summary>
    Task<InferenceResult> DryRunRuleAsync(
        Guid ruleId,
        InferenceOptions? options = null,
        CancellationToken ct = default);
}

/// <summary>
/// Request to create a new inference rule.
/// </summary>
public record CreateInferenceRuleRequest
{
    /// <summary>
    /// Human-readable rule name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Optional description of what the rule does.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The rule definition in DSL syntax.
    /// </summary>
    public required string RuleText { get; init; }

    /// <summary>
    /// Priority (lower = higher priority, default 100).
    /// </summary>
    public int Priority { get; init; } = 100;

    /// <summary>
    /// Whether the rule is enabled by default.
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Scope: Workspace, Project, or Global.
    /// </summary>
    public InferenceRuleScope Scope { get; init; } = InferenceRuleScope.Workspace;
}

/// <summary>
/// Request to update an existing rule.
/// </summary>
public record UpdateInferenceRuleRequest
{
    /// <summary>
    /// Updated rule name (optional).
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Updated description (optional).
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Updated rule definition (optional).
    /// </summary>
    public string? RuleText { get; init; }

    /// <summary>
    /// Updated priority (optional).
    /// </summary>
    public int? Priority { get; init; }
}

/// <summary>
/// Filter criteria for listing rules.
/// </summary>
public record InferenceRuleFilter
{
    /// <summary>
    /// Filter by rule name (partial match).
    /// </summary>
    public string? NameContains { get; init; }

    /// <summary>
    /// Filter by enabled status.
    /// </summary>
    public bool? IsEnabled { get; init; }

    /// <summary>
    /// Filter by scope.
    /// </summary>
    public InferenceRuleScope? Scope { get; init; }

    /// <summary>
    /// Filter by creator ID.
    /// </summary>
    public Guid? CreatedBy { get; init; }

    /// <summary>
    /// Include built-in rules in results.
    /// </summary>
    public bool IncludeBuiltIn { get; init; } = true;
}

/// <summary>
/// Validation result for rule syntax/semantics.
/// </summary>
public record RuleValidationResult
{
    /// <summary>
    /// Whether the rule is valid.
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Validation errors (empty if valid).
    /// </summary>
    public IReadOnlyList<RuleValidationError> Errors { get; init; } = [];

    /// <summary>
    /// Warnings (non-fatal issues).
    /// </summary>
    public IReadOnlyList<RuleValidationWarning> Warnings { get; init; } = [];

    /// <summary>
    /// Compiled rule representation (if valid).
    /// </summary>
    public CompiledRule? CompiledRule { get; init; }
}

/// <summary>
/// A validation error in rule syntax.
/// </summary>
public record RuleValidationError
{
    /// <summary>
    /// Line number (1-based).
    /// </summary>
    public int LineNumber { get; init; }

    /// <summary>
    /// Column number (1-based).
    /// </summary>
    public int ColumnNumber { get; init; }

    /// <summary>
    /// Error message.
    /// </summary>
    public required string Message { get; init; }
}

/// <summary>
/// A validation warning for rule semantics.
/// </summary>
public record RuleValidationWarning
{
    /// <summary>
    /// Warning message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Severity: Info, Warning, or Error.
    /// </summary>
    public ValidationSeverity Severity { get; init; } = ValidationSeverity.Warning;
}

public enum ValidationSeverity { Info, Warning, Error }
```

### 4.2 Derived Facts API

```csharp
namespace Lexichord.Modules.CKVS.Inference.Services;

/// <summary>
/// API for querying and viewing derived facts.
/// </summary>
public interface IDerivedFactService
{
    /// <summary>
    /// Lists all derived facts with optional filtering.
    /// </summary>
    Task<IReadOnlyList<DerivedFact>> ListDerivedFactsAsync(
        DerivedFactFilter? filter = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a single derived fact by ID.
    /// </summary>
    Task<DerivedFact?> GetDerivedFactAsync(
        Guid factId,
        CancellationToken ct = default);

    /// <summary>
    /// Counts derived facts matching filter.
    /// </summary>
    Task<int> CountDerivedFactsAsync(
        DerivedFactFilter? filter = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets statistics about derived facts.
    /// </summary>
    Task<DerivedFactStatistics> GetStatisticsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Retracts a specific derived fact.
    /// </summary>
    Task RetractFactAsync(
        Guid factId,
        CancellationToken ct = default);
}

/// <summary>
/// Filter criteria for derived facts.
/// </summary>
public record DerivedFactFilter
{
    /// <summary>
    /// Filter by fact type.
    /// </summary>
    public DerivedFactType? FactType { get; init; }

    /// <summary>
    /// Filter by rule ID.
    /// </summary>
    public Guid? RuleId { get; init; }

    /// <summary>
    /// Filter by source entity ID.
    /// </summary>
    public Guid? SourceEntityId { get; init; }

    /// <summary>
    /// Filter by target entity ID.
    /// </summary>
    public Guid? TargetEntityId { get; init; }

    /// <summary>
    /// Filter by relationship type.
    /// </summary>
    public string? RelationshipType { get; init; }

    /// <summary>
    /// Filter by minimum confidence.
    /// </summary>
    public float? MinConfidence { get; init; }

    /// <summary>
    /// Filter by derivation date range.
    /// </summary>
    public DateTimeOffset? DerivedAfter { get; init; }

    /// <summary>
    /// Pagination: number of results per page.
    /// </summary>
    public int PageSize { get; init; } = 50;

    /// <summary>
    /// Pagination: page number (0-based).
    /// </summary>
    public int PageNumber { get; init; } = 0;
}

/// <summary>
/// Statistics about derived facts in the workspace.
/// </summary>
public record DerivedFactStatistics
{
    /// <summary>
    /// Total number of derived facts.
    /// </summary>
    public int TotalFacts { get; init; }

    /// <summary>
    /// Number of derived relationships.
    /// </summary>
    public int RelationshipCount { get; init; }

    /// <summary>
    /// Number of derived properties.
    /// </summary>
    public int PropertyCount { get; init; }

    /// <summary>
    /// Number of derived claims.
    /// </summary>
    public int ClaimCount { get; init; }

    /// <summary>
    /// Facts derived in last 24 hours.
    /// </summary>
    public int DerivedToday { get; init; }

    /// <summary>
    /// Average confidence of derived facts.
    /// </summary>
    public float AverageConfidence { get; init; }

    /// <summary>
    /// Most active rules (by fact count).
    /// </summary>
    public IReadOnlyList<RuleStatistic> TopRules { get; init; } = [];
}

/// <summary>
/// Statistics for a single rule.
/// </summary>
public record RuleStatistic
{
    /// <summary>
    /// Rule ID.
    /// </summary>
    public Guid RuleId { get; init; }

    /// <summary>
    /// Rule name.
    /// </summary>
    public string RuleName { get; init; } = "";

    /// <summary>
    /// Number of facts derived by this rule.
    /// </summary>
    public int FactCount { get; init; }

    /// <summary>
    /// Average execution time in milliseconds.
    /// </summary>
    public double AverageExecutionTimeMs { get; init; }

    /// <summary>
    /// Number of times rule has been evaluated.
    /// </summary>
    public int EvaluationCount { get; init; }
}
```

### 4.3 Explanation API

```csharp
namespace Lexichord.Modules.CKVS.Inference.Services;

/// <summary>
/// API for retrieving explanations of derived facts.
/// </summary>
public interface IExplanationService
{
    /// <summary>
    /// Gets detailed explanation for a derived fact.
    /// </summary>
    Task<DerivationExplanation?> GetExplanationAsync(
        Guid factId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets short one-line explanation.
    /// </summary>
    Task<string?> GetSummaryAsync(
        Guid factId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all explanations for facts derived by a rule.
    /// </summary>
    Task<IReadOnlyList<DerivationExplanation>> GetExplanationsByRuleAsync(
        Guid ruleId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets explanation chain as structured tree (for UI rendering).
    /// </summary>
    Task<ExplanationTreeNode?> GetExplanationTreeAsync(
        Guid factId,
        CancellationToken ct = default);
}

/// <summary>
/// Tree structure for rendering derivation chains in UI.
/// </summary>
public record ExplanationTreeNode
{
    /// <summary>
    /// Fact being explained.
    /// </summary>
    public required Guid FactId { get; init; }

    /// <summary>
    /// Description of the fact.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Is this fact derived (true) or asserted (false).
    /// </summary>
    public bool IsDerived { get; init; }

    /// <summary>
    /// If derived, the rule that created it.
    /// </summary>
    public string? DerivedByRuleName { get; init; }

    /// <summary>
    /// Child nodes (premises of this fact's rule).
    /// </summary>
    public IReadOnlyList<ExplanationTreeNode> Children { get; init; } = [];

    /// <summary>
    /// Depth in derivation chain (0 = root premises).
    /// </summary>
    public int Depth { get; init; }
}
```

### 4.4 Inference Control API

```csharp
namespace Lexichord.Modules.CKVS.Inference.Services;

/// <summary>
/// API for controlling inference execution.
/// </summary>
public interface IInferenceControlService
{
    /// <summary>
    /// Runs full inference on workspace.
    /// </summary>
    Task<InferenceResult> RunInferenceAsync(
        InferenceOptions? options = null,
        IProgress<InferenceProgress>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Runs inference on specified rules only.
    /// </summary>
    Task<InferenceResult> RunSelectedRulesAsync(
        IReadOnlyList<Guid> ruleIds,
        InferenceOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Dry-runs inference without persisting facts.
    /// </summary>
    Task<InferenceResult> DryRunInferenceAsync(
        InferenceOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Cancels running inference operation.
    /// </summary>
    Task CancelInferenceAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets current inference execution status.
    /// </summary>
    Task<InferenceExecutionStatus> GetStatusAsync(CancellationToken ct = default);

    /// <summary>
    /// Clears all derived facts and provenance.
    /// </summary>
    Task ClearAllDerivationsAsync(CancellationToken ct = default);

    /// <summary>
    /// Resets specific rules (clears their derived facts).
    /// </summary>
    Task ResetRuleAsync(Guid ruleId, CancellationToken ct = default);
}

/// <summary>
/// Progress updates during inference execution.
/// </summary>
public record InferenceProgress
{
    /// <summary>
    /// Current phase of inference (parsing, compiling, inferring, etc).
    /// </summary>
    public string Phase { get; init; } = "";

    /// <summary>
    /// Progress as percentage (0-100).
    /// </summary>
    public int ProgressPercentage { get; init; }

    /// <summary>
    /// Rules evaluated so far.
    /// </summary>
    public int RulesEvaluated { get; init; }

    /// <summary>
    /// Facts derived so far.
    /// </summary>
    public int FactsDerived { get; init; }

    /// <summary>
    /// Current iteration number.
    /// </summary>
    public int CurrentIteration { get; init; }
}

/// <summary>
/// Current status of inference execution.
/// </summary>
public record InferenceExecutionStatus
{
    /// <summary>
    /// Whether inference is currently running.
    /// </summary>
    public bool IsRunning { get; init; }

    /// <summary>
    /// Current phase.
    /// </summary>
    public string CurrentPhase { get; init; } = "";

    /// <summary>
    /// Start time of current execution.
    /// </summary>
    public DateTimeOffset? StartedAt { get; init; }

    /// <summary>
    /// Progress percentage.
    /// </summary>
    public int ProgressPercentage { get; init; }

    /// <summary>
    /// Last completion time (if not currently running).
    /// </summary>
    public DateTimeOffset? LastCompletedAt { get; init; }

    /// <summary>
    /// Duration of last execution.
    /// </summary>
    public TimeSpan? LastDuration { get; init; }
}
```

---

## 5. UI Components

### 5.1 Rule Editor Component

```
┌────────────────────────────────────────────────────────────┐
│ Rule Editor                                    [Save] [Cancel]
├────────────────────────────────────────────────────────────┤
│ Name: [____________________________________]                │
│ Description: [_______________________________]              │
│                                                            │
│ Priority: [___] (lower = higher priority)                  │
│ Scope: [Workspace  ▼]  Enabled: [✓]                       │
│                                                            │
│ Rule Text (Syntax Highlighting):                          │
│ ┌──────────────────────────────────────────────────────┐  │
│ │ 1 │ RULE "Grandparent Inference"                      │  │
│ │ 2 │ WHEN                                              │  │
│ │ 3 │     ?a -[PARENT_OF]-> ?b                          │  │
│ │ 4 │     ?b -[PARENT_OF]-> ?c                          │  │
│ │ 5 │ THEN                                              │  │
│ │ 6 │     DERIVE ?a -[GRANDPARENT_OF]-> ?c              │  │
│ │   │                                                    │  │
│ │   │ [Errors/warnings shown in red/yellow]             │  │
│ └──────────────────────────────────────────────────────┘  │
│                                                            │
│ [✓] Validate Syntax   [Run Dry Run]   [Test Rule]        │
│                                                            │
│ Validation Results:                                        │
│ ✓ Syntax valid                                             │
│ ✓ All variables bound                                      │
│ ✓ 4 patterns recognized                                    │
└────────────────────────────────────────────────────────────┘
```

**Features:**
- Real-time syntax highlighting
- Line numbers and error markers
- Auto-completion for keywords, variables
- Inline validation errors
- Dry-run button shows what facts would be derived

### 5.2 Derived Facts Browser

```
┌────────────────────────────────────────────────────────────┐
│ Derived Facts                                   [Refresh]  │
├────────────────────────────────────────────────────────────┤
│ Filters:                                                   │
│ [Type: All Types ▼] [Rule: All Rules ▼] [Confidence: 0▲]  │
│ [Search: _____________________________] [Apply Filters]   │
│                                                            │
│ Results: 247 facts  [First] [◀] 1 / 5 [▶] [Last]          │
│                                                            │
│ ┌────────────────────────────────────────────────────────┐ │
│ │ Source Entity    │ Relationship    │ Target Entity      │ │
│ │ ─────────────────┼─────────────────┼──────────────────  │ │
│ │ UserService      │ DEPENDS_ON      │ AuthService        │ │
│ │   [Explanation]                                         │ │
│ │                                                          │ │
│ │ EmailService     │ DEPENDS_ON      │ StorageService     │ │
│ │   [Explanation]                                         │ │
│ │                                                          │ │
│ │ OrderService     │ REQUIRES_AUTH   │ true               │ │
│ │   [Explanation]                                         │ │
│ └────────────────────────────────────────────────────────┘ │
│                                                            │
│ Showing 50 of 247 results                                  │
└────────────────────────────────────────────────────────────┘
```

**Features:**
- Sortable/filterable table
- Pagination controls
- "Explanation" link opens modal for each fact
- Shows derived fact type (relationship/property/claim)
- Confidence score display

### 5.3 Explanation Modal

(From SBD section 5 — shows derivation chain with premises, rule, and natural language explanation)

### 5.4 Metrics Dashboard

```
┌────────────────────────────────────────────────────────────┐
│ Inference Metrics                    [Run Inference] [Clear]
├────────────────────────────────────────────────────────────┤
│ Summary:                                                   │
│  • Total Derived Facts: 1,247                             │
│  • Last Run: 2026-01-31 14:30 UTC (4.2 seconds)          │
│  • Rules Evaluated: 18 / 20 (2 disabled)                 │
│  • Average Confidence: 0.95                               │
│                                                            │
│ Top Rules (by facts derived):                             │
│ ┌──────────────────────────────────────────────────────┐  │
│ │ Rule Name              │ Facts │ Avg Time │ Eval Count │  │
│ │ ────────────────────────┼───────┼──────────┼────────── │  │
│ │ Service Dependency     │ 342   │ 12.4ms   │ 24        │  │
│ │ Document Dependency    │ 198   │ 8.7ms    │ 20        │  │
│ │ Auth Propagation       │ 156   │ 15.2ms   │ 18        │  │
│ │ Deprecation Propagation│ 89    │ 11.3ms   │ 14        │  │
│ └──────────────────────────────────────────────────────┘  │
│                                                            │
│ Recent Executions:                                        │
│ • 2026-01-31 14:30: 1,247 facts → 4.2s ✓                 │
│ • 2026-01-31 13:15: 1,201 facts → 3.9s ✓                 │
│ • 2026-01-31 12:00: Error - Max iterations exceeded       │
│                                                            │
│ [View Full History] [Export Results]                     │
└────────────────────────────────────────────────────────────┘
```

---

## 6. REST API Routes

### 6.1 Rule Management

```
GET    /api/inference/rules
       - List all rules with optional filtering
       - Query params: filter=name, isEnabled, scope, includeBuiltIn

GET    /api/inference/rules/{ruleId}
       - Get single rule details

POST   /api/inference/rules
       - Create new rule (Teams+ license required)
       - Body: CreateInferenceRuleRequest

PUT    /api/inference/rules/{ruleId}
       - Update rule (cannot modify built-in rules)
       - Body: UpdateInferenceRuleRequest

DELETE /api/inference/rules/{ruleId}
       - Delete custom rule (built-in rules cannot be deleted)

PATCH  /api/inference/rules/{ruleId}/enabled
       - Enable/disable rule without deletion
       - Body: { enabled: true/false }

POST   /api/inference/rules/validate
       - Validate rule syntax
       - Body: { ruleText: "..." }

POST   /api/inference/rules/{ruleId}/dry-run
       - Dry-run rule without persisting facts
       - Query params: maxDepth=10, timeout=30000
```

### 6.2 Derived Facts

```
GET    /api/inference/facts
       - List derived facts with filtering
       - Query params: filter (JSON encoded), pageSize=50, page=0

GET    /api/inference/facts/{factId}
       - Get single derived fact

GET    /api/inference/facts/count
       - Count derived facts matching filter
       - Query params: filter (JSON encoded)

GET    /api/inference/stats
       - Get statistics about derived facts

DELETE /api/inference/facts/{factId}
       - Retract a derived fact
```

### 6.3 Explanations

```
GET    /api/inference/explanations/{factId}
       - Get detailed explanation

GET    /api/inference/explanations/{factId}/summary
       - Get one-line summary

GET    /api/inference/explanations/rule/{ruleId}
       - Get all explanations for rule's facts

GET    /api/inference/explanations/{factId}/tree
       - Get explanation as tree structure
```

### 6.4 Inference Control

```
POST   /api/inference/run
       - Run full inference
       - Body: { maxDepth: 10, maxIterations: 1000, timeout: 30000 }

POST   /api/inference/run/selected
       - Run selected rules only
       - Body: { ruleIds: [guid, guid, ...] }

POST   /api/inference/dry-run
       - Dry-run without persisting facts

POST   /api/inference/cancel
       - Cancel running inference

GET    /api/inference/status
       - Get current execution status

DELETE /api/inference/clear
       - Clear all derived facts

PATCH  /api/inference/rules/{ruleId}/clear
       - Clear derived facts for specific rule
```

---

## 7. Error Handling

### 7.1 Rule Validation Errors

**Scenario:** User submits rule with syntax errors.

**Handling:**
- Return 400 Bad Request with RuleValidationResult
- Include line/column numbers for error location
- Suggest corrections if possible

**Code:**
```csharp
[HttpPost("rules/validate")]
public async Task<ActionResult<RuleValidationResult>> ValidateRule(
    ValidateRuleRequest request,
    CancellationToken ct)
{
    try
    {
        var result = await _ruleService.ValidateRuleAsync(request.RuleText, ct);
        if (!result.IsValid)
            return BadRequest(result);
        return Ok(result);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Rule validation failed");
        return StatusCode(500, new { error = "Rule validation failed" });
    }
}
```

### 7.2 Inference Execution Error

**Scenario:** Inference hits cycle or max iterations.

**Handling:**
- Return InferenceResult with Status = CycleDetected or Error
- Include partial results (facts derived before error)
- Log details for debugging

**Code:**
```csharp
[HttpPost("run")]
public async Task<ActionResult<InferenceResult>> RunInference(
    InferenceOptions options,
    CancellationToken ct)
{
    try
    {
        var result = await _inferenceService.RunInferenceAsync(options, null, ct);
        return Ok(result);
    }
    catch (OperationCanceledException)
    {
        return StatusCode(408, new { error = "Inference timeout exceeded" });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Inference execution failed");
        return StatusCode(500, new { error = "Inference failed", detail = ex.Message });
    }
}
```

### 7.3 License Tier Restriction

**Scenario:** WriterPro user tries to create custom rule.

**Handling:**
- Return 403 Forbidden
- Explain license requirement in error message

**Code:**
```csharp
[HttpPost("rules")]
[Authorize]
public async Task<ActionResult<InferenceRule>> CreateRule(
    CreateInferenceRuleRequest request,
    CancellationToken ct)
{
    var licenseContext = _licenseProvider.GetCurrentLicense();
    if (licenseContext.Tier < LicenseTier.Teams)
    {
        return Forbid("Custom rules require Teams license or higher");
    }

    var rule = await _ruleService.CreateRuleAsync(request, ct);
    return Created($"/api/inference/rules/{rule.RuleId}", rule);
}
```

---

## 8. Testing

### 8.1 API Unit Tests

```csharp
[TestClass]
public class InferenceRuleServiceTests
{
    private IInferenceRuleService _service;

    [TestInitialize]
    public void Setup()
    {
        _service = new InferenceRuleService(/* dependencies */);
    }

    [TestMethod]
    public async Task ValidateRule_WithValidSyntax_ReturnsSuccess()
    {
        var ruleText = @"RULE ""Test""
                        WHEN ?a -[PARENT]-> ?b
                        THEN DERIVE ?a -[GRANDPARENT]-> ?b";

        var result = await _service.ValidateRuleAsync(ruleText);

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual(0, result.Errors.Count);
    }

    [TestMethod]
    public async Task ValidateRule_WithSyntaxError_ReturnsError()
    {
        var ruleText = @"RULE Test WHEN THEN DERIVE"; // Invalid

        var result = await _service.ValidateRuleAsync(ruleText);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Count > 0);
    }

    [TestMethod]
    public async Task CreateRule_WithValidRequest_CreatesRule()
    {
        var request = new CreateInferenceRuleRequest
        {
            Name = "Test Rule",
            RuleText = "RULE \"Test\" WHEN ?a -[P]-> ?b THEN DERIVE ?a -[GP]-> ?b",
            Priority = 100
        };

        var rule = await _service.CreateRuleAsync(request);

        Assert.IsNotNull(rule);
        Assert.AreEqual("Test Rule", rule.Name);
    }

    [TestMethod]
    public async Task DryRunRule_DoesNotPersistFacts()
    {
        var ruleId = await CreateTestRule();
        var beforeCount = await _factRepository.CountAsync();

        var result = await _service.DryRunRuleAsync(ruleId);

        var afterCount = await _factRepository.CountAsync();
        Assert.AreEqual(beforeCount, afterCount); // No facts persisted
    }
}
```

### 8.2 Integration Tests

```csharp
[TestClass]
public class InferenceUIIntegrationTests
{
    [TestMethod]
    public async Task FullWorkflow_CreateRule_ExecuteInference_ViewResults()
    {
        var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };

        // 1. Create rule
        var createRequest = new CreateInferenceRuleRequest
        {
            Name = "Test Rule",
            RuleText = "RULE \"Test\" WHEN ... THEN ..."
        };
        var createResponse = await httpClient.PostAsJsonAsync(
            "/api/inference/rules", createRequest);
        Assert.AreEqual(HttpStatusCode.Created, createResponse.StatusCode);
        var rule = await createResponse.Content.ReadAsAsync<InferenceRule>();

        // 2. Run inference
        var runResponse = await httpClient.PostAsync(
            "/api/inference/run", new StringContent("{}"));
        var result = await runResponse.Content.ReadAsAsync<InferenceResult>();
        Assert.IsTrue(result.FactsDerived > 0);

        // 3. List derived facts
        var listResponse = await httpClient.GetAsync(
            "/api/inference/facts?pageSize=10");
        var facts = await listResponse.Content.ReadAsAsync<IReadOnlyList<DerivedFact>>();
        Assert.IsTrue(facts.Count > 0);

        // 4. Get explanation
        var explainResponse = await httpClient.GetAsync(
            $"/api/inference/explanations/{facts[0].FactId}");
        var explanation = await explainResponse.Content.ReadAsAsync<DerivationExplanation>();
        Assert.IsNotNull(explanation.NaturalLanguageExplanation);
    }
}
```

---

## 9. Performance Considerations

| Operation | Target | Strategy |
| :--- | :--- | :--- |
| List rules | <50ms | In-memory cache, simple filtering |
| Validate rule | <100ms | AST generation only, no execution |
| Dry-run rule | <500ms | Full execution, discard results |
| List facts (page) | <100ms | Indexed repository, pagination |
| Get explanation | <500ms | Cached generation, lazy loading |
| Run full inference | <30s | Async operation, progress updates |

---

## 10. Security & Safety

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| Rule injection | High | Input validation, DSL sandboxing |
| Unauthorized access | High | Permission checks, license tier validation |
| Information disclosure | Medium | Explanation access control |
| DoS via complex rules | Medium | Timeout limits, iteration caps |
| Fact deletion abuse | Low | Audit logging, soft deletes |

---

## 11. License Gating

| Tier | Support |
| :--- | :--- |
| WriterPro | Read-only view of rules/facts, no editing |
| Teams | Full rule management (create, edit, delete), full UI |
| Enterprise | All features + API access + metrics API |

---

## 12. Acceptance Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | Valid rule | Submitted via API | Rule persisted, returned in list |
| 2 | Invalid rule | Submitted via API | 400 error, line/column errors provided |
| 3 | Rule in editor | User clicks "Validate" | Real-time validation feedback shown |
| 4 | Derived facts | User filters and pages | Pagination works, filters applied |
| 5 | Derived fact | User clicks "Explanation" | Modal shows derivation chain |
| 6 | Inference running | User clicks "Cancel" | Execution stops, partial results returned |
| 7 | WriterPro user | Tries to create rule | 403 Forbidden, license error |
| 8 | Rule with dry-run | Executed | No facts persisted, preview shown |

---

## 13. Changelog

| Version | Date | Changes |
| :--- | :--- | :--- |
| 1.0 | 2026-01-31 | Initial design - Rule management API, facts browser, explanations UI |
