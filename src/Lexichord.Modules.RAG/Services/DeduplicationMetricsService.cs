// =============================================================================
// File: DeduplicationMetricsService.cs
// Project: Lexichord.Modules.RAG
// Description: Implementation of IDeduplicationMetricsService.
// =============================================================================
// VERSION: v0.5.9h (Hardening & Metrics)
// LOGIC: Records operational metrics and provides dashboard data aggregation.
//   - Recording methods delegate to static DeduplicationMetrics class.
//   - Query methods aggregate from static metrics and database queries.
//   - License-gated for full dashboard data (Writer Pro).
// =============================================================================

using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Metrics;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// Implementation of <see cref="IDeduplicationMetricsService"/> for recording and querying metrics.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9h as part of the Hardening &amp; Metrics phase.
/// </para>
/// <para>
/// This service bridges the static <see cref="DeduplicationMetrics"/> class with the
/// application's dependency injection container, providing:
/// </para>
/// <list type="bullet">
///   <item><description>Thread-safe recording methods that delegate to static counters.</description></item>
///   <item><description>Dashboard data aggregation from static metrics and database queries.</description></item>
///   <item><description>Health status calculation based on P99 latencies vs targets.</description></item>
///   <item><description>License gating for full dashboard data access.</description></item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> All public methods are thread-safe.
/// </para>
/// </remarks>
public sealed class DeduplicationMetricsService : IDeduplicationMetricsService
{
    private readonly ILicenseContext _licenseContext;
    private readonly IDeduplicationService _deduplicationService;
    private readonly IContradictionService _contradictionService;
    private readonly ILogger<DeduplicationMetricsService> _logger;

    // Cache for dashboard data to avoid expensive queries on every call.
    private DeduplicationDashboardData? _cachedDashboardData;
    private DateTimeOffset _cacheExpiry = DateTimeOffset.MinValue;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromSeconds(30);
    private readonly object _cacheLock = new();

    /// <summary>
    /// Average chunk size in bytes for storage savings estimation.
    /// </summary>
    private const int AverageChunkSizeBytes = 2048;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeduplicationMetricsService"/> class.
    /// </summary>
    /// <param name="licenseContext">License context for feature gating.</param>
    /// <param name="deduplicationService">Deduplication service for pending review queries.</param>
    /// <param name="contradictionService">Contradiction service for pending contradiction queries.</param>
    /// <param name="logger">Logger for structured diagnostics.</param>
    public DeduplicationMetricsService(
        ILicenseContext licenseContext,
        IDeduplicationService deduplicationService,
        IContradictionService contradictionService,
        ILogger<DeduplicationMetricsService> logger)
    {
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _deduplicationService = deduplicationService ?? throw new ArgumentNullException(nameof(deduplicationService));
        _contradictionService = contradictionService ?? throw new ArgumentNullException(nameof(contradictionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("DeduplicationMetricsService initialized");
    }

    #region Recording Methods

    /// <inheritdoc/>
    public void RecordChunkProcessed(DeduplicationAction action, TimeSpan processingTime)
    {
        DeduplicationMetrics.RecordChunkProcessed(action, processingTime.TotalMilliseconds);
        InvalidateCache();

        _logger.LogTrace(
            "Recorded chunk processed: Action={Action}, Duration={DurationMs}ms",
            action,
            processingTime.TotalMilliseconds);
    }

    /// <inheritdoc/>
    public void RecordSimilarityQuery(TimeSpan duration, int matchCount)
    {
        DeduplicationMetrics.RecordSimilarityQuery(duration.TotalMilliseconds, matchCount);

        _logger.LogTrace(
            "Recorded similarity query: Duration={DurationMs}ms, Matches={MatchCount}",
            duration.TotalMilliseconds,
            matchCount);
    }

    /// <inheritdoc/>
    public void RecordClassification(ClassificationMethod method, RelationshipType result, TimeSpan duration)
    {
        DeduplicationMetrics.RecordClassification(method, duration.TotalMilliseconds);

        _logger.LogTrace(
            "Recorded classification: Method={Method}, Result={Result}, Duration={DurationMs}ms",
            method,
            result,
            duration.TotalMilliseconds);
    }

    /// <inheritdoc/>
    public void RecordContradictionDetected(ContradictionSeverity severity)
    {
        DeduplicationMetrics.RecordContradictionDetected(severity);
        InvalidateCache();

        _logger.LogTrace("Recorded contradiction detected: Severity={Severity}", severity);
    }

    /// <inheritdoc/>
    public void RecordBatchJobCompleted(BatchDeduplicationResult result)
    {
        DeduplicationMetrics.RecordBatchJobCompleted();
        InvalidateCache();

        _logger.LogInformation(
            "Recorded batch job completed: Processed={Processed}, Duplicates={Duplicates}, Duration={Duration}",
            result.ChunksProcessed,
            result.DuplicatesFound,
            result.Duration);
    }

    #endregion

    #region Query Methods

    /// <inheritdoc/>
    public async Task<DeduplicationDashboardData> GetDashboardDataAsync(CancellationToken ct = default)
    {
        // LOGIC: License check - return empty data if unlicensed.
        if (!_licenseContext.IsFeatureEnabled(FeatureCodes.DeduplicationMetrics))
        {
            _logger.LogDebug("Dashboard data request denied: Feature not licensed");
            return DeduplicationDashboardData.Empty;
        }

        // LOGIC: Check cache first.
        lock (_cacheLock)
        {
            if (_cachedDashboardData is not null && DateTimeOffset.UtcNow < _cacheExpiry)
            {
                _logger.LogTrace("Returning cached dashboard data");
                return _cachedDashboardData;
            }
        }

        _logger.LogDebug("Building dashboard data");

        try
        {
            // LOGIC: Query database for live counts.
            var pendingReviews = await _deduplicationService.GetPendingReviewsAsync(null, ct);
            var pendingContradictions = await _contradictionService.GetPendingAsync(null, ct);

            // NOTE: Total canonical/variant counts are derived from static metrics since
            // ICanonicalManager doesn't have a GetAll method. For accurate counts in production,
            // consider adding a statistics query endpoint.
            var totalCanonicals = (int)DeduplicationMetrics.ChunksStoredAsNew;
            var totalVariants = (int)DeduplicationMetrics.ChunksMerged;

            // LOGIC: Build action breakdown from static metrics.
            var breakdown = new DeduplicationOperationBreakdown(
                StoredAsNew: (int)DeduplicationMetrics.ChunksStoredAsNew,
                MergedIntoExisting: (int)DeduplicationMetrics.ChunksMerged,
                LinkedToExisting: (int)DeduplicationMetrics.ChunksLinked,
                FlaggedAsContradiction: (int)DeduplicationMetrics.ChunksFlaggedContradiction,
                QueuedForReview: (int)DeduplicationMetrics.ChunksQueuedForReview,
                Errors: (int)DeduplicationMetrics.ProcessingErrors);

            // LOGIC: Calculate storage savings (estimate based on merged/linked chunks).
            var deduplicatedChunks = DeduplicationMetrics.ChunksMerged + DeduplicationMetrics.ChunksLinked;
            var storageSaved = deduplicatedChunks * AverageChunkSizeBytes;

            var dashboardData = new DeduplicationDashboardData(
                DeduplicationRate: DeduplicationMetrics.GetDeduplicationRate(),
                StorageSavedBytes: storageSaved,
                TotalCanonicalRecords: totalCanonicals,
                TotalVariants: totalVariants,
                PendingReviews: pendingReviews.Count,
                PendingContradictions: pendingContradictions.Count,
                ChunksProcessedToday: (int)DeduplicationMetrics.ChunksProcessedTotal, // TODO: Add daily reset
                AverageProcessingTimeMs: DeduplicationMetrics.GetAverageProcessingDuration(),
                ActionBreakdown: breakdown);

            // LOGIC: Update cache.
            lock (_cacheLock)
            {
                _cachedDashboardData = dashboardData;
                _cacheExpiry = DateTimeOffset.UtcNow.Add(_cacheDuration);
            }

            return dashboardData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building dashboard data");

            // Return partial data from static metrics.
            return new DeduplicationDashboardData(
                DeduplicationRate: DeduplicationMetrics.GetDeduplicationRate(),
                StorageSavedBytes: 0,
                TotalCanonicalRecords: 0,
                TotalVariants: 0,
                PendingReviews: 0,
                PendingContradictions: 0,
                ChunksProcessedToday: (int)DeduplicationMetrics.ChunksProcessedTotal,
                AverageProcessingTimeMs: DeduplicationMetrics.GetAverageProcessingDuration(),
                ActionBreakdown: DeduplicationOperationBreakdown.Empty);
        }
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<DeduplicationTrend>> GetTrendsAsync(
        TimeSpan period,
        TimeSpan? interval = null,
        CancellationToken ct = default)
    {
        // LOGIC: License check - return empty list if unlicensed.
        if (!_licenseContext.IsFeatureEnabled(FeatureCodes.DeduplicationMetrics))
        {
            _logger.LogDebug("Trends request denied: Feature not licensed");
            return Task.FromResult<IReadOnlyList<DeduplicationTrend>>(Array.Empty<DeduplicationTrend>());
        }

        // LOGIC: Determine interval based on period if not specified.
        var effectiveInterval = interval ?? DetermineInterval(period);

        _logger.LogDebug(
            "Getting trends for period={Period}, interval={Interval}",
            period,
            effectiveInterval);

        // NOTE: Full trend data would require persistent time-series storage.
        // For now, return a single data point representing current state.
        var now = DateTimeOffset.UtcNow;
        var trends = new List<DeduplicationTrend>
        {
            new DeduplicationTrend(
                Timestamp: now,
                ChunksProcessed: (int)DeduplicationMetrics.ChunksProcessedTotal,
                MergedCount: (int)DeduplicationMetrics.ChunksMerged,
                ContradictionsDetected: (int)DeduplicationMetrics.ContradictionsDetectedTotal,
                DeduplicationRate: DeduplicationMetrics.GetDeduplicationRate())
        };

        return Task.FromResult<IReadOnlyList<DeduplicationTrend>>(trends);
    }

    /// <inheritdoc/>
    public Task<DeduplicationHealthStatus> GetHealthStatusAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Getting health status");

        // LOGIC: Calculate P99 latencies.
        var similarityP99 = DeduplicationMetrics.GetSimilarityQueryDurationP99();
        var classificationP99 = DeduplicationMetrics.GetClassificationDurationP99();
        var processingP99 = DeduplicationMetrics.GetProcessingDurationP99();

        // LOGIC: Check against targets.
        var withinTargets =
            similarityP99 <= DeduplicationHealthStatus.SimilarityQueryTargetMs &&
            classificationP99 <= DeduplicationHealthStatus.ClassificationTargetMs &&
            processingP99 <= DeduplicationHealthStatus.ProcessingTargetMs;

        // LOGIC: Determine health level and build warnings list.
        var warnings = new List<string>();
        var level = HealthLevel.Healthy;

        if (similarityP99 > DeduplicationHealthStatus.SimilarityQueryTargetMs)
        {
            warnings.Add($"Similarity query P99 ({similarityP99:F1}ms) exceeds target ({DeduplicationHealthStatus.SimilarityQueryTargetMs}ms)");
            level = HealthLevel.Degraded;
        }

        if (classificationP99 > DeduplicationHealthStatus.ClassificationTargetMs)
        {
            warnings.Add($"Classification P99 ({classificationP99:F1}ms) exceeds target ({DeduplicationHealthStatus.ClassificationTargetMs}ms)");
            level = HealthLevel.Degraded;
        }

        if (processingP99 > DeduplicationHealthStatus.ProcessingTargetMs)
        {
            warnings.Add($"Processing P99 ({processingP99:F1}ms) exceeds target ({DeduplicationHealthStatus.ProcessingTargetMs}ms)");
            level = HealthLevel.Degraded;
        }

        // LOGIC: Check for high error rate.
        var totalProcessed = DeduplicationMetrics.ChunksProcessedTotal;
        var errors = DeduplicationMetrics.ProcessingErrors;
        if (totalProcessed > 0 && (double)errors / totalProcessed > 0.05)
        {
            warnings.Add($"High error rate: {errors}/{totalProcessed} ({(double)errors / totalProcessed:P1})");
            level = HealthLevel.Unhealthy;
        }

        var message = level switch
        {
            HealthLevel.Healthy => "All systems operating normally",
            HealthLevel.Degraded => "Some performance targets are not being met",
            HealthLevel.Unhealthy => "Critical issues detected",
            _ => "Unknown status"
        };

        var status = new DeduplicationHealthStatus(
            Level: level,
            Message: message,
            Warnings: warnings,
            SimilarityQueryP99Ms: similarityP99,
            ClassificationP99Ms: classificationP99,
            ProcessingP99Ms: processingP99,
            IsWithinPerformanceTargets: withinTargets);

        _logger.LogDebug(
            "Health status: Level={Level}, Warnings={WarningCount}",
            level,
            warnings.Count);

        return Task.FromResult(status);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Invalidates the cached dashboard data.
    /// </summary>
    private void InvalidateCache()
    {
        lock (_cacheLock)
        {
            _cachedDashboardData = null;
            _cacheExpiry = DateTimeOffset.MinValue;
        }
    }

    /// <summary>
    /// Determines the appropriate interval based on the time period.
    /// </summary>
    /// <param name="period">The time period.</param>
    /// <returns>The suggested interval.</returns>
    private static TimeSpan DetermineInterval(TimeSpan period)
    {
        if (period <= TimeSpan.FromHours(24))
            return TimeSpan.FromHours(1);
        if (period <= TimeSpan.FromDays(7))
            return TimeSpan.FromDays(1);
        return TimeSpan.FromDays(7);
    }

    #endregion
}
