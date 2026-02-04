# LDS-01: Feature Design Specification — Token Budget Manager

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `COMP-07` | Matches the Roadmap ID. |
| **Feature Name** | Token Budget Manager | The internal display name. |
| **Target Version** | `v0.7.9g` | The semantic version target. |
| **Module Scope** | `Lexichord.Modules.Agents` | The specific DLL/Project this code lives in. |
| **Swimlane** | Memory | The functional vertical. |
| **License Tier** | Teams | The minimum license required to load this. |
| **Feature Gate Key** | `Agents.Compression.BudgetManager` | The string key used in `ILicenseService`. |
| **Author** | Lexichord Architecture | Primary Architect. |
| **Reviewer** | — | Lead Architect / Peer. |
| **Status** | Draft | Current lifecycle state. |
| **Last Updated** | 2026-02-03 | Date of last modification. |

---

## 2. Executive Summary

### 2.1 The Requirement
With multiple compression levels available, the system needs intelligent allocation of context window tokens across segments. Recent context should be prioritized, anchors must be preserved, and older content should use higher compression levels.

### 2.2 The Proposed Solution
Implement `ITokenBudgetManager` for dynamic token budget allocation across compression levels. The manager prioritizes recent messages (full detail), anchor-containing segments, and uses progressive compression for older content.

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies
*   **Upstream Modules:**
    *   `Lexichord.Modules.Agents.Compression` (v0.7.9a-f)
    *   `ITokenCounter` (Token counting)

### 3.2 Licensing Behavior
*   **Load Behavior:**
    *   [x] **Soft Gate:** Basic allocation for Writer Pro; dynamic rebalancing for Teams.

---

## 4. Data Contract (The API)

```csharp
namespace Lexichord.Modules.Agents.Compression.Abstractions;

/// <summary>
/// Dynamically allocates context window across compression levels.
/// </summary>
public interface ITokenBudgetManager
{
    /// <summary>
    /// Allocates token budget across conversation segments.
    /// </summary>
    Task<BudgetAllocation> AllocateAsync(
        string conversationId,
        int totalBudget,
        BudgetAllocationOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Rebalances allocation after expansion or new messages.
    /// </summary>
    Task<BudgetAllocation> RebalanceAsync(
        string conversationId,
        BudgetAllocation currentAllocation,
        int newBudget,
        CancellationToken ct = default);
}

public record BudgetAllocation(
    IReadOnlyList<SegmentAllocation> Segments,
    int UsedTokens,
    int RemainingTokens,
    IReadOnlyDictionary<CompressionLevel, int> TokensByLevel);

public record SegmentAllocation(
    string SegmentId,
    CompressionLevel Level,
    int AllocatedTokens,
    AllocationReason Reason);

public enum AllocationReason
{
    Recent,           // Recent messages get full detail
    ContainsAnchors,  // Segments with anchors prioritized
    TopicRelevant,    // Matches current topic
    Expanded,         // User/agent requested expansion
    Baseline          // Minimum context coverage
}

public record BudgetAllocationOptions(
    int RecentMessagesFullBudget = 2000,
    int AnchorBudget = 1000,
    float CompressionAggressiveness = 0.5f,
    int MinSegmentsAtBrief = 3);
```

---

## 5. Implementation Logic

**Allocation Algorithm:**
```csharp
public async Task<BudgetAllocation> AllocateAsync(
    string conversationId,
    int totalBudget,
    BudgetAllocationOptions options,
    CancellationToken ct)
{
    var segments = await _store.GetConversationSegmentsAsync(conversationId, CompressionLevel.Full, ct);
    var allocations = new List<SegmentAllocation>();
    var remainingBudget = totalBudget;

    // 1. Allocate budget for recent messages (full detail)
    var recentSegments = segments.TakeLast(2).ToList();
    foreach (var segment in recentSegments)
    {
        var tokens = Math.Min(segment.TokenCount, options.RecentMessagesFullBudget / 2);
        allocations.Add(new SegmentAllocation(segment.SegmentId, CompressionLevel.Full, tokens, AllocationReason.Recent));
        remainingBudget -= tokens;
    }

    // 2. Allocate for anchor-heavy segments (detailed)
    var anchorSegments = segments.Except(recentSegments)
        .Where(s => s.Anchors.Count > 2)
        .OrderByDescending(s => s.Anchors.Sum(a => a.Importance));

    foreach (var segment in anchorSegments.Take(3))
    {
        var detailed = await _store.GetAsync(segment.SegmentId, CompressionLevel.Detailed, ct);
        var tokens = Math.Min(detailed?.TokenCount ?? 500, options.AnchorBudget / 3);
        allocations.Add(new SegmentAllocation(segment.SegmentId, CompressionLevel.Detailed, tokens, AllocationReason.ContainsAnchors));
        remainingBudget -= tokens;
    }

    // 3. Fill remaining with brief summaries
    var remainingSegments = segments.Except(allocations.Select(a => segments.First(s => s.SegmentId == a.SegmentId)));
    foreach (var segment in remainingSegments)
    {
        if (remainingBudget <= 0) break;
        var brief = await _store.GetAsync(segment.SegmentId, CompressionLevel.Brief, ct);
        var tokens = Math.Min(brief?.TokenCount ?? 100, remainingBudget / remainingSegments.Count());
        allocations.Add(new SegmentAllocation(segment.SegmentId, CompressionLevel.Brief, tokens, AllocationReason.Baseline));
        remainingBudget -= tokens;
    }

    return new BudgetAllocation(
        allocations,
        totalBudget - remainingBudget,
        remainingBudget,
        allocations.GroupBy(a => a.Level).ToDictionary(g => g.Key, g => g.Sum(a => a.AllocatedTokens)));
}
```

---

## 6. Observability & Logging

*   **Metric:** `Agents.Compression.Budget.TotalAllocated` (Gauge)
*   **Metric:** `Agents.Compression.Budget.ByLevel` (Gauge by level)
*   **Log (Info):** `[COMP:BUDGET] Allocated {Used}/{Total} tokens across {SegmentCount} segments`

---

## 7. Acceptance Criteria (QA)

1.  **[Functional]** Recent segments SHALL receive highest priority (Full level).
2.  **[Functional]** Anchor-containing segments SHALL be at Detailed level minimum.
3.  **[Functional]** Total allocation SHALL NOT exceed budget.
4.  **[Rebalance]** After expansion, budget SHALL rebalance to accommodate.

---

## 8. Test Scenarios

```gherkin
Scenario: Recent messages get full allocation
    Given a conversation with 10 segments
    And budget of 8000 tokens
    When AllocateAsync is called
    Then the last 2 segments SHALL be at Full level

Scenario: Budget rebalancing after expansion
    Given an allocation using 7000 of 8000 tokens
    When a segment is expanded (adding 2000 tokens)
    And RebalanceAsync is called with same budget
    Then older segments SHALL be compressed further to fit
```
