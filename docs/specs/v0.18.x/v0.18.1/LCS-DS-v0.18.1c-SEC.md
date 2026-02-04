# LCS-DS-v0.18.1c-SEC: Design Specification — User Consent Dialog System

## 1. Document Control

| Field                 | Value                                        |
| :-------------------- | :------------------------------------------- |
| **Document ID**       | LCS-DS-v0.18.1c-SEC                          |
| **Parent SBD**        | LCS-SBD-v0.18.1-SEC                          |
| **Release Version**   | v0.18.1c                                     |
| **Component Name**    | User Consent Dialog System                   |
| **Document Type**     | Design Specification (DS)                    |
| **Author**            | Gemini Architect                             |
| **Created Date**      | 2026-02-03                                   |
| **Last Updated**      | 2026-02-03                                   |
| **Status**            | DRAFT                                        |
| **Classification**    | Internal — Technical Specification           |

---

## 2. Overview

This document provides the detailed design for the **User Consent Dialog System** (v0.18.1c). This system is a critical user-facing component of the Permission Framework. It is responsible for presenting permission requests to users in a clear, understandable, and actionable format. It captures the user's explicit consent, including any user-defined modifications to scope or duration, and returns this decision to the permission request pipeline.

---

## 3. Detailed Design

### 3.1. Objective

Implement a comprehensive user consent dialog system that presents permission requests to users in clear, actionable ways, captures user decisions, and provides granular control over scopes and conditions, while ensuring a positive user experience.

### 3.2. Scope

-   Define the `IConsentDialogService` interface, which acts as the bridge between the backend pipeline and the frontend UI.
-   Implement a service that can be called from the `PermissionRequestPipeline` to trigger the display of a consent dialog.
-   Design and implement frontend components (e.g., in React) for various dialog presentation modes (Modal, Sidebar, Banner).
-   Develop UI that dynamically renders permission details (name, description, risk level, warnings) fetched from the `IPermissionRegistry`.
-   Implement UI controls for users to customize the scope (e.g., narrow from "Project" to "File") and set an expiration time for the grant.
-   Display a history of previous decisions for the same permission to provide context to the user.
-   Ensure all UI components are fully accessible (WCAG 2.1 AA) and responsive.

### 3.3. Detailed Architecture

The system is composed of a backend service (`ConsentDialogService`) and frontend components. The backend service acts as an orchestrator, using a signaling mechanism (like WebSockets or Server-Sent Events) to push consent requests to the appropriate client, and a `TaskCompletionSource` to await the user's response.

```mermaid
graph TD
    subgraph Backend
        A[PermissionRequestPipeline] --> B(IConsentDialogService.ShowConsentDialogAsync);
        B --> C{Create TaskCompletionSource<ConsentDecision>};
        C --> D{Store TCS by RequestId};
        D --> E{Send 'ShowDialog' event via SignalR/WebSocket};
        E -- RequestId, DialogModel --> F[Client Frontend];
        G[Receive 'SubmitDecision' event] -- ConsentDecision --> H{Find TCS by RequestId};
        H --> I{Set TCS Result};
        I --> B;
    end

    subgraph Frontend (React Client)
        F --> J[ConsentDialogContainer];
        J --> K{Listen for 'ShowDialog' event};
        K --> L[Render appropriate Dialog Component];
        L --> M{User interacts with UI};
        M -- Clicks 'Grant'/'Deny' --> N{Construct ConsentDecision object};
        N --> O[Send 'SubmitDecision' event to backend];
        O --> G;
    end

    style B fill:#bbf,stroke:#333,stroke-width:2px
```

#### 3.3.1. Communication Flow

1.  The `PermissionRequestPipeline` calls `ShowConsentDialogAsync` on the `IConsentDialogService`.
2.  The service generates a unique `RequestId`, creates a `TaskCompletionSource<ConsentDecision>`, and stores it in a `ConcurrentDictionary` keyed by the `RequestId`.
3.  It then uses a real-time communication channel (e.g., a SignalR hub) to send a message to the specific user's client session, passing the `RequestId` and a `ConsentDialogModel` containing all the necessary information to render the dialog.
4.  The method then `await`s the `Task` from the `TaskCompletionSource`.
5.  On the client, a listener receives the message and uses the `ConsentDialogModel` to render the consent dialog.
6.  The user makes a decision. The client constructs a `ConsentDecision` object.
7.  The client sends the `ConsentDecision` and `RequestId` back to the server via the real-time channel.
8.  The server receives the decision, finds the corresponding `TaskCompletionSource`, and calls `SetResult()` with the `ConsentDecision`.
9.  The `await` in `ShowConsentDialogAsync` completes, and the decision is returned to the pipeline.

### 3.4. Data & UI Models

#### 3.4.1. Interfaces & Records (Backend)

```csharp
/// <summary>
/// Service for rendering permission consent dialogs and capturing user decisions.
/// </summary>
public interface IConsentDialogService
{
    /// <summary>
    /// Presents a permission request dialog to the user and awaits their decision.
    /// </summary>
    /// <param name="request">The permission request details.</param>
    /// <param name="permissionType">The metadata for the permission being requested.</param>
    /// <param name="mode">The desired presentation mode for the dialog.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user's consent decision.</returns>
    Task<ConsentDecision> ShowConsentDialogAsync(
        PermissionRequest request,
        PermissionType permissionType,
        DialogPresentationMode mode = DialogPresentationMode.Modal,
        CancellationToken cancellationToken = default);

    // Other methods like ShowBatchConsentDialogAsync...
}

/// <summary>
/// Represents a user's choice and any customizations for a permission request.
/// </summary>
public record ConsentDecision(
    string DecisionId,
    string RequestId,
    string UserId,
    string PermissionId,
    ConsentChoice Choice,
    DateTimeOffset DecidedAt,
    string? ScopedResourceId = null,
    PermissionScopeType? ScopedScopeType = null,
    DateTimeOffset? ExpiresAt = null,
    Dictionary<string, object>? Conditions = null,
    string? Notes = null);

/// <summary>
/// The user's choice for a permission.
/// </summary>
public enum ConsentChoice
{
    Pending = 0,
    Granted = 1,
    Denied = 2,
    GrantedOnce = 3, // For a single action or session
    DeniedOnce = 4
}

/// <summary>
/// How the consent dialog should be presented to the user.
/// </summary>
public enum DialogPresentationMode
{
    Modal = 0,        // Blocks UI until a decision is made.
    Sidebar = 1,      // Appears in a side panel.
    Banner = 2,       // A non-intrusive banner at the top/bottom of the screen.
    Notification = 3, // A toast-style notification.
    InlineAlert = 4   // An alert within the flow of the content.
}

/// <summary>
/// A view model sent to the client to render the consent dialog.
/// </summary>
public record ConsentDialogModel(
    PermissionRequest Request,
    PermissionType Permission,
    PermissionExplanation Explanation,
    IReadOnlyCollection<ConsentDecision>? DecisionHistory = null,
    DialogPresentationMode PresentationMode = DialogPresentationMode.Modal,
    bool AllowScopeCustomization = true,
    bool AllowExpiryCustomization = true);

/// <summary>
/// User-friendly text explaining the permission.
/// </summary>
public record PermissionExplanation(
    string PermissionId,
    string SimpleSummary,
    string DetailedDescription,
    string? RiskWarning = null,
    IReadOnlyCollection<string>? Examples = null,
    IReadOnlyCollection<string>? Alternatives = null);
```

#### 3.4.2. UI Component State (Frontend - e.g., React)
```typescript
interface ConsentDialogProps {
  model: ConsentDialogModel;
  onDecide: (decision: ConsentDecision) => void;
  onDismiss: (requestId: string) => void;
}

interface ConsentDialogState {
  selectedChoice: ConsentChoice;
  customScopeType: PermissionScopeType | null;
  customScopeResource: string | null;
  customExpiry: Date | null;
  notes: string;
}
```

### 3.5. Error Handling

-   **Client Disconnect**: If the client disconnects before making a decision, the `CancellationToken` on the `ShowConsentDialogAsync` method (linked to the client's session) will be triggered. The `TaskCompletionSource` will be cancelled, propagating a `TaskCanceledException` which the pipeline will interpret as a "Denied" decision.
-   **Request Timeout**: If the user does not respond within a configurable timeout (e.g., 5 minutes), a timer on the server will cancel the `TaskCompletionSource`, leading to a "Denied" decision.
-   **Invalid Decision from Client**: If the client sends back a malformed `ConsentDecision`, the server will reject it, log a security warning, and treat the request as "Denied".

### 3.6. Security Considerations

-   **Authentication**: The real-time communication channel must be securely authenticated and authorized, ensuring that decisions can only be submitted by the user to whom the request was sent.
-   **Cross-Site Request Forgery (CSRF)**: Standard anti-CSRF tokens and policies must be applied to the communication channel.
-   **Data Validation**: The server must rigorously validate the `ConsentDecision` object received from the client. It should never trust the client to send back valid data (e.g., it must re-verify that the user is allowed to select a certain scope).
-   **UI Redressing (Clickjacking)**: The frontend application should employ anti-clickjacking measures (e.g., `X-Frame-Options` header) to prevent the dialog from being rendered in a malicious iframe.

### 3.7. Performance & UX Considerations

-   **Responsiveness**: The dialog must render quickly (< 100ms) after the client receives the signal from the server.
-   **Clarity**: The UI must be exceptionally clear and concise. Technical jargon should be avoided. The "What this means" and "Risk Warning" sections are crucial.
-   **Friction**: For low-risk, common permissions, a less intrusive `Banner` or `Notification` mode should be used to reduce user friction. High-risk permissions should always use a blocking `Modal` dialog.
-   **Batching**: The `ShowBatchConsentDialogAsync` method is essential for scenarios where an action requires multiple permissions, preventing the user from being bombarded with a series of individual dialogs.

### 3.8. Testing Strategy

-   **Backend Unit Tests**:
    -   Test the `ConsentDialogService` logic, ensuring `TaskCompletionSource` is managed correctly.
    -   Test timeout and cancellation logic.
-   **Frontend Unit Tests (e.g., with Jest/React Testing Library)**:
    -   Test that the dialog component renders correctly for different `ConsentDialogModel` inputs.
    -   Test user interactions (clicking buttons, changing scope).
    -   Test that the correct `ConsentDecision` object is constructed and sent.
-   **End-to-End (E2E) Tests (e.g., with Cypress or Playwright)**:
    -   Simulate the full flow: trigger a request from the backend, assert the dialog appears on the client, interact with the dialog, and verify that the backend receives the correct decision.
    -   Test all presentation modes (`Modal`, `Banner`, etc.).
    -   Test responsiveness on different screen sizes.
    -   Test accessibility with automated tools (e.g., Axe).

---

## 4. Key Artifacts & Deliverables

| Artifact                      | Description                                                                    |
| :---------------------------- | :----------------------------------------------------------------------------- |
| `IConsentDialogService`       | The backend service interface for showing dialogs.                             |
| `ConsentDialogService`        | The default implementation using a real-time signaling channel.                |
| `ConsentDecision`             | C# record capturing the user's decision.                                       |
| `ConsentDialogModel`          | C# record acting as the view model for the frontend.                           |
| React Components              | A suite of React components for rendering the various dialogs and controls.    |
| E2E Tests                     | Automated tests verifying the full client-server interaction.                  |

---

## 5. Acceptance Criteria

-   [ ] The consent dialog renders on the client within 200ms of the pipeline initiating the request.
-   [ ] All permission types have clear, user-friendly explanations, and the risk level dynamically adjusts the visual emphasis and warnings in the UI.
-   [ ] Users can successfully customize the scope and set an expiry time for a permission grant via the UI, and this information is correctly captured in the `ConsentDecision`.
-   [ ] The UI displays a history of up to 5 previous decisions for the same permission, providing context to the user.
-   [ ] A batch consent dialog is implemented and can support up to 10 simultaneous permission requests in a single, consolidated UI.
-   [ ] An automated accessibility audit passes the WCAG 2.1 AA standard for all dialog components.
-   [ ] The dialog's layout is responsive and tested on standard mobile and desktop viewport sizes.
-   [ ] E2E tests are in place to verify the entire dialog rendering and decision capture flow.

---

## 6. Dependencies & Integration Points

### 6.1. Dependencies
-   **`v0.18.1a` (Permission Registry)**: To fetch explanations and metadata.
-   **`v0.18.1b` (Permission Request Pipeline)**: The pipeline is the primary consumer of this service.
-   **Real-time Communication Library**: (e.g., SignalR, Socket.IO) for client-server communication.
-   **Frontend Framework**: (e.g., React) for UI components.

### 6.2. Integration Points
-   **`IPermissionRequestPipeline`**: Calls `IConsentDialogService` when user consent is the next step.
-   **`IPermissionGrantStore` (from v0.18.1e)**: The decision captured by this service will be used to create records in the grant store.
-   **Client-side Application**: The frontend application must host the listener for the real-time events and the container for rendering the dialogs.
