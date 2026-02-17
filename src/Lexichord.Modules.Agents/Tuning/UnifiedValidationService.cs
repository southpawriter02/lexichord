// =============================================================================
// File: UnifiedValidationService.cs
// Project: Lexichord.Modules.Agents
// Description: Aggregates validation results from multiple validators.
// =============================================================================
// LOGIC: Orchestrates parallel validation from Style Linter, Grammar Linter
//   (future), and CKVS Validation Engine. Results are normalized to UnifiedIssue
//   format, deduplicated, filtered by license tier, and cached.
//
// v0.7.5f: Issue Aggregator (Unified Validation Feature)
// =============================================================================

using System.Collections.Concurrent;
using System.Diagnostics;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Agents;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.Knowledge.Validation;
using Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;
using Lexichord.Abstractions.Contracts.Validation;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Tuning;

/// <summary>
/// Service that aggregates validation results from multiple validators.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This service orchestrates validation from three sources:
/// <list type="bullet">
///   <item><description>Style Linter — via <see cref="IStyleDeviationScanner"/></description></item>
///   <item><description>Grammar Linter — placeholder for future implementation (nullable)</description></item>
///   <item><description>CKVS Validation Engine — via <see cref="IValidationEngine"/></description></item>
/// </list>
/// </para>
/// <para>
/// <b>License Gating:</b>
/// <list type="bullet">
///   <item><description>Core: Style Linter only</description></item>
///   <item><description>WriterPro: Style + Grammar Linter</description></item>
///   <item><description>Teams/Enterprise: All validators including CKVS</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Deduplication:</b> When the same issue is detected by multiple validators
/// at the same location (±1 char tolerance), the highest-severity instance is
/// kept and others are excluded from the result.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Thread-safe via <see cref="SemaphoreSlim"/> and
/// <see cref="IMemoryCache"/> for concurrent validation requests.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5f as part of the Unified Validation feature.
/// </para>
/// </remarks>
public sealed class UnifiedValidationService : IUnifiedValidationService, IDisposable
{
    #region Constants

    /// <summary>
    /// Validator name constant for Style Linter.
    /// </summary>
    private const string StyleLinterName = "StyleLinter";

    /// <summary>
    /// Validator name constant for Grammar Linter.
    /// </summary>
    private const string GrammarLinterName = "GrammarLinter";

    /// <summary>
    /// Validator name constant for CKVS Validation Engine.
    /// </summary>
    private const string ValidationEngineName = "ValidationEngine";

    /// <summary>
    /// Location tolerance for deduplication (characters).
    /// </summary>
    private const int DeduplicationLocationTolerance = 1;

    #endregion

    #region Fields

    private readonly IStyleDeviationScanner _styleDeviationScanner;
    private readonly IValidationEngine _validationEngine;
    private readonly ILicenseContext _licenseContext;
    private readonly IMemoryCache _cache;
    private readonly ILogger<UnifiedValidationService> _logger;

    /// <summary>
    /// Tracks cache keys per document for invalidation purposes.
    /// </summary>
    private readonly ConcurrentDictionary<string, string> _documentCacheKeys = new();

    /// <summary>
    /// Semaphore to prevent duplicate concurrent validations of the same document.
    /// </summary>
    private readonly SemaphoreSlim _validationLock = new(1, 1);

    /// <summary>
    /// Flag indicating whether the service has been disposed.
    /// </summary>
    private bool _disposed;

    #endregion

    #region Events

    /// <inheritdoc />
    public event EventHandler<ValidationCompletedEventArgs>? ValidationCompleted;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="UnifiedValidationService"/> class.
    /// </summary>
    /// <param name="styleDeviationScanner">The style deviation scanner (v0.7.5a).</param>
    /// <param name="validationEngine">The CKVS validation engine (v0.6.5e).</param>
    /// <param name="licenseContext">The license context for tier validation.</param>
    /// <param name="cache">The memory cache for result caching.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when any required dependency is null.</exception>
    /// <remarks>
    /// <b>LOGIC:</b> Grammar linter is not yet implemented, so it's not a constructor
    /// parameter. When it becomes available, it will be added as a nullable dependency.
    /// </remarks>
    public UnifiedValidationService(
        IStyleDeviationScanner styleDeviationScanner,
        IValidationEngine validationEngine,
        ILicenseContext licenseContext,
        IMemoryCache cache,
        ILogger<UnifiedValidationService> logger)
    {
        _styleDeviationScanner = styleDeviationScanner ?? throw new ArgumentNullException(nameof(styleDeviationScanner));
        _validationEngine = validationEngine ?? throw new ArgumentNullException(nameof(validationEngine));
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("UnifiedValidationService initialized");
    }

    #endregion

    #region Public Methods

    /// <inheritdoc />
    public async Task<UnifiedValidationResult> ValidateAsync(
        string documentPath,
        string content,
        UnifiedValidationOptions options,
        CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(documentPath);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(options);

        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Starting unified validation for: {Path}", documentPath);

        // LOGIC: Check cache first
        if (options.EnableCaching)
        {
            var cached = await GetCachedResultAsync(documentPath, ct);
            if (cached is not null && CacheIsValid(cached, options))
            {
                _logger.LogDebug("Returning cached validation result for: {Path}", documentPath);
                stopwatch.Stop();
                return cached.AsCached();
            }
        }

        // LOGIC: Get current license tier
        var tier = _licenseContext.GetCurrentTier();
        _logger.LogDebug("License tier: {Tier}", tier);

        // LOGIC: Acquire lock to prevent duplicate validations
        await _validationLock.WaitAsync(ct);
        try
        {
            // Double-check cache after acquiring lock
            if (options.EnableCaching)
            {
                var cached = await GetCachedResultAsync(documentPath, ct);
                if (cached is not null && CacheIsValid(cached, options))
                {
                    stopwatch.Stop();
                    return cached.AsCached();
                }
            }

            // LOGIC: Run validators
            var validatorResults = await RunValidatorsAsync(
                documentPath, content, options, tier, ct);

            // LOGIC: Combine results and sort by severity (most severe first), then by location.
            // UnifiedSeverity enum: Error=0, Warning=1, Info=2, Hint=3
            // OrderBy (ascending) ensures Error (0) comes first.
            var allIssues = validatorResults.Issues
                .OrderBy(i => (int)i.Severity)
                .ThenBy(i => i.Location.Start)
                .ToList();

            // LOGIC: Deduplicate if enabled
            if (options.EnableDeduplication)
            {
                allIssues = DeduplicateIssues(allIssues);
            }

            // LOGIC: Apply filters
            var filteredIssues = ApplyFilters(allIssues, options);

            // LOGIC: Apply max limit
            if (options.MaxIssuesPerDocument > 0 && filteredIssues.Count > options.MaxIssuesPerDocument)
            {
                _logger.LogWarning(
                    "Max issues limit reached ({Max}), truncating from {Total} issues",
                    options.MaxIssuesPerDocument, filteredIssues.Count);
                filteredIssues = filteredIssues
                    .Take(options.MaxIssuesPerDocument)
                    .ToList();
            }

            stopwatch.Stop();

            // LOGIC: Create result
            var result = new UnifiedValidationResult
            {
                DocumentPath = documentPath,
                Issues = filteredIssues,
                Duration = stopwatch.Elapsed,
                ValidatedAt = DateTimeOffset.UtcNow,
                IsCached = false,
                Options = options,
                ValidatorDetails = validatorResults.Details
            };

            // LOGIC: Cache result
            if (options.EnableCaching)
            {
                CacheResult(documentPath, result, options);
            }

            _logger.LogInformation(
                "Unified validation completed for {Path}: {Count} issues in {ElapsedMs}ms",
                documentPath, result.TotalIssueCount, stopwatch.ElapsedMilliseconds);

            // LOGIC: Raise event
            OnValidationCompleted(
                documentPath,
                result,
                validatorResults.SuccessfulValidators,
                validatorResults.FailedValidators);

            return result;
        }
        finally
        {
            _validationLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<UnifiedValidationResult> ValidateRangeAsync(
        string documentPath,
        string content,
        TextSpan range,
        UnifiedValidationOptions options,
        CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(documentPath);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(options);

        _logger.LogDebug(
            "Validating range for {Path}: [{Start}-{End}]",
            documentPath, range.Start, range.End);

        // LOGIC: Perform full validation
        var fullResult = await ValidateAsync(documentPath, content, options, ct);

        // LOGIC: Filter to issues within range
        var rangeIssues = fullResult.Issues
            .Where(i => i.Location.Start >= range.Start && i.Location.End <= range.End)
            .ToList();

        _logger.LogDebug(
            "Range validation: {Count} issues in range (of {Total} total)",
            rangeIssues.Count, fullResult.TotalIssueCount);

        return fullResult with
        {
            Issues = rangeIssues,
            IsCached = false // Range results are not directly cached
        };
    }

    /// <inheritdoc />
    public Task<UnifiedValidationResult?> GetCachedResultAsync(
        string documentPath,
        CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentPath);

        // LOGIC: Try to find cached result by stored cache key
        if (_documentCacheKeys.TryGetValue(documentPath, out var cacheKey) &&
            _cache.TryGetValue(cacheKey, out UnifiedValidationResult? cachedResult))
        {
            _logger.LogDebug("Found cached result for: {Path}", documentPath);
            return Task.FromResult<UnifiedValidationResult?>(cachedResult);
        }

        _logger.LogDebug("No cached result for: {Path}", documentPath);
        return Task.FromResult<UnifiedValidationResult?>(null);
    }

    /// <inheritdoc />
    public void InvalidateCache(string documentPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentPath);

        // LOGIC: Remove tracked cache key
        if (_documentCacheKeys.TryRemove(documentPath, out var cacheKey))
        {
            _cache.Remove(cacheKey);
            _logger.LogDebug("Cache invalidated for: {Path}", documentPath);
        }
    }

    /// <inheritdoc />
    public void InvalidateAllCaches()
    {
        _logger.LogInformation(
            "Invalidating all validation caches ({Count} documents tracked)",
            _documentCacheKeys.Count);

        // LOGIC: Remove all tracked cache entries
        foreach (var kvp in _documentCacheKeys)
        {
            _cache.Remove(kvp.Value);
        }
        _documentCacheKeys.Clear();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Runs all applicable validators based on options and license tier.
    /// </summary>
    private async Task<ValidatorRunResults> RunValidatorsAsync(
        string documentPath,
        string content,
        UnifiedValidationOptions options,
        LicenseTier tier,
        CancellationToken ct)
    {
        var issues = new List<UnifiedIssue>();
        var successfulValidators = new List<string>();
        var failedValidators = new Dictionary<string, string>();
        var details = new Dictionary<string, object>();

        var tasks = new List<Task<ValidatorResult>>();

        // LOGIC: Style Linter - available at Core and above
        if (options.IncludeStyleLinter && IsAvailableForTier(tier, StyleLinterName))
        {
            tasks.Add(RunStyleLinterAsync(documentPath, options, ct));
        }

        // LOGIC: Grammar Linter - available at WriterPro and above (placeholder)
        if (options.IncludeGrammarLinter && IsAvailableForTier(tier, GrammarLinterName))
        {
            tasks.Add(RunGrammarLinterAsync(documentPath, options, ct));
        }

        // LOGIC: CKVS Validation Engine - available at Teams and above
        if (options.IncludeValidationEngine && IsAvailableForTier(tier, ValidationEngineName))
        {
            tasks.Add(RunValidationEngineAsync(documentPath, content, options, tier, ct));
        }

        // LOGIC: Execute validators (parallel or sequential)
        IEnumerable<ValidatorResult> results;
        if (options.ParallelValidation)
        {
            results = await Task.WhenAll(tasks);
        }
        else
        {
            var sequentialResults = new List<ValidatorResult>();
            foreach (var task in tasks)
            {
                sequentialResults.Add(await task);
            }
            results = sequentialResults;
        }

        // LOGIC: Aggregate results
        foreach (var result in results)
        {
            if (result.Success)
            {
                issues.AddRange(result.Issues);
                successfulValidators.Add(result.ValidatorName);
                details[result.ValidatorName] = new { IssueCount = result.Issues.Count };
            }
            else
            {
                failedValidators[result.ValidatorName] = result.ErrorMessage ?? "Unknown error";
                details[result.ValidatorName] = new { Error = result.ErrorMessage };
            }
        }

        return new ValidatorRunResults(
            issues,
            successfulValidators,
            failedValidators,
            details);
    }

    /// <summary>
    /// Runs the Style Linter via <see cref="IStyleDeviationScanner"/>.
    /// </summary>
    private async Task<ValidatorResult> RunStyleLinterAsync(
        string documentPath,
        UnifiedValidationOptions options,
        CancellationToken ct)
    {
        try
        {
            using var cts = new CancellationTokenSource(options.ValidatorTimeoutMs);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token);

            _logger.LogDebug("Running Style Linter for: {Path}", documentPath);

            var scanResult = await _styleDeviationScanner.ScanDocumentAsync(
                documentPath,
                linkedCts.Token);

            _logger.LogDebug(
                "Style Linter found {Count} deviations",
                scanResult.TotalCount);

            // LOGIC: Convert StyleDeviations to UnifiedIssues
            var issues = UnifiedIssueFactory.FromDeviations(scanResult.Deviations);

            return ValidatorResult.Succeeded(StyleLinterName, issues);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "Style Linter timed out after {Timeout}ms",
                options.ValidatorTimeoutMs);
            return ValidatorResult.Failed(StyleLinterName, "Validation timed out");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Style Linter validation failed");
            return ValidatorResult.Failed(StyleLinterName, ex.Message);
        }
    }

    /// <summary>
    /// Runs the Grammar Linter (placeholder for future implementation).
    /// </summary>
    private Task<ValidatorResult> RunGrammarLinterAsync(
        string documentPath,
        UnifiedValidationOptions options,
        CancellationToken ct)
    {
        // LOGIC: Grammar linter is not yet implemented
        // Return empty success result
        _logger.LogDebug("Grammar Linter not yet implemented, returning empty result");
        return Task.FromResult(ValidatorResult.Succeeded(
            GrammarLinterName,
            Array.Empty<UnifiedIssue>()));
    }

    /// <summary>
    /// Runs the CKVS Validation Engine via <see cref="IValidationEngine"/>.
    /// </summary>
    private async Task<ValidatorResult> RunValidationEngineAsync(
        string documentPath,
        string content,
        UnifiedValidationOptions options,
        LicenseTier tier,
        CancellationToken ct)
    {
        try
        {
            using var cts = new CancellationTokenSource(options.ValidatorTimeoutMs);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token);

            _logger.LogDebug("Running Validation Engine for: {Path}", documentPath);

            // LOGIC: Create ValidationContext
            var documentType = GetDocumentType(documentPath);
            var validationOptions = new ValidationOptions(
                Mode: ValidationMode.OnDemand,
                Timeout: options.ValidatorTimeout,
                MaxFindings: options.MaxIssuesPerDocument > 0 ? options.MaxIssuesPerDocument : null,
                LicenseTier: tier);

            var context = ValidationContext.Create(
                documentPath,
                documentType,
                content,
                validationOptions);

            var validationResult = await _validationEngine.ValidateDocumentAsync(
                context,
                linkedCts.Token);

            _logger.LogDebug(
                "Validation Engine found {Count} findings",
                validationResult.Findings.Count);

            // LOGIC: Convert ValidationFindings to UnifiedIssues
            var issues = validationResult.Findings
                .Select(UnifiedIssueFactory.FromValidationFinding)
                .ToList();

            return ValidatorResult.Succeeded(ValidationEngineName, issues);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "Validation Engine timed out after {Timeout}ms",
                options.ValidatorTimeoutMs);
            return ValidatorResult.Failed(ValidationEngineName, "Validation timed out");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validation Engine validation failed");
            return ValidatorResult.Failed(ValidationEngineName, ex.Message);
        }
    }

    /// <summary>
    /// Deduplicates issues detected at the same location by different validators.
    /// </summary>
    private List<UnifiedIssue> DeduplicateIssues(List<UnifiedIssue> issues)
    {
        if (issues.Count <= 1)
        {
            return issues;
        }

        var deduplicated = new List<UnifiedIssue>();
        var processed = new HashSet<Guid>();

        // LOGIC: Issues are already sorted by severity (descending), so highest severity comes first
        foreach (var issue in issues)
        {
            if (processed.Contains(issue.IssueId))
            {
                continue;
            }

            // LOGIC: Find potential duplicates (same location from different validators)
            var duplicates = issues
                .Where(i => !processed.Contains(i.IssueId) &&
                            i.IssueId != issue.IssueId &&
                            IsDuplicate(issue, i))
                .ToList();

            // LOGIC: Mark duplicates as processed (they will be excluded)
            foreach (var dup in duplicates)
            {
                processed.Add(dup.IssueId);
                _logger.LogDebug(
                    "Deduplicating issue {DupId} (duplicate of {PrimaryId})",
                    dup.IssueId, issue.IssueId);
            }

            // LOGIC: Keep the primary issue (highest severity)
            deduplicated.Add(issue);
            processed.Add(issue.IssueId);
        }

        if (deduplicated.Count < issues.Count)
        {
            _logger.LogDebug(
                "Deduplicated {Removed} issues ({Original} → {Remaining})",
                issues.Count - deduplicated.Count,
                issues.Count,
                deduplicated.Count);
        }

        return deduplicated;
    }

    /// <summary>
    /// Determines if two issues are duplicates (same location, different validators).
    /// </summary>
    private static bool IsDuplicate(UnifiedIssue issue1, UnifiedIssue issue2)
    {
        // LOGIC: Must be from different validators
        if (issue1.SourceType == issue2.SourceType)
        {
            return false;
        }

        // LOGIC: Must be at the same location (within tolerance)
        var locationMatch = Math.Abs(issue1.Location.Start - issue2.Location.Start) <= DeduplicationLocationTolerance;

        return locationMatch;
    }

    /// <summary>
    /// Applies severity and category filters to issues.
    /// </summary>
    private static List<UnifiedIssue> ApplyFilters(
        List<UnifiedIssue> issues,
        UnifiedValidationOptions options)
    {
        return issues
            .Where(i => options.PassesSeverityFilter(i.Severity))
            .Where(i => options.PassesCategoryFilter(i.Category))
            .ToList();
    }

    /// <summary>
    /// Checks if a validator is available for the given license tier.
    /// </summary>
    private static bool IsAvailableForTier(LicenseTier tier, string validatorName) =>
        (tier, validatorName) switch
        {
            // Style Linter - available at Core and above
            (>= LicenseTier.Core, StyleLinterName) => true,

            // Grammar Linter - available at WriterPro and above
            (>= LicenseTier.WriterPro, GrammarLinterName) => true,

            // Validation Engine - available at Teams and above
            (>= LicenseTier.Teams, ValidationEngineName) => true,

            _ => false
        };

    /// <summary>
    /// Determines the document type from the file extension.
    /// </summary>
    private static string GetDocumentType(string documentPath)
    {
        var extension = Path.GetExtension(documentPath)?.ToLowerInvariant();
        return extension switch
        {
            ".md" or ".markdown" => "markdown",
            ".json" => "json",
            ".yaml" or ".yml" => "yaml",
            ".xml" => "xml",
            ".html" or ".htm" => "html",
            ".txt" => "text",
            _ => "unknown"
        };
    }

    /// <summary>
    /// Checks if a cached result is still valid for the given options.
    /// </summary>
    private bool CacheIsValid(UnifiedValidationResult cached, UnifiedValidationOptions options)
    {
        // LOGIC: Check if options match
        if (cached.Options is null)
        {
            return false;
        }

        // LOGIC: Check cache age
        var age = DateTimeOffset.UtcNow - cached.ValidatedAt;
        if (age > options.CacheTtl)
        {
            _logger.LogDebug(
                "Cache expired: age={AgeMs}ms, ttl={TtlMs}ms",
                age.TotalMilliseconds, options.CacheTtlMs);
            return false;
        }

        // LOGIC: Check if key options match
        var optionsMatch =
            cached.Options.IncludeStyleLinter == options.IncludeStyleLinter &&
            cached.Options.IncludeGrammarLinter == options.IncludeGrammarLinter &&
            cached.Options.IncludeValidationEngine == options.IncludeValidationEngine &&
            cached.Options.MinimumSeverity == options.MinimumSeverity &&
            cached.Options.EnableDeduplication == options.EnableDeduplication;

        if (!optionsMatch)
        {
            _logger.LogDebug("Cache invalidated: options changed");
        }

        return optionsMatch;
    }

    /// <summary>
    /// Caches a validation result.
    /// </summary>
    private void CacheResult(
        string documentPath,
        UnifiedValidationResult result,
        UnifiedValidationOptions options)
    {
        var cacheKey = $"unified-validation:{documentPath}:{result.ValidatedAt.Ticks}";

        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = options.CacheTtl,
            SlidingExpiration = TimeSpan.FromMilliseconds(options.CacheTtlMs / 2.0)
        };

        _cache.Set(cacheKey, result, cacheOptions);
        _documentCacheKeys[documentPath] = cacheKey;

        _logger.LogDebug("Cached validation result for: {Path}", documentPath);
    }

    /// <summary>
    /// Raises the <see cref="ValidationCompleted"/> event.
    /// </summary>
    private void OnValidationCompleted(
        string documentPath,
        UnifiedValidationResult result,
        IReadOnlyList<string> successfulValidators,
        IReadOnlyDictionary<string, string> failedValidators)
    {
        var eventArgs = failedValidators.Count == 0
            ? ValidationCompletedEventArgs.Success(documentPath, result, successfulValidators)
            : ValidationCompletedEventArgs.WithFailures(
                documentPath, result, successfulValidators, failedValidators);

        ValidationCompleted?.Invoke(this, eventArgs);
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Disposes the service and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _validationLock.Dispose();
        _documentCacheKeys.Clear();
        _disposed = true;

        _logger.LogDebug("UnifiedValidationService disposed");
    }

    #endregion

    #region Nested Types

    /// <summary>
    /// Result from a single validator run.
    /// </summary>
    private sealed record ValidatorResult(
        string ValidatorName,
        bool Success,
        IReadOnlyList<UnifiedIssue> Issues,
        string? ErrorMessage)
    {
        public static ValidatorResult Succeeded(string name, IReadOnlyList<UnifiedIssue> issues) =>
            new(name, true, issues, null);

        public static ValidatorResult Failed(string name, string error) =>
            new(name, false, Array.Empty<UnifiedIssue>(), error);
    }

    /// <summary>
    /// Aggregated results from all validator runs.
    /// </summary>
    private sealed record ValidatorRunResults(
        List<UnifiedIssue> Issues,
        List<string> SuccessfulValidators,
        Dictionary<string, string> FailedValidators,
        Dictionary<string, object> Details);

    #endregion
}
