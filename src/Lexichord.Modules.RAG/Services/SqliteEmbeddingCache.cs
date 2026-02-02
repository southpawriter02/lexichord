// =============================================================================
// File: SqliteEmbeddingCache.cs
// Project: Lexichord.Modules.RAG
// Description: SQLite-based implementation of the embedding cache.
// Version: v0.4.8d
// =============================================================================
// LOGIC: Provides persistent local storage for embedding vectors using SQLite.
//   - Content hash as primary key for efficient lookups.
//   - LRU eviction based on last_accessed_at timestamp.
//   - Binary serialization for embedding storage efficiency.
//   - Thread-safe operations via connection pooling.
// =============================================================================

using System.Security.Cryptography;
using System.Text;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.RAG.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// SQLite-based implementation of <see cref="IEmbeddingCache"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SqliteEmbeddingCache"/> stores embedding vectors in a local SQLite database,
/// keyed by the SHA-256 hash of the original content. This enables efficient retrieval
/// of previously computed embeddings and reduces API costs.
/// </para>
/// <para>
/// <b>Features:</b>
/// <list type="bullet">
///   <item>LRU (Least Recently Used) eviction when size limit is exceeded.</item>
///   <item>Binary serialization for efficient embedding storage.</item>
///   <item>Thread-safe via SQLite connection pooling.</item>
///   <item>Automatic schema initialization on first use.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.8d as part of the RAG Hardening phase.
/// </para>
/// </remarks>
public sealed class SqliteEmbeddingCache : IEmbeddingCache, IDisposable
{
    private readonly EmbeddingCacheOptions _options;
    private readonly ILogger<SqliteEmbeddingCache> _logger;
    private readonly string _connectionString;
    private readonly object _lock = new();

    private long _hitCount;
    private long _missCount;
    private int _evictionCount;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="SqliteEmbeddingCache"/>.
    /// </summary>
    /// <param name="options">Configuration options for the cache.</param>
    /// <param name="logger">Logger for cache operations.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> or <paramref name="logger"/> is null.
    /// </exception>
    public SqliteEmbeddingCache(
        IOptions<EmbeddingCacheOptions> options,
        ILogger<SqliteEmbeddingCache> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _logger = logger;

        // LOGIC: Build connection string with pooling enabled for thread safety.
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _options.CachePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            Pooling = true
        }.ToString();

        InitializeSchema();

        _logger.LogInformation(
            "Embedding cache initialized at {CachePath}, max size: {MaxSizeMB} MB",
            _options.CachePath,
            _options.MaxSizeMB);
    }

    /// <inheritdoc/>
    public bool TryGet(string contentHash, out float[]? embedding)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentHash);

        if (!_options.Enabled)
        {
            embedding = null;
            return false;
        }

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        // LOGIC: Select embedding and update last_accessed_at in a single transaction.
        using var transaction = connection.BeginTransaction();

        try
        {
            using var selectCmd = connection.CreateCommand();
            selectCmd.Transaction = transaction;
            selectCmd.CommandText = """
                SELECT embedding FROM embeddings WHERE content_hash = $hash
                """;
            selectCmd.Parameters.AddWithValue("$hash", contentHash);

            var result = selectCmd.ExecuteScalar();

            if (result is byte[] bytes)
            {
                // LOGIC: Update last_accessed_at for LRU tracking.
                using var updateCmd = connection.CreateCommand();
                updateCmd.Transaction = transaction;
                updateCmd.CommandText = """
                    UPDATE embeddings SET last_accessed_at = $now WHERE content_hash = $hash
                    """;
                updateCmd.Parameters.AddWithValue("$hash", contentHash);
                updateCmd.Parameters.AddWithValue("$now", DateTime.UtcNow.Ticks);
                updateCmd.ExecuteNonQuery();

                transaction.Commit();

                embedding = DeserializeEmbedding(bytes);
                Interlocked.Increment(ref _hitCount);

                _logger.LogDebug("Cache hit for content hash {ContentHash}", TruncateHash(contentHash));
                return true;
            }

            transaction.Commit();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error accessing cache for hash {ContentHash}", TruncateHash(contentHash));
            transaction.Rollback();
        }

        embedding = null;
        Interlocked.Increment(ref _missCount);

        _logger.LogDebug("Cache miss for content hash {ContentHash}", TruncateHash(contentHash));
        return false;
    }

    /// <inheritdoc/>
    public void Set(string contentHash, float[] embedding)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentHash);
        ArgumentNullException.ThrowIfNull(embedding);

        if (!_options.Enabled)
        {
            return;
        }

        var bytes = SerializeEmbedding(embedding);
        var now = DateTime.UtcNow.Ticks;

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        // LOGIC: Upsert the embedding - update if exists, insert if new.
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO embeddings (content_hash, embedding, created_at, last_accessed_at)
            VALUES ($hash, $embedding, $now, $now)
            ON CONFLICT(content_hash) DO UPDATE SET
                embedding = $embedding,
                last_accessed_at = $now
            """;
        cmd.Parameters.AddWithValue("$hash", contentHash);
        cmd.Parameters.AddWithValue("$embedding", bytes);
        cmd.Parameters.AddWithValue("$now", now);
        cmd.ExecuteNonQuery();

        _logger.LogDebug("Cached embedding for content hash {ContentHash}", TruncateHash(contentHash));

        // LOGIC: Check if compaction is needed after insertion.
        CheckForCompaction();
    }

    /// <inheritdoc/>
    public EmbeddingCacheStatistics GetStatistics()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT COUNT(*), COALESCE(SUM(LENGTH(embedding)), 0) FROM embeddings
            """;

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            var entryCount = reader.GetInt32(0);
            var sizeBytes = reader.GetInt64(1);
            var hitCount = Interlocked.Read(ref _hitCount);
            var missCount = Interlocked.Read(ref _missCount);
            var total = hitCount + missCount;
            var hitRate = total > 0 ? (double)hitCount / total : 0.0;

            return new EmbeddingCacheStatistics(
                EntryCount: entryCount,
                SizeBytes: sizeBytes,
                HitCount: hitCount,
                MissCount: missCount,
                HitRate: hitRate,
                EvictionCount: _evictionCount);
        }

        return EmbeddingCacheStatistics.Empty;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        lock (_lock)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM embeddings";
            var deleted = cmd.ExecuteNonQuery();

            // Reset statistics
            Interlocked.Exchange(ref _hitCount, 0);
            Interlocked.Exchange(ref _missCount, 0);
            _evictionCount = 0;

            _logger.LogInformation("Cleared embedding cache, removed {Count} entries", deleted);
        }
    }

    /// <inheritdoc/>
    public void Compact()
    {
        lock (_lock)
        {
            PerformEviction();

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // LOGIC: Run SQLite VACUUM to reclaim disk space.
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "VACUUM";
            cmd.ExecuteNonQuery();

            _logger.LogDebug("Compaction completed");
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // LOGIC: Clear the connection pool to release all connections.
        SqliteConnection.ClearAllPools();

        _logger.LogDebug("Embedding cache disposed");
    }

    #region Private Methods

    private void InitializeSchema()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS embeddings (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                content_hash TEXT NOT NULL UNIQUE,
                embedding BLOB NOT NULL,
                created_at INTEGER NOT NULL,
                last_accessed_at INTEGER NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_embeddings_hash ON embeddings(content_hash);
            CREATE INDEX IF NOT EXISTS idx_embeddings_lru ON embeddings(last_accessed_at);
            """;
        cmd.ExecuteNonQuery();
    }

    private void CheckForCompaction()
    {
        var stats = GetStatistics();

        if (stats.SizeBytes > _options.MaxSizeBytes)
        {
            _logger.LogDebug(
                "Cache size {CurrentSize} exceeds limit {MaxSize}, triggering eviction",
                stats.FormatSize(),
                $"{_options.MaxSizeMB} MB");

            PerformEviction();
        }
    }

    private void PerformEviction()
    {
        lock (_lock)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // LOGIC: Calculate target size (80% of max to avoid frequent evictions).
            var targetBytes = (long)(_options.MaxSizeBytes * 0.8);

            // LOGIC: Get current size.
            using var sizeCmd = connection.CreateCommand();
            sizeCmd.CommandText = "SELECT COALESCE(SUM(LENGTH(embedding)), 0) FROM embeddings";
            var currentSize = Convert.ToInt64(sizeCmd.ExecuteScalar());

            if (currentSize <= targetBytes)
            {
                return;
            }

            var bytesToFree = currentSize - targetBytes;
            var entriesEvicted = 0;
            var bytesFreed = 0L;

            // LOGIC: Delete entries in LRU order until we've freed enough space.
            while (bytesFreed < bytesToFree)
            {
                using var selectCmd = connection.CreateCommand();
                selectCmd.CommandText = """
                    SELECT id, LENGTH(embedding) FROM embeddings
                    ORDER BY last_accessed_at ASC
                    LIMIT 100
                    """;

                var idsToDelete = new List<long>();
                using (var reader = selectCmd.ExecuteReader())
                {
                    while (reader.Read() && bytesFreed < bytesToFree)
                    {
                        idsToDelete.Add(reader.GetInt64(0));
                        bytesFreed += reader.GetInt64(1);
                    }
                }

                if (idsToDelete.Count == 0)
                {
                    break;
                }

                using var deleteCmd = connection.CreateCommand();
                deleteCmd.CommandText = $"DELETE FROM embeddings WHERE id IN ({string.Join(",", idsToDelete)})";
                entriesEvicted += deleteCmd.ExecuteNonQuery();
            }

            _evictionCount += entriesEvicted;

            _logger.LogInformation(
                "Evicted {Count} entries, freed approximately {Size} bytes",
                entriesEvicted,
                bytesFreed);
        }
    }

    /// <summary>
    /// Serializes a float array to a byte array.
    /// </summary>
    private static byte[] SerializeEmbedding(float[] embedding)
    {
        var bytes = new byte[embedding.Length * sizeof(float)];
        Buffer.BlockCopy(embedding, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    /// <summary>
    /// Deserializes a byte array to a float array.
    /// </summary>
    private static float[] DeserializeEmbedding(byte[] bytes)
    {
        var embedding = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, embedding, 0, bytes.Length);
        return embedding;
    }

    /// <summary>
    /// Computes the SHA-256 hash of the given content.
    /// </summary>
    public static string ComputeContentHash(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Truncates a hash to a maximum of 8 characters for logging purposes.
    /// </summary>
    private static string TruncateHash(string hash) =>
        hash.Length <= 8 ? hash : hash[..8];

    #endregion
}
