// =============================================================================
// File: CacheKeyGenerator.cs
// Project: Lexichord.Modules.RAG
// Description: Generates deterministic cache keys from search parameters.
// Version: v0.5.8c
// =============================================================================
// LOGIC: Creates reproducible SHA256 hash keys from normalized query text,
//   search options, and filters. Same inputs always produce the same key.
// =============================================================================

using System.Security.Cryptography;
using System.Text;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// Generates deterministic cache keys from search parameters.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="CacheKeyGenerator"/> creates reproducible SHA256 hash keys by combining
/// normalized query text with search options. This ensures that identical
/// searches produce identical cache keys.
/// </para>
/// <para>
/// <b>Normalization:</b>
/// <list type="bullet">
///   <item>Query text is lowercased and trimmed.</item>
///   <item>Options are serialized in a deterministic format.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.8c as part of the Multi-Layer Caching System.
/// </para>
/// </remarks>
public sealed class CacheKeyGenerator
{
    private readonly ILogger<CacheKeyGenerator> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="CacheKeyGenerator"/>.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="logger"/> is null.
    /// </exception>
    public CacheKeyGenerator(ILogger<CacheKeyGenerator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates a cache key from query and search options.
    /// </summary>
    /// <param name="query">The search query text.</param>
    /// <param name="options">The search options.</param>
    /// <returns>A SHA256 hash string suitable for use as a cache key.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="query"/> or <paramref name="options"/> is null.
    /// </exception>
    public string GenerateKey(string query, SearchOptions options)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(options);

        // LOGIC: Normalize query - lowercase and trim whitespace
        var normalizedQuery = query.Trim().ToLowerInvariant();

        // LOGIC: Build a deterministic string representation
        var keyBuilder = new StringBuilder();
        keyBuilder.Append("q:");
        keyBuilder.Append(normalizedQuery);
        keyBuilder.Append("|k:");
        keyBuilder.Append(options.TopK);
        keyBuilder.Append("|s:");
        keyBuilder.Append(options.MinScore.ToString("F4"));
        keyBuilder.Append("|e:");
        keyBuilder.Append(options.ExpandAbbreviations ? "1" : "0");
        
        // LOGIC: Include document filter if present
        if (options.DocumentFilter.HasValue)
        {
            keyBuilder.Append("|d:");
            keyBuilder.Append(options.DocumentFilter.Value.ToString("N"));
        }

        var keySource = keyBuilder.ToString();
        var hash = ComputeHash(keySource);

        _logger.LogDebug(
            "Generated cache key {Key} from query '{Query}' with TopK={TopK}, MinScore={MinScore}",
            TruncateHash(hash), normalizedQuery, options.TopK, options.MinScore);

        return hash;
    }

    /// <summary>
    /// Generates a cache key from query text only (minimal configuration).
    /// </summary>
    /// <param name="query">The search query text.</param>
    /// <returns>A SHA256 hash string suitable for use as a cache key.</returns>
    public string GenerateKey(string query)
    {
        return GenerateKey(query, SearchOptions.Default);
    }

    /// <summary>
    /// Computes the SHA-256 hash of the given content.
    /// </summary>
    /// <param name="content">The content to hash.</param>
    /// <returns>A lowercase hexadecimal hash string.</returns>
    private static string ComputeHash(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Truncates a hash for logging purposes.
    /// </summary>
    private static string TruncateHash(string hash) =>
        hash.Length <= 12 ? hash : hash[..12] + "...";
}

