// =============================================================================
// File: SearchBenchmarks.cs
// Project: Lexichord.Benchmarks
// Description: Database-backed performance benchmarks for search operations.
// =============================================================================
// v0.5.8b: Measures search latency and memory allocation across corpus sizes.
//   - Hybrid Search (baseline)
//   - BM25 Keyword Search
//   - Semantic Vector Search
//   - Filtered Search
//   - Query Suggestions
//   - Context Expansion
// =============================================================================

using BenchmarkDotNet.Attributes;
using Dapper;
using Lexichord.Benchmarks.Setup;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Pgvector;

namespace Lexichord.Benchmarks.Search;

/// <summary>
/// Performance benchmarks for search operations against a realistic PostgreSQL corpus.
/// </summary>
/// <remarks>
/// <para>
/// These benchmarks measure end-to-end search latency including database queries.
/// They use Testcontainers to spin up an isolated PostgreSQL instance with pgvector.
/// </para>
/// <para>
/// <b>Performance Targets (P95):</b>
/// <list type="bullet">
///   <item>HybridSearch: 50ms (1K), 150ms (10K), 500ms (50K)</item>
///   <item>BM25Search: 30ms (1K), 100ms (10K), 350ms (50K)</item>
///   <item>SemanticSearch: 40ms (1K), 120ms (10K), 400ms (50K)</item>
///   <item>FilteredSearch: 60ms (1K), 180ms (10K), 600ms (50K)</item>
///   <item>QuerySuggestions: 20ms (1K), 50ms (10K), 150ms (50K)</item>
///   <item>ContextExpansion: 15ms (1K), 40ms (10K), 120ms (50K)</item>
/// </list>
/// </para>
/// <para><b>Introduced:</b> v0.5.8b.</para>
/// </remarks>
[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
[RankColumn]
public class SearchBenchmarks
{
    private BenchmarkDataSeeder _seeder = null!;
    private NpgsqlConnection _connection = null!;
    private float[] _queryEmbedding = null!;
    private readonly Random _random = new(42);
    private readonly string[] _queryTerms =
    [
        "semantic search",
        "vector embeddings",
        "hybrid search",
        "knowledge management",
        "text chunking",
        "BM25 ranking",
        "cosine similarity",
        "full-text search",
        "query expansion",
        "documentation"
    ];

    /// <summary>
    /// Corpus size parameter for the benchmark (1K, 10K, 50K chunks).
    /// </summary>
    [Params(1_000, 10_000, 50_000)]
    public int ChunkCount { get; set; }

    /// <summary>
    /// One-time setup that starts the PostgreSQL container and seeds data.
    /// </summary>
    [GlobalSetup]
    public async Task GlobalSetup()
    {
        _seeder = new BenchmarkDataSeeder(NullLogger<BenchmarkDataSeeder>.Instance);
        await _seeder.SeedAsync(ChunkCount);
        _connection = await _seeder.CreateConnectionAsync();
        _queryEmbedding = GenerateRandomEmbedding();
    }

    /// <summary>
    /// Cleanup that disposes of the PostgreSQL container.
    /// </summary>
    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        _connection.Dispose();
        await _seeder.DisposeAsync();
    }

    // =========================================================================
    // Hybrid Search Benchmark (Baseline)
    // =========================================================================

    /// <summary>
    /// Baseline benchmark for hybrid search combining BM25 and semantic results.
    /// </summary>
    /// <remarks>
    /// LOGIC: Executes both BM25 and semantic queries, then applies RRF fusion.
    /// This is the primary search mode for Lexichord's RAG system.
    /// </remarks>
    [Benchmark(Baseline = true)]
    public async Task<int> HybridSearch()
    {
        var query = GetRandomQuery();
        var embedding = _queryEmbedding;
        const int topK = 10;
        const float rrfK = 60f;

        // BM25 results
        var bm25Results = await _connection.QueryAsync<(Guid ChunkId, float Score)>("""
            SELECT c.id, ts_rank(c.content_tsvector, plainto_tsquery('english', @Query)) AS score
            FROM chunks c
            WHERE c.content_tsvector @@ plainto_tsquery('english', @Query)
            ORDER BY score DESC
            LIMIT @TopK
            """,
            new { Query = query, TopK = topK * 2 });

        // Semantic results (cosine similarity)
        var semanticResults = await _connection.QueryAsync<(Guid ChunkId, float Score)>("""
            SELECT c.id, 1 - (c.embedding <=> @Embedding::vector) AS score
            FROM chunks c
            ORDER BY c.embedding <=> @Embedding::vector
            LIMIT @TopK
            """,
            new { Embedding = new Vector(embedding), TopK = topK * 2 });

        // RRF fusion
        var bm25List = bm25Results.ToList();
        var semanticList = semanticResults.ToList();
        var fusedScores = new Dictionary<Guid, float>();

        for (int i = 0; i < bm25List.Count; i++)
        {
            var id = bm25List[i].ChunkId;
            fusedScores[id] = fusedScores.GetValueOrDefault(id) + 1f / (rrfK + i + 1);
        }

        for (int i = 0; i < semanticList.Count; i++)
        {
            var id = semanticList[i].ChunkId;
            fusedScores[id] = fusedScores.GetValueOrDefault(id) + 1f / (rrfK + i + 1);
        }

        return fusedScores.OrderByDescending(kv => kv.Value).Take(topK).Count();
    }

    // =========================================================================
    // BM25 Search Benchmark
    // =========================================================================

    /// <summary>
    /// Benchmark for BM25 keyword search using PostgreSQL full-text search.
    /// </summary>
    [Benchmark]
    public async Task<int> BM25Search()
    {
        var query = GetRandomQuery();
        const int topK = 10;

        var results = await _connection.QueryAsync<(Guid ChunkId, float Score)>("""
            SELECT c.id, ts_rank(c.content_tsvector, plainto_tsquery('english', @Query)) AS score
            FROM chunks c
            WHERE c.content_tsvector @@ plainto_tsquery('english', @Query)
            ORDER BY score DESC
            LIMIT @TopK
            """,
            new { Query = query, TopK = topK });

        return results.Count();
    }

    // =========================================================================
    // Semantic Search Benchmark
    // =========================================================================

    /// <summary>
    /// Benchmark for pure semantic vector search using pgvector.
    /// </summary>
    [Benchmark]
    public async Task<int> SemanticSearchOnly()
    {
        var embedding = _queryEmbedding;
        const int topK = 10;

        var results = await _connection.QueryAsync<(Guid ChunkId, float Score)>("""
            SELECT c.id, 1 - (c.embedding <=> @Embedding::vector) AS score
            FROM chunks c
            ORDER BY c.embedding <=> @Embedding::vector
            LIMIT @TopK
            """,
            new { Embedding = new Vector(embedding), TopK = topK });

        return results.Count();
    }

    // =========================================================================
    // Filtered Search Benchmark
    // =========================================================================

    /// <summary>
    /// Benchmark for search with document-level filtering.
    /// </summary>
    [Benchmark]
    public async Task<int> FilteredSearch()
    {
        var embedding = _queryEmbedding;
        const int topK = 10;

        // Get a random document ID to filter by
        var docId = await _connection.QueryFirstOrDefaultAsync<Guid>(
            "SELECT id FROM documents ORDER BY RANDOM() LIMIT 1");

        var results = await _connection.QueryAsync<(Guid ChunkId, float Score)>("""
            SELECT c.id, 1 - (c.embedding <=> @Embedding::vector) AS score
            FROM chunks c
            WHERE c.document_id = @DocId
            ORDER BY c.embedding <=> @Embedding::vector
            LIMIT @TopK
            """,
            new { Embedding = new Vector(embedding), DocId = docId, TopK = topK });

        return results.Count();
    }

    // =========================================================================
    // Query Suggestions Benchmark
    // =========================================================================

    /// <summary>
    /// Benchmark for autocomplete-style query suggestions.
    /// </summary>
    [Benchmark]
    public async Task<int> QuerySuggestions()
    {
        var prefix = GetRandomQuery()[..4]; // First 4 chars as prefix
        const int topK = 5;

        // Simulate prefix-based suggestion using tsvector
        var results = await _connection.QueryAsync<string>("""
            SELECT DISTINCT word
            FROM (
                SELECT unnest(tsvector_to_array(content_tsvector)) AS word
                FROM chunks
                LIMIT 1000
            ) words
            WHERE word LIKE @Prefix || '%'
            LIMIT @TopK
            """,
            new { Prefix = prefix.ToLowerInvariant(), TopK = topK });

        return results.Count();
    }

    // =========================================================================
    // Context Expansion Benchmark
    // =========================================================================

    /// <summary>
    /// Benchmark for retrieving surrounding context for a chunk.
    /// </summary>
    [Benchmark]
    public async Task<int> ContextExpansion()
    {
        // Get a random chunk
        var chunk = await _connection.QueryFirstOrDefaultAsync<(Guid DocId, int ChunkIndex)>("""
            SELECT document_id, chunk_index FROM chunks ORDER BY RANDOM() LIMIT 1
            """);

        // Fetch surrounding chunks
        var results = await _connection.QueryAsync<string>("""
            SELECT content
            FROM chunks
            WHERE document_id = @DocId
              AND chunk_index BETWEEN @StartIndex AND @EndIndex
            ORDER BY chunk_index
            """,
            new
            {
                chunk.DocId,
                StartIndex = Math.Max(0, chunk.ChunkIndex - 2),
                EndIndex = chunk.ChunkIndex + 2
            });

        return results.Count();
    }

    // =========================================================================
    // Helper Methods
    // =========================================================================

    /// <summary>
    /// Gets a random query term for benchmarking.
    /// </summary>
    private string GetRandomQuery()
    {
        return _queryTerms[_random.Next(_queryTerms.Length)];
    }

    /// <summary>
    /// Generates a random normalized embedding vector.
    /// </summary>
    private float[] GenerateRandomEmbedding()
    {
        const int dimensions = 1536;
        var vector = new float[dimensions];
        float magnitude = 0;

        for (int i = 0; i < dimensions; i++)
        {
            vector[i] = (float)(_random.NextDouble() * 2 - 1);
            magnitude += vector[i] * vector[i];
        }

        magnitude = MathF.Sqrt(magnitude);
        for (int i = 0; i < dimensions; i++)
        {
            vector[i] /= magnitude;
        }

        return vector;
    }
}
