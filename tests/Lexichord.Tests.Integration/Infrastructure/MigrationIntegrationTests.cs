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

    // ═══════════════════════════════════════════════════════════════════════════
    // v0.4.1b: Vector Schema Migration Tests
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact(Skip = "Requires running PostgreSQL container with pgvector. Run ./scripts/db-start.sh first.")]
    public void MigrateUp_CreatesDocumentsTable()
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
                WHERE table_name = 'Documents'
            )", connection);

        var tableExists = (bool)cmd.ExecuteScalar()!;
        tableExists.Should().BeTrue("Documents table should exist after migration");
    }

    [Fact(Skip = "Requires running PostgreSQL container with pgvector. Run ./scripts/db-start.sh first.")]
    public void MigrateUp_CreatesChunksTable()
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
                WHERE table_name = 'Chunks'
            )", connection);

        var tableExists = (bool)cmd.ExecuteScalar()!;
        tableExists.Should().BeTrue("Chunks table should exist after migration");
    }

    [Fact(Skip = "Requires running PostgreSQL container with pgvector. Run ./scripts/db-start.sh first.")]
    public void MigrateUp_CreatesHnswIndex()
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
                SELECT FROM pg_indexes
                WHERE indexname = 'IX_Chunks_Embedding_hnsw'
            )", connection);

        var indexExists = (bool)cmd.ExecuteScalar()!;
        indexExists.Should().BeTrue("HNSW index should exist for vector similarity search");
    }

    [Fact(Skip = "Requires running PostgreSQL container with pgvector. Run ./scripts/db-start.sh first.")]
    public void MigrateUp_ChunksHasVectorColumn()
    {
        // Arrange
        EnsureTestDatabaseExists();

        // Act
        _runner.MigrateUp();

        // Assert
        using var connection = new NpgsqlConnection(TestConnectionString);
        connection.Open();

        using var cmd = new NpgsqlCommand(@"
            SELECT udt_name 
            FROM information_schema.columns 
            WHERE table_name = 'Chunks' AND column_name = 'Embedding'", connection);

        var udtName = cmd.ExecuteScalar() as string;
        udtName.Should().Be("vector", "Embedding column should be vector type");
    }

    [Fact(Skip = "Requires running PostgreSQL container with pgvector. Run ./scripts/db-start.sh first.")]
    public void MigrateUp_CascadeDeleteRemovesChunks()
    {
        // Arrange
        EnsureTestDatabaseExists();
        _runner.MigrateUp();

        using var connection = new NpgsqlConnection(TestConnectionString);
        connection.Open();

        // Insert a document
        var docId = Guid.NewGuid();
        using (var insertDoc = new NpgsqlCommand($@"
            INSERT INTO ""Documents"" (""Id"", ""FilePath"", ""FileHash"", ""Status"")
            VALUES (@id, '/test/file.md', 'abc123', 'Indexed')", connection))
        {
            insertDoc.Parameters.AddWithValue("id", docId);
            insertDoc.ExecuteNonQuery();
        }

        // Insert a chunk
        using (var insertChunk = new NpgsqlCommand($@"
            INSERT INTO ""Chunks"" (""DocumentId"", ""Content"", ""ChunkIndex"")
            VALUES (@docId, 'Test content', 0)", connection))
        {
            insertChunk.Parameters.AddWithValue("docId", docId);
            insertChunk.ExecuteNonQuery();
        }

        // Act - Delete the document
        using (var deleteDoc = new NpgsqlCommand($@"
            DELETE FROM ""Documents"" WHERE ""Id"" = @id", connection))
        {
            deleteDoc.Parameters.AddWithValue("id", docId);
            deleteDoc.ExecuteNonQuery();
        }

        // Assert - Chunk should be deleted via cascade
        using var countCmd = new NpgsqlCommand($@"
            SELECT COUNT(*) FROM ""Chunks"" WHERE ""DocumentId"" = @docId", connection);
        countCmd.Parameters.AddWithValue("docId", docId);
        var chunkCount = (long)countCmd.ExecuteScalar()!;
        chunkCount.Should().Be(0, "chunks should be deleted when parent document is deleted");
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // v0.5.1a: Full-Text Search Migration Tests
    // ═══════════════════════════════════════════════════════════════════════════

    [Fact(Skip = "Requires running PostgreSQL container with pgvector. Run ./scripts/db-start.sh first.")]
    public void MigrateUp_CreatesContentTsvectorColumn()
    {
        // Arrange
        EnsureTestDatabaseExists();

        // Act
        _runner.MigrateUp();

        // Assert
        using var connection = new NpgsqlConnection(TestConnectionString);
        connection.Open();

        using var cmd = new NpgsqlCommand(@"
            SELECT column_name, data_type 
            FROM information_schema.columns 
            WHERE table_name = 'Chunks' AND column_name = 'ContentTsvector'", connection);

        using var reader = cmd.ExecuteReader();
        reader.Read().Should().BeTrue("ContentTsvector column should exist after migration");
        reader.GetString(1).Should().Be("tsvector", "column should be tsvector type");
    }

    [Fact(Skip = "Requires running PostgreSQL container with pgvector. Run ./scripts/db-start.sh first.")]
    public void MigrateUp_CreatesContentTsvectorGinIndex()
    {
        // Arrange
        EnsureTestDatabaseExists();

        // Act
        _runner.MigrateUp();

        // Assert
        using var connection = new NpgsqlConnection(TestConnectionString);
        connection.Open();

        using var cmd = new NpgsqlCommand(@"
            SELECT indexname, indexdef
            FROM pg_indexes
            WHERE tablename = 'Chunks' AND indexname = 'IX_Chunks_ContentTsvector_gin'", connection);

        using var reader = cmd.ExecuteReader();
        reader.Read().Should().BeTrue("GIN index should exist for full-text search");
        reader.GetString(1).Should().Contain("gin", "index should use GIN access method");
    }

    [Fact(Skip = "Requires running PostgreSQL container with pgvector. Run ./scripts/db-start.sh first.")]
    public void MigrateUp_ContentTsvectorIsGeneratedColumn()
    {
        // Arrange
        EnsureTestDatabaseExists();
        _runner.MigrateUp();

        using var connection = new NpgsqlConnection(TestConnectionString);
        connection.Open();

        // Insert a document
        var docId = Guid.NewGuid();
        using (var insertDoc = new NpgsqlCommand($@"
            INSERT INTO ""Documents"" (""Id"", ""FilePath"", ""FileHash"", ""Status"")
            VALUES (@id, '/test/fulltext.md', 'def456', 'Indexed')", connection))
        {
            insertDoc.Parameters.AddWithValue("id", docId);
            insertDoc.ExecuteNonQuery();
        }

        // Act - Insert a chunk with content
        const string testContent = "The quick brown fox jumps over the lazy dog";
        using (var insertChunk = new NpgsqlCommand($@"
            INSERT INTO ""Chunks"" (""DocumentId"", ""Content"", ""ChunkIndex"")
            VALUES (@docId, @content, 0)", connection))
        {
            insertChunk.Parameters.AddWithValue("docId", docId);
            insertChunk.Parameters.AddWithValue("content", testContent);
            insertChunk.ExecuteNonQuery();
        }

        // Assert - Verify tsvector was auto-generated and is searchable
        using var searchCmd = new NpgsqlCommand(@"
            SELECT ""Content"", ""ContentTsvector"" IS NOT NULL as has_tsvector,
                   ""ContentTsvector"" @@ plainto_tsquery('english', 'fox') as matches_fox,
                   ""ContentTsvector"" @@ plainto_tsquery('english', 'elephant') as matches_elephant
            FROM ""Chunks""
            WHERE ""DocumentId"" = @docId", connection);
        searchCmd.Parameters.AddWithValue("docId", docId);

        using var reader = searchCmd.ExecuteReader();
        reader.Read().Should().BeTrue();
        reader.GetBoolean(1).Should().BeTrue("ContentTsvector should be auto-generated");
        reader.GetBoolean(2).Should().BeTrue("should match 'fox' in content");
        reader.GetBoolean(3).Should().BeFalse("should not match 'elephant' (not in content)");
    }

    [Fact(Skip = "Requires running PostgreSQL container with pgvector. Run ./scripts/db-start.sh first.")]
    public void MigrateDown_DropsContentTsvectorColumn()
    {
        // Arrange
        EnsureTestDatabaseExists();
        _runner.MigrateUp();

        // Act - Migrate down to version 3 (before full-text search schema)
        _runner.MigrateDown(3);

        // Assert
        using var connection = new NpgsqlConnection(TestConnectionString);
        connection.Open();

        using var cmd = new NpgsqlCommand(@"
            SELECT COUNT(*) FROM information_schema.columns
            WHERE table_name = 'Chunks' AND column_name = 'ContentTsvector'", connection);

        var columnCount = (long)cmd.ExecuteScalar()!;
        columnCount.Should().Be(0, "ContentTsvector column should be dropped after rollback");

        // Also verify GIN index is gone
        using var indexCmd = new NpgsqlCommand(@"
            SELECT COUNT(*) FROM pg_indexes
            WHERE tablename = 'Chunks' AND indexname = 'IX_Chunks_ContentTsvector_gin'", connection);

        var indexCount = (long)indexCmd.ExecuteScalar()!;
        indexCount.Should().Be(0, "GIN index should be dropped after rollback");
    }

    [Fact(Skip = "Requires running PostgreSQL container with pgvector. Run ./scripts/db-start.sh first.")]
    public void MigrateDown_DropsVectorTables()
    {
        // Arrange
        EnsureTestDatabaseExists();
        _runner.MigrateUp();

        // Act - Migrate down to version 2 (before vector schema)
        _runner.MigrateDown(2);

        // Assert
        using var connection = new NpgsqlConnection(TestConnectionString);
        connection.Open();

        using var cmd = new NpgsqlCommand(@"
            SELECT COUNT(*) FROM information_schema.tables
            WHERE table_name IN ('Documents', 'Chunks')", connection);

        var tableCount = (long)cmd.ExecuteScalar()!;
        tableCount.Should().Be(0, "vector tables should be dropped after rollback");
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
