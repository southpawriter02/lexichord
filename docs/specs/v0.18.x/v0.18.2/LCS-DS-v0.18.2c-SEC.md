# LCS-DS-v0.18.2c-SEC: Design Specification — Approval Queue & UI

## 1. Document Control

| Field                 | Value                                        |
| :-------------------- | :------------------------------------------- |
| **Document ID**       | LCS-DS-v0.18.2c-SEC                          |
| **Parent SBD**        | LCS-SBD-v0.18.2-SEC                          |
| **Release Version**   | v0.18.2c                                     |
| **Component Name**    | Approval Queue & UI                          |
| **Document Type**     | Design Specification (DS)                    |
| **Author**            | Gemini Architect                             |
| **Created Date**      | 2026-02-03                                   |
| **Last Updated**      | 2026-02-03                                   |
| **Status**            | DRAFT                                        |
| **Classification**    | Internal — Technical Specification           |

---

## 2. Overview

This document provides the detailed design for the **Approval Queue & UI** (v0.18.2c). This component provides the critical human-in-the-loop checkpoint for the Command Sandbox. After a command is classified with a risk level that requires manual review, it is placed into an approval queue. This system manages the queue, routes requests to the correct reviewers, and provides the user interface for review, approval, or denial.

---

## 3. Detailed Design

### 3.1. Objective

To implement a robust, real-time approval workflow system with a clear, actionable UI that allows human reviewers to safely inspect, approve, or deny potentially dangerous commands before they are executed.

### 3.2. Scope

-   Define an `IApprovalQueue` service for managing the lifecycle of approval requests.
-   Implement a backend service that persists approval requests to a PostgreSQL database.
-   Design a risk-based routing system that can assign requests to specific users or roles (e.g., `SAFE` is auto-approved, `MEDIUM` requires one developer, `CRITICAL` requires two senior admins).
-   Develop a real-time UI using WebSockets (e.g., SignalR) that displays pending requests to authorized reviewers.
-   Create a detailed **Command Approval Dialog** UI component that shows the command, its risk analysis, and provides clear approve/deny actions.
-   Build an **Approval Queue Dashboard** for managing and auditing all requests.
-   Implement an audit trail for all approval decisions.

### 3.3. Detailed Architecture

The system follows a similar client-server pattern to the Consent Dialog system, but is more complex, involving a persistent queue and role-based routing.

```mermaid
graph TD
    A[RiskClassifier] -- Risk > SAFE --> B{Submit to IApprovalQueue};
    
    subgraph ApprovalQueue Service
        B --> C{1. Create 'ApprovalQueueEntry' in DB (status: PENDING)};
        C --> D{2. Determine required approvers based on risk};
        D --> E{3. Push 'NewApprovalRequest' event via WebSocket};
    end
    
    E -- To specific User/Role clients --> F[Frontend Clients];

    subgraph Reviewer's UI
        F --> G{Listen for 'NewApprovalRequest'};
        G --> H[Update Queue Dashboard];
        H -- Reviewer clicks item --> I[Show Approval Dialog];
        I -- Reviewer clicks Approve/Deny --> J{Send 'SubmitDecision' event};
    end
    
    J -- decision, reason --> K[Backend Hub];

    subgraph ApprovalQueue Service
        K --> L{Receive Decision};
        L --> M{2a. For multi-review, check if all have approved};
        M -- No --> N{Update DB entry, await more reviews};
        M -- Yes --> O{2b. Update DB entry (status: APPROVED/DENIED)};
        O --> P{3. Notify Command Sandbox to proceed/halt};
    end
```

#### 3.3.1. Multi-Reviewer Logic

For commands requiring multiple approvals (e.g., `CRITICAL` risk), the system will:
1.  Store each individual decision.
2.  Only change the final status of the `ApprovalQueueEntry` to `APPROVED` when the required number of unique, authorized reviewers have approved it.
3.  If any single reviewer denies the request, the status is immediately set to `DENIED`, and the process halts.

#### 3.3.2. Real-Time Communication

WebSockets are essential for this component.
-   When a new command requires approval, the server pushes an event to all connected clients belonging to the required reviewer role(s). This makes the dashboard update in real-time.
-   When a command is approved or denied by one reviewer, an event is pushed to all other reviewers looking at the same item, indicating that it has been actioned and removing it from their active queue.

### 3.4. Interfaces & Data Models

```csharp
/// <summary>
/// Manages the command approval workflow, including queueing requests and processing decisions.
/// </summary>
public interface IApprovalQueue
{
    /// <summary>
    /// Submits a classified command to the approval queue.
    /// </summary>
    /// <param name="command">The parsed command.</param>
    /// <param name="classification">The risk classification for the command.</param>
    /// <param name="submitter">The user who submitted the command.</param>
    /// <returns>The created approval queue entry.</returns>
    Task<ApprovalQueueEntry> SubmitForApprovalAsync(ParsedCommand command, RiskClassification classification, User submitter);

    /// <summary>
    /// Processes a decision (approve/deny) from a reviewer.
    /// </summary>
    /// <param name="approvalId">The ID of the approval entry.</param>
    /// <param name="reviewer">The user making the decision.</param>
    /// <param name="isApproved">True for approve, false for deny.</param>
    /// <param name="reason">An optional justification or reason for the decision.</param>
    /// <returns>The updated state of the approval entry.</returns>
    Task<ApprovalQueueEntry> ProcessDecisionAsync(Guid approvalId, User reviewer, bool isApproved, string reason);

    /// <summary>
    /// Retrieves all approval entries currently pending for a specific user or their roles.
    /// </summary>
    Task<IEnumerable<ApprovalQueueEntry>> GetPendingApprovalsForUserAsync(User user);
}

/// <summary>
/// Represents a single command request awaiting approval in the database.
/// </summary>
public record ApprovalQueueEntry(
    Guid Id,
    Guid CommandExecutionId,
    RiskCategory Risk,
    ApprovalStatus Status,
    User Submitter,
    IReadOnlyList<ApprovalDecision> Decisions,
    int RequiredApproverCount,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? Deadline);

/// <summary>
/// The status of a request in the approval queue.
/// </summary>
public enum ApprovalStatus
{
    Pending,
    Approved,
    Denied,
    Expired,
    Escalated
}

/// <summary>
/// Represents the decision of a single reviewer.
/// </summary>
public record ApprovalDecision(
    User Reviewer,
    bool IsApproved,
    string Reason,
    DateTimeOffset DecidedAt);
```

### 3.5. Database Schema

```sql
-- Approval Queue Table (v0.18.2c)
CREATE TABLE approval_queue (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    command_execution_id UUID NOT NULL REFERENCES command_executions(id) ON DELETE CASCADE,
    risk_classification VARCHAR(20) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'PENDING',
    submitter_id UUID NOT NULL REFERENCES users(id),
    required_approver_count INTEGER NOT NULL DEFAULT 1,
    deadline TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Approval Decisions Table
CREATE TABLE approval_decisions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    approval_queue_id UUID NOT NULL REFERENCES approval_queue(id) ON DELETE CASCADE,
    reviewer_id UUID NOT NULL REFERENCES users(id),
    is_approved BOOLEAN NOT NULL,
    reason TEXT,
    decided_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (approval_queue_id, reviewer_id) -- A user can only vote once per request
);
```

### 3.6. Error Handling

-   **Invalid Approver**: The `ProcessDecisionAsync` method will validate that the `reviewer` has the necessary role/permission to approve a request of that risk level. If not, it will throw an `UnauthorizedAccessException`.
-   **Request Already Actioned**: If two reviewers attempt to action the same request simultaneously, the database transaction will fail for the second one due to a state change. The service will catch this and inform the second reviewer that the request has already been handled.
-   **Client Disconnect**: The system is stateless from the client's perspective. If a client disconnects, the request simply remains in the queue. The UI will re-fetch the current queue state upon reconnection.

### 3.7. Security Considerations

-   **Authentication & Authorization**: The WebSocket hub must be strictly authenticated. The connection and event subscription logic on the server must verify that a user is only receiving events for roles they belong to. When a decision is submitted, the backend must re-verify the user's authority to make that decision.
-   **Sensitive Data in UI**: The approval dialog may display sensitive information (e.g., arguments to a command). The UI must have features to mask sensitive data by default, with a "reveal" button that is audited.
-   **Audit Trail**: The `approval_decisions` table forms an immutable audit trail. No records should ever be deleted from it.

### 3.8. Performance Considerations

-   **Dashboard Load**: The initial query for the dashboard (`GetPendingApprovalsForUserAsync`) could be slow if the queue is large. The query will be heavily optimized with indexes on `(status, approver_role, risk_level)`.
-   **WebSocket Scalability**: For large numbers of reviewers, broadcasting events can be resource-intensive. The WebSocket implementation should use groups/rooms to target events only to relevant users (e.g., users in the "Admins" role) rather than broadcasting to all connected clients.

### 3.9. Testing Strategy

-   **Backend Unit Tests**: Test the routing logic, the multi-reviewer decision aggregation, and the database interaction logic (mocking the DB).
-   **UI Unit Tests**: Test the React components for the dashboard and dialog to ensure they render correctly based on different `ApprovalQueueEntry` states.
-   **E2E Tests**: Use a framework like Cypress or Playwright with two concurrent browser sessions to test the multi-reviewer workflow:
    1.  Admin A and Admin B both open the dashboard.
    2.  A critical command is submitted. Both dashboards update in real-time.
    3.  Admin A approves the command. Admin B's UI updates to show "1 of 2 approvals".
    4.  Admin B approves the command. The command is removed from the queue and executed.

---

## 4. Key Artifacts & Deliverables

| Artifact                  | Description                                                              |
| :------------------------ | :----------------------------------------------------------------------- |
| `IApprovalQueue`          | The core service interface for the approval queue.                       |
| `ApprovalQueueService`    | The default implementation of the service.                               |
| `ApprovalQueueEntry`      | The C# record for a queue entry.                                         |
| Database Schema Scripts   | SQL for the `approval_queue` and `approval_decisions` tables.            |
| Frontend Components       | React components for the dashboard and approval dialog.                  |
| E2E Test Suite            | Automated tests covering real-time UI updates and multi-user workflows. |

---

## 5. Acceptance Criteria

-   [ ] The approval dialog renders within 500ms of a command being submitted for review.
-   [ ] The approval queue dashboard updates in real-time (<1 second latency) via WebSockets when new items are added or existing items are actioned.
-   [ ] The UI handles a queue of 100+ pending commands without any discernible performance degradation.
-   [ ] The multi-reviewer workflow is fully functional: a command is only approved when the required number of unique, authorized users have approved it.
-   [ ] Approval reasons and justifications are correctly persisted and are accessible in the command's audit log.
-   [ ] The system correctly enforces that a user cannot approve a request they themselves submitted.
-   [ ] The UI components are fully responsive and pass a WCAG 2.1 AA accessibility audit.

---

## 6. Dependencies & Integration Points

### 6.1. Dependencies
-   **`v0.18.2b` (Risk Classification Engine)**: The output of this engine determines if a command enters the approval queue.
-   **`v0.18.1-SEC` (Permission Framework)**: Used to determine which roles are authorized to approve requests of different risk levels.
-   **Entity Framework Core / Dapper**: For database persistence.
-   **SignalR / WebSocket Library**: For real-time client-server communication.

### 6.2. Integration Points
-   **`CommandSandbox` Orchestrator**: After classifying a command, the orchestrator will call `IApprovalQueue.SubmitForApprovalAsync`. It will then await a signal from the queue service indicating the final decision before proceeding with execution or termination.
-   **User/Role Management System**: The queue service will need to query the user management system to resolve user roles for routing decisions.
-   **`Command History` (v0.18.2g)**: All decisions made in the approval queue will be logged as part of the command's overall history.
