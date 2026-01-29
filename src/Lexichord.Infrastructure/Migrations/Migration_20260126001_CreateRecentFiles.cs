using FluentMigrator;

namespace Lexichord.Infrastructure.Migrations;

/// <summary>
/// Creates the RecentFiles table for MRU file tracking.
/// </summary>
/// <remarks>
/// LOGIC: This migration creates the persistence layer for v0.1.4d Recent Files History.
///
/// Schema design:
/// - Id (UUID): Surrogate primary key for efficient indexing
/// - FilePath: Unique natural key for lookup by path
/// - FileName: Cached display name (avoids path parsing at runtime)
/// - LastOpenedAt: Primary sort key for MRU ordering
/// - OpenCount: Usage frequency for potential smart sorting
/// - CreatedAt/UpdatedAt: Audit timestamps
///
/// Index:
/// - IX_RecentFiles_LastOpenedAt: Descending index for MRU queries
/// </remarks>
[Migration(20260126001, "Create RecentFiles table for MRU history")]
public class Migration_20260126001_CreateRecentFiles : LexichordMigration
{
    public override void Up()
    {
        // =========================================================================
        // RecentFiles Table
        // =========================================================================
        Create.Table("RecentFiles")
            .WithColumn("Id")
                .AsGuid()
                .NotNullable()
                .PrimaryKey()
                .WithDefaultValue(RawSql.Insert("gen_random_uuid()"))
            .WithColumn("FilePath")
                .AsString(1024)
                .NotNullable()
                .Unique()
            .WithColumn("FileName")
                .AsString(255)
                .NotNullable()
            .WithColumn("LastOpenedAt")
                .AsCustom("TIMESTAMPTZ")
                .NotNullable()
                .WithDefaultValue(RawSql.Insert("NOW()"))
            .WithColumn("OpenCount")
                .AsInt32()
                .NotNullable()
                .WithDefaultValue(1)
            .WithColumn("CreatedAt")
                .AsCustom("TIMESTAMPTZ")
                .NotNullable()
                .WithDefaultValue(RawSql.Insert("NOW()"))
            .WithColumn("UpdatedAt")
                .AsCustom("TIMESTAMPTZ")
                .NotNullable()
                .WithDefaultValue(RawSql.Insert("NOW()"));

        // LOGIC: Descending index on LastOpenedAt for efficient MRU queries
        Create.Index(IndexName("RecentFiles", "LastOpenedAt"))
            .OnTable("RecentFiles")
            .OnColumn("LastOpenedAt")
            .Descending();

        // LOGIC: Auto-update UpdatedAt on row modification
        CreateUpdateTrigger("RecentFiles");

        // Add table and column comments
        Execute.Sql(@"
            COMMENT ON TABLE ""RecentFiles"" IS 'Most Recently Used (MRU) file history';
            COMMENT ON COLUMN ""RecentFiles"".""Id"" IS 'Unique identifier (UUID)';
            COMMENT ON COLUMN ""RecentFiles"".""FilePath"" IS 'Absolute path to the file (unique)';
            COMMENT ON COLUMN ""RecentFiles"".""FileName"" IS 'Display name (cached from path)';
            COMMENT ON COLUMN ""RecentFiles"".""LastOpenedAt"" IS 'When the file was last opened';
            COMMENT ON COLUMN ""RecentFiles"".""OpenCount"" IS 'Number of times the file has been opened';
            COMMENT ON COLUMN ""RecentFiles"".""CreatedAt"" IS 'When the entry was created';
            COMMENT ON COLUMN ""RecentFiles"".""UpdatedAt"" IS 'When the entry was last updated';
        ");
    }

    public override void Down()
    {
        // LOGIC: Drop in reverse order

        // Remove trigger first
        DropUpdateTrigger("RecentFiles");

        // Drop index (happens automatically with table drop, but explicit for clarity)
        Delete.Index(IndexName("RecentFiles", "LastOpenedAt")).OnTable("RecentFiles");

        // Drop table
        Delete.Table("RecentFiles");
    }
}
