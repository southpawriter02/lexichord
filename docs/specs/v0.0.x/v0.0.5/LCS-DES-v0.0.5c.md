# LCS-DES-005c: FluentMigrator Runner

## 1. Metadata & Categorization

| Field                | Value                                | Description                                  |
| :------------------- | :----------------------------------- | :------------------------------------------- |
| **Feature ID**       | `INF-005c`                           | Infrastructure - FluentMigrator Runner       |
| **Feature Name**     | FluentMigrator Runner                | Database Schema Versioning                   |
| **Target Version**   | `v0.0.5c`                            | Third sub-part of v0.0.5                     |
| **Module Scope**     | `Lexichord.Infrastructure`           | Data access infrastructure                   |
| **Swimlane**         | `Infrastructure`                     | The Podium (Platform)                        |
| **License Tier**     | `Core`                               | Foundation (Required for all tiers)          |
| **Author**           | System Architect                     |                                              |
| **Status**           | **Draft**                            | Pending implementation                       |
| **Last Updated**     | 2026-01-26                           |                                              |

---

## 2. Executive Summary

### 2.1 The Requirement

Lexichord requires a **versioned database schema management system** that:

- Tracks schema changes in source control.
- Applies migrations automatically on deployment.
- Supports rollback for failed deployments.
- Provides consistent schema across all environments.

Without this foundation:

- Schema changes require manual SQL execution.
- Developers have inconsistent database states.
- Rollback of failed deployments is manual and error-prone.
- No audit trail of schema evolution exists.

### 2.2 The Proposed Solution

We **SHALL** implement FluentMigrator for schema versioning with:

1. **Migration Infrastructure** — Conventions, base classes, and runner configuration.
2. **Migration_001_InitSystem** — Initial schema with `Users` and `SystemSettings` tables.
3. **CLI Integration** — `--migrate` flag for startup migration execution.
4. **Rollback Support** — `--rollback` flag for reverting migrations.

---

## 3. Implementation Tasks

### Task 1.1: Install NuGet Packages

**File:** `src/Lexichord.Infrastructure/Lexichord.Infrastructure.csproj`

```xml
<ItemGroup>
  <!-- FluentMigrator -->
  <PackageReference Include="FluentMigrator" Version="6.2.0" />
  <PackageReference Include="FluentMigrator.Runner" Version="6.2.0" />
  <PackageReference Include="FluentMigrator.Runner.Postgres" Version="6.2.0" />
</ItemGroup>
```

**Rationale:**

- `FluentMigrator` provides the migration definition API.
- `FluentMigrator.Runner` provides execution and version tracking.
- `FluentMigrator.Runner.Postgres` adds PostgreSQL-specific features.

---

### Task 1.2: Define Migration Conventions

**File:** `src/Lexichord.Infrastructure/Migrations/MigrationConventions.cs`

```csharp
using FluentMigrator;
using FluentMigrator.Runner.Conventions;

namespace Lexichord.Infrastructure.Migrations;

/// <summary>
/// Lexichord migration conventions and base classes.
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
    /// Creates a standard UUID primary key column.
    /// </summary>
    /// <param name="table">The table expression builder.</param>
    /// <returns>The column expression builder for chaining.</returns>
    protected ICreateTableColumnOptionOrWithColumnSyntax WithUuidPrimaryKey(
        ICreateTableWithColumnSyntax table)
    {
        return table
            .WithColumn("Id")
            .AsCustom(MigrationConventions.UuidType)
            .NotNullable()
            .PrimaryKey()
            .WithDefaultValue(RawSql.Insert(MigrationConventions.UuidDefault));
    }

    /// <summary>
    /// Adds standard audit columns (CreatedAt, UpdatedAt) to a table.
    /// </summary>
    /// <param name="table">The table expression builder.</param>
    /// <returns>The column expression builder for chaining.</returns>
    protected ICreateTableColumnOptionOrWithColumnSyntax WithAuditColumns(
        ICreateTableColumnAsTypeSyntax table)
    {
        return table
            .WithColumn("CreatedAt")
            .AsCustom(MigrationConventions.TimestampType)
            .NotNullable()
            .WithDefaultValue(RawSql.Insert(MigrationConventions.TimestampDefault))
            .WithColumn("UpdatedAt")
            .AsCustom(MigrationConventions.TimestampType)
            .NotNullable()
            .WithDefaultValue(RawSql.Insert(MigrationConventions.TimestampDefault));
    }

    /// <summary>
    /// Creates an index name following conventions.
    /// </summary>
    /// <param name="tableName">The table name.</param>
    /// <param name="columns">The column names.</param>
    /// <returns>The formatted index name.</returns>
    protected static string IndexName(string tableName, params string[] columns)
    {
        return $"IX_{tableName}_{string.Join("_", columns)}";
    }

    /// <summary>
    /// Creates a foreign key name following conventions.
    /// </summary>
    /// <param name="fromTable">The referencing table.</param>
    /// <param name="toTable">The referenced table.</param>
    /// <returns>The formatted foreign key name.</returns>
    protected static string ForeignKeyName(string fromTable, string toTable)
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
```

---

### Task 1.3: Implement Migration_001_InitSystem

**File:** `src/Lexichord.Infrastructure/Migrations/Migration_001_InitSystem.cs`

```csharp
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
```

---

### Task 1.4: Configure Migration Runner in DI

**File:** `src/Lexichord.Infrastructure/Migrations/MigrationServices.cs`

```csharp
using FluentMigrator.Runner;
using FluentMigrator.Runner.Initialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Lexichord.Abstractions.Contracts;

namespace Lexichord.Infrastructure.Migrations;

/// <summary>
/// Extension methods for configuring FluentMigrator services.
/// </summary>
public static class MigrationServices
{
    /// <summary>
    /// Adds FluentMigrator services to the DI container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMigrationServices(this IServiceCollection services)
    {
        // LOGIC: FluentMigrator requires its own service configuration
        // We get the connection string from our DatabaseOptions
        services.AddFluentMigratorCore()
            .ConfigureRunner(rb =>
            {
                // Use PostgreSQL
                rb.AddPostgres()
                    // Scan this assembly for migrations
                    .ScanIn(typeof(MigrationServices).Assembly).For.Migrations()
                    // Configure version table
                    .WithVersionTable(new VersionTableMetaData());
            })
            // Enable logging
            .AddLogging(lb => lb.AddFluentMigratorConsole());

        // Register the migration runner wrapper
        services.AddScoped<IMigrationRunner, MigrationRunnerWrapper>();

        return services;
    }
}

/// <summary>
/// Custom version table configuration.
/// </summary>
/// <remarks>
/// LOGIC: Customizes the VersionInfo table that FluentMigrator uses to track
/// which migrations have been applied. We use a custom name to avoid conflicts.
/// </remarks>
public class VersionTableMetaData : FluentMigrator.Runner.VersionTableInfo.DefaultVersionTableMetaData
{
    public override string TableName => "SchemaVersions";
    public override string ColumnName => "Version";
    public override string DescriptionColumnName => "Description";
    public override string AppliedOnColumnName => "AppliedOn";
    public override string UniqueIndexName => "UC_SchemaVersions_Version";
}

/// <summary>
/// Interface for migration execution.
/// </summary>
public interface IMigrationRunner
{
    /// <summary>
    /// Runs all pending migrations.
    /// </summary>
    void MigrateUp();

    /// <summary>
    /// Runs migrations up to a specific version.
    /// </summary>
    /// <param name="version">The target version.</param>
    void MigrateUp(long version);

    /// <summary>
    /// Rolls back the last migration.
    /// </summary>
    void MigrateDown();

    /// <summary>
    /// Rolls back to a specific version.
    /// </summary>
    /// <param name="version">The target version.</param>
    void MigrateDown(long version);

    /// <summary>
    /// Lists all migrations and their status.
    /// </summary>
    /// <returns>List of migration info.</returns>
    IEnumerable<MigrationInfo> ListMigrations();

    /// <summary>
    /// Validates that all migrations can be applied without actually running them.
    /// </summary>
    /// <returns>True if validation passes.</returns>
    bool ValidateMigrations();
}

/// <summary>
/// Information about a migration.
/// </summary>
public record MigrationInfo(
    long Version,
    string Description,
    bool IsApplied,
    DateTime? AppliedOn);

/// <summary>
/// Wrapper around FluentMigrator's migration runner.
/// </summary>
public class MigrationRunnerWrapper : IMigrationRunner
{
    private readonly FluentMigrator.Runner.IMigrationRunner _runner;
    private readonly IVersionLoader _versionLoader;
    private readonly ILogger<MigrationRunnerWrapper> _logger;
    private readonly string _connectionString;

    public MigrationRunnerWrapper(
        IOptions<DatabaseOptions> options,
        ILogger<MigrationRunnerWrapper> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _connectionString = options.Value.ConnectionString;

        // LOGIC: Build a scoped FluentMigrator runner with the connection string
        var serviceScope = serviceProvider.CreateScope();

        // Configure the connection string at runtime
        var runnerBuilder = serviceScope.ServiceProvider.GetRequiredService<IMigrationRunnerBuilder>();

        _runner = CreateRunner(options.Value.ConnectionString);
        _versionLoader = serviceScope.ServiceProvider.GetRequiredService<IVersionLoader>();
    }

    private FluentMigrator.Runner.IMigrationRunner CreateRunner(string connectionString)
    {
        var services = new ServiceCollection()
            .AddFluentMigratorCore()
            .ConfigureRunner(rb =>
            {
                rb.AddPostgres()
                    .WithGlobalConnectionString(connectionString)
                    .ScanIn(typeof(MigrationServices).Assembly).For.Migrations()
                    .WithVersionTable(new VersionTableMetaData());
            })
            .AddLogging(lb => lb.AddFluentMigratorConsole())
            .BuildServiceProvider();

        return services.GetRequiredService<FluentMigrator.Runner.IMigrationRunner>();
    }

    public void MigrateUp()
    {
        _logger.LogInformation("Starting database migration (all pending)");

        try
        {
            _runner.MigrateUp();
            _logger.LogInformation("Database migration completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database migration failed");
            throw;
        }
    }

    public void MigrateUp(long version)
    {
        _logger.LogInformation("Starting database migration to version {Version}", version);

        try
        {
            _runner.MigrateUp(version);
            _logger.LogInformation("Database migration to version {Version} completed", version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database migration to version {Version} failed", version);
            throw;
        }
    }

    public void MigrateDown()
    {
        _logger.LogWarning("Rolling back last database migration");

        try
        {
            _runner.MigrateDown(0);
            _logger.LogInformation("Database migration rollback completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database migration rollback failed");
            throw;
        }
    }

    public void MigrateDown(long version)
    {
        _logger.LogWarning("Rolling back database to version {Version}", version);

        try
        {
            _runner.MigrateDown(version);
            _logger.LogInformation("Database rollback to version {Version} completed", version);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database rollback to version {Version} failed", version);
            throw;
        }
    }

    public IEnumerable<MigrationInfo> ListMigrations()
    {
        _versionLoader.LoadVersionInfo();
        var appliedVersions = _versionLoader.VersionInfo.AppliedMigrations()
            .ToDictionary(v => v, _ => true);

        var assembly = typeof(MigrationServices).Assembly;
        var migrations = assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Migration)) && !t.IsAbstract)
            .Select(t =>
            {
                var attr = t.GetCustomAttributes(typeof(MigrationAttribute), false)
                    .OfType<MigrationAttribute>()
                    .FirstOrDefault();

                if (attr is null) return null;

                return new MigrationInfo(
                    attr.Version,
                    attr.Description ?? t.Name,
                    appliedVersions.ContainsKey(attr.Version),
                    null); // AppliedOn would require querying the version table
            })
            .Where(m => m is not null)
            .Cast<MigrationInfo>()
            .OrderBy(m => m.Version);

        return migrations;
    }

    public bool ValidateMigrations()
    {
        try
        {
            _runner.ValidateVersionOrder();
            _logger.LogInformation("Migration validation passed");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration validation failed");
            return false;
        }
    }
}
```

---

### Task 1.5: Implement CLI Integration

**File:** `src/Lexichord.Host/Commands/MigrationCommand.cs`

```csharp
using Microsoft.Extensions.Logging;
using Lexichord.Infrastructure.Migrations;

namespace Lexichord.Host.Commands;

/// <summary>
/// Handles database migration CLI commands.
/// </summary>
/// <remarks>
/// LOGIC: Migration commands are handled before the main application starts.
/// This ensures the database schema is ready before any data access occurs.
///
/// Commands:
/// --migrate         Run all pending migrations
/// --migrate:up:N    Run migrations up to version N
/// --migrate:down    Rollback the last migration
/// --migrate:down:N  Rollback to version N
/// --migrate:list    List all migrations and their status
/// --migrate:validate Validate migrations without executing
/// </remarks>
public static class MigrationCommand
{
    /// <summary>
    /// Processes migration command-line arguments.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <param name="migrationRunner">The migration runner.</param>
    /// <param name="logger">Logger instance.</param>
    /// <returns>True if a migration command was processed (app should exit); false to continue.</returns>
    public static bool ProcessMigrationArgs(
        string[] args,
        IMigrationRunner migrationRunner,
        ILogger logger)
    {
        foreach (var arg in args)
        {
            if (arg.Equals("--migrate", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation("Executing: Migrate all pending");
                migrationRunner.MigrateUp();
                return true;
            }

            if (arg.StartsWith("--migrate:up:", StringComparison.OrdinalIgnoreCase))
            {
                var versionStr = arg["--migrate:up:".Length..];
                if (long.TryParse(versionStr, out var version))
                {
                    logger.LogInformation("Executing: Migrate up to version {Version}", version);
                    migrationRunner.MigrateUp(version);
                    return true;
                }

                logger.LogError("Invalid version: {VersionStr}", versionStr);
                return true;
            }

            if (arg.Equals("--migrate:down", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("Executing: Rollback last migration");
                migrationRunner.MigrateDown();
                return true;
            }

            if (arg.StartsWith("--migrate:down:", StringComparison.OrdinalIgnoreCase))
            {
                var versionStr = arg["--migrate:down:".Length..];
                if (long.TryParse(versionStr, out var version))
                {
                    logger.LogWarning("Executing: Rollback to version {Version}", version);
                    migrationRunner.MigrateDown(version);
                    return true;
                }

                logger.LogError("Invalid version: {VersionStr}", versionStr);
                return true;
            }

            if (arg.Equals("--migrate:list", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation("Executing: List migrations");
                var migrations = migrationRunner.ListMigrations().ToList();

                Console.WriteLine();
                Console.WriteLine("=== Database Migrations ===");
                Console.WriteLine();
                Console.WriteLine($"{"Version",-10} {"Status",-12} {"Description"}");
                Console.WriteLine(new string('-', 60));

                foreach (var m in migrations)
                {
                    var status = m.IsApplied ? "Applied" : "Pending";
                    Console.WriteLine($"{m.Version,-10} {status,-12} {m.Description}");
                }

                Console.WriteLine();
                Console.WriteLine($"Total: {migrations.Count} migrations ({migrations.Count(m => m.IsApplied)} applied, {migrations.Count(m => !m.IsApplied)} pending)");
                Console.WriteLine();

                return true;
            }

            if (arg.Equals("--migrate:validate", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation("Executing: Validate migrations");
                var valid = migrationRunner.ValidateMigrations();

                Console.WriteLine();
                Console.WriteLine(valid
                    ? "Migration validation PASSED"
                    : "Migration validation FAILED");
                Console.WriteLine();

                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Prints migration help information.
    /// </summary>
    public static void PrintHelp()
    {
        Console.WriteLine(@"
Database Migration Commands:
  --migrate             Run all pending migrations
  --migrate:up:N        Run migrations up to version N
  --migrate:down        Rollback the last migration
  --migrate:down:N      Rollback to version N
  --migrate:list        List all migrations and their status
  --migrate:validate    Validate migrations without executing

Examples:
  dotnet run -- --migrate              # Run all pending
  dotnet run -- --migrate:up:5         # Run up to version 5
  dotnet run -- --migrate:down         # Rollback last
  dotnet run -- --migrate:list         # Show status
");
    }
}
```

---

### Task 1.6: Integrate with Application Startup

**File:** `src/Lexichord.Host/Program.cs` (Add to existing)

```csharp
// In Main method, after building services but before starting UI:

// Check for migration commands
var migrationRunner = services.GetService<IMigrationRunner>();
if (migrationRunner is not null)
{
    if (MigrationCommand.ProcessMigrationArgs(args, migrationRunner, logger))
    {
        Log.Information("Migration command completed. Exiting.");
        return 0;
    }

    // Auto-migrate on startup if configured
    var dbOptions = services.GetRequiredService<IOptions<DatabaseOptions>>().Value;
    if (dbOptions.AutoMigrateOnStartup)
    {
        Log.Information("Auto-migrating database on startup");
        migrationRunner.MigrateUp();
    }
}
```

---

## 4. Decision Tree: Migration Execution

```text
START: "When should migrations run?"
|
+-- Is this a CLI migration command? (--migrate, --migrate:*)
|   +-- YES -> Execute migration command and EXIT
|   |         (Don't start the application)
|   |
|   +-- NO -> Continue startup
|
+-- Is AutoMigrateOnStartup enabled in configuration?
|   +-- YES -> Run MigrateUp() before creating MainWindow
|   |   |
|   |   +-- Migration succeeded?
|   |   |   +-- YES -> Continue startup
|   |   |   +-- NO -> Log error and EXIT with code 1
|   |   |
|   +-- NO -> Skip auto-migration
|
+-- Application starts normally

---

MIGRATION COMMAND FLOW:

--migrate:
|-- Scan assembly for Migration classes
|-- Load VersionInfo table (or create if not exists)
|-- For each migration NOT in VersionInfo (ascending order):
|   |-- Begin transaction
|   |-- Execute Up() method
|   |-- Insert version into VersionInfo
|   |-- Commit transaction
|   +-- (Rollback on any error)
+-- Exit with success/failure code

--migrate:down:
|-- Load VersionInfo table
|-- Get highest applied version
|-- Begin transaction
|-- Execute Down() method
|-- Remove version from VersionInfo
|-- Commit transaction
+-- Exit with success/failure code
```

---

## 5. Unit Testing Requirements

### 5.1 Test: Migration Conventions

```csharp
[TestFixture]
[Category("Unit")]
public class MigrationConventionsTests
{
    [Test]
    public void IndexName_FormatsCorrectly()
    {
        // Act
        var result = LexichordMigration.IndexName("Users", "Email");

        // Assert
        Assert.That(result, Is.EqualTo("IX_Users_Email"));
    }

    [Test]
    public void IndexName_MultipleColumns_FormatsCorrectly()
    {
        // Act
        var result = LexichordMigration.IndexName("Documents", "UserId", "CreatedAt");

        // Assert
        Assert.That(result, Is.EqualTo("IX_Documents_UserId_CreatedAt"));
    }

    [Test]
    public void ForeignKeyName_FormatsCorrectly()
    {
        // Act
        var result = LexichordMigration.ForeignKeyName("Documents", "Users");

        // Assert
        Assert.That(result, Is.EqualTo("FK_Documents_Users"));
    }
}
```

### 5.2 Test: Migration Discovery

```csharp
[TestFixture]
[Category("Unit")]
public class MigrationDiscoveryTests
{
    [Test]
    public void Assembly_ContainsMigrations()
    {
        // Arrange
        var assembly = typeof(Migration_001_InitSystem).Assembly;

        // Act
        var migrations = assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Migration)) && !t.IsAbstract)
            .ToList();

        // Assert
        Assert.That(migrations, Is.Not.Empty);
        Assert.That(migrations.Any(t => t.Name.Contains("001")), Is.True);
    }

    [Test]
    public void Migration_001_HasCorrectAttribute()
    {
        // Arrange
        var migrationType = typeof(Migration_001_InitSystem);

        // Act
        var attr = migrationType.GetCustomAttributes(typeof(MigrationAttribute), false)
            .OfType<MigrationAttribute>()
            .FirstOrDefault();

        // Assert
        Assert.That(attr, Is.Not.Null);
        Assert.That(attr!.Version, Is.EqualTo(1));
    }

    [Test]
    public void AllMigrations_HaveUniqueVersions()
    {
        // Arrange
        var assembly = typeof(Migration_001_InitSystem).Assembly;

        // Act
        var versions = assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Migration)) && !t.IsAbstract)
            .Select(t => t.GetCustomAttributes(typeof(MigrationAttribute), false)
                .OfType<MigrationAttribute>()
                .FirstOrDefault()?.Version)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .ToList();

        // Assert
        Assert.That(versions.Distinct().Count(), Is.EqualTo(versions.Count),
            "All migrations must have unique version numbers");
    }
}
```

### 5.3 Integration Test: Migration Execution

```csharp
[TestFixture]
[Category("Integration")]
[Explicit("Requires running PostgreSQL")]
public class MigrationIntegrationTests
{
    private string _connectionString = null!;
    private IMigrationRunner _runner = null!;

    [SetUp]
    public void SetUp()
    {
        // Use a test database that can be reset
        _connectionString = Environment.GetEnvironmentVariable("LEXICHORD_TEST_DB")
            ?? "Host=localhost;Port=5432;Database=lexichord_migration_test;Username=lexichord;Password=lexichord_dev";

        // Create fresh database for each test
        DropAndCreateDatabase();

        // Create migration runner
        var services = new ServiceCollection();
        services.Configure<DatabaseOptions>(o => o.ConnectionString = _connectionString);
        services.AddMigrationServices();
        services.AddLogging(b => b.AddConsole());

        var provider = services.BuildServiceProvider();
        _runner = provider.GetRequiredService<IMigrationRunner>();
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up test database
        DropDatabase();
    }

    [Test]
    public void MigrateUp_CreatesUsersTable()
    {
        // Act
        _runner.MigrateUp();

        // Assert
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        var tableExists = connection.ExecuteScalar<bool>(@"
            SELECT EXISTS (
                SELECT FROM information_schema.tables
                WHERE table_name = 'Users'
            )");

        Assert.That(tableExists, Is.True);
    }

    [Test]
    public void MigrateUp_CreatesSystemSettingsTable()
    {
        // Act
        _runner.MigrateUp();

        // Assert
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        var tableExists = connection.ExecuteScalar<bool>(@"
            SELECT EXISTS (
                SELECT FROM information_schema.tables
                WHERE table_name = 'SystemSettings'
            )");

        Assert.That(tableExists, Is.True);
    }

    [Test]
    public void MigrateUp_SeedsSystemSettings()
    {
        // Act
        _runner.MigrateUp();

        // Assert
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        var count = connection.ExecuteScalar<int>(@"
            SELECT COUNT(*) FROM ""SystemSettings""
            WHERE ""Key"" IN ('app:initialized', 'app:version', 'system:maintenance_mode')");

        Assert.That(count, Is.EqualTo(3));
    }

    [Test]
    public void MigrateDown_DropsTablesInOrder()
    {
        // Arrange - First migrate up
        _runner.MigrateUp();

        // Act - Rollback
        _runner.MigrateDown(0);

        // Assert
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        var tablesExist = connection.ExecuteScalar<bool>(@"
            SELECT EXISTS (
                SELECT FROM information_schema.tables
                WHERE table_name IN ('Users', 'SystemSettings')
            )");

        Assert.That(tablesExist, Is.False);
    }

    [Test]
    public void MigrateUp_CreatesVersionTable()
    {
        // Act
        _runner.MigrateUp();

        // Assert
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        var tableExists = connection.ExecuteScalar<bool>(@"
            SELECT EXISTS (
                SELECT FROM information_schema.tables
                WHERE table_name = 'SchemaVersions'
            )");

        Assert.That(tableExists, Is.True);
    }

    [Test]
    public void MigrateUp_RecordsVersionInVersionTable()
    {
        // Act
        _runner.MigrateUp();

        // Assert
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        var version = connection.ExecuteScalar<long>(@"
            SELECT ""Version"" FROM ""SchemaVersions""
            ORDER BY ""Version"" DESC LIMIT 1");

        Assert.That(version, Is.EqualTo(1));
    }

    private void DropAndCreateDatabase()
    {
        var builder = new NpgsqlConnectionStringBuilder(_connectionString);
        var dbName = builder.Database;
        builder.Database = "postgres";

        using var connection = new NpgsqlConnection(builder.ConnectionString);
        connection.Open();

        // Drop if exists
        connection.Execute($@"
            DROP DATABASE IF EXISTS ""{dbName}"" WITH (FORCE)");

        // Create fresh
        connection.Execute($@"
            CREATE DATABASE ""{dbName}""");
    }

    private void DropDatabase()
    {
        var builder = new NpgsqlConnectionStringBuilder(_connectionString);
        var dbName = builder.Database;
        builder.Database = "postgres";

        using var connection = new NpgsqlConnection(builder.ConnectionString);
        connection.Open();

        connection.Execute($@"
            DROP DATABASE IF EXISTS ""{dbName}"" WITH (FORCE)");
    }
}
```

---

## 6. Observability & Logging

### 6.1 Log Events

| Level       | Context                 | Message Template                                                      |
| :---------- | :---------------------- | :-------------------------------------------------------------------- |
| Information | MigrationRunnerWrapper  | `Starting database migration (all pending)`                           |
| Information | MigrationRunnerWrapper  | `Database migration completed successfully`                           |
| Information | MigrationRunnerWrapper  | `Starting database migration to version {Version}`                    |
| Information | MigrationRunnerWrapper  | `Database migration to version {Version} completed`                   |
| Warning     | MigrationRunnerWrapper  | `Rolling back last database migration`                                |
| Warning     | MigrationRunnerWrapper  | `Rolling back database to version {Version}`                          |
| Information | MigrationRunnerWrapper  | `Database migration rollback completed`                               |
| Error       | MigrationRunnerWrapper  | `Database migration failed`                                           |
| Error       | MigrationRunnerWrapper  | `Database migration to version {Version} failed`                      |
| Error       | MigrationRunnerWrapper  | `Database migration rollback failed`                                  |
| Information | MigrationRunnerWrapper  | `Migration validation passed`                                         |
| Error       | MigrationRunnerWrapper  | `Migration validation failed`                                         |
| Information | FluentMigrator          | `[{timestamp}] [Migration_{Version}] Executing migration: {Description}` |
| Information | FluentMigrator          | `[{timestamp}] [Migration_{Version}] Migration completed successfully` |

---

## 7. Security & Safety

### 7.1 Migration Safety

> [!WARNING]
> Migrations modify database schema. Always backup before running in production.

**Best Practices:**

1. **Test migrations locally first:**
   ```bash
   ./scripts/db-reset.sh
   ./scripts/db-start.sh
   dotnet run -- --migrate
   ```

2. **Review Down() methods:**
   - Ensure all Up() changes have corresponding Down() logic
   - Test rollback path: `--migrate` then `--migrate:down`

3. **Production deployment:**
   - Run migrations before deploying new application version
   - Have rollback plan ready
   - Consider blue-green deployment

### 7.2 Avoid Breaking Changes

```csharp
// DANGEROUS: Dropping columns loses data
Delete.Column("OldColumn").FromTable("Users"); // NO!

// SAFE: Mark as deprecated first, then drop in later migration
// Migration N: Add new column
// Migration N+1: Migrate data
// Migration N+2: Drop old column (after verifying no code uses it)
```

---

## 8. Definition of Done

- [ ] FluentMigrator packages installed
- [ ] `MigrationConventions` class with helper methods created
- [ ] `LexichordMigration` base class created
- [ ] `Migration_001_InitSystem` creates `Users` table with all columns
- [ ] `Migration_001_InitSystem` creates `SystemSettings` table
- [ ] `Migration_001_InitSystem` creates indexes
- [ ] `Migration_001_InitSystem` creates update triggers
- [ ] `Migration_001_InitSystem` seeds initial system settings
- [ ] `Migration_001_InitSystem` implements `Down()` for rollback
- [ ] `MigrationServices` configures FluentMigrator in DI
- [ ] `IMigrationRunner` interface with MigrateUp/Down methods
- [ ] `MigrationRunnerWrapper` implementation complete
- [ ] `MigrationCommand` CLI handler implemented
- [ ] `--migrate` flag executes pending migrations
- [ ] `--migrate:down` flag rolls back last migration
- [ ] `--migrate:list` shows migration status
- [ ] `--migrate:validate` validates without executing
- [ ] Unit tests for conventions passing
- [ ] Unit tests for migration discovery passing
- [ ] Integration tests for migration execution passing

---

## 9. Verification Commands

```bash
# 1. Start test database
./scripts/db-start.sh

# 2. List available migrations
dotnet run --project src/Lexichord.Host -- --migrate:list

# 3. Validate migrations
dotnet run --project src/Lexichord.Host -- --migrate:validate

# 4. Run all migrations
dotnet run --project src/Lexichord.Host -- --migrate

# 5. Verify tables exist
docker exec -it lexichord-postgres psql -U lexichord -c "\dt"

# 6. Verify Users table structure
docker exec -it lexichord-postgres psql -U lexichord -c "\d \"Users\""

# 7. Verify SystemSettings seeded
docker exec -it lexichord-postgres psql -U lexichord -c "SELECT * FROM \"SystemSettings\""

# 8. Verify version table
docker exec -it lexichord-postgres psql -U lexichord -c "SELECT * FROM \"SchemaVersions\""

# 9. Test rollback
dotnet run --project src/Lexichord.Host -- --migrate:down

# 10. Verify tables dropped
docker exec -it lexichord-postgres psql -U lexichord -c "\dt"

# 11. Re-apply migrations
dotnet run --project src/Lexichord.Host -- --migrate

# 12. Run integration tests
dotnet test --filter "Category=Integration&FullyQualifiedName~Migration"
```

---

## 10. Future Migrations Template

When adding new migrations, follow this template:

```csharp
using FluentMigrator;

namespace Lexichord.Infrastructure.Migrations;

/// <summary>
/// [Brief description of what this migration does]
/// </summary>
/// <remarks>
/// LOGIC: [Explain the purpose and any important details]
///
/// Changes:
/// - [List of tables/columns/indexes added]
/// - [List of data modifications]
///
/// Rollback:
/// - [What Down() does]
/// </remarks>
[Migration(2, "Brief description here")]
public class Migration_002_DescriptiveName : LexichordMigration
{
    public override void Up()
    {
        // Implementation
    }

    public override void Down()
    {
        // Reverse of Up()
    }
}
```
