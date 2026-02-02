// =============================================================================
// File: Migration_006_AxiomTables.cs
// Project: Lexichord.Infrastructure
// Description: Database migration for the Axioms table.
// =============================================================================
// LOGIC: Creates the Axioms table for storing domain axioms.
//   - Id: String primary key (e.g., "AXIOM-001").
//   - JSONB columns for Rules and Tags (PostgreSQL native JSON support).
//   - Composite index for common queries (TargetType + IsEnabled).
//   - GIN index for tag-based queries.
//
// v0.4.6f: Axiom Repository (CKVS Phase 1c)
// Dependencies: FluentMigrator, PostgreSQL
// =============================================================================

using FluentMigrator;

namespace Lexichord.Infrastructure.Migrations;

/// <summary>
/// Migration 006: Creates the Axioms table for domain axiom storage.
/// </summary>
/// <remarks>
/// <para>
/// This migration establishes the database schema for storing axioms,
/// which define structural invariants for Knowledge Graph entities.
/// </para>
/// <para>
/// <b>Table:</b> Axioms
/// </para>
/// <para>
/// <b>Indexes:</b>
/// <list type="bullet">
///   <item><description>IX_Axioms_TargetType — Fast lookup by entity type.</description></item>
///   <item><description>IX_Axioms_Category — Category-based queries.</description></item>
///   <item><description>IX_Axioms_SourceFile — Source file queries.</description></item>
///   <item><description>IX_Axioms_Type_Enabled — Composite index for common queries.</description></item>
///   <item><description>IX_Axioms_Tags — GIN index for tag queries.</description></item>
/// </list>
/// </para>
/// </remarks>
[Migration(6)]
public class Migration_006_AxiomTables : LexichordMigration
{
    /// <summary>
    /// Creates the Axioms table and associated indexes.
    /// </summary>
    public override void Up()
    {
        // LOGIC: Create the Axioms table with all required columns.
        Create.Table("Axioms")
            // Primary key: String identifier (e.g., "AXIOM-001").
            .WithColumn("Id").AsString(100).NotNullable().PrimaryKey()

            // Core metadata.
            .WithColumn("Name").AsString(200).NotNullable()
            .WithColumn("Description").AsString(1000).Nullable()

            // Target specification.
            .WithColumn("TargetType").AsString(100).NotNullable()
            .WithColumn("TargetKind").AsString(50).NotNullable().WithDefaultValue("Entity")

            // Rules stored as JSONB for efficient querying.
            .WithColumn("RulesJson").AsCustom("jsonb").NotNullable()

            // Severity level.
            .WithColumn("Severity").AsString(50).NotNullable().WithDefaultValue("Error")

            // Organization.
            .WithColumn("Category").AsString(100).Nullable()
            .WithColumn("TagsJson").AsCustom("jsonb").NotNullable().WithDefaultValue("[]")

            // State.
            .WithColumn("IsEnabled").AsBoolean().NotNullable().WithDefaultValue(true)

            // Provenance.
            .WithColumn("SourceFile").AsString(500).Nullable()

            // Versioning.
            .WithColumn("SchemaVersion").AsString(20).NotNullable().WithDefaultValue("1.0")

            // Timestamps.
            .WithColumn("CreatedAt").AsDateTimeOffset().NotNullable()
            .WithColumn("UpdatedAt").AsDateTimeOffset().NotNullable()

            // Optimistic concurrency.
            .WithColumn("Version").AsInt32().NotNullable().WithDefaultValue(1);

        // LOGIC: Add table comment.
        Execute.Sql("""
            COMMENT ON TABLE "Axioms" IS 'Stores domain axioms that define structural invariants for Knowledge Graph entities (v0.4.6f).'
            """);

        // LOGIC: Create single-column indexes.
        Create.Index("IX_Axioms_TargetType")
            .OnTable("Axioms")
            .OnColumn("TargetType");

        Create.Index("IX_Axioms_Category")
            .OnTable("Axioms")
            .OnColumn("Category");

        Create.Index("IX_Axioms_SourceFile")
            .OnTable("Axioms")
            .OnColumn("SourceFile");

        Create.Index("IX_Axioms_IsEnabled")
            .OnTable("Axioms")
            .OnColumn("IsEnabled");

        // LOGIC: Create composite index for common query pattern.
        Create.Index("IX_Axioms_Type_Enabled")
            .OnTable("Axioms")
            .OnColumn("TargetType")
            .Ascending()
            .OnColumn("IsEnabled")
            .Ascending();

        // LOGIC: Create GIN index for JSONB tag containment queries.
        Execute.Sql("""
            CREATE INDEX "IX_Axioms_Tags" ON "Axioms" USING GIN ("TagsJson")
            """);

        // LOGIC: Create update trigger for UpdatedAt column.
        Execute.Sql("""
            CREATE OR REPLACE FUNCTION update_axioms_updated_at()
            RETURNS TRIGGER AS $$
            BEGIN
                NEW."UpdatedAt" = NOW();
                RETURN NEW;
            END;
            $$ LANGUAGE plpgsql;

            CREATE TRIGGER trg_axioms_updated_at
            BEFORE UPDATE ON "Axioms"
            FOR EACH ROW
            EXECUTE FUNCTION update_axioms_updated_at();
            """);
    }

    /// <summary>
    /// Drops the Axioms table and associated objects.
    /// </summary>
    public override void Down()
    {
        // LOGIC: Drop trigger and function first.
        Execute.Sql("""
            DROP TRIGGER IF EXISTS trg_axioms_updated_at ON "Axioms";
            DROP FUNCTION IF EXISTS update_axioms_updated_at();
            """);

        // LOGIC: Drop indexes (handled automatically by table drop, but explicit for clarity).
        Delete.Index("IX_Axioms_Tags").OnTable("Axioms");
        Delete.Index("IX_Axioms_Type_Enabled").OnTable("Axioms");
        Delete.Index("IX_Axioms_IsEnabled").OnTable("Axioms");
        Delete.Index("IX_Axioms_SourceFile").OnTable("Axioms");
        Delete.Index("IX_Axioms_Category").OnTable("Axioms");
        Delete.Index("IX_Axioms_TargetType").OnTable("Axioms");

        // LOGIC: Drop the table.
        Delete.Table("Axioms");
    }
}
