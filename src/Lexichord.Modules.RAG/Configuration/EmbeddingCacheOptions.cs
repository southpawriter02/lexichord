// =============================================================================
// File: EmbeddingCacheOptions.cs
// Project: Lexichord.Modules.RAG
// Description: Configuration options for the embedding cache.
// Version: v0.4.8d
// =============================================================================
// LOGIC: Provides configuration for the SQLite-based embedding cache.
//   - Enabled: Toggle caching on/off.
//   - MaxSizeMB: Maximum cache size before LRU eviction.
//   - CachePath: Path to the SQLite database file.
//   - EmbeddingDimensions: Expected embedding vector size (for storage estimation).
//   - CompactionInterval: How often to check for compaction.
// =============================================================================

namespace Lexichord.Modules.RAG.Configuration;

/// <summary>
/// Configuration options for the embedding cache.
/// </summary>
/// <remarks>
/// <para>
/// These options control the behavior of <see cref="Services.SqliteEmbeddingCache"/>,
/// including size limits, file location, and eviction timing.
/// </para>
/// <para>
/// <b>Configuration Section:</b> <c>EmbeddingCache</c>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.8d as part of the RAG Hardening phase.
/// </para>
/// </remarks>
public sealed class EmbeddingCacheOptions
{
    /// <summary>
    /// Gets or sets whether the embedding cache is enabled.
    /// </summary>
    /// <remarks>
    /// When disabled, <see cref="Services.CachedEmbeddingService"/> passes through
    /// directly to the inner embedding service without caching.
    /// </remarks>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum cache size in megabytes.
    /// </summary>
    /// <remarks>
    /// When the cache exceeds this size, LRU eviction is triggered to remove
    /// the least recently accessed entries. Default is 100 MB.
    /// </remarks>
    public int MaxSizeMB { get; set; } = 100;

    /// <summary>
    /// Gets or sets the path to the SQLite cache database file.
    /// </summary>
    /// <remarks>
    /// If a relative path is specified, it is resolved relative to the
    /// application's data directory. Default is "embedding_cache.db".
    /// </remarks>
    public string CachePath { get; set; } = "embedding_cache.db";

    /// <summary>
    /// Gets or sets the expected embedding dimensions.
    /// </summary>
    /// <remarks>
    /// Used for storage size estimation. Should match the embedding model's
    /// output dimensions. Default is 1536 (text-embedding-3-small).
    /// </remarks>
    public int EmbeddingDimensions { get; set; } = 1536;

    /// <summary>
    /// Gets or sets the compaction check interval.
    /// </summary>
    /// <remarks>
    /// The cache checks for compaction needs at this interval.
    /// Compaction removes entries if the cache exceeds <see cref="MaxSizeMB"/>.
    /// Default is 1 hour.
    /// </remarks>
    public TimeSpan CompactionInterval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets the default configuration options.
    /// </summary>
    public static EmbeddingCacheOptions Default => new();

    /// <summary>
    /// Gets the maximum size in bytes.
    /// </summary>
    public long MaxSizeBytes => MaxSizeMB * 1024L * 1024L;

    /// <summary>
    /// Estimates the size in bytes for a single embedding entry.
    /// </summary>
    /// <remarks>
    /// Each float is 4 bytes, plus approximately 100 bytes overhead for
    /// the hash and metadata.
    /// </remarks>
    public int EstimatedEntrySize => (EmbeddingDimensions * sizeof(float)) + 100;
}
