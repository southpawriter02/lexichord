// =============================================================================
// File: QueryHistoryService.cs
// Project: Lexichord.Modules.RAG
// Description: Service for tracking and analyzing search query history.
// =============================================================================
// LOGIC: Implements IQueryHistoryService with:
//   - License-gated recording (Writer Pro)
//   - SHA256 hash-based deduplication
//   - Automatic cleanup (1000 entry limit)
//   - Optional analytics event publishing
//   - Zero-result content gap tracking
// =============================================================================
// VERSION: v0.5.4d (Query History & Analytics)
// =============================================================================

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Abstractions.Constants;
using Lexichord.Modules.RAG.Data;
using Lexichord.Modules.RAG.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// Service for recording and analyzing search query history.
/// </summary>
/// <remarks>
/// <para>
/// LOGIC: QueryHistoryService tracks all search queries to enable:
/// <list type="bullet">
///   <item><description>Quick re-access via recent queries list</description></item>
///   <item><description>Content gap analysis (zero-result queries)</description></item>
///   <item><description>Search performance monitoring</description></item>
///   <item><description>Intent distribution analytics</description></item>
/// </list>
/// </para>
/// <para>
/// <b>License Gating:</b> This feature requires WriterPro tier via
/// <see cref="FeatureCodes.KnowledgeHub"/>. Unlicensed users silently skip recording.
/// </para>
/// <para>
/// <b>Privacy:</b> Query text is stored locally only. Analytics events are
/// published with SHA256 hashed queries and only if telemetry is opt-ed in via ISettingsService.
/// </para>
/// <para>
/// <b>Performance:</b> Recording is fire-and-forget. Cleanup runs periodically
/// to maintain max 1000 entries automatically.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.4d as part of The Relevance Tuner feature.
/// </para>
/// </remarks>
public class QueryHistoryService : IQueryHistoryService
{
    private readonly IDbConnection _connection;
    private readonly IMediator _mediator;
    private readonly ILicenseContext _licenseContext;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<QueryHistoryService> _logger;

    private const int MaxHistoryEntries = 1000;
    private const int MaxQueryLength = 500;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryHistoryService"/> class.
    /// </summary>
    /// <param name="connection">Database connection (Npgsql).</param>
    /// <param name="mediator">MediatR for publishing analytics events.</param>
    /// <param name="licenseContext">License validation.</param>
    /// <param name="settingsService">Settings access.</param>
    /// <param name="logger">Logger instance.</param>
    public QueryHistoryService(
        IDbConnection connection,
        IMediator mediator,
        ILicenseContext licenseContext,
        ISettingsService settingsService,
        ILogger<QueryHistoryService> logger)
    {
        _connection = connection;
        _mediator = mediator;
        _licenseContext = licenseContext;
        _settingsService = settingsService;
        _logger = logger;
    }

    ///inheritdoc />
    public async Task RecordAsync(
        QueryHistoryEntry entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        // LOGIC: License gate - silently skip if not WriterPro
        if (!_licenseContext.IsFeatureEnabled(FeatureCodes.KnowledgeHub))
        {
            _logger.LogDebug("Query history disabled - user not licensed for KnowledgeHub");
            return;
        }

        // LOGIC: Sanitize query to prevent excessively long entries
        var sanitizedQuery = entry.Query.Length > MaxQueryLength
            ? entry.Query[..MaxQueryLength]
            : entry.Query;

        var queryHash = ComputeHash(sanitizedQuery);

        _logger.LogDebug(
            "Recording query: '{Query}' with {ResultCount} results in {DurationMs}ms",
            sanitizedQuery, entry.ResultCount, entry.DurationMs);

        // LOGIC: Insert query record
        const string sql = @"
            INSERT INTO query_history
                (id, query, query_hash, intent, result_count, top_result_score, executed_at, duration_ms)
            VALUES
                (@Id, @Query, @QueryHash, @Intent, @ResultCount, @TopResultScore, @ExecutedAt, @DurationMs)";

        await _connection.ExecuteAsync(sql, new
        {
            entry.Id,
            Query = sanitizedQuery,
            QueryHash = queryHash,
            Intent = entry.Intent.ToString(),
            entry.ResultCount,
            entry.TopResultScore,
            entry.ExecutedAt,
            entry.DurationMs
        });

        if (entry.IsZeroResult)
        {
            _logger.LogInformation("Zero-result query recorded: '{Query}'", sanitizedQuery);
        }

        // LOGIC: Publish analytics event if opted in
        await PublishAnalyticsEventIfEnabledAsync(entry, queryHash, cancellationToken);

        // LOGIC: Cleanup old entries periodically
        await CleanupOldEntriesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<QueryHistoryEntry>> GetRecentAsync(
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        // LOGIC: License gate - return empty list if not licensed
        if (!_licenseContext.IsFeatureEnabled(FeatureCodes.KnowledgeHub))
        {
            return Array.Empty<QueryHistoryEntry>();
        }

        // LOGIC: Clamp limit to reasonable range
        limit = Math.Clamp(limit, 1, 50);

        // LOGIC: Get most recent unique queries (deduplicated by hash)
        const string sql = @"
            SELECT DISTINCT ON (query_hash)
                id, query, query_hash, intent, result_count, top_result_score, executed_at, duration_ms
            FROM query_history
            ORDER BY query_hash, executed_at DESC
            LIMIT @Limit";

        var entities = await _connection.QueryAsync<QueryHistoryEntity>(sql, new { Limit = limit });

        return entities.Select(MapToEntry).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ZeroResultQuery>> GetZeroResultQueriesAsync(
        DateTime since,
        CancellationToken cancellationToken = default)
    {
        // LOGIC: License gate - return empty list if not licensed
        if (!_licenseContext.IsFeatureEnabled(FeatureCodes.KnowledgeHub))
        {
            return Array.Empty<ZeroResultQuery>();
        }

        // LOGIC: Group zero-result queries by hash and count occurrences
        const string sql = @"
            SELECT 
                query,
                COUNT(*) as occurrence_count,
                MAX(executed_at) as last_searched_at
            FROM query_history
            WHERE result_count = 0 
              AND executed_at >= @Since
            GROUP BY query_hash, query
            ORDER BY occurrence_count DESC, last_searched_at DESC
            LIMIT 50";

        var rows = await _connection.QueryAsync(sql, new { Since = since });

        var results = new List<ZeroResultQuery>();
        foreach (var row in rows)
        {
            results.Add(new ZeroResultQuery(
                Query: (string)row.query,
                OccurrenceCount: (int)row.occurrence_count,
                LastSearchedAt: (DateTime)row.last_searched_at,
                SuggestedContent: null));  // TODO: LLM-based suggestion in future
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        // LOGIC: License gate - return zero if not licensed
        if (!_licenseContext.IsFeatureEnabled(FeatureCodes.KnowledgeHub))
        {
            return 0;
        }

        const string sql = "SELECT COUNT(*) FROM query_history";
        var count = await _connection.ExecuteScalarAsync<int>(sql);
        return count;
    }

    /// <inheritdoc />
    public async Task ClearAsync(
        DateTime? olderThan = null,
        CancellationToken cancellationToken = default)
    {
        // LOGIC: License gate - prevent accidental clears if not licensed
        if (!_licenseContext.IsFeatureEnabled(FeatureCodes.KnowledgeHub))
        {
            _logger.LogWarning("Clear query history blocked - user not licensed");
            return;
        }

        if (olderThan.HasValue)
        {
            const string sql = "DELETE FROM query_history WHERE executed_at < @OlderThan";
            var deleted = await _connection.ExecuteAsync(sql, new { OlderThan = olderThan.Value });
            _logger.LogInformation("Cleared {Count} query history entries older than {Date}", deleted, olderThan);
        }
        else
        {
            const string sql = "DELETE FROM query_history";
            var deleted = await _connection.ExecuteAsync(sql);
            _logger.LogWarning("Cleared ALL query history ({Count} entries)", deleted);
        }
    }

    // =========================================================================
    // Private Helpers
    // =========================================================================

    /// <summary>
    /// Computes SHA256 hash of query text for deduplication.
    /// </summary>
    private static string ComputeHash(string query)
    {
        // LOGIC: SHA256 hash for secure, collision-resistant deduplication
        var normalized = query.Trim().ToLowerInvariant();
        var bytes = Encoding.UTF8.GetBytes(normalized);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Publishes analytics event if user has opted in.
    /// </summary>
    private async Task PublishAnalyticsEventIfEnabledAsync(
        QueryHistoryEntry entry,
        string queryHash,
        CancellationToken cancellationToken)
    {
        // LOGIC: Check if telemetry is enabled in settings
        if (!_settingsService.Get(TelemetrySettingsKeys.UsageAnalyticsEnabled, false))
        {
            return;
        }

        var analyticsEvent = new QueryAnalyticsEvent(
            QueryHash: queryHash,
            Intent: entry.Intent,
            ResultCount: entry.ResultCount,
            DurationMs: entry.DurationMs,
            Timestamp: entry.ExecutedAt);

        await _mediator.Publish(analyticsEvent, cancellationToken);
    }

    /// <summary>
    /// Periodically cleans up old entries to maintain max size.
    /// </summary>
    private async Task CleanupOldEntriesAsync(CancellationToken cancellationToken)
    {
        // LOGIC: Keep only the most recent MaxHistoryEntries
        const string sql = @"
            DELETE FROM query_history
            WHERE id NOT IN (
                SELECT id FROM query_history
                ORDER BY executed_at DESC
                LIMIT @MaxEntries
            )";

        var deleted = await _connection.ExecuteAsync(sql, new { MaxEntries = MaxHistoryEntries });

        if (deleted > 0)
        {
            _logger.LogDebug("Cleaned up {Count} old query history entries", deleted);
        }
    }

    /// <summary>
    /// Maps database entity to domain contract.
    /// </summary>
    private static QueryHistoryEntry MapToEntry(QueryHistoryEntity entity)
    {
        return new QueryHistoryEntry(
            Id: entity.id,
            Query: entity.query,
            Intent: Enum.Parse<QueryIntent>(entity.intent),
            ResultCount: entity.result_count,
            TopResultScore: entity.top_result_score,
            ExecutedAt: entity.executed_at,
            DurationMs: entity.duration_ms);
    }
}
