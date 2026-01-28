using FluentMigrator;

namespace Lexichord.Infrastructure.Migrations;

/// <summary>
/// Lexichord migration conventions and constants.
/// </summary>
/// <remarks>
/// LOGIC: All migrations follow these conventions:
///
/// Naming:
/// - Migration classes: Migration_{Version}_{Description}
/// - Version format: 001, 002, ... (3-digit padded)
/// - Tables: PascalCase, plural (Users, Documents, SystemSettings)
/// - Columns: PascalCase (Id, CreatedAt, UpdatedAt)
/// - Indexes: IX_{Table}_{Column(s)}
/// - Foreign Keys: FK_{Table}_{ReferencedTable}
///
/// Standard columns for all entities:
/// - Id: UUID primary key (gen_random_uuid())
/// - CreatedAt: TIMESTAMPTZ NOT NULL DEFAULT NOW()
/// - UpdatedAt: TIMESTAMPTZ NOT NULL DEFAULT NOW()
///
/// Best practices:
/// - Always implement Down() for rollback capability
/// - Use transactions (FluentMigrator default for Postgres)
/// - Create indexes for commonly queried columns
/// - Add comments/descriptions to tables and columns
/// </remarks>
public static class MigrationConventions
{
    /// <summary>
    /// Standard timestamp column type for PostgreSQL.
    /// </summary>
    public const string TimestampType = "TIMESTAMPTZ";

    /// <summary>
    /// Standard UUID column type for PostgreSQL.
    /// </summary>
    public const string UuidType = "UUID";

    /// <summary>
    /// Default value expression for UUID primary keys.
    /// </summary>
    public const string UuidDefault = "gen_random_uuid()";

    /// <summary>
    /// Default value expression for timestamp columns.
    /// </summary>
    public const string TimestampDefault = "NOW()";

    /// <summary>
    /// Schema name for Lexichord tables.
    /// </summary>
    public const string SchemaName = "public";
}

/// <summary>
/// Base class for Lexichord migrations with helper methods.
/// </summary>
/// <remarks>
/// LOGIC: Provides consistent patterns for common migration operations:
/// - Standard column definitions (UUID PK, timestamps)
/// - Index naming conventions
/// - Audit trigger creation
/// </remarks>
public abstract class LexichordMigration : Migration
{
    /// <summary>
    /// Creates an index name following conventions.
    /// </summary>
    /// <param name="tableName">The table name.</param>
    /// <param name="columns">The column names.</param>
    /// <returns>The formatted index name: IX_{Table}_{Column(s)}.</returns>
    public static string IndexName(string tableName, params string[] columns)
    {
        return $"IX_{tableName}_{string.Join("_", columns)}";
    }

    /// <summary>
    /// Creates a foreign key name following conventions.
    /// </summary>
    /// <param name="fromTable">The referencing table.</param>
    /// <param name="toTable">The referenced table.</param>
    /// <returns>The formatted foreign key name: FK_{FromTable}_{ToTable}.</returns>
    public static string ForeignKeyName(string fromTable, string toTable)
    {
        return $"FK_{fromTable}_{toTable}";
    }

    /// <summary>
    /// Creates an update trigger for the UpdatedAt column.
    /// </summary>
    /// <param name="tableName">The table to add the trigger to.</param>
    protected void CreateUpdateTrigger(string tableName)
    {
        // LOGIC: PostgreSQL trigger to auto-update UpdatedAt on row modification
        Execute.Sql($@"
            CREATE OR REPLACE FUNCTION update_{tableName.ToLowerInvariant()}_updated_at()
            RETURNS TRIGGER AS $$
            BEGIN
                NEW.""UpdatedAt"" = NOW();
                RETURN NEW;
            END;
            $$ LANGUAGE plpgsql;

            CREATE TRIGGER trg_{tableName.ToLowerInvariant()}_updated_at
                BEFORE UPDATE ON ""{tableName}""
                FOR EACH ROW
                EXECUTE FUNCTION update_{tableName.ToLowerInvariant()}_updated_at();
        ");
    }

    /// <summary>
    /// Drops an update trigger for the UpdatedAt column.
    /// </summary>
    /// <param name="tableName">The table to remove the trigger from.</param>
    protected void DropUpdateTrigger(string tableName)
    {
        Execute.Sql($@"
            DROP TRIGGER IF EXISTS trg_{tableName.ToLowerInvariant()}_updated_at ON ""{tableName}"";
            DROP FUNCTION IF EXISTS update_{tableName.ToLowerInvariant()}_updated_at();
        ");
    }
}
