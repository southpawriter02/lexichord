// =============================================================================
// File: Migration_010_Contradictions.cs
// Project: Lexichord.Infrastructure
// Description: Migration for the contradictions table.
// =============================================================================
// VERSION: v0.5.9e (Contradiction Detection & Resolution)
// LOGIC: Creates the Contradictions table to store detected contradictions
//   between chunks, including classification metadata, lifecycle status,
//   and resolution details. Supports the admin review workflow.
//
// Dependencies:
//   - Migration_003_VectorSchema (Chunks table)
//   - Migration_008_CanonicalRecords (CanonicalRecords table for context)
// =============================================================================

using FluentMigrator;

namespace Lexichord.Infrastructure.Migrations;

/// <summary>
/// Migration for the contradictions schema.
/// </summary>
/// <remarks>
/// <para>
/// LOGIC: This migration creates a comprehensive table for tracking detected
/// contradictions between chunks, supporting the full lifecycle from detection
/// through resolution.
/// </para>
/// <para>
/// Table: Contradictions
/// <list type="bullet">
///   <item><description>Stores pairs of conflicting chunk IDs</description></item>
///   <item><description>Tracks classification metadata (similarity, confidence, reason)</description></item>
///   <item><description>Maintains lifecycle status (Pending, UnderReview, Resolved, etc.)</description></item>
///   <item><description>Records resolution details for audit trail</description></item>
/// </list>
/// </para>
/// <para>
/// Indexes:
/// <list type="bullet">
///   <item><description>Unique index on (ChunkAId, ChunkBId) to prevent duplicates</description></item>
///   <item><description>Index on Status for filtering pending contradictions</description></item>
///   <item><description>Index on ProjectId for multi-tenant filtering</description></item>
///   <item><description>Partial index on Status = Pending for admin dashboard</description></item>
/// </list>
/// </para>
/// <para>
/// v0.5.9e: Contradiction Detection & Resolution
/// Dependencies: v0.5.9d (IDeduplicationService), Migration_003_VectorSchema
/// </para>
/// </remarks>
[Migration(10, "Contradictions schema for contradiction detection")]
[Tags("RAG", "Deduplication")]
public class Migration_010_Contradictions : LexichordMigration
{
    private const string ContradictionsTable = "Contradictions";
    private const string ChunksTable = "Chunks";

    public override void Up()
    {
        // ═══════════════════════════════════════════════════════════════════════
        // Contradictions Table
        // ═══════════════════════════════════════════════════════════════════════
        Create.Table(ContradictionsTable)
            // Primary Key
            .WithColumn("Id")
                .AsGuid()
                .NotNullable()
                .PrimaryKey()
                .WithDefaultValue(RawSql.Insert("gen_random_uuid()"))

            // Conflicting Chunks
            .WithColumn("ChunkAId")
                .AsGuid()
                .NotNullable()
                .ForeignKey(ForeignKeyName(ContradictionsTable, ChunksTable, "A"), ChunksTable, "Id")
                .OnDelete(System.Data.Rule.Cascade)
            .WithColumn("ChunkBId")
                .AsGuid()
                .NotNullable()
                .ForeignKey(ForeignKeyName(ContradictionsTable, ChunksTable, "B"), ChunksTable, "Id")
                .OnDelete(System.Data.Rule.Cascade)

            // Classification Metadata
            .WithColumn("SimilarityScore")
                .AsFloat()
                .NotNullable()
            .WithColumn("ClassificationConfidence")
                .AsFloat()
                .NotNullable()
            .WithColumn("ContradictionReason")
                .AsString(2000)
                .Nullable()

            // Lifecycle Status
            .WithColumn("Status")
                .AsInt32()
                .NotNullable()
                .WithDefaultValue(0) // Pending

            // Detection Metadata
            .WithColumn("DetectedAt")
                .AsCustom("TIMESTAMPTZ")
                .NotNullable()
                .WithDefaultValue(RawSql.Insert("NOW()"))
            .WithColumn("DetectedBy")
                .AsString(255)
                .NotNullable()
                .WithDefaultValue("DeduplicationService")

            // Project Scope
            .WithColumn("ProjectId")
                .AsGuid()
                .Nullable()

            // Review Tracking
            .WithColumn("ReviewedBy")
                .AsString(255)
                .Nullable()
            .WithColumn("ReviewedAt")
                .AsCustom("TIMESTAMPTZ")
                .Nullable()

            // Resolution Details
            .WithColumn("ResolutionType")
                .AsInt32()
                .Nullable()
            .WithColumn("ResolutionRationale")
                .AsString(2000)
                .Nullable()
            .WithColumn("RetainedChunkId")
                .AsGuid()
                .Nullable()
            .WithColumn("ArchivedChunkId")
                .AsGuid()
                .Nullable()
            .WithColumn("SynthesizedContent")
                .AsCustom("TEXT")
                .Nullable()
            .WithColumn("SynthesizedChunkId")
                .AsGuid()
                .Nullable();

        // ═══════════════════════════════════════════════════════════════════════
        // Indexes
        // ═══════════════════════════════════════════════════════════════════════

        // Unique index on chunk pair (symmetric - use LEAST/GREATEST for normalized order)
        // This prevents (A, B) and (B, A) from both existing.
        Execute.Sql($@"
            CREATE UNIQUE INDEX ""IX_{ContradictionsTable}_ChunkPair""
            ON ""{ContradictionsTable}"" (LEAST(""ChunkAId"", ""ChunkBId""), GREATEST(""ChunkAId"", ""ChunkBId""));
        ");

        // Index on Status for filtering by lifecycle state
        Create.Index(IndexName(ContradictionsTable, "Status"))
            .OnTable(ContradictionsTable)
            .OnColumn("Status")
            .Ascending();

        // Index on ProjectId for multi-tenant filtering (partial index)
        Execute.Sql($@"
            CREATE INDEX ""IX_{ContradictionsTable}_ProjectId""
            ON ""{ContradictionsTable}"" (""ProjectId"")
            WHERE ""ProjectId"" IS NOT NULL;
        ");

        // Partial index for pending contradictions (most common query)
        Execute.Sql($@"
            CREATE INDEX ""IX_{ContradictionsTable}_Pending""
            ON ""{ContradictionsTable}"" (""DetectedAt"" ASC)
            WHERE ""Status"" IN (0, 1);
        ");

        // Index for looking up contradictions by chunk
        Create.Index(IndexName(ContradictionsTable, "ChunkAId"))
            .OnTable(ContradictionsTable)
            .OnColumn("ChunkAId")
            .Ascending();

        Create.Index(IndexName(ContradictionsTable, "ChunkBId"))
            .OnTable(ContradictionsTable)
            .OnColumn("ChunkBId")
            .Ascending();

        // ═══════════════════════════════════════════════════════════════════════
        // Column Comments
        // ═══════════════════════════════════════════════════════════════════════
        Execute.Sql($@"
            COMMENT ON TABLE ""{ContradictionsTable}"" IS 'Detected contradictions between chunks for resolution workflow';
            COMMENT ON COLUMN ""{ContradictionsTable}"".""Id"" IS 'Unique identifier (UUID)';
            COMMENT ON COLUMN ""{ContradictionsTable}"".""ChunkAId"" IS 'First conflicting chunk';
            COMMENT ON COLUMN ""{ContradictionsTable}"".""ChunkBId"" IS 'Second conflicting chunk';
            COMMENT ON COLUMN ""{ContradictionsTable}"".""SimilarityScore"" IS 'Cosine similarity between chunks (0.0-1.0)';
            COMMENT ON COLUMN ""{ContradictionsTable}"".""ClassificationConfidence"" IS 'Confidence that this is a genuine contradiction (0.0-1.0)';
            COMMENT ON COLUMN ""{ContradictionsTable}"".""ContradictionReason"" IS 'LLM or rule-based explanation of the contradiction';
            COMMENT ON COLUMN ""{ContradictionsTable}"".""Status"" IS 'Lifecycle status: 0=Pending, 1=UnderReview, 2=Resolved, 3=Dismissed, 4=AutoResolved';
            COMMENT ON COLUMN ""{ContradictionsTable}"".""DetectedAt"" IS 'When the contradiction was first detected';
            COMMENT ON COLUMN ""{ContradictionsTable}"".""DetectedBy"" IS 'Source of detection (usually DeduplicationService)';
            COMMENT ON COLUMN ""{ContradictionsTable}"".""ProjectId"" IS 'Optional project scope for multi-tenant filtering';
            COMMENT ON COLUMN ""{ContradictionsTable}"".""ReviewedBy"" IS 'Admin who reviewed this contradiction';
            COMMENT ON COLUMN ""{ContradictionsTable}"".""ReviewedAt"" IS 'When the contradiction was reviewed/resolved';
            COMMENT ON COLUMN ""{ContradictionsTable}"".""ResolutionType"" IS 'Resolution action: 0=KeepOlder, 1=KeepNewer, 2=KeepBoth, 3=CreateSynthesis, 4=DeleteBoth';
            COMMENT ON COLUMN ""{ContradictionsTable}"".""ResolutionRationale"" IS 'Explanation for the resolution decision';
            COMMENT ON COLUMN ""{ContradictionsTable}"".""RetainedChunkId"" IS 'Chunk ID retained after KeepOlder/KeepNewer resolution';
            COMMENT ON COLUMN ""{ContradictionsTable}"".""ArchivedChunkId"" IS 'Chunk ID archived after KeepOlder/KeepNewer resolution';
            COMMENT ON COLUMN ""{ContradictionsTable}"".""SynthesizedContent"" IS 'New content created for CreateSynthesis resolution';
            COMMENT ON COLUMN ""{ContradictionsTable}"".""SynthesizedChunkId"" IS 'ID of newly created synthesis chunk';
        ");
    }

    public override void Down()
    {
        // LOGIC: Drop table (cascades foreign keys)
        Delete.Table(ContradictionsTable);
    }

    /// <summary>
    /// Creates a foreign key name with discriminator for multi-FK tables.
    /// </summary>
    private static string ForeignKeyName(string childTable, string parentTable, string discriminator)
    {
        return $"FK_{childTable}_{parentTable}_{discriminator}";
    }
}
