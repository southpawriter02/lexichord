# LCS-DES-077-KG-e: Validation Step Types

## Document Control

| Field | Value |
| :--- | :--- |
| **Spec ID** | LCS-DES-077-KG-e |
| **System Breakdown** | LCS-SBD-077-KG |
| **Version** | v0.7.7 |
| **Codename** | Validation Step Types (CKVS Phase 4d) |
| **Estimated Hours** | 4 |
| **Status** | Draft |
| **Last Updated** | 2026-01-31 |

---

## 1. Overview

### 1.1 Purpose

The **Validation Step Types** module defines unified validation step implementations for the Validation Workflows system. These steps enable consistent execution of validation rules within the `IWorkflowEngine`, providing standardized failure actions, severity tracking, and async support across all validation workflows.

### 1.2 Key Responsibilities

- Define ValidationWorkflowStep interface implementing IWorkflowStep
- Support multiple validation step types (schema, consistency, reference, custom)
- Implement failure action handling (Halt, Continue, Branch)
- Track failure severity levels (Warning, Error, Critical)
- Support async and sync execution modes
- Integrate with IUnifiedValidationService
- Handle conditional execution based on options

### 1.3 Module Location

```
src/
  Lexichord.Workflows/
    Validation/
      Steps/
        IValidationWorkflowStep.cs
        ValidationWorkflowStep.cs
        ValidationStepType.cs
        ValidationFailureAction.cs
        ValidationFailureSeverity.cs
```

---

## 2. Interface Definitions

### 2.1 Validation Workflow Step Interface

```csharp
namespace Lexichord.Workflows.Validation.Steps;

/// <summary>
/// Validation step for workflow engine.
/// </summary>
public interface IValidationWorkflowStep : IWorkflowStep
{
    /// <summary>
    /// Type of validation step.
    /// </summary>
    ValidationStepType StepType { get; }

    /// <summary>
    /// Options for validation execution.
    /// </summary>
    IReadOnlyDictionary<string, object> Options { get; }

    /// <summary>
    /// Action to take on validation failure.
    /// </summary>
    ValidationFailureAction FailureAction { get; }

    /// <summary>
    /// Severity level of validation failure.
    /// </summary>
    ValidationFailureSeverity FailureSeverity { get; }

    /// <summary>
    /// Whether to execute asynchronously.
    /// </summary>
    bool ExecuteAsync { get; }

    /// <summary>
    /// Gets validation rules for this step.
    /// </summary>
    Task<IReadOnlyList<ValidationRule>> GetValidationRulesAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Executes validation step.
    /// </summary>
    Task<ValidationStepResult> ExecuteValidationAsync(
        Document document,
        ValidationContext context,
        CancellationToken ct = default);
}
```

### 2.2 Base Workflow Step Interface

```csharp
/// <summary>
/// Base interface for workflow steps (from v0.7.7).
/// </summary>
public interface IWorkflowStep
{
    /// <summary>Step identifier.</summary>
    string Id { get; }

    /// <summary>Step display name.</summary>
    string Name { get; }

    /// <summary>Step description.</summary>
    string? Description { get; }

    /// <summary>Execution order within workflow.</summary>
    int Order { get; }

    /// <summary>Whether step is enabled.</summary>
    bool IsEnabled { get; }

    /// <summary>Step execution timeout in milliseconds.</summary>
    int? TimeoutMs { get; }

    /// <summary>Executes the step.</summary>
    Task<WorkflowStepResult> ExecuteAsync(
        WorkflowContext context,
        CancellationToken ct = default);

    /// <summary>Validates step configuration.</summary>
    IReadOnlyList<ValidationError> ValidateConfiguration();
}
```

---

## 3. Data Types

### 3.1 Validation Step Type Enum

```csharp
/// <summary>
/// Types of validation steps.
/// </summary>
public enum ValidationStepType
{
    /// <summary>Schema validation (JSON Schema, XML Schema, etc.)</summary>
    Schema = 0,

    /// <summary>Cross-reference validation (links, citations, etc.)</summary>
    CrossReference = 1,

    /// <summary>Content consistency validation (duplicate terms, contradictions)</summary>
    Consistency = 2,

    /// <summary>Custom validation rule execution.</summary>
    Custom = 3,

    /// <summary>Spell and grammar checking.</summary>
    Grammar = 4,

    /// <summary>Knowledge graph alignment validation.</summary>
    KnowledgeGraphAlignment = 5,

    /// <summary>Metadata validation.</summary>
    Metadata = 6
}
```

### 3.2 Validation Failure Action Enum

```csharp
/// <summary>
/// Actions to take when validation fails.
/// </summary>
public enum ValidationFailureAction
{
    /// <summary>Stop workflow execution.</summary>
    Halt = 0,

    /// <summary>Continue with next step.</summary>
    Continue = 1,

    /// <summary>Branch to alternate workflow path.</summary>
    Branch = 2,

    /// <summary>Log and notify but continue.</summary>
    Notify = 3
}
```

### 3.3 Validation Failure Severity Enum

```csharp
/// <summary>
/// Severity levels for validation failures.
/// </summary>
public enum ValidationFailureSeverity
{
    /// <summary>Informational message.</summary>
    Info = 0,

    /// <summary>Warning - proceed with caution.</summary>
    Warning = 1,

    /// <summary>Error - should be addressed.</summary>
    Error = 2,

    /// <summary>Critical - requires immediate action.</summary>
    Critical = 3
}
```

### 3.4 Validation Step Result

```csharp
/// <summary>
/// Result of validation step execution.
/// </summary>
public record ValidationStepResult
{
    /// <summary>Step identifier.</summary>
    public required string StepId { get; init; }

    /// <summary>Whether validation passed.</summary>
    public required bool IsValid { get; init; }

    /// <summary>Validation errors found.</summary>
    public IReadOnlyList<ValidationError> Errors { get; init; } = [];

    /// <summary>Validation warnings found.</summary>
    public IReadOnlyList<ValidationWarning> Warnings { get; init; } = [];

    /// <summary>Rules that were executed.</summary>
    public IReadOnlyList<string> ExecutedRuleIds { get; init; } = [];

    /// <summary>Total items checked.</summary>
    public int ItemsChecked { get; init; }

    /// <summary>Items with issues.</summary>
    public int ItemsWithIssues { get; init; }

    /// <summary>Execution duration in milliseconds.</summary>
    public long ExecutionTimeMs { get; init; }

    /// <summary>Step failure action to take.</summary>
    public ValidationFailureAction FailureAction { get; init; }

    /// <summary>Failure severity level.</summary>
    public ValidationFailureSeverity FailureSeverity { get; init; }

    /// <summary>Metadata about execution.</summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}
```

### 3.5 Validation Context

```csharp
/// <summary>
/// Context for validation execution.
/// </summary>
public record ValidationContext
{
    /// <summary>Workspace ID.</summary>
    public required Guid WorkspaceId { get; init; }

    /// <summary>Document being validated.</summary>
    public required Document Document { get; init; }

    /// <summary>Workflow ID executing validation.</summary>
    public Guid? WorkflowId { get; init; }

    /// <summary>User ID initiating validation.</summary>
    public Guid? UserId { get; init; }

    /// <summary>Trigger for validation.</summary>
    public ValidationTrigger Trigger { get; init; }

    /// <summary>Configuration overrides.</summary>
    public IReadOnlyDictionary<string, object>? ConfigOverrides { get; init; }

    /// <summary>Previously executed step results.</summary>
    public IReadOnlyList<ValidationStepResult>? PreviousResults { get; init; }

    /// <summary>Timestamp of validation start.</summary>
    public DateTime StartTime { get; init; }
}

public enum ValidationTrigger
{
    Manual,
    OnSave,
    PrePublish,
    ScheduledNightly,
    PreWorkflow,
    Custom
}
```

### 3.6 Validation Rule

```csharp
/// <summary>
/// Single validation rule.
/// </summary>
public record ValidationRule
{
    /// <summary>Rule identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Rule display name.</summary>
    public required string Name { get; init; }

    /// <summary>Rule description.</summary>
    public string? Description { get; init; }

    /// <summary>Rule category.</summary>
    public string? Category { get; init; }

    /// <summary>Rule type.</summary>
    public ValidationStepType Type { get; init; }

    /// <summary>Whether rule is enabled.</summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>Rule configuration.</summary>
    public IReadOnlyDictionary<string, object>? Config { get; init; }

    /// <summary>Minimum severity to fail validation.</summary>
    public ValidationFailureSeverity MinimumSeverity { get; init; } = ValidationFailureSeverity.Error;
}
```

---

## 4. Implementation

### 4.1 Validation Workflow Step Implementation

```csharp
public class ValidationWorkflowStep : IValidationWorkflowStep
{
    private readonly IUnifiedValidationService _validationService;
    private readonly ILogger<ValidationWorkflowStep> _logger;

    public string Id { get; }
    public string Name { get; }
    public string? Description { get; }
    public int Order { get; }
    public bool IsEnabled { get; set; }
    public int? TimeoutMs { get; }

    public ValidationStepType StepType { get; }
    public IReadOnlyDictionary<string, object> Options { get; }
    public ValidationFailureAction FailureAction { get; }
    public ValidationFailureSeverity FailureSeverity { get; }
    public bool ExecuteAsync { get; }

    public ValidationWorkflowStep(
        string id,
        string name,
        ValidationStepType stepType,
        IUnifiedValidationService validationService,
        ILogger<ValidationWorkflowStep> logger,
        string? description = null,
        int order = 0,
        int? timeoutMs = null,
        ValidationFailureAction failureAction = ValidationFailureAction.Halt,
        ValidationFailureSeverity failureSeverity = ValidationFailureSeverity.Error,
        bool executeAsync = true,
        IReadOnlyDictionary<string, object>? options = null)
    {
        Id = id;
        Name = name;
        Description = description;
        Order = order;
        TimeoutMs = timeoutMs ?? 30000;
        StepType = stepType;
        FailureAction = failureAction;
        FailureSeverity = failureSeverity;
        ExecuteAsync = executeAsync;
        Options = options ?? new Dictionary<string, object>();

        _validationService = validationService;
        _logger = logger;
    }

    public IReadOnlyList<ValidationError> ValidateConfiguration()
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(Id))
            errors.Add(new ValidationError { Message = "Step ID cannot be empty" });

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add(new ValidationError { Message = "Step name cannot be empty" });

        if (TimeoutMs.HasValue && TimeoutMs.Value <= 0)
            errors.Add(new ValidationError { Message = "Timeout must be positive" });

        return errors;
    }

    public async Task<ValidationStepResult> ExecuteValidationAsync(
        Document document,
        ValidationContext context,
        CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Executing validation step: {StepId} ({StepType})",
                Id, StepType);

            // Get validation rules for this step type
            var rules = await GetValidationRulesAsync(ct);

            if (!rules.Any())
            {
                _logger.LogWarning("No validation rules found for step: {StepId}", Id);
                return new ValidationStepResult
                {
                    StepId = Id,
                    IsValid = true,
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                    FailureAction = FailureAction,
                    FailureSeverity = FailureSeverity
                };
            }

            // Execute validation through service
            var validationRequest = new ValidationRequest
            {
                DocumentId = document.Id,
                WorkspaceId = context.WorkspaceId,
                RuleIds = rules.Where(r => r.IsEnabled).Select(r => r.Id).ToList(),
                StepType = StepType,
                Options = Options,
                Trigger = context.Trigger
            };

            var validationResult = ExecuteAsync
                ? await _validationService.ValidateAsync(validationRequest, ct)
                : await _validationService.ValidateSyncAsync(validationRequest, ct);

            stopwatch.Stop();

            _logger.LogInformation(
                "Validation step {StepId} completed: Valid={Valid}, Errors={ErrorCount}, Time={TimeMs}ms",
                Id, validationResult.IsValid, validationResult.Errors.Count, stopwatch.ElapsedMilliseconds);

            return new ValidationStepResult
            {
                StepId = Id,
                IsValid = validationResult.IsValid,
                Errors = validationResult.Errors,
                Warnings = validationResult.Warnings,
                ExecutedRuleIds = rules.Select(r => r.Id).ToList(),
                ItemsChecked = validationResult.ItemsChecked,
                ItemsWithIssues = validationResult.ItemsWithIssues,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                FailureAction = FailureAction,
                FailureSeverity = FailureSeverity,
                Metadata = new Dictionary<string, object>
                {
                    ["stepType"] = StepType.ToString(),
                    ["ruleCount"] = rules.Count
                }
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Validation step {StepId} was cancelled", Id);
            stopwatch.Stop();
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validation step {StepId} failed", Id);
            stopwatch.Stop();
            throw;
        }
    }

    public async Task<IReadOnlyList<ValidationRule>> GetValidationRulesAsync(
        CancellationToken ct = default)
    {
        var rules = await _validationService.GetRulesForTypeAsync(StepType, ct);
        return rules.ToList();
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
                Message = "Step is disabled"
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

            var result = await ExecuteValidationAsync(
                context.CurrentDocument,
                validationContext,
                linked.Token);

            return new WorkflowStepResult
            {
                StepId = Id,
                Success = result.IsValid || result.FailureAction != ValidationFailureAction.Halt,
                Message = result.IsValid
                    ? $"Validation passed ({result.ItemsChecked} items checked)"
                    : $"Validation failed ({result.Errors.Count} errors, {result.Warnings.Count} warnings)",
                Data = new Dictionary<string, object>
                {
                    ["validationResult"] = result
                }
            };
        }
        catch (OperationCanceledException)
        {
            return new WorkflowStepResult
            {
                StepId = Id,
                Success = false,
                Message = "Validation step timed out"
            };
        }
    }
}
```

### 4.2 Validation Step Factory

```csharp
public class ValidationWorkflowStepFactory
{
    private readonly IUnifiedValidationService _validationService;
    private readonly ILoggerFactory _loggerFactory;

    public ValidationWorkflowStepFactory(
        IUnifiedValidationService validationService,
        ILoggerFactory loggerFactory)
    {
        _validationService = validationService;
        _loggerFactory = loggerFactory;
    }

    public IValidationWorkflowStep CreateStep(
        string id,
        string name,
        ValidationStepType stepType,
        ValidationWorkflowStepOptions options)
    {
        return new ValidationWorkflowStep(
            id: id,
            name: name,
            stepType: stepType,
            validationService: _validationService,
            logger: _loggerFactory.CreateLogger<ValidationWorkflowStep>(),
            description: options.Description,
            order: options.Order,
            timeoutMs: options.TimeoutMs,
            failureAction: options.FailureAction,
            failureSeverity: options.FailureSeverity,
            executeAsync: options.ExecuteAsync,
            options: options.StepOptions);
    }
}

/// <summary>
/// Configuration for creating validation workflow steps.
/// </summary>
public record ValidationWorkflowStepOptions
{
    public string? Description { get; init; }
    public int Order { get; init; } = 0;
    public int? TimeoutMs { get; init; } = 30000;
    public ValidationFailureAction FailureAction { get; init; } = ValidationFailureAction.Halt;
    public ValidationFailureSeverity FailureSeverity { get; init; } = ValidationFailureSeverity.Error;
    public bool ExecuteAsync { get; init; } = true;
    public IReadOnlyDictionary<string, object>? StepOptions { get; init; }
}
```

---

## 5. Execution Flow

```
[Workflow Execution]
        |
        v
[ValidationWorkflowStep.ExecuteAsync]
        |
        +---> [Validate Configuration]
        |
        +---> [Get Validation Rules]
        |
        +---> [Build ValidationRequest]
        |
        +---> [Execute Via IUnifiedValidationService]
        |     (async or sync)
        |
        +---> [Collect Results]
        |
        +---> [Map to ValidationStepResult]
        |
        +---> [Return WorkflowStepResult]
```

---

## 6. Error Handling

| Error | Handling Strategy |
| :---- | :---------------- |
| Invalid step configuration | Return validation errors from ValidateConfiguration() |
| Service unavailable | Log error, return failed result |
| Validation timeout | Cancel with TimeoutMs, return timeout error |
| Cancelled operation | Propagate OperationCanceledException |
| Unknown step type | Return validation error |

---

## 7. Testing Requirements

| Test Case | Description |
| :-------- | :---------- |
| `CreateStep_ValidConfiguration` | Step created successfully |
| `ValidateConfiguration_Invalid` | Invalid configs detected |
| `ExecuteValidation_Passes` | Validation passes correctly |
| `ExecuteValidation_FailsWithHalt` | Failure halts workflow |
| `ExecuteValidation_FailsWithContinue` | Failure continues workflow |
| `ExecuteValidation_Timeout` | Timeout enforced |
| `ExecuteValidation_Cancelled` | Cancellation handled |
| `GetValidationRules_ReturnsRules` | Rules retrieved correctly |

---

## 8. Performance Considerations

| Aspect | Target |
| :------ | :------ |
| Rule retrieval | < 100ms |
| Validation execution | < TimeoutMs (default 30s) |
| Result mapping | < 10ms |
| Memory per step | < 50MB |

---

## 9. License Gating

| Tier | Access |
| :--- | :----- |
| Core | Not available |
| WriterPro | Schema, Grammar steps only |
| Teams | All step types + custom rules |
| Enterprise | Full + unlimited rules + timeouts |

---

## 10. Changelog

| Version | Date | Author | Changes |
| :------ | :--- | :----- | :------ |
| 1.0 | 2026-01-31 | Lead Architect | Initial creation |
