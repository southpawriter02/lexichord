# LDS-01: Feature Design Specification — Compression Hardening & Metrics

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `COMP-09` | Matches the Roadmap ID. |
| **Feature Name** | Compression Hardening & Metrics | The internal display name. |
| **Target Version** | `v0.7.9i` | The semantic version target. |
| **Module Scope** | `Lexichord.Modules.Agents` | The specific DLL/Project this code lives in. |
| **Swimlane** | Memory | The functional vertical. |
| **License Tier** | Writer Pro | The minimum license required to load this. |
| **Feature Gate Key** | `Agents.Compression.Metrics` | The string key used in `ILicenseService`. |
| **Author** | Lexichord Architecture | Primary Architect. |
| **Reviewer** | — | Lead Architect / Peer. |
| **Status** | Draft | Current lifecycle state. |
| **Last Updated** | 2026-02-03 | Date of last modification. |

---

## 2. Executive Summary

### 2.1 The Requirement
Before the compression engine is production-ready, it requires comprehensive testing and observability infrastructure to ensure quality, performance, and reliability.

### 2.2 The Proposed Solution
Implement unit tests, integration tests, fidelity benchmarks, and a metrics dashboard for the compression system.

---

## 3. Success Metrics

| Metric | Target |
|--------|--------|
| Compression Ratio (L0 → L2) | 10:1 |
| Compression Ratio (L0 → L3) | 50:1 |
| Anchor Preservation Rate | 100% |
| Fidelity Score (LLM evaluation) | >0.85 |
| Compression Latency | <500ms per segment |
| Expansion Latency | <200ms |

---

## 4. Data Contract (The API)

```csharp
namespace Lexichord.Modules.Agents.Compression.Abstractions;

/// <summary>
/// Evaluates compression fidelity using LLM-based comparison.
/// </summary>
public interface ICompressionFidelityEvaluator
{
    /// <summary>
    /// Evaluates how well compressed content preserves original meaning.
    /// </summary>
    Task<FidelityScore> EvaluateAsync(
        ConversationSegment original,
        CompressedSegment compressed,
        IReadOnlyList<string> testQuestions,
        CancellationToken ct = default);
}

public record FidelityScore(
    float OverallScore,
    float AnchorPreservation,
    float FactualAccuracy,
    float ContextRetention);
```

---

## 5. Test Coverage Requirements

| Component | Test Class | Coverage Target |
|-----------|------------|-----------------|
| `CompressionLevel` | `CompressionLevelTests` | 100% |
| `IConversationSegmenter` | `SegmenterTests` | 95% |
| `IAnchorExtractor` | `AnchorExtractorTests` | 95% |
| `IHierarchicalSummarizer` | `SummarizerTests` | 90% |
| `ICompressionStore` | `CompressionStoreTests` | 95% |
| `IContextExpander` | `ExpanderTests` | 90% |
| `ITokenBudgetManager` | `BudgetManagerTests` | 90% |
| Integration | `CompressionIntegrationTests` | 85% |

---

## 6. Metrics Dashboard Panels

**Overview:**
- Total compressions
- Average compression ratio
- Storage savings
- Fidelity scores

**Performance:**
- Compression latency (P50, P95, P99)
- Expansion latency
- LLM token usage

**Quality:**
- Anchor preservation rate
- Fidelity score distribution
- Failed compressions

---

## 7. Acceptance Criteria (QA)

1.  **[Coverage]** Unit test coverage SHALL be >= 90%.
2.  **[Fidelity]** Fidelity testing SHALL verify semantic preservation.
3.  **[Performance]** All latency targets SHALL be met.
4.  **[Anchors]** Anchor preservation SHALL be 100%.

---

## 8. Test Scenarios

```gherkin
Scenario: Fidelity evaluation
    Given an original segment and its compressed version
    And 5 test questions about the content
    When EvaluateAsync is called
    Then FidelityScore.OverallScore SHALL be >= 0.85
    And FidelityScore.AnchorPreservation SHALL be 1.0

Scenario: Performance benchmark
    Given 100 segments to compress
    When compression is performed
    Then average latency SHALL be < 500ms
    And P99 latency SHALL be < 2000ms
```
