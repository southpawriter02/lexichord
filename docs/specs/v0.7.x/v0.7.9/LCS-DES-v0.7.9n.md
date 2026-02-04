# LDS-01: Feature Design Specification — Continuation Directive Generator

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `CONT-05` | Matches the Roadmap ID. |
| **Feature Name** | Continuation Directive Generator | The internal display name. |
| **Target Version** | `v0.7.9n` | The semantic version target. |
| **Module Scope** | `Lexichord.Modules.Agents` | The specific DLL/Project this code lives in. |
| **Swimlane** | Memory | The functional vertical. |
| **License Tier** | Writer Pro | The minimum license required to load this. |
| **Feature Gate Key** | `Agents.Continuity.Directive` | The string key used in `ILicenseService`. |
| **Author** | Lexichord Architecture | Primary Architect. |
| **Reviewer** | — | Lead Architect / Peer. |
| **Status** | Draft | Current lifecycle state. |
| **Last Updated** | 2026-02-03 | Date of last modification. |

---

## 2. Executive Summary

### 2.1 The Requirement
When resuming from a handoff, the new session needs clear instructions to continue work seamlessly. The user should not notice a session break occurred.

### 2.2 The Proposed Solution
Implement `IContinuationDirectiveGenerator` that creates context-aware instructions for resumed sessions, formatted for injection into system prompts.

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies
*   **Upstream Modules:**
    *   `Lexichord.Modules.Agents.Continuity` (v0.7.9j-m)
    *   `IPromptRenderer`

---

## 4. Data Contract (The API)

```csharp
namespace Lexichord.Modules.Agents.Continuity.Abstractions;

/// <summary>
/// Generates instructions for resuming work in a new session.
/// </summary>
public interface IContinuationDirectiveGenerator
{
    /// <summary>
    /// Generates a continuation directive from a handoff.
    /// </summary>
    Task<ContinuationDirective> GenerateAsync(
        SessionHandoff handoff,
        DirectiveOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Formats the directive as a system prompt section.
    /// </summary>
    string FormatAsSystemPrompt(ContinuationDirective directive);
}

public record DirectiveOptions(
    bool IncludeImmediateActions = true,
    bool IncludeContextReminders = true,
    int MaxReminderCount = 5,
    DirectiveStyle Style = DirectiveStyle.Concise);

public enum DirectiveStyle
{
    Concise,    // Brief bullet points
    Narrative,  // Flowing prose
    Structured  // Formal sections
}
```

---

## 5. Implementation Logic

**Directive Template:**
```yaml
template_id: "continuation-directive"
system_prompt: |
  You are resuming a conversation that was compacted due to context limits.

  ## Previous Session Summary
  {{compacted_summary}}

  ## Preserved Decisions & Commitments
  {{#anchors}}
  - [{{type}}]: {{content}}
  {{/anchors}}

  ## Unfinished Work
  {{#pending_tasks}}
  ### {{description}}
  - Status: {{status}}
  - Progress: {{progress_percentage}}%
  {{#remaining_steps}}
  - [ ] {{.}}
  {{/remaining_steps}}
  {{#blocking_reason}}
  - ⚠️ Blocked: {{blocking_reason}}
  {{/blocking_reason}}
  {{/pending_tasks}}

  ## Immediate Actions
  {{#immediate_actions}}
  1. {{.}}
  {{/immediate_actions}}

  Continue the work seamlessly. The user should not notice a session break.
```

**System Prompt Formatting:**
```csharp
public string FormatAsSystemPrompt(ContinuationDirective directive)
{
    var builder = new StringBuilder();

    builder.AppendLine("--- SESSION CONTINUATION ---");
    builder.AppendLine();
    builder.AppendLine(directive.Summary);
    builder.AppendLine();

    if (directive.ImmediateActions.Any())
    {
        builder.AppendLine("**Immediate Actions:**");
        foreach (var action in directive.ImmediateActions)
        {
            builder.AppendLine($"1. {action}");
        }
        builder.AppendLine();
    }

    if (directive.ContextReminders.Any())
    {
        builder.AppendLine("**Remember:**");
        foreach (var reminder in directive.ContextReminders)
        {
            builder.AppendLine($"- {reminder}");
        }
    }

    builder.AppendLine("--- END CONTINUATION ---");

    return builder.ToString();
}
```

---

## 6. Observability & Logging

*   **Metric:** `Agents.Continuity.Directive.Generated` (Counter)
*   **Log (Info):** `[CONT:DIR] Generated directive with {ActionCount} actions, {ReminderCount} reminders`

---

## 7. Acceptance Criteria (QA)

1.  **[Functional]** `GenerateAsync` SHALL create a directive with summary and actions.
2.  **[Functional]** `FormatAsSystemPrompt` SHALL produce valid prompt text.
3.  **[Anchors]** All preserved anchors SHALL appear in the directive.
4.  **[Tasks]** Pending tasks SHALL include remaining steps.

---

## 8. Test Scenarios

```gherkin
Scenario: Generate directive from handoff
    Given a handoff with 3 anchors and 2 pending tasks
    When GenerateAsync is called
    Then ImmediateActions SHALL include "Continue task X"
    And ContextReminders SHALL include anchor content

Scenario: Format as system prompt
    Given a continuation directive
    When FormatAsSystemPrompt is called
    Then output SHALL contain "SESSION CONTINUATION" markers
    And output SHALL be valid for system prompt injection
```
