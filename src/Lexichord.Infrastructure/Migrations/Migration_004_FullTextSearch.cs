using FluentMigrator;

namespace Lexichord.Infrastructure.Migrations;

/// <summary>
/// Migration for PostgreSQL full-text search support on the Chunks table.
/// </summary>
/// <remarks>
/// <para>
/// LOGIC: This migration extends the Chunks table with full-text search capabilities
/// required for BM25-style keyword search in the hybrid search engine.
/// </para>
/// <para>
/// Schema Changes:
/// </para>
/// <list type="bullet">
///   <item>
///     <description>
///       <c>ContentTsvector</c> — Generated TSVECTOR column that automatically maintains
///       a full-text search index of the Content column using the 'english' text search
///       configuration. Uses STORED to persist the computed value for query performance.
///     </description>
///   </item>
///   <item>
///     <description>
///       <c>IX_Chunks_ContentTsvector_gin</c> — GIN (Generalized Inverted Index) on the
///       tsvector column for fast full-text search queries using @@ operator.
///     </description>
///   </item>
/// </list>
/// <para>
/// v0.5.1a: BM25 Index Schema
/// Dependencies: v0.4.1b (Migration_003_VectorSchema - Chunks table)
/// </para>
/// </remarks>
[Migration(4, "Full-text search schema for BM25 hybrid search")]
[Tags("RAG", "FullTextSearch", "BM25")]
public class Migration_004_FullTextSearch : LexichordMigration
{
    /// <summary>
    /// Name of the Chunks table containing text content.
    /// </summary>
    private const string ChunksTable = "Chunks";

    /// <summary>
    /// Name of the generated tsvector column for full-text search.
    /// </summary>
    private const string TsvectorColumn = "ContentTsvector";

    /// <summary>
    /// Name of the GIN index on the tsvector column.
    /// </summary>
    private const string GinIndexName = "IX_Chunks_ContentTsvector_gin";

    /// <summary>
    /// Applies the migration: adds the content_tsvector column and GIN index.
    /// </summary>
    /// <remarks>
    /// <para>
    /// LOGIC: Uses a GENERATED ALWAYS ... STORED column to automatically maintain
    /// the tsvector representation of the Content column. PostgreSQL handles all
    /// updates transparently—no triggers or application code required.
    /// </para>
    /// <para>
    /// The 'english' text search configuration provides:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Stemming (running → run)</description></item>
    ///   <item><description>Stop word removal (the, a, an)</description></item>
    ///   <item><description>Normalization for consistent matching</description></item>
    /// </list>
    /// <para>
    /// GIN index enables fast @@-based full-text queries with sub-millisecond
    /// performance even on large datasets.
    /// </para>
    /// </remarks>
    public override void Up()
    {
        // ═══════════════════════════════════════════════════════════════════════
        // Add Generated TSVECTOR Column
        // ═══════════════════════════════════════════════════════════════════════
        // LOGIC: GENERATED ALWAYS AS ... STORED creates a computed column that:
        //   1. Is automatically populated on INSERT
        //   2. Is automatically updated when Content changes
        //   3. Is physically stored for query performance (vs virtual)
        //   4. Cannot be directly written to (enforced by PostgreSQL)
        Execute.Sql($@"
            ALTER TABLE ""{ChunksTable}""
            ADD COLUMN ""{TsvectorColumn}"" TSVECTOR
            GENERATED ALWAYS AS (to_tsvector('english', ""Content"")) STORED;
        ");

        // ═══════════════════════════════════════════════════════════════════════
        // Create GIN Index for Fast Full-Text Search
        // ═══════════════════════════════════════════════════════════════════════
        // LOGIC: GIN (Generalized Inverted Index) is optimized for containment queries.
        // It maps each lexeme to the rows containing it, enabling fast @@ lookups.
        // Trade-off: Slower writes, but much faster reads for full-text search.
        Execute.Sql($@"
            CREATE INDEX ""{GinIndexName}"" ON ""{ChunksTable}""
            USING GIN (""{TsvectorColumn}"");
        ");

        // ═══════════════════════════════════════════════════════════════════════
        // Column Comments
        // ═══════════════════════════════════════════════════════════════════════
        Execute.Sql($@"
            COMMENT ON COLUMN ""{ChunksTable}"".""{TsvectorColumn}"" IS 
            'Generated tsvector for full-text search (BM25). Auto-updated from Content column using english text search configuration.';
        ");
    }

    /// <summary>
    /// Reverts the migration: removes the GIN index and tsvector column.
    /// </summary>
    /// <remarks>
    /// LOGIC: Drop in reverse dependency order:
    /// 1. Drop GIN index first (depends on column)
    /// 2. Drop tsvector column
    /// </remarks>
    public override void Down()
    {
        // ═══════════════════════════════════════════════════════════════════════
        // Drop GIN Index
        // ═══════════════════════════════════════════════════════════════════════
        Execute.Sql($@"DROP INDEX IF EXISTS ""{GinIndexName}"";");

        // ═══════════════════════════════════════════════════════════════════════
        // Drop TSVECTOR Column
        // ═══════════════════════════════════════════════════════════════════════
        Execute.Sql($@"ALTER TABLE ""{ChunksTable}"" DROP COLUMN IF EXISTS ""{TsvectorColumn}"";");
    }
}
