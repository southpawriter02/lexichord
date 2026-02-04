# LCS-DS-v0.18.6a-SEC: Design Specification — Prompt Injection Detection & Mitigation

## 1. Document Control

| Field                 | Value                                        |
| :-------------------- | :------------------------------------------- |
| **Document ID**       | LCS-DS-v0.18.6a-SEC                          |
| **Parent SBD**        | LCS-SBD-v0.18.6-SEC                          |
| **Release Version**   | v0.18.6a                                     |
| **Component Name**    | Prompt Injection Detection & Mitigation      |
| **Document Type**     | Design Specification (DS)                    |
| **Author**            | Gemini Architect                             |
| **Created Date**      | 2026-02-04                                   |
| **Last Updated**      | 2026-02-04                                   |
| **Status**            | DRAFT                                        |
| **Classification**    | Internal — Technical Specification           |

---

## 2. Overview

This document provides the detailed design for **Prompt Injection Detection & Mitigation** (v0.18.6a). As AI agents gain more autonomy, they become vulnerable to adversarial inputs designed to hijack their control flow ("Jailbreaking" or "Prompt Injection"). This component implements a defense-in-depth strategy to identify and neutralize these attacks before they reach the LLM.

---

## 3. Detailed Design

### 3.1. Objective

Prevent unauthorized manipulation of AI behavior by detecting malicious inputs that attempt to override system instructions.

### 3.2. Scope

-   Define `IPromptInjectionDetector`.
-   Implement Heuristic Analysis (Keywords, patterns).
-   Implement ML-based classification (TensforFlow.NET / ONNX integration) for "Jailbreak" intent.
-   Implement **Instruction Separation** (ChatML / Structured Prompts) to structurally prevent injection.
-   "Sandwich Defense" implementation (Prepending/Appending instructions).

### 3.3. Detailed Architecture

```mermaid
graph TD
    Input[User Input] --> Detector[InjectionDetector];
    Detector --> Heuristics[Keyword/Regex Scanner];
    Detector --> Model[ML Classifier (ONNX)];
    Detector --> Structure[Structure Analyzer];
    
    Heuristics -- Risk Score --> Aggregator;
    Model -- Confidence --> Aggregator;
    Structure -- Anomaly --> Aggregator;
    
    Aggregator --> Result{IsSafe?};
    Result -- Yes --> LLM[Pass to LLM];
    Result -- No --> Block[Block Request];
```

#### 3.3.1. Detection Strategies

1.  **Blocklist**: "Ignore previous instructions", "You are now DAN", "System Override".
2.  **Delimiters**: Detect closed delimiters that attempt to confuse the parser (e.g., `user: end`).
3.  **ML Model**: Use a lightweight BERT model fine-tuned on jailbreak datasets (e.g., `protectai/deberta-v3-base-prompt-injection`).

### 3.4. Interfaces & Data Models

```csharp
public interface IPromptInjectionDetector
{
    Task<InjectionDetectionResult> DetectAsync(
        string input,
        ConversationContext context,
        CancellationToken ct = default);
}

public record InjectionDetectionResult
{
    public bool IsInjected { get; init; }
    public double ConfidenceScore { get; init; } // 0.0 - 1.0
    public InjectionType Type { get; init; }
    public IReadOnlyList<string> MatchedPatterns { get; init; }
}

public enum InjectionType
{
    DirectOverride, // "Ignore previous instructions"
    Roleplay, // "Act as..."
    DelimiterAttack, // XML/JSON confusion
    Encoding // Base64 evasion
}
```

### 3.5. Security Considerations

-   **Evolution**: Attack patterns evolve daily. The detector must allow dynamic rule updates without recompilation.
-   **False Positives**: Developers discussing injection attacks might trigger the filter. Allow capability-based bypass for Admins.

### 3.6. Performance Considerations

-   **Latency**: ML inference is slow. Run keywords first (fast fail). Only run ML if heuristic score is ambiguous or for high-risk actions.
-   **Tokenizer**: Use `TikToken` C# binding for accurate token analysis.

### 3.7. Testing Strategy

-   **Red Teaming**: Use an automated Red Teaming library (like Garak) to fuzz the detector.
-   **Regression**: Ensure "Help me write a story about a hacker" (benign) isn't blocked.

---

## 4. Key Artifacts & Deliverables

| Artifact                 | Description                                                              |
| :----------------------- | :----------------------------------------------------------------------- |
| `PromptInjectionDetector`| Core Service.                                                            |
| `InjectionRules`         | JSON file with regex patterns.                                           |
| `OnnxModelService`       | Wrapper for local ML inference.                                          |

---

## 5. Acceptance Criteria

-   [ ] **Direct Attacks**: Blocks "Ignore previous instructions".
-   [ ] **DAN**: Blocks "Do Anything Now" variants.
-   [ ] **Latency**: Detection <50ms (Heuristic), <200ms (ML).
-   [ ] **Audit**: Logs blocked attempts with input hash.
