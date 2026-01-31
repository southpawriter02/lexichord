# LCS-DES-077-KG-f: Gating Step Type

## Document Control

| Field | Value |
| :--- | :--- |
| **Spec ID** | LCS-DES-077-KG-f |
| **System Breakdown** | LCS-SBD-077-KG |
| **Version** | v0.7.7 |
| **Codename** | Gating Step Type (CKVS Phase 4d) |
| **Estimated Hours** | 3 |
| **Status** | Draft |
| **Last Updated** | 2026-01-31 |

---

## 1. Overview

### 1.1 Purpose

The **Gating Step Type** implements workflow gates that block execution or trigger alternative paths based on validation condition expressions. Gates prevent invalid documents from proceeding to publication, distribution, or downstream workflows.

### 1.2 Key Responsibilities

- Evaluate condition expressions on validated documents
- Block workflow progression on unmet conditions
- Support branching to alternate workflow paths
- Execute gate logic at strategic workflow checkpoints
- Log gating decisions for audit trails
- Provide meaningful failure messages to users
- Support complex boolean conditions

### 1.3 Module Location

```
src/
  Lexichord.Workflows/
    Validation/
      Steps/
        IGatingWorkflowStep.cs
        GatingWorkflowStep.cs
        GatingCondition.cs
        GatingConditionEvaluator.cs
```

---

## 2. Interface Definitions

### 2.1 Gating Workflow Step Interface

```csharp
namespace Lexichord.Workflows.Validation.Steps;

/// <summary>
/// Gating step that blocks workflow progression.
/// </summary>
public interface IGatingWorkflowStep : IWorkflowStep
{
    /// <summary>
    /// Condition expression to evaluate.
    /// </summary>
    string ConditionExpression { get; }

    /// <summary>
    /// Message to display on gate failure.
    /// </summary>
    string FailureMessage { get; }

    /// <summary>
    /// Path to branch to on gate failure (if set).
    /// </summary>
    string? BranchPath { get; }

    /// <summary>
    /// Whether to require all conditions to pass.
    /// </summary>
    bool RequireAll { get; }

    /// <summary>
    /// Evaluates gate condition.
    /// </summary>
    Task<GatingResult> EvaluateAsync(
        Document document,
        ValidationContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Gets readable representation of condition.
    /// </summary>
    string GetConditionDescription();
}
```

---

## 3. Data Types

### 3.1 Gating Result

```csharp
/// <summary>
/// Result of gating evaluation.
/// </summary>
public record GatingResult
{
    /// <summary>Whether gate passed.</summary>
    public required bool Passed { get; init; }

    /// <summary>Gate step ID.</summary>
    public required string GateId { get; init; }

    /// <summary>Condition that was evaluated.</summary>
    public required string Condition { get; init; }

    /// <summary>Failure message (if not passed).</summary>
    public string? FailureMessage { get; init; }

    /// <summary>Branch to take on failure (if set).</summary>
    public string? BranchPath { get; init; }

    /// <summary>Detailed evaluation logs.</summary>
    public IReadOnlyList<string> EvaluationLogs { get; init; } = [];

    /// <summary>Execution time in milliseconds.</summary>
    public long ExecutionTimeMs { get; init; }

    /// <summary>Metadata about evaluation.</summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}
```

### 3.2 Gating Condition

```csharp
/// <summary>
/// Single condition in gate expression.
/// </summary>
public record GatingCondition
{
    /// <summary>Condition identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Condition expression.</summary>
    public required string Expression { get; init; }

    /// <summary>Expected result (true/false).</summary>
    public bool ExpectedResult { get; init; } = true;

    /// <summary>Error message if condition fails.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Metadata for evaluation.</summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}
```

### 3.3 Expression Evaluation Context

```csharp
/// <summary>
/// Context for evaluating condition expressions.
/// </summary>
public record ExpressionEvaluationContext
{
    /// <summary>Document being evaluated.</summary>
    public required Document Document { get; init; }

    /// <summary>Validation results from previous steps.</summary>
    public IReadOnlyList<ValidationStepResult>? ValidationResults { get; init; }

    /// <summary>Workflow context.</summary>
    public WorkflowContext? WorkflowContext { get; init; }

    /// <summary>Available variables for expression.</summary>
    public IReadOnlyDictionary<string, object>? Variables { get; init; }

    /// <summary>Validation context.</summary>
    public ValidationContext? ValidationContext { get; init; }
}
```

---

## 4. Implementation

### 4.1 Gating Workflow Step

```csharp
public class GatingWorkflowStep : IGatingWorkflowStep
{
    private readonly IGatingConditionEvaluator _evaluator;
    private readonly ILogger<GatingWorkflowStep> _logger;
    private readonly INotificationService _notificationService;

    public string Id { get; }
    public string Name { get; }
    public string? Description { get; }
    public int Order { get; }
    public bool IsEnabled { get; set; }
    public int? TimeoutMs { get; }

    public string ConditionExpression { get; }
    public string FailureMessage { get; }
    public string? BranchPath { get; }
    public bool RequireAll { get; }

    public GatingWorkflowStep(
        string id,
        string name,
        string conditionExpression,
        string failureMessage,
        IGatingConditionEvaluator evaluator,
        ILogger<GatingWorkflowStep> logger,
        INotificationService notificationService,
        string? description = null,
        int order = 0,
        int? timeoutMs = null,
        string? branchPath = null,
        bool requireAll = true)
    {
        Id = id;
        Name = name;
        Description = description;
        Order = order;
        TimeoutMs = timeoutMs ?? 10000;
        ConditionExpression = conditionExpression;
        FailureMessage = failureMessage;
        BranchPath = branchPath;
        RequireAll = requireAll;

        _evaluator = evaluator;
        _logger = logger;
        _notificationService = notificationService;
    }

    public IReadOnlyList<ValidationError> ValidateConfiguration()
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(Id))
            errors.Add(new ValidationError { Message = "Gate ID cannot be empty" });

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add(new ValidationError { Message = "Gate name cannot be empty" });

        if (string.IsNullOrWhiteSpace(ConditionExpression))
            errors.Add(new ValidationError { Message = "Condition expression cannot be empty" });

        if (string.IsNullOrWhiteSpace(FailureMessage))
            errors.Add(new ValidationError { Message = "Failure message cannot be empty" });

        if (TimeoutMs.HasValue && TimeoutMs.Value <= 0)
            errors.Add(new ValidationError { Message = "Timeout must be positive" });

        return errors;
    }

    public async Task<GatingResult> EvaluateAsync(
        Document document,
        ValidationContext context,
        CancellationToken ct = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var logs = new List<string>();

        try
        {
            logs.Add($"[{DateTime.UtcNow:O}] Starting gate evaluation: {Id}");
            logs.Add($"Condition: {ConditionExpression}");

            // Parse expression into conditions
            var conditions = ParseExpression(ConditionExpression);
            logs.Add($"Parsed {conditions.Count} conditions from expression");

            // Evaluate each condition
            var conditionResults = new Dictionary<string, bool>();
            foreach (var condition in conditions)
            {
                logs.Add($"Evaluating condition: {condition.Id}");

                var result = await _evaluator.EvaluateAsync(
                    condition,
                    new ExpressionEvaluationContext
                    {
                        Document = document,
                        ValidationContext = context
                    },
                    ct);

                conditionResults[condition.Id] = result;
                logs.Add($"  Result: {(result ? "PASS" : "FAIL")}");
            }

            stopwatch.Stop();

            // Determine overall result
            var passed = RequireAll
                ? conditionResults.Values.All(v => v)
                : conditionResults.Values.Any(v => v);

            logs.Add($"Overall result: {(passed ? "PASS" : "FAIL")} (RequireAll={RequireAll})");

            _logger.LogInformation(
                "Gate {GateId} evaluated: {Result}",
                Id, passed ? "PASS" : "FAIL");

            return new GatingResult
            {
                Passed = passed,
                GateId = Id,
                Condition = ConditionExpression,
                FailureMessage = passed ? null : FailureMessage,
                BranchPath = passed ? null : BranchPath,
                EvaluationLogs = logs,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                Metadata = new Dictionary<string, object>
                {
                    ["conditionCount"] = conditions.Count,
                    ["requireAll"] = RequireAll
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gate {GateId} evaluation failed", Id);
            stopwatch.Stop();

            logs.Add($"ERROR: {ex.Message}");

            return new GatingResult
            {
                Passed = false,
                GateId = Id,
                Condition = ConditionExpression,
                FailureMessage = $"Gate evaluation error: {ex.Message}",
                EvaluationLogs = logs,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
    }

    public async Task<WorkflowStepResult> ExecuteAsync(
        WorkflowContext context,
        CancellationToken ct = default)
    {
        if (!IsEnabled)
        {
            return new WorkflowStepResult
            {
                StepId = Id,
                Success = true,
                Message = "Gate is disabled (auto-passing)"
            };
        }

        try
        {
            using var cts = TimeoutMs.HasValue
                ? new CancellationTokenSource(TimeoutMs.Value)
                : new CancellationTokenSource();

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token);

            var validationContext = new ValidationContext
            {
                WorkspaceId = context.WorkspaceId,
                Document = context.CurrentDocument,
                WorkflowId = context.WorkflowId,
                UserId = context.UserId,
                Trigger = ValidationTrigger.PreWorkflow,
                StartTime = DateTime.UtcNow
            };

            var result = await EvaluateAsync(
                context.CurrentDocument,
                validationContext,
                linked.Token);

            if (!result.Passed)
            {
                _logger.LogWarning(
                    "Gate {GateId} blocked workflow: {Message}",
                    Id, result.FailureMessage);

                // Notify user if in interactive context
                if (context.UserId.HasValue)
                {
                    await _notificationService.NotifyAsync(
                        new Notification
                        {
                            UserId = context.UserId.Value,
                            Type = NotificationType.GateBlocked,
                            Title = $"Gate Failed: {Name}",
                            Message = result.FailureMessage,
                            Severity = NotificationSeverity.Warning
                        },
                        ct);
                }
            }

            return new WorkflowStepResult
            {
                StepId = Id,
                Success = result.Passed,
                Message = result.Passed
                    ? "Gate passed, workflow continues"
                    : $"Gate blocked: {result.FailureMessage}",
                BranchPath = result.BranchPath,
                Data = new Dictionary<string, object>
                {
                    ["gatingResult"] = result
                }
            };
        }
        catch (OperationCanceledException)
        {
            return new WorkflowStepResult
            {
                StepId = Id,
                Success = false,
                Message = "Gate evaluation timed out"
            };
        }
    }

    public string GetConditionDescription()
    {
        var conditions = ParseExpression(ConditionExpression);
        var descriptions = conditions.Select(c => c.Expression);
        var joined = string.Join(RequireAll ? " AND " : " OR ", descriptions);
        return joined;
    }

    private List<GatingCondition> ParseExpression(string expression)
    {
        // Parse expression like:
        // "validation_count(error) == 0 AND validation_count(warning) <= 5"
        // Returns list of individual conditions

        var conditions = new List<GatingCondition>();
        var parts = expression.Split(new[] { " AND ", " OR " }, StringSplitOptions.None);

        for (int i = 0; i < parts.Length; i++)
        {
            var part = parts[i].Trim();
            conditions.Add(new GatingCondition
            {
                Id = $"condition_{i}",
                Expression = part,
                ExpectedResult = true
            });
        }

        return conditions;
    }
}
```

### 4.2 Gating Condition Evaluator

```csharp
public interface IGatingConditionEvaluator
{
    /// <summary>
    /// Evaluates a single condition.
    /// </summary>
    Task<bool> EvaluateAsync(
        GatingCondition condition,
        ExpressionEvaluationContext context,
        CancellationToken ct = default);
}

public class GatingConditionEvaluator : IGatingConditionEvaluator
{
    private readonly ILogger<GatingConditionEvaluator> _logger;

    public GatingConditionEvaluator(
        ILogger<GatingConditionEvaluator> logger)
    {
        _logger = logger;
    }

    public async Task<bool> EvaluateAsync(
        GatingCondition condition,
        ExpressionEvaluationContext context,
        CancellationToken ct = default)
    {
        try
        {
            // Evaluate condition expression
            // Supports: validation_count(severity), metadata(key), content_length, etc.

            var result = EvaluateExpression(condition.Expression, context);
            return result == condition.ExpectedResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Condition evaluation failed: {Condition}", condition.Expression);
            return false;
        }
    }

    private bool EvaluateExpression(
        string expression,
        ExpressionEvaluationContext context)
    {
        // Parse and evaluate expression
        // Examples:
        // - validation_count(error) == 0
        // - validation_count(warning) <= 5
        // - metadata('published') == true
        // - content_length > 100
        // - has_schema == true

        if (expression.Contains("validation_count"))
        {
            return EvaluateValidationCount(expression, context);
        }
        else if (expression.Contains("metadata"))
        {
            return EvaluateMetadata(expression, context);
        }
        else if (expression.Contains("content_length"))
        {
            return EvaluateContentLength(expression, context);
        }
        else if (expression.Contains("has_"))
        {
            return EvaluateHasProperty(expression, context);
        }

        throw new InvalidOperationException($"Unknown expression: {expression}");
    }

    private bool EvaluateValidationCount(
        string expression,
        ExpressionEvaluationContext context)
    {
        // Parse: validation_count(error) == 0
        var match = System.Text.RegularExpressions.Regex.Match(
            expression,
            @"validation_count\((\w+)\)\s*(==|!=|<|>|<=|>=)\s*(\d+)");

        if (!match.Success)
            throw new InvalidOperationException($"Invalid validation_count expression: {expression}");

        var severity = match.Groups[1].Value;
        var op = match.Groups[2].Value;
        var value = int.Parse(match.Groups[3].Value);

        var count = context.ValidationResults?
            .Where(r => r.FailureSeverity.ToString().ToLower() == severity.ToLower())
            .Sum(r => r.ItemsWithIssues) ?? 0;

        return EvaluateComparison(count, op, value);
    }

    private bool EvaluateMetadata(
        string expression,
        ExpressionEvaluationContext context)
    {
        // Parse: metadata('key') == value
        var match = System.Text.RegularExpressions.Regex.Match(
            expression,
            @"metadata\('([^']+)'\)\s*(==|!=)\s*(.+)");

        if (!match.Success)
            throw new InvalidOperationException($"Invalid metadata expression: {expression}");

        var key = match.Groups[1].Value;
        var op = match.Groups[2].Value;
        var expectedValue = match.Groups[3].Value.Trim('\'');

        var actualValue = context.Document?.Metadata?.ContainsKey(key) == true
            ? context.Document.Metadata[key]?.ToString()
            : null;

        return op == "=="
            ? actualValue == expectedValue
            : actualValue != expectedValue;
    }

    private bool EvaluateContentLength(
        string expression,
        ExpressionEvaluationContext context)
    {
        // Parse: content_length > 100
        var match = System.Text.RegularExpressions.Regex.Match(
            expression,
            @"content_length\s*(==|!=|<|>|<=|>=)\s*(\d+)");

        if (!match.Success)
            throw new InvalidOperationException($"Invalid content_length expression: {expression}");

        var op = match.Groups[1].Value;
        var value = int.Parse(match.Groups[2].Value);
        var length = context.Document?.Content?.Length ?? 0;

        return EvaluateComparison(length, op, value);
    }

    private bool EvaluateHasProperty(
        string expression,
        ExpressionEvaluationContext context)
    {
        // Parse: has_schema == true
        if (expression.Contains("has_schema"))
        {
            var hasSchema = context.Document?.Schema != null;
            return expression.Contains("== true") ? hasSchema : !hasSchema;
        }

        throw new InvalidOperationException($"Unknown property check: {expression}");
    }

    private bool EvaluateComparison(int actual, string op, int expected)
    {
        return op switch
        {
            "==" => actual == expected,
            "!=" => actual != expected,
            "<" => actual < expected,
            ">" => actual > expected,
            "<=" => actual <= expected,
            ">=" => actual >= expected,
            _ => throw new InvalidOperationException($"Unknown operator: {op}")
        };
    }
}
```

---

## 5. Gate Evaluation Flow

```
[Workflow Execution]
        |
        v
[GatingWorkflowStep.ExecuteAsync]
        |
        +---> [Validate Configuration]
        |
        +---> [Parse Condition Expression]
        |
        +---> [For Each Condition]
        |     +---> [Evaluate via IGatingConditionEvaluator]
        |     +---> [Collect Result]
        |
        +---> [Combine Results (AND/OR)]
        |
        +---> [Create GatingResult]
        |
        +---> [Notify User if Failed]
        |
        +---> [Return WorkflowStepResult]
        |
        +---> [If Failed + BranchPath]
              +---> [Branch to Alternate Path]
```

---

## 6. Error Handling

| Error | Handling Strategy |
| :---- | :---------------- |
| Invalid expression syntax | Return evaluation error with details |
| Unknown expression function | Log and return false |
| Document missing | Return false |
| Evaluation timeout | Cancel and return timeout error |
| Reference to missing property | Return false |

---

## 7. Testing Requirements

| Test Case | Description |
| :-------- | :---------- |
| `EvaluateCondition_ValidationCount_Zero` | Count validation passes |
| `EvaluateCondition_ValidationCount_Exceeds` | Count validation fails |
| `EvaluateCondition_Metadata_Matches` | Metadata matches |
| `EvaluateCondition_ContentLength_Exceeds` | Content length checked |
| `EvaluateCondition_HasSchema_True` | Schema presence checked |
| `EvaluateMultiple_AND_AllPass` | AND logic works |
| `EvaluateMultiple_AND_OneFails` | AND logic fails |
| `EvaluateMultiple_OR_OnePasses` | OR logic works |
| `EvaluateExpression_InvalidSyntax` | Invalid syntax handled |
| `Gate_BlocksWorkflow_OnFailure` | Workflow blocked |
| `Gate_BranchesOnFailure` | Branching works |

---

## 8. Performance Considerations

| Aspect | Target |
| :------ | :------ |
| Expression parsing | < 50ms |
| Condition evaluation | < 100ms per condition |
| Overall gate evaluation | < TimeoutMs (default 10s) |
| Memory per gate | < 10MB |

---

## 9. License Gating

| Tier | Access |
| :--- | :----- |
| Core | Not available |
| WriterPro | Basic gates (validation count only) |
| Teams | All expressions + branching |
| Enterprise | Full + custom expression functions |

---

## 10. Changelog

| Version | Date | Author | Changes |
| :------ | :--- | :----- | :------ |
| 1.0 | 2026-01-31 | Lead Architect | Initial creation |
