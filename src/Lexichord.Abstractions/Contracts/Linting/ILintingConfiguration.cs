namespace Lexichord.Abstractions.Contracts.Linting;

/// <summary>
/// Configuration interface for linting behavior.
/// </summary>
/// <remarks>
/// LOGIC: Abstracts linting configuration to enable testing with
/// custom values and future support for per-document or workspace
/// configuration overrides.
///
/// Version: v0.2.3c
/// </remarks>
public interface ILintingConfiguration
{
    /// <summary>
    /// Debounce delay in milliseconds between content changes and scan trigger.
    /// </summary>
    /// <remarks>
    /// LOGIC: Prevents excessive scans during rapid typing.
    /// Range: 100-2000ms, Default: 300ms
    /// </remarks>
    int DebounceMilliseconds { get; }

    /// <summary>
    /// Maximum number of concurrent document scans.
    /// </summary>
    /// <remarks>
    /// LOGIC: Limits CPU pressure when many documents are open.
    /// Range: 1-8, Default: 2
    /// </remarks>
    int MaxConcurrentScans { get; }

    /// <summary>
    /// Whether linting is globally enabled.
    /// </summary>
    /// <remarks>
    /// LOGIC: Master switch for disabling all background linting.
    /// </remarks>
    bool Enabled { get; }

    /// <summary>
    /// Maximum number of compiled patterns to cache.
    /// </summary>
    /// <remarks>
    /// LOGIC: LRU cache for compiled regex patterns.
    /// Larger cache improves hit ratio but uses more memory.
    /// Range: 100-2000, Default: 500
    ///
    /// Version: v0.2.3c
    /// </remarks>
    int PatternCacheMaxSize { get; }

    /// <summary>
    /// Timeout in milliseconds for pattern matching operations.
    /// </summary>
    /// <remarks>
    /// LOGIC: ReDoS protection - aborts patterns that take too long.
    /// If a pattern times out, it returns empty matches.
    /// Range: 50-1000ms, Default: 100ms
    ///
    /// Version: v0.2.3c
    /// </remarks>
    int PatternTimeoutMilliseconds { get; }

    /// <summary>
    /// Whether to perform complexity analysis on regex patterns.
    /// </summary>
    /// <remarks>
    /// LOGIC: Optional heuristic check for known ReDoS patterns.
    /// When enabled, dangerous patterns are flagged before execution.
    /// Default: true
    ///
    /// Version: v0.2.3c
    /// </remarks>
    bool UseComplexityAnalysis { get; }

    /// <summary>
    /// Maximum number of violations to report per document.
    /// </summary>
    /// <remarks>
    /// LOGIC: Prevents memory exhaustion on very noisy documents.
    /// Violations beyond this limit are silently dropped after sorting
    /// by position, ensuring the most prominent issues are reported.
    /// Range: 100-10000, Default: 1000
    ///
    /// Version: v0.2.3d
    /// </remarks>
    int MaxViolationsPerDocument { get; }
}

