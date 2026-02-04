# LDS-01: Feature Design Specification — Context Assembler Integration

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `COMP-08` | Matches the Roadmap ID. |
| **Feature Name** | Context Assembler Integration | The internal display name. |
| **Target Version** | `v0.7.9h` | The semantic version target. |
| **Module Scope** | `Lexichord.Modules.Agents` | The specific DLL/Project this code lives in. |
| **Swimlane** | Memory | The functional vertical. |
| **License Tier** | Writer Pro | The minimum license required to load this. |
| **Feature Gate Key** | `Agents.Compression.Integration` | The string key used in `ILicenseService`. |
| **Author** | Lexichord Architecture | Primary Architect. |
| **Reviewer** | — | Lead Architect / Peer. |
| **Status** | Draft | Current lifecycle state. |
| **Last Updated** | 2026-02-03 | Date of last modification. |

---

## 2. Executive Summary

### 2.1 The Requirement
The compression engine must integrate seamlessly with the existing Context Assembler (v0.7.2) to provide compressed conversation history as a context strategy. This enables automatic context management without changes to agent code.

### 2.2 The Proposed Solution
Implement `CompressionContextStrategy` as an `IContextStrategy` that integrates with the Context Assembler. When gathering context, it uses the budget manager to assemble optimally compressed conversation history.

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies
*   **Upstream Modules:**
    *   `Lexichord.Modules.Agents.Compression` (v0.7.9a-g)
    *   `IContextOrchestrator` (v0.7.2c)
    *   `IContextStrategy` (v0.7.2a)

### 3.2 Licensing Behavior
*   **Load Behavior:**
    *   [x] **Soft Gate:** Strategy loads for Writer Pro; advanced features for Teams.

---

## 4. Data Contract (The API)

```csharp
namespace Lexichord.Modules.Agents.Compression;

/// <summary>
/// Context strategy that provides compressed conversation history.
/// </summary>
public class CompressionContextStrategy : IContextStrategy
{
    public string StrategyId => "compression";
    public int Priority => 10; // High priority for conversation context

    private readonly IContextCompressor _compressor;
    private readonly ICompressionStore _store;
    private readonly ITokenBudgetManager _budgetManager;

    /// <summary>
    /// Gathers compressed conversation context within the available budget.
    /// </summary>
    public async Task<ContextFragment?> GatherAsync(
        ContextGatheringRequest request,
        CancellationToken ct)
    {
        var conversationId = request.Hints?.GetValueOrDefault("conversation_id") as string;
        if (string.IsNullOrEmpty(conversationId)) return null;

        var remainingBudget = request.Hints?.GetValueOrDefault("remaining_budget") as int? ?? 4000;

        // Allocate budget across compression levels
        var allocation = await _budgetManager.AllocateAsync(
            conversationId,
            remainingBudget,
            new BudgetAllocationOptions(),
            ct);

        // Assemble content from allocated segments
        var content = await AssembleFromAllocationAsync(allocation, ct);

        return new ContextFragment(
            StrategyId,
            "Conversation History",
            content,
            allocation.UsedTokens,
            relevance: 0.9f);
    }

    private async Task<string> AssembleFromAllocationAsync(
        BudgetAllocation allocation,
        CancellationToken ct)
    {
        var builder = new StringBuilder();

        foreach (var segment in allocation.Segments.OrderBy(s => s.SegmentId))
        {
            var compressed = await _store.GetAsync(segment.SegmentId, segment.Level, ct);
            if (compressed != null)
            {
                builder.AppendLine(compressed.Content);
                builder.AppendLine();
            }
        }

        return builder.ToString();
    }
}
```

---

## 5. Implementation Logic

**Strategy Registration:**
```csharp
// In module registration
services.AddSingleton<IContextStrategy, CompressionContextStrategy>();
```

**Fallback Behavior:**
```csharp
public async Task<ContextFragment?> GatherAsync(ContextGatheringRequest request, CancellationToken ct)
{
    try
    {
        // Try compressed context
        return await GatherCompressedAsync(request, ct);
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "[COMP:STRATEGY] Falling back to uncompressed context");
        // Fall back to raw conversation history
        return await GatherRawAsync(request, ct);
    }
}
```

---

## 6. Observability & Logging

*   **Metric:** `Agents.Compression.Strategy.Invocations` (Counter)
*   **Metric:** `Agents.Compression.Strategy.TokensSaved` (Counter)
*   **Log (Info):** `[COMP:STRATEGY] Assembled context: {UsedTokens} tokens (saved {SavedTokens})`

---

## 7. Acceptance Criteria (QA)

1.  **[Functional]** Strategy SHALL return compressed conversation history within budget.
2.  **[Integration]** Strategy SHALL work with Context Orchestrator without modification.
3.  **[Fallback]** If compression fails, raw context SHALL be returned.
4.  **[Priority]** Strategy priority SHALL be high (10) to ensure conversation context is included.

---

## 8. Test Scenarios

```gherkin
Scenario: Strategy provides compressed context
    Given a conversation with 100 messages (50,000 tokens)
    And a budget of 8,000 tokens
    When GatherAsync is called
    Then a ContextFragment SHALL be returned
    And UsedTokens SHALL be <= 8,000

Scenario: Strategy falls back on error
    Given compression storage is unavailable
    When GatherAsync is called
    Then raw conversation context SHALL be returned
    And a warning SHALL be logged
```
