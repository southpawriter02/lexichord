namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Configuration options for the Terminology Repository cache.
/// </summary>
/// <remarks>
/// LOGIC: Configures the IMemoryCache behavior for cached terminology queries.
/// 
/// Default values are optimized for:
/// - Low memory overhead (single HashSet)
/// - Reasonable refresh for active development
/// - Sub-millisecond response for document analysis
/// </remarks>
public record TerminologyCacheOptions
{
    /// <summary>
    /// Configuration section name for IOptions binding.
    /// </summary>
    public const string SectionName = "TerminologyCache";

    /// <summary>
    /// Cache key for the active terms HashSet.
    /// </summary>
    public string ActiveTermsCacheKey { get; init; } = "Terminology:ActiveTerms";

    /// <summary>
    /// Cache key for the fuzzy-enabled terms list.
    /// </summary>
    public string FuzzyTermsCacheKey { get; init; } = "Terminology:FuzzyTerms";

    /// <summary>
    /// Sliding expiration duration in minutes.
    /// </summary>
    /// <remarks>
    /// Sliding expiration means the cache resets on each access.
    /// Default: 60 minutes (1 hour).
    /// </remarks>
    public int CacheDurationMinutes { get; init; } = 60;

    /// <summary>
    /// Maximum absolute expiration in minutes (safety cap).
    /// </summary>
    /// <remarks>
    /// Prevents indefinite caching even with frequent access.
    /// Default: 240 minutes (4 hours).
    /// </remarks>
    public int MaxCacheDurationMinutes { get; init; } = 240;
}
