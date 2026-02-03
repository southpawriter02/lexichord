# LCS-CL-054d: Query History & Analytics

**Version:** v0.5.4d
**Date:** 2026-02
**Status:** ✅ Complete

## Summary

Implemented the Query History Service for tracking executed queries, identifying zero-result queries (content gaps), and publishing analytics events via MediatR for opt-in telemetry in the Relevance Tuner feature.

## Changes

### Abstractions (`Lexichord.Abstractions`)

| File                        | Change                                                     |
| --------------------------- | ---------------------------------------------------------- |
| `QueryHistory.cs`           | `QueryHistoryEntry` record with full query metadata        |
| `QueryHistory.cs`           | Computed properties: `HasResults`, `IsZeroResult`, `DurationDisplay`, `RelativeTime`|
| `QueryHistory.cs`           | `ZeroResultQuery` record with occurrence count and priority|
| `IQueryHistoryService.cs`   | Interface for recording, retrieval, and analytics          |

### Database Schema (`Lexichord.Infrastructure`)

| File                            | Change                                              |
| ------------------------------- | --------------------------------------------------- |
| `Migration_007_QueryAnalytics.cs` | `query_history` table with execution metadata     |
| `Migration_007_QueryAnalytics.cs` | `executed_at` index for recent queries lookup     |
| `Migration_007_QueryAnalytics.cs` | `result_count` partial index for zero-result queries|
| `Migration_007_QueryAnalytics.cs` | `query_hash` index for aggregation                |

### Events (`Lexichord.Modules.RAG`)

| File                       | Change                                               |
| -------------------------- | ---------------------------------------------------- |
| `QueryAnalyticsEvent.cs`   | MediatR notification for opt-in telemetry            |
| `QueryAnalyticsEvent.cs`   | Contains query hash, intent, result count, duration  |

### Services (`Lexichord.Modules.RAG`)

| File                      | Change                                                 |
| ------------------------- | ------------------------------------------------------ |
| `QueryHistoryService.cs`  | Core service with database-backed storage              |
| `QueryHistoryService.cs`  | SHA256 query hashing for anonymous aggregation         |
| `QueryHistoryService.cs`  | 60-second deduplication window for repeated queries    |
| `QueryHistoryService.cs`  | Zero-result query aggregation and prioritization       |
| `QueryHistoryService.cs`  | MediatR event publishing for analytics                 |

### Query History Table Schema

```sql
CREATE TABLE query_history (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    query TEXT NOT NULL,
    query_hash TEXT NOT NULL,
    intent TEXT NOT NULL CHECK (intent IN ('Factual', 'Procedural', 'Conceptual', 'Navigational')),
    result_count INT NOT NULL,
    top_result_score REAL,
    executed_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    duration_ms BIGINT NOT NULL
);
```

### Indexes

| Index                          | Purpose                              |
| ------------------------------ | ------------------------------------ |
| `idx_query_history_executed_at`| Recent queries lookup (DESC)         |
| `idx_query_history_result_count`| Zero-result query filtering (WHERE=0)|
| `idx_query_history_query_hash` | Deduplication and aggregation        |

### QueryHistoryEntry Computed Properties

| Property          | Logic                                              |
| ----------------- | -------------------------------------------------- |
| `HasResults`      | `ResultCount > 0`                                  |
| `IsZeroResult`    | `ResultCount == 0`                                 |
| `DurationDisplay` | `< 1000ms`: "Xms", `>= 1000ms`: "X.Xs"            |
| `RelativeTime`    | "just now", "X min ago", "X hours ago", "X days ago"|

### ZeroResultQuery Priority Calculation

| Occurrence Count | Priority |
| ---------------- | -------- |
| 1-4              | Low      |
| 5-9              | Medium   |
| 10+              | High     |

### Privacy Features

- Query text is stored locally only
- `query_hash` (SHA256) enables anonymous aggregation
- No query text is included in telemetry events
- Users can clear history via `ClearAsync()`

### Module Registration (`RAGModule.cs`)

| Service                  | Lifetime   |
| ------------------------ | ---------- |
| `IQueryAnalyzer`         | Scoped     |
| `IQueryExpander`         | Scoped     |
| `IQuerySuggestionService`| Scoped     |
| `IQueryHistoryService`   | Scoped     |

Module version bumped from 0.5.3 to 0.5.4.

## Tests

| File                      | Tests                                              |
| ------------------------- | -------------------------------------------------- |
| `QueryHistoryTests.cs`    | 3 tests - QueryHistoryEntry properties             |
| `QueryHistoryTests.cs`    | 3 tests - HasResults and IsZeroResult              |
| `QueryHistoryTests.cs`    | 5 tests - DurationDisplay formatting               |
| `QueryHistoryTests.cs`    | 5 tests - RelativeTime calculation                 |
| `QueryHistoryTests.cs`    | 2 tests - Record equality and with-expression      |
| `QueryHistoryTests.cs`    | 5 tests - ZeroResultQuery properties and priority  |
| `QueryHistoryTests.cs`    | 1 test - QueryIntent enum values                   |

**Total: 24 unit tests**

## License Gating

- Feature Code: None (Core analytics feature)
- Minimum Tier: Core (available to all users)
- Note: Query expansion suggestions (v0.5.4b-c) require Writer Pro

## Dependencies

- Microsoft.Extensions.Logging (existing)
- Dapper (existing, for database queries)
- Npgsql (existing, for PostgreSQL connection)
- MediatR (existing, for analytics events)
- System.Security.Cryptography (framework, for SHA256)
