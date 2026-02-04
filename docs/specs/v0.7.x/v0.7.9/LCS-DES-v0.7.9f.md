# LDS-01: Feature Design Specification — Context Expander

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `COMP-06` | Matches the Roadmap ID. |
| **Feature Name** | Context Expander | The internal display name. |
| **Target Version** | `v0.7.9f` | The semantic version target. |
| **Module Scope** | `Lexichord.Modules.Agents` | The specific DLL/Project this code lives in. |
| **Swimlane** | Memory | The functional vertical. |
| **License Tier** | Teams | The minimum license required to load this. |
| **Feature Gate Key** | `Agents.Compression.Expander` | The string key used in `ILicenseService`. |
| **Author** | Lexichord Architecture | Primary Architect. |
| **Reviewer** | — | Lead Architect / Peer. |
| **Status** | Draft | Current lifecycle state. |
| **Last Updated** | 2026-02-03 | Date of last modification. |

---

## 2. Executive Summary

### 2.1 The Requirement
When compressed context is insufficient for the current task, the system needs to retrieve more detailed content on-demand. Users and agents should be able to "drill down" into compressed summaries to access the original detail.

### 2.2 The Proposed Solution
Implement `IContextExpander` that retrieves more detailed content when needed, supporting cascading expansion (L3→L2→L1→L0), expansion trigger detection, and caching to avoid redundant fetches.

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies
*   **Upstream Modules:**
    *   `Lexichord.Modules.Agents.Compression` (v0.7.9a-e)
*   **NuGet Packages:**
    *   `Microsoft.Extensions.Caching.Memory`

### 3.2 Licensing Behavior
*   **Load Behavior:**
    *   [x] **Soft Gate:** Single-level expansion for Writer Pro; cascading expansion for Teams.

---

## 4. Data Contract (The API)

```csharp
namespace Lexichord.Modules.Agents.Compression.Abstractions;

/// <summary>
/// Retrieves more detailed content when compressed info is insufficient.
/// </summary>
public interface IContextExpander
{
    /// <summary>
    /// Expands a segment to a more detailed level.
    /// </summary>
    Task<ExpandedContext> ExpandAsync(
        string segmentId,
        CompressionLevel fromLevel,
        CompressionLevel toLevel,
        CancellationToken ct = default);

    /// <summary>
    /// Expands content referenced by an expansion marker.
    /// </summary>
    Task<ExpandedContext> ExpandMarkerAsync(
        ExpansionMarker marker,
        CancellationToken ct = default);

    /// <summary>
    /// Cascading expansion: retrieves progressively more detail.
    /// </summary>
    Task<IReadOnlyList<ExpandedContext>> CascadeExpandAsync(
        string segmentId,
        CompressionLevel startLevel,
        CompressionLevel targetLevel,
        CancellationToken ct = default);
}

/// <summary>
/// Detects when context expansion is needed.
/// </summary>
public interface IExpansionTriggerDetector
{
    /// <summary>
    /// Analyzes agent response to determine if expansion is needed.
    /// </summary>
    Task<ExpansionDecision> ShouldExpandAsync(
        string agentResponse,
        AssembledContext currentContext,
        CancellationToken ct = default);
}

public record ExpandedContext(
    string SegmentId,
    CompressionLevel Level,
    string Content,
    int TokenCount,
    bool IsFullTranscript);

public record ExpansionDecision(
    bool ShouldExpand,
    IReadOnlyList<string> SegmentIds,
    CompressionLevel TargetLevel,
    string Reason);
```

---

## 5. Implementation Logic

**Expansion Trigger Detection Patterns:**
- "I don't have enough context about..."
- "Could you remind me what..."
- "Earlier in the conversation..."
- Referencing specific details not in current context

**Cascading Expansion:**
```csharp
public async Task<IReadOnlyList<ExpandedContext>> CascadeExpandAsync(
    string segmentId,
    CompressionLevel startLevel,
    CompressionLevel targetLevel,
    CancellationToken ct)
{
    var results = new List<ExpandedContext>();
    var currentLevel = startLevel;

    while (currentLevel > targetLevel)
    {
        var nextLevel = (CompressionLevel)((int)currentLevel - 1);
        var expanded = await ExpandAsync(segmentId, currentLevel, nextLevel, ct);
        results.Add(expanded);
        currentLevel = nextLevel;
    }

    return results;
}
```

---

## 6. Data Persistence (Database)

*   **Migration ID:** None (uses v0.7.9e storage)

---

## 7. Observability & Logging

*   **Metric:** `Agents.Compression.Expansion.Count` (Counter by level)
*   **Metric:** `Agents.Compression.Expansion.CacheHitRate` (Gauge)
*   **Log (Info):** `[COMP:EXPAND] Expanded segment {SegmentId} from {FromLevel} to {ToLevel}`

---

## 8. Acceptance Criteria (QA)

1.  **[Functional]** `ExpandAsync` SHALL retrieve more detailed content from storage.
2.  **[Functional]** `CascadeExpandAsync` SHALL step through each level progressively.
3.  **[Caching]** Recently expanded content SHALL be cached.
4.  **[Triggers]** Expansion triggers SHALL detect "need more context" patterns.

---

## 9. Test Scenarios

```gherkin
Scenario: Expand from Brief to Detailed
    Given a segment stored at Brief and Detailed levels
    When ExpandAsync is called from Brief to Detailed
    Then the Detailed content SHALL be returned

Scenario: Cascade expansion
    Given a segment at Tags level
    When CascadeExpandAsync is called to Full level
    Then 3 expansions SHALL occur (Tags→Brief→Detailed→Full)
```
