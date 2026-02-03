// =============================================================================
// File: Migration_007_QueryAnalytics.cs
// Project: Lexichord.Infrastructure
// Description: Database migration for query analytics tables (v0.5.4c-d).
// =============================================================================
// LOGIC: Creates tables for query suggestions and history tracking:
//   - QuerySuggestions: Autocomplete suggestions from multiple sources
//   - QueryHistory: Executed query tracking for analytics
// =============================================================================
// VERSION: v0.5.4c-d (Query Suggestions & History)
// DEPENDENCIES:
//   - Migration_003_VectorSchema (Documents table for foreign key)
// =============================================================================

using FluentMigrator;

namespace Lexichord.Infrastructure.Migrations;

/// <summary>
/// Migration for query analytics tables (suggestions and history).
/// </summary>
/// <remarks>
/// <para>
/// This migration creates the schema for the Relevance Tuner feature:
/// </para>
/// <para>
/// <b>QuerySuggestions Table:</b>
/// Stores autocomplete suggestions from multiple sources:
/// <list type="bullet">
///   <item><description>Query history (previously executed searches)</description></item>
///   <item><description>Document headings (extracted during indexing)</description></item>
///   <item><description>Content n-grams (common phrases)</description></item>
///   <item><description>Domain terms (from terminology database)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>QueryHistory Table:</b>
/// Tracks executed queries with metadata for:
/// <list type="bullet">
///   <item><description>Recent queries quick-access panel</description></item>
///   <item><description>Zero-result query identification (content gaps)</description></item>
///   <item><description>Search performance monitoring</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Privacy:</b> Query history is stored locally only. The QueryHash column
/// enables anonymous aggregation without exposing query text.
/// </para>
/// <para>
/// v0.5.4c: Query Suggestions schema
/// v0.5.4d: Query History schema
/// </para>
/// </remarks>
[Migration(7, "Query analytics schema for Relevance Tuner")]
[Tags("RAG", "RelevanceTuner", "QueryAnalytics")]
public class Migration_007_QueryAnalytics : LexichordMigration
{
    #region Table Names

    /// <summary>
    /// Name of the query suggestions table.
    /// </summary>
    private const string SuggestionsTable = "query_suggestions";

    /// <summary>
    /// Name of the query history table.
    /// </summary>
    private const string HistoryTable = "query_history";

    #endregion

    /// <summary>
    /// Applies the migration: creates QuerySuggestions and QueryHistory tables.
    /// </summary>
    public override void Up()
    {
        CreateQuerySuggestionsTable();
        CreateQueryHistoryTable();
    }

    /// <summary>
    /// Reverts the migration: drops QueryHistory and QuerySuggestions tables.
    /// </summary>
    public override void Down()
    {
        // LOGIC: Drop in reverse dependency order
        Execute.Sql($@"DROP TABLE IF EXISTS ""{HistoryTable}"" CASCADE;");
        Execute.Sql($@"DROP TABLE IF EXISTS ""{SuggestionsTable}"" CASCADE;");
    }

    #region QuerySuggestions Table

    /// <summary>
    /// Creates the query_suggestions table with indexes.
    /// </summary>
    private void CreateQuerySuggestionsTable()
    {
        // ═══════════════════════════════════════════════════════════════════════
        // Create QuerySuggestions Table
        // ═══════════════════════════════════════════════════════════════════════
        // LOGIC: Stores autocomplete suggestions with deduplication via
        // unique constraint on (normalized_text, source).
        Execute.Sql($@"
            CREATE TABLE ""{SuggestionsTable}"" (
                -- Primary key
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

                -- Suggestion content
                text TEXT NOT NULL,
                normalized_text TEXT NOT NULL,  -- Lowercase, trimmed for deduplication

                -- Source classification
                source TEXT NOT NULL CHECK (source IN ('query_history', 'heading', 'ngram', 'term')),

                -- Frequency tracking
                frequency INT NOT NULL DEFAULT 1,
                last_seen_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

                -- Optional source document reference
                document_id UUID,

                -- Timestamps
                created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

                -- Unique constraint for deduplication
                CONSTRAINT uq_{SuggestionsTable}_normalized_source UNIQUE (normalized_text, source)
            );
        ");

        // ═══════════════════════════════════════════════════════════════════════
        // Create Indexes
        // ═══════════════════════════════════════════════════════════════════════

        // LOGIC: Prefix index for efficient LIKE 'prefix%' queries.
        // text_pattern_ops enables index usage for pattern matching.
        Execute.Sql($@"
            CREATE INDEX idx_{SuggestionsTable}_prefix
            ON ""{SuggestionsTable}"" (normalized_text text_pattern_ops);
        ");

        // LOGIC: Frequency index for sorting by popularity.
        Execute.Sql($@"
            CREATE INDEX idx_{SuggestionsTable}_frequency
            ON ""{SuggestionsTable}"" (frequency DESC);
        ");

        // LOGIC: Document ID index for clearing suggestions on re-index.
        Execute.Sql($@"
            CREATE INDEX idx_{SuggestionsTable}_document_id
            ON ""{SuggestionsTable}"" (document_id)
            WHERE document_id IS NOT NULL;
        ");

        // ═══════════════════════════════════════════════════════════════════════
        // Table Comments
        // ═══════════════════════════════════════════════════════════════════════
        Execute.Sql($@"
            COMMENT ON TABLE ""{SuggestionsTable}"" IS
            'Query autocomplete suggestions from multiple sources (v0.5.4c). Supports query history, document headings, content n-grams, and domain terms.';

            COMMENT ON COLUMN ""{SuggestionsTable}"".normalized_text IS
            'Lowercase, trimmed text for deduplication and prefix matching.';

            COMMENT ON COLUMN ""{SuggestionsTable}"".source IS
            'Suggestion source: query_history, heading, ngram, or term.';

            COMMENT ON COLUMN ""{SuggestionsTable}"".frequency IS
            'How often this suggestion has been used/seen. Incremented on each occurrence.';

            COMMENT ON COLUMN ""{SuggestionsTable}"".document_id IS
            'Source document for heading/ngram suggestions. Used for cleanup on re-index.';
        ");
    }

    #endregion

    #region QueryHistory Table

    /// <summary>
    /// Creates the query_history table with indexes.
    /// </summary>
    private void CreateQueryHistoryTable()
    {
        // ═══════════════════════════════════════════════════════════════════════
        // Create QueryHistory Table
        // ═══════════════════════════════════════════════════════════════════════
        // LOGIC: Stores executed queries with metadata for analytics and
        // content gap identification (zero-result queries).
        Execute.Sql($@"
            CREATE TABLE ""{HistoryTable}"" (
                -- Primary key
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

                -- Query content
                query TEXT NOT NULL,
                query_hash TEXT NOT NULL,  -- SHA256 hash for anonymous aggregation

                -- Classification
                intent TEXT NOT NULL CHECK (intent IN ('Factual', 'Procedural', 'Conceptual', 'Navigational')),

                -- Results
                result_count INT NOT NULL,
                top_result_score REAL,  -- Null if no results

                -- Execution metadata
                executed_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                duration_ms BIGINT NOT NULL
            );
        ");

        // ═══════════════════════════════════════════════════════════════════════
        // Create Indexes
        // ═══════════════════════════════════════════════════════════════════════

        // LOGIC: Execution time index for recent queries lookup.
        Execute.Sql($@"
            CREATE INDEX idx_{HistoryTable}_executed_at
            ON ""{HistoryTable}"" (executed_at DESC);
        ");

        // LOGIC: Result count index for zero-result query analysis.
        Execute.Sql($@"
            CREATE INDEX idx_{HistoryTable}_result_count
            ON ""{HistoryTable}"" (result_count)
            WHERE result_count = 0;
        ");

        // LOGIC: Query hash index for deduplication and aggregation.
        Execute.Sql($@"
            CREATE INDEX idx_{HistoryTable}_query_hash
            ON ""{HistoryTable}"" (query_hash);
        ");

        // ═══════════════════════════════════════════════════════════════════════
        // Table Comments
        // ═══════════════════════════════════════════════════════════════════════
        Execute.Sql($@"
            COMMENT ON TABLE ""{HistoryTable}"" IS
            'Executed query history for analytics and content gap analysis (v0.5.4d). Stored locally only.';

            COMMENT ON COLUMN ""{HistoryTable}"".query_hash IS
            'SHA256 hash of normalized query text. Used for anonymous aggregation and deduplication.';

            COMMENT ON COLUMN ""{HistoryTable}"".intent IS
            'Detected query intent: Factual, Procedural, Conceptual, or Navigational.';

            COMMENT ON COLUMN ""{HistoryTable}"".result_count IS
            'Number of search results returned. Zero indicates potential content gap.';

            COMMENT ON COLUMN ""{HistoryTable}"".top_result_score IS
            'Relevance score of best result. Null when result_count is 0.';

            COMMENT ON COLUMN ""{HistoryTable}"".duration_ms IS
            'Query execution time in milliseconds for performance monitoring.';
        ");
    }

    #endregion
}
