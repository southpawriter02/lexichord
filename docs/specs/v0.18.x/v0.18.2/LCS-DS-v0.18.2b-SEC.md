# LCS-DS-v0.18.2b-SEC: Design Specification — Risk Classification Engine

## 1. Document Control

| Field                 | Value                                        |
| :-------------------- | :------------------------------------------- |
| **Document ID**       | LCS-DS-v0.18.2b-SEC                          |
| **Parent SBD**        | LCS-SBD-v0.18.2-SEC                          |
| **Release Version**   | v0.18.2b                                     |
| **Component Name**    | Risk Classification Engine                   |
| **Document Type**     | Design Specification (DS)                    |
| **Author**            | Gemini Architect                             |
| **Created Date**      | 2026-02-03                                   |
| **Last Updated**      | 2026-02-03                                   |
| **Status**            | DRAFT                                        |
| **Classification**    | Internal — Technical Specification           |

---

## 2. Overview

This document provides the detailed design for the **Risk Classification Engine** (v0.18.2b). This is the security brain of the Command Sandbox. After a command has been parsed into a normalized structure by the `Command Parser`, this engine evaluates it to assign a quantitative risk score and a qualitative risk category. This classification is the primary input for determining the necessary approval workflow.

---

## 3. Detailed Design

### 3.1. Objective

To implement a sophisticated, multi-factor risk assessment engine that can accurately evaluate the potential danger of a parsed command, providing a reliable basis for the approval and execution workflow.

### 3.2. Scope

-   Define the `IRiskClassifier` interface for evaluating `ParsedCommand` objects.
-   Implement a multi-stage classification process that combines pattern matching, resource analysis, and heuristics.
-   Maintain a database of "dangerous patterns" (e.g., via a JSON file or a database table) used for matching.
-   Develop a scoring algorithm that aggregates risk factors into a final score from 0-100.
-   Categorize commands into `SAFE`, `LOW`, `MEDIUM`, `HIGH`, and `CRITICAL` tiers based on the score.
-   Integrate an ML-based anomaly detector (as a future extension point) to identify novel threats.
-   Generate a human-readable explanation for why a command was assigned a particular risk level.

### 3.3. Detailed Architecture

The `RiskClassifier` service will implement a pipeline of analysis steps. The `ParsedCommand` object is passed through each step, accumulating risk points and flags.

```mermaid
graph TD
    A[IRiskClassifier.ClassifyAsync(ParsedCommand)] --> B{Start: RiskScore = 0};
    
    subgraph Classification Pipeline
        B --> C{1. Pattern Matching};
        C -- Check against Dangerous Patterns DB --> D{Found Match?};
        D -- Yes --> E[Add score based on pattern severity];
        D -- No --> F;
        E --> F;
        
        F --> G{2. Resource & Argument Analysis};
        G -- Is executable sensitive? (e.g., sudo, rm) --> H[Add score];
        G -- Are arguments sensitive? (e.g., -rf, /) --> H;
        H --> I{3. Privilege Analysis};
        I -- Does command attempt privilege escalation? --> J[Add significant score];
        J --> K{4. Data Exposure Analysis};
        K -- Does command read/send sensitive data? --> L[Add score];
        L --> M{5. Heuristic Analysis};
        M -- e.g., command contains IPs, obfuscated strings --> N[Add score];
    end

    N --> O{Final Score Aggregation};
    O --> P{Map Score to Risk Category};
    P --> Q[Generate Risk Explanation];
    Q --> R[Return RiskClassification object];
    A --> R;
```

#### 3.3.1. Scoring Algorithm

Each analysis step contributes to a final score. The scoring is not purely additive; some factors act as multipliers.
-   **Base Score**: Determined by the executable itself (e.g., `rm` starts higher than `ls`).
-   **Argument Score**: Arguments add points (e.g., `-rf` adds more than `-l`).
-   **Pattern Match**: A direct match on a "Critical" pattern (e.g., `rm -rf /`) can immediately set the score to 100.
-   **Multipliers**: Privilege escalation (e.g., `sudo`) acts as a multiplier on the combined score of the command it's wrapping. `(Base + Args) * Sudo_Multiplier`.

#### 3.3.2. Dangerous Patterns Database

This will be a list of known-bad command patterns stored in a `dangerous_patterns.json` file, loaded at startup. This allows the security team to add new patterns without redeploying the application.

**Example `dangerous_patterns.json` entry:**
```json
[
  {
    "id": "DP-001",
    "pattern": "^rm\\s+-rf\\s+/$",
    "patternType": "Regex",
    "description": "Recursive deletion of the root directory.",
    "riskLevel": "Critical",
    "baseScore": 100,
    "tags": ["filesystem", "destructive"]
  }
]
```

### 3.4. Interfaces & Data Models

```csharp
/// <summary>
/// Evaluates parsed commands for security and safety risks, assigning a score and category.
/// </summary>
public interface IRiskClassifier
{
    /// <summary>
    /// Classifies a parsed command and assigns a comprehensive risk assessment.
    /// </summary>
    /// <param name="command">The structured, parsed command to evaluate.</param>
    /// <param name="executionContext">The context in which the command would be executed.</param>
    /// <returns>A detailed risk classification including score, category, and explanatory factors.</returns>
    Task<RiskClassification> ClassifyAsync(ParsedCommand command, ExecutionContext executionContext);

    /// <summary>
    /// Generates a human-readable explanation for a given risk classification.
    /// </summary>
    /// <param name="classification">The classification to explain.</param>
    /// <returns>A detailed explanation of the identified risks.</returns>
    Task<RiskExplanation> GenerateRiskExplanation(RiskClassification classification);
}

/// <summary>
/// The result of a risk classification operation.
/// </summary>
public record RiskClassification(
    string CommandId,
    int RiskScore, // 0-100
    RiskCategory Category,
    IReadOnlyList<RiskFactor> ContributingFactors,
    bool IsAutoApprovable);

/// <summary>
/// The qualitative risk category.
/// </summary>
public enum RiskCategory
{
    SAFE,       // 0-20
    LOW,        // 21-40
    MEDIUM,     // 41-60
    HIGH,       // 61-80
    CRITICAL    // 81-100
}

/// <summary>
/// A specific factor that contributed to the risk score.
/// </summary>
public record RiskFactor(
    string Description,
    int ScoreContribution,
    string? MatchedPattern = null);

/// <summary>
/// A human-readable explanation of the risks.
/// </summary>
public record RiskExplanation(
    string Summary,
    IReadOnlyList<string> DetailedPoints,
    IReadOnlyList<string> SuggestedMitigations);

/// <summary>
/// Represents a known dangerous command pattern loaded from the pattern database.
/// </summary>
public record DangerousPattern(
    string Id,
    string Pattern,
    PatternType PatternType,
    string Description,
    RiskCategory RiskLevel,
    int BaseScore);

public enum PatternType { Regex, Glob, Exact }
```

### 3.5. Error Handling

-   **Parser Failure**: The classifier assumes it receives a valid `ParsedCommand` object. It does not handle parsing errors; that is the responsibility of the `CommandParser`.
-   **Pattern Database Failure**: If the `dangerous_patterns.json` file is missing or malformed, the engine will log a critical error but continue to function in a degraded state, relying only on heuristic and resource analysis. It will assign a minimum risk of `MEDIUM` to all commands in this state to ensure a fail-safe posture.
-   **Regex Errors**: If a regex pattern in the database is invalid, it will be skipped, and an error will be logged during startup.

### 3.6. Security Considerations

-   **Engine Evasion**: The primary security concern is an attacker crafting a command that bypasses the engine's checks. This is mitigated by:
    1.  **Normalization**: The parser normalizes the command, foiling many obfuscation techniques.
    2.  **Defense in Depth**: The engine uses multiple analysis types (patterns, heuristics, resource checks), so a failure in one may be caught by another.
    3.  **Pattern Updates**: The external pattern database allows for rapid response to new threats.
-   **Regex DoS (ReDoS)**: The regex patterns in the database must be written carefully to avoid catastrophic backtracking. A timeout will be applied to all regex matching operations to prevent ReDoS attacks.

### 3.7. Performance Considerations

-   **Classification Time**: The classification process must be very fast. The target is <50ms per command.
-   **Pattern Matching**: The pattern matching step is the most expensive. The regex patterns will be pre-compiled at startup for performance. The number of patterns should be kept manageable (<1000) to ensure low latency.
-   **Caching**: The results of risk classification can be cached. The cache key would be a hash of the `ParsedCommand` object's content. This can significantly speed up processing for frequently repeated commands.

### 3.8. Testing Strategy

-   **Unit Tests**:
    -   Test the scoring algorithm with various inputs to ensure scores are calculated as expected.
    -   Test the pattern matching logic against a library of safe and malicious command strings.
    -   Test the risk category mapping for all score ranges.
    -   Test the `RiskExplanation` generator to ensure it produces clear, accurate output.
-   **Security Testing (Red Team)**:
    -   A dedicated set of tests will focus on attempting to bypass the classifier.
    -   This includes using shell obfuscation techniques, Unicode tricks, and novel command combinations to see if they can slip past the engine. The results will be used to create new patterns and heuristics.

---

## 4. Key Artifacts & Deliverables

| Artifact                 | Description                                                              |
| :----------------------- | :----------------------------------------------------------------------- |
| `IRiskClassifier`        | The core interface for the classification service.                       |
| `RiskClassifier`         | The default implementation of the service.                               |
| `RiskClassification`     | The C# record for the output of a classification.                        |
| `dangerous_patterns.json`| The external database of known-bad command patterns.                     |
| Security Test Suite      | A suite of tests specifically designed to try and evade the engine.      |

---

## 5. Acceptance Criteria

-   [ ] The engine correctly classifies a corpus of 100+ known dangerous commands with 100% accuracy (assigning `HIGH` or `CRITICAL`).
-   [ ] The final risk scores are consistent, with a variance of <5% across multiple evaluations of the same command.
-   [ ] Classification for a single command completes in <50ms on average.
-   [ ] The dangerous operations database contains at least 500 distinct patterns at launch.
-   [ ] The false positive rate (classifying a safe command as `HIGH` or `CRITICAL`) is below 2%. The false negative rate (classifying a dangerous command as `LOW` or `SAFE`) is below 1%.
-   [ ] The engine provides a clear, human-readable explanation for why a command was assigned its risk score.
-   [ ] The engine is resilient and fails securely (assigning higher risk) if its pattern database cannot be loaded.

---

## 6. Dependencies & Integration Points

### 6.1. Dependencies
-   **`v0.18.2a` (Command Parser & Analyzer)**: This is a hard dependency. The classifier operates on the `ParsedCommand` object produced by the parser.
-   **ML.NET (Optional)**: For the future anomaly detection feature.

### 6.2. Integration Points
-   **`ApprovalQueue` (v0.18.2c)**: The `RiskClassification` output is the primary input for the approval queue, determining which workflow a command enters (e.g., auto-approve, single reviewer, multi-reviewer).
-   **`CommandAllowlist/Blocklist` (v0.18.2e)**: The blocklist can be seen as a precursor to risk classification, providing an immediate "deny" for certain patterns before they even reach the main classification engine.
