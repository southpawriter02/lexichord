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
/// --migrate:down    Rollback all migrations (to version 0)
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
  --migrate:down        Rollback all migrations (to version 0)
  --migrate:down:N      Rollback to version N
  --migrate:list        List all migrations and their status
  --migrate:validate    Validate migrations without executing

Examples:
  dotnet run -- --migrate              # Run all pending
  dotnet run -- --migrate:up:5         # Run up to version 5
  dotnet run -- --migrate:down         # Rollback all
  dotnet run -- --migrate:list         # Show status
");
    }
}
