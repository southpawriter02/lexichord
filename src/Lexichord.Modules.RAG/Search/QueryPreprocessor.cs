// =============================================================================
// File: QueryPreprocessor.cs
// Project: Lexichord.Modules.RAG
// Description: Full query preprocessing implementation with normalization,
//              abbreviation expansion, and embedding caching.
// =============================================================================
// LOGIC: Implements IQueryPreprocessor with the complete preprocessing pipeline:
//
//   Processing Pipeline (Process method):
//     1. Null/whitespace guard → return empty string.
//     2. Trim leading and trailing whitespace.
//     3. Collapse multiple whitespace characters to a single space.
//     4. Unicode NFC normalization for consistent character representation.
//     5. Optional abbreviation expansion via word boundary regex matching.
//
//   Embedding Cache:
//     - SHA256-based cache keys (stable across runtimes, unlike GetHashCode).
//     - 5-minute sliding expiration via MemoryCacheEntryOptions.
//     - Case-insensitive: keys derived from lowercased query text.
//     - First 16 hex characters of SHA256 hash used as compact key suffix.
//
//   Abbreviation Expansion:
//     - 35+ common technical abbreviations mapped to full forms.
//     - Format: "ABBR (Full Form)" — original abbreviation preserved.
//     - Word boundary matching (\b) prevents partial replacements.
//     - Double-expansion guard prevents re-expanding already-expanded text.
//     - Controlled by SearchOptions.ExpandAbbreviations (default: false).
//
//   Dependencies:
//     - Microsoft.Extensions.Caching.Memory (IMemoryCache) for embedding cache.
//     - v0.4.5a: SearchOptions for expansion/cache configuration.
//
//   Replaces: PassthroughQueryPreprocessor (v0.4.5b temporary stub).
//   Introduced: v0.4.5c.
// =============================================================================

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.Search;

/// <summary>
/// Query preprocessor with normalization, abbreviation expansion, and embedding caching.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="QueryPreprocessor"/> transforms raw user search queries into a
/// normalized form suitable for consistent embedding generation. This ensures that
/// semantically identical queries (differing only in whitespace, Unicode encoding,
/// or abbreviation usage) produce the same embedding vectors.
/// </para>
/// <para>
/// <b>Processing Pipeline:</b>
/// </para>
/// <list type="number">
///   <item><description>Trim leading and trailing whitespace.</description></item>
///   <item><description>Collapse multiple whitespace characters to single spaces.</description></item>
///   <item><description>Apply Unicode NFC normalization for canonical character forms.</description></item>
///   <item><description>Optionally expand abbreviations (e.g., "API" → "API (Application Programming Interface)").</description></item>
/// </list>
/// <para>
/// <b>Embedding Caching:</b> Query embeddings are cached using SHA256-based keys
/// derived from the lowercased processed query text. Cache entries have a 5-minute
/// sliding expiration to balance memory usage with API cost savings for repeated queries.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This class is thread-safe. <see cref="IMemoryCache"/> is
/// inherently thread-safe, and the <see cref="Abbreviations"/> dictionary is read-only
/// after static initialization. The <see cref="Process"/> method operates on local
/// variables with no shared mutable state.
/// </para>
/// <para>
/// <b>Introduced:</b> v0.4.5c. Replaces <see cref="PassthroughQueryPreprocessor"/> (v0.4.5b).
/// </para>
/// </remarks>
internal sealed class QueryPreprocessor : IQueryPreprocessor
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<QueryPreprocessor> _logger;
    private readonly MemoryCacheEntryOptions _cacheOptions;

    /// <summary>
    /// Cache key prefix for query embeddings.
    /// </summary>
    /// <remarks>
    /// LOGIC: Prefix ensures query embedding cache entries are namespaced and
    /// do not collide with other IMemoryCache consumers sharing the same instance.
    /// </remarks>
    private const string CacheKeyPrefix = "query_embedding:";

    /// <summary>
    /// Default cache expiration time for query embeddings.
    /// </summary>
    /// <remarks>
    /// LOGIC: 5-minute sliding expiration balances memory usage with API cost savings.
    /// Active queries stay cached; idle queries expire naturally without manual eviction.
    /// </remarks>
    private static readonly TimeSpan DefaultCacheExpiry = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Common abbreviation expansions for technical writing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// LOGIC: Abbreviation expansion improves embedding quality by providing the
    /// full semantic context for common acronyms. For example, "API docs" is expanded
    /// to "API (Application Programming Interface) docs", giving the embedding model
    /// richer input text.
    /// </para>
    /// <para>
    /// The dictionary uses <see cref="StringComparer.OrdinalIgnoreCase"/> for
    /// case-insensitive lookups during expansion.
    /// </para>
    /// </remarks>
    private static readonly Dictionary<string, string> Abbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        // Programming & Development
        ["API"] = "Application Programming Interface",
        ["SDK"] = "Software Development Kit",
        ["IDE"] = "Integrated Development Environment",
        ["CLI"] = "Command Line Interface",
        ["GUI"] = "Graphical User Interface",
        ["UI"] = "User Interface",
        ["UX"] = "User Experience",

        // Data & Databases
        ["DB"] = "Database",
        ["SQL"] = "Structured Query Language",
        ["NoSQL"] = "Not Only SQL",
        ["JSON"] = "JavaScript Object Notation",
        ["XML"] = "Extensible Markup Language",
        ["CSV"] = "Comma Separated Values",
        ["ORM"] = "Object Relational Mapping",
        ["CRUD"] = "Create Read Update Delete",

        // Web & Networking
        ["HTML"] = "HyperText Markup Language",
        ["CSS"] = "Cascading Style Sheets",
        ["HTTP"] = "HyperText Transfer Protocol",
        ["HTTPS"] = "HTTP Secure",
        ["REST"] = "Representational State Transfer",
        ["URL"] = "Uniform Resource Locator",
        ["DNS"] = "Domain Name System",

        // Architecture & Patterns
        ["DI"] = "Dependency Injection",
        ["IoC"] = "Inversion of Control",
        ["MVC"] = "Model View Controller",
        ["MVVM"] = "Model View ViewModel",
        ["SOLID"] = "Single responsibility Open closed Liskov substitution Interface segregation Dependency inversion",
        ["DRY"] = "Don't Repeat Yourself",

        // Process & Methodology
        ["TDD"] = "Test Driven Development",
        ["BDD"] = "Behavior Driven Development",
        ["CI"] = "Continuous Integration",
        ["CD"] = "Continuous Deployment",
        ["MVP"] = "Minimum Viable Product",
        ["POC"] = "Proof of Concept",
        ["QA"] = "Quality Assurance",

        // AI & ML
        ["AI"] = "Artificial Intelligence",
        ["ML"] = "Machine Learning",
        ["NLP"] = "Natural Language Processing",
        ["LLM"] = "Large Language Model",
        ["RAG"] = "Retrieval Augmented Generation"
    };

    /// <summary>
    /// Creates a new <see cref="QueryPreprocessor"/> instance.
    /// </summary>
    /// <param name="cache">
    /// Memory cache for storing query embeddings. Registered via
    /// <c>services.AddMemoryCache()</c> in <see cref="RAGModule"/>.
    /// </param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="cache"/> or <paramref name="logger"/> is null.
    /// </exception>
    public QueryPreprocessor(
        IMemoryCache cache,
        ILogger<QueryPreprocessor> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // LOGIC: Pre-create cache entry options to avoid allocation on every CacheEmbedding call.
        // Sliding expiration resets the 5-minute window on each cache access.
        _cacheOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = DefaultCacheExpiry
        };
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// LOGIC: Processing pipeline applies transformations in a fixed order:
    /// </para>
    /// <list type="number">
    ///   <item><description>Trim → removes leading/trailing whitespace.</description></item>
    ///   <item><description>CollapseWhitespace → multiple spaces/tabs/newlines become single space.</description></item>
    ///   <item><description>Unicode NFC → canonical decomposition followed by canonical composition.
    ///     This ensures that "café" (e + combining accent) and "café" (precomposed) are identical.</description></item>
    ///   <item><description>ExpandAbbreviations → controlled by <see cref="SearchOptions.ExpandAbbreviations"/>.
    ///     Disabled by default to preserve original query intent.</description></item>
    /// </list>
    /// </remarks>
    public string Process(string query, SearchOptions options)
    {
        if (string.IsNullOrWhiteSpace(query))
            return string.Empty;

        var original = query;

        // Step 1: Trim leading/trailing whitespace.
        var processed = query.Trim();

        // Step 2: Collapse multiple whitespace characters to single space.
        processed = CollapseWhitespace(processed);

        // Step 3: Unicode normalization (NFC form).
        // LOGIC: NFC (Canonical Decomposition, followed by Canonical Composition) ensures
        // that characters with equivalent Unicode representations are normalized to a
        // single canonical form. This is critical for consistent cache key generation
        // and embedding quality.
        processed = processed.Normalize(NormalizationForm.FormC);

        // Step 4: Optional abbreviation expansion.
        if (options.ExpandAbbreviations)
        {
            processed = ExpandAbbreviations(processed);
        }

        // LOGIC: Only log when the query was actually modified to reduce noise.
        if (processed != original)
        {
            _logger.LogDebug(
                "Preprocessed query: '{Original}' -> '{Processed}'",
                original, processed);
        }

        return processed;
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Cache lookup uses a SHA256-derived key for deterministic,
    /// case-insensitive matching. Cache hits avoid an embedding API call,
    /// saving latency and cost for repeated queries.
    /// </remarks>
    public float[]? GetCachedEmbedding(string query)
    {
        var key = GetCacheKey(query);

        if (_cache.TryGetValue(key, out float[]? embedding))
        {
            _logger.LogDebug("Cache hit for query: '{Query}'", TruncateForLog(query));
            return embedding;
        }

        return null;
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Stores the embedding with a 5-minute sliding expiration.
    /// The sliding window resets each time the entry is accessed via
    /// <see cref="GetCachedEmbedding"/>, keeping actively-used entries alive.
    /// </remarks>
    public void CacheEmbedding(string query, float[] embedding)
    {
        var key = GetCacheKey(query);
        _cache.Set(key, embedding, _cacheOptions);

        _logger.LogDebug(
            "Cached embedding for query: '{Query}' ({Dimensions} dimensions)",
            TruncateForLog(query), embedding.Length);
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: IMemoryCache does not expose a native "clear all entries" API.
    /// Entries expire automatically via the 5-minute sliding window configured
    /// in <see cref="_cacheOptions"/>. This method logs the clear request for
    /// observability but does not force eviction.
    /// </remarks>
    public void ClearCache()
    {
        // LOGIC: IMemoryCache has no Clear() method. Entries will expire
        // naturally via the sliding expiration window (5 minutes).
        _logger.LogInformation("Cache clear requested (entries will expire naturally)");
    }

    /// <summary>
    /// Collapses multiple whitespace characters into a single space.
    /// </summary>
    /// <param name="text">The input text to normalize.</param>
    /// <returns>Text with all whitespace sequences replaced by single spaces.</returns>
    /// <remarks>
    /// LOGIC: The regex <c>\s+</c> matches one or more whitespace characters
    /// (spaces, tabs, newlines, etc.) and replaces them with a single space.
    /// This ensures consistent tokenization regardless of input formatting.
    /// </remarks>
    private static string CollapseWhitespace(string text)
    {
        return Regex.Replace(text, @"\s+", " ");
    }

    /// <summary>
    /// Expands known abbreviations with their full forms.
    /// </summary>
    /// <param name="text">The input text containing potential abbreviations.</param>
    /// <returns>Text with abbreviations expanded in "ABBR (Full Form)" format.</returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Expansion preserves the original abbreviation and appends the full form
    /// in parentheses. This provides richer context to the embedding model while
    /// maintaining readability.
    /// </para>
    /// <para>
    /// <b>Word Boundary Matching:</b> The regex pattern <c>\bABBR\b</c> ensures only
    /// whole-word matches are expanded, preventing partial matches (e.g., "RAPID" would
    /// not match the "API" abbreviation).
    /// </para>
    /// <para>
    /// <b>Double-Expansion Guard:</b> Before each replacement, the method checks whether
    /// the expansion text already exists in the string. This prevents
    /// "API (Application Programming Interface) (Application Programming Interface)"
    /// if the method is called multiple times or if one abbreviation's expansion
    /// contains another abbreviation.
    /// </para>
    /// </remarks>
    private static string ExpandAbbreviations(string text)
    {
        foreach (var (abbrev, expansion) in Abbreviations)
        {
            // LOGIC: Match whole words only using word boundaries.
            var pattern = $@"\b{Regex.Escape(abbrev)}\b";

            // LOGIC: Double-expansion guard — only expand if the full form
            // is not already present in the text.
            if (!text.Contains($"({expansion})", StringComparison.OrdinalIgnoreCase))
            {
                text = Regex.Replace(
                    text,
                    pattern,
                    $"{abbrev} ({expansion})",
                    RegexOptions.IgnoreCase);
            }
        }

        return text;
    }

    /// <summary>
    /// Generates a deterministic cache key for a query string.
    /// </summary>
    /// <param name="query">The processed query text.</param>
    /// <returns>A cache key in the format <c>query_embedding:{hash16}</c>.</returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Uses SHA256 hashing instead of <see cref="string.GetHashCode()"/>
    /// because GetHashCode is not guaranteed to be stable across .NET runtimes
    /// or process restarts. SHA256 produces consistent hashes regardless of
    /// runtime environment.
    /// </para>
    /// <para>
    /// The query is lowercased before hashing to enable case-insensitive cache
    /// lookups. Only the first 16 hex characters of the hash are used as a
    /// compact yet collision-resistant key suffix.
    /// </para>
    /// </remarks>
    private static string GetCacheKey(string query)
    {
        var hash = ComputeStableHash(query.ToLowerInvariant());
        return $"{CacheKeyPrefix}{hash}";
    }

    /// <summary>
    /// Computes a stable SHA256 hash for a string, returning the first 16 hex characters.
    /// </summary>
    /// <param name="input">The string to hash.</param>
    /// <returns>The first 16 hex characters of the SHA256 hash.</returns>
    /// <remarks>
    /// LOGIC: SHA256 provides deterministic hashing across all .NET runtimes and
    /// process restarts. The first 16 hex characters (64 bits) provide sufficient
    /// collision resistance for an in-memory cache with a 5-minute TTL and
    /// low cardinality (typically fewer than 1,000 unique queries in a session).
    /// </remarks>
    private static string ComputeStableHash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash)[..16];
    }

    /// <summary>
    /// Truncates a string for safe logging to prevent oversized log entries.
    /// </summary>
    /// <param name="text">The text to truncate.</param>
    /// <param name="maxLength">Maximum allowed length before truncation (default: 50).</param>
    /// <returns>The original text if within bounds, or a truncated version with "..." suffix.</returns>
    /// <remarks>
    /// LOGIC: Long query strings in log messages can bloat log storage and reduce
    /// readability. This helper ensures query text is truncated at 50 characters
    /// by default, with a "..." suffix indicating truncation occurred.
    /// </remarks>
    private static string TruncateForLog(string text, int maxLength = 50)
    {
        return text.Length <= maxLength
            ? text
            : text[..maxLength] + "...";
    }
}
