using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Entities;
using Lexichord.Infrastructure.Migrations;
using Lexichord.Modules.Style.Data;
using Lexichord.Modules.Style.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Lexichord.Tests.Integration.Data;

/// <summary>
/// Integration tests for TerminologySeeder with real PostgreSQL.
/// </summary>
/// <remarks>
/// Prerequisites:
/// 1. Run ./scripts/db-start.sh to start the PostgreSQL container
/// 2. Ensure the connection string matches docker-compose settings
/// 
/// These tests are marked with Skip by default. Remove Skip attribute to run.
/// </remarks>
public class TerminologySeederIntegrationTests : IDisposable
{
    private const string TestConnectionString =
        "Host=localhost;Port=5432;Database=lexichord_seeder_test;Username=lexichord;Password=lexichord_dev_password";

    private readonly MigrationRunnerWrapper _runner;
    private readonly TestDbConnectionFactory _connectionFactory;
    private readonly MemoryCache _cache;
    private readonly TerminologyRepository _repository;
    private readonly TerminologySeeder _seeder;

    public TerminologySeederIntegrationTests()
    {
        var options = Options.Create(new DatabaseOptions
        {
            ConnectionString = TestConnectionString
        });
        var migrationLogger = NullLogger<MigrationRunnerWrapper>.Instance;
        _runner = new MigrationRunnerWrapper(options, migrationLogger);

        _connectionFactory = new TestDbConnectionFactory(TestConnectionString);
        _cache = new MemoryCache(new MemoryCacheOptions());
        var cacheOptions = Options.Create(new TerminologyCacheOptions());
        var repoLogger = NullLogger<TerminologyRepository>.Instance;

        _repository = new TerminologyRepository(_connectionFactory, _cache, cacheOptions, repoLogger);
        _seeder = new TerminologySeeder(_repository, NullLogger<TerminologySeeder>.Instance);
    }

    public void Dispose()
    {
        _cache.Dispose();
        _connectionFactory.Dispose();
        GC.SuppressFinalize(this);
    }

    #region Seeding Tests

    [Fact(Skip = "Requires running PostgreSQL container. Run ./scripts/db-start.sh first.")]
    public async Task SeedIfEmptyAsync_EmptyDatabase_SeedsAllDefaultTerms()
    {
        // Arrange
        EnsureTestDatabaseExists();
        _runner.MigrateUp();

        // Act
        var result = await _seeder.SeedIfEmptyAsync();

        // Assert
        result.WasEmpty.Should().BeTrue();
        result.TermsSeeded.Should().BeGreaterThan(40);

        var count = await _repository.CountAsync();
        count.Should().Be(result.TermsSeeded);
    }

    [Fact(Skip = "Requires running PostgreSQL container. Run ./scripts/db-start.sh first.")]
    public async Task SeedIfEmptyAsync_Idempotent_SecondCallDoesNothing()
    {
        // Arrange
        EnsureTestDatabaseExists();
        _runner.MigrateUp();

        // Act
        var first = await _seeder.SeedIfEmptyAsync();
        var second = await _seeder.SeedIfEmptyAsync();

        // Assert
        first.WasEmpty.Should().BeTrue();
        first.TermsSeeded.Should().BeGreaterThan(0);

        second.WasEmpty.Should().BeFalse();
        second.TermsSeeded.Should().Be(0);

        // Verify count hasn't changed
        var count = await _repository.CountAsync();
        count.Should().Be(first.TermsSeeded);
    }

    [Fact(Skip = "Requires running PostgreSQL container. Run ./scripts/db-start.sh first.")]
    public async Task SeededTerms_AreQueryableByCategory()
    {
        // Arrange
        EnsureTestDatabaseExists();
        _runner.MigrateUp();
        await _seeder.SeedIfEmptyAsync();

        // Act
        var terminologyTerms = await _repository.GetByCategoryAsync("Terminology");
        var grammarTerms = await _repository.GetByCategoryAsync("Grammar");

        // Assert
        terminologyTerms.Should().NotBeEmpty();
        grammarTerms.Should().NotBeEmpty();

        terminologyTerms.Should().AllSatisfy(t => t.Category.Should().Be("Terminology"));
        grammarTerms.Should().AllSatisfy(t => t.Category.Should().Be("Grammar"));
    }

    [Fact(Skip = "Requires running PostgreSQL container. Run ./scripts/db-start.sh first.")]
    public async Task SeededTerms_AreSearchable()
    {
        // Arrange
        EnsureTestDatabaseExists();
        _runner.MigrateUp();
        await _seeder.SeedIfEmptyAsync();

        // Act - Search for a known seed term
        var results = await _repository.SearchAsync("leverage");

        // Assert
        results.Should().NotBeEmpty();
        results.Should().Contain(t => t.Term == "leverage");
    }

    [Fact(Skip = "Requires running PostgreSQL container. Run ./scripts/db-start.sh first.")]
    public async Task ReseedAsync_ClearExisting_RemovesAndReseeds()
    {
        // Arrange
        EnsureTestDatabaseExists();
        _runner.MigrateUp();

        // Seed once
        await _seeder.SeedIfEmptyAsync();
        var initialCount = await _repository.CountAsync();

        // Add a custom term
        var customTerm = new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = Guid.Parse("00000000-0000-0000-0000-000000000002"),
            Term = "custom-term",
            Category = "Custom"
        };
        await _repository.InsertAsync(customTerm);
        var afterCustomCount = await _repository.CountAsync();
        afterCustomCount.Should().Be(initialCount + 1);

        // Act - Reseed with clear
        var result = await _seeder.ReseedAsync(clearExisting: true);

        // Assert
        result.TermsSeeded.Should().Be(_seeder.GetDefaultTerms().Count);

        // Custom term should be gone
        var customAfterReseed = await _repository.GetByIdAsync(customTerm.Id);
        customAfterReseed.Should().BeNull();
    }

    #endregion

    #region Helper Methods

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

    /// <summary>
    /// Simple connection factory for testing.
    /// </summary>
    private class TestDbConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;
        private readonly NpgsqlDataSource _dataSource;

        public TestDbConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
            var builder = new NpgsqlDataSourceBuilder(connectionString);
            _dataSource = builder.Build();
        }

        public NpgsqlDataSource DataSource => _dataSource;
        public bool IsHealthy => true;

        public async Task<NpgsqlConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
        {
            var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            return connection;
        }

        public Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(true);

        public void Dispose() => _dataSource.Dispose();
    }

    #endregion
}
