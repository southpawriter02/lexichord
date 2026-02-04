# LCS-DS-v0.18.4a-SEC: Design Specification — Outbound Request Controls

## 1. Document Control

| Field                 | Value                                        |
| :-------------------- | :------------------------------------------- |
| **Document ID**       | LCS-DS-v0.18.4a-SEC                          |
| **Parent SBD**        | LCS-SBD-v0.18.4-SEC                          |
| **Release Version**   | v0.18.4a                                     |
| **Component Name**    | Outbound Request Controls                    |
| **Document Type**     | Design Specification (DS)                    |
| **Author**            | Gemini Architect                             |
| **Created Date**      | 2026-02-04                                   |
| **Last Updated**      | 2026-02-04                                   |
| **Status**            | DRAFT                                        |
| **Classification**    | Internal — Technical Specification           |

---

## 2. Overview

This document provides the detailed design for the **Outbound Request Controls** system (v0.18.4a). This component is the foundational gateway for all network requests originating from the Lexichord platform. It establishes a centralized pipeline through which every outbound HTTP request must pass, be evaluated, and be executed. This control hub is critical for enforcing all other network security policies, including host allow/block lists, data exfiltration prevention, and certificate validation.

---

## 3. Detailed Design

### 3.1. Objective

To implement the core infrastructure for controlling and managing all outbound network requests originating from the Lexichord system. This component will establish a unified request pipeline, a validation framework, and the decision-making logic required to determine if an outbound communication is permitted, all while maintaining high performance and providing clear audit trails.

### 3.2. Scope

-   **Centralized Controller**: Implement a singleton service, `IOutboundRequestController`, as the single point of entry for all outbound HTTP requests.
-   **HTTP Client Integration**: Provide a factory or wrapper for `HttpClient` that ensures all created clients route their requests through the controller.
-   **Request Evaluation Pipeline**: Establish a pipeline within the controller that invokes other security services in a specific order (e.g., Host Policy, Data Exfiltration, etc.).
-   **Secure Execution**: The controller will be responsible for the final execution of the request, but only after it has passed all security checks.
-   **Metadata and Context**: Define and propagate a rich `OutboundRequest` model and a `RequestSecurityContext` to be used by all components in the pipeline.
-   **Performance**: The overhead of the controller's evaluation pipeline must be minimal, with a target of < 10ms average latency before the request is dispatched.

### 3.3. Detailed Architecture

The `OutboundRequestController` will be a singleton service that orchestrates a series of validation steps. It will be injected into any service that needs to make an outbound call. To enforce its use, a custom `IHttpClientFactory` will be implemented to return `HttpClient` instances configured with a custom `HttpMessageHandler` that delegates the request sending to the controller.

```mermaid
graph TD
    subgraph Service Layer
        A[Any Service] --> B(Get HttpClient from Factory);
        B --> C[client.GetAsync(...)];
    end

    subgraph Custom HttpMessageHandler
        C --> D{ControllerMessageHandler};
        D -- Creates OutboundRequest --> E{IOutboundRequestController.ExecuteSecureRequestAsync};
    end

    subgraph OutboundRequestController
        E --> F{1. Call IHostPolicyManager};
        F -- Allowed --> G{2. Call IDataExfiltrationGuard};
        G -- Safe --> H{3. Call IApiKeyVault (if needed)};
        H -- Valid Key --> I{4. Call ICertificateValidator};
        I -- Valid Cert --> J{5. Execute actual HTTP Request};
        J -- Response --> K{6. Call IRequestInspector};
        K --> E;

        F -- Blocked --> L[Block & Log];
        G -- Blocked --> L;
        H -- Invalid Key --> L;
        I -- Invalid Cert --> L;
        L --> E;
    end
```

#### 3.3.1. The Controller Pipeline

The `ExecuteSecureRequestAsync` method will be the heart of the controller. It will execute the validators in a predefined, non-configurable order to ensure security:
1.  **Host Policy Check**: Is the destination host allowed? (Fastest check, fails early).
2.  **Data Exfiltration Check**: Does the request body contain sensitive data?
3.  **API Key Validation**: If the request uses a managed API key, is it valid?
4.  **Certificate Validation**: Is the destination server's TLS certificate trusted and valid?
5.  **Execution**: If all checks pass, execute the HTTP request.
6.  **Inspection**: Log the request and response.

If any step fails, the pipeline is short-circuited, the request is blocked, and the failure is logged.

### 3.4. Data Flow

1.  A service within Lexichord needs to call an external API. It gets an `HttpClient` from the central `IHttpClientFactory`.
2.  The service calls `httpClient.PostAsync(...)`.
3.  The custom `ControllerMessageHandler` intercepts this call. It packages the `HttpRequestMessage` into an `OutboundRequest` object.
4.  The handler calls `IOutboundRequestController.ExecuteSecureRequestAsync` with the `OutboundRequest`.
5.  The controller executes its internal validation pipeline in order.
6.  If all validators pass, the controller uses its own internal, raw `HttpClient` to dispatch the request to the external destination.
7.  The response is received.
8.  The controller passes the request and response to the `IRequestInspector` for logging.
9.  The final `HttpResponseMessage` is returned up the stack to the original service caller.

### 3.5. Interfaces & Records

The primary interface and its related models are defined in the parent SBD. Here is the C# implementation based on that definition:

```csharp
/// <summary>
/// Controls and validates all outbound network requests from Lexichord,
/// serving as the central decision point for all egress traffic.
/// </summary>
public interface IOutboundRequestController
{
    /// <summary>
    /// Executes an outbound request, applying all security controls in the pipeline.
    /// This is the primary method for making secure outbound requests.
    /// </summary>
    /// <param name="request">The request to execute.</param>
    /// <param name="context">The security context for this request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The HTTP response, wrapped with security metadata.</returns>
    Task<SecureHttpResponse> ExecuteSecureRequestAsync(
        OutboundRequest request,
        RequestSecurityContext context,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a structured outbound request for evaluation and execution.
/// </summary>
public record OutboundRequest(
    string Method,
    Uri Destination,
    IReadOnlyDictionary<string, string> Headers,
    byte[] Body,
    string SourceService,
    TimeSpan Timeout);

/// <summary>
/// Represents the security context in which a request is being made.
/// </summary>
public record RequestSecurityContext(
    string ServiceIdentity,
    Guid WorkspaceId,
    bool IsProduction);

/// <summary>
/// Represents a secure HTTP response, including the security evaluation.
/// </summary>
public record SecureHttpResponse(
    HttpStatusCode StatusCode,
    IReadOnlyDictionary<string, string> Headers,
    byte[] Body,
    long ResponseTimeMs,
    SecurityEvaluationSummary SecuritySummary);

/// <summary>
/// A summary of the security checks performed on the request.
/// </summary>
public record SecurityEvaluationSummary(
    bool WasAllowed,
    string? BlockReason,
    IReadOnlyCollection<string> ViolatedPolicies);
```

### 3.6. Error Handling

-   **Validator Failure**: If one of the validator services (e.g., `IHostPolicyManager`) throws an exception, the controller will catch it, log it as a critical error, and default to blocking the request (fail-closed).
-   **Network Errors**: Standard `HttpRequestException`s during the final request execution will be caught, logged by the `IRequestInspector`, and re-thrown or wrapped in a custom exception to be handled by the original calling service.

### 3.7. Security Considerations

-   **Handler Enforcement**: It is critical that all `HttpClient` instances used for external communication are created via the controlled factory. Code reviews and static analysis will be used to enforce this. Direct instantiation of `HttpClient` for external calls must be forbidden.
-   **Context Integrity**: The `RequestSecurityContext` must be constructed reliably. The `SourceService` and `WorkspaceId` must come from a trusted source (e.g., application configuration, user claims) and not be user-spoofable.

### 3.8. Performance Considerations

-   **Pipeline Latency**: The primary performance goal is to keep the total evaluation latency (before dispatching the request) under 10ms. This requires each component in the pipeline to be highly performant (e.g., using caches).
-   **`HttpClient` Management**: The controller's internal `HttpClient` will be a long-lived, static instance to avoid socket exhaustion, following best practices for `HttpClient` usage.

### 3.9. Testing Strategy

-   **Unit Tests**:
    -   Test the controller's pipeline logic. Use mocks for each of the validator interfaces (`IHostPolicyManager`, etc.).
    -   Test the short-circuiting logic: if the first validator fails, ensure the subsequent ones are not called.
    -   Test the `ControllerMessageHandler` to ensure it correctly constructs the `OutboundRequest` object.
-   **Integration Tests**:
    -   Test the full pipeline with an in-memory `IHttpClientFactory`.
    -   Make a call through a created `HttpClient` and verify that all mock validators are called in the correct order.
    -   Test the end-to-end blocking behavior.
-   **Performance Tests**:
    -   Benchmark the `ExecuteSecureRequestAsync` method with all validators returning success to measure the baseline overhead of the pipeline.

---

## 4. Key Artifacts & Deliverables

| Artifact                      | Description                                                              |
| :---------------------------- | :----------------------------------------------------------------------- |
| `IOutboundRequestController`  | The core service interface for the request hub.                          |
| `OutboundRequestController`   | The default implementation of the service with the validation pipeline.  |
| `OutboundRequest`             | The data model for a request.                                            |
| `RequestSecurityContext`      | The data model for the security context.                                 |
| `SecureHttpClientFactory`     | A custom `IHttpClientFactory` implementation.                          |
| `ControllerMessageHandler`    | The custom `HttpMessageHandler` that integrates with the controller.     |
| Developer Documentation       | A guide for developers on how to correctly obtain and use `HttpClient`. |

---

## 5. Acceptance Criteria

- [ ] An `IOutboundRequestController` interface and implementation are created.
- [ ] All `HttpClient` instances intended for external use are created via a factory that injects the `ControllerMessageHandler`.
- [ ] The evaluation of a request (before execution) completes in under 10ms on average.
- [ ] Integration tests successfully demonstrate that a request is blocked if any of the validation steps in the pipeline fail.
- [ ] Performance benchmarks show no more than a 10ms regression on total request time compared to a direct `HttpClient` call.
- [ ] All outbound request attempts (allowed and denied) are logged via the `IRequestInspector`.

---

## 6. Dependencies & Integration Points

-   **ASP.NET Core `IHttpClientFactory`**: The system will integrate deeply with this to control `HttpClient` creation.
-   **`IHostPolicyManager (v0.18.4d)`**: Will be called by the controller.
-   **`IDataExfiltrationGuard (v0.18.4c)`**: Will be called by the controller.
-   **`IApiKeyVault (v0.18.4b)`**: Will be called by the controller.
-   **`ICertificateValidator (v0.18.4f)`**: Will be called by the controller.
-   **`IRequestInspector (v0.18.4e)`**: Will be called by the controller.
-   **All application services**: Any service making an outbound call will be a consumer of this system.
