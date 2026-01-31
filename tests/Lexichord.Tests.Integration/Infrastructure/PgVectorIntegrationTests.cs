using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Lexichord.Abstractions.Contracts;
using Lexichord.Infrastructure.Data;
using Npgsql;
using Pgvector;

namespace Lexichord.Tests.Integration.Infrastructure;

/// <summary>
/// Integration tests for pgvector extension availability and vector operations.
/// </summary>
/// <remarks>
/// v0.4.1a: Verifies that the pgvector extension is correctly installed and
/// operational in the PostgreSQL container.
/// 
/// Prerequisites:
/// 1. Run ./scripts/db-start.sh to start the PostgreSQL container
/// 2. Ensure the container is healthy (health check includes vector extension verification)
/// 
/// These tests are marked with Skip by default. Remove Skip attribute to run.
/// </remarks>
public class PgVectorIntegrationTests : IDisposable
{
    private readonly NpgsqlConnectionFactory _factory;
    private readonly ILogger<NpgsqlConnectionFactory> _logger;
    private readonly DatabaseOptions _dbOptions;

    public PgVectorIntegrationTests()
    {
        _logger = NullLogger<NpgsqlConnectionFactory>.Instance;

        _dbOptions = new DatabaseOptions
        {
            // Match docker-compose.yml settings
            ConnectionString = "Host=localhost;Port=5432;Database=lexichord;Username=lexichord;Password=lexichord_dev",
            MaxPoolSize = 10,
            MinPoolSize = 1,
            ConnectionTimeoutSeconds = 10,
            EnableMultiplexing = true
        };

        _factory = new NpgsqlConnectionFactory(Options.Create(_dbOptions), _logger);
    }

    [Fact(Skip = "Requires running PostgreSQL container with pgvector. Run ./scripts/db-start.sh first.")]
    public async Task PgVectorExtension_ShouldBeInstalled()
    {
        // Arrange
        await using var connection = await _factory.CreateConnectionAsync();

        // Act
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT extname FROM pg_extension WHERE extname = 'vector';";
        var result = await command.ExecuteScalarAsync();

        // Assert
        result.Should().Be("vector", "pgvector extension should be installed");
    }

    [Fact(Skip = "Requires running PostgreSQL container with pgvector. Run ./scripts/db-start.sh first.")]
    public async Task PgVectorExtension_ShouldReturnVersion()
    {
        // Arrange
        await using var connection = await _factory.CreateConnectionAsync();

        // Act
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT extversion FROM pg_extension WHERE extname = 'vector';";
        var result = await command.ExecuteScalarAsync();

        // Assert
        result.Should().NotBeNull("pgvector version should be available");
        result.Should().BeOfType<string>();
    }

    [Fact(Skip = "Requires running PostgreSQL container with pgvector. Run ./scripts/db-start.sh first.")]
    public async Task VectorType_ShouldSupportCreateTable()
    {
        // Arrange
        await using var connection = await _factory.CreateConnectionAsync();
        const string tableName = "_test_vector_table";

        try
        {
            // Act - Create a table with vector column
            await using var createCommand = connection.CreateCommand();
            createCommand.CommandText = $@"
                DROP TABLE IF EXISTS {tableName};
                CREATE TABLE {tableName} (
                    id SERIAL PRIMARY KEY,
                    embedding vector(1536)
                );";
            await createCommand.ExecuteNonQueryAsync();

            // Assert - Verify table was created
            await using var verifyCommand = connection.CreateCommand();
            verifyCommand.CommandText = $@"
                SELECT column_name, udt_name 
                FROM information_schema.columns 
                WHERE table_name = '{tableName}' AND column_name = 'embedding';";
            await using var reader = await verifyCommand.ExecuteReaderAsync();

            reader.HasRows.Should().BeTrue("vector column should exist");
            await reader.ReadAsync();
            reader.GetString(1).Should().Be("vector", "column type should be 'vector'");
        }
        finally
        {
            // Cleanup
            await using var dropCommand = connection.CreateCommand();
            dropCommand.CommandText = $"DROP TABLE IF EXISTS {tableName};";
            await dropCommand.ExecuteNonQueryAsync();
        }
    }

    [Fact(Skip = "Requires running PostgreSQL container with pgvector. Run ./scripts/db-start.sh first.")]
    public async Task VectorType_ShouldSupportInsertAndQuery()
    {
        // Arrange
        await using var connection = await _factory.CreateConnectionAsync();
        const string tableName = "_test_vector_ops";

        try
        {
            // Setup - Create table
            await using var setupCommand = connection.CreateCommand();
            setupCommand.CommandText = $@"
                DROP TABLE IF EXISTS {tableName};
                CREATE TABLE {tableName} (
                    id SERIAL PRIMARY KEY,
                    embedding vector(3)
                );";
            await setupCommand.ExecuteNonQueryAsync();

            // Act - Insert vectors using parameterized query
            await using var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = $"INSERT INTO {tableName} (embedding) VALUES (@v1), (@v2), (@v3)";
            
            var v1 = new Vector(new float[] { 1.0f, 2.0f, 3.0f });
            var v2 = new Vector(new float[] { 4.0f, 5.0f, 6.0f });
            var v3 = new Vector(new float[] { 7.0f, 8.0f, 9.0f });
            
            insertCommand.Parameters.AddWithValue("v1", v1);
            insertCommand.Parameters.AddWithValue("v2", v2);
            insertCommand.Parameters.AddWithValue("v3", v3);
            
            var inserted = await insertCommand.ExecuteNonQueryAsync();
            inserted.Should().Be(3, "three rows should be inserted");

            // Act - Query by similarity (L2 distance)
            await using var queryCommand = connection.CreateCommand();
            queryCommand.CommandText = $@"
                SELECT id, embedding <-> @query AS distance 
                FROM {tableName} 
                ORDER BY embedding <-> @query 
                LIMIT 1;";
            
            // Query vector closest to [1,2,3]
            queryCommand.Parameters.AddWithValue("query", new Vector(new float[] { 1.1f, 2.1f, 3.1f }));
            
            await using var reader = await queryCommand.ExecuteReaderAsync();
            await reader.ReadAsync();

            // Assert - First row should be id=1 (closest to query)
            reader.GetInt32(0).Should().Be(1, "closest vector should be id=1");
        }
        finally
        {
            // Cleanup
            await using var dropCommand = connection.CreateCommand();
            dropCommand.CommandText = $"DROP TABLE IF EXISTS {tableName};";
            await dropCommand.ExecuteNonQueryAsync();
        }
    }

    [Fact(Skip = "Requires running PostgreSQL container with pgvector. Run ./scripts/db-start.sh first.")]
    public async Task VectorType_ShouldSupportCosineSimilarity()
    {
        // Arrange
        await using var connection = await _factory.CreateConnectionAsync();
        const string tableName = "_test_vector_cosine";

        try
        {
            // Setup
            await using var setupCommand = connection.CreateCommand();
            setupCommand.CommandText = $@"
                DROP TABLE IF EXISTS {tableName};
                CREATE TABLE {tableName} (
                    id SERIAL PRIMARY KEY,
                    embedding vector(3)
                );
                INSERT INTO {tableName} (embedding) VALUES 
                    ('[1,0,0]'),
                    ('[0,1,0]'),
                    ('[0.707,0.707,0]');";
            await setupCommand.ExecuteNonQueryAsync();

            // Act - Query using cosine distance (<=>)
            await using var queryCommand = connection.CreateCommand();
            queryCommand.CommandText = $@"
                SELECT id 
                FROM {tableName} 
                ORDER BY embedding <=> '[1,0,0]' 
                LIMIT 1;";
            var result = await queryCommand.ExecuteScalarAsync();

            // Assert - [1,0,0] should be most similar to itself
            result.Should().Be(1, "identical vectors should have cosine distance of 0");
        }
        finally
        {
            // Cleanup
            await using var dropCommand = connection.CreateCommand();
            dropCommand.CommandText = $"DROP TABLE IF EXISTS {tableName};";
            await dropCommand.ExecuteNonQueryAsync();
        }
    }

    public void Dispose()
    {
        _factory.Dispose();
    }
}
