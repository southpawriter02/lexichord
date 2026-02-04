// =============================================================================
// File: Migration_011_RetrievalIntegrationIndexes.cs
// Project: Lexichord.Infrastructure
// Description: Index optimization for deduplication-aware search queries.
// =============================================================================
// VERSION: v0.5.9f (Retrieval Integration)
// LOGIC: Creates covering indexes to optimize the canonical-aware search query
//   introduced in SearchSimilarWithDeduplicationAsync. Key optimizations:
//
//   1. CanonicalChunkId lookup: Covering index on CanonicalRecords
//   2. VariantChunkId lookup: Covering index on ChunkVariants
//   3. Contradiction EXISTS check: Index on Contradictions for chunk lookup
//
// Dependencies:
//   - Migration_008_CanonicalRecords (CanonicalRecords, ChunkVariants tables)
//   - Migration_010_Contradictions (Contradictions table)
// =============================================================================

using FluentMigrator;

namespace Lexichord.Infrastructure.Migrations;

/// <summary>
/// Index optimization for deduplication-aware search queries.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9f as part of the Retrieval Integration feature.
/// </para>
/// <para>
/// This migration creates covering indexes to optimize the canonical-aware search
/// query used by <c>IChunkRepository.SearchSimilarWithDeduplicationAsync</c>.
/// The query performs LEFT JOINs to CanonicalRecords and ChunkVariants tables,
/// and uses an EXISTS subquery on the Contradictions table.
/// </para>
/// <para>
/// Indexes created:
/// <list type="bullet">
///   <item><description>
///     IX_CanonicalRecords_CanonicalChunkId_Covering: Covering index on CanonicalChunkId
///     including Id and MergeCount for efficient canonical lookup.
///   </description></item>
///   <item><description>
///     IX_ChunkVariants_VariantChunkId_Covering: Covering index on VariantChunkId
///     including CanonicalRecordId for efficient variant exclusion.
///   </description></item>
///   <item><description>
///     IX_Contradictions_ChunkLookup: Composite index on ChunkAId/ChunkBId with
///     Status for efficient EXISTS check.
///   </description></item>
/// </list>
/// </para>
/// </remarks>
[Migration(11, "Index optimization for retrieval integration")]
[Tags("RAG", "Deduplication")]
public class Migration_011_RetrievalIntegrationIndexes : LexichordMigration
{
    private const string CanonicalRecordsTable = "CanonicalRecords";
    private const string ChunkVariantsTable = "ChunkVariants";
    private const string ContradictionsTable = "Contradictions";

    public override void Up()
    {
        // ═══════════════════════════════════════════════════════════════════════
        // CanonicalRecords Covering Index
        // ═══════════════════════════════════════════════════════════════════════
        // LOGIC: v0.5.9f - Covering index for canonical chunk lookup.
        // The search query LEFT JOINs on CanonicalChunkId and selects Id, MergeCount.
        // Including these columns avoids a table lookup after index scan.
        Execute.Sql($@"
            CREATE INDEX IF NOT EXISTS ""IX_{CanonicalRecordsTable}_CanonicalChunkId_Covering""
            ON ""{CanonicalRecordsTable}"" (""CanonicalChunkId"")
            INCLUDE (""Id"", ""MergeCount"");
        ");

        // ═══════════════════════════════════════════════════════════════════════
        // ChunkVariants Covering Index
        // ═══════════════════════════════════════════════════════════════════════
        // LOGIC: v0.5.9f - Covering index for variant chunk exclusion.
        // When RespectCanonicals=true, we filter out chunks where they appear
        // in ChunkVariants.VariantChunkId (they are variants, not canonicals).
        // Including CanonicalRecordId allows for future provenance lookups.
        Execute.Sql($@"
            CREATE INDEX IF NOT EXISTS ""IX_{ChunkVariantsTable}_VariantChunkId_Covering""
            ON ""{ChunkVariantsTable}"" (""VariantChunkId"")
            INCLUDE (""Id"", ""CanonicalRecordId"");
        ");

        // ═══════════════════════════════════════════════════════════════════════
        // Contradictions Chunk Lookup Index
        // ═══════════════════════════════════════════════════════════════════════
        // LOGIC: v0.5.9f - Index for EXISTS subquery checking contradiction status.
        // The search query checks if a chunk has active contradictions using:
        //   EXISTS (SELECT 1 FROM Contradictions WHERE (ChunkAId = ? OR ChunkBId = ?) AND Status = 'Flagged')
        // Create separate indexes on each chunk column filtered by Status.
        Execute.Sql($@"
            CREATE INDEX IF NOT EXISTS ""IX_{ContradictionsTable}_ChunkA_Flagged""
            ON ""{ContradictionsTable}"" (""ChunkAId"")
            WHERE ""Status"" = 0;
        ");

        Execute.Sql($@"
            CREATE INDEX IF NOT EXISTS ""IX_{ContradictionsTable}_ChunkB_Flagged""
            ON ""{ContradictionsTable}"" (""ChunkBId"")
            WHERE ""Status"" = 0;
        ");

        // ═══════════════════════════════════════════════════════════════════════
        // Index Comments
        // ═══════════════════════════════════════════════════════════════════════
        Execute.Sql($@"
            COMMENT ON INDEX ""IX_{CanonicalRecordsTable}_CanonicalChunkId_Covering"" 
                IS 'v0.5.9f: Covering index for canonical lookup in dedup search';
            COMMENT ON INDEX ""IX_{ChunkVariantsTable}_VariantChunkId_Covering"" 
                IS 'v0.5.9f: Covering index for variant exclusion in dedup search';
            COMMENT ON INDEX ""IX_{ContradictionsTable}_ChunkA_Flagged"" 
                IS 'v0.5.9f: Partial index for contradiction EXISTS check (ChunkA)';
            COMMENT ON INDEX ""IX_{ContradictionsTable}_ChunkB_Flagged"" 
                IS 'v0.5.9f: Partial index for contradiction EXISTS check (ChunkB)';
        ");
    }

    public override void Down()
    {
        // LOGIC: Drop indexes in reverse order (no dependencies between indexes).
        Execute.Sql($@"DROP INDEX IF EXISTS ""IX_{ContradictionsTable}_ChunkB_Flagged"";");
        Execute.Sql($@"DROP INDEX IF EXISTS ""IX_{ContradictionsTable}_ChunkA_Flagged"";");
        Execute.Sql($@"DROP INDEX IF EXISTS ""IX_{ChunkVariantsTable}_VariantChunkId_Covering"";");
        Execute.Sql($@"DROP INDEX IF EXISTS ""IX_{CanonicalRecordsTable}_CanonicalChunkId_Covering"";");
    }
}
