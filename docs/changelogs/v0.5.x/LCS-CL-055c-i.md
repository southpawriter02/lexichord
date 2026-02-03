# Changelog: v0.5.5c-i

## Document Information

| Field | Value |
|-------|-------|
| **Document ID** | LCS-CL-055c-i |
| **Version** | v0.5.5c-i |
| **Component** | Filter Query Builder + Linking Review UI |
| **Status** | Released |
| **Release Date** | 2026-02-03 |

---

## Overview

This release implements two components:

1. **v0.5.5c - Filter Query Builder**: Translates `SearchFilter` criteria into parameterized SQL for efficient filtered vector search.
2. **v0.5.5i (KG-i) - Linking Review UI**: Provides a streamlined interface for human review of entity links.

---

## Changes

### Added

#### Filter Query Builder (v0.5.5c)

- `IFilterQueryBuilder` interface in `Lexichord.Abstractions.Contracts`
  - `Build(SearchFilter)` - Builds SQL query components from filter criteria
  - `ConvertGlobToSql(string)` - Converts glob patterns to SQL LIKE expressions
  - `TryBuild(SearchFilter, out errors)` - Validates and builds with error reporting
  - `LastResult` property - Debug access to most recent build result

- `FilterQueryResult` record in `Lexichord.Abstractions.Contracts`
  - `WhereClause` - SQL WHERE conditions (without keyword)
  - `Parameters` - Named parameters for safe SQL execution
  - `JoinClause` - Optional JOIN for cross-table filtering
  - `CteClause` - Optional CTE for efficient filtered lookups
  - `HasFilters` - Whether any criteria are applied
  - `FilterCount` - Count of active criteria
  - `Summary` - Human-readable filter description
  - `Empty` static property - Pre-built empty result

- `FilterQueryBuilder` service in `Lexichord.Modules.RAG.Search`
  - Glob-to-SQL conversion (`**` → `%`, `*` → `%`, `?` → `_`)
  - Path pattern LIKE clauses (OR'd for multiple patterns)
  - Extension array ANY() clauses (case-insensitive)
  - Date range BETWEEN conditions
  - Heading IS NOT NULL checks (chunk-level)
  - CTE pattern for HNSW index preservation
  - Thread-safe, stateless design

#### Linking Review UI (v0.5.5i / KG-i)

- `ILinkingReviewService` interface in `Lexichord.Abstractions.Contracts`
  - `GetPendingAsync(ReviewFilter?, CancellationToken)` - Retrieves pending links
  - `GetPendingCountAsync(CancellationToken)` - Gets pending count
  - `SubmitDecisionAsync(LinkReviewDecision, CancellationToken)` - Submits single decision
  - `SubmitDecisionsBatchAsync(IReadOnlyList<LinkReviewDecision>, CancellationToken)` - Batch submission
  - `GetStatsAsync(CancellationToken)` - Retrieves review statistics
  - `QueueChanged` event - Notification for queue updates

- `PendingLinkItem` record - Entity link pending human review
  - Mention details, proposed entity, candidates list
  - Document context and extended context
  - Group support for similar mentions
  - AI-suggested decisions

- `LinkReviewDecision` record - Review decision capture
  - Action (Accept, Reject, SelectAlternate, CreateNew, Skip, NotAnEntity)
  - Selected entity or new entity properties
  - Group decision support
  - Reviewer tracking

- `ReviewStats` record - Queue statistics
  - Pending count, reviewed today, total reviewed
  - Acceptance rate, average review time
  - Breakdown by action and entity type
  - Top reviewers list

- `ReviewFilter` record - Queue filter criteria
  - Document, entity type, confidence range filtering
  - Sort order options (Priority, Confidence, CreatedAt, DocumentOrder)
  - Configurable result limit

- `LinkingReviewViewModel` in `Lexichord.Modules.Knowledge.UI.ViewModels.LinkingReview`
  - Queue loading with filtering and sorting
  - Accept, Reject, Skip, CreateNew, MarkNotEntity commands
  - SelectAlternate command for choosing different candidates
  - Group decision support (ApplyToGroup)
  - Statistics tracking and display
  - Entity type filter dropdown

- `LinkingReviewPanel.axaml` in `Lexichord.Modules.Knowledge.UI.Views`
  - Three-column layout (Queue, Context, Candidates)
  - Statistics card with pending count and acceptance rate
  - Filter bar with entity type and sort order
  - Mention context view with highlighted text
  - Candidate list with scores
  - Action buttons with keyboard shortcuts
  - Group checkbox for bulk decisions

### DI Registrations

- `IFilterQueryBuilder → FilterQueryBuilder` (Singleton) in RAG module
- `LinkingReviewViewModel` (Transient) in Knowledge module

### Unit Tests

- `FilterQueryBuilderTests.cs` - 20+ tests covering:
  - Empty filter handling
  - Path pattern conversion (glob to SQL)
  - Extension filtering
  - Date range conditions
  - Heading filters
  - Combined criteria
  - Validation error handling
  - TryBuild with error reporting

- `LinkingReviewViewModelTests.cs` - 15+ tests covering:
  - Loading pending items
  - Accept/Reject/Skip commands
  - Select alternate candidates
  - Group decision application
  - CreateNew and NotEntity actions
  - Selection management

---

## Dependencies

### Upstream (Required)

| Component | Version | Purpose |
|-----------|---------|---------|
| SearchFilter | v0.5.5a | Filter model records |
| DateRange | v0.5.5a | Date range filtering |
| IFilterValidator | v0.5.5a | Filter validation |
| EntityMention | v0.4.5g | Mention data structure |
| IGraphRepository | v0.4.7e | Entity storage |

### Downstream (Consumers)

| Component | Version | Usage |
|-----------|---------|-------|
| PgVectorSearchService | v0.4.5b | Will use IFilterQueryBuilder for filtered search |
| HybridSearchService | v0.5.1c | Will use IFilterQueryBuilder for filtered search |
| EntityLinkingService | v0.5.5g | Will provide pending links to ILinkingReviewService |

---

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Ctrl+A | Accept proposed link |
| Ctrl+R | Reject link |
| Ctrl+S | Skip (defer for later) |
| Ctrl+N | Create new entity |

---

## License Gating

| Feature | Tier | Behavior |
|---------|------|----------|
| Filter Query Builder | All | Available to all tiers |
| Linking Review (view) | WriterPro | View-only queue access |
| Linking Review (edit) | Teams | Full review capabilities |

---

## Technical Notes

### Filter Query Builder Strategy

The builder generates CTEs (Common Table Expressions) to pre-filter documents before vector search, ensuring the HNSW index is still used efficiently:

```sql
WITH filtered_docs AS (
    SELECT id FROM documents
    WHERE file_path LIKE @pathPattern0
      AND LOWER(file_extension) = ANY(@extensions)
      AND modified_at >= @modifiedStart
)
SELECT c.*, c.embedding <=> @vector AS distance
FROM chunks c
WHERE c.document_id IN (SELECT id FROM filtered_docs)
  AND c.heading IS NOT NULL
ORDER BY c.embedding <=> @vector
LIMIT @topK;
```

### Glob-to-SQL Conversion

| Glob | SQL LIKE |
|------|----------|
| `**` | `%` |
| `*` | `%` |
| `?` | `_` |
| `%` | `\%` (escaped) |
| `_` | `\_` (escaped) |

### Group Decision Flow

When `ApplyToGroup` is enabled:
1. Decision is submitted for the selected item
2. All items with matching `GroupId` are removed from the local queue
3. Service applies decision to all grouped mentions in the backend

---

## Migration Notes

No database migrations required for this release. The filter query builder works with existing schema.

---

## Related Documents

- [LCS-DES-v0.5.5c.md](../specs/v0.5.x/v0.5.5/LCS-DES-v0.5.5c.md) - Filter Query Builder specification
- [LCS-DES-v0.5.5-KG-i.md](../specs/v0.5.x/v0.5.5/LCS-DES-v0.5.5-KG-i.md) - Linking Review UI specification
- [LCS-CL-055a.md](LCS-CL-055a.md) - v0.5.5a changelog (Filter Model)
- [LCS-CL-055b.md](LCS-CL-055b.md) - v0.5.5b changelog (Filter UI Component)
