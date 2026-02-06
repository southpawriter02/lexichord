// =============================================================================
// File: Migration_013_QueryHistory.cs
// Project: Lexichord.Infrastructure
// Description: Creates query_history table for query history tracking (v0.5.4d).
// =============================================================================
// LOGIC: Implements database schema for Query History & Analytics feature:
//   - query_history table with columns for query tracking
//   - Optimized indexes for different access patterns:
//     * Recent queries (executed_at DESC)
//     * Zero-result queries (partial index)
//     * Query deduplication (query_hash)  
//     * Intent-based analytics (intent)
// =============================================================================
// VERSION: v0.5.4d (Query History & Analytics)
// =============================================================================

using FluentMigrator;

namespace Lexichord.Infrastructure.Migrations;

/// \u003csummary\u003e
/// Creates the query_history table for tracking search patterns.
/// \u003c/summary\u003e
/// \u003cremarks\u003e
/// \u003cpara\u003e
/// LOGIC: This migration creates the persistence layer for the Query History
/// feature (v0.5.4d), which tracks user search patterns for analytics and
/// quick re-access.
/// \u003c/para\u003e
/// \u003cpara\u003e
/// Features:
/// \u003clist type="bullet"\u003e
///   \u003citem\u003e\u003cdescription\u003eFull query text storage (up to 500 chars)\u003c/description\u003e\u003c/item\u003e
///   \u003citem\u003e\u003cdescription\u003eSHA256 hash-based deduplication\u003c/description\u003e\u003c/item\u003e
///   \u003citem\u003e\u003cdescription\u003eQuery intent tracking (v0.5.4a integration)\u003c/description\u003e\u003c/item\u003e
///   \u003citem\u003e\u003cdescription\u003eResult count and quality metrics\u003c/description\u003e\u003c/item\u003e
///   \u003citem\u003e\u003cdescription\u003ePerformance metrics (execution duration)\u003c/description\u003e\u003c/item\u003e
/// \u003c/list\u003e
/// \u003c/para\u003e
/// \u003cpara\u003e
/// Indexes:
/// \u003clist type="table"\u003e
///   \u003clistheader\u003e
///     \u003cterm\u003eIndex\u003c/term\u003e
///     \u003cdescription\u003ePurpose\u003c/description\u003e
///   \u003c/listheader\u003e
///   \u003citem\u003e
///     \u003cterm\u003eidx_query_history_executed_at\u003c/term\u003e
///     \u003cdescription\u003eRecent queries retrieval (DESC)\u003c/description\u003e
///   \u003c/item\u003e
///   \u003citem\u003e
///     \u003cterm\u003eidx_query_history_zero_results\u003c/term\u003e
///     \u003cdescription\u003ePartial index for content gap analysis\u003c/description\u003e
///   \u003c/item\u003e
///   \u003citem\u003e
///     \u003cterm\u003eidx_query_history_hash\u003c/term\u003e
///     \u003cdescription\u003eQuery deduplication\u003c/description\u003e
///   \u003c/item\u003e
///   \u003citem\u003e
///     \u003cterm\u003eidx_query_history_intent\u003c/term\u003e
///     \u003cdescription\u003eIntent-based analytics\u003c/description\u003e
///   \u003c/item\u003e
/// \u003c/list\u003e
/// \u003c/para\u003e
/// \u003c/remarks\u003e
[Migration(013_20260206)]
public class Migration_013_QueryHistory : LexichordMigration
{
    private const string TableName = "query_history";

    /// \u003csummary\u003e
    /// Creates the query_history table and indexes.
    /// \u003c/summary\u003e
    public override void Up()
    {
        // LOGIC: Create table with all tracking columns
        Create.Table(TableName)
            .WithColumn("id").AsCustom(MigrationConventions.UuidType).PrimaryKey()
            .WithColumn("query").AsString(500).NotNullable()
            .WithColumn("query_hash").AsString(64).NotNullable()  // SHA256 = 64 hex chars
            .WithColumn("intent").AsString(20).NotNullable()
            .WithColumn("result_count").AsInt32().NotNullable()
            .WithColumn("top_result_score").AsFloat().Nullable()  // Null if no results
            .WithColumn("executed_at").AsCustom(MigrationConventions.TimestampType).NotNullable()
            .WithColumn("duration_ms").AsInt64().NotNullable()
            .WithColumn("created_at").AsCustom(MigrationConventions.TimestampType).NotNullable()
                .WithDefaultValue(SystemMethods.CurrentUTCDateTime);

        // LOGIC: Index for recent queries (DESC for most recent first)
        Create.Index("idx_query_history_executed_at")
            .OnTable(TableName)
            .OnColumn("executed_at").Descending();

        // LOGIC: Partial index for zero-result queries (content gap analysis)
        // Only indexes rows where result_count = 0 to save space
        Execute.Sql(@"
            CREATE INDEX idx_query_history_zero_results 
            ON query_history (query, executed_at DESC) 
            WHERE result_count = 0;
        ");

        // LOGIC: Index for query deduplication via hash
        Create.Index("idx_query_history_hash")
            .OnTable(TableName)
            .OnColumn("query_hash");

        // LOGIC: Index for intent-based analytics
        Create.Index("idx_query_history_intent")
            .OnTable(TableName)
            .OnColumn("intent");
    }

    /// \u003csummary\u003e
    /// Drops the query_history table and all indexes.
    /// \u003c/summary\u003e
    public override void Down()
    {
        // LOGIC: Drop table (cascades to all indexes automatically)
        Delete.Table(TableName);
    }
}
