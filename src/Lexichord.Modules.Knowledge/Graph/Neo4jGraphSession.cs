// =============================================================================
// File: Neo4jGraphSession.cs
// Project: Lexichord.Modules.Knowledge
// Description: IGraphSession implementation wrapping a Neo4j IAsyncSession.
// =============================================================================
// LOGIC: Wraps the Neo4j driver's IAsyncSession with:
//   - Stopwatch timing for all queries
//   - Slow query detection (>100ms) with Warning-level logging
//   - Exception wrapping (Neo4jException → GraphQueryException)
//   - Typed result mapping via Neo4jRecordMapper
//   - Raw record access via Neo4jGraphRecord adapter
//
// Performance Monitoring:
//   - All queries are timed with System.Diagnostics.Stopwatch
//   - Debug: "{Duration}ms, {Count} records" for all queries
//   - Warning: "Slow Cypher query ({Duration}ms): {Query}" for >100ms
//
// v0.4.5e: Graph Database Integration (CKVS Phase 1)
// Dependencies: Neo4j.Driver, Microsoft.Extensions.Logging
// =============================================================================

using System.Diagnostics;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;

namespace Lexichord.Modules.Knowledge.Graph;

/// <summary>
/// Neo4j session wrapper implementing <see cref="IGraphSession"/>.
/// </summary>
/// <remarks>
/// <para>
/// Provides Cypher query execution with automatic timing, logging, and
/// exception wrapping. Sessions are short-lived and should be disposed
/// promptly after use.
/// </para>
/// <para>
/// <b>Performance:</b> Queries exceeding <see cref="SlowQueryThresholdMs"/>
/// (100ms) are logged at Warning level with the Cypher text (truncated to
/// 200 characters) for performance diagnostics.
/// </para>
/// <para>
/// <b>Error Handling:</b> All <see cref="Neo4jException"/> instances are
/// caught and re-thrown as <see cref="GraphQueryException"/> to prevent
/// Neo4j driver types from leaking through the abstraction.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5e as part of the Knowledge Graph Foundation.
/// </para>
/// </remarks>
internal sealed class Neo4jGraphSession : IGraphSession
{
    /// <summary>
    /// Threshold in milliseconds above which queries are logged as slow.
    /// </summary>
    /// <remarks>
    /// LOGIC: 100ms matches the performance target from the spec
    /// (Section 10: Success Metrics — "Neo4j query latency < 100ms avg").
    /// </remarks>
    internal const int SlowQueryThresholdMs = 100;

    private readonly IAsyncSession _session;
    private readonly int _queryTimeoutSeconds;
    private readonly Microsoft.Extensions.Logging.ILogger _logger;

    /// <summary>
    /// Initializes a new instance wrapping the specified Neo4j session.
    /// </summary>
    /// <param name="session">The Neo4j driver session to wrap.</param>
    /// <param name="queryTimeoutSeconds">Query timeout in seconds.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="session"/> or <paramref name="logger"/> is null.
    /// </exception>
    public Neo4jGraphSession(
        IAsyncSession session,
        int queryTimeoutSeconds,
        Microsoft.Extensions.Logging.ILogger logger)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _queryTimeoutSeconds = queryTimeoutSeconds;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Execution flow:
    /// 1. Start stopwatch for timing.
    /// 2. Execute Cypher via _session.RunAsync().
    /// 3. Consume all records into a list.
    /// 4. Map each record to type T via Neo4jRecordMapper.
    /// 5. Log duration and record count at Debug level.
    /// 6. Log Warning if duration exceeds SlowQueryThresholdMs.
    /// 7. On failure, wrap Neo4jException in GraphQueryException.
    /// </remarks>
    public async Task<IReadOnlyList<T>> QueryAsync<T>(
        string cypher,
        object? parameters = null,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("Executing Cypher query: {Query}", TruncateQuery(cypher));

            var cursor = await _session.RunAsync(cypher, parameters?.ToDictionary());
            var records = await cursor.ToListAsync();

            stopwatch.Stop();

            _logger.LogDebug(
                "Cypher query completed in {Duration}ms, {Count} records",
                stopwatch.ElapsedMilliseconds, records.Count);

            // LOGIC: Slow query detection — log at Warning level for performance monitoring.
            if (stopwatch.ElapsedMilliseconds > SlowQueryThresholdMs)
            {
                _logger.LogWarning(
                    "Slow Cypher query ({Duration}ms): {Query}",
                    stopwatch.ElapsedMilliseconds, TruncateQuery(cypher));
            }

            return records.Select(r => Neo4jRecordMapper.MapRecord<T>(r)).ToList();
        }
        catch (Neo4jException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Cypher query failed after {Duration}ms: {Query}",
                stopwatch.ElapsedMilliseconds, TruncateQuery(cypher));
            throw new GraphQueryException($"Graph query failed: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Returns raw records wrapped in Neo4jGraphRecord adapters.
    /// Use this for dynamic queries where the result schema is not known
    /// at compile time.
    /// </remarks>
    public async Task<IReadOnlyList<IGraphRecord>> QueryRawAsync(
        string cypher,
        object? parameters = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Executing raw Cypher query: {Query}", TruncateQuery(cypher));

            var cursor = await _session.RunAsync(cypher, parameters?.ToDictionary());
            var records = await cursor.ToListAsync();

            _logger.LogDebug("Raw query returned {Count} records", records.Count);

            return records.Select(r => (IGraphRecord)new Neo4jGraphRecord(r)).ToList();
        }
        catch (Neo4jException ex)
        {
            _logger.LogError(ex, "Raw Cypher query failed: {Query}", TruncateQuery(cypher));
            throw new GraphQueryException($"Graph query failed: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Execution flow:
    /// 1. Start stopwatch for timing.
    /// 2. Execute Cypher write via _session.RunAsync().
    /// 3. Consume the result to get IResultSummary with write statistics.
    /// 4. Map summary counters to GraphWriteResult.
    /// 5. Log timing at Debug level.
    /// 6. On failure, wrap Neo4jException in GraphQueryException.
    /// </remarks>
    public async Task<GraphWriteResult> ExecuteAsync(
        string cypher,
        object? parameters = null,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("Executing Cypher write: {Query}", TruncateQuery(cypher));

            var cursor = await _session.RunAsync(cypher, parameters?.ToDictionary());
            var summary = await cursor.ConsumeAsync();

            stopwatch.Stop();

            _logger.LogDebug(
                "Cypher write completed in {Duration}ms",
                stopwatch.ElapsedMilliseconds);

            // LOGIC: Slow query detection for write operations.
            if (stopwatch.ElapsedMilliseconds > SlowQueryThresholdMs)
            {
                _logger.LogWarning(
                    "Slow Cypher write ({Duration}ms): {Query}",
                    stopwatch.ElapsedMilliseconds, TruncateQuery(cypher));
            }

            return new GraphWriteResult
            {
                NodesCreated = summary.Counters.NodesCreated,
                NodesDeleted = summary.Counters.NodesDeleted,
                RelationshipsCreated = summary.Counters.RelationshipsCreated,
                RelationshipsDeleted = summary.Counters.RelationshipsDeleted,
                PropertiesSet = summary.Counters.PropertiesSet
            };
        }
        catch (Neo4jException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Cypher write failed after {Duration}ms: {Query}",
                stopwatch.ElapsedMilliseconds, TruncateQuery(cypher));
            throw new GraphQueryException($"Graph write failed: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Creates a new transaction within this session. The transaction
    /// must be committed or rolled back before the session is disposed.
    /// </remarks>
    public async Task<IGraphTransaction> BeginTransactionAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Beginning graph transaction");

            var tx = await _session.BeginTransactionAsync();
            return new Neo4jGraphTransaction(tx, _logger);
        }
        catch (Neo4jException ex)
        {
            _logger.LogError(ex, "Failed to begin graph transaction");
            throw new GraphQueryException($"Failed to begin transaction: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Disposes the session, returning the connection to the pool.
    /// </summary>
    /// <remarks>
    /// LOGIC: Closes the underlying Neo4j session. The connection is returned
    /// to the driver's connection pool for reuse.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        try
        {
            await _session.CloseAsync();
        }
        catch (Exception ex)
        {
            // LOGIC: Swallow exceptions during dispose — the session may already
            // be in a closed or error state. Log for diagnostics only.
            _logger.LogWarning(ex, "Error closing graph session (may already be closed)");
        }
    }

    /// <summary>
    /// Truncates a Cypher query string for logging purposes.
    /// </summary>
    /// <param name="cypher">The full Cypher query string.</param>
    /// <returns>The query truncated to 200 characters maximum.</returns>
    /// <remarks>
    /// LOGIC: Prevents excessively long queries from bloating log output.
    /// 200 characters is enough to identify the query pattern while keeping
    /// log entries manageable.
    /// </remarks>
    private static string TruncateQuery(string cypher)
    {
        return cypher.Length <= 200 ? cypher : cypher[..200] + "...";
    }
}
