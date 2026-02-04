// =============================================================================
// File: BenchmarkDataSeeder.cs
// Project: Lexichord.Benchmarks
// Description: Generates synthetic test corpus for search performance benchmarks.
// =============================================================================
// v0.5.8b: Seeds a Testcontainers PostgreSQL database with documents and chunks
//          for realistic search performance testing.
// =============================================================================

using System.Text;
using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;
using Pgvector;
using Testcontainers.PostgreSql;

namespace Lexichord.Benchmarks.Setup;

/// <summary>
/// Generates and seeds synthetic test data for search performance benchmarks.
/// </summary>
/// <remarks>
/// <para>
/// This seeder creates a realistic corpus of documents and chunks with embeddings
/// for benchmarking search operations. It uses Testcontainers to spin up an isolated
/// PostgreSQL instance with pgvector extension.
/// </para>
/// <para>
/// <b>Data Generation:</b>
/// <list type="bullet">
///   <item>Documents: ~10% of chunk count (min 10, max 500)</item>
///   <item>Chunks: Parameterized count (1K, 10K, 50K)</item>
///   <item>Embeddings: 1536-dimensional random vectors (normalized)</item>
///   <item>Content: Synthetic technical documentation text</item>
/// </list>
/// </para>
/// <para><b>Introduced:</b> v0.5.8b.</para>
/// </remarks>
public sealed class BenchmarkDataSeeder : IAsyncDisposable
{
    private readonly PostgreSqlContainer _postgres;
    private readonly ILogger<BenchmarkDataSeeder> _logger;
    private NpgsqlDataSource? _dataSource;
    private const int EmbeddingDimensions = 1536;
    private readonly Random _random = new(42); // Deterministic seed

    /// <summary>
    /// Gets the connection string for the seeded database.
    /// </summary>
    public string ConnectionString => _postgres.GetConnectionString();

    /// <summary>
    /// Creates a new <see cref="BenchmarkDataSeeder"/> instance.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public BenchmarkDataSeeder(ILogger<BenchmarkDataSeeder> logger)
    {
        _logger = logger;

        // LOGIC: Use pgvector image for vector similarity operations.
        _postgres = new PostgreSqlBuilder()
            .WithImage("pgvector/pgvector:pg16")
            .WithDatabase("lexichord_bench")
            .WithUsername("bench")
            .WithPassword("bench_password")
            .Build();
    }

    /// <summary>
    /// Starts the PostgreSQL container and seeds the database with test data.
    /// </summary>
    /// <param name="chunkCount">Number of chunks to generate (1K, 10K, 50K).</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task SeedAsync(int chunkCount, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting PostgreSQL container for benchmark...");
        await _postgres.StartAsync(ct);

        _logger.LogInformation("Container started. Connection: {ConnectionString}",
            _postgres.GetConnectionString());

        // LOGIC: Configure Npgsql for pgvector support.
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(_postgres.GetConnectionString());
        dataSourceBuilder.UseVector();
        _dataSource = dataSourceBuilder.Build();

        await using var connection = await _dataSource.OpenConnectionAsync(ct);

        await CreateSchemaAsync(connection, ct);

        var documentCount = Math.Max(10, Math.Min(500, chunkCount / 10));
        var documents = await SeedDocumentsAsync(connection, documentCount, ct);
        await SeedChunksAsync(connection, documents, chunkCount, ct);

        _logger.LogInformation(
            "Seeding complete: {DocCount} documents, {ChunkCount} chunks",
            documentCount,
            chunkCount);
    }

    /// <summary>
    /// Creates a new database connection from the seeded data source.
    /// </summary>
    public async Task<NpgsqlConnection> CreateConnectionAsync()
    {
        if (_dataSource is null)
            throw new InvalidOperationException("Database not seeded. Call SeedAsync first.");

        return await _dataSource.OpenConnectionAsync();
    }

    /// <summary>
    /// Creates the schema required for search benchmarks.
    /// </summary>
    private async Task CreateSchemaAsync(NpgsqlConnection connection, CancellationToken ct)
    {
        _logger.LogInformation("Creating benchmark schema...");

        // LOGIC: Create pgvector extension and minimal schema for benchmarks.
        await connection.ExecuteAsync("""
            CREATE EXTENSION IF NOT EXISTS vector;

            CREATE TABLE IF NOT EXISTS documents (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                project_id UUID NOT NULL,
                file_path TEXT NOT NULL,
                title TEXT NOT NULL,
                hash TEXT NOT NULL,
                status TEXT NOT NULL DEFAULT 'Indexed',
                indexed_at TIMESTAMPTZ DEFAULT NOW(),
                failure_reason TEXT,
                UNIQUE(project_id, file_path)
            );

            CREATE TABLE IF NOT EXISTS chunks (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                document_id UUID NOT NULL REFERENCES documents(id) ON DELETE CASCADE,
                chunk_index INT NOT NULL,
                content TEXT NOT NULL,
                content_tsvector TSVECTOR,
                embedding vector(1536),
                metadata JSONB DEFAULT '{}',
                created_at TIMESTAMPTZ DEFAULT NOW(),
                UNIQUE(document_id, chunk_index)
            );

            CREATE INDEX IF NOT EXISTS idx_chunks_embedding 
                ON chunks USING ivfflat (embedding vector_cosine_ops)
                WITH (lists = 100);

            CREATE INDEX IF NOT EXISTS idx_chunks_content_tsvector 
                ON chunks USING GIN (content_tsvector);

            CREATE INDEX IF NOT EXISTS idx_chunks_document_id 
                ON chunks (document_id);
            """);
    }

    /// <summary>
    /// Seeds documents into the database.
    /// </summary>
    private async Task<List<Guid>> SeedDocumentsAsync(
        NpgsqlConnection connection,
        int count,
        CancellationToken ct)
    {
        _logger.LogInformation("Seeding {Count} documents...", count);

        var projectId = Guid.NewGuid();
        var documentIds = new List<Guid>();
        var documentTypes = new[] { "guide", "reference", "tutorial", "api", "concept" };

        for (int i = 0; i < count; i++)
        {
            var docType = documentTypes[i % documentTypes.Length];
            var docId = Guid.NewGuid();
            documentIds.Add(docId);

            await connection.ExecuteAsync("""
                INSERT INTO documents (id, project_id, file_path, title, hash, status)
                VALUES (@Id, @ProjectId, @FilePath, @Title, @Hash, 'Indexed')
                """,
                new
                {
                    Id = docId,
                    ProjectId = projectId,
                    FilePath = $"docs/{docType}/{docType}_{i:D4}.md",
                    Title = $"{char.ToUpper(docType[0])}{docType[1..]} Document {i}",
                    Hash = Guid.NewGuid().ToString("N")
                });
        }

        return documentIds;
    }

    /// <summary>
    /// Seeds chunks with synthetic content and embeddings.
    /// </summary>
    private async Task SeedChunksAsync(
        NpgsqlConnection connection,
        List<Guid> documentIds,
        int chunkCount,
        CancellationToken ct)
    {
        _logger.LogInformation("Seeding {Count} chunks with embeddings...", chunkCount);

        var batchSize = 500;
        var inserted = 0;

        while (inserted < chunkCount)
        {
            var batch = Math.Min(batchSize, chunkCount - inserted);

            for (int i = 0; i < batch; i++)
            {
                var docId = documentIds[(inserted + i) % documentIds.Count];
                var chunkIndex = (inserted + i) / documentIds.Count;
                var content = GenerateChunkContent();
                var embedding = GenerateRandomEmbedding();

                await connection.ExecuteAsync("""
                    INSERT INTO chunks (document_id, chunk_index, content, content_tsvector, embedding)
                    VALUES (@DocId, @ChunkIndex, @Content, to_tsvector('english', @Content), @Embedding::vector)
                    """,
                    new
                    {
                        DocId = docId,
                        ChunkIndex = chunkIndex,
                        Content = content,
                        Embedding = new Vector(embedding)
                    });
            }

            inserted += batch;

            if (inserted % 1000 == 0)
            {
                _logger.LogInformation("Inserted {Inserted}/{Total} chunks...", inserted, chunkCount);
            }
        }
    }

    /// <summary>
    /// Generates synthetic technical documentation content.
    /// </summary>
    private string GenerateChunkContent()
    {
        var topics = new[]
        {
            "technical writing", "documentation", "semantic search", "vector embeddings",
            "knowledge management", "retrieval augmented generation", "natural language processing",
            "text chunking", "hybrid search", "BM25 ranking", "cosine similarity",
            "PostgreSQL", "pgvector", "full-text search", "query expansion"
        };

        var sb = new StringBuilder();
        var sentenceCount = _random.Next(3, 8);

        for (int i = 0; i < sentenceCount; i++)
        {
            var topic1 = topics[_random.Next(topics.Length)];
            var topic2 = topics[_random.Next(topics.Length)];
            var templates = new[]
            {
                $"The {topic1} system integrates with {topic2} for enhanced performance.",
                $"When implementing {topic1}, consider the implications for {topic2}.",
                $"Best practices for {topic1} include proper handling of {topic2}.",
                $"The relationship between {topic1} and {topic2} is fundamental to understanding.",
                $"Advanced features in {topic1} leverage {topic2} capabilities."
            };
            sb.Append(templates[_random.Next(templates.Length)]);
            sb.Append(' ');
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    /// Generates a random normalized embedding vector.
    /// </summary>
    private float[] GenerateRandomEmbedding()
    {
        var vector = new float[EmbeddingDimensions];
        float magnitude = 0;

        for (int i = 0; i < EmbeddingDimensions; i++)
        {
            vector[i] = (float)(_random.NextDouble() * 2 - 1);
            magnitude += vector[i] * vector[i];
        }

        magnitude = MathF.Sqrt(magnitude);
        for (int i = 0; i < EmbeddingDimensions; i++)
        {
            vector[i] /= magnitude;
        }

        return vector;
    }

    /// <summary>
    /// Disposes of the PostgreSQL container.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        _dataSource?.Dispose();
        await _postgres.DisposeAsync();
    }
}
