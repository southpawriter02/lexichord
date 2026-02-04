# LDS-01: Feature Design Specification — Memory Capture Integration

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `MEM-08` | Matches the Roadmap ID. |
| **Feature Name** | Memory Capture Integration | The internal display name. |
| **Target Version** | `v0.8.9h` | The semantic version target. |
| **Module Scope** | `Lexichord.Modules.Agents` | The specific DLL/Project this code lives in. |
| **Swimlane** | Memory | The functional vertical. |
| **License Tier** | Writer Pro | The minimum license required to load this. |
| **Feature Gate Key** | `Agents.Memory.Capture` | The string key used in `ILicenseService`. |
| **Author** | Lexichord Architecture | Primary Architect. |
| **Reviewer** | — | Lead Architect / Peer. |
| **Status** | Draft | Current lifecycle state. |
| **Last Updated** | 2026-02-03 | Date of last modification. |

---

## 2. Executive Summary

### 2.1 The Requirement
The memory fabric needs to learn from everyday interactions without explicit user commands. Conversations, corrections, configuration changes, and workflow patterns should automatically generate memories, enabling the system to improve over time.

### 2.2 The Proposed Solution
Implement `IMemoryCaptureService` that observes Lexichord workflows and creates memories at strategic capture points: conversation turns, user corrections, configuration changes, and repeated action patterns. Uses MediatR event handlers to react to existing events without modifying core workflows.

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies
*   **Upstream Modules:**
    *   `Lexichord.Modules.Agents.Memory` (v0.8.9a-f)
    *   `Lexichord.Host` (Configuration service)
    *   `Lexichord.Modules.Agents` (Conversation events)
*   **NuGet Packages:**
    *   `MediatR`

### 3.2 Licensing Behavior
*   **Load Behavior:**
    *   [x] **Soft Gate:** Automatic capture requires Writer Pro.
*   **Fallback Experience:**
    *   Core users see no automatic memory capture; must use explicit remember commands.

---

## 4. Data Contract (The API)

```csharp
namespace Lexichord.Modules.Agents.Memory.Abstractions;

/// <summary>
/// Captures memories from various workflow observation points.
/// </summary>
public interface IMemoryCaptureService
{
    /// <summary>
    /// Capture memories from a conversation turn.
    /// </summary>
    /// <param name="turn">The conversation turn to analyze.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Memories extracted from the conversation.</returns>
    Task<IReadOnlyList<Memory>> CaptureFromConversationAsync(
        ConversationTurn turn,
        CancellationToken ct = default);

    /// <summary>
    /// Capture a memory from user correction of agent output.
    /// </summary>
    /// <param name="originalContent">What the agent originally produced.</param>
    /// <param name="correctedContent">What the user corrected it to.</param>
    /// <param name="context">Additional context about the correction.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Memory about the correction.</returns>
    Task<Memory> CaptureUserCorrectionAsync(
        string originalContent,
        string correctedContent,
        string context,
        CancellationToken ct = default);

    /// <summary>
    /// Capture a memory from configuration change.
    /// </summary>
    /// <param name="settingKey">The setting that was changed.</param>
    /// <param name="oldValue">Previous value.</param>
    /// <param name="newValue">New value.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Memory about the preference.</returns>
    Task<Memory> CaptureConfigurationChangeAsync(
        string settingKey,
        object? oldValue,
        object newValue,
        CancellationToken ct = default);

    /// <summary>
    /// Capture procedural memory from observed workflow pattern.
    /// </summary>
    /// <param name="actions">Sequence of user actions.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Memory about the workflow if pattern detected.</returns>
    Task<Memory?> CaptureWorkflowPatternAsync(
        IReadOnlyList<UserAction> actions,
        CancellationToken ct = default);

    /// <summary>
    /// Capture memory when user confirms search result was helpful.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="resultId">The result that was confirmed.</param>
    /// <param name="ct">Cancellation token.</param>
    Task CaptureSearchConfirmationAsync(
        string query,
        string resultId,
        CancellationToken ct = default);
}

/// <summary>
/// Represents a single turn in a conversation.
/// </summary>
public record ConversationTurn(
    string ConversationId,
    string UserId,
    string UserMessage,
    string AssistantResponse,
    DateTimeOffset Timestamp,
    string? ProjectId = null);

/// <summary>
/// Represents a user action in a workflow.
/// </summary>
public record UserAction(
    string ActionType,
    string Description,
    DateTimeOffset Timestamp,
    Dictionary<string, object>? Metadata = null);

/// <summary>
/// Configuration for memory capture behavior.
/// </summary>
public record MemoryCaptureOptions
{
    /// <summary>
    /// Minimum confidence for extracting memories from conversation.
    /// </summary>
    public float MinExtractionConfidence { get; init; } = 0.7f;

    /// <summary>
    /// Maximum memories to extract per conversation turn.
    /// </summary>
    public int MaxMemoriesPerTurn { get; init; } = 3;

    /// <summary>
    /// Minimum action count to detect a workflow pattern.
    /// </summary>
    public int MinActionsForPattern { get; init; } = 3;

    /// <summary>
    /// Time window for grouping actions into patterns.
    /// </summary>
    public TimeSpan PatternTimeWindow { get; init; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Whether to capture from all conversation turns.
    /// </summary>
    public bool CaptureAllConversations { get; init; } = true;

    /// <summary>
    /// Whether to capture configuration changes.
    /// </summary>
    public bool CaptureConfigChanges { get; init; } = true;

    /// <summary>
    /// Patterns that indicate memory-worthy content.
    /// </summary>
    public IReadOnlyList<string> MemoryTriggerPatterns { get; init; } = new[]
    {
        @"\bremember\s+that\b",
        @"\balways\s+use\b",
        @"\bnever\s+do\b",
        @"\bprefer\s+to\b",
        @"\bi\s+want\b",
        @"\bfrom\s+now\s+on\b"
    };
}
```

---

## 5. Implementation Logic

**Memory Capture Service:**
```csharp
public class MemoryCaptureService : IMemoryCaptureService
{
    private readonly IMemoryFabric _fabric;
    private readonly IChatCompletionService _llm;
    private readonly MemoryCaptureOptions _options;
    private readonly ILogger<MemoryCaptureService> _logger;

    public async Task<IReadOnlyList<Memory>> CaptureFromConversationAsync(
        ConversationTurn turn,
        CancellationToken ct)
    {
        var memories = new List<Memory>();

        // Check for explicit memory triggers
        var hasTrigger = _options.MemoryTriggerPatterns
            .Any(p => Regex.IsMatch(turn.UserMessage, p, RegexOptions.IgnoreCase));

        // Extract learnable content
        var extractions = await ExtractMemorableContentAsync(turn, ct);

        foreach (var extraction in extractions.Where(e => e.Confidence >= _options.MinExtractionConfidence))
        {
            var context = new MemoryContext(
                turn.UserId,
                turn.ConversationId,
                turn.ProjectId,
                null,
                $"Learned from conversation on {turn.Timestamp:yyyy-MM-dd}");

            var memory = await _fabric.RememberAsAsync(
                extraction.Content,
                extraction.SuggestedType,
                context,
                ct);

            memories.Add(memory);

            // Boost salience if explicit trigger was used
            if (hasTrigger)
            {
                await _fabric.ReinforceMemoryAsync(
                    memory.Id,
                    ReinforcementReason.ExplicitUserConfirmation,
                    ct);
            }

            _logger.LogInformation(
                "[MEM:CAPTURE] Captured {Type} memory from conversation: {Preview}",
                extraction.SuggestedType,
                extraction.Content[..Math.Min(50, extraction.Content.Length)]);
        }

        await _mediator.Publish(new MemoryCapturedEvent(
            turn.ConversationId,
            CaptureSource.Conversation,
            memories), ct);

        return memories;
    }

    private async Task<IReadOnlyList<MemoryExtraction>> ExtractMemorableContentAsync(
        ConversationTurn turn,
        CancellationToken ct)
    {
        var prompt = $"""
            Analyze this conversation turn and extract any learnable information.

            User: {turn.UserMessage}
            Assistant: {turn.AssistantResponse}

            Extract information that would be useful to remember for future conversations:
            - User preferences and requirements
            - Project facts and constraints
            - Decisions made
            - Procedures or workflows mentioned

            Respond with JSON array:
            [
              {{
                "content": "statement to remember",
                "type": "semantic|episodic|procedural",
                "confidence": 0.0-1.0
              }}
            ]

            Return empty array [] if nothing notable to remember.
            """;

        var response = await _llm.CompleteAsync(prompt, ct);
        return JsonSerializer.Deserialize<List<MemoryExtraction>>(response) ?? new();
    }

    public async Task<Memory> CaptureUserCorrectionAsync(
        string originalContent,
        string correctedContent,
        string context,
        CancellationToken ct)
    {
        // Learn from the correction
        var learningContent = await SynthesizeCorrectionLearningAsync(
            originalContent,
            correctedContent,
            context,
            ct);

        var memoryContext = new MemoryContext(
            GetCurrentUserId(),
            null,
            null,
            null,
            $"Learned from correction: {context}");

        var memory = await _fabric.RememberAsAsync(
            learningContent,
            MemoryType.Semantic,
            memoryContext,
            ct);

        // Corrections are high-value learning
        await _fabric.ReinforceMemoryAsync(
            memory.Id,
            ReinforcementReason.UserCorrection,
            ct);

        _logger.LogInformation(
            "[MEM:CAPTURE] Captured correction: {Original} -> {Corrected}",
            originalContent[..Math.Min(30, originalContent.Length)],
            correctedContent[..Math.Min(30, correctedContent.Length)]);

        return memory;
    }

    private async Task<string> SynthesizeCorrectionLearningAsync(
        string original,
        string corrected,
        string context,
        CancellationToken ct)
    {
        var prompt = $"""
            The user corrected this output:
            Original: {original}
            Corrected to: {corrected}
            Context: {context}

            Synthesize a concise statement to remember that will prevent this mistake in the future.
            Focus on the underlying preference or rule, not the specific instance.

            Example: If original was "the API uses OAuth" and corrected to "the API uses API keys",
            the statement should be "The API uses API keys for authentication, not OAuth."

            Respond with just the statement to remember:
            """;

        return await _llm.CompleteAsync(prompt, ct);
    }

    public async Task<Memory> CaptureConfigurationChangeAsync(
        string settingKey,
        object? oldValue,
        object newValue,
        CancellationToken ct)
    {
        var content = $"User prefers {settingKey} set to {newValue}";

        if (oldValue != null)
        {
            content = $"User changed {settingKey} from {oldValue} to {newValue}";
        }

        var context = new MemoryContext(
            GetCurrentUserId(),
            null,
            null,
            null,
            $"Configuration change: {settingKey}");

        var memory = await _fabric.RememberAsAsync(
            content,
            MemoryType.Semantic,
            context,
            ct);

        _logger.LogInformation(
            "[MEM:CAPTURE] Captured config change: {Key} = {Value}",
            settingKey, newValue);

        return memory;
    }

    public async Task<Memory?> CaptureWorkflowPatternAsync(
        IReadOnlyList<UserAction> actions,
        CancellationToken ct)
    {
        if (actions.Count < _options.MinActionsForPattern)
        {
            return null;
        }

        // Check if actions are within time window
        var firstAction = actions.MinBy(a => a.Timestamp);
        var lastAction = actions.MaxBy(a => a.Timestamp);

        if (lastAction.Timestamp - firstAction.Timestamp > _options.PatternTimeWindow)
        {
            return null;
        }

        // Synthesize workflow description
        var workflow = await SynthesizeWorkflowAsync(actions, ct);

        if (string.IsNullOrEmpty(workflow))
        {
            return null;
        }

        var context = new MemoryContext(
            GetCurrentUserId(),
            null,
            null,
            null,
            "Observed workflow pattern");

        var memory = await _fabric.RememberAsAsync(
            workflow,
            MemoryType.Procedural,
            context,
            ct);

        _logger.LogInformation(
            "[MEM:CAPTURE] Captured workflow: {Workflow}",
            workflow[..Math.Min(50, workflow.Length)]);

        return memory;
    }

    private async Task<string?> SynthesizeWorkflowAsync(
        IReadOnlyList<UserAction> actions,
        CancellationToken ct)
    {
        var actionDescriptions = string.Join("\n", actions.Select((a, i) =>
            $"{i + 1}. {a.ActionType}: {a.Description}"));

        var prompt = $"""
            The user performed these actions in sequence:
            {actionDescriptions}

            If this represents a meaningful workflow pattern, describe it as a procedure.
            If the actions are unrelated or random, respond with "none".

            Example good pattern: "To deploy the application: 1) Run tests, 2) Build the project, 3) Push to main"
            """;

        var response = await _llm.CompleteAsync(prompt, ct);

        if (response.Trim().Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return response;
    }

    public async Task CaptureSearchConfirmationAsync(
        string query,
        string resultId,
        CancellationToken ct)
    {
        // Reinforce the memory that produced this result
        await _fabric.ReinforceMemoryAsync(
            resultId,
            ReinforcementReason.SuccessfulApplication,
            ct);

        _logger.LogInformation(
            "[MEM:CAPTURE] Reinforced memory {MemoryId} from successful search",
            resultId);
    }
}

record MemoryExtraction(
    string Content,
    MemoryType SuggestedType,
    float Confidence);
```

**MediatR Event Handlers:**
```csharp
/// <summary>
/// Handles conversation completion to capture memories.
/// </summary>
public class ConversationCompletedHandler : INotificationHandler<ConversationCompletedEvent>
{
    private readonly IMemoryCaptureService _captureService;
    private readonly MemoryCaptureOptions _options;

    public async Task Handle(ConversationCompletedEvent notification, CancellationToken ct)
    {
        if (!_options.CaptureAllConversations)
            return;

        var turn = new ConversationTurn(
            notification.ConversationId,
            notification.UserId,
            notification.UserMessage,
            notification.AssistantResponse,
            notification.Timestamp,
            notification.ProjectId);

        await _captureService.CaptureFromConversationAsync(turn, ct);
    }
}

/// <summary>
/// Handles configuration changes to capture preferences.
/// </summary>
public class ConfigurationChangedHandler : INotificationHandler<ConfigurationChangedEvent>
{
    private readonly IMemoryCaptureService _captureService;
    private readonly MemoryCaptureOptions _options;

    public async Task Handle(ConfigurationChangedEvent notification, CancellationToken ct)
    {
        if (!_options.CaptureConfigChanges)
            return;

        await _captureService.CaptureConfigurationChangeAsync(
            notification.Key,
            notification.OldValue,
            notification.NewValue,
            ct);
    }
}
```

---

## 6. MediatR Events

```csharp
/// <summary>
/// Published when memories are captured from a workflow.
/// </summary>
public record MemoryCapturedEvent(
    string SourceId,
    CaptureSource Source,
    IReadOnlyList<Memory> Memories) : INotification;

public enum CaptureSource
{
    Conversation,
    Correction,
    Configuration,
    WorkflowPattern,
    SearchConfirmation
}
```

---

## 7. Capture Points Summary

| Trigger | Memory Type | Example |
|---------|-------------|---------|
| Conversation turn | Semantic/Episodic | "User mentioned the API uses OAuth" |
| User correction | Semantic | "Actually, the API uses API keys, not OAuth" |
| Configuration change | Semantic | "User prefers dark mode" |
| Search confirmation | Reinforcement | Boost result memory salience |
| Workflow observation | Procedural | "To deploy: run tests, then push" |
| Explicit "remember" | Semantic | "Remember that we always use PostgreSQL" |

---

## 8. Observability & Logging

*   **Metric:** `Agents.Memory.Capture.Events` (Counter by Source)
*   **Metric:** `Agents.Memory.Capture.MemoriesCreated` (Counter by Type)
*   **Metric:** `Agents.Memory.Capture.ExtractionConfidence` (Histogram)
*   **Log (Info):** `[MEM:CAPTURE] Captured {Type} memory from conversation: {Preview}`
*   **Log (Info):** `[MEM:CAPTURE] Captured correction: {Original} -> {Corrected}`
*   **Log (Info):** `[MEM:CAPTURE] Captured config change: {Key} = {Value}`
*   **Log (Info):** `[MEM:CAPTURE] Captured workflow: {Workflow}`

---

## 9. Acceptance Criteria (QA)

1.  **[Conversation]** Conversation turns SHALL be analyzed for memorable content.
2.  **[Triggers]** Explicit triggers ("remember that") SHALL boost salience.
3.  **[Corrections]** User corrections SHALL create corrective memories.
4.  **[Config]** Configuration changes SHALL create preference memories.
5.  **[Workflow]** Repeated action patterns SHALL create procedural memories.
6.  **[Confidence]** Low-confidence extractions SHALL be filtered.
7.  **[Events]** Capture SHALL publish `MemoryCapturedEvent`.

---

## 10. Test Scenarios

```gherkin
Scenario: Capture from conversation with trigger
    Given a conversation turn "Remember that we use PostgreSQL"
    When CaptureFromConversationAsync is called
    Then a semantic memory SHALL be created
    And salience SHALL be boosted via reinforcement

Scenario: Capture user correction
    Given original output "API uses OAuth"
    And corrected to "API uses API keys"
    When CaptureUserCorrectionAsync is called
    Then memory SHALL contain "API keys"
    And memory SHALL be reinforced

Scenario: Capture configuration change
    Given setting "theme" changed from "light" to "dark"
    When CaptureConfigurationChangeAsync is called
    Then memory SHALL be "User prefers dark mode"

Scenario: Capture workflow pattern
    Given 4 actions: "run tests", "build", "commit", "push"
    And all within 30 minutes
    When CaptureWorkflowPatternAsync is called
    Then procedural memory SHALL describe the workflow
```

