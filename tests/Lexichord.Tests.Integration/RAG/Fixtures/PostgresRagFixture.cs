// =============================================================================
// File: PostgresRagFixture.cs
// Project: Lexichord.Tests.Integration
// Description: PostgreSQL + pgvector fixture for RAG integration tests.
// =============================================================================
// v0.4.8b: Provides shared database infrastructure for RAG integration tests.
//   - Uses Testcontainers to spin up PostgreSQL with pgvector
//   - Runs migrations to create schema
//   - Provides DI container with real repositories and mock embedding service
//   - Resets database between tests using Respawn
// =============================================================================

using System.Security.Cryptography;
using System.Text;
using Dapper;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Ingestion;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Infrastructure.Data;
using FluentMigrator.Runner;
using Lexichord.Modules.RAG.Chunking;
using Lexichord.Modules.RAG.Data;
using Lexichord.Modules.RAG.Indexing;
using Lexichord.Modules.RAG.Search;
using Lexichord.Modules.RAG.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using Pgvector;
using Respawn;
using Testcontainers.PostgreSql;

namespace Lexichord.Tests.Integration.RAG.Fixtures;

/// <summary>
/// Fixture that manages a PostgreSQL container with pgvector for RAG integration tests.
/// </summary>
/// <remarks>
/// <para>
/// This fixture provides a complete RAG infrastructure stack for integration testing:
/// </para>
/// <list type="bullet">
///   <item><description>PostgreSQL container with pgvector extension</description></item>
///   <item><description>FluentMigrator schema setup</description></item>
///   <item><description>DI container with production repositories</description></item>
///   <item><description>Mock embedding service with deterministic results</description></item>
///   <item><description>Respawn for database reset between tests</description></item>
/// </list>
/// <para>
/// <b>Usage:</b> Add tests to a collection using <see cref="PostgresRagCollection"/>
/// and inject this fixture via constructor. Call <see cref="ResetDatabaseAsync"/>
/// in test class <c>InitializeAsync</c> to ensure clean state.
/// </para>
/// </remarks>
public class PostgresRagFixture : IAsyncLifetime
{
    private PostgreSqlContainer _container = null!;
    private Respawner _respawner = null!;
    private NpgsqlConnection? _respawnConnection;

    /// <summary>
    /// Gets the connection string for the test database.
    /// </summary>
    public string ConnectionString => _container.GetConnectionString();

    /// <summary>
    /// Gets the DI service provider with all RAG services configured.
    /// </summary>
    public IServiceProvider Services { get; private set; } = null!;

    /// <summary>
    /// Gets a test project ID for use in integration tests.
    /// </summary>
    public Guid TestProjectId { get; } = Guid.NewGuid();

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        // Start PostgreSQL container with pgvector
        _container = new PostgreSqlBuilder()
            .WithImage("pgvector/pgvector:pg16")
            .WithDatabase("lexichord_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();

        await _container.StartAsync();

        // Initialize services
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        // Run migrations
        await RunMigrationsAsync();

        // Configure Respawn for database reset
        _respawnConnection = new NpgsqlConnection(ConnectionString);
        await _respawnConnection.OpenAsync();

        _respawner = await Respawner.CreateAsync(_respawnConnection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            TablesToIgnore = ["VersionInfo", "SchemaVersions"],
            SchemasToInclude = ["public"]
        });
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_respawnConnection != null)
        {
            await _respawnConnection.DisposeAsync();
        }

        await _container.DisposeAsync();
    }

    /// <summary>
    /// Resets the database to a clean state between tests.
    /// </summary>
    /// <remarks>
    /// This method uses Respawn to efficiently truncate all tables except
    /// migration tracking tables. Call this in test class <c>InitializeAsync</c>.
    /// </remarks>
    public async Task ResetDatabaseAsync()
    {
        if (_respawnConnection != null)
        {
            await _respawner.ResetAsync(_respawnConnection);
        }
    }

    /// <summary>
    /// Creates a new database connection for direct SQL operations.
    /// </summary>
    /// <returns>An open database connection.</returns>
    public async Task<NpgsqlConnection> CreateConnectionAsync()
    {
        var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // CRITICAL: Register VectorTypeHandler with Dapper BEFORE any repository operations.
        // This enables transparent mapping between .NET float[] and PostgreSQL VECTOR.
        SqlMapper.AddTypeHandler(new VectorTypeHandler());

        // CRITICAL: Create a custom connection factory with pgvector support.
        // The production NpgsqlConnectionFactory doesn't have UseVector() enabled,
        // so for tests we create a direct data source with vector support.
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(ConnectionString);
        dataSourceBuilder.UseVector(); // Enable pgvector type mapping
        var dataSource = dataSourceBuilder.Build();
        
        services.AddSingleton<IDbConnectionFactory>(
            new VectorEnabledConnectionFactory(dataSource));

        // Repositories
        services.AddSingleton<IDocumentRepository>(sp =>
            new DocumentRepository(
                sp.GetRequiredService<IDbConnectionFactory>(),
                NullLogger<DocumentRepository>.Instance));

        // SiblingCache for ChunkRepository (v0.5.3b)
        services.AddSingleton<SiblingCache>(sp =>
            new SiblingCache(NullLogger<SiblingCache>.Instance));

        // Mock ICanonicalManager for ChunkRepository (v0.5.9f)
        var mockCanonicalManager = new Mock<ICanonicalManager>();
        mockCanonicalManager
            .Setup(c => c.GetProvenanceAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ChunkProvenance>());
        mockCanonicalManager
            .Setup(c => c.GetProvenanceBatchAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, IReadOnlyList<ChunkProvenance>>());
        services.AddSingleton(mockCanonicalManager.Object);

        services.AddSingleton<IChunkRepository>(sp =>
            new ChunkRepository(
                sp.GetRequiredService<IDbConnectionFactory>(),
                sp.GetRequiredService<SiblingCache>(),
                sp.GetRequiredService<ICanonicalManager>(),
                NullLogger<ChunkRepository>.Instance));

        // Mock embedding service with deterministic embeddings
        var mockEmbedder = new Mock<IEmbeddingService>();
        mockEmbedder
            .Setup(e => e.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string text, CancellationToken _) => GenerateDeterministicEmbedding(text));
        mockEmbedder
            .Setup(e => e.EmbedBatchAsync(It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<string> texts, CancellationToken _) =>
                texts.Select(GenerateDeterministicEmbedding).ToList());
        mockEmbedder.Setup(e => e.Dimensions).Returns(1536);
        mockEmbedder.Setup(e => e.ModelName).Returns("test-embedding-model");
        mockEmbedder.Setup(e => e.MaxTokens).Returns(8192); // OpenAI text-embedding-ada-002 limit
        services.AddSingleton(mockEmbedder.Object);

        // Chunking strategies (concrete types first for MarkdownHeaderChunkingStrategy dependencies)
        // MarkdownHeaderChunkingStrategy depends on ParagraphChunkingStrategy and FixedSizeChunkingStrategy
        services.AddSingleton<FixedSizeChunkingStrategy>();
        services.AddSingleton<ParagraphChunkingStrategy>();
        services.AddSingleton<MarkdownHeaderChunkingStrategy>();
        // Register as interfaces for ChunkingStrategyFactory
        services.AddSingleton<IChunkingStrategy>(sp => sp.GetRequiredService<FixedSizeChunkingStrategy>());
        services.AddSingleton<IChunkingStrategy>(sp => sp.GetRequiredService<ParagraphChunkingStrategy>());
        services.AddSingleton<IChunkingStrategy>(sp => sp.GetRequiredService<MarkdownHeaderChunkingStrategy>());
        services.AddSingleton<ChunkingStrategyFactory>();

        // File hash service
        services.AddSingleton<IFileHashService>(
            new FileHashService(NullLogger<FileHashService>.Instance));

        // Token counter (mock for tests)
        var mockTokenCounter = new Mock<ITokenCounter>();
        mockTokenCounter
            .Setup(t => t.CountTokens(It.IsAny<string>()))
            .Returns((string text) => text.Length / 4); // Approximate 4 chars per token
        mockTokenCounter
            .Setup(t => t.TruncateToTokenLimit(It.IsAny<string>(), It.IsAny<int>()))
            .Returns((string text, int limit) =>
            {
                var maxChars = limit * 4;
                if (text.Length > maxChars)
                    return (text[..maxChars], true);
                return (text, false);
            });
        services.AddSingleton(mockTokenCounter.Object);

        // Mock MediatR for event publishing
        var mockMediator = new Mock<IMediator>();
        mockMediator
            .Setup(m => m.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        services.AddSingleton(mockMediator.Object);

        // Mock license context for WriterPro tier
        var mockLicenseContext = new Mock<ILicenseContext>();
        mockLicenseContext
            .Setup(l => l.GetCurrentTier())
            .Returns(LicenseTier.WriterPro);
        services.AddSingleton(mockLicenseContext.Object);

        // Embedding options
        services.AddSingleton(Options.Create(EmbeddingOptions.Default));

        // Query preprocessor (mock for tests - just returns the query as-is)
        var mockPreprocessor = new Mock<IQueryPreprocessor>();
        mockPreprocessor
            .Setup(p => p.Process(It.IsAny<string>(), It.IsAny<SearchOptions>()))
            .Returns((string query, SearchOptions _) => query);
        mockPreprocessor
            .Setup(p => p.GetCachedEmbedding(It.IsAny<string>()))
            .Returns((float[]?)null);
        services.AddSingleton(mockPreprocessor.Object);

        // Search license guard
        services.AddSingleton(sp =>
            new SearchLicenseGuard(
                sp.GetRequiredService<ILicenseContext>(),
                sp.GetRequiredService<IMediator>(),
                NullLogger<SearchLicenseGuard>.Instance));

        // Search service
        services.AddSingleton<ISemanticSearchService>(sp =>
            new PgVectorSearchService(
                sp.GetRequiredService<IDbConnectionFactory>(),
                sp.GetRequiredService<IEmbeddingService>(),
                sp.GetRequiredService<IQueryPreprocessor>(),
                sp.GetRequiredService<IDocumentRepository>(),
                sp.GetRequiredService<SearchLicenseGuard>(),
                sp.GetRequiredService<IMediator>(),
                NullLogger<PgVectorSearchService>.Instance));

        // Indexing pipeline
        services.AddSingleton<DocumentIndexingPipeline>(sp =>
            new DocumentIndexingPipeline(
                sp.GetRequiredService<ChunkingStrategyFactory>(),
                sp.GetRequiredService<ITokenCounter>(),
                sp.GetRequiredService<IEmbeddingService>(),
                sp.GetRequiredService<IChunkRepository>(),
                sp.GetRequiredService<IDocumentRepository>(),
                sp.GetRequiredService<IMediator>(),
                sp.GetRequiredService<ILicenseContext>(),
                sp.GetRequiredService<IOptions<EmbeddingOptions>>(),
                NullLogger<DocumentIndexingPipeline>.Instance));

        // Logging
        services.AddLogging();
    }

    private async Task RunMigrationsAsync()
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();

        // Enable pgvector extension
        await conn.ExecuteAsync("CREATE EXTENSION IF NOT EXISTS vector;");

        // Create RAG-specific tables directly (matches repository lowercase naming)
        await conn.ExecuteAsync(@"
            -- Documents table (lowercase to match repository SQL; status is TEXT for enum string)
            CREATE TABLE IF NOT EXISTS documents (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                project_id UUID NOT NULL,
                file_path TEXT NOT NULL,
                title TEXT NOT NULL,
                hash TEXT NOT NULL,
                status TEXT NOT NULL DEFAULT 'Pending',
                indexed_at TIMESTAMP WITH TIME ZONE,
                failure_reason TEXT,
                CONSTRAINT uq_documents_project_id_file_path UNIQUE (project_id, file_path)
            );

            -- Chunks table with vector column (lowercase to match repository SQL)
            CREATE TABLE IF NOT EXISTS chunks (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                document_id UUID NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
                chunk_index INTEGER NOT NULL,
                content TEXT NOT NULL,
                embedding vector(1536) NOT NULL,
                start_offset INTEGER NOT NULL DEFAULT 0,
                end_offset INTEGER NOT NULL DEFAULT 0,
                metadata TEXT,
                heading TEXT,
                heading_level INTEGER NOT NULL DEFAULT 0,
                content_tsvector TSVECTOR GENERATED ALWAYS AS (to_tsvector('english', content)) STORED,
                CONSTRAINT uq_chunks_document_id_chunk_index UNIQUE (document_id, chunk_index)
            );

            -- Create HNSW index for fast similarity search if it doesn't exist
            CREATE INDEX IF NOT EXISTS ix_chunks_embedding ON chunks 
            USING hnsw (embedding vector_cosine_ops);

            -- Create GIN index for full-text search (v0.5.1a)
            CREATE INDEX IF NOT EXISTS ix_chunks_content_tsvector ON chunks
            USING GIN (content_tsvector);
        ");

        // Create views with PascalCase quoted names for PgVectorSearchService compatibility
        // (the search service uses quoted identifiers like ""Chunks"")
        await conn.ExecuteAsync(@"
            CREATE OR REPLACE VIEW ""Chunks"" AS
            SELECT 
                id AS ""Id"",
                document_id AS ""DocumentId"",
                chunk_index AS ""ChunkIndex"",
                content AS ""Content"",
                embedding AS ""Embedding"",
                start_offset AS ""StartOffset"",
                end_offset AS ""EndOffset"",
                metadata AS ""Metadata"",
                heading AS ""Heading"",
                heading_level AS ""HeadingLevel"",
                content_tsvector AS ""ContentTsvector""
            FROM chunks;
        ");
    }

    /// <summary>
    /// Generates a deterministic embedding based on text content.
    /// </summary>
    /// <remarks>
    /// This allows predictable search results in tests. The embedding is
    /// derived from a SHA-256 hash of the input text, ensuring that identical
    /// text always produces identical embeddings.
    /// </remarks>
    /// <param name="text">The text to embed.</param>
    /// <returns>A normalized 1536-dimension embedding vector.</returns>
    private static float[] GenerateDeterministicEmbedding(string text)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        var embedding = new float[1536];

        for (int i = 0; i < 1536; i++)
        {
            embedding[i] = (float)hash[i % hash.Length] / 255f;
        }

        // Normalize to unit vector
        var magnitude = MathF.Sqrt(embedding.Sum(x => x * x));
        for (int i = 0; i < 1536; i++)
        {
            embedding[i] /= magnitude;
        }

        return embedding;
    }
}

/// <summary>
/// Simple connection factory wrapper for test fixtures with pgvector support.
/// </summary>
/// <remarks>
/// This factory wraps an NpgsqlDataSource that was built with UseVector() enabled,
/// providing transparent vector type mapping for Dapper operations.
/// </remarks>
internal sealed class VectorEnabledConnectionFactory : IDbConnectionFactory, IDisposable
{
    private readonly NpgsqlDataSource _dataSource;

    public VectorEnabledConnectionFactory(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
    }

    public NpgsqlDataSource DataSource => _dataSource;

    public bool IsHealthy => true;

    public async Task<NpgsqlConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        return await _dataSource.OpenConnectionAsync(cancellationToken);
    }

    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await CreateConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        _dataSource.Dispose();
    }
}
