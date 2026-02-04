// =============================================================================
// File: QueryCacheOptions.cs
// Project: Lexichord.Modules.RAG
// Description: Configuration options for the query result cache.
// Version: v0.5.8c
// =============================================================================
// LOGIC: Configures cache behavior including capacity, TTL, and enable toggle.
// =============================================================================

namespace Lexichord.Modules.RAG.Configuration;

/// <summary>
/// Configuration options for <see cref="Services.QueryResultCache"/>.
/// </summary>
/// <remarks>
/// <para>
/// These options control the behavior of the global query result cache:
/// </para>
/// <list type="bullet">
///   <item><see cref="MaxEntries"/>: Maximum cached results before LRU eviction.</item>
///   <item><see cref="TtlSeconds"/>: Time-to-live for each cache entry.</item>
///   <item><see cref="Enabled"/>: Master switch to enable/disable caching.</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.5.8c as part of the Multi-Layer Caching System.
/// </para>
/// </remarks>
public sealed class QueryCacheOptions
{
    /// <summary>
    /// The configuration section name for binding.
    /// </summary>
    public const string SectionName = "RAG:QueryCache";

    /// <summary>
    /// Gets or sets the maximum number of cached entries before LRU eviction.
    /// </summary>
    /// <value>Default is 100.</value>
    /// <remarks>
    /// LOGIC: When this limit is exceeded, the least recently accessed entry is removed.
    /// </remarks>
    public int MaxEntries { get; set; } = 100;

    /// <summary>
    /// Gets or sets the time-to-live for cached entries in seconds.
    /// </summary>
    /// <value>Default is 300 (5 minutes).</value>
    /// <remarks>
    /// LOGIC: Entries older than this are considered expired and not returned.
    /// Expired entries are lazily removed on access.
    /// </remarks>
    public int TtlSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets whether the cache is enabled.
    /// </summary>
    /// <value>Default is <c>true</c>.</value>
    /// <remarks>
    /// When disabled, <c>TryGet</c> always returns false and <c>Set</c> is a no-op.
    /// </remarks>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets the TTL as a <see cref="TimeSpan"/>.
    /// </summary>
    public TimeSpan TtlTimeSpan => TimeSpan.FromSeconds(TtlSeconds);
}
