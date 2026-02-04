# LDS-01: Feature Design Specification — Session Continuity Model

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `CONT-01` | Matches the Roadmap ID. |
| **Feature Name** | Session Continuity Model | The internal display name. |
| **Target Version** | `v0.7.9j` | The semantic version target. |
| **Module Scope** | `Lexichord.Modules.Agents` | The specific DLL/Project this code lives in. |
| **Swimlane** | Memory | The functional vertical. |
| **License Tier** | Writer Pro | The minimum license required to load this. |
| **Feature Gate Key** | `Agents.Continuity.Model` | The string key used in `ILicenseService`. |
| **Author** | Lexichord Architecture | Primary Architect. |
| **Reviewer** | — | Lead Architect / Peer. |
| **Status** | Draft | Current lifecycle state. |
| **Last Updated** | 2026-02-03 | Date of last modification. |

---

## 2. Executive Summary

### 2.1 The Requirement
When conversations approach context window limits, the system needs to seamlessly hand off to a new session without losing work in progress. This requires standardized data structures for capturing session state, pending tasks, and continuation directives.

### 2.2 The Proposed Solution
Define foundational data models for session continuity: `SessionHandoff` for cross-session state transfer, `PendingTask` for unfinished work tracking, `SessionState` for working context, and `ContinuationDirective` for guiding resumed sessions.

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies
*   **Upstream Modules:**
    *   `Lexichord.Modules.Agents.Compression` (v0.7.9a-i)
*   **NuGet Packages:**
    *   None (pure data models)

### 3.2 Licensing Behavior
*   **Load Behavior:**
    *   [x] **Soft Gate:** Models available to Writer Pro; automatic handoffs for Teams.

---

## 4. Data Contract (The API)

```csharp
namespace Lexichord.Modules.Agents.Continuity.Abstractions;

/// <summary>
/// Package for transferring state between sessions when approaching context limits.
/// </summary>
public record SessionHandoff
{
    /// <summary>
    /// Unique identifier for this handoff.
    /// </summary>
    public required string HandoffId { get; init; }

    /// <summary>
    /// The conversation being handed off.
    /// </summary>
    public required string ConversationId { get; init; }

    /// <summary>
    /// Compressed summary of the conversation.
    /// </summary>
    public required string CompactedSummary { get; init; }

    /// <summary>
    /// Anchor points preserved from the original conversation.
    /// </summary>
    public required IReadOnlyList<AnchorPoint> PreservedAnchors { get; init; }

    /// <summary>
    /// Tasks that were in progress when handoff occurred.
    /// </summary>
    public required IReadOnlyList<PendingTask> UnfinishedWork { get; init; }

    /// <summary>
    /// Session state snapshot (files, variables, etc.).
    /// </summary>
    public required SessionState State { get; init; }

    /// <summary>
    /// Metadata about the handoff.
    /// </summary>
    public required HandoffMetadata Metadata { get; init; }
}

/// <summary>
/// Represents work that was in progress when a session ended.
/// </summary>
public record PendingTask
{
    /// <summary>
    /// Unique identifier for the task.
    /// </summary>
    public required string TaskId { get; init; }

    /// <summary>
    /// Human-readable description of the task.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Current status of the task.
    /// </summary>
    public required TaskStatus Status { get; init; }

    /// <summary>
    /// Reason if task is blocked.
    /// </summary>
    public string? BlockingReason { get; init; }

    /// <summary>
    /// Steps that have been completed.
    /// </summary>
    public required IReadOnlyList<string> CompletedSteps { get; init; }

    /// <summary>
    /// Steps remaining to complete the task.
    /// </summary>
    public required IReadOnlyList<string> RemainingSteps { get; init; }

    /// <summary>
    /// Estimated completion percentage (0-100).
    /// </summary>
    public required float ProgressPercentage { get; init; }
}

/// <summary>
/// Status of a pending task.
/// </summary>
public enum TaskStatus
{
    NotStarted = 0,
    InProgress = 1,
    Blocked = 2,
    AwaitingInput = 3,
    NearCompletion = 4
}

/// <summary>
/// Snapshot of session working state.
/// </summary>
public record SessionState
{
    /// <summary>
    /// Variables and values established during the session.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Variables { get; init; }

    /// <summary>
    /// Files that were being worked on.
    /// </summary>
    public IReadOnlyList<string>? ActiveFiles { get; init; }

    /// <summary>
    /// Recent commands executed.
    /// </summary>
    public IReadOnlyList<string>? RecentCommands { get; init; }

    /// <summary>
    /// Current working directory.
    /// </summary>
    public string? CurrentWorkingDirectory { get; init; }

    /// <summary>
    /// Active git branch.
    /// </summary>
    public string? ActiveBranch { get; init; }
}

/// <summary>
/// Metadata about the handoff event.
/// </summary>
public record HandoffMetadata
{
    /// <summary>
    /// When the handoff was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Token count of original conversation.
    /// </summary>
    public required int OriginalTokenCount { get; init; }

    /// <summary>
    /// Token count after compaction.
    /// </summary>
    public required int CompactedTokenCount { get; init; }

    /// <summary>
    /// Compression ratio achieved.
    /// </summary>
    public float CompressionRatio => OriginalTokenCount > 0
        ? (float)OriginalTokenCount / CompactedTokenCount
        : 1.0f;

    /// <summary>
    /// Reason for triggering handoff.
    /// </summary>
    public required string TriggerReason { get; init; }
}

/// <summary>
/// Instructions for the new session to continue work.
/// </summary>
public record ContinuationDirective
{
    /// <summary>
    /// Brief summary of what happened before.
    /// </summary>
    public required string Summary { get; init; }

    /// <summary>
    /// Actions to take immediately in the new session.
    /// </summary>
    public required IReadOnlyList<string> ImmediateActions { get; init; }

    /// <summary>
    /// Important context to remember.
    /// </summary>
    public required IReadOnlyList<string> ContextReminders { get; init; }

    /// <summary>
    /// The user's original intent/goal.
    /// </summary>
    public string? UserIntent { get; init; }
}
```

---

## 5. Acceptance Criteria (QA)

1.  **[Functional]** `SessionHandoff` SHALL contain all required fields for session resumption.
2.  **[Functional]** `PendingTask` SHALL track progress with completed and remaining steps.
3.  **[Functional]** `SessionState` SHALL capture working context (files, directories, branches).
4.  **[Serialization]** All models SHALL serialize/deserialize correctly with JSON.

---

## 6. Test Scenarios

```gherkin
Scenario: SessionHandoff contains all required data
    Given a conversation approaching context limit
    When a handoff is created
    Then CompactedSummary SHALL NOT be empty
    And PreservedAnchors SHALL contain critical decisions
    And UnfinishedWork SHALL list any in-progress tasks

Scenario: PendingTask tracks progress
    Given a task with 5 steps, 3 completed
    Then ProgressPercentage SHALL be 60
    And CompletedSteps.Count SHALL be 3
    And RemainingSteps.Count SHALL be 2
```
