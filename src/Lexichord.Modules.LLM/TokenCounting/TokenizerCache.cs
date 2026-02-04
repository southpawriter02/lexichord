// -----------------------------------------------------------------------
// <copyright file="TokenizerCache.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Thread-safe cache for tokenizer instances.
//   - Uses ConcurrentDictionary with Lazy<T> for thread-safe lazy initialization.
//   - Normalizes model names to cache keys for efficient reuse.
//   - Models in the same family share tokenizer instances (e.g., all gpt-4o variants).
//   - Provides cache management methods (Clear, Count).
//   - Logs cache operations for observability.
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using Lexichord.Modules.LLM.Logging;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.LLM.TokenCounting;

/// <summary>
/// Thread-safe cache for tokenizer instances.
/// </summary>
/// <remarks>
/// <para>
/// This class provides efficient caching of <see cref="ITokenizer"/> instances
/// to avoid repeated initialization of expensive tokenizers. Models within the
/// same family share tokenizer instances through normalized cache keys.
/// </para>
/// <para>
/// <b>Cache Key Normalization:</b>
/// </para>
/// <list type="bullet">
///   <item><description>"gpt-4o", "gpt-4o-mini" → "gpt-4o" (shared o200k_base tokenizer)</description></item>
///   <item><description>"gpt-4", "gpt-4-turbo" → "gpt-4" (shared cl100k_base tokenizer)</description></item>
///   <item><description>"gpt-3.5-turbo" variants → "gpt-3.5" (shared cl100k_base tokenizer)</description></item>
///   <item><description>"claude-3-*" variants → "claude" (shared approximate tokenizer)</description></item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> This class is fully thread-safe. All operations use
/// <see cref="ConcurrentDictionary{TKey,TValue}"/> with <see cref="Lazy{T}"/>
/// to ensure exactly-once initialization under concurrent access.
/// </para>
/// </remarks>
internal sealed class TokenizerCache
{
    private readonly ConcurrentDictionary<string, Lazy<ITokenizer>> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<TokenizerCache> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenizerCache"/> class.
    /// </summary>
    /// <param name="logger">
    /// The logger instance for diagnostic output. Must not be <c>null</c>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="logger"/> is <c>null</c>.
    /// </exception>
    public TokenizerCache(ILogger<TokenizerCache> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets or creates a tokenizer for the given model.
    /// </summary>
    /// <param name="model">
    /// The model name (e.g., "gpt-4o", "claude-3-haiku-20240307").
    /// Must not be <c>null</c> or whitespace.
    /// </param>
    /// <param name="factory">
    /// Factory function to create the tokenizer if not already cached.
    /// Must not be <c>null</c>.
    /// </param>
    /// <returns>
    /// The cached or newly created <see cref="ITokenizer"/> instance.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="model"/> is <c>null</c>, empty, or whitespace.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="factory"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The model name is normalized to a cache key to enable sharing between
    /// models in the same family. For example, "gpt-4o-mini" and "gpt-4o" both
    /// map to the "gpt-4o" cache key and share the same tokenizer instance.
    /// </para>
    /// <para>
    /// <b>Thread Safety:</b> This method is thread-safe. Under concurrent access
    /// for the same model, the factory is invoked exactly once.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var cache = new TokenizerCache(logger);
    /// var factory = new TokenizerFactory(logger);
    ///
    /// // First call: creates and caches the tokenizer
    /// var tokenizer1 = cache.GetOrCreate("gpt-4o-mini", () => factory.CreateForModel("gpt-4o-mini"));
    ///
    /// // Second call: returns cached tokenizer (same instance)
    /// var tokenizer2 = cache.GetOrCreate("gpt-4o", () => factory.CreateForModel("gpt-4o"));
    ///
    /// // tokenizer1 and tokenizer2 are the same instance
    /// </code>
    /// </example>
    public ITokenizer GetOrCreate(string model, Func<ITokenizer> factory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model, nameof(model));
        ArgumentNullException.ThrowIfNull(factory, nameof(factory));

        var key = NormalizeModelKey(model);

        var lazy = _cache.GetOrAdd(key, _ =>
        {
            LLMLogEvents.TokenizerCacheCreating(_logger, key, model);
            return new Lazy<ITokenizer>(factory, LazyThreadSafetyMode.ExecutionAndPublication);
        });

        var tokenizer = lazy.Value;
        LLMLogEvents.TokenizerCacheHit(_logger, key);

        return tokenizer;
    }

    /// <summary>
    /// Clears all cached tokenizer instances.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method removes all entries from the cache. Subsequent calls to
    /// <see cref="GetOrCreate"/> will create new tokenizer instances.
    /// </para>
    /// <para>
    /// <b>Use Case:</b> This method is primarily useful for testing or when
    /// reconfiguration of tokenizers is needed.
    /// </para>
    /// </remarks>
    public void Clear()
    {
        var count = _cache.Count;
        _cache.Clear();
        LLMLogEvents.TokenizerCacheCleared(_logger, count);
    }

    /// <summary>
    /// Gets the number of cached tokenizer instances.
    /// </summary>
    /// <value>
    /// The current number of entries in the cache.
    /// </value>
    /// <remarks>
    /// This count reflects the number of unique model families that have been
    /// accessed, not the total number of model requests.
    /// </remarks>
    public int Count => _cache.Count;

    /// <summary>
    /// Checks whether a tokenizer is cached for the given model.
    /// </summary>
    /// <param name="model">
    /// The model name to check. May be <c>null</c> or whitespace.
    /// </param>
    /// <returns>
    /// <c>true</c> if a tokenizer is cached for this model's family;
    /// <c>false</c> otherwise or if the model name is invalid.
    /// </returns>
    public bool ContainsKey(string model)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            return false;
        }

        var key = NormalizeModelKey(model);
        return _cache.ContainsKey(key);
    }

    /// <summary>
    /// Normalizes a model name to a cache key for tokenizer reuse.
    /// </summary>
    /// <param name="model">The model name to normalize.</param>
    /// <returns>The normalized cache key.</returns>
    /// <remarks>
    /// <para>
    /// Models within the same family share tokenizers because they use the same
    /// encoding scheme. This normalization groups models appropriately:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>gpt-4o-* → "gpt-4o" (o200k_base)</description></item>
    ///   <item><description>gpt-4-* → "gpt-4" (cl100k_base)</description></item>
    ///   <item><description>gpt-3.5-* → "gpt-3.5" (cl100k_base)</description></item>
    ///   <item><description>claude-* → "claude" (approximation)</description></item>
    ///   <item><description>Others → lowercase model name</description></item>
    /// </list>
    /// </remarks>
    private static string NormalizeModelKey(string model)
    {
        var lower = model.ToLowerInvariant();

        // LOGIC: Group GPT-4o models together (o200k_base encoding).
        if (lower.StartsWith("gpt-4o"))
        {
            return "gpt-4o";
        }

        // LOGIC: Group other GPT-4 models together (cl100k_base encoding).
        if (lower.StartsWith("gpt-4"))
        {
            return "gpt-4";
        }

        // LOGIC: Group GPT-3.5 models together (cl100k_base encoding).
        if (lower.StartsWith("gpt-3.5"))
        {
            return "gpt-3.5";
        }

        // LOGIC: Group Claude models together (approximation).
        if (lower.StartsWith("claude"))
        {
            return "claude";
        }

        // LOGIC: Return lowercase model name for unknown models.
        return lower;
    }
}
