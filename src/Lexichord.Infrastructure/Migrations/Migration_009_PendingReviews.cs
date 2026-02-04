// =============================================================================
// File: Migration_009_PendingReviews.cs
// Project: Lexichord.Infrastructure
// Description: Migration for the pending reviews table (manual deduplication queue).
// =============================================================================
// VERSION: v0.5.9d (Deduplication Service)
// LOGIC: Creates the pending_reviews table for storing chunks that require
//   manual deduplication decisions due to ambiguous classification.
//
// Dependencies:
//   - Migration_003_VectorSchema (Chunks table)
//   - Migration_008_CanonicalRecords (CanonicalRecords table)
// =============================================================================

using FluentMigrator;

namespace Lexichord.Infrastructure.Migrations;

/// <summary>
/// Migration for the pending reviews schema (manual deduplication queue).
/// </summary>
/// <remarks>
/// <para>
/// LOGIC: This migration creates the pending_reviews table for the manual
/// deduplication review queue. Chunks are queued when:
/// </para>
/// <list type="bullet">
///   <item><description>Classification confidence is below threshold (0.7).</description></item>
///   <item><description><see cref="Abstractions.Contracts.RAG.DeduplicationOptions.EnableManualReviewQueue"/> is true.</description></item>
/// </list>
/// <para>
/// Table structure:
/// </para>
/// <list type="bullet">
///   <item><description>id: Primary key (UUID).</description></item>
///   <item><description>new_chunk_id: The chunk awaiting review.</description></item>
///   <item><description>project_id: Optional project scope filter.</description></item>
///   <item><description>candidates: JSONB array of DuplicateCandidate data.</description></item>
///   <item><description>auto_classification_reason: Why auto-classification failed.</description></item>
///   <item><description>queued_at: When the review was queued.</description></item>
///   <item><description>reviewed_at: When the review was processed (null = pending).</description></item>
///   <item><description>reviewed_by: User/process that resolved the review.</description></item>
///   <item><description>decision: The decision type (ManualDecisionType enum value).</description></item>
///   <item><description>decision_notes: Optional notes explaining the decision.</description></item>
/// </list>
/// <para>
/// Indexes:
/// </para>
/// <list type="bullet">
///   <item><description>Partial index on project_id for pending reviews.</description></item>
///   <item><description>Partial index on queued_at for oldest-first ordering.</description></item>
/// </list>
/// <para>
/// v0.5.9d: Deduplication Service
/// Dependencies: v0.5.9c (canonical records), Migration_003_VectorSchema
/// </para>
/// </remarks>
[Migration(9, "Pending reviews for manual deduplication queue")]
[Tags("RAG", "Deduplication")]
public class Migration_009_PendingReviews : LexichordMigration
{
    private const string PendingReviewsTable = "PendingReviews";
    private const string ChunksTable = "Chunks";

    public override void Up()
    {
        // ═══════════════════════════════════════════════════════════════════════
        // PendingReviews Table
        // ═══════════════════════════════════════════════════════════════════════
        Create.Table(PendingReviewsTable)
            .WithColumn("Id")
                .AsGuid()
                .NotNullable()
                .PrimaryKey()
                .WithDefaultValue(RawSql.Insert("gen_random_uuid()"))
            .WithColumn("NewChunkId")
                .AsGuid()
                .NotNullable()
                .ForeignKey(ForeignKeyName(PendingReviewsTable, ChunksTable), ChunksTable, "Id")
                .OnDelete(System.Data.Rule.Cascade)
            .WithColumn("ProjectId")
                .AsGuid()
                .Nullable()
            .WithColumn("Candidates")
                .AsCustom("JSONB")
                .NotNullable()
            .WithColumn("AutoClassificationReason")
                .AsString(500)
                .Nullable()
            .WithColumn("QueuedAt")
                .AsCustom("TIMESTAMPTZ")
                .NotNullable()
                .WithDefaultValue(RawSql.Insert("NOW()"))
            .WithColumn("ReviewedAt")
                .AsCustom("TIMESTAMPTZ")
                .Nullable()
            .WithColumn("ReviewedBy")
                .AsString(255)
                .Nullable()
            .WithColumn("Decision")
                .AsString(50)
                .Nullable()
            .WithColumn("DecisionNotes")
                .AsString(1000)
                .Nullable();

        // ═══════════════════════════════════════════════════════════════════════
        // Indexes
        // ═══════════════════════════════════════════════════════════════════════

        // LOGIC: Partial index for filtering pending reviews by project
        // Only indexes rows where ReviewedAt IS NULL (pending reviews)
        Execute.Sql($@"
            CREATE INDEX {IndexName(PendingReviewsTable, "ProjectId")}
            ON ""{PendingReviewsTable}"" (""ProjectId"")
            WHERE ""ReviewedAt"" IS NULL;
        ");

        // LOGIC: Partial index for ordering pending reviews by queue time (oldest first)
        Execute.Sql($@"
            CREATE INDEX {IndexName(PendingReviewsTable, "QueuedAt")}
            ON ""{PendingReviewsTable}"" (""QueuedAt"" ASC)
            WHERE ""ReviewedAt"" IS NULL;
        ");

        // ═══════════════════════════════════════════════════════════════════════
        // Column Comments
        // ═══════════════════════════════════════════════════════════════════════
        Execute.Sql($@"
            COMMENT ON TABLE ""{PendingReviewsTable}"" IS 'Chunks awaiting manual deduplication decision';
            COMMENT ON COLUMN ""{PendingReviewsTable}"".""Id"" IS 'Unique identifier (UUID)';
            COMMENT ON COLUMN ""{PendingReviewsTable}"".""NewChunkId"" IS 'The chunk awaiting review';
            COMMENT ON COLUMN ""{PendingReviewsTable}"".""ProjectId"" IS 'Optional project scope filter';
            COMMENT ON COLUMN ""{PendingReviewsTable}"".""Candidates"" IS 'JSONB array of DuplicateCandidate data';
            COMMENT ON COLUMN ""{PendingReviewsTable}"".""AutoClassificationReason"" IS 'Why automatic classification failed';
            COMMENT ON COLUMN ""{PendingReviewsTable}"".""QueuedAt"" IS 'When this review was queued';
            COMMENT ON COLUMN ""{PendingReviewsTable}"".""ReviewedAt"" IS 'When the review was processed (null = pending)';
            COMMENT ON COLUMN ""{PendingReviewsTable}"".""ReviewedBy"" IS 'User/process that resolved the review';
            COMMENT ON COLUMN ""{PendingReviewsTable}"".""Decision"" IS 'The decision type (ManualDecisionType)';
            COMMENT ON COLUMN ""{PendingReviewsTable}"".""DecisionNotes"" IS 'Optional notes explaining the decision';
        ");
    }

    public override void Down()
    {
        // LOGIC: Drop indexes first, then table
        Execute.Sql($@"DROP INDEX IF EXISTS {IndexName(PendingReviewsTable, "QueuedAt")};");
        Execute.Sql($@"DROP INDEX IF EXISTS {IndexName(PendingReviewsTable, "ProjectId")};");
        Delete.Table(PendingReviewsTable);
    }
}
