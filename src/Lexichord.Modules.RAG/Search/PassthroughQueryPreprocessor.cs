// =============================================================================
// File: PassthroughQueryPreprocessor.cs
// Project: Lexichord.Modules.RAG
// Description: Temporary no-op implementation of IQueryPreprocessor for v0.4.5b.
// =============================================================================
// LOGIC: Provides a minimal passthrough implementation that allows
//   PgVectorSearchService to function before the full QueryPreprocessor
//   is delivered in v0.4.5c. This stub:
//   - Returns the trimmed query without abbreviation expansion or Unicode normalization.
//   - Has no caching (always returns null for cache lookups, no-op for cache writes).
//   - Will be replaced by the full QueryPreprocessor registration in v0.4.5c.
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.Search;

/// <summary>
/// Temporary passthrough implementation of <see cref="IQueryPreprocessor"/>
/// that performs minimal query processing without caching.
/// </summary>
/// <remarks>
/// <para>
/// This stub implementation is registered by <see cref="RAGModule"/> in v0.4.5b
/// to satisfy the <see cref="PgVectorSearchService"/> dependency on
/// <see cref="IQueryPreprocessor"/>. It will be replaced by the full
/// <c>QueryPreprocessor</c> implementation in v0.4.5c, which adds:
/// </para>
/// <list type="bullet">
///   <item><description>Unicode NFC normalization.</description></item>
///   <item><description>Abbreviation expansion with a lookup table.</description></item>
///   <item><description>5-minute query embedding cache via <c>IMemoryCache</c>.</description></item>
/// </list>
/// <para>
/// <b>Introduced:</b> v0.4.5b (temporary). Replaced by v0.4.5c.
/// </para>
/// </remarks>
internal sealed class PassthroughQueryPreprocessor : IQueryPreprocessor
{
    private readonly ILogger<PassthroughQueryPreprocessor> _logger;

    /// <summary>
    /// Creates a new <see cref="PassthroughQueryPreprocessor"/> instance.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="logger"/> is null.
    /// </exception>
    public PassthroughQueryPreprocessor(ILogger<PassthroughQueryPreprocessor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Passthrough implementation trims whitespace only.
    /// Full normalization and abbreviation expansion deferred to v0.4.5c.
    /// </remarks>
    public string Process(string query, SearchOptions options)
    {
        if (string.IsNullOrWhiteSpace(query))
            return string.Empty;

        var processed = query.Trim();

        _logger.LogDebug(
            "Passthrough preprocessor: '{Original}' -> '{Processed}' (full preprocessing in v0.4.5c)",
            query, processed);

        return processed;
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: No caching in passthrough implementation. Always returns null
    /// to force embedding generation. Full caching deferred to v0.4.5c.
    /// </remarks>
    public float[]? GetCachedEmbedding(string query)
    {
        // No caching in passthrough implementation.
        return null;
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: No-op in passthrough implementation. Embeddings are not cached.
    /// Full caching with 5-minute TTL deferred to v0.4.5c.
    /// </remarks>
    public void CacheEmbedding(string query, float[] embedding)
    {
        // No-op: caching deferred to v0.4.5c QueryPreprocessor.
    }
}
