using FluentMigrator;

namespace Lexichord.Infrastructure.Migrations;

/// <summary>
/// Migration to add fuzzy matching columns to style_terms table.
/// </summary>
/// <remarks>
/// LOGIC: This migration adds support for fuzzy (approximate) matching on style terms.
/// 
/// New columns:
/// - FuzzyEnabled: Boolean flag to enable fuzzy matching for a term
/// - FuzzyThreshold: Similarity threshold (0.50-1.00) for fuzzy matches
///
/// Index:
/// - Partial index on FuzzyEnabled for efficient filtered queries
///
/// Design decisions:
/// - Default FuzzyEnabled to FALSE to preserve existing exact-match behavior
/// - Default threshold to 0.80 (80% similarity) as a sensible starting point
/// - DECIMAL(3,2) allows values 0.00-9.99 but application validates 0.50-1.00
/// </remarks>
[Migration(20260126_1500, "Add fuzzy matching columns to style_terms")]
public class Migration_20260126_1500_AddFuzzyColumnsToStyleTerms : LexichordMigration
{
    private const string TableName = "style_terms";

    public override void Up()
    {
        // =========================================================================
        // Add FuzzyEnabled column
        // =========================================================================
        Alter.Table(TableName)
            .AddColumn("FuzzyEnabled")
                .AsBoolean()
                .NotNullable()
                .WithDefaultValue(false);

        // =========================================================================
        // Add FuzzyThreshold column
        // =========================================================================
        Alter.Table(TableName)
            .AddColumn("FuzzyThreshold")
                .AsDecimal(3, 2)
                .NotNullable()
                .WithDefaultValue(0.80m);

        // =========================================================================
        // Partial index for fuzzy-enabled terms
        // =========================================================================
        Execute.Sql($@"
            CREATE INDEX ""IX_{TableName}_fuzzy_enabled""
            ON ""{TableName}"" (""FuzzyEnabled"")
            WHERE ""FuzzyEnabled"" = TRUE;
        ");

        // =========================================================================
        // Column Comments
        // =========================================================================
        Execute.Sql($@"
            COMMENT ON COLUMN ""{TableName}"".""FuzzyEnabled"" IS 'Whether this term supports fuzzy (approximate) matching';
            COMMENT ON COLUMN ""{TableName}"".""FuzzyThreshold"" IS 'Similarity threshold for fuzzy matching (0.50-1.00)';
        ");
    }

    public override void Down()
    {
        // LOGIC: Drop in reverse order - index first, then columns

        // Drop partial index
        Execute.Sql($@"DROP INDEX IF EXISTS ""IX_{TableName}_fuzzy_enabled"";");

        // Drop columns
        Delete.Column("FuzzyThreshold").FromTable(TableName);
        Delete.Column("FuzzyEnabled").FromTable(TableName);
    }
}
