using FluentMigrator;

namespace Lexichord.Infrastructure.Migrations;

/// <summary>
/// Migration for the style_terms table in the Terminology Database.
/// </summary>
/// <remarks>
/// LOGIC: This migration creates the style_terms table which stores individual
/// terminology entries that are part of a style sheet. Each term represents
/// a word or phrase that should be flagged during writing, with an optional
/// replacement suggestion.
///
/// Table design decisions:
/// - StyleSheetId: Links term to its parent stylesheet (FK added in later migration)
/// - Term: The text pattern to match (unique within a stylesheet)
/// - Replacement: Suggested alternative (nullable for "avoid this" rules)
/// - Category: Groups terms for filtering (General, Terminology, Brand, etc.)
/// - Severity: Determines UI treatment (Error, Warning, Suggestion)
/// - IsActive: Soft-disable without deletion
///
/// Indexes:
/// - Compound unique on (StyleSheetId, Term) prevents duplicates
/// - Partial index on IsActive for active-only queries
/// - Category index for filtered views
/// - Trigram index for fuzzy search
///
/// Rollback:
/// - Drops trigger, indexes, and table in correct order
/// </remarks>
[Migration(2, "Style terms table for terminology database")]
public class Migration_002_StyleSchema : LexichordMigration
{
    private const string TableName = "style_terms";

    public override void Up()
    {
        // =========================================================================
        // style_terms Table
        // =========================================================================
        Create.Table(TableName)
            .WithColumn("Id")
                .AsGuid()
                .NotNullable()
                .PrimaryKey()
                .WithDefaultValue(RawSql.Insert("gen_random_uuid()"))
            .WithColumn("StyleSheetId")
                .AsGuid()
                .NotNullable()
            .WithColumn("Term")
                .AsString(255)
                .NotNullable()
            .WithColumn("Replacement")
                .AsString(500)
                .Nullable()
            .WithColumn("Category")
                .AsString(100)
                .NotNullable()
                .WithDefaultValue("General")
            .WithColumn("Severity")
                .AsString(20)
                .NotNullable()
                .WithDefaultValue("Suggestion")
            .WithColumn("IsActive")
                .AsBoolean()
                .NotNullable()
                .WithDefaultValue(true)
            .WithColumn("Notes")
                .AsCustom("TEXT")
                .Nullable()
            .WithColumn("CreatedAt")
                .AsCustom("TIMESTAMPTZ")
                .NotNullable()
                .WithDefaultValue(RawSql.Insert("NOW()"))
            .WithColumn("UpdatedAt")
                .AsCustom("TIMESTAMPTZ")
                .NotNullable()
                .WithDefaultValue(RawSql.Insert("NOW()"));

        // =========================================================================
        // Unique Constraint: One term per stylesheet
        // =========================================================================
        Create.Index(IndexName(TableName, "StyleSheetId", "Term"))
            .OnTable(TableName)
            .OnColumn("StyleSheetId").Ascending()
            .OnColumn("Term").Ascending()
            .WithOptions().Unique();

        // =========================================================================
        // Index: Active terms filter (partial index)
        // =========================================================================
        Execute.Sql($@"
            CREATE INDEX ""IX_{TableName}_IsActive""
            ON ""{TableName}"" (""IsActive"")
            WHERE ""IsActive"" = TRUE;
        ");

        // =========================================================================
        // Index: Category lookup
        // =========================================================================
        Create.Index(IndexName(TableName, "Category"))
            .OnTable(TableName)
            .OnColumn("Category")
            .Ascending();

        // =========================================================================
        // Index: Full-text search using pg_trgm
        // =========================================================================
        Execute.Sql($@"
            CREATE EXTENSION IF NOT EXISTS pg_trgm;
            CREATE INDEX ""IX_{TableName}_Term_trgm""
            ON ""{TableName}""
            USING gin (""Term"" gin_trgm_ops);
        ");

        // =========================================================================
        // Trigger: Auto-update UpdatedAt
        // =========================================================================
        CreateUpdateTrigger(TableName);

        // =========================================================================
        // Column Comments
        // =========================================================================
        Execute.Sql($@"
            COMMENT ON TABLE ""{TableName}"" IS 'Terminology entries for style sheets';
            COMMENT ON COLUMN ""{TableName}"".""Id"" IS 'Unique identifier for the term (UUID)';
            COMMENT ON COLUMN ""{TableName}"".""StyleSheetId"" IS 'Parent style sheet this term belongs to';
            COMMENT ON COLUMN ""{TableName}"".""Term"" IS 'Text pattern to match (case-sensitive)';
            COMMENT ON COLUMN ""{TableName}"".""Replacement"" IS 'Suggested replacement text (null for avoid-only rules)';
            COMMENT ON COLUMN ""{TableName}"".""Category"" IS 'Grouping category (General, Terminology, Brand, etc.)';
            COMMENT ON COLUMN ""{TableName}"".""Severity"" IS 'Violation severity (Error, Warning, Suggestion)';
            COMMENT ON COLUMN ""{TableName}"".""IsActive"" IS 'Whether this term is currently active';
            COMMENT ON COLUMN ""{TableName}"".""Notes"" IS 'Optional notes or rationale for this term';
            COMMENT ON COLUMN ""{TableName}"".""CreatedAt"" IS 'When the term was created';
            COMMENT ON COLUMN ""{TableName}"".""UpdatedAt"" IS 'When the term was last updated';
        ");
    }

    public override void Down()
    {
        // LOGIC: Drop in reverse dependency order

        // Remove trigger first
        DropUpdateTrigger(TableName);

        // Drop indexes (explicit for clarity, though table drop would remove them)
        Execute.Sql($@"DROP INDEX IF EXISTS ""IX_{TableName}_Term_trgm"";");
        Delete.Index(IndexName(TableName, "Category")).OnTable(TableName);
        Execute.Sql($@"DROP INDEX IF EXISTS ""IX_{TableName}_IsActive"";");
        Delete.Index(IndexName(TableName, "StyleSheetId", "Term")).OnTable(TableName);

        // Drop table
        Delete.Table(TableName);
    }
}
