using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Entities;
using Lexichord.Infrastructure.Migrations;
using Lexichord.Modules.Style.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Lexichord.Tests.Integration.Data;

/// <summary>
/// Integration tests for TerminologyRepository with real PostgreSQL.
/// </summary>
/// <remarks>
/// Prerequisites:
/// 1. Run ./scripts/db-start.sh to start the PostgreSQL container
/// 2. Ensure the connection string matches docker-compose settings
/// 
/// These tests are marked with Skip by default. Remove Skip attribute to run.
/// </remarks>
public class TerminologyRepositoryIntegrationTests : IDisposable
{
    private const string TestConnectionString =
        "Host=localhost;Port=5432;Database=lexichord_terminology_test;Username=lexichord;Password=lexichord_dev_password";

    private readonly MigrationRunnerWrapper _runner;
    private readonly TestDbConnectionFactory _connectionFactory;
    private readonly MemoryCache _cache;
    private readonly TerminologyRepository _repository;

    private static readonly Guid TestStyleSheetId = Guid.Parse("12345678-1234-1234-1234-123456789012");

    public TerminologyRepositoryIntegrationTests()
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
    }

    public void Dispose()
    {
        _cache.Dispose();
        _connectionFactory.Dispose();
        GC.SuppressFinalize(this);
    }

    #region CRUD Tests

    [Fact(Skip = "Requires running PostgreSQL container. Run ./scripts/db-start.sh first.")]
    public async Task InsertAsync_CreatesNewTerm()
    {
        // Arrange
        EnsureTestDatabaseExists();
        _runner.MigrateUp();

        var term = new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = TestStyleSheetId,
            Term = "leverage",
            Replacement = "use",
            Category = "Jargon",
            Severity = "Warning",
            IsActive = true
        };

        // Act
        var result = await _repository.InsertAsync(term);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(term.Id);
        result.Term.Should().Be("leverage");
        result.Replacement.Should().Be("use");
    }

    [Fact(Skip = "Requires running PostgreSQL container. Run ./scripts/db-start.sh first.")]
    public async Task GetByIdAsync_ReturnsInsertedTerm()
    {
        // Arrange
        EnsureTestDatabaseExists();
        _runner.MigrateUp();

        var id = Guid.NewGuid();
        var term = new StyleTerm
        {
            Id = id,
            StyleSheetId = TestStyleSheetId,
            Term = "synergy",
            Category = "Jargon"
        };
        await _repository.InsertAsync(term);

        // Act
        var result = await _repository.GetByIdAsync(id);

        // Assert
        result.Should().NotBeNull();
        result!.Term.Should().Be("synergy");
    }

    [Fact(Skip = "Requires running PostgreSQL container. Run ./scripts/db-start.sh first.")]
    public async Task UpdateAsync_ModifiesTerm()
    {
        // Arrange
        EnsureTestDatabaseExists();
        _runner.MigrateUp();

        var id = Guid.NewGuid();
        var term = new StyleTerm
        {
            Id = id,
            StyleSheetId = TestStyleSheetId,
            Term = "original",
            Category = "General"
        };
        await _repository.InsertAsync(term);

        // Act
        var updated = term with { Term = "modified", UpdatedAt = DateTimeOffset.UtcNow };
        var result = await _repository.UpdateAsync(updated);

        // Assert
        result.Should().BeTrue();
        var fetched = await _repository.GetByIdAsync(id);
        fetched!.Term.Should().Be("modified");
    }

    [Fact(Skip = "Requires running PostgreSQL container. Run ./scripts/db-start.sh first.")]
    public async Task DeleteAsync_RemovesTerm()
    {
        // Arrange
        EnsureTestDatabaseExists();
        _runner.MigrateUp();

        var id = Guid.NewGuid();
        var term = new StyleTerm
        {
            Id = id,
            StyleSheetId = TestStyleSheetId,
            Term = "toDelete",
            Category = "General"
        };
        await _repository.InsertAsync(term);

        // Act
        var result = await _repository.DeleteAsync(id);

        // Assert
        result.Should().BeTrue();
        var fetched = await _repository.GetByIdAsync(id);
        fetched.Should().BeNull();
    }

    #endregion

    #region Active Terms Cache Tests

    [Fact(Skip = "Requires running PostgreSQL container. Run ./scripts/db-start.sh first.")]
    public async Task GetAllActiveTermsAsync_ReturnsOnlyActiveTerms()
    {
        // Arrange
        EnsureTestDatabaseExists();
        _runner.MigrateUp();

        var activeTerm = new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = TestStyleSheetId,
            Term = "active",
            IsActive = true,
            Category = "General"
        };
        var inactiveTerm = new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = TestStyleSheetId,
            Term = "inactive",
            IsActive = false,
            Category = "General"
        };
        await _repository.InsertAsync(activeTerm);
        await _repository.InsertAsync(inactiveTerm);

        // Clear cache to force fresh load
        await _repository.InvalidateCacheAsync();

        // Act
        var result = await _repository.GetAllActiveTermsAsync();

        // Assert
        result.Should().Contain(t => t.Term == "active");
        result.Should().NotContain(t => t.Term == "inactive");
    }

    #endregion

    #region Category Filter Tests

    [Fact(Skip = "Requires running PostgreSQL container. Run ./scripts/db-start.sh first.")]
    public async Task GetByCategoryAsync_FiltersCorrectly()
    {
        // Arrange
        EnsureTestDatabaseExists();
        _runner.MigrateUp();

        var jargonTerm = new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = TestStyleSheetId,
            Term = "paradigm",
            Category = "Jargon"
        };
        var brandTerm = new StyleTerm
        {
            Id = Guid.NewGuid(),
            StyleSheetId = TestStyleSheetId,
            Term = "Lexichord",
            Category = "Brand"
        };
        await _repository.InsertAsync(jargonTerm);
        await _repository.InsertAsync(brandTerm);

        // Act
        var jargonResults = await _repository.GetByCategoryAsync("Jargon");
        var brandResults = await _repository.GetByCategoryAsync("Brand");

        // Assert
        jargonResults.Should().Contain(t => t.Term == "paradigm");
        jargonResults.Should().NotContain(t => t.Term == "Lexichord");
        brandResults.Should().Contain(t => t.Term == "Lexichord");
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
