using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using Lexichord.Abstractions.Contracts;
using Lexichord.Infrastructure.Migrations;

namespace Lexichord.Tests.Integration.Infrastructure;

/// <summary>
/// Integration tests for database migrations.
/// </summary>
/// <remarks>
/// Prerequisites:
/// 1. Run ./scripts/db-start.sh to start the PostgreSQL container
/// 2. Ensure the connection string in test options matches docker-compose settings
/// 
/// These tests are marked with Skip by default. Remove Skip attribute to run.
/// </remarks>
public class MigrationIntegrationTests : IDisposable
{
    private const string TestConnectionString = 
        "Host=localhost;Port=5432;Database=lexichord_migration_test;Username=lexichord;Password=lexichord_dev_password";

    private readonly MigrationRunnerWrapper _runner;

    public MigrationIntegrationTests()
    {
        var options = Options.Create(new DatabaseOptions 
        { 
            ConnectionString = TestConnectionString 
        });
        var logger = NullLogger<MigrationRunnerWrapper>.Instance;
        _runner = new MigrationRunnerWrapper(options, logger);
    }

    public void Dispose()
    {
        // Clean up test database after tests
        GC.SuppressFinalize(this);
    }

    [Fact(Skip = "Requires running PostgreSQL container. Run ./scripts/db-start.sh first.")]
    public void MigrationRunnerWrapper_CanBeCreated()
    {
        // Assert
        _runner.Should().NotBeNull();
    }

    [Fact(Skip = "Requires running PostgreSQL container. Run ./scripts/db-start.sh first.")]
    public void MigrateUp_CreatesUsersTable()
    {
        // Arrange
        EnsureTestDatabaseExists();

        // Act
        _runner.MigrateUp();

        // Assert
        using var connection = new NpgsqlConnection(TestConnectionString);
        connection.Open();

        using var cmd = new NpgsqlCommand(@"
            SELECT EXISTS (
                SELECT FROM information_schema.tables
                WHERE table_name = 'Users'
            )", connection);

        var tableExists = (bool)cmd.ExecuteScalar()!;
        tableExists.Should().BeTrue();
    }

    [Fact(Skip = "Requires running PostgreSQL container. Run ./scripts/db-start.sh first.")]
    public void MigrateUp_CreatesSystemSettingsTable()
    {
        // Arrange
        EnsureTestDatabaseExists();

        // Act
        _runner.MigrateUp();

        // Assert
        using var connection = new NpgsqlConnection(TestConnectionString);
        connection.Open();

        using var cmd = new NpgsqlCommand(@"
            SELECT EXISTS (
                SELECT FROM information_schema.tables
                WHERE table_name = 'SystemSettings'
            )", connection);

        var tableExists = (bool)cmd.ExecuteScalar()!;
        tableExists.Should().BeTrue();
    }

    [Fact(Skip = "Requires running PostgreSQL container. Run ./scripts/db-start.sh first.")]
    public void MigrateUp_SeedsSystemSettings()
    {
        // Arrange
        EnsureTestDatabaseExists();

        // Act
        _runner.MigrateUp();

        // Assert
        using var connection = new NpgsqlConnection(TestConnectionString);
        connection.Open();

        using var cmd = new NpgsqlCommand(@"
            SELECT COUNT(*) FROM ""SystemSettings""
            WHERE ""Key"" IN ('app:initialized', 'app:version', 'system:maintenance_mode')", connection);

        var count = (long)cmd.ExecuteScalar()!;
        count.Should().Be(3);
    }

    [Fact(Skip = "Requires running PostgreSQL container. Run ./scripts/db-start.sh first.")]
    public void MigrateUp_CreatesSchemaVersionsTable()
    {
        // Arrange
        EnsureTestDatabaseExists();

        // Act
        _runner.MigrateUp();

        // Assert
        using var connection = new NpgsqlConnection(TestConnectionString);
        connection.Open();

        using var cmd = new NpgsqlCommand(@"
            SELECT EXISTS (
                SELECT FROM information_schema.tables
                WHERE table_name = 'SchemaVersions'
            )", connection);

        var tableExists = (bool)cmd.ExecuteScalar()!;
        tableExists.Should().BeTrue();
    }

    [Fact(Skip = "Requires running PostgreSQL container. Run ./scripts/db-start.sh first.")]
    public void MigrateDown_DropsTablesInOrder()
    {
        // Arrange
        EnsureTestDatabaseExists();
        _runner.MigrateUp();

        // Act
        _runner.MigrateDown(0);

        // Assert
        using var connection = new NpgsqlConnection(TestConnectionString);
        connection.Open();

        using var cmd = new NpgsqlCommand(@"
            SELECT EXISTS (
                SELECT FROM information_schema.tables
                WHERE table_name IN ('Users', 'SystemSettings')
            )", connection);

        var tablesExist = (bool)cmd.ExecuteScalar()!;
        tablesExist.Should().BeFalse();
    }

    [Fact(Skip = "Requires running PostgreSQL container. Run ./scripts/db-start.sh first.")]
    public void ListMigrations_ReturnsMigrations()
    {
        // Arrange
        EnsureTestDatabaseExists();

        // Act
        var migrations = _runner.ListMigrations().ToList();

        // Assert
        migrations.Should().NotBeEmpty();
        migrations.Should().Contain(m => m.Version == 1);
    }

    [Fact(Skip = "Requires running PostgreSQL container. Run ./scripts/db-start.sh first.")]
    public void ValidateMigrations_ReturnsTrue()
    {
        // Arrange
        EnsureTestDatabaseExists();

        // Act
        var result = _runner.ValidateMigrations();

        // Assert
        result.Should().BeTrue();
    }

    private static void EnsureTestDatabaseExists()
    {
        var builder = new NpgsqlConnectionStringBuilder(TestConnectionString);
        var dbName = builder.Database;
        builder.Database = "postgres";

        using var connection = new NpgsqlConnection(builder.ConnectionString);
        connection.Open();

        // Drop if exists
        using (var cmd = new NpgsqlCommand($@"DROP DATABASE IF EXISTS ""{dbName}"" WITH (FORCE)", connection))
        {
            cmd.ExecuteNonQuery();
        }

        // Create fresh
        using (var cmd = new NpgsqlCommand($@"CREATE DATABASE ""{dbName}""", connection))
        {
            cmd.ExecuteNonQuery();
        }
    }
}
