// =============================================================================
// File: Migration_005_GraphMetadata.cs
// Project: Lexichord.Infrastructure
// Description: Creates PostgreSQL metadata tables for Knowledge Graph tracking.
// =============================================================================
// LOGIC: This migration creates the relational metadata tables that track
//   graph database state in PostgreSQL. While entities and relationships live
//   in Neo4j, their metadata (connection status, counts, document linkage)
//   is tracked in PostgreSQL for consistency with the existing data layer.
//
// Tables:
//   - GraphMetadata: Tracks Neo4j connection state, entity/relationship counts,
//     and schema version for operational monitoring.
//   - DocumentEntities: Links RAG documents to Knowledge Graph entities,
//     enabling cross-system queries (e.g., "which documents mention entity X?").
//
// Indexes:
//   - IX_DocumentEntities_EntityId: Fast lookup of documents by entity.
//   - IX_GraphMetadata_ConnectionUri: Unique constraint on connection URI.
//
// v0.4.5e: Graph Database Integration (CKVS Phase 1)
// Dependencies: Migration_003_VectorSchema (Documents table for FK reference)
// =============================================================================

using FluentMigrator;

namespace Lexichord.Infrastructure.Migrations;

/// <summary>
/// Creates metadata tables for Knowledge Graph tracking in PostgreSQL.
/// </summary>
/// <remarks>
/// <para>
/// This migration creates the relational side of the Knowledge Graph integration.
/// While the graph entities and relationships are stored in Neo4j, PostgreSQL
/// tracks operational metadata and document-entity linkage.
/// </para>
/// <para>
/// <b>Tables Created:</b>
/// <list type="bullet">
///   <item><description><c>GraphMetadata</c>: Tracks Neo4j connection state and graph statistics.</description></item>
///   <item><description><c>DocumentEntities</c>: Links RAG documents to Knowledge Graph entities.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Cross-System Linkage:</b> The <c>DocumentEntities</c> table bridges the RAG
/// system (Documents in PostgreSQL) with the Knowledge Graph (entities in Neo4j).
/// The <c>EntityId</c> column stores the UUID of the Neo4j entity, enabling
/// queries like "find all documents that mention a specific entity."
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5e as part of the Knowledge Graph Foundation.
/// </para>
/// </remarks>
[Migration(5, "Graph metadata tables for Knowledge Graph tracking")]
[Tags("Knowledge", "Graph")]
public class Migration_005_GraphMetadata : LexichordMigration
{
    private const string GraphMetadataTable = "GraphMetadata";
    private const string DocumentEntitiesTable = "DocumentEntities";
    private const string DocumentsTable = "Documents";

    /// <summary>
    /// Creates the GraphMetadata and DocumentEntities tables.
    /// </summary>
    /// <remarks>
    /// LOGIC: Creates two tables:
    /// 1. GraphMetadata — singleton row tracking Neo4j connection state.
    /// 2. DocumentEntities — many-to-many link between Documents and graph entities.
    /// Both tables follow Lexichord conventions (PascalCase, UUID PKs, TIMESTAMPTZ).
    /// </remarks>
    public override void Up()
    {
        // ═══════════════════════════════════════════════════════════════════════
        // GraphMetadata Table
        // ═══════════════════════════════════════════════════════════════════════
        // LOGIC: Tracks the operational state of the Neo4j graph database.
        // Typically contains a single row representing the current connection.
        // Used by health checks and admin dashboards for monitoring.
        Create.Table(GraphMetadataTable)
            .WithColumn("Id")
                .AsGuid()
                .NotNullable()
                .PrimaryKey()
                .WithDefaultValue(RawSql.Insert("gen_random_uuid()"))
            .WithColumn("ConnectionUri")
                .AsString(500)
                .NotNullable()
            .WithColumn("DatabaseName")
                .AsString(100)
                .NotNullable()
            .WithColumn("LastConnectedAt")
                .AsCustom("TIMESTAMPTZ")
                .Nullable()
            .WithColumn("EntityCount")
                .AsInt32()
                .NotNullable()
                .WithDefaultValue(0)
            .WithColumn("RelationshipCount")
                .AsInt32()
                .NotNullable()
                .WithDefaultValue(0)
            .WithColumn("SchemaVersion")
                .AsString(50)
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
        // GraphMetadata Indexes
        // ═══════════════════════════════════════════════════════════════════════
        // LOGIC: Unique constraint on ConnectionUri prevents duplicate entries
        // for the same Neo4j instance.
        Create.Index(IndexName(GraphMetadataTable, "ConnectionUri"))
            .OnTable(GraphMetadataTable)
            .OnColumn("ConnectionUri")
            .Ascending()
            .WithOptions().Unique();

        // LOGIC: Auto-update trigger for the UpdatedAt column.
        CreateUpdateTrigger(GraphMetadataTable);

        // ═══════════════════════════════════════════════════════════════════════
        // DocumentEntities Table
        // ═══════════════════════════════════════════════════════════════════════
        // LOGIC: Many-to-many relationship between RAG Documents and Knowledge
        // Graph entities. The DocumentId references the Documents table (created
        // in Migration_003_VectorSchema). The EntityId is a UUID matching the
        // entity's "id" property in Neo4j (not a PostgreSQL FK — the entity
        // lives in Neo4j).
        Create.Table(DocumentEntitiesTable)
            .WithColumn("DocumentId")
                .AsGuid()
                .NotNullable()
                .ForeignKey(ForeignKeyName(DocumentEntitiesTable, DocumentsTable), DocumentsTable, "Id")
                .OnDelete(System.Data.Rule.Cascade)
            .WithColumn("EntityId")
                .AsGuid()
                .NotNullable()
            .WithColumn("MentionCount")
                .AsInt32()
                .NotNullable()
                .WithDefaultValue(1)
            .WithColumn("FirstSeenAt")
                .AsCustom("TIMESTAMPTZ")
                .NotNullable()
                .WithDefaultValue(RawSql.Insert("NOW()"))
            .WithColumn("LastSeenAt")
                .AsCustom("TIMESTAMPTZ")
                .NotNullable()
                .WithDefaultValue(RawSql.Insert("NOW()"));

        // ═══════════════════════════════════════════════════════════════════════
        // DocumentEntities Indexes & Constraints
        // ═══════════════════════════════════════════════════════════════════════
        // LOGIC: Composite primary key on (DocumentId, EntityId) enforces
        // uniqueness of the document-entity relationship.
        Create.PrimaryKey($"PK_{DocumentEntitiesTable}")
            .OnTable(DocumentEntitiesTable)
            .Columns("DocumentId", "EntityId");

        // LOGIC: Index on EntityId for reverse lookups ("which documents
        // mention this entity?").
        Create.Index(IndexName(DocumentEntitiesTable, "EntityId"))
            .OnTable(DocumentEntitiesTable)
            .OnColumn("EntityId")
            .Ascending();

        // ═══════════════════════════════════════════════════════════════════════
        // Column Comments
        // ═══════════════════════════════════════════════════════════════════════
        // LOGIC: PostgreSQL column comments provide inline documentation
        // visible in pgAdmin and other database tools.
        Execute.Sql($@"
            COMMENT ON TABLE ""{GraphMetadataTable}"" IS 'Operational metadata for the Neo4j Knowledge Graph connection';
            COMMENT ON COLUMN ""{GraphMetadataTable}"".""Id"" IS 'Unique identifier (UUID)';
            COMMENT ON COLUMN ""{GraphMetadataTable}"".""ConnectionUri"" IS 'Neo4j Bolt connection URI';
            COMMENT ON COLUMN ""{GraphMetadataTable}"".""DatabaseName"" IS 'Neo4j database name';
            COMMENT ON COLUMN ""{GraphMetadataTable}"".""LastConnectedAt"" IS 'Timestamp of last successful connection';
            COMMENT ON COLUMN ""{GraphMetadataTable}"".""EntityCount"" IS 'Cached count of entities in the graph';
            COMMENT ON COLUMN ""{GraphMetadataTable}"".""RelationshipCount"" IS 'Cached count of relationships in the graph';
            COMMENT ON COLUMN ""{GraphMetadataTable}"".""SchemaVersion"" IS 'Current graph schema version identifier';

            COMMENT ON TABLE ""{DocumentEntitiesTable}"" IS 'Links RAG documents to Knowledge Graph entities';
            COMMENT ON COLUMN ""{DocumentEntitiesTable}"".""DocumentId"" IS 'FK to Documents table (RAG system)';
            COMMENT ON COLUMN ""{DocumentEntitiesTable}"".""EntityId"" IS 'UUID of the entity in Neo4j';
            COMMENT ON COLUMN ""{DocumentEntitiesTable}"".""MentionCount"" IS 'Number of times the entity is mentioned in the document';
            COMMENT ON COLUMN ""{DocumentEntitiesTable}"".""FirstSeenAt"" IS 'When the entity was first detected in the document';
            COMMENT ON COLUMN ""{DocumentEntitiesTable}"".""LastSeenAt"" IS 'When the entity was last detected in the document';
        ");
    }

    /// <summary>
    /// Drops the DocumentEntities and GraphMetadata tables.
    /// </summary>
    /// <remarks>
    /// LOGIC: Drop in reverse dependency order:
    /// 1. DocumentEntities (has FK to Documents).
    /// 2. GraphMetadata trigger and table.
    /// </remarks>
    public override void Down()
    {
        // LOGIC: Drop DocumentEntities first (has FK dependency)
        Delete.Table(DocumentEntitiesTable);

        // LOGIC: Drop GraphMetadata trigger and table
        DropUpdateTrigger(GraphMetadataTable);
        Delete.Table(GraphMetadataTable);
    }
}
