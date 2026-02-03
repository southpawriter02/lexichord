// =============================================================================
// File: QueryHistoryService.cs
// Project: Lexichord.Modules.RAG
// Description: Implementation of query history tracking and analytics (v0.5.4d).
// =============================================================================
// LOGIC: Tracks search patterns to identify content gaps and improve search:
//   - Records executed queries with metadata
//   - Identifies zero-result queries (content gaps)
//   - Publishes anonymized analytics events (opt-in)
// =============================================================================
// VERSION: v0.5.4d (Query History & Analytics)
// DEPENDENCIES:
//   - IDbConnectionFactory (v0.0.5b) for database access
//   - ILicenseContext (v0.0.4c) for Writer Pro gating
//   - IMediator (v0.0.7a) for event publishing
//   - ILogger<T> (v0.0.3b) for structured logging
// =============================================================================

using System.Security.Cryptography;
using System.Text;
using Dapper;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.Search;

/// <summary>
/// Tracks query history and provides search analytics.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="QueryHistoryService"/> stores executed queries locally and provides:
/// <list type="bullet">
///   <item><description>Recent queries for quick-access panel</description></item>
///   <item><description>Zero-result query identification</description></item>
///   <item><description>Optional anonymized telemetry via MediatR</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Privacy:</b> Query text is stored locally only. Analytics events use
/// SHA256-hashed queries. Telemetry is opt-in via settings.
/// </para>
/// <para>
/// <b>License Gate:</b> History tracking is gated at Writer Pro tier.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.4d as part of The Relevance Tuner feature.
/// </para>
/// </remarks>
public sealed class QueryHistoryService : IQueryHistoryService
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILicenseContext _licenseContext;
    private readonly IMediator _mediator;
    private readonly ILogger<QueryHistoryService> _logger;

    /// <summary>
    /// Feature flag for Writer Pro gating.
    /// </summary>
    private const string FeatureCode = "RAG.RelevanceTuner";

    /// <summary>
    /// Deduplication window in seconds (avoid recording duplicate searches).
    /// </summary>
    private const int DeduplicationWindowSeconds = 60;

    /// <summary>
    /// Last recorded query for deduplication.
    /// </summary>
    private (string QueryHash, DateTime RecordedAt)? _lastRecorded;
    private readonly object _lastRecordedLock = new();

    /// <summary>
    /// Creates a new <see cref="QueryHistoryService"/> instance.
    /// </summary>
    /// <param name="connectionFactory">Database connection factory.</param>
    /// <param name="licenseContext">License context for Writer Pro gating.</param>
    /// <param name="mediator">MediatR for event publishing.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public QueryHistoryService(
        IDbConnectionFactory connectionFactory,
        ILicenseContext licenseContext,
        IMediator mediator,
        ILogger<QueryHistoryService> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("QueryHistoryService initialized");
    }

    /// <inheritdoc/>
    public async Task RecordAsync(QueryHistoryEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        // LOGIC: Check license.
        if (!_licenseContext.IsFeatureEnabled(FeatureCode))
        {
            _logger.LogDebug("Query history recording skipped: Writer Pro license required");
            return;
        }

        // LOGIC: Compute query hash for deduplication and analytics.
        var queryHash = ComputeHash(entry.Query);

        // LOGIC: Check deduplication window.
        lock (_lastRecordedLock)
        {
            if (_lastRecorded.HasValue &&
                _lastRecorded.Value.QueryHash == queryHash &&
                (DateTime.UtcNow - _lastRecorded.Value.RecordedAt).TotalSeconds < DeduplicationWindowSeconds)
            {
                _logger.LogDebug("Query recording skipped: duplicate within deduplication window");
                return;
            }

            _lastRecorded = (queryHash, DateTime.UtcNow);
        }

        try
        {
            await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            const string sql = """
                INSERT INTO query_history (id, query, query_hash, intent, result_count, top_result_score, executed_at, duration_ms)
                VALUES (@Id, @Query, @QueryHash, @Intent, @ResultCount, @TopResultScore, @ExecutedAt, @DurationMs)
                """;

            await connection.ExecuteAsync(sql, new
            {
                entry.Id,
                entry.Query,
                QueryHash = queryHash,
                Intent = entry.Intent.ToString(),
                entry.ResultCount,
                entry.TopResultScore,
                entry.ExecutedAt,
                entry.DurationMs
            });

            _logger.LogDebug("Recorded query: {ResultCount} results, {DurationMs}ms",
                entry.ResultCount, entry.DurationMs);

            // LOGIC: Log zero-result queries for content gap analysis.
            if (entry.ResultCount == 0)
            {
                _logger.LogInformation("Zero-result query recorded: '{Query}'", entry.Query);
            }

            // LOGIC: Publish analytics event (anonymized).
            await _mediator.Publish(new QueryAnalyticsEvent(
                QueryHash: queryHash,
                Intent: entry.Intent,
                ResultCount: entry.ResultCount,
                DurationMs: entry.DurationMs,
                Timestamp: entry.ExecutedAt), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record query history entry");
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<QueryHistoryEntry>> GetRecentAsync(
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        // LOGIC: Check license.
        if (!_licenseContext.IsFeatureEnabled(FeatureCode))
        {
            return Array.Empty<QueryHistoryEntry>();
        }

        try
        {
            await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            const string sql = """
                SELECT id, query, intent, result_count, top_result_score, executed_at, duration_ms
                FROM query_history
                ORDER BY executed_at DESC
                LIMIT @Limit
                """;

            var rows = await connection.QueryAsync<HistoryRow>(sql, new { Limit = limit });

            return rows.Select(r => new QueryHistoryEntry(
                Id: r.id,
                Query: r.query,
                Intent: Enum.TryParse<QueryIntent>(r.intent, out var intent) ? intent : QueryIntent.Factual,
                ResultCount: r.result_count,
                TopResultScore: r.top_result_score,
                ExecutedAt: r.executed_at,
                DurationMs: r.duration_ms))
                .ToList()
                .AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recent query history");
            return Array.Empty<QueryHistoryEntry>();
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ZeroResultQuery>> GetZeroResultQueriesAsync(
        DateTime since,
        CancellationToken cancellationToken = default)
    {
        // LOGIC: Check license.
        if (!_licenseContext.IsFeatureEnabled(FeatureCode))
        {
            return Array.Empty<ZeroResultQuery>();
        }

        try
        {
            await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            // LOGIC: Aggregate zero-result queries by normalized text.
            const string sql = """
                SELECT
                    LOWER(TRIM(query)) as query,
                    COUNT(*) as occurrence_count,
                    MAX(executed_at) as last_searched_at
                FROM query_history
                WHERE result_count = 0
                  AND executed_at >= @Since
                GROUP BY LOWER(TRIM(query))
                ORDER BY occurrence_count DESC, last_searched_at DESC
                LIMIT 50
                """;

            var rows = await connection.QueryAsync<ZeroResultRow>(sql, new { Since = since });

            return rows.Select(r => new ZeroResultQuery(
                Query: r.query,
                OccurrenceCount: (int)r.occurrence_count,
                LastSearchedAt: r.last_searched_at,
                SuggestedContent: null)) // AI suggestion is a future feature
                .ToList()
                .AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get zero-result queries");
            return Array.Empty<ZeroResultQuery>();
        }
    }

    /// <inheritdoc/>
    public async Task ClearAsync(DateTime? olderThan = null, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            string sql;
            object? parameters;

            if (olderThan.HasValue)
            {
                sql = "DELETE FROM query_history WHERE executed_at < @OlderThan";
                parameters = new { OlderThan = olderThan.Value };
            }
            else
            {
                sql = "DELETE FROM query_history";
                parameters = null;
            }

            var deleted = await connection.ExecuteAsync(sql, parameters);

            _logger.LogInformation("Cleared {Count} query history entries{Constraint}",
                deleted,
                olderThan.HasValue ? $" older than {olderThan.Value:yyyy-MM-dd}" : "");

            // LOGIC: Reset deduplication state.
            lock (_lastRecordedLock)
            {
                _lastRecorded = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear query history");
        }
    }

    /// <inheritdoc/>
    public async Task<int> GetTotalCountAsync(CancellationToken cancellationToken = default)
    {
        // LOGIC: Check license.
        if (!_licenseContext.IsFeatureEnabled(FeatureCode))
        {
            return 0;
        }

        try
        {
            await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            const string sql = "SELECT COUNT(*) FROM query_history";
            return await connection.ExecuteScalarAsync<int>(sql);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get query history count");
            return 0;
        }
    }

    #region Private Methods

    /// <summary>
    /// Computes SHA256 hash of query text for anonymization.
    /// </summary>
    private static string ComputeHash(string query)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(query.Trim().ToLowerInvariant()));
        return Convert.ToHexString(bytes);
    }

    /// <summary>
    /// Row type for history query.
    /// </summary>
    private record HistoryRow(
        Guid id,
        string query,
        string intent,
        int result_count,
        float? top_result_score,
        DateTime executed_at,
        long duration_ms);

    /// <summary>
    /// Row type for zero-result query.
    /// </summary>
    private record ZeroResultRow(
        string query,
        long occurrence_count,
        DateTime last_searched_at);

    #endregion
}
