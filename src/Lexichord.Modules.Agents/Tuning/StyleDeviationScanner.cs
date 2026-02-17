// -----------------------------------------------------------------------
// <copyright file="StyleDeviationScanner.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Agents;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.Linting;
using Lexichord.Abstractions.Events;
using Lexichord.Modules.Agents.Tuning.Configuration;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lexichord.Modules.Agents.Tuning;

/// <summary>
/// Scans documents for style deviations by bridging the linting infrastructure
/// with the Tuning Agent's fix generation system.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> StyleDeviationScanner wraps <see cref="ILintingOrchestrator"/> and enriches
/// raw <see cref="StyleViolation"/> objects with context needed for AI fix generation:
/// <list type="bullet">
///   <item><description>Surrounding text context for AI prompts</description></item>
///   <item><description>Auto-fixability classification based on rule properties</description></item>
///   <item><description>Priority mapping from violation severity</description></item>
///   <item><description>Complete rule details for fix generation</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Caching:</b> Results are cached using a key composed of document path, content hash,
/// and rules version. Cache entries have configurable TTL with sliding expiration.
/// </para>
/// <para>
/// <b>Real-time Updates:</b> Subscribes to MediatR events for automatic cache invalidation
/// and live deviation detection:
/// <list type="bullet">
///   <item><description><see cref="LintingCompletedEvent"/> — Re-scans open documents</description></item>
///   <item><description><see cref="StyleSheetReloadedEvent"/> — Invalidates all caches</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> Thread-safe via <see cref="SemaphoreSlim"/> for scan operations
/// and <see cref="ConcurrentDictionary{TKey, TValue}"/> for cache key tracking.
/// </para>
/// <para>
/// <b>License Requirement:</b> Requires WriterPro tier. Returns empty results for unlicensed users.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5a as part of the Tuning Agent feature set.
/// </para>
/// </remarks>
public sealed class StyleDeviationScanner :
    IStyleDeviationScanner,
    INotificationHandler<LintingCompletedEvent>,
    INotificationHandler<StyleSheetReloadedEvent>,
    IDisposable
{
    #region Fields

    private readonly ILintingOrchestrator _lintingOrchestrator;
    private readonly IEditorService _editorService;
    private readonly IMemoryCache _cache;
    private readonly ILicenseContext _licenseContext;
    private readonly ILogger<StyleDeviationScanner> _logger;
    private readonly ScannerOptions _options;

    /// <summary>
    /// Tracks cache keys per document for invalidation purposes.
    /// </summary>
    private readonly ConcurrentDictionary<string, string> _documentCacheKeys = new();

    /// <summary>
    /// Semaphore to prevent duplicate concurrent scans of the same document.
    /// </summary>
    private readonly SemaphoreSlim _scanLock = new(1, 1);

    /// <summary>
    /// Flag indicating whether the scanner has been disposed.
    /// </summary>
    private bool _disposed;

    #endregion

    #region Events

    /// <inheritdoc />
    public event EventHandler<DeviationsDetectedEventArgs>? DeviationsDetected;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="StyleDeviationScanner"/> class.
    /// </summary>
    /// <param name="lintingOrchestrator">The linting orchestrator for getting violations.</param>
    /// <param name="editorService">The editor service for document access.</param>
    /// <param name="cache">The memory cache for result caching.</param>
    /// <param name="licenseContext">The license context for tier validation.</param>
    /// <param name="options">The scanner configuration options.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when any dependency is null.</exception>
    public StyleDeviationScanner(
        ILintingOrchestrator lintingOrchestrator,
        IEditorService editorService,
        IMemoryCache cache,
        ILicenseContext licenseContext,
        IOptions<ScannerOptions> options,
        ILogger<StyleDeviationScanner> logger)
    {
        _lintingOrchestrator = lintingOrchestrator ?? throw new ArgumentNullException(nameof(lintingOrchestrator));
        _editorService = editorService ?? throw new ArgumentNullException(nameof(editorService));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;

        _logger.LogDebug("StyleDeviationScanner initialized with ContextWindowSize={WindowSize}, CacheTtl={CacheTtl}min",
            _options.ContextWindowSize, _options.CacheTtlMinutes);
    }

    #endregion

    #region Public Methods

    /// <inheritdoc />
    public async Task<DeviationScanResult> ScanDocumentAsync(
        string documentPath,
        CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentPath);

        // LOGIC: Check license tier first - WriterPro required
        if (_licenseContext.GetCurrentTier() < LicenseTier.WriterPro)
        {
            _logger.LogWarning("License check failed: Writer Pro required for deviation scanning. Current tier: {Tier}",
                _licenseContext.GetCurrentTier());
            return DeviationScanResult.LicenseRequired(documentPath);
        }

        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Scanning document for deviations: {Path}", documentPath);

        // LOGIC: Get document content for hashing and context extraction
        var documentText = await GetDocumentContentAsync(documentPath, ct);
        if (documentText is null)
        {
            _logger.LogWarning("Document not found or empty: {Path}", documentPath);
            stopwatch.Stop();
            return DeviationScanResult.Empty(documentPath, stopwatch.Elapsed);
        }

        // LOGIC: Generate cache key and check cache
        var contentHash = ComputeContentHash(documentText);
        var cacheKey = GenerateCacheKey(documentPath, contentHash);

        if (_cache.TryGetValue(cacheKey, out DeviationScanResult? cachedResult))
        {
            _logger.LogDebug("Cache hit for document: {Path}", documentPath);
            stopwatch.Stop();
            return cachedResult! with { IsCached = true, ScanDuration = stopwatch.Elapsed };
        }

        // LOGIC: Acquire lock to prevent duplicate scans
        await _scanLock.WaitAsync(ct);
        try
        {
            // Double-check cache after acquiring lock
            if (_cache.TryGetValue(cacheKey, out cachedResult))
            {
                stopwatch.Stop();
                return cachedResult! with { IsCached = true, ScanDuration = stopwatch.Elapsed };
            }

            // LOGIC: Get lint result from orchestrator
            var lintResult = await GetLintResultAsync(documentPath, documentText, ct);
            if (lintResult is null || !lintResult.IsSuccess)
            {
                _logger.LogWarning("Lint operation failed or returned no result for: {Path}", documentPath);
                stopwatch.Stop();
                return DeviationScanResult.Empty(documentPath, stopwatch.Elapsed);
            }

            // LOGIC: Filter and convert violations to deviations
            var deviations = new List<StyleDeviation>();
            foreach (var violation in lintResult.Violations)
            {
                // Filter by minimum severity
                if (violation.Severity > _options.MinimumSeverity)
                {
                    continue;
                }

                // Filter by excluded categories
                if (_options.ExcludedCategories.Contains(violation.Rule.Category.ToString()))
                {
                    continue;
                }

                // Create deviation with enriched context
                var deviation = CreateDeviation(violation, documentText);
                if (deviation is null)
                {
                    continue;
                }

                // Filter manual-only if configured
                if (!_options.IncludeManualOnly && !deviation.IsAutoFixable)
                {
                    continue;
                }

                deviations.Add(deviation);

                // Respect max limit
                if (_options.MaxDeviationsPerScan > 0 &&
                    deviations.Count >= _options.MaxDeviationsPerScan)
                {
                    _logger.LogWarning(
                        "Max deviations limit reached ({Max}), some violations may be omitted",
                        _options.MaxDeviationsPerScan);
                    break;
                }
            }

            stopwatch.Stop();

            // LOGIC: Create result with all metadata
            var result = new DeviationScanResult
            {
                DocumentPath = documentPath,
                Deviations = deviations,
                ScannedAt = DateTimeOffset.UtcNow,
                ScanDuration = stopwatch.Elapsed,
                IsCached = false,
                ContentHash = contentHash,
                RulesVersion = null // Could be populated from style sheet metadata if available
            };

            // LOGIC: Cache the result
            CacheResult(cacheKey, documentPath, result);

            _logger.LogInformation(
                "Scan completed: {Count} deviations ({AutoFixable} auto-fixable) in {ElapsedMs}ms",
                result.TotalCount,
                result.AutoFixableCount,
                stopwatch.ElapsedMilliseconds);

            // LOGIC: Raise event for UI updates
            OnDeviationsDetected(result, isIncremental: false);

            return result;
        }
        finally
        {
            _scanLock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<DeviationScanResult> ScanRangeAsync(
        string documentPath,
        TextSpan range,
        CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentPath);

        // LOGIC: Check license tier
        if (_licenseContext.GetCurrentTier() < LicenseTier.WriterPro)
        {
            _logger.LogWarning("License check failed: Writer Pro required for range scanning");
            return DeviationScanResult.LicenseRequired(documentPath);
        }

        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug(
            "Scanning range for deviations: {Path} [{Start}-{End}]",
            documentPath, range.Start, range.End);

        // LOGIC: Get document content
        var documentText = await GetDocumentContentAsync(documentPath, ct);
        if (documentText is null)
        {
            stopwatch.Stop();
            return DeviationScanResult.Empty(documentPath, stopwatch.Elapsed);
        }

        // LOGIC: Range scans are not cached (too granular)
        var lintResult = await GetLintResultAsync(documentPath, documentText, ct);
        if (lintResult is null || !lintResult.IsSuccess)
        {
            stopwatch.Stop();
            return DeviationScanResult.Empty(documentPath, stopwatch.Elapsed);
        }

        // LOGIC: Filter violations to those within the range
        var deviations = new List<StyleDeviation>();
        foreach (var violation in lintResult.Violations)
        {
            // Check if violation overlaps with range
            var violationSpan = new TextSpan(violation.StartOffset, violation.Length);
            if (!violationSpan.OverlapsWith(range))
            {
                continue;
            }

            var deviation = CreateDeviation(violation, documentText);
            if (deviation is not null)
            {
                deviations.Add(deviation);
            }
        }

        stopwatch.Stop();

        _logger.LogDebug("Range scan completed: {Count} deviations in range", deviations.Count);

        return new DeviationScanResult
        {
            DocumentPath = documentPath,
            Deviations = deviations,
            ScannedAt = DateTimeOffset.UtcNow,
            ScanDuration = stopwatch.Elapsed,
            IsCached = false
        };
    }

    /// <inheritdoc />
    public Task<DeviationScanResult?> GetCachedResultAsync(
        string documentPath,
        CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // LOGIC: Try to find cached result by stored cache key
        if (_documentCacheKeys.TryGetValue(documentPath, out var cacheKey) &&
            _cache.TryGetValue(cacheKey, out DeviationScanResult? cachedResult))
        {
            _logger.LogDebug("Found cached result for: {Path}", documentPath);
            return Task.FromResult<DeviationScanResult?>(cachedResult);
        }

        _logger.LogDebug("No cached result for: {Path}", documentPath);
        return Task.FromResult<DeviationScanResult?>(null);
    }

    /// <inheritdoc />
    public void InvalidateCache(string documentPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentPath);

        // LOGIC: Remove tracked cache key and let IMemoryCache handle removal
        if (_documentCacheKeys.TryRemove(documentPath, out var cacheKey))
        {
            _cache.Remove(cacheKey);
            _logger.LogDebug("Cache invalidated for document: {Path}", documentPath);
        }
    }

    /// <inheritdoc />
    public void InvalidateAllCaches()
    {
        _logger.LogInformation("Invalidating all deviation caches ({Count} documents tracked)",
            _documentCacheKeys.Count);

        // LOGIC: Remove all tracked cache entries
        foreach (var kvp in _documentCacheKeys)
        {
            _cache.Remove(kvp.Value);
        }
        _documentCacheKeys.Clear();
    }

    #endregion

    #region MediatR Event Handlers

    /// <summary>
    /// Handles <see cref="LintingCompletedEvent"/> for real-time deviation updates.
    /// </summary>
    /// <param name="notification">The linting completed event.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A completed task.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> When linting completes, invalidate the cache for that document
    /// and optionally trigger a re-scan if real-time updates are enabled.
    /// </remarks>
    public async Task Handle(LintingCompletedEvent notification, CancellationToken ct)
    {
        if (!_options.EnableRealTimeUpdates)
        {
            return;
        }

        var documentId = notification.Result.DocumentId;
        _logger.LogDebug("Real-time linting completed for: {DocumentId}", documentId);

        // LOGIC: Find the document path from the open documents
        var document = _editorService.GetDocumentById(documentId);
        if (document?.FilePath is null)
        {
            return;
        }

        // Invalidate cache
        InvalidateCache(document.FilePath);

        // LOGIC: Re-scan to update deviations list
        try
        {
            await ScanDocumentAsync(document.FilePath, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to re-scan document after linting: {Path}", document.FilePath);
        }
    }

    /// <summary>
    /// Handles <see cref="StyleSheetReloadedEvent"/> for cache invalidation.
    /// </summary>
    /// <param name="notification">The style sheet reloaded event.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A completed task.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> When style rules change, all cached results are invalidated
    /// because the rules used to generate them are now stale.
    /// </remarks>
    public Task Handle(StyleSheetReloadedEvent notification, CancellationToken ct)
    {
        _logger.LogInformation(
            "Style rules changed ({Source}), invalidating all caches",
            notification.ReloadSource);

        InvalidateAllCaches();
        return Task.CompletedTask;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Gets document content from the editor service.
    /// </summary>
    private async Task<string?> GetDocumentContentAsync(string documentPath, CancellationToken ct)
    {
        try
        {
            var document = _editorService.GetDocumentByPath(documentPath);
            if (document is not null)
            {
                // Document is open - get current content
                return _editorService.GetDocumentText();
            }

            // Document is not open - try to read from disk
            if (File.Exists(documentPath))
            {
                return await File.ReadAllTextAsync(documentPath, ct);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get document content: {Path}", documentPath);
            return null;
        }
    }

    /// <summary>
    /// Gets lint result for a document using the orchestrator.
    /// </summary>
    private async Task<LintResult?> GetLintResultAsync(string documentPath, string content, CancellationToken ct)
    {
        try
        {
            // LOGIC: Get the document state from the orchestrator
            var document = _editorService.GetDocumentByPath(documentPath);
            if (document is null)
            {
                return null;
            }

            var documentId = document.DocumentId ?? documentPath;
            var state = _lintingOrchestrator.GetDocumentState(documentId);

            // LOGIC: If we have recent violations, create a LintResult from them
            if (state is not null && state.LastViolations.Count > 0)
            {
                return LintResult.Success(
                    documentId,
                    state.LastViolations,
                    state.LastLintTime.HasValue
                        ? DateTimeOffset.UtcNow - state.LastLintTime.Value
                        : TimeSpan.Zero);
            }

            // LOGIC: Trigger a manual scan if no result is available
            return await _lintingOrchestrator.TriggerManualScanAsync(documentId, content, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get lint result: {Path}", documentPath);
            return null;
        }
    }

    /// <summary>
    /// Creates a <see cref="StyleDeviation"/> from a <see cref="StyleViolation"/>.
    /// </summary>
    private StyleDeviation? CreateDeviation(StyleViolation violation, string documentContent)
    {
        try
        {
            // Extract original text at violation location
            var originalText = documentContent.Substring(
                violation.StartOffset,
                Math.Min(violation.Length, documentContent.Length - violation.StartOffset));

            // Extract surrounding context
            var surroundingContext = ExtractSurroundingContext(
                documentContent,
                violation.StartOffset,
                violation.EndOffset,
                _options.ContextWindowSize);

            // Create text span
            var location = new TextSpan(violation.StartOffset, violation.Length);

            // Determine auto-fixability
            var isAutoFixable = DetermineAutoFixability(violation);

            // Map severity to priority
            var priority = MapSeverityToPriority(violation.Severity);

            return new StyleDeviation
            {
                DeviationId = Guid.NewGuid(),
                Violation = violation,
                Location = location,
                OriginalText = originalText,
                SurroundingContext = surroundingContext,
                ViolatedRule = violation.Rule,
                IsAutoFixable = isAutoFixable,
                Priority = priority
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to create deviation for violation: {RuleId} at offset {Offset}",
                violation.Rule.Id,
                violation.StartOffset);
            return null;
        }
    }

    /// <summary>
    /// Extracts surrounding context for a violation location.
    /// </summary>
    private string ExtractSurroundingContext(
        string content,
        int startOffset,
        int endOffset,
        int windowSize)
    {
        var contextStart = Math.Max(0, startOffset - windowSize);
        var contextEnd = Math.Min(content.Length, endOffset + windowSize);

        // LOGIC: Optionally adjust to sentence/paragraph boundaries
        if (_options.AdjustContextToBoundaries)
        {
            contextStart = FindPreviousBoundary(content, contextStart, startOffset);
            contextEnd = FindNextBoundary(content, contextEnd, endOffset);
        }

        return content.Substring(contextStart, contextEnd - contextStart);
    }

    /// <summary>
    /// Finds a natural text boundary before the start position.
    /// </summary>
    private static int FindPreviousBoundary(string content, int start, int max)
    {
        // Look for paragraph break or sentence end within the range
        for (var i = start; i < max && i < content.Length - 1; i++)
        {
            // Paragraph break
            if (content[i] == '\n' && i + 1 < content.Length && content[i + 1] == '\n')
            {
                return i + 2;
            }
            // Sentence end
            if (content[i] == '.' && i + 1 < content.Length && char.IsWhiteSpace(content[i + 1]))
            {
                return i + 2;
            }
        }
        return start;
    }

    /// <summary>
    /// Finds a natural text boundary after the end position.
    /// </summary>
    private static int FindNextBoundary(string content, int end, int min)
    {
        // Look backwards for paragraph break or sentence end
        for (var i = end - 1; i > min && i > 0; i--)
        {
            // Paragraph break
            if (content[i] == '\n' && i - 1 >= 0 && content[i - 1] == '\n')
            {
                return i - 1;
            }
            // Sentence end
            if (content[i] == '.')
            {
                return i + 1;
            }
        }
        return end;
    }

    /// <summary>
    /// Determines whether a violation can be automatically fixed by AI.
    /// </summary>
    private static bool DetermineAutoFixability(StyleViolation violation)
    {
        var rule = violation.Rule;

        // LOGIC: Rules with suggestions are auto-fixable
        if (!string.IsNullOrEmpty(violation.Suggestion))
        {
            return true;
        }

        // LOGIC: Pattern-based rules (terminology) are auto-fixable
        if (rule.Category == RuleCategory.Terminology)
        {
            return true;
        }

        // LOGIC: Simple pattern match rules are auto-fixable
        if (rule.PatternType is PatternType.Literal or
            PatternType.LiteralIgnoreCase or
            PatternType.Contains)
        {
            return true;
        }

        // LOGIC: Formatting rules typically need manual intervention
        if (rule.Category == RuleCategory.Formatting)
        {
            return false;
        }

        // LOGIC: Syntax rules with regex patterns are often auto-fixable
        if (rule.Category == RuleCategory.Syntax && rule.PatternType == PatternType.Regex)
        {
            return true;
        }

        // Default: assume fixable, let validation catch failures
        return true;
    }

    /// <summary>
    /// Maps violation severity to deviation priority.
    /// </summary>
    private static DeviationPriority MapSeverityToPriority(ViolationSeverity severity) =>
        severity switch
        {
            ViolationSeverity.Error => DeviationPriority.Critical,
            ViolationSeverity.Warning => DeviationPriority.High,
            ViolationSeverity.Info => DeviationPriority.Normal,
            ViolationSeverity.Hint => DeviationPriority.Low,
            _ => DeviationPriority.Normal
        };

    /// <summary>
    /// Computes a content hash for cache key generation.
    /// </summary>
    private static string ComputeContentHash(string content)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToBase64String(hashBytes)[..16];
    }

    /// <summary>
    /// Generates a cache key for the given document and content hash.
    /// </summary>
    private static string GenerateCacheKey(string documentPath, string contentHash) =>
        $"deviation-scan:{documentPath}:{contentHash}";

    /// <summary>
    /// Caches a scan result with configured TTL.
    /// </summary>
    private void CacheResult(string cacheKey, string documentPath, DeviationScanResult result)
    {
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_options.CacheTtlMinutes),
            SlidingExpiration = TimeSpan.FromMinutes(_options.CacheTtlMinutes / 2.0)
        };

        _cache.Set(cacheKey, result, cacheOptions);
        _documentCacheKeys[documentPath] = cacheKey;

        _logger.LogDebug("Cached scan result for: {Path} (key: {Key})", documentPath, cacheKey);
    }

    /// <summary>
    /// Raises the <see cref="DeviationsDetected"/> event.
    /// </summary>
    private void OnDeviationsDetected(DeviationScanResult result, bool isIncremental)
    {
        if (result.TotalCount == 0)
        {
            return;
        }

        DeviationsDetected?.Invoke(this, new DeviationsDetectedEventArgs
        {
            DocumentPath = result.DocumentPath,
            NewDeviations = result.Deviations,
            TotalDeviationCount = result.TotalCount,
            IsIncremental = isIncremental
        });
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Disposes the scanner and releases resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _scanLock.Dispose();
        _documentCacheKeys.Clear();
        _disposed = true;

        _logger.LogDebug("StyleDeviationScanner disposed");
    }

    #endregion
}
