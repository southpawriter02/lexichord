using System.Diagnostics;
using System.Text.RegularExpressions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Linting;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Style.Services.Linting;

/// <summary>
/// Pattern matching engine with caching and ReDoS protection.
/// </summary>
/// <remarks>
/// LOGIC: Implements IScannerService with:
/// - LRU caching of compiled regex patterns
/// - Configurable timeout for ReDoS protection
/// - Optional complexity analysis for dangerous patterns
/// - Batch scanning optimization
///
/// Threading:
/// - All public methods are thread-safe
/// - Pattern cache uses ConcurrentDictionary internally
/// - Statistics use Interlocked operations
///
/// Version: v0.2.3c
/// </remarks>
internal sealed class ScannerService : IScannerService
{
    private readonly ILintingConfiguration _config;
    private readonly ILogger<ScannerService> _logger;
    private readonly PatternCache<string, CompiledPattern> _cache;

    private long _totalScans;

    /// <summary>
    /// Initializes a new scanner service.
    /// </summary>
    /// <param name="config">Linting configuration.</param>
    /// <param name="logger">Logger instance.</param>
    public ScannerService(ILintingConfiguration config, ILogger<ScannerService> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = new PatternCache<string, CompiledPattern>(_config.PatternCacheMaxSize);

        _logger.LogDebug(
            "ScannerService initialized with cache size {MaxSize}, timeout {TimeoutMs}ms, complexity analysis {Enabled}",
            _config.PatternCacheMaxSize,
            _config.PatternTimeoutMilliseconds,
            _config.UseComplexityAnalysis);
    }

    /// <inheritdoc />
    public Task<ScannerResult> ScanAsync(
        string content,
        StyleRule rule,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rule);

        Interlocked.Increment(ref _totalScans);

        // Return early for disabled rules or empty content
        if (!rule.IsEnabled || string.IsNullOrEmpty(content) || string.IsNullOrEmpty(rule.Pattern))
        {
            return Task.FromResult(ScannerResult.Empty(rule.Id));
        }

        var stopwatch = Stopwatch.StartNew();

        // Only regex patterns use the cache; other pattern types use direct matching
        if (rule.PatternType != PatternType.Regex)
        {
            var matches = ScanNonRegex(content, rule);
            stopwatch.Stop();
            return Task.FromResult(new ScannerResult(
                rule.Id,
                matches,
                stopwatch.Elapsed,
                WasCacheHit: false));
        }

        // Check complexity analysis if enabled
        if (_config.UseComplexityAnalysis)
        {
            var analysis = PatternComplexityAnalyzer.Analyze(rule.Pattern);
            if (analysis.IsDangerous)
            {
                _logger.LogWarning(
                    "Pattern {RuleId} blocked due to ReDoS risk: {Reason}",
                    rule.Id, analysis.Reason);
                stopwatch.Stop();
                return Task.FromResult(ScannerResult.Empty(rule.Id));
            }
        }

        // Get or compile the pattern
        var cacheKey = GetCacheKey(rule);
        var wasCacheHit = _cache.TryGet(cacheKey, out var compiled);

        if (!wasCacheHit || compiled is null)
        {
            compiled = CompilePattern(rule);
            if (compiled is null)
            {
                stopwatch.Stop();
                return Task.FromResult(ScannerResult.Empty(rule.Id));
            }
            _cache.Set(cacheKey, compiled);
        }

        // Execute the scan
        var scanMatches = ExecuteScan(content, compiled, cancellationToken);
        stopwatch.Stop();

        return Task.FromResult(new ScannerResult(
            rule.Id,
            scanMatches,
            stopwatch.Elapsed,
            wasCacheHit));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ScannerResult>> ScanBatchAsync(
        string content,
        IEnumerable<StyleRule> rules,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rules);

        var ruleList = rules.ToList();
        if (ruleList.Count == 0 || string.IsNullOrEmpty(content))
        {
            return Array.Empty<ScannerResult>();
        }

        // For small batches, scan sequentially
        if (ruleList.Count <= 3)
        {
            var results = new List<ScannerResult>(ruleList.Count);
            foreach (var rule in ruleList)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                results.Add(await ScanAsync(content, rule, cancellationToken));
            }
            return results;
        }

        // For larger batches, scan in parallel
        var tasks = ruleList.Select(rule => ScanAsync(content, rule, cancellationToken));
        var batchResults = await Task.WhenAll(tasks);
        return batchResults;
    }

    /// <inheritdoc />
    public ScannerStatistics GetStatistics()
    {
        return new ScannerStatistics(
            TotalScans: Interlocked.Read(ref _totalScans),
            CacheHits: _cache.Hits,
            CacheMisses: _cache.Misses,
            CurrentCacheSize: _cache.Count,
            MaxCacheSize: _cache.MaxSize);
    }

    /// <inheritdoc />
    public void ClearCache()
    {
        _cache.Clear();
        Interlocked.Exchange(ref _totalScans, 0);
        _logger.LogDebug("Scanner cache cleared");
    }

    /// <summary>
    /// Generates a cache key for a rule's pattern.
    /// </summary>
    private static string GetCacheKey(StyleRule rule)
    {
        // Include pattern and pattern type in key
        return $"{rule.PatternType}:{rule.Pattern}";
    }

    /// <summary>
    /// Compiles a regex pattern with timeout.
    /// </summary>
    private CompiledPattern? CompilePattern(StyleRule rule)
    {
        try
        {
            var timeout = TimeSpan.FromMilliseconds(_config.PatternTimeoutMilliseconds);
            var regex = new Regex(
                rule.Pattern,
                RegexOptions.Compiled | RegexOptions.Multiline,
                timeout);

            return new CompiledPattern(regex, timeout);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid regex pattern for rule {RuleId}: {Pattern}", rule.Id, rule.Pattern);
            return null;
        }
    }

    /// <summary>
    /// Executes a scan with the compiled pattern.
    /// </summary>
    private IReadOnlyList<PatternMatchSpan> ExecuteScan(
        string content,
        CompiledPattern compiled,
        CancellationToken cancellationToken)
    {
        var matches = new List<PatternMatchSpan>();

        try
        {
            var regexMatches = compiled.Regex.Matches(content);
            foreach (Match match in regexMatches)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                matches.Add(new PatternMatchSpan(match.Index, match.Length));
            }
        }
        catch (RegexMatchTimeoutException)
        {
            _logger.LogWarning("Pattern timeout during scan - ReDoS protection triggered");
        }

        return matches;
    }

    /// <summary>
    /// Scans using non-regex pattern types.
    /// </summary>
    private static IReadOnlyList<PatternMatchSpan> ScanNonRegex(string content, StyleRule rule)
    {
        var matches = new List<PatternMatchSpan>();

        switch (rule.PatternType)
        {
            case PatternType.Literal:
                AddLiteralMatches(content, rule.Pattern, StringComparison.Ordinal, matches);
                break;

            case PatternType.LiteralIgnoreCase:
                AddLiteralMatches(content, rule.Pattern, StringComparison.OrdinalIgnoreCase, matches);
                break;

            case PatternType.StartsWith:
                AddStartsWithMatches(content, rule.Pattern, matches);
                break;

            case PatternType.EndsWith:
                AddEndsWithMatches(content, rule.Pattern, matches);
                break;

            case PatternType.Contains:
                AddLiteralMatches(content, rule.Pattern, StringComparison.Ordinal, matches);
                break;
        }

        return matches;
    }

    private static void AddLiteralMatches(
        string content,
        string pattern,
        StringComparison comparison,
        List<PatternMatchSpan> matches)
    {
        var index = 0;
        while ((index = content.IndexOf(pattern, index, comparison)) >= 0)
        {
            matches.Add(new PatternMatchSpan(index, pattern.Length));
            index += pattern.Length;
        }
    }

    private static void AddStartsWithMatches(
        string content,
        string pattern,
        List<PatternMatchSpan> matches)
    {
        var lines = content.Split('\n');
        var offset = 0;
        foreach (var line in lines)
        {
            if (line.StartsWith(pattern, StringComparison.OrdinalIgnoreCase))
            {
                matches.Add(new PatternMatchSpan(offset, pattern.Length));
            }
            offset += line.Length + 1;
        }
    }

    private static void AddEndsWithMatches(
        string content,
        string pattern,
        List<PatternMatchSpan> matches)
    {
        var lines = content.Split('\n');
        var offset = 0;
        foreach (var line in lines)
        {
            var trimmedLine = line.TrimEnd('\r');
            if (trimmedLine.EndsWith(pattern, StringComparison.OrdinalIgnoreCase))
            {
                var matchStart = offset + trimmedLine.Length - pattern.Length;
                matches.Add(new PatternMatchSpan(matchStart, pattern.Length));
            }
            offset += line.Length + 1;
        }
    }

    /// <summary>
    /// Holds a compiled regex pattern.
    /// </summary>
    private sealed record CompiledPattern(Regex Regex, TimeSpan Timeout);
}
