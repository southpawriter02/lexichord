using FluentMigrator;

namespace Lexichord.Infrastructure.Migrations;

/// <summary>
/// Migration for the vector storage schema (Documents and Chunks tables).
/// </summary>
/// <remarks>
/// LOGIC: This migration creates the foundation for RAG (Retrieval-Augmented Generation)
/// by setting up tables to store document metadata and text chunks with vector embeddings.
///
/// Tables:
/// - Documents: Stores metadata about indexed files (path, hash, status, chunk count)
/// - Chunks: Stores text fragments with pgvector embeddings for similarity search
///
/// Indexes:
/// - HNSW index on Chunks.Embedding for fast approximate nearest neighbor search
/// - B-tree indexes on commonly queried columns (FilePath, Status, DocumentId)
///
/// v0.4.1b: Vector Foundation - Schema Migration
/// Dependencies: v0.4.1a (pgvector Docker configuration)
/// </remarks>
[Migration(3, "Vector storage schema for RAG")]
[Tags("RAG", "Vector")]
public class Migration_003_VectorSchema : LexichordMigration
{
    private const string DocumentsTable = "Documents";
    private const string ChunksTable = "Chunks";

    public override void Up()
    {
        // ═══════════════════════════════════════════════════════════════════════
        // Ensure pgvector extension exists (idempotent)
        // ═══════════════════════════════════════════════════════════════════════
        Execute.Sql("CREATE EXTENSION IF NOT EXISTS vector;");

        // ═══════════════════════════════════════════════════════════════════════
        // Documents Table
        // ═══════════════════════════════════════════════════════════════════════
        Create.Table(DocumentsTable)
            .WithColumn("Id")
                .AsGuid()
                .NotNullable()
                .PrimaryKey()
                .WithDefaultValue(RawSql.Insert("gen_random_uuid()"))
            .WithColumn("FilePath")
                .AsString(1000)
                .NotNullable()
                .Unique()
            .WithColumn("FileHash")
                .AsString(64)
                .NotNullable()
            .WithColumn("Title")
                .AsString(500)
                .Nullable()
            .WithColumn("FileSize")
                .AsInt64()
                .NotNullable()
                .WithDefaultValue(0)
            .WithColumn("LastModified")
                .AsCustom("TIMESTAMPTZ")
                .Nullable()
            .WithColumn("IndexedAt")
                .AsCustom("TIMESTAMPTZ")
                .NotNullable()
                .WithDefaultValue(RawSql.Insert("NOW()"))
            .WithColumn("ChunkCount")
                .AsInt32()
                .NotNullable()
                .WithDefaultValue(0)
            .WithColumn("Status")
                .AsString(20)
                .NotNullable()
                .WithDefaultValue("Pending")
            .WithColumn("ErrorMessage")
                .AsCustom("TEXT")
                .Nullable()
            .WithColumn("Metadata")
                .AsCustom("JSONB")
                .Nullable()
            .WithColumn("CreatedAt")
                .AsCustom("TIMESTAMPTZ")
                .NotNullable()
                .WithDefaultValue(RawSql.Insert("NOW()"))
            .WithColumn("UpdatedAt")
                .AsCustom("TIMESTAMPTZ")
                .NotNullable()
                .WithDefaultValue(RawSql.Insert("NOW()"));

        // ═══════════════════════════════════════════════════════════════════════
        // Documents Indexes
        // ═══════════════════════════════════════════════════════════════════════
        Create.Index(IndexName(DocumentsTable, "Status"))
            .OnTable(DocumentsTable)
            .OnColumn("Status")
            .Ascending();

        // UpdatedAt trigger for Documents
        CreateUpdateTrigger(DocumentsTable);

        // ═══════════════════════════════════════════════════════════════════════
        // Chunks Table
        // ═══════════════════════════════════════════════════════════════════════
        Create.Table(ChunksTable)
            .WithColumn("Id")
                .AsGuid()
                .NotNullable()
                .PrimaryKey()
                .WithDefaultValue(RawSql.Insert("gen_random_uuid()"))
            .WithColumn("DocumentId")
                .AsGuid()
                .NotNullable()
                .ForeignKey(ForeignKeyName(ChunksTable, DocumentsTable), DocumentsTable, "Id")
                .OnDelete(System.Data.Rule.Cascade)
            .WithColumn("Content")
                .AsCustom("TEXT")
                .NotNullable()
            .WithColumn("ChunkIndex")
                .AsInt32()
                .NotNullable()
            .WithColumn("StartOffset")
                .AsInt32()
                .NotNullable()
                .WithDefaultValue(0)
            .WithColumn("EndOffset")
                .AsInt32()
                .NotNullable()
                .WithDefaultValue(0)
            .WithColumn("Embedding")
                .AsCustom("vector(1536)")
                .Nullable()
            .WithColumn("Heading")
                .AsString(500)
                .Nullable()
            .WithColumn("HeadingLevel")
                .AsInt32()
                .Nullable()
            .WithColumn("Metadata")
                .AsCustom("JSONB")
                .Nullable()
            .WithColumn("CreatedAt")
                .AsCustom("TIMESTAMPTZ")
                .NotNullable()
                .WithDefaultValue(RawSql.Insert("NOW()"));

        // ═══════════════════════════════════════════════════════════════════════
        // Chunks Indexes
        // ═══════════════════════════════════════════════════════════════════════
        Create.Index(IndexName(ChunksTable, "DocumentId"))
            .OnTable(ChunksTable)
            .OnColumn("DocumentId")
            .Ascending();

        // Unique constraint: one chunk index per document
        Create.Index(IndexName(ChunksTable, "DocumentId", "ChunkIndex"))
            .OnTable(ChunksTable)
            .OnColumn("DocumentId").Ascending()
            .OnColumn("ChunkIndex").Ascending()
            .WithOptions().Unique();

        // ═══════════════════════════════════════════════════════════════════════
        // HNSW Vector Index for Similarity Search
        // ═══════════════════════════════════════════════════════════════════════
        // LOGIC: HNSW (Hierarchical Navigable Small World) provides fast ANN search
        // Parameters: m=16 (connections per node), ef_construction=64 (build quality)
        // Operator: vector_cosine_ops for cosine similarity (normalized vectors)
        Execute.Sql(@"
            CREATE INDEX ""IX_Chunks_Embedding_hnsw"" ON ""Chunks""
            USING hnsw (""Embedding"" vector_cosine_ops)
            WITH (m = 16, ef_construction = 64);
        ");

        // ═══════════════════════════════════════════════════════════════════════
        // Column Comments
        // ═══════════════════════════════════════════════════════════════════════
        Execute.Sql($@"
            COMMENT ON TABLE ""{DocumentsTable}"" IS 'Indexed documents for RAG vector search';
            COMMENT ON COLUMN ""{DocumentsTable}"".""Id"" IS 'Unique identifier (UUID)';
            COMMENT ON COLUMN ""{DocumentsTable}"".""FilePath"" IS 'Absolute path to the source file';
            COMMENT ON COLUMN ""{DocumentsTable}"".""FileHash"" IS 'SHA-256 hash for change detection';
            COMMENT ON COLUMN ""{DocumentsTable}"".""Title"" IS 'Document title extracted from content';
            COMMENT ON COLUMN ""{DocumentsTable}"".""FileSize"" IS 'File size in bytes';
            COMMENT ON COLUMN ""{DocumentsTable}"".""LastModified"" IS 'File last modification timestamp';
            COMMENT ON COLUMN ""{DocumentsTable}"".""IndexedAt"" IS 'When the document was last indexed';
            COMMENT ON COLUMN ""{DocumentsTable}"".""ChunkCount"" IS 'Number of chunks extracted';
            COMMENT ON COLUMN ""{DocumentsTable}"".""Status"" IS 'Indexing status: Pending, Indexed, Failed';
            COMMENT ON COLUMN ""{DocumentsTable}"".""ErrorMessage"" IS 'Error details if indexing failed';
            COMMENT ON COLUMN ""{DocumentsTable}"".""Metadata"" IS 'Additional metadata as JSON';

            COMMENT ON TABLE ""{ChunksTable}"" IS 'Text chunks with vector embeddings for similarity search';
            COMMENT ON COLUMN ""{ChunksTable}"".""Id"" IS 'Unique identifier (UUID)';
            COMMENT ON COLUMN ""{ChunksTable}"".""DocumentId"" IS 'Parent document reference';
            COMMENT ON COLUMN ""{ChunksTable}"".""Content"" IS 'Text content of this chunk';
            COMMENT ON COLUMN ""{ChunksTable}"".""ChunkIndex"" IS 'Position within the document (0-based)';
            COMMENT ON COLUMN ""{ChunksTable}"".""StartOffset"" IS 'Character offset from document start';
            COMMENT ON COLUMN ""{ChunksTable}"".""EndOffset"" IS 'Character offset of chunk end';
            COMMENT ON COLUMN ""{ChunksTable}"".""Embedding"" IS 'Vector embedding (1536-dim for OpenAI ada-002)';
            COMMENT ON COLUMN ""{ChunksTable}"".""Heading"" IS 'Section heading this chunk belongs to';
            COMMENT ON COLUMN ""{ChunksTable}"".""HeadingLevel"" IS 'Heading level (1-6 for H1-H6)';
            COMMENT ON COLUMN ""{ChunksTable}"".""Metadata"" IS 'Additional metadata as JSON';
        ");
    }

    public override void Down()
    {
        // LOGIC: Drop in reverse dependency order

        // Drop HNSW index explicitly (for clarity, table drop would also remove it)
        Execute.Sql(@"DROP INDEX IF EXISTS ""IX_Chunks_Embedding_hnsw"";");

        // Drop Chunks first (has FK to Documents)
        Delete.Table(ChunksTable);

        // Drop Documents trigger and table
        DropUpdateTrigger(DocumentsTable);
        Delete.Table(DocumentsTable);
    }
}
