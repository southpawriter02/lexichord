# LCS-DS-v0.18.1b-SEC: Design Specification — Permission Request Pipeline

## 1. Document Control

| Field                 | Value                                        |
| :-------------------- | :------------------------------------------- |
| **Document ID**       | LCS-DS-v0.18.1b-SEC                          |
| **Parent SBD**        | LCS-SBD-v0.18.1-SEC                          |
| **Release Version**   | v0.18.1b                                     |
| **Component Name**    | Permission Request Pipeline                  |
| **Document Type**     | Design Specification (DS)                    |
| **Author**            | Gemini Architect                             |
| **Created Date**      | 2026-02-03                                   |
| **Last Updated**      | 2026-02-03                                   |
| **Status**            | DRAFT                                        |
| **Classification**    | Internal — Technical Specification           |

---

## 2. Overview

This document provides the detailed design for the **Permission Request Pipeline** (v0.18.1b). This component is a core part of the Lexichord Permission Framework, providing an asynchronous and extensible pipeline for processing all permission requests. It orchestrates validation, caching, user consent, and auditing, ensuring that all requests are handled consistently and securely.

---

## 3. Detailed Design

### 3.1. Objective

Implement an asynchronous, extensible pipeline for requesting permissions, validating requests, and managing the request lifecycle from initiation through to a final grant or denial decision.

### 3.2. Scope

-   Define the `IPermissionRequestPipeline` interface supporting asynchronous request processing.
-   Implement a middleware-based architecture, allowing for the injection of cross-cutting concerns like logging, caching, and validation.
-   Implement a robust validation middleware to check for request syntax, permission existence (via `IPermissionRegistry`), and scope validity.
-   Implement a caching middleware to reduce user prompt fatigue by caching recent decisions.
-   Orchestrate the hand-off to the `IConsentDialogService` when user interaction is required.
-   Implement comprehensive audit logging for every stage of the pipeline.

### 3.3. Detailed Architecture

The pipeline is designed using the chain-of-responsibility pattern, implemented with a series of middleware components. Each middleware can inspect the request, perform an action, and either terminate the request (e.g., return a cached response) or pass it to the next middleware in the chain.

```mermaid
graph TD
    A[Start: ProcessAsync(request, context)] --> B[Logging Middleware];
    B --> C[Validation Middleware];
    C -- Valid --> D[Caching Middleware];
    C -- Invalid --> Z[End: Return Denied];
    D -- Cache Miss --> E[Existing Grant Check Middleware];
    D -- Cache Hit --> Z[End: Return Cached Decision];
    E -- No Active Grant --> F[Consent Middleware];
    E -- Grant Found --> Z[End: Return Granted];
    F --> G[IConsentDialogService];
    G --> H{User Decision};
    H -- Granted --> I[Create Grant Middleware];
    H -- Denied --> J[Audit & Deny Middleware];
    I --> K[IPermissionGrantStore];
    J --> Z;
    K --> Z[End: Return Granted];

    style Z fill:#f9f,stroke:#333,stroke-width:2px;
```

#### 3.3.1. Middleware Chain

1.  **Logging Middleware**: Logs the incoming request details.
2.  **Validation Middleware**:
    -   Injects `IPermissionRequestValidator`.
    -   Validates the request's `PermissionId` against the `IPermissionRegistry`.
    -   Validates the scope and context.
    -   If invalid, terminates the pipeline and returns a "Denied" response.
3.  **Caching Middleware**:
    -   Checks a distributed cache (e.g., Redis) for a recent decision for the same user, permission, and scope.
    -   If a non-expired decision is found, it terminates the pipeline and returns the cached response.
4.  **Existing Grant Check Middleware**:
    -   Queries `IPermissionGrantStore` to see if an active grant already exists that satisfies the request.
    -   If a valid grant is found, it terminates the pipeline and returns a "Granted" response.
5.  **Consent Middleware (Final Middleware)**:
    -   This is the final step if no other middleware has terminated the request.
    -   It invokes `IConsentDialogService.ShowConsentDialogAsync()` to prompt the user.
    -   Based on the user's `ConsentDecision`, it proceeds accordingly.
6.  **Create Grant Middleware**:
    -   Triggered after a "Granted" decision from the user.
    -   Calls `IPermissionGrantStore.CreateGrantAsync()` to persist the new grant.
7.  **Audit & Deny Middleware**:
    -   Triggered after a "Denied" decision.
    -   Logs the denial reason to the audit trail.

### 3.4. Data Flow

1.  A service calls `IPermissionRequestPipeline.ProcessAsync(request, context)`.
2.  The `request` object flows through each middleware in the defined order.
3.  The `ValidationMiddleware` calls `IPermissionRegistry` to get `PermissionType`.
4.  The `CachingMiddleware` communicates with a distributed cache service.
5.  The `ExistingGrantCheckMiddleware` communicates with `IPermissionGrantStore`.
6.  If the request reaches the `ConsentMiddleware`, it calls out to the `IConsentDialogService`, which is responsible for the UI interaction. This is a long-running, awaitable task.
7.  The `ConsentDecision` is returned to the pipeline.
8.  If granted, a `PermissionGrant` object is created and sent to `IPermissionGrantStore`.
9.  A final `PermissionRequestResponse` is constructed and returned to the original caller.

### 3.5. Interfaces & Records

```csharp
/// <summary>
/// Pipeline for processing permission requests with async middleware support.
/// </summary>
public interface IPermissionRequestPipeline
{
    /// <summary>
    /// Processes a permission request through the pipeline.
    /// </summary>
    /// <param name="request">The permission request to process.</param>
    /// <param name="context">The context in which the request is made.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the permission request (granted, denied, or escalated).</returns>
    Task<PermissionRequestResponse> ProcessAsync(
        PermissionRequest request,
        PermissionRequestContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a middleware component in the pipeline, building the chain of responsibility.
    /// </summary>
    IPermissionRequestPipeline Use(IPermissionRequestMiddleware middleware);
}

/// <summary>
/// Represents a single piece of middleware in the permission request pipeline.
/// </summary>
public interface IPermissionRequestMiddleware
{
    /// <summary>
    /// Invokes the middleware logic.
    /// </summary>
    /// <param name="request">The permission request.</param>
    /// <param name="context">The request context.</param>
    /// <param name="next">A delegate to the next middleware in the pipeline.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<PermissionRequestResponse> InvokeAsync(
        PermissionRequest request,
        PermissionRequestContext context,
        Func<Task<PermissionRequestResponse>> next,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a request for a specific permission, initiated by an agent or service.
/// </summary>
public record PermissionRequest(
    string RequestId,
    string PermissionId,
    string UserId,
    string SessionId,
    string? ResourceId = null,
    PermissionScopeType ScopeType = PermissionScopeType.Global,
    DateTimeOffset RequestedAt = default,
    string? Justification = null,
    Dictionary<string, object>? AdditionalContext = null)
{
    public PermissionRequest() : this(
        Guid.NewGuid().ToString(),
        string.Empty,
        string.Empty,
        string.Empty) { }
}

/// <summary>
/// Provides contextual information for processing a permission request.
/// </summary>
public record PermissionRequestContext(
    string UserId,
    string SessionId,
    string? ProjectId = null,
    string? DocumentId = null,
    string? ImpersonatedUserId = null,
    Dictionary<string, object>? ContextData = null,
    DateTimeOffset ProcessedAt = default);

/// <summary>
/// Represents the final response to a permission request after processing through the pipeline.
/// </summary>
public record PermissionRequestResponse(
    string RequestId,
    PermissionRequestDecision Decision,
    string? GrantId = null,
    DateTimeOffset? ExpiresAt = null,
    string? DenialReason = null,
    string? EscalationReason = null)
{
    public bool IsGranted => Decision == PermissionRequestDecision.Granted;
    public bool IsDenied => Decision == PermissionRequestDecision.Denied;
    public bool IsEscalated => Decision == PermissionRequestDecision.Escalated;
}

/// <summary>
/// Defines the possible outcomes for a permission request.
/// </summary>
public enum PermissionRequestDecision
{
    Pending = 0,
    Granted = 1,
    Denied = 2,
    Escalated = 3,
    Expired = 4,
    Revoked = 5
}
```

### 3.6. Error Handling

-   **Invalid `PermissionId`**: The `ValidationMiddleware` will catch this, log the error, and return a `PermissionRequestResponse` with a `Denied` decision and a reason of "Invalid permission".
-   **Upstream Service Failure**: If `IPermissionRegistry` or `IPermissionGrantStore` throws an exception, the pipeline will catch it, log the exception, and return a `Denied` response with a reason of "Internal server error". This ensures the pipeline is resilient and fails securely.
-   **Consent Dialog Failure**: If the `IConsentDialogService` fails, the pipeline will assume a denial to err on the side of caution.
-   **Timeout**: The `ProcessAsync` method will honor the `CancellationToken`. If a timeout occurs during the user consent phase, the request will be considered denied.

### 3.7. Security Considerations

-   **Immutable Context**: The `PermissionRequest` and `PermissionRequestContext` records are immutable, preventing middleware from tampering with the request as it flows through the pipeline.
-   **Fail-Closed Principle**: The default behavior in case of any exception or ambiguity is to deny the permission request.
-   **Audit Trail**: Every request, and the decision made by each middleware, will be logged to an audit trail for forensic analysis.
-   **Caching**: Cached decisions must be stored securely, especially if they pertain to sensitive permissions. The cache key will be a cryptographic hash of the user ID, permission ID, and scope to prevent enumeration.

### 3.8. Performance Considerations

-   **Overhead**: The pipeline introduces some overhead. Each middleware adds a small amount of latency. The goal is for the entire pipeline (excluding user consent time and database calls) to execute in under 20ms.
-   **Caching**: The decision cache is critical for performance and user experience. The cache TTL will be configurable, likely defaulting to 5-10 minutes for "Granted" decisions and a shorter 1 minute for "Denied" decisions.
-   **Asynchronicity**: All long-running operations (I/O for caching, database access, user consent) are fully asynchronous to prevent blocking threads.

### 3.9. Testing Strategy

-   **Unit Tests**:
    -   Test each middleware in isolation, mocking its dependencies.
    -   Verify that the `ValidationMiddleware` correctly identifies valid and invalid requests.
    -   Verify that the `CachingMiddleware` correctly returns cached responses and proceeds to the next middleware on a cache miss.
    -   Test the pipeline's composition logic (`Use` method).
-   **Integration Tests**:
    -   Test the entire pipeline with a mock `IConsentDialogService`.
    -   Verify the end-to-end flow for a granted permission, including the creation of the grant in a test database.
    -   Verify the end-to-end flow for a denied permission.
    -   Test the caching behavior with a real cache provider (e.g., in-memory or Redis).

---

## 4. Key Artifacts & Deliverables

| Artifact                       | Description                                                          |
| :----------------------------- | :------------------------------------------------------------------- |
| `IPermissionRequestPipeline`   | The core pipeline interface.                                         |
| `PermissionRequestPipeline`    | The default implementation of the pipeline.                          |
| `IPermissionRequestMiddleware` | The interface for all pipeline middleware components.                |
| `PermissionRequest`            | Record representing a permission request.                            |
| `PermissionRequestContext`     | Record providing context for the request.                            |
| `PermissionRequestResponse`    | Record representing the final outcome.                               |
| Unit & Integration Tests       | Comprehensive tests ensuring the pipeline's correctness and resilience. |

---

## 5. Acceptance Criteria

-   [ ] `IPermissionRequestPipeline` interface is defined with full asynchronous support and documented with XML comments.
-   [ ] The pipeline processes requests (excluding user decision time and I/O) within 100ms.
-   [ ] The middleware architecture is implemented and allows for easy extension.
-   [ ] The `ValidationMiddleware` correctly identifies and rejects invalid permission requests.
-   [ ] A complete audit log is generated for each request, capturing the request details, context, and final decision.
-   [ ] The recent decision caching mechanism is implemented and reduces duplicate user prompts for the same permission within a 2-hour window.
-   [ ] Critical permissions (as defined by `PermissionRiskLevel`) are automatically escalated for review if not already granted.
-   [ ] Unit tests cover all validation scenarios and each middleware's logic.
-   [ ] Integration tests verify the end-to-end request processing flow, including interaction with the cache and grant store.

---

## 6. Dependencies & Integration Points

### 6.1. Dependencies
-   **`v0.18.1a` (Permission Registry)**: Used by the `ValidationMiddleware`.
-   **`Lexichord.Abstractions` (v0.0.3b)**
-   **`Microsoft.Extensions.Logging`**

### 6.2. Integration Points
-   **`IPermissionRegistry` (from v0.18.1a)**: Consumed to validate permissions.
-   **`IConsentDialogService` (from v0.18.1c)**: Invoked to get user consent.
-   **`IPermissionScopeManager` (from v0.18.1d)**: Consumed to evaluate and validate scopes.
-   **`IPermissionGrantStore` (from v0.18.1e)**: Consumed to check for existing grants and to store new ones.
