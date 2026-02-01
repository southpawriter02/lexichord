// =============================================================================
// File: ChunkRepository.cs
// Project: Lexichord.Modules.RAG
// Description: Dapper implementation of IChunkRepository with vector similarity search.
// =============================================================================
// LOGIC: Provides chunk storage and pgvector-based semantic search.
//   - SearchSimilarAsync uses the <=> operator for cosine distance.
//   - AddRangeAsync uses batch INSERT for efficiency.
//   - Embedding dimension validation prevents costly database errors.
// =============================================================================

using System.Data;
using System.Diagnostics;
using Dapper;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Microsoft.Extensions.Logging;
using DapperCommandDefinition = Dapper.CommandDefinition;

namespace Lexichord.Modules.RAG.Data;

/// <summary>
/// Dapper-based implementation of <see cref="IChunkRepository"/> for managing
/// document chunks and performing vector similarity searches.
/// </summary>
/// <remarks>
/// <para>
/// This repository provides storage and retrieval for chunks, including the core
/// semantic search capability via <see cref="SearchSimilarAsync"/>.
/// </para>
/// <para>
/// <b>Vector Search:</b> Uses pgvector's <c>&lt;=&gt;</c> operator for cosine distance,
/// which leverages the HNSW index on the <c>embedding</c> column for efficient
/// approximate nearest neighbor queries.
/// </para>
/// <para>
/// <b>Embedding Dimensions:</b> The repository validates that query embeddings
/// have 1536 dimensions (OpenAI text-embedding-3-small) to prevent costly
/// database errors.
/// </para>
/// </remarks>
public sealed class ChunkRepository : IChunkRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<ChunkRepository> _logger;

    private const string TableName = "chunks";
    private const int ExpectedEmbeddingDimensions = 1536;

    /// <summary>
    /// Creates a new <see cref="ChunkRepository"/> instance.
    /// </summary>
    /// <param name="connectionFactory">Factory for database connections.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="connectionFactory"/> or <paramref name="logger"/> is null.
    /// </exception>
    public ChunkRepository(
        IDbConnectionFactory connectionFactory,
        ILogger<ChunkRepository> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Read Operations

    /// <inheritdoc />
    public async Task<IEnumerable<Chunk>> GetByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        const string sql = @"
            SELECT id AS ""Id"", 
                   document_id AS ""DocumentId"", 
                   content AS ""Content"", 
                   embedding AS ""Embedding"", 
                   chunk_index AS ""ChunkIndex"", 
                   start_offset AS ""StartOffset"", 
                   end_offset AS ""EndOffset""
            FROM chunks
            WHERE document_id = @DocumentId
            ORDER BY chunk_index";

        var command = new DapperCommandDefinition(sql, new { DocumentId = documentId }, cancellationToken: cancellationToken);
        var results = await connection.QueryAsync<Chunk>(command);
        var resultList = results.ToList();

        _logger.LogDebug("GetByDocumentId {Table} DocumentId={DocumentId}: {Count} chunks", 
            TableName, documentId, resultList.Count);
        return resultList;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ChunkSearchResult>> SearchSimilarAsync(
        float[] queryEmbedding,
        int topK = 10,
        double threshold = 0.5,
        Guid? projectId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queryEmbedding);

        // LOGIC: Validate embedding dimensions to prevent costly database errors.
        if (queryEmbedding.Length != ExpectedEmbeddingDimensions)
        {
            throw new ArgumentException(
                $"Query embedding must have {ExpectedEmbeddingDimensions} dimensions, " +
                $"but has {queryEmbedding.Length}.",
                nameof(queryEmbedding));
        }

        var stopwatch = Stopwatch.StartNew();
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        // LOGIC: Use pgvector's <=> operator for cosine distance.
        // Similarity score = 1 - cosine_distance.
        // Filter by threshold and optionally by project through document join.
        string sql;
        object parameters;

        if (projectId.HasValue)
        {
            sql = @"
                SELECT c.id AS ""Id"", 
                       c.document_id AS ""DocumentId"", 
                       c.content AS ""Content"", 
                       c.embedding AS ""Embedding"", 
                       c.chunk_index AS ""ChunkIndex"", 
                       c.start_offset AS ""StartOffset"", 
                       c.end_offset AS ""EndOffset"",
                       1 - (c.embedding <=> @QueryEmbedding::vector) AS ""SimilarityScore""
                FROM chunks c
                INNER JOIN documents d ON c.document_id = d.id
                WHERE d.project_id = @ProjectId
                  AND c.embedding IS NOT NULL
                  AND 1 - (c.embedding <=> @QueryEmbedding::vector) >= @Threshold
                ORDER BY c.embedding <=> @QueryEmbedding::vector
                LIMIT @TopK";
            parameters = new { QueryEmbedding = queryEmbedding, TopK = topK, Threshold = threshold, ProjectId = projectId.Value };
        }
        else
        {
            sql = @"
                SELECT c.id AS ""Id"", 
                       c.document_id AS ""DocumentId"", 
                       c.content AS ""Content"", 
                       c.embedding AS ""Embedding"", 
                       c.chunk_index AS ""ChunkIndex"", 
                       c.start_offset AS ""StartOffset"", 
                       c.end_offset AS ""EndOffset"",
                       1 - (c.embedding <=> @QueryEmbedding::vector) AS ""SimilarityScore""
                FROM chunks c
                WHERE c.embedding IS NOT NULL
                  AND 1 - (c.embedding <=> @QueryEmbedding::vector) >= @Threshold
                ORDER BY c.embedding <=> @QueryEmbedding::vector
                LIMIT @TopK";
            parameters = new { QueryEmbedding = queryEmbedding, TopK = topK, Threshold = threshold };
        }

        var command = new DapperCommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<ChunkWithScore>(command);
        var resultList = rows.Select(r => new ChunkSearchResult(
            new Chunk(r.Id, r.DocumentId, r.Content, r.Embedding, r.ChunkIndex, r.StartOffset, r.EndOffset),
            r.SimilarityScore
        )).ToList();

        stopwatch.Stop();
        var elapsed = stopwatch.Elapsed;

        _logger.LogDebug(
            "SearchSimilar {Table}: {Count} results in {ElapsedMs}ms (TopK={TopK}, Threshold={Threshold}, ProjectId={ProjectId})",
            TableName, resultList.Count, elapsed.TotalMilliseconds, topK, threshold, projectId);

        // LOGIC: Warn if query is slow (>100ms) to aid performance troubleshooting.
        if (elapsed.TotalMilliseconds > 100)
        {
            _logger.LogWarning(
                "Slow vector search: {ElapsedMs}ms for TopK={TopK}. Consider index tuning.",
                elapsed.TotalMilliseconds, topK);
        }

        return resultList;
    }

    #endregion

    #region Write Operations

    /// <inheritdoc />
    public async Task AddRangeAsync(IEnumerable<Chunk> chunks, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(chunks);

        var chunkList = chunks.ToList();
        if (chunkList.Count == 0)
        {
            _logger.LogDebug("AddRange {Table}: Empty collection, nothing to insert", TableName);
            return;
        }

        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        // LOGIC: Use Dapper's multi-row insert capability for batch efficiency.
        // Generate IDs for chunks with Guid.Empty.
        const string sql = @"
            INSERT INTO chunks (id, document_id, content, embedding, chunk_index, start_offset, end_offset)
            VALUES (@Id, @DocumentId, @Content, @Embedding, @ChunkIndex, @StartOffset, @EndOffset)";

        var parameters = chunkList.Select(c => new
        {
            Id = c.Id == Guid.Empty ? Guid.NewGuid() : c.Id,
            c.DocumentId,
            c.Content,
            c.Embedding,
            c.ChunkIndex,
            c.StartOffset,
            c.EndOffset
        }).ToList();

        var stopwatch = Stopwatch.StartNew();
        var affected = await connection.ExecuteAsync(sql, parameters);
        stopwatch.Stop();

        _logger.LogDebug(
            "AddRange {Table}: Inserted {Count} chunks in {ElapsedMs}ms",
            TableName, affected, stopwatch.Elapsed.TotalMilliseconds);
    }

    /// <inheritdoc />
    public async Task<int> DeleteByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        const string sql = "DELETE FROM chunks WHERE document_id = @DocumentId";
        var command = new DapperCommandDefinition(sql, new { DocumentId = documentId }, cancellationToken: cancellationToken);
        var affected = await connection.ExecuteAsync(command);

        _logger.LogDebug("DeleteByDocumentId {Table} DocumentId={DocumentId}: {Count} chunks deleted", 
            TableName, documentId, affected);
        return affected;
    }

    #endregion

    /// <summary>
    /// Internal record for mapping query results with similarity scores.
    /// </summary>
    private record ChunkWithScore(
        Guid Id,
        Guid DocumentId,
        string Content,
        float[]? Embedding,
        int ChunkIndex,
        int StartOffset,
        int EndOffset,
        double SimilarityScore);
}
