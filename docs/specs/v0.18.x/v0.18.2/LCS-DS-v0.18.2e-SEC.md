# LCS-DS-v0.18.2e-SEC: Design Specification — Command Allowlist/Blocklist

## 1. Document Control

| Field                 | Value                                        |
| :-------------------- | :------------------------------------------- |
| **Document ID**       | LCS-DS-v0.18.2e-SEC                          |
| **Parent SBD**        | LCS-SBD-v0.18.2-SEC                          |
| **Release Version**   | v0.18.2e                                     |
| **Component Name**    | Command Allowlist/Blocklist                  |
| **Document Type**     | Design Specification (DS)                    |
| **Author**            | Gemini Architect                             |
| **Created Date**      | 2026-02-04                                   |
| **Last Updated**      | 2026-02-04                                   |
| **Status**            | DRAFT                                        |
| **Classification**    | Internal — Technical Specification           |

---

## 2. Overview

This document provides the detailed design for the **Command Allowlist/Blocklist** system (v0.18.2e). This component provides deterministic, rule-based control over command execution. Unlike the probabilistic risk classification engine, the Allowlist/Blocklist provides explicit "Allow" and "Deny" overrides based on administrator-defined patterns. It acts as a final gatekeeper (or fast-path approver) before user prompts are generated.

---

## 3. Detailed Design

### 3.1. Objective

Enable administrators to define explicit rules that automatically block dangerous commands or permit safe commands, bypassing or enforcing the standard approval workflow.

### 3.2. Scope

-   Define the `Rule` data model supporting Regex, Glob, and Exact matching.
-   Implement `ICommandRuleManager` to manage and evaluate rules.
-   Implement a high-performance evaluation engine with conflict resolution logic.
-   Support role-based application of rules.
-   Provide audit trails for rule changes.

### 3.3. Detailed Architecture

The system evaluates rules in a specific order. The evaluation engine must be highly optimized as it runs for every command.

1.  **Exact Matches**: Looked up via Hash/Dictionary (O(1)).
2.  **Pattern Matches**: Evaluated linearly or via optimized regex sets.

#### 3.3.1. Rule Precedence & Logic

1.  **Blocklist Priority**: A matching **Block** rule always takes precedence over an **Allow** rule unless the Allow rule is marked with a higher `Priority` (e.g., specific exception to a general ban).
2.  **Default Stance**: If no rules match, the command proceeds to the standard Risk Classification Engine.
3.  **Short-Circuiting**:
    -   If a **Block** rule matches: The command is immediately rejected.
    -   If an **Allow** rule matches (and no higher-priority Block rule matches): The command is approved immediately, bypassing risk classification (Fast Path).

### 3.4. Interfaces & Data Models

```csharp
/// <summary>
/// Manages and evaluates command allow/block rules.
/// </summary>
public interface ICommandRuleManager
{
    /// <summary>
    /// Evaluates a command against the active rule set.
    /// </summary>
    Task<RuleEvaluationResult> EvaluateAsync(
        ParsedCommand command,
        RuleEvaluationContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Adds or updates a rule.
    /// </summary>
    Task UpsertRuleAsync(CommandRule rule, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all active rules.
    /// </summary>
    Task<IReadOnlyList<CommandRule>> GetRulesAsync(CancellationToken ct = default);
}

public record CommandRule
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public RuleType Type { get; init; } // Allowlist or Blocklist
    public PatternType PatternType { get; init; } // Exact, Glob, Regex
    public string Pattern { get; init; }
    public int Priority { get; init; } // Higher number = higher precedence
    public IReadOnlyList<string> AppliedRoles { get; init; } // Null = Global
    public bool IsEnabled { get; init; }
}

public enum RuleType { Allowlist, Blocklist }
public enum PatternType { Exact, Glob, Regex }

public record RuleEvaluationResult
{
    public RuleDecision Decision { get; init; } // Allowed, Blocked, Neutral
    public CommandRule? MatchingRule { get; init; }
}
```

### 3.5. Security Considerations

-   **Regex ReDoS**: Regular expression rules supplied by admins must be validated to prevent ReDoS (Regular Expression Denial of Service) attacks. The system should use a timeout for regex evaluation or a safe regex engine.
-   **Rule Bypass**: Ensure that "normalization" happens *before* rule evaluation. For example, `rm  -rf /` (extra spaces) should match the rule `rm -rf /` if using normalized matching, or the regex should be robust.
    -   *Strategy*: Rules should primarily target the *normalized* `ParsedCommand` fields, not the raw string, whenever possible.

### 3.6. Performance Considerations

-   **Caching**: Compiled Regex objects and rule sets should be cached effectively.
-   **Latency**: Evaluation must be <10ms.
-   **Structure**: Use a Trie or Aho-Corasick algorithm if a large number of simple text patterns are used for blocking.

### 3.7. Testing Strategy

-   **Matrix Testing**: Test combinations of Allow/Block rules with varying priorities to ensure precedence logic is flawless.
-   **Pattern Tests**: Verify Glob and Regex patterns match intended targets and *do not* match unintended ones.
-   **ReDoS Fuzzing**: Attempt to crash the evaluator with complex regexes and long strings.

---

## 4. Key Artifacts & Deliverables

| Artifact                 | Description                                                              |
| :----------------------- | :----------------------------------------------------------------------- |
| `ICommandRuleManager`    | Core interface.                                                          |
| `RuleEvaluationEngine`   | Logic for matching and precedence.                                       |
| `RuleRepository`         | Persistence (EF Core / DB).                                              |
| Admin API                | Endpoints for managing rules.                                            |
| Default Rule Set         | Seed data with common safety rules (e.g., blocking `rm -rf /`).       |

---

## 5. Acceptance Criteria

-   [ ] **Precedence**: Block rules prevent execution even if an Allow rule matches, unless priority overrides.
-   [ ] **Performance**: Evaluation takes <10ms for a set of 50 active rules.
-   [ ] **Accuracy**: Regex and Glob patterns correctly identify matching commands.
-   [ ] **Safety**: Malformed regexes are rejected upon rule creation.
-   [ ] **Default Safety**: The system comes pre-seeded with rules blocking obvious destruction (e.g., `rm -rf root`).
