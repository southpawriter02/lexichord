# LDS-01: Feature Design Specification — Pending Task Extractor

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `CONT-03` | Matches the Roadmap ID. |
| **Feature Name** | Pending Task Extractor | The internal display name. |
| **Target Version** | `v0.7.9l` | The semantic version target. |
| **Module Scope** | `Lexichord.Modules.Agents` | The specific DLL/Project this code lives in. |
| **Swimlane** | Memory | The functional vertical. |
| **License Tier** | Teams | The minimum license required to load this. |
| **Feature Gate Key** | `Agents.Continuity.TaskExtractor` | The string key used in `ILicenseService`. |
| **Author** | Lexichord Architecture | Primary Architect. |
| **Reviewer** | — | Lead Architect / Peer. |
| **Status** | Draft | Current lifecycle state. |
| **Last Updated** | 2026-02-03 | Date of last modification. |

---

## 2. Executive Summary

### 2.1 The Requirement
When preparing a session handoff, the system needs to identify any work in progress that should be continued in the new session. This includes tasks mentioned in conversation, TodoWrite state, and implicit work patterns.

### 2.2 The Proposed Solution
Implement `IPendingTaskExtractor` that analyzes conversation content and integrates with the TodoWrite tool state to extract pending tasks with progress information.

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies
*   **Upstream Modules:**
    *   `Lexichord.Modules.Agents.Continuity` (v0.7.9j models)
    *   `ITodoService` (TodoWrite state)
    *   `IChatCompletionService` (LLM analysis)

### 3.2 Licensing Behavior
*   **Load Behavior:**
    *   [x] **Soft Gate:** Basic extraction for Writer Pro; LLM-assisted for Teams.

---

## 4. Data Contract (The API)

```csharp
namespace Lexichord.Modules.Agents.Continuity.Abstractions;

/// <summary>
/// Extracts pending tasks from conversation content and tool state.
/// </summary>
public interface IPendingTaskExtractor
{
    /// <summary>
    /// Extracts pending tasks for a conversation.
    /// </summary>
    Task<IReadOnlyList<PendingTask>> ExtractPendingTasksAsync(
        string conversationId,
        CancellationToken ct = default);

    /// <summary>
    /// Extracts pending tasks from raw messages.
    /// </summary>
    Task<IReadOnlyList<PendingTask>> ExtractFromMessagesAsync(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken ct = default);

    /// <summary>
    /// Merges tasks from TodoWrite with extracted tasks.
    /// </summary>
    Task<IReadOnlyList<PendingTask>> MergeWithTodoStateAsync(
        IReadOnlyList<PendingTask> extractedTasks,
        string conversationId,
        CancellationToken ct = default);
}
```

---

## 5. Implementation Logic

**Task Status Detection Patterns:**
```csharp
private static readonly Dictionary<TaskStatus, string[]> StatusIndicators = new()
{
    [TaskStatus.InProgress] = new[] { "working on", "currently", "in the middle of", "let me", "I'll now" },
    [TaskStatus.Blocked] = new[] { "waiting for", "blocked by", "need", "requires", "can't proceed" },
    [TaskStatus.AwaitingInput] = new[] { "which would you prefer", "should I", "do you want", "please confirm" },
    [TaskStatus.NearCompletion] = new[] { "almost done", "just need to", "final step", "one more thing" }
};
```

**LLM Extraction Prompt:**
```yaml
template_id: "pending-task-extraction"
system_prompt: |
  Analyze this conversation and identify unfinished tasks.

  For each task, determine:
  1. Description of what needs to be done
  2. Status: not_started, in_progress, blocked, awaiting_input, near_completion
  3. Steps completed and remaining
  4. Progress percentage
  5. Any blocking issues

  Focus on:
  - Code changes mentioned but not yet made
  - Tests that need to be run
  - Files to create or modify
  - Questions not fully answered

  Return as JSON array.
```

**Integration with TodoWrite:**
```csharp
public async Task<IReadOnlyList<PendingTask>> MergeWithTodoStateAsync(
    IReadOnlyList<PendingTask> extractedTasks,
    string conversationId,
    CancellationToken ct)
{
    var todoState = await _todoService.GetTodosAsync(conversationId, ct);
    var mergedTasks = new List<PendingTask>(extractedTasks);

    foreach (var todo in todoState.Where(t => t.Status != "completed"))
    {
        var existing = mergedTasks.FirstOrDefault(t =>
            IsSimilarTask(t.Description, todo.Content));

        if (existing == null)
        {
            mergedTasks.Add(new PendingTask
            {
                TaskId = todo.Id,
                Description = todo.Content,
                Status = MapTodoStatus(todo.Status),
                CompletedSteps = Array.Empty<string>(),
                RemainingSteps = new[] { todo.Content },
                ProgressPercentage = todo.Status == "in_progress" ? 50 : 0
            });
        }
    }

    return mergedTasks;
}
```

---

## 6. Observability & Logging

*   **Metric:** `Agents.Continuity.TaskExtraction.Count` (Histogram)
*   **Metric:** `Agents.Continuity.TaskExtraction.Duration` (Timer)
*   **Log (Info):** `[CONT:TASK] Extracted {Count} pending tasks for conversation {ConversationId}`

---

## 7. Acceptance Criteria (QA)

1.  **[Functional]** `ExtractPendingTasksAsync` SHALL identify in-progress work.
2.  **[Integration]** TodoWrite state SHALL be merged with extracted tasks.
3.  **[Status]** Task status SHALL be inferred from conversation patterns.
4.  **[Progress]** Progress percentage SHALL be estimated for each task.

---

## 8. Test Scenarios

```gherkin
Scenario: Extract task from "I'll fix the bug"
    Given a message "I'll fix the authentication bug in the login service"
    When ExtractFromMessagesAsync is called
    Then a PendingTask SHALL be extracted
    And Status SHALL be InProgress
    And Description SHALL mention "authentication bug"

Scenario: Merge with TodoWrite state
    Given extracted tasks from conversation
    And TodoWrite has 3 incomplete items
    When MergeWithTodoStateAsync is called
    Then all TodoWrite items SHALL be included
    And duplicates SHALL be merged
```
