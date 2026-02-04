// =============================================================================
// File: Migration_012_BatchDeduplicationJobs.cs
// Project: Lexichord.Infrastructure
// Description: Migration for batch deduplication job tracking tables.
// =============================================================================
// VERSION: v0.5.9g (Batch Retroactive Deduplication)
// LOGIC: Creates tables for tracking batch deduplication jobs, including
//   job state, progress counters, checkpoints, and processed chunk tracking.
//
// Dependencies:
//   - Migration_003_VectorSchema (Chunks table)
//   - Migration_008_CanonicalRecords (Deduplication context)
// =============================================================================

using FluentMigrator;

namespace Lexichord.Infrastructure.Migrations;

/// <summary>
/// Migration for batch deduplication job tracking schema.
/// </summary>
/// <remarks>
/// <para>
/// LOGIC: This migration creates tables for tracking batch deduplication jobs,
/// enabling progress monitoring, resumability, and job history.
/// </para>
/// <para>
/// Table: BatchDedupJobs
/// <list type="bullet">
///   <item><description>Stores job configuration and current state</description></item>
///   <item><description>Tracks progress counters (processed, merged, linked, etc.)</description></item>
///   <item><description>Maintains checkpoint for resumability</description></item>
///   <item><description>Records timing information for duration calculation</description></item>
/// </list>
/// </para>
/// <para>
/// Table: BatchDedupProcessed
/// <list type="bullet">
///   <item><description>Tracks which chunks have been processed per job</description></item>
///   <item><description>Enables resume without re-processing</description></item>
///   <item><description>Stores per-chunk processing result</description></item>
/// </list>
/// </para>
/// <para>
/// Indexes:
/// <list type="bullet">
///   <item><description>Index on State for filtering active jobs</description></item>
///   <item><description>Index on ProjectId for project-scoped queries</description></item>
///   <item><description>Index on CreatedAt for ordering job history</description></item>
///   <item><description>Unique index on (JobId, ChunkId) for processed tracking</description></item>
/// </list>
/// </para>
/// <para>
/// v0.5.9g: Batch Retroactive Deduplication
/// Dependencies: v0.5.9d (IDeduplicationService), Migration_003_VectorSchema
/// </para>
/// </remarks>
[Migration(12, "Batch deduplication job tracking schema")]
[Tags("RAG", "Deduplication", "Batch")]
public class Migration_012_BatchDeduplicationJobs : LexichordMigration
{
    private const string JobsTable = "BatchDedupJobs";
    private const string ProcessedTable = "BatchDedupProcessed";
    private const string ChunksTable = "Chunks";

    public override void Up()
    {
        // ═══════════════════════════════════════════════════════════════════════
        // BatchDedupJobs Table
        // ═══════════════════════════════════════════════════════════════════════
        Create.Table(JobsTable)
            // Primary Key
            .WithColumn("Id")
                .AsGuid()
                .NotNullable()
                .PrimaryKey()
                .WithDefaultValue(RawSql.Insert("gen_random_uuid()"))

            // Job Configuration (stored as JSON)
            .WithColumn("OptionsJson")
                .AsCustom("JSONB")
                .NotNullable()

            // State
            .WithColumn("State")
                .AsInt32()
                .NotNullable()
                .WithDefaultValue(0) // Pending

            // Project Scope
            .WithColumn("ProjectId")
                .AsGuid()
                .Nullable()

            // User-defined label  
            .WithColumn("Label")
                .AsString(256)
                .Nullable()

            // Dry-run flag (denormalized for easy filtering)
            .WithColumn("IsDryRun")
                .AsBoolean()
                .NotNullable()
                .WithDefaultValue(false)

            // Progress Counters
            .WithColumn("TotalChunks")
                .AsInt32()
                .Nullable()
            .WithColumn("ChunksProcessed")
                .AsInt32()
                .NotNullable()
                .WithDefaultValue(0)
            .WithColumn("DuplicatesFound")
                .AsInt32()
                .NotNullable()
                .WithDefaultValue(0)
            .WithColumn("MergedCount")
                .AsInt32()
                .NotNullable()
                .WithDefaultValue(0)
            .WithColumn("LinkedCount")
                .AsInt32()
                .NotNullable()
                .WithDefaultValue(0)
            .WithColumn("ContradictionsFound")
                .AsInt32()
                .NotNullable()
                .WithDefaultValue(0)
            .WithColumn("QueuedForReview")
                .AsInt32()
                .NotNullable()
                .WithDefaultValue(0)
            .WithColumn("ErrorCount")
                .AsInt32()
                .NotNullable()
                .WithDefaultValue(0)
            .WithColumn("StorageSavedBytes")
                .AsInt64()
                .NotNullable()
                .WithDefaultValue(0)

            // Checkpoint for resume
            .WithColumn("LastCheckpointChunkId")
                .AsGuid()
                .Nullable()
            .WithColumn("LastCheckpointAt")
                .AsCustom("TIMESTAMPTZ")
                .Nullable()

            // Timestamps
            .WithColumn("CreatedAt")
                .AsCustom("TIMESTAMPTZ")
                .NotNullable()
                .WithDefaultValue(RawSql.Insert("NOW()"))
            .WithColumn("StartedAt")
                .AsCustom("TIMESTAMPTZ")
                .Nullable()
            .WithColumn("CompletedAt")
                .AsCustom("TIMESTAMPTZ")
                .Nullable()

            // Error tracking
            .WithColumn("ErrorMessage")
                .AsString(4000)
                .Nullable();

        // ═══════════════════════════════════════════════════════════════════════
        // BatchDedupProcessed Table
        // ═══════════════════════════════════════════════════════════════════════
        Create.Table(ProcessedTable)
            // Primary Key
            .WithColumn("Id")
                .AsInt64()
                .NotNullable()
                .PrimaryKey()
                .Identity()

            // Foreign key to job
            .WithColumn("JobId")
                .AsGuid()
                .NotNullable()
                .ForeignKey($"FK_{ProcessedTable}_{JobsTable}", JobsTable, "Id")
                .OnDelete(System.Data.Rule.Cascade)

            // Processed chunk
            .WithColumn("ChunkId")
                .AsGuid()
                .NotNullable()

            // Processing result
            .WithColumn("Action")
                .AsInt32()
                .NotNullable() // DeduplicationAction enum

            // When processed
            .WithColumn("ProcessedAt")
                .AsCustom("TIMESTAMPTZ")
                .NotNullable()
                .WithDefaultValue(RawSql.Insert("NOW()"));

        // ═══════════════════════════════════════════════════════════════════════
        // Indexes - BatchDedupJobs
        // ═══════════════════════════════════════════════════════════════════════

        // Index on State for filtering active jobs
        Create.Index(IndexName(JobsTable, "State"))
            .OnTable(JobsTable)
            .OnColumn("State")
            .Ascending();

        // Index on ProjectId for project-scoped queries
        Execute.Sql($@"
            CREATE INDEX ""{IndexName(JobsTable, "ProjectId")}""
            ON ""{JobsTable}"" (""ProjectId"")
            WHERE ""ProjectId"" IS NOT NULL;
        ");

        // Index on CreatedAt for ordering job history
        Create.Index(IndexName(JobsTable, "CreatedAt"))
            .OnTable(JobsTable)
            .OnColumn("CreatedAt")
            .Descending();

        // Partial index for active (non-terminal) jobs
        Execute.Sql($@"
            CREATE INDEX ""{IndexName(JobsTable, "Active")}""
            ON ""{JobsTable}"" (""State"", ""ProjectId"")
            WHERE ""State"" IN (0, 1, 2);
        ");

        // ═══════════════════════════════════════════════════════════════════════
        // Indexes - BatchDedupProcessed
        // ═══════════════════════════════════════════════════════════════════════

        // Unique index on (JobId, ChunkId) to prevent duplicate processing
        Create.Index(IndexName(ProcessedTable, "JobChunk"))
            .OnTable(ProcessedTable)
            .OnColumn("JobId").Ascending()
            .OnColumn("ChunkId").Ascending()
            .WithOptions().Unique();

        // Index on JobId for job-specific queries
        Create.Index(IndexName(ProcessedTable, "JobId"))
            .OnTable(ProcessedTable)
            .OnColumn("JobId")
            .Ascending();

        // ═══════════════════════════════════════════════════════════════════════
        // Column Comments
        // ═══════════════════════════════════════════════════════════════════════
        Execute.Sql($@"
            COMMENT ON TABLE ""{JobsTable}"" IS 'Batch deduplication job tracking for retroactive processing';
            COMMENT ON COLUMN ""{JobsTable}"".""Id"" IS 'Unique job identifier (UUID)';
            COMMENT ON COLUMN ""{JobsTable}"".""OptionsJson"" IS 'Job configuration as JSON (BatchDeduplicationOptions)';
            COMMENT ON COLUMN ""{JobsTable}"".""State"" IS 'Job state: 0=Pending, 1=Running, 2=Paused, 3=Completed, 4=Cancelled, 5=Failed';
            COMMENT ON COLUMN ""{JobsTable}"".""ProjectId"" IS 'Optional project scope for the batch job';
            COMMENT ON COLUMN ""{JobsTable}"".""Label"" IS 'User-defined label for job identification';
            COMMENT ON COLUMN ""{JobsTable}"".""IsDryRun"" IS 'Whether this is a dry-run (preview only) job';
            COMMENT ON COLUMN ""{JobsTable}"".""TotalChunks"" IS 'Total chunks to process (counted at start)';
            COMMENT ON COLUMN ""{JobsTable}"".""ChunksProcessed"" IS 'Number of chunks processed so far';
            COMMENT ON COLUMN ""{JobsTable}"".""DuplicatesFound"" IS 'Number of duplicate pairs identified';
            COMMENT ON COLUMN ""{JobsTable}"".""MergedCount"" IS 'Number of chunks merged into canonical records';
            COMMENT ON COLUMN ""{JobsTable}"".""LinkedCount"" IS 'Number of chunks linked as complementary';
            COMMENT ON COLUMN ""{JobsTable}"".""ContradictionsFound"" IS 'Number of contradictory pairs flagged';
            COMMENT ON COLUMN ""{JobsTable}"".""QueuedForReview"" IS 'Number of chunks queued for manual review';
            COMMENT ON COLUMN ""{JobsTable}"".""ErrorCount"" IS 'Number of processing errors encountered';
            COMMENT ON COLUMN ""{JobsTable}"".""StorageSavedBytes"" IS 'Estimated storage saved by deduplication';
            COMMENT ON COLUMN ""{JobsTable}"".""LastCheckpointChunkId"" IS 'Last processed chunk ID for resume capability';
            COMMENT ON COLUMN ""{JobsTable}"".""LastCheckpointAt"" IS 'When the last checkpoint was saved';
            COMMENT ON COLUMN ""{JobsTable}"".""CreatedAt"" IS 'When the job was created';
            COMMENT ON COLUMN ""{JobsTable}"".""StartedAt"" IS 'When the job started executing';
            COMMENT ON COLUMN ""{JobsTable}"".""CompletedAt"" IS 'When the job completed (success, cancel, or fail)';
            COMMENT ON COLUMN ""{JobsTable}"".""ErrorMessage"" IS 'Error message if job failed';
        ");

        Execute.Sql($@"
            COMMENT ON TABLE ""{ProcessedTable}"" IS 'Tracks processed chunks per batch job for resume capability';
            COMMENT ON COLUMN ""{ProcessedTable}"".""Id"" IS 'Auto-incrementing primary key';
            COMMENT ON COLUMN ""{ProcessedTable}"".""JobId"" IS 'Reference to the batch job';
            COMMENT ON COLUMN ""{ProcessedTable}"".""ChunkId"" IS 'Processed chunk ID';
            COMMENT ON COLUMN ""{ProcessedTable}"".""Action"" IS 'DeduplicationAction taken: 0=StoredAsNew, 1=Merged, 2=Linked, etc.';
            COMMENT ON COLUMN ""{ProcessedTable}"".""ProcessedAt"" IS 'When the chunk was processed';
        ");
    }

    public override void Down()
    {
        // LOGIC: Drop tables in reverse order (child first)
        Delete.Table(ProcessedTable);
        Delete.Table(JobsTable);
    }
}
