// -----------------------------------------------------------------------
// <copyright file="SqliteFeedbackStore.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Agents;
using Lexichord.Modules.Agents.Tuning.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lexichord.Modules.Agents.Tuning.Storage;

/// <summary>
/// SQLite-based storage for learning feedback data.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Implements <see cref="IFeedbackStore"/> using <c>Microsoft.Data.Sqlite</c>
/// with connection pooling and parameterized queries. Follows the
/// <c>SqliteEmbeddingCache</c> pattern from <c>Lexichord.Modules.RAG</c>.
/// </para>
/// <para>
/// <b>Schema:</b> Manages two tables:
/// <list type="bullet">
///   <item><description><c>feedback</c> — Raw feedback records with indexes on rule_id, timestamp, and decision</description></item>
///   <item><description><c>pattern_cache</c> — Computed aggregations with composite primary key</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Connection Strategy:</b> Uses <c>SqliteConnectionStringBuilder</c> with
/// <c>Pooling = true</c> and <c>Cache = Shared</c>. Each method opens a fresh
/// connection from the pool.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Thread-safe via connection pooling — each operation
/// opens its own pooled connection.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5d as part of the Learning Loop feature.
/// </para>
/// </remarks>
internal sealed class SqliteFeedbackStore : IFeedbackStore, IDisposable
{
    private readonly string _connectionString;
    private readonly ILogger<SqliteFeedbackStore> _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteFeedbackStore"/> class.
    /// </summary>
    /// <param name="options">Storage configuration options.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> or <paramref name="logger"/> is null.
    /// </exception>
    public SqliteFeedbackStore(
        IOptions<LearningStorageOptions> options,
        ILogger<SqliteFeedbackStore> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;

        // LOGIC: Build the SQLite connection string with pooling enabled.
        // This follows the SqliteEmbeddingCache pattern: build once, open per-method.
        var dbPath = GetDatabasePath(options.Value);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            Pooling = true
        }.ToString();

        _logger.LogDebug(
            "SqliteFeedbackStore created with database path: {DatabasePath}",
            dbPath);
    }

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken ct = default)
    {
        // LOGIC: Create tables and indexes if they don't exist.
        // Using synchronous execution since schema creation is fast and one-time.
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS feedback (
                id TEXT PRIMARY KEY,
                suggestion_id TEXT NOT NULL,
                deviation_id TEXT NOT NULL,
                rule_id TEXT NOT NULL,
                category TEXT NOT NULL,
                decision INTEGER NOT NULL,
                original_text TEXT,
                suggested_text TEXT,
                final_text TEXT,
                user_modification TEXT,
                original_confidence REAL NOT NULL,
                timestamp TEXT NOT NULL,
                user_comment TEXT,
                anonymized_user_id TEXT,
                is_bulk_operation INTEGER NOT NULL DEFAULT 0
            );

            CREATE INDEX IF NOT EXISTS idx_feedback_rule ON feedback(rule_id);
            CREATE INDEX IF NOT EXISTS idx_feedback_timestamp ON feedback(timestamp);
            CREATE INDEX IF NOT EXISTS idx_feedback_decision ON feedback(decision);

            CREATE TABLE IF NOT EXISTS pattern_cache (
                rule_id TEXT NOT NULL,
                pattern_type TEXT NOT NULL,
                original_pattern TEXT NOT NULL,
                suggested_pattern TEXT NOT NULL,
                count INTEGER NOT NULL,
                success_rate REAL NOT NULL,
                last_updated TEXT NOT NULL,
                PRIMARY KEY (rule_id, pattern_type, original_pattern, suggested_pattern)
            );
            """;
        cmd.ExecuteNonQuery();

        _logger.LogDebug("Learning database schema initialized");

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StoreFeedbackAsync(FeedbackRecord record, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO feedback (
                id, suggestion_id, deviation_id, rule_id, category,
                decision, original_text, suggested_text, final_text,
                user_modification, original_confidence, timestamp,
                user_comment, anonymized_user_id, is_bulk_operation
            ) VALUES (
                $id, $suggestion_id, $deviation_id, $rule_id, $category,
                $decision, $original_text, $suggested_text, $final_text,
                $user_modification, $original_confidence, $timestamp,
                $user_comment, $anonymized_user_id, $is_bulk_operation
            )
            """;

        cmd.Parameters.AddWithValue("$id", record.Id);
        cmd.Parameters.AddWithValue("$suggestion_id", record.SuggestionId);
        cmd.Parameters.AddWithValue("$deviation_id", record.DeviationId);
        cmd.Parameters.AddWithValue("$rule_id", record.RuleId);
        cmd.Parameters.AddWithValue("$category", record.Category);
        cmd.Parameters.AddWithValue("$decision", record.Decision);
        cmd.Parameters.AddWithValue("$original_text", (object?)record.OriginalText ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$suggested_text", (object?)record.SuggestedText ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$final_text", (object?)record.FinalText ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$user_modification", (object?)record.UserModification ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$original_confidence", record.OriginalConfidence);
        cmd.Parameters.AddWithValue("$timestamp", record.Timestamp);
        cmd.Parameters.AddWithValue("$user_comment", (object?)record.UserComment ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$anonymized_user_id", (object?)record.AnonymizedUserId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$is_bulk_operation", record.IsBulkOperation);

        cmd.ExecuteNonQuery();

        _logger.LogDebug("Feedback record stored: {FeedbackId}", record.Id);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<FeedbackRecord>> GetFeedbackByRuleAsync(
        string ruleId,
        int limit = 1000,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(ruleId);

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT id, suggestion_id, deviation_id, rule_id, category,
                   decision, original_text, suggested_text, final_text,
                   user_modification, original_confidence, timestamp,
                   user_comment, anonymized_user_id, is_bulk_operation
            FROM feedback
            WHERE rule_id = $rule_id
            ORDER BY timestamp DESC
            LIMIT $limit
            """;
        cmd.Parameters.AddWithValue("$rule_id", ruleId);
        cmd.Parameters.AddWithValue("$limit", limit);

        var records = new List<FeedbackRecord>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            records.Add(ReadFeedbackRecord(reader));
        }

        _logger.LogDebug(
            "Retrieved {Count} feedback records for rule {RuleId}",
            records.Count, ruleId);

        return Task.FromResult<IReadOnlyList<FeedbackRecord>>(records);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PatternCacheRecord>> GetAcceptedPatternsAsync(
        string ruleId,
        int minFrequency = 2,
        CancellationToken ct = default)
    {
        return GetPatternsByTypeAsync(ruleId, "Accepted", minFrequency, ct);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<PatternCacheRecord>> GetRejectedPatternsAsync(
        string ruleId,
        int minFrequency = 2,
        CancellationToken ct = default)
    {
        return GetPatternsByTypeAsync(ruleId, "Rejected", minFrequency, ct);
    }

    /// <inheritdoc />
    public Task<LearningStatistics> GetStatisticsAsync(
        LearningStatisticsFilter? filter = null,
        CancellationToken ct = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        // LOGIC: Build the WHERE clause dynamically based on filter criteria.
        var whereClauses = new List<string>();
        var parameters = new Dictionary<string, object>();

        if (filter?.Period?.Start is not null)
        {
            whereClauses.Add("timestamp >= $start");
            parameters["$start"] = filter.Period.Start.Value.ToString("O");
        }

        if (filter?.Period?.End is not null)
        {
            whereClauses.Add("timestamp <= $end");
            parameters["$end"] = filter.Period.End.Value.ToString("O");
        }

        if (filter?.ExcludeSkipped == true)
        {
            whereClauses.Add("decision != $skipped");
            parameters["$skipped"] = (int)FeedbackDecision.Skipped;
        }

        if (filter?.ExcludeBulk == true)
        {
            whereClauses.Add("is_bulk_operation = 0");
        }

        if (filter?.RuleIds is { Count: > 0 })
        {
            // LOGIC: Build IN clause with numbered parameters for safety
            var ruleParams = new List<string>();
            for (var i = 0; i < filter.RuleIds.Count; i++)
            {
                var paramName = $"$rule_{i}";
                ruleParams.Add(paramName);
                parameters[paramName] = filter.RuleIds[i];
            }
            whereClauses.Add($"rule_id IN ({string.Join(", ", ruleParams)})");
        }

        if (filter?.Categories is { Count: > 0 })
        {
            var catParams = new List<string>();
            for (var i = 0; i < filter.Categories.Count; i++)
            {
                var paramName = $"$cat_{i}";
                catParams.Add(paramName);
                parameters[paramName] = filter.Categories[i];
            }
            whereClauses.Add($"category IN ({string.Join(", ", catParams)})");
        }

        var whereClause = whereClauses.Count > 0
            ? "WHERE " + string.Join(" AND ", whereClauses)
            : "";

        // LOGIC: Fetch all matching records for aggregation.
        // For large datasets this could be optimized with SQL aggregation,
        // but LINQ-based aggregation keeps the code clear and testable.
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"""
            SELECT id, suggestion_id, deviation_id, rule_id, category,
                   decision, original_text, suggested_text, final_text,
                   user_modification, original_confidence, timestamp,
                   user_comment, anonymized_user_id, is_bulk_operation
            FROM feedback
            {whereClause}
            """;

        foreach (var (key, value) in parameters)
        {
            cmd.Parameters.AddWithValue(key, value);
        }

        var records = new List<FeedbackRecord>();
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                records.Add(ReadFeedbackRecord(reader));
            }
        }

        // LOGIC: Aggregate statistics from the fetched records.
        var total = records.Count;
        var accepted = records.Count(r => r.Decision == (int)FeedbackDecision.Accepted);
        var modified = records.Count(r => r.Decision == (int)FeedbackDecision.Modified);
        var rejected = records.Count(r => r.Decision == (int)FeedbackDecision.Rejected);
        var skipped = records.Count(r => r.Decision == (int)FeedbackDecision.Skipped);

        var nonSkipped = total - skipped;
        var acceptanceRate = nonSkipped > 0 ? (double)(accepted + modified) / nonSkipped : 0;
        var modificationRate = nonSkipped > 0 ? (double)modified / nonSkipped : 0;
        var skipRate = total > 0 ? (double)skipped / total : 0;

        // LOGIC: Aggregate by rule
        var byRule = records
            .GroupBy(r => r.RuleId)
            .ToDictionary(
                g => g.Key,
                g => new RuleLearningStats(
                    g.Key,
                    g.Key,
                    g.Count(),
                    CalculateAcceptanceRate(g),
                    CalculateModificationRate(g),
                    CalculateConfidenceAccuracy(g)));

        // LOGIC: Aggregate by category
        var byCategory = records
            .GroupBy(r => r.Category)
            .ToDictionary(
                g => g.Key,
                g => new CategoryLearningStats(
                    g.Key,
                    g.Count(),
                    CalculateAcceptanceRate(g),
                    GetTopRulesByAcceptance(g, 3),
                    GetTopRulesByRejection(g, 3)));

        var stats = new LearningStatistics
        {
            TotalFeedback = total,
            OverallAcceptanceRate = acceptanceRate,
            AcceptanceRateTrend = 0, // LOGIC: Trend calculation deferred to service layer
            RulesWithFeedback = byRule.Count,
            ByRule = byRule,
            ByCategory = byCategory,
            Period = filter?.Period ?? new DateRange(null, null),
            ModificationRate = modificationRate,
            SkipRate = skipRate
        };

        _logger.LogDebug(
            "Statistics computed: {Total} total, {AcceptanceRate:P1} acceptance rate",
            total, acceptanceRate);

        return Task.FromResult(stats);
    }

    /// <inheritdoc />
    public Task UpdatePatternCacheAsync(string ruleId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(ruleId);

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            // LOGIC: Clear existing patterns for this rule
            using (var deleteCmd = connection.CreateCommand())
            {
                deleteCmd.Transaction = transaction;
                deleteCmd.CommandText = "DELETE FROM pattern_cache WHERE rule_id = $rule_id";
                deleteCmd.Parameters.AddWithValue("$rule_id", ruleId);
                deleteCmd.ExecuteNonQuery();
            }

            // LOGIC: Fetch feedback records for re-aggregation
            using var selectCmd = connection.CreateCommand();
            selectCmd.Transaction = transaction;
            selectCmd.CommandText = """
                SELECT original_text, suggested_text, decision
                FROM feedback
                WHERE rule_id = $rule_id AND decision != $skipped
                    AND original_text IS NOT NULL AND suggested_text IS NOT NULL
                """;
            selectCmd.Parameters.AddWithValue("$rule_id", ruleId);
            selectCmd.Parameters.AddWithValue("$skipped", (int)FeedbackDecision.Skipped);

            // LOGIC: Group by (original, suggested) and compute acceptance stats
            var groups = new Dictionary<(string Original, string Suggested), (int Accepted, int Rejected, int Total)>();
            using (var reader = selectCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    var original = reader.GetString(0);
                    var suggested = reader.GetString(1);
                    var decision = reader.GetInt32(2);

                    var key = (original, suggested);
                    if (!groups.TryGetValue(key, out var counts))
                    {
                        counts = (0, 0, 0);
                    }

                    var isAccepted = decision is (int)FeedbackDecision.Accepted or (int)FeedbackDecision.Modified;
                    var isRejected = decision == (int)FeedbackDecision.Rejected;

                    groups[key] = (
                        counts.Accepted + (isAccepted ? 1 : 0),
                        counts.Rejected + (isRejected ? 1 : 0),
                        counts.Total + 1);
                }
            }

            // LOGIC: Insert computed patterns into the cache
            var now = DateTime.UtcNow.ToString("O");
            foreach (var ((original, suggested), (acceptedCount, rejectedCount, totalCount)) in groups)
            {
                if (totalCount < 2) continue; // LOGIC: Skip patterns with insufficient data

                var successRate = (double)acceptedCount / totalCount;
                var patternType = successRate >= 0.5 ? "Accepted" : "Rejected";
                var count = patternType == "Accepted" ? acceptedCount : rejectedCount;

                using var insertCmd = connection.CreateCommand();
                insertCmd.Transaction = transaction;
                insertCmd.CommandText = """
                    INSERT OR REPLACE INTO pattern_cache
                        (rule_id, pattern_type, original_pattern, suggested_pattern, count, success_rate, last_updated)
                    VALUES
                        ($rule_id, $pattern_type, $original, $suggested, $count, $success_rate, $last_updated)
                    """;
                insertCmd.Parameters.AddWithValue("$rule_id", ruleId);
                insertCmd.Parameters.AddWithValue("$pattern_type", patternType);
                insertCmd.Parameters.AddWithValue("$original", original);
                insertCmd.Parameters.AddWithValue("$suggested", suggested);
                insertCmd.Parameters.AddWithValue("$count", count);
                insertCmd.Parameters.AddWithValue("$success_rate", successRate);
                insertCmd.Parameters.AddWithValue("$last_updated", now);
                insertCmd.ExecuteNonQuery();
            }

            transaction.Commit();

            _logger.LogDebug(
                "Pattern cache updated for rule {RuleId}: {PatternCount} patterns",
                ruleId, groups.Count);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ClearDataAsync(ClearLearningDataOptions options, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            if (options.ClearAll)
            {
                // LOGIC: Delete everything from both tables
                ExecuteNonQuery(connection, transaction, "DELETE FROM feedback");
                ExecuteNonQuery(connection, transaction, "DELETE FROM pattern_cache");

                _logger.LogInformation("All learning data cleared");
            }
            else
            {
                // LOGIC: Build targeted delete based on filter criteria
                var whereClauses = new List<string>();
                var parameters = new Dictionary<string, object>();

                if (options.Period?.Start is not null)
                {
                    whereClauses.Add("timestamp >= $start");
                    parameters["$start"] = options.Period.Start.Value.ToString("O");
                }

                if (options.Period?.End is not null)
                {
                    whereClauses.Add("timestamp <= $end");
                    parameters["$end"] = options.Period.End.Value.ToString("O");
                }

                if (options.RuleIds is { Count: > 0 })
                {
                    var ruleParams = new List<string>();
                    for (var i = 0; i < options.RuleIds.Count; i++)
                    {
                        var paramName = $"$rule_{i}";
                        ruleParams.Add(paramName);
                        parameters[paramName] = options.RuleIds[i];
                    }
                    whereClauses.Add($"rule_id IN ({string.Join(", ", ruleParams)})");
                }

                if (whereClauses.Count > 0)
                {
                    var whereClause = "WHERE " + string.Join(" AND ", whereClauses);

                    using var deleteCmd = connection.CreateCommand();
                    deleteCmd.Transaction = transaction;
                    deleteCmd.CommandText = $"DELETE FROM feedback {whereClause}";
                    foreach (var (key, value) in parameters)
                    {
                        deleteCmd.Parameters.AddWithValue(key, value);
                    }
                    var deleted = deleteCmd.ExecuteNonQuery();

                    // LOGIC: Also clear pattern cache for affected rules
                    if (options.RuleIds is { Count: > 0 })
                    {
                        foreach (var ruleId in options.RuleIds)
                        {
                            using var cacheDeleteCmd = connection.CreateCommand();
                            cacheDeleteCmd.Transaction = transaction;
                            cacheDeleteCmd.CommandText = "DELETE FROM pattern_cache WHERE rule_id = $rule_id";
                            cacheDeleteCmd.Parameters.AddWithValue("$rule_id", ruleId);
                            cacheDeleteCmd.ExecuteNonQuery();
                        }
                    }

                    _logger.LogInformation("Cleared {Count} feedback records", deleted);
                }
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<int> GetFeedbackCountAsync(DateRange? period = null, CancellationToken ct = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();

        if (period?.Start is not null || period?.End is not null)
        {
            var whereClauses = new List<string>();
            if (period.Start is not null)
            {
                whereClauses.Add("timestamp >= $start");
                cmd.Parameters.AddWithValue("$start", period.Start.Value.ToString("O"));
            }
            if (period.End is not null)
            {
                whereClauses.Add("timestamp <= $end");
                cmd.Parameters.AddWithValue("$end", period.End.Value.ToString("O"));
            }
            cmd.CommandText = $"SELECT COUNT(*) FROM feedback WHERE {string.Join(" AND ", whereClauses)}";
        }
        else
        {
            cmd.CommandText = "SELECT COUNT(*) FROM feedback";
        }

        var count = Convert.ToInt32(cmd.ExecuteScalar());

        return Task.FromResult(count);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ExportedPattern>> ExportPatternsAsync(
        LearningExportOptions options,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();

        var whereClauses = new List<string>();
        if (options.RuleIds is { Count: > 0 })
        {
            var ruleParams = new List<string>();
            for (var i = 0; i < options.RuleIds.Count; i++)
            {
                var paramName = $"$rule_{i}";
                ruleParams.Add(paramName);
                cmd.Parameters.AddWithValue(paramName, options.RuleIds[i]);
            }
            whereClauses.Add($"rule_id IN ({string.Join(", ", ruleParams)})");
        }

        var whereClause = whereClauses.Count > 0
            ? "WHERE " + string.Join(" AND ", whereClauses)
            : "";

        cmd.CommandText = $"""
            SELECT rule_id, pattern_type, original_pattern, suggested_pattern, count, success_rate
            FROM pattern_cache
            {whereClause}
            ORDER BY count DESC
            """;

        var patterns = new List<ExportedPattern>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            patterns.Add(new ExportedPattern(
                RuleId: reader.GetString(0),
                PatternType: reader.GetString(1),
                OriginalPattern: options.IncludeOriginalText ? reader.GetString(2) : "[REDACTED]",
                SuggestedPattern: options.IncludeOriginalText ? reader.GetString(3) : "[REDACTED]",
                Count: reader.GetInt32(4),
                SuccessRate: reader.GetDouble(5)));
        }

        _logger.LogInformation(
            "Exported {Count} patterns",
            patterns.Count);

        return Task.FromResult<IReadOnlyList<ExportedPattern>>(patterns);
    }

    /// <inheritdoc />
    public Task<int> DeleteOlderThanAsync(DateTime cutoff, CancellationToken ct = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM feedback WHERE timestamp < $cutoff";
        cmd.Parameters.AddWithValue("$cutoff", cutoff.ToString("O"));

        var deleted = cmd.ExecuteNonQuery();

        if (deleted > 0)
        {
            _logger.LogInformation(
                "Deleted {Count} feedback records older than {Cutoff}",
                deleted, cutoff);
        }

        return Task.FromResult(deleted);
    }

    /// <inheritdoc />
    public Task<int> DeleteOldestAsync(int count, CancellationToken ct = default)
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            DELETE FROM feedback WHERE id IN (
                SELECT id FROM feedback ORDER BY timestamp ASC LIMIT $count
            )
            """;
        cmd.Parameters.AddWithValue("$count", count);

        var deleted = cmd.ExecuteNonQuery();

        if (deleted > 0)
        {
            _logger.LogInformation(
                "Deleted {Count} oldest feedback records to enforce limit",
                deleted);
        }

        return Task.FromResult(deleted);
    }

    /// <summary>
    /// Disposes pooled connections.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // LOGIC: Release pooled connections held by the driver.
        // Follows the SqliteEmbeddingCache pattern.
        SqliteConnection.ClearAllPools();

        _logger.LogDebug("SqliteFeedbackStore disposed");
    }

    #region Private Helpers

    /// <summary>
    /// Determines the database file path from options or platform defaults.
    /// </summary>
    private static string GetDatabasePath(LearningStorageOptions options)
    {
        if (!string.IsNullOrEmpty(options.DatabasePath))
            return options.DatabasePath;

        // LOGIC: Use platform-specific default location
        string basePath;
        if (OperatingSystem.IsWindows())
        {
            basePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Lexichord", "Learning");
        }
        else if (OperatingSystem.IsMacOS())
        {
            basePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library", "Application Support", "Lexichord", "Learning");
        }
        else
        {
            basePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".config", "Lexichord", "Learning");
        }

        Directory.CreateDirectory(basePath);
        return Path.Combine(basePath, "learning.db");
    }

    /// <summary>
    /// Gets patterns by type from the pattern cache.
    /// </summary>
    private Task<IReadOnlyList<PatternCacheRecord>> GetPatternsByTypeAsync(
        string ruleId,
        string patternType,
        int minFrequency,
        CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrEmpty(ruleId);

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT rule_id, pattern_type, original_pattern, suggested_pattern,
                   count, success_rate, last_updated
            FROM pattern_cache
            WHERE rule_id = $rule_id AND pattern_type = $pattern_type AND count >= $min_freq
            ORDER BY count DESC
            """;
        cmd.Parameters.AddWithValue("$rule_id", ruleId);
        cmd.Parameters.AddWithValue("$pattern_type", patternType);
        cmd.Parameters.AddWithValue("$min_freq", minFrequency);

        var records = new List<PatternCacheRecord>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            records.Add(new PatternCacheRecord
            {
                RuleId = reader.GetString(0),
                PatternType = reader.GetString(1),
                OriginalPattern = reader.GetString(2),
                SuggestedPattern = reader.GetString(3),
                Count = reader.GetInt32(4),
                SuccessRate = reader.GetDouble(5),
                LastUpdated = reader.GetString(6)
            });
        }

        return Task.FromResult<IReadOnlyList<PatternCacheRecord>>(records);
    }

    /// <summary>
    /// Reads a FeedbackRecord from a SqliteDataReader at the current position.
    /// </summary>
    private static FeedbackRecord ReadFeedbackRecord(SqliteDataReader reader)
    {
        return new FeedbackRecord
        {
            Id = reader.GetString(0),
            SuggestionId = reader.GetString(1),
            DeviationId = reader.GetString(2),
            RuleId = reader.GetString(3),
            Category = reader.GetString(4),
            Decision = reader.GetInt32(5),
            OriginalText = reader.IsDBNull(6) ? null : reader.GetString(6),
            SuggestedText = reader.IsDBNull(7) ? null : reader.GetString(7),
            FinalText = reader.IsDBNull(8) ? null : reader.GetString(8),
            UserModification = reader.IsDBNull(9) ? null : reader.GetString(9),
            OriginalConfidence = reader.GetDouble(10),
            Timestamp = reader.GetString(11),
            UserComment = reader.IsDBNull(12) ? null : reader.GetString(12),
            AnonymizedUserId = reader.IsDBNull(13) ? null : reader.GetString(13),
            IsBulkOperation = reader.GetInt32(14)
        };
    }

    /// <summary>
    /// Executes a non-query SQL command within a transaction.
    /// </summary>
    private static void ExecuteNonQuery(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string sql)
    {
        using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Calculates acceptance rate from a group of feedback records.
    /// </summary>
    private static double CalculateAcceptanceRate(IEnumerable<FeedbackRecord> records)
    {
        var list = records.ToList();
        var nonSkipped = list.Count(r => r.Decision != (int)FeedbackDecision.Skipped);
        if (nonSkipped == 0) return 0;
        var accepted = list.Count(r =>
            r.Decision is (int)FeedbackDecision.Accepted or (int)FeedbackDecision.Modified);
        return (double)accepted / nonSkipped;
    }

    /// <summary>
    /// Calculates modification rate from a group of feedback records.
    /// </summary>
    private static double CalculateModificationRate(IEnumerable<FeedbackRecord> records)
    {
        var list = records.ToList();
        var nonSkipped = list.Count(r => r.Decision != (int)FeedbackDecision.Skipped);
        if (nonSkipped == 0) return 0;
        var modified = list.Count(r => r.Decision == (int)FeedbackDecision.Modified);
        return (double)modified / nonSkipped;
    }

    /// <summary>
    /// Calculates how well confidence predicted acceptance.
    /// </summary>
    private static double CalculateConfidenceAccuracy(IEnumerable<FeedbackRecord> records)
    {
        var list = records.Where(r => r.Decision != (int)FeedbackDecision.Skipped).ToList();
        if (list.Count == 0) return 0;

        // LOGIC: A prediction is "correct" when:
        // - High confidence (>= 0.8) and the user accepted/modified
        // - Low confidence (< 0.8) and the user rejected
        var correct = list.Count(r =>
            (r.OriginalConfidence >= 0.8 &&
             r.Decision is (int)FeedbackDecision.Accepted or (int)FeedbackDecision.Modified) ||
            (r.OriginalConfidence < 0.8 &&
             r.Decision == (int)FeedbackDecision.Rejected));

        return (double)correct / list.Count;
    }

    /// <summary>
    /// Gets top rules by acceptance rate within a category.
    /// </summary>
    private static IReadOnlyList<string> GetTopRulesByAcceptance(
        IEnumerable<FeedbackRecord> records,
        int count)
    {
        return records
            .GroupBy(r => r.RuleId)
            .OrderByDescending(g => CalculateAcceptanceRate(g))
            .Take(count)
            .Select(g => g.Key)
            .ToList();
    }

    /// <summary>
    /// Gets top rules by rejection rate within a category.
    /// </summary>
    private static IReadOnlyList<string> GetTopRulesByRejection(
        IEnumerable<FeedbackRecord> records,
        int count)
    {
        return records
            .GroupBy(r => r.RuleId)
            .OrderBy(g => CalculateAcceptanceRate(g))
            .Take(count)
            .Select(g => g.Key)
            .ToList();
    }

    #endregion
}
