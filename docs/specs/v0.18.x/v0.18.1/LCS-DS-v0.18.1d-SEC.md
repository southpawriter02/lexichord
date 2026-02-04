# LCS-DS-v0.18.1d-SEC: Design Specification — Permission Scope Manager

## 1. Document Control

| Field                 | Value                                        |
| :-------------------- | :------------------------------------------- |
| **Document ID**       | LCS-DS-v0.18.1d-SEC                          |
| **Parent SBD**        | LCS-SBD-v0.18.1-SEC                          |
| **Release Version**   | v0.18.1d                                     |
| **Component Name**    | Permission Scope Manager                     |
| **Document Type**     | Design Specification (DS)                    |
| **Author**            | Gemini Architect                             |
| **Created Date**      | 2026-02-03                                   |
| **Last Updated**      | 2026-02-03                                   |
| **Status**            | DRAFT                                        |
| **Classification**    | Internal — Technical Specification           |

---

## 2. Overview

This document provides the detailed design for the **Permission Scope Manager** (v0.18.1d). This component is a cornerstone of the Permission Framework, enabling fine-grained control over where and when permissions are applicable. It provides the logic for creating, evaluating, composing, and validating permission scopes, moving beyond simple binary grant/deny decisions to a contextual, resource-aware authorization model.

---

## 3. Detailed Design

### 3.1. Objective

Implement a sophisticated scope manager that allows permissions to be scoped to specific projects, documents, resources, or time windows, with support for scope composition, hierarchical constraints, and fast, context-aware evaluation.

### 3.2. Scope

-   Define the `IPermissionScopeManager` interface for all scope-related operations.
-   Implement a `PermissionScopeManager` service that can create, validate, and evaluate `PermissionScope` objects.
-   Define a flexible `ScopeConstraint` model that can represent different types of constraints (e.g., `ResourceScopeConstraint`, `TimeWindowScopeConstraint`).
-   Implement the evaluation logic that checks a `PermissionScope` against a given `ScopeEvaluationContext`.
-   Support scope composition, allowing multiple `ScopeConstraint` records to be combined with AND/OR logic.
-   Implement scope "narrowing," where a user can be granted a broad scope but have it temporarily narrowed for a specific operation.
-   Provide a set of predefined `ScopeTemplate` records for common use cases (e.g., "Current Project Only," "This Session Only").

### 3.3. Detailed Architecture

The `PermissionScopeManager` is a stateless service that operates on `PermissionScope` objects. These objects are simple data containers (records) that are stored as part of a `PermissionGrant` in the database. The core logic resides in the `EvaluateScopeAsync` method, which iterates through the constraints of a scope and evaluates them against the current context.

```mermaid
graph TD
    A[Permission Check] --> B{Call IPermissionManager.HasPermissionAsync};
    B --> C{Get Active Grants from GrantStore};
    C --> D{For each Grant...};
    D --> E{Call IPermissionScopeManager.EvaluateScopeAsync};
    E -- Grant.Scope, EvaluationContext --> F[PermissionScopeManager];
    F --> G{Iterate over Scope.Constraints};
    G --> H{Evaluate Constraint against Context};
    H -- All constraints match (AND) --> I[Return true];
    H -- One constraint fails (AND) --> J[Return false];
    I --> B;
    J --> D;

    subgraph ScopeEvaluationContext
        ctx1[UserId]
        ctx2[SessionId]
        ctx3[CurrentProjectId]
        ctx4[CurrentDocumentId]
        ctx5[...]
    end
    
    subgraph PermissionScope
        ps1[ScopeId]
        ps2[List<ScopeConstraint>]
        ps3[CompositionMode (AND/OR)]
    end

    ScopeEvaluationContext --> F;
    PermissionScope --> F;

```

#### 3.3.1. Evaluation Logic

The evaluation is a straightforward process:
1.  The `EvaluateScopeAsync` method receives a `PermissionScope` and a `ScopeEvaluationContext`.
2.  It iterates through the `List<ScopeConstraint>` within the scope.
3.  For each `ScopeConstraint`, it performs a `switch` on the constraint type.
4.  The corresponding evaluation is performed:
    -   **`ProjectScopeConstraint`**: `constraint.ProjectId == context.CurrentProjectId`
    -   **`DocumentScopeConstraint`**: `constraint.DocumentId == context.CurrentDocumentId`
    -   **`TimeWindowScopeConstraint`**: `context.EvaluatedAt >= constraint.StartTime && context.EvaluatedAt <= constraint.EndTime`
    -   ...and so on.
5.  The results of each constraint evaluation are aggregated based on the `CompositionMode` (`And` or `Or`).
6.  A final boolean result is returned.

This design is highly efficient as it involves no I/O and operates on simple in-memory objects and comparisons.

### 3.4. Data Model & Interfaces

```csharp
/// <summary>
/// Manages permission scopes, including their creation, composition, and evaluation against a given context.
/// </summary>
public interface IPermissionScopeManager
{
    /// <summary>
    /// Creates a new permission scope object from a collection of constraints.
    /// Note: This method does not persist the scope.
    /// </summary>
    /// <param name="constraints">The collection of constraints defining the scope.</param>
    /// <param name="compositionMode">How the constraints should be combined (AND/OR).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly created, un-persisted PermissionScope object.</returns>
    Task<PermissionScope> CreateScopeAsync(
        IReadOnlyCollection<ScopeConstraint> constraints,
        ScopeCompositionMode compositionMode = ScopeCompositionMode.And,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates whether a permission grant's scope is valid for the given operational context.
    /// </summary>
    /// <param name="scope">The permission scope to evaluate.</param>
    /// <param name="context">The current operational context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the scope applies to the context, otherwise false.</returns>
    Task<bool> EvaluateScopeAsync(
        PermissionScope scope,
        ScopeEvaluationContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Narrows an existing scope by adding more restrictive constraints.
    /// </summary>
    /// <param name="originalScope">The original scope.</param>
    /// <param name="additionalConstraints">The additional, narrowing constraints to apply.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A new, more restrictive PermissionScope object.</returns>
    Task<PermissionScope> NarrowScopeAsync(
        PermissionScope originalScope,
        IReadOnlyCollection<ScopeConstraint> additionalConstraints,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a collection of predefined scope templates for common use cases.
    /// </summary>
    Task<IReadOnlyCollection<ScopeTemplate>> GetScopeTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a scope to ensure its constraints are well-formed.
    /// </summary>
    Task<ScopeValidationResult> ValidateScopeAsync(
        PermissionScope scope,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a permission scope, defined by a set of constraints. This object is persisted as part of a PermissionGrant.
/// </summary>
public record PermissionScope(
    string ScopeId,
    IReadOnlyCollection<ScopeConstraint> Constraints,
    DateTimeOffset CreatedAt,
    ScopeCompositionMode CompositionMode = ScopeCompositionMode.And);

/// <summary>
/// Base record for all scope constraint types, enabling polymorphism.
/// </summary>
public abstract record ScopeConstraint
{
    public abstract string ConstraintType { get; }
}

/// <summary>
/// Constraint that scopes a permission to a specific resource identifier and type.
/// </summary>
public record ResourceScopeConstraint(string ResourceId, string ResourceType) : ScopeConstraint
{
    public override string ConstraintType => "Resource";
}

/// <summary>
/// Constraint that scopes a permission to a specific project.
/// </summary>
public record ProjectScopeConstraint(string ProjectId) : ScopeConstraint
{
    public override string ConstraintType => "Project";
}

/// <summary>
/// Constraint that scopes a permission to a specific document.
/// </summary>
public record DocumentScopeConstraint(string DocumentId) : ScopeConstraint
{
    public override string ConstraintType => "Document";
}

/// <summary>
/// Constraint that scopes a permission to a specific time window.
/// </summary>
public record TimeWindowScopeConstraint(DateTimeOffset StartTime, DateTimeOffset EndTime) : ScopeConstraint
{
    public override string ConstraintType => "TimeWindow";
}

/// <summary>
/// Constraint that scopes a permission to the current user session.
/// </summary>
public record SessionScopeConstraint(string SessionId) : ScopeConstraint
{
    public override string ConstraintType => "Session";
}

/// <summary>
/// Provides the current operational context against which a permission scope is evaluated.
/// </summary>
public record ScopeEvaluationContext(
    string UserId,
    string SessionId,
    DateTimeOffset EvaluatedAt,
    string? CurrentResourceId = null,
    string? CurrentProjectId = null,
    string? CurrentDocumentId = null,
    Dictionary<string, object>? AdditionalContext = null);

/// <summary>
/// Defines how multiple constraints in a scope are logically combined.
/// </summary>
public enum ScopeCompositionMode
{
    And = 0,  // All constraints must be satisfied.
    Or = 1    // Any single constraint must be satisfied.
}

/// <summary>
/// Represents a pre-defined, user-selectable scope pattern.
/// </summary>
public record ScopeTemplate(
    string TemplateId,
    string Name,
    string Description,
    IReadOnlyCollection<ScopeConstraint> Constraints);

/// <summary>
/// The result of a scope validation check.
/// </summary>
public record ScopeValidationResult(bool IsValid, IReadOnlyCollection<string> Errors);
```

### 3.5. Error Handling

-   **Invalid `ScopeEvaluationContext`**: If the context provided to `EvaluateScopeAsync` is missing required fields (e.g., `ProjectId` for a `ProjectScopeConstraint`), the evaluation for that constraint will default to `false`. The operation will not throw an exception but will fail securely.
-   **Invalid `ScopeConstraint`**: The `ValidateScopeAsync` method will be used to check for malformed constraints (e.g., a `TimeWindowScopeConstraint` where `EndTime` is before `StartTime`). The `PermissionGrantStore` will call this before persisting any grant with a scope.
-   **Null Inputs**: All public methods will perform null checks on their arguments and throw `ArgumentNullException` if required parameters are null.

### 3.6. Security Considerations

-   **Context Integrity**: The `ScopeEvaluationContext` is the most critical input. The system must ensure that this context is constructed reliably and cannot be tampered with by the agent or user action being authorized. It should be constructed by the trusted permission-checking kernel, not the code requesting the permission.
-   **Scope Validation**: All scopes created based on user input (via the `ConsentDialog`) must be rigorously validated by `ValidateScopeAsync` before being persisted. The system should not trust that the client-side UI has sent a valid scope definition.
-   **Complexity Attacks**: A scope with an extremely large number of constraints could be used in a denial-of-service attack against the evaluation logic. A hard limit (e.g., 50 constraints per scope) will be enforced to mitigate this.

### 3.7. Performance Considerations

-   **Evaluation Speed**: Scope evaluation is on the critical path for every permission check. The logic is designed to be extremely fast (in-memory comparisons). The target evaluation time for a typical scope is << 1ms.
-   **Object Allocation**: The use of C# `record` types helps minimize allocations. The evaluation process itself should allocate minimal memory.

### 3.8. Testing Strategy

-   **Unit Tests**:
    -   Test each `ScopeConstraint` evaluation logic in isolation.
    -   Test the `PermissionScopeManager.EvaluateScopeAsync` method with various combinations of constraints and `ScopeCompositionMode` (AND/OR).
    -   Test `NarrowScopeAsync` to ensure it correctly combines constraints.
    -   Test `ValidateScopeAsync` with both valid and invalid scopes.
    -   Mock `ScopeEvaluationContext` to simulate various operational contexts.
-   **Integration Tests**:
    -   Integrate with the `PermissionManager` to test the full flow where a permission check triggers a scope evaluation.
    -   Test with scopes retrieved from a test database to ensure serialization/deserialization works correctly.

---

## 4. Key Artifacts & Deliverables

| Artifact                 | Description                                                          |
| :----------------------- | :------------------------------------------------------------------- |
| `IPermissionScopeManager`| The core service interface for scope operations.                     |
| `PermissionScopeManager` | The default, stateless implementation of the service.                |
| `PermissionScope`        | Record representing a persisted scope.                               |
| `ScopeConstraint`        | The hierarchy of record types defining individual constraints.       |
| `ScopeEvaluationContext` | Record providing the context for evaluation.                         |
| Unit Tests               | Comprehensive tests for all evaluation and validation logic.         |

---

## 5. Acceptance Criteria

-   [ ] Scope evaluation completes within 5ms for a scope with up to 10 constraints.
-   [ ] All defined scope constraint types (`Resource`, `Project`, `Document`, `TimeWindow`, `Session`) are fully implemented and validated.
-   [ ] Scope "narrowing" correctly preserves original constraints while adding new ones.
-   [ ] Context-aware evaluation correctly applies time window and session constraints based on the `ScopeEvaluationContext`.
-   [ ] At least 10 scope templates are provided for common use cases (e.g., "This Project," "This Document," "Next 24 hours").
-   [ ] Scope composition logic correctly handles both `AND` and `OR` modes.
-   [ ] The validation logic correctly detects invalid resource IDs, expired time windows, and malformed constraints.
-   [ ] Unit tests cover all constraint types and composition modes with at least 95% code coverage.

---

## 6. Dependencies & Integration Points

### 6.1. Dependencies
-   **`Lexichord.Abstractions` (v0.0.3b)**: For common types.

### 6.2. Integration Points
-   **`IPermissionManager`**: The core permission checking logic will be the primary consumer of `IPermissionScopeManager.EvaluateScopeAsync`.
-   **`IPermissionGrantStore` (from v0.18.1e)**: The `PermissionScope` objects created and validated by this manager will be persisted as part of `PermissionGrant` records in the database. The store will call `ValidateScopeAsync` before saving.
-   **`IConsentDialogService` (from v0.18.1c)**: The consent UI will allow users to select from `ScopeTemplate` records or build a custom `PermissionScope`, which will then be passed to the grant store.
