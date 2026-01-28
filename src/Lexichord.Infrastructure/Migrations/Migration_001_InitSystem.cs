using FluentMigrator;

namespace Lexichord.Infrastructure.Migrations;

/// <summary>
/// Initial database schema migration for Lexichord system tables.
/// </summary>
/// <remarks>
/// LOGIC: This migration creates the foundational tables required for Lexichord:
///
/// Users table:
/// - Stores user accounts and authentication data
/// - Email is unique and used as login identifier
/// - PasswordHash is nullable to support OAuth-only users
/// - IsActive enables soft-delete/deactivation
///
/// SystemSettings table:
/// - Key-value store for application configuration
/// - Used for settings that may change without restart
/// - Key is the primary key (unique identifier)
///
/// Rollback:
/// - Drops tables in reverse dependency order
/// - Removes triggers before dropping tables
/// </remarks>
[Migration(1, "Initial system tables: Users and SystemSettings")]
public class Migration_001_InitSystem : LexichordMigration
{
    public override void Up()
    {
        // =========================================================================
        // Users Table
        // =========================================================================
        Create.Table("Users")
            .WithColumn("Id")
                .AsGuid()
                .NotNullable()
                .PrimaryKey()
                .WithDefaultValue(RawSql.Insert("gen_random_uuid()"))
            .WithColumn("Email")
                .AsString(255)
                .NotNullable()
                .Unique()
            .WithColumn("DisplayName")
                .AsString(100)
                .NotNullable()
            .WithColumn("PasswordHash")
                .AsString(255)
                .Nullable()
            .WithColumn("IsActive")
                .AsBoolean()
                .NotNullable()
                .WithDefaultValue(true)
            .WithColumn("CreatedAt")
                .AsCustom("TIMESTAMPTZ")
                .NotNullable()
                .WithDefaultValue(RawSql.Insert("NOW()"))
            .WithColumn("UpdatedAt")
                .AsCustom("TIMESTAMPTZ")
                .NotNullable()
                .WithDefaultValue(RawSql.Insert("NOW()"));

        // LOGIC: Index on Email for login queries
        Create.Index(IndexName("Users", "Email"))
            .OnTable("Users")
            .OnColumn("Email")
            .Ascending();

        // LOGIC: Partial index for active users (common filter)
        Execute.Sql(@"
            CREATE INDEX ""IX_Users_IsActive""
            ON ""Users"" (""IsActive"")
            WHERE ""IsActive"" = TRUE;
        ");

        // LOGIC: Auto-update UpdatedAt on row modification
        CreateUpdateTrigger("Users");

        // Add table description
        Execute.Sql(@"
            COMMENT ON TABLE ""Users"" IS 'Lexichord user accounts and authentication data';
            COMMENT ON COLUMN ""Users"".""Id"" IS 'Unique identifier for the user (UUID)';
            COMMENT ON COLUMN ""Users"".""Email"" IS 'User email address (unique, used for login)';
            COMMENT ON COLUMN ""Users"".""DisplayName"" IS 'User display name shown in the UI';
            COMMENT ON COLUMN ""Users"".""PasswordHash"" IS 'Bcrypt password hash (null for OAuth users)';
            COMMENT ON COLUMN ""Users"".""IsActive"" IS 'Whether the user account is active';
            COMMENT ON COLUMN ""Users"".""CreatedAt"" IS 'When the user was created';
            COMMENT ON COLUMN ""Users"".""UpdatedAt"" IS 'When the user was last updated';
        ");

        // =========================================================================
        // SystemSettings Table
        // =========================================================================
        Create.Table("SystemSettings")
            .WithColumn("Key")
                .AsString(100)
                .NotNullable()
                .PrimaryKey()
            .WithColumn("Value")
                .AsCustom("TEXT")
                .NotNullable()
            .WithColumn("Description")
                .AsString(500)
                .Nullable()
            .WithColumn("UpdatedAt")
                .AsCustom("TIMESTAMPTZ")
                .NotNullable()
                .WithDefaultValue(RawSql.Insert("NOW()"));

        // LOGIC: Auto-update UpdatedAt on row modification
        CreateUpdateTrigger("SystemSettings");

        // Add table description
        Execute.Sql(@"
            COMMENT ON TABLE ""SystemSettings"" IS 'Key-value store for application configuration';
            COMMENT ON COLUMN ""SystemSettings"".""Key"" IS 'Setting key (unique identifier)';
            COMMENT ON COLUMN ""SystemSettings"".""Value"" IS 'Setting value as text';
            COMMENT ON COLUMN ""SystemSettings"".""Description"" IS 'Human-readable description of the setting';
            COMMENT ON COLUMN ""SystemSettings"".""UpdatedAt"" IS 'When the setting was last updated';
        ");

        // =========================================================================
        // Seed Initial System Settings
        // =========================================================================
        Insert.IntoTable("SystemSettings")
            .Row(new
            {
                Key = "app:initialized",
                Value = "true",
                Description = "Indicates the application has been initialized"
            })
            .Row(new
            {
                Key = "app:version",
                Value = "0.0.5",
                Description = "Current application version"
            })
            .Row(new
            {
                Key = "system:maintenance_mode",
                Value = "false",
                Description = "When true, application is in maintenance mode"
            });
    }

    public override void Down()
    {
        // LOGIC: Drop in reverse dependency order

        // Remove triggers first
        DropUpdateTrigger("SystemSettings");
        DropUpdateTrigger("Users");

        // Drop indexes (happens automatically with table drop, but explicit for clarity)
        Delete.Index("IX_Users_IsActive").OnTable("Users");
        Delete.Index(IndexName("Users", "Email")).OnTable("Users");

        // Drop tables
        Delete.Table("SystemSettings");
        Delete.Table("Users");
    }
}
