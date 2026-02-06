// =============================================================================
// File: QueryHistoryEntity.cs
// Project: Lexichord.Modules.RAG
// Description: Dapper entity for query_history table mapping.
// =============================================================================
// LOGIC: Database entity for persisting QueryHistoryEntry records.
//   Maps between domain contract (QueryHistoryEntry) and database schema.
// =============================================================================
// VERSION: v0.5.4d (Query History & Analytics)
// =============================================================================

using System;

namespace Lexichord.Modules.RAG.Data;

/// \u003csummary\u003e
/// Entity mapping for the query_history table.
/// \u003c/summary\u003e
/// \u003cremarks\u003e
/// \u003cpara\u003e
/// LOGIC: This class uses Dapper's convention-based mapping. Column names
/// match database schema exactly (snake_case).
/// \u003c/para\u003e
/// \u003cpara\u003e
/// Conversion to/from \u003csee cref="Lexichord.Abstractions.Contracts.RAG.QueryHistoryEntry"/\u003e
/// is handled by \u003csee cref="Services.QueryHistoryService"/\u003e.
/// \u003c/para\u003e
/// \u003cpara\u003e
/// \u003cb\u003eIntroduced in:\u003c/b\u003e v0.5.4d as part of The Relevance Tuner feature.
/// \u003c/para\u003e
/// \u003c/remarks\u003e
internal sealed class QueryHistoryEntity
{
    /// \u003csummary\u003e
    /// Unique identifier (UUID).
    /// \u003c/summary\u003e
    public Guid id { get; set; }

    /// \u003csummary\u003e
    /// The search query text (max 500 chars).
    /// \u003c/summary\u003e
    public string query { get; set; } = string.Empty;

    /// \u003csummary\u003e
    /// SHA256 hash of the query for deduplication.
    /// \u003c/summary\u003e
    public string query_hash { get; set; } = string.Empty;

    /// \u003csummary\u003e
    /// Query intent as string (e.g., "Factual", "Procedural").
    /// \u003c/summary\u003e
    public string intent { get; set; } = string.Empty;

    /// \u003csummary\u003e
    /// Number of results returned.
    /// \u003c/summary\u003e
    public int result_count { get; set; }

    /// \u003csummary\u003e
    /// Score of the top result (null if no results).
    /// \u003c/summary\u003e
    public float? top_result_score { get; set; }

    /// \u003csummary\u003e
    /// When the query was executed (UTC).
    /// \u003c/summary\u003e
    public DateTime executed_at { get; set; }

    /// \u003csummary\u003e
    /// Query execution duration in milliseconds.
    /// \u003c/summary\u003e
    public long duration_ms { get; set; }

    /// \u003csummary\u003e
    /// When this record was created (UTC).
    /// \u003c/summary\u003e
    public DateTime created_at { get; set; }
}
