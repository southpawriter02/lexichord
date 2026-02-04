// =============================================================================
// File: Migration_008_CanonicalRecords.cs
// Project: Lexichord.Infrastructure
// Description: Migration for canonical records and deduplication tables.
// =============================================================================
// VERSION: v0.5.9c (Canonical Record Management)
// LOGIC: Creates three tables for semantic memory deduplication:
//   - CanonicalRecords: Tracks authoritative chunks representing unique facts
//   - ChunkVariants: Links duplicate chunks to their canonical records
//   - ChunkProvenance: Records origin and verification of chunks
//
// Dependencies:
//   - Migration_003_VectorSchema (Chunks and Documents tables)
// =============================================================================

using FluentMigrator;

namespace Lexichord.Infrastructure.Migrations;

/// <summary>
/// Migration for the canonical records schema (deduplication tables).
/// </summary>
/// <remarks>
/// <para>
/// LOGIC: This migration creates the foundation for Semantic Memory Deduplication
/// by setting up tables to track canonical records, chunk variants, and provenance.
/// </para>
/// <para>
/// Tables:
/// <list type="bullet">
///   <item><description>CanonicalRecords: Authoritative chunk for each unique fact</description></item>
///   <item><description>ChunkVariants: Tracks merged duplicates with relationship type</description></item>
///   <item><description>ChunkProvenance: Origin and verification tracking</description></item>
/// </list>
/// </para>
/// <para>
/// Indexes:
/// <list type="bullet">
///   <item><description>Unique index on CanonicalRecords.CanonicalChunkId</description></item>
///   <item><description>Unique index on ChunkVariants.VariantChunkId</description></item>
///   <item><description>Index on ChunkVariants.CanonicalRecordId</description></item>
///   <item><description>Index on ChunkProvenance.ChunkId</description></item>
/// </list>
/// </para>
/// <para>
/// v0.5.9c: Canonical Record Management
/// Dependencies: v0.5.9b (IRelationshipClassifier), Migration_003_VectorSchema
/// </para>
/// </remarks>
[Migration(8, "Canonical records and deduplication schema")]
[Tags("RAG", "Deduplication")]
public class Migration_008_CanonicalRecords : LexichordMigration
{
    private const string CanonicalRecordsTable = "CanonicalRecords";
    private const string ChunkVariantsTable = "ChunkVariants";
    private const string ChunkProvenanceTable = "ChunkProvenance";
    private const string ChunksTable = "Chunks";
    private const string DocumentsTable = "Documents";

    public override void Up()
    {
        // ═══════════════════════════════════════════════════════════════════════
        // CanonicalRecords Table
        // ═══════════════════════════════════════════════════════════════════════
        Create.Table(CanonicalRecordsTable)
            .WithColumn("Id")
                .AsGuid()
                .NotNullable()
                .PrimaryKey()
                .WithDefaultValue(RawSql.Insert("gen_random_uuid()"))
            .WithColumn("CanonicalChunkId")
                .AsGuid()
                .NotNullable()
                .ForeignKey(ForeignKeyName(CanonicalRecordsTable, ChunksTable), ChunksTable, "Id")
                .OnDelete(System.Data.Rule.Cascade)
            .WithColumn("CreatedAt")
                .AsCustom("TIMESTAMPTZ")
                .NotNullable()
                .WithDefaultValue(RawSql.Insert("NOW()"))
            .WithColumn("UpdatedAt")
                .AsCustom("TIMESTAMPTZ")
                .NotNullable()
                .WithDefaultValue(RawSql.Insert("NOW()"))
            .WithColumn("MergeCount")
                .AsInt32()
                .NotNullable()
                .WithDefaultValue(0);

        // Unique constraint: one canonical record per chunk
        Create.Index(IndexName(CanonicalRecordsTable, "CanonicalChunkId"))
            .OnTable(CanonicalRecordsTable)
            .OnColumn("CanonicalChunkId")
            .Ascending()
            .WithOptions().Unique();

        // UpdatedAt trigger for CanonicalRecords
        CreateUpdateTrigger(CanonicalRecordsTable);

        // ═══════════════════════════════════════════════════════════════════════
        // ChunkVariants Table
        // ═══════════════════════════════════════════════════════════════════════
        Create.Table(ChunkVariantsTable)
            .WithColumn("Id")
                .AsGuid()
                .NotNullable()
                .PrimaryKey()
                .WithDefaultValue(RawSql.Insert("gen_random_uuid()"))
            .WithColumn("CanonicalRecordId")
                .AsGuid()
                .NotNullable()
                .ForeignKey(ForeignKeyName(ChunkVariantsTable, CanonicalRecordsTable), CanonicalRecordsTable, "Id")
                .OnDelete(System.Data.Rule.Cascade)
            .WithColumn("VariantChunkId")
                .AsGuid()
                .NotNullable()
                .ForeignKey(ForeignKeyName(ChunkVariantsTable, ChunksTable), ChunksTable, "Id")
                .OnDelete(System.Data.Rule.Cascade)
            .WithColumn("RelationshipType")
                .AsInt32()
                .NotNullable()
            .WithColumn("SimilarityScore")
                .AsFloat()
                .NotNullable()
            .WithColumn("MergedAt")
                .AsCustom("TIMESTAMPTZ")
                .NotNullable()
                .WithDefaultValue(RawSql.Insert("NOW()"));

        // Unique constraint: a chunk can only be a variant of one canonical
        Create.Index(IndexName(ChunkVariantsTable, "VariantChunkId"))
            .OnTable(ChunkVariantsTable)
            .OnColumn("VariantChunkId")
            .Ascending()
            .WithOptions().Unique();

        // Index for looking up variants by canonical
        Create.Index(IndexName(ChunkVariantsTable, "CanonicalRecordId"))
            .OnTable(ChunkVariantsTable)
            .OnColumn("CanonicalRecordId")
            .Ascending();

        // ═══════════════════════════════════════════════════════════════════════
        // ChunkProvenance Table
        // ═══════════════════════════════════════════════════════════════════════
        Create.Table(ChunkProvenanceTable)
            .WithColumn("Id")
                .AsGuid()
                .NotNullable()
                .PrimaryKey()
                .WithDefaultValue(RawSql.Insert("gen_random_uuid()"))
            .WithColumn("ChunkId")
                .AsGuid()
                .NotNullable()
                .ForeignKey(ForeignKeyName(ChunkProvenanceTable, ChunksTable), ChunksTable, "Id")
                .OnDelete(System.Data.Rule.Cascade)
            .WithColumn("SourceDocumentId")
                .AsGuid()
                .Nullable()
                .ForeignKey(ForeignKeyName(ChunkProvenanceTable, DocumentsTable), DocumentsTable, "Id")
                .OnDelete(System.Data.Rule.SetNull)
            .WithColumn("SourceLocation")
                .AsString(500)
                .Nullable()
            .WithColumn("IngestedAt")
                .AsCustom("TIMESTAMPTZ")
                .NotNullable()
                .WithDefaultValue(RawSql.Insert("NOW()"))
            .WithColumn("VerifiedAt")
                .AsCustom("TIMESTAMPTZ")
                .Nullable()
            .WithColumn("VerifiedBy")
                .AsString(255)
                .Nullable();

        // Index for looking up provenance by chunk (also unique for UPSERT support)
        Create.Index(IndexName(ChunkProvenanceTable, "ChunkId"))
            .OnTable(ChunkProvenanceTable)
            .OnColumn("ChunkId")
            .Ascending()
            .WithOptions().Unique();

        // ═══════════════════════════════════════════════════════════════════════
        // Column Comments
        // ═══════════════════════════════════════════════════════════════════════
        Execute.Sql($@"
            COMMENT ON TABLE ""{CanonicalRecordsTable}"" IS 'Authoritative chunks representing unique facts for deduplication';
            COMMENT ON COLUMN ""{CanonicalRecordsTable}"".""Id"" IS 'Unique identifier (UUID)';
            COMMENT ON COLUMN ""{CanonicalRecordsTable}"".""CanonicalChunkId"" IS 'The authoritative chunk for this fact';
            COMMENT ON COLUMN ""{CanonicalRecordsTable}"".""CreatedAt"" IS 'When this canonical record was created';
            COMMENT ON COLUMN ""{CanonicalRecordsTable}"".""UpdatedAt"" IS 'When this canonical record was last modified';
            COMMENT ON COLUMN ""{CanonicalRecordsTable}"".""MergeCount"" IS 'Number of variants merged into this canonical';

            COMMENT ON TABLE ""{ChunkVariantsTable}"" IS 'Chunks merged as duplicates of canonical records';
            COMMENT ON COLUMN ""{ChunkVariantsTable}"".""Id"" IS 'Unique identifier (UUID)';
            COMMENT ON COLUMN ""{ChunkVariantsTable}"".""CanonicalRecordId"" IS 'Parent canonical record';
            COMMENT ON COLUMN ""{ChunkVariantsTable}"".""VariantChunkId"" IS 'The merged duplicate chunk';
            COMMENT ON COLUMN ""{ChunkVariantsTable}"".""RelationshipType"" IS 'Classified relationship (enum value)';
            COMMENT ON COLUMN ""{ChunkVariantsTable}"".""SimilarityScore"" IS 'Cosine similarity (0.0-1.0)';
            COMMENT ON COLUMN ""{ChunkVariantsTable}"".""MergedAt"" IS 'When this variant was merged';

            COMMENT ON TABLE ""{ChunkProvenanceTable}"" IS 'Origin and verification tracking for chunks';
            COMMENT ON COLUMN ""{ChunkProvenanceTable}"".""Id"" IS 'Unique identifier (UUID)';
            COMMENT ON COLUMN ""{ChunkProvenanceTable}"".""ChunkId"" IS 'The chunk this provenance describes';
            COMMENT ON COLUMN ""{ChunkProvenanceTable}"".""SourceDocumentId"" IS 'Original source document (if known)';
            COMMENT ON COLUMN ""{ChunkProvenanceTable}"".""SourceLocation"" IS 'Location within the source (section, page, etc.)';
            COMMENT ON COLUMN ""{ChunkProvenanceTable}"".""IngestedAt"" IS 'When this chunk was ingested';
            COMMENT ON COLUMN ""{ChunkProvenanceTable}"".""VerifiedAt"" IS 'When the content was verified as accurate';
            COMMENT ON COLUMN ""{ChunkProvenanceTable}"".""VerifiedBy"" IS 'User/process that verified the content';
        ");
    }

    public override void Down()
    {
        // LOGIC: Drop in reverse dependency order

        // Drop ChunkProvenance first (no dependencies on it)
        Delete.Table(ChunkProvenanceTable);

        // Drop ChunkVariants (depends on CanonicalRecords)
        Delete.Table(ChunkVariantsTable);

        // Drop CanonicalRecords trigger and table
        DropUpdateTrigger(CanonicalRecordsTable);
        Delete.Table(CanonicalRecordsTable);
    }
}
