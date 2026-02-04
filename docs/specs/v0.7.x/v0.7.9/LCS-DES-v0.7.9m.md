# LDS-01: Feature Design Specification — Handoff Storage

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `CONT-04` | Matches the Roadmap ID. |
| **Feature Name** | Handoff Storage | The internal display name. |
| **Target Version** | `v0.7.9m` | The semantic version target. |
| **Module Scope** | `Lexichord.Modules.Agents` | The specific DLL/Project this code lives in. |
| **Swimlane** | Memory | The functional vertical. |
| **License Tier** | Writer Pro | The minimum license required to load this. |
| **Feature Gate Key** | `Agents.Continuity.Storage` | The string key used in `ILicenseService`. |
| **Author** | Lexichord Architecture | Primary Architect. |
| **Reviewer** | — | Lead Architect / Peer. |
| **Status** | Draft | Current lifecycle state. |
| **Last Updated** | 2026-02-03 | Date of last modification. |

---

## 2. Executive Summary

### 2.1 The Requirement
Session handoff packages need persistent storage for reliable resumption, even after application restarts. Additionally, multi-hop handoff chains need tracking for long-running work spanning many sessions.

### 2.2 The Proposed Solution
Implement `IHandoffStore` for CRUD operations on session handoffs with TTL-based cleanup and handoff chain tracking.

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies
*   **Upstream Modules:**
    *   `Lexichord.Modules.Agents.Continuity` (v0.7.9j models)
*   **NuGet Packages:**
    *   `FluentMigrator`
    *   `Dapper`

---

## 4. Data Contract (The API)

```csharp
namespace Lexichord.Modules.Agents.Continuity.Abstractions;

/// <summary>
/// Persistent storage for session handoffs.
/// </summary>
public interface IHandoffStore
{
    Task<SessionHandoff> StoreAsync(SessionHandoff handoff, CancellationToken ct = default);
    Task<SessionHandoff?> GetAsync(string handoffId, CancellationToken ct = default);
    Task<SessionHandoff?> GetLatestForConversationAsync(string conversationId, CancellationToken ct = default);
    Task MarkResumedAsync(string handoffId, string newConversationId, CancellationToken ct = default);
    Task<HandoffChain?> GetChainAsync(string rootConversationId, CancellationToken ct = default);
    Task CleanupExpiredAsync(CancellationToken ct = default);
}

public record HandoffChain(
    string ChainId,
    string RootConversationId,
    IReadOnlyList<SessionHandoff> Handoffs,
    int TotalTokensProcessed,
    DateTimeOffset StartedAt,
    DateTimeOffset LastActivityAt);
```

---

## 5. Data Persistence (Database)

*   **Migration ID:** `20260203_1700_AddHandoffStorageTables`
*   **Module Schema:** `agents`

### Schema Definition

```sql
CREATE TABLE session_handoffs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    conversation_id TEXT NOT NULL,
    previous_handoff_id UUID REFERENCES session_handoffs(id),
    compacted_summary TEXT NOT NULL,
    anchors JSONB NOT NULL,
    pending_tasks JSONB NOT NULL,
    session_state JSONB,
    metadata JSONB NOT NULL,
    continuation_directive JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    resumed_at TIMESTAMPTZ,
    resumed_conversation_id TEXT,
    expires_at TIMESTAMPTZ NOT NULL DEFAULT NOW() + INTERVAL '30 days'
);

CREATE INDEX idx_handoffs_conversation ON session_handoffs(conversation_id);
CREATE INDEX idx_handoffs_expires ON session_handoffs(expires_at) WHERE resumed_at IS NULL;

CREATE TABLE handoff_chains (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    root_conversation_id TEXT NOT NULL,
    current_handoff_id UUID REFERENCES session_handoffs(id),
    total_handoffs INTEGER NOT NULL DEFAULT 1,
    total_tokens_processed INTEGER NOT NULL,
    started_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_activity_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_chains_root ON handoff_chains(root_conversation_id);
```

---

## 6. Observability & Logging

*   **Metric:** `Agents.Continuity.Storage.HandoffCount` (Gauge)
*   **Metric:** `Agents.Continuity.Storage.ChainLength` (Histogram)
*   **Log (Info):** `[CONT:STORE] Stored handoff {HandoffId} for conversation {ConversationId}`

---

## 7. Acceptance Criteria (QA)

1.  **[Functional]** `StoreAsync` SHALL persist handoff to database.
2.  **[Functional]** `GetLatestForConversationAsync` SHALL return most recent handoff.
3.  **[Chains]** `GetChainAsync` SHALL return full handoff chain history.
4.  **[Cleanup]** `CleanupExpiredAsync` SHALL delete expired, unresumed handoffs.

---

## 8. Test Scenarios

```gherkin
Scenario: Store and retrieve handoff
    Given a session handoff package
    When StoreAsync is called
    And GetAsync is called with the handoff ID
    Then the retrieved handoff SHALL match the stored one

Scenario: Track handoff chain
    Given conversation A handed off to B, then B to C
    When GetChainAsync is called with A's conversation ID
    Then Handoffs SHALL contain all 3 handoff records
    And TotalHandoffs SHALL be 3
```
