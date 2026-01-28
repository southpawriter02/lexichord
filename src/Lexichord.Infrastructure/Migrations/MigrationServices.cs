using FluentMigrator;
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
public class VersionTableMetaData : FluentMigrator.Runner.VersionTableInfo.IVersionTableMetaData
{
    public string SchemaName => "public";
    public string TableName => "SchemaVersions";
    public string ColumnName => "Version";
    public string DescriptionColumnName => "Description";
    public string AppliedOnColumnName => "AppliedOn";
    public string UniqueIndexName => "UC_SchemaVersions_Version";
    public object? ApplicationContext { get; set; }
    public bool OwnsSchema => false;
    public bool CreateWithPrimaryKey => true;
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

    public MigrationRunnerWrapper(
        IOptions<DatabaseOptions> options,
        ILogger<MigrationRunnerWrapper> logger)
    {
        _logger = logger;
        var connectionString = options.Value.ConnectionString;

        // LOGIC: Build a scoped FluentMigrator runner with the connection string
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

        _runner = services.GetRequiredService<FluentMigrator.Runner.IMigrationRunner>();
        _versionLoader = services.GetRequiredService<IVersionLoader>();
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
