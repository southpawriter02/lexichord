# LCS-CL-054c: Query Suggestions

**Version:** v0.5.4c
**Date:** 2026-02
**Status:** ✅ Complete

## Summary

Implemented the Query Suggestion Service for providing autocomplete suggestions from indexed content, query history, and n-gram extraction, enhancing the search experience in the Relevance Tuner feature.

## Changes

### Abstractions (`Lexichord.Abstractions`)

| File                         | Change                                                    |
| ---------------------------- | --------------------------------------------------------- |
| `QuerySuggestion.cs`         | `QuerySuggestion` record with text, source, score, metadata|
| `QuerySuggestion.cs`         | `SuggestionSource` enum (QueryHistory, Heading, NGram, Term)|
| `QuerySuggestion.cs`         | Computed properties: `ScorePercent`, `SourceLabel`, `SourceIcon`|
| `IQuerySuggestionService.cs` | Interface for suggestions, recording, and extraction      |

### Database Schema (`Lexichord.Infrastructure`)

| File                            | Change                                              |
| ------------------------------- | --------------------------------------------------- |
| `Migration_007_QueryAnalytics.cs` | `query_suggestions` table with deduplication      |
| `Migration_007_QueryAnalytics.cs` | Prefix index for efficient LIKE 'prefix%' queries |
| `Migration_007_QueryAnalytics.cs` | Frequency index for popularity sorting            |
| `Migration_007_QueryAnalytics.cs` | Document ID index for re-index cleanup            |

### Services (`Lexichord.Modules.RAG`)

| File                         | Change                                                  |
| ---------------------------- | ------------------------------------------------------- |
| `QuerySuggestionService.cs`  | Core service with database-backed storage               |
| `QuerySuggestionService.cs`  | Prefix matching with frequency-weighted scoring         |
| `QuerySuggestionService.cs`  | N-gram extraction from headings, bold text, code blocks |
| `QuerySuggestionService.cs`  | 30-second cache TTL for suggestion results              |
| `QuerySuggestionService.cs`  | License gating (WriterPro+ tier)                        |

### Query Suggestions Table Schema

```sql
CREATE TABLE query_suggestions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    text TEXT NOT NULL,
    normalized_text TEXT NOT NULL,
    source TEXT NOT NULL CHECK (source IN ('query_history', 'heading', 'ngram', 'term')),
    frequency INT NOT NULL DEFAULT 1,
    last_seen_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    document_id UUID,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_query_suggestions_normalized_source UNIQUE (normalized_text, source)
);
```

### Indexes

| Index                              | Purpose                          |
| ---------------------------------- | -------------------------------- |
| `idx_query_suggestions_prefix`     | Prefix matching (text_pattern_ops)|
| `idx_query_suggestions_frequency`  | Popularity sorting (DESC)        |
| `idx_query_suggestions_document_id`| Document cleanup on re-index     |

### N-Gram Extraction

Content sources for suggestion extraction:
- **Headings**: H1-H6 heading text from Markdown documents
- **Bold Text**: Emphasized phrases (`**text**` or `__text__`)
- **Code Identifiers**: PascalCase/camelCase identifiers from code blocks
- **Domain Terms**: Entries from the terminology database

### Scoring Algorithm

Suggestions are scored (0.0-1.0) based on:
- Prefix match: 1.0 base if query is prefix of suggestion
- Frequency bonus: +0.1 per 10 occurrences (max +0.3)
- Source weight: Heading (0.9), Term (0.85), History (0.8), NGram (0.7)

## Tests

| File                          | Tests                                             |
| ----------------------------- | ------------------------------------------------- |
| `QuerySuggestionTests.cs`     | 4 tests - Record properties and defaults          |
| `QuerySuggestionTests.cs`     | 3 tests - ScorePercent calculation                |
| `QuerySuggestionTests.cs`     | 4 tests - SourceLabel and SourceIcon mapping      |
| `QuerySuggestionTests.cs`     | 4 tests - Record equality and immutability        |

**Total: 15 unit tests**

## License Gating

- Feature Code: `FeatureFlags.RAG.RelevanceTuner`
- Minimum Tier: Writer Pro
- Fallback: Returns empty suggestions for Core tier

## Dependencies

- Microsoft.Extensions.Logging (existing)
- Dapper (existing, for database queries)
- Npgsql (existing, for PostgreSQL connection)
- ILicenseContext (existing, for license verification)
