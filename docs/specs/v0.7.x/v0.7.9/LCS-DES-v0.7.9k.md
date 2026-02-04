# LDS-01: Feature Design Specification — Session Continuity Service

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `CONT-02` | Matches the Roadmap ID. |
| **Feature Name** | Session Continuity Service | The internal display name. |
| **Target Version** | `v0.7.9k` | The semantic version target. |
| **Module Scope** | `Lexichord.Modules.Agents` | The specific DLL/Project this code lives in. |
| **Swimlane** | Memory | The functional vertical. |
| **License Tier** | Teams | The minimum license required to load this. |
| **Feature Gate Key** | `Agents.Continuity.Service` | The string key used in `ILicenseService`. |
| **Author** | Lexichord Architecture | Primary Architect. |
| **Reviewer** | — | Lead Architect / Peer. |
| **Status** | Draft | Current lifecycle state. |
| **Last Updated** | 2026-02-03 | Date of last modification. |

---

## 2. Executive Summary

### 2.1 The Requirement
The system needs an orchestration service to prepare handoffs when approaching context limits, store them for retrieval, and generate continuation directives for resumed sessions. This includes real-time token tracking to know when to trigger handoffs.

### 2.2 The Proposed Solution
Implement `IConversationTokenTracker` for real-time context usage monitoring and `ISessionContinuityService` for handoff preparation and resumption. The service detects context limits at 85% utilization and prepares seamless handoffs.

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies
*   **Upstream Modules:**
    *   `Lexichord.Modules.Agents.Compression` (v0.7.9a-i)
    *   `Lexichord.Modules.Agents.Continuity` (v0.7.9j models)
    *   `ITokenCounter` (existing)
*   **NuGet Packages:**
    *   `MediatR`

### 3.2 Licensing Behavior
*   **Load Behavior:**
    *   [x] **Soft Gate:** Manual handoffs for Writer Pro; automatic triggers for Teams.

---

## 4. Data Contract (The API)

```csharp
namespace Lexichord.Modules.Agents.Continuity.Abstractions;

/// <summary>
/// Tracks token usage across a conversation for context limit detection.
/// </summary>
public interface IConversationTokenTracker
{
    /// <summary>
    /// Get current token usage for a conversation.
    /// </summary>
    Task<TokenUsage> GetUsageAsync(string conversationId, CancellationToken ct = default);

    /// <summary>
    /// Record token usage for a new message.
    /// </summary>
    Task RecordMessageAsync(string conversationId, ChatMessage message, CancellationToken ct = default);

    /// <summary>
    /// Get utilization percentage against model's context window.
    /// </summary>
    float GetUtilization(TokenUsage usage, int maxContextTokens);

    /// <summary>
    /// Estimate remaining turns before context exhaustion.
    /// </summary>
    int EstimateRemainingTurns(TokenUsage usage, int maxContextTokens);

    /// <summary>
    /// Reset tracking for a conversation (e.g., after handoff).
    /// </summary>
    Task ResetAsync(string conversationId, CancellationToken ct = default);
}

public record TokenUsage(
    string ConversationId,
    int TotalTokens,
    int SystemPromptTokens,
    int ConversationHistoryTokens,
    int LastMessageTokens,
    int MessageCount,
    int AverageTokensPerTurn,
    DateTimeOffset LastUpdated)
{
    public float GetUtilization(int maxContextTokens) =>
        maxContextTokens > 0 ? (float)TotalTokens / maxContextTokens : 0f;

    public int EstimateRemainingTurns(int maxContextTokens) =>
        AverageTokensPerTurn > 0
            ? Math.Max(0, (maxContextTokens - TotalTokens) / AverageTokensPerTurn)
            : 0;
}

/// <summary>
/// Orchestrates session handoffs and resumptions.
/// </summary>
public interface ISessionContinuityService
{
    /// <summary>
    /// Prepare a handoff package when approaching context limits.
    /// </summary>
    Task<SessionHandoff> PrepareHandoffAsync(
        string conversationId,
        int targetTokenBudget,
        HandoffOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Resume a conversation from a handoff.
    /// </summary>
    Task<ResumedSession> ResumeFromHandoffAsync(
        string handoffId,
        CancellationToken ct = default);

    /// <summary>
    /// Check if a session should trigger a handoff.
    /// </summary>
    Task<HandoffDecision> ShouldHandoffAsync(
        string conversationId,
        int maxTokenCount,
        CancellationToken ct = default);

    /// <summary>
    /// Generate continuation directive for a new session.
    /// </summary>
    Task<ContinuationDirective> GenerateContinuationDirectiveAsync(
        SessionHandoff handoff,
        CancellationToken ct = default);
}

public record HandoffOptions(
    int TargetSummaryTokens = 2000,
    bool IncludeSessionState = true,
    bool ExtractPendingTasks = true,
    CompressionLevel SummaryLevel = CompressionLevel.Detailed);

public record ResumedSession(
    string NewConversationId,
    string PreviousConversationId,
    ContinuationDirective Directive,
    IReadOnlyList<AnchorPoint> RestoredAnchors,
    SessionState? RestoredState);

public record HandoffDecision(
    bool ShouldHandoff,
    float ContextUtilization,
    int EstimatedRemainingTurns,
    TokenUsage CurrentUsage,
    string Reason);
```

---

## 5. Implementation Logic

**Token Tracking Implementation:**
```csharp
public class ConversationTokenTracker : IConversationTokenTracker
{
    private readonly ConcurrentDictionary<string, TokenUsageAccumulator> _usage = new();
    private readonly ITokenCounter _tokenCounter;

    public Task RecordMessageAsync(string conversationId, ChatMessage message, CancellationToken ct)
    {
        var tokens = _tokenCounter.CountTokens(message.Content ?? string.Empty);
        var accumulator = _usage.GetOrAdd(conversationId, _ => new TokenUsageAccumulator(conversationId));
        accumulator.AddMessage(tokens, message.Role);
        return Task.CompletedTask;
    }

    public Task<TokenUsage> GetUsageAsync(string conversationId, CancellationToken ct)
    {
        if (_usage.TryGetValue(conversationId, out var accumulator))
            return Task.FromResult(accumulator.ToTokenUsage());
        return Task.FromResult(new TokenUsage(conversationId, 0, 0, 0, 0, 0, 0, DateTimeOffset.UtcNow));
    }
}
```

**Handoff Trigger Logic:**
```csharp
public async Task<HandoffDecision> ShouldHandoffAsync(
    string conversationId,
    int maxTokenCount,
    CancellationToken ct)
{
    var usage = await _tokenTracker.GetUsageAsync(conversationId, ct);
    var utilization = usage.GetUtilization(maxTokenCount);
    var remainingTurns = usage.EstimateRemainingTurns(maxTokenCount);

    // Trigger at 85% utilization
    if (utilization >= 0.85f)
    {
        return new HandoffDecision(true, utilization, remainingTurns, usage,
            "Context window utilization exceeded 85% threshold");
    }

    // Also trigger for long-running tasks at 70%
    var pendingTasks = await _taskExtractor.ExtractPendingTasksAsync(conversationId, ct);
    if (utilization >= 0.70f && pendingTasks.Any(t => t.RemainingSteps.Count > 5))
    {
        return new HandoffDecision(true, utilization, remainingTurns, usage,
            "Long-running task detected with limited remaining context");
    }

    // Publish warning event at 75%
    if (utilization >= 0.75f && remainingTurns <= 5)
    {
        await _mediator.Publish(new ContextLimitApproachingEvent(conversationId, utilization, remainingTurns, usage), ct);
    }

    return new HandoffDecision(false, utilization, remainingTurns, usage, "Sufficient context remaining");
}
```

---

## 6. Observability & Logging

*   **Metric:** `Agents.Continuity.TokenUsage` (Gauge by conversation)
*   **Metric:** `Agents.Continuity.Utilization` (Histogram)
*   **Metric:** `Agents.Continuity.HandoffsTriggered` (Counter)
*   **Log (Info):** `[CONT] Context utilization {Utilization}% for conversation {ConversationId}`
*   **Log (Warn):** `[CONT] Approaching context limit: {RemainingTurns} turns remaining`

---

## 7. MediatR Events

```csharp
public record ContextLimitApproachingEvent(
    string ConversationId,
    float Utilization,
    int RemainingTurns,
    TokenUsage Usage) : INotification;

public record SessionHandoffPreparedEvent(
    SessionHandoff Handoff) : INotification;

public record SessionResumedEvent(
    ResumedSession Session) : INotification;
```

---

## 8. Acceptance Criteria (QA)

1.  **[Tracking]** `RecordMessageAsync` SHALL accurately count tokens for each message.
2.  **[Utilization]** `GetUtilization` SHALL return percentage of context used.
3.  **[Trigger]** `ShouldHandoffAsync` SHALL return true at 85% utilization.
4.  **[Handoff]** `PrepareHandoffAsync` SHALL create a complete handoff package.
5.  **[Resume]** `ResumeFromHandoffAsync` SHALL restore conversation context.

---

## 9. Test Scenarios

```gherkin
Scenario: Token tracking accumulates correctly
    Given an empty conversation
    When 10 messages are recorded (100 tokens each)
    Then TotalTokens SHALL be approximately 1000
    And AverageTokensPerTurn SHALL be approximately 100

Scenario: Handoff triggered at 85% utilization
    Given a conversation with 85,000 tokens
    And maxTokenCount is 100,000
    When ShouldHandoffAsync is called
    Then ShouldHandoff SHALL be true
    And Reason SHALL mention "85%"

Scenario: Session resumption restores context
    Given a stored handoff with pending tasks
    When ResumeFromHandoffAsync is called
    Then Directive SHALL contain immediate actions
    And RestoredAnchors SHALL include original decisions
```
