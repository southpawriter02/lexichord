// =============================================================================
// File: Neo4jGraphTransaction.cs
// Project: Lexichord.Modules.Knowledge
// Description: IGraphTransaction implementation wrapping Neo4j IAsyncTransaction.
// =============================================================================
// LOGIC: Wraps the Neo4j driver's IAsyncTransaction to provide explicit
//   transaction support through the Lexichord IGraphTransaction abstraction.
//   Ensures proper commit/rollback semantics and exception wrapping.
//
// v0.4.5e: Graph Database Integration (CKVS Phase 1)
// Dependencies: Neo4j.Driver
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;

namespace Lexichord.Modules.Knowledge.Graph;

/// <summary>
/// Wraps a Neo4j <see cref="IAsyncTransaction"/> as an <see cref="IGraphTransaction"/>.
/// </summary>
/// <remarks>
/// <para>
/// Provides explicit transaction support for multi-statement graph operations.
/// If disposed without calling <see cref="CommitAsync"/>, the transaction is
/// automatically rolled back.
/// </para>
/// <para>
/// <b>Error Handling:</b> All Neo4j-specific exceptions are wrapped in
/// <see cref="GraphQueryException"/> to prevent driver type leakage.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5e as part of the Knowledge Graph Foundation.
/// </para>
/// </remarks>
internal sealed class Neo4jGraphTransaction : IGraphTransaction
{
    private readonly IAsyncTransaction _transaction;
    private readonly Microsoft.Extensions.Logging.ILogger _logger;
    private bool _committed;
    private bool _rolledBack;

    /// <summary>
    /// Initializes a new instance wrapping the specified Neo4j transaction.
    /// </summary>
    /// <param name="transaction">The Neo4j driver transaction to wrap.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="transaction"/> or <paramref name="logger"/> is null.
    /// </exception>
    public Neo4jGraphTransaction(IAsyncTransaction transaction, Microsoft.Extensions.Logging.ILogger logger)
    {
        _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Executes a read query within the transaction boundary. Results are
    /// mapped to the requested type using the same mapping logic as
    /// <see cref="Neo4jGraphSession.QueryAsync{T}"/>.
    /// </remarks>
    public async Task<IReadOnlyList<T>> QueryAsync<T>(
        string cypher,
        object? parameters = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Transaction query: {Query}", cypher[..Math.Min(200, cypher.Length)]);

            var cursor = await _transaction.RunAsync(cypher, parameters?.ToDictionary());
            var records = await cursor.ToListAsync();

            return records.Select(r => Neo4jRecordMapper.MapRecord<T>(r)).ToList();
        }
        catch (Neo4jException ex)
        {
            _logger.LogError(ex, "Transaction query failed: {Query}", cypher[..Math.Min(200, cypher.Length)]);
            throw new GraphQueryException($"Transaction query failed: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Executes a write operation within the transaction. The result
    /// is consumed to extract write statistics from the query summary.
    /// </remarks>
    public async Task<GraphWriteResult> ExecuteAsync(
        string cypher,
        object? parameters = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Transaction execute: {Query}", cypher[..Math.Min(200, cypher.Length)]);

            var cursor = await _transaction.RunAsync(cypher, parameters?.ToDictionary());
            var summary = await cursor.ConsumeAsync();

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
            _logger.LogError(ex, "Transaction write failed: {Query}", cypher[..Math.Min(200, cypher.Length)]);
            throw new GraphQueryException($"Transaction write failed: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Commits all pending operations in the transaction. After commit,
    /// the transaction is in a terminal state and cannot be reused.
    /// </remarks>
    public async Task CommitAsync(CancellationToken ct = default)
    {
        try
        {
            await _transaction.CommitAsync();
            _committed = true;
            _logger.LogDebug("Transaction committed successfully");
        }
        catch (Neo4jException ex)
        {
            _logger.LogError(ex, "Transaction commit failed");
            throw new GraphQueryException($"Transaction commit failed: {ex.Message}", ex);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Explicitly rolls back all pending operations. After rollback,
    /// the transaction is in a terminal state and cannot be reused.
    /// </remarks>
    public async Task RollbackAsync(CancellationToken ct = default)
    {
        try
        {
            await _transaction.RollbackAsync();
            _rolledBack = true;
            _logger.LogDebug("Transaction rolled back");
        }
        catch (Neo4jException ex)
        {
            _logger.LogWarning(ex, "Transaction rollback failed");
            throw new GraphQueryException($"Transaction rollback failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Disposes the transaction, rolling back if not committed.
    /// </summary>
    /// <remarks>
    /// LOGIC: Safety net — if the transaction was not explicitly committed or
    /// rolled back, disposal triggers an implicit rollback. This prevents
    /// forgotten transactions from holding locks indefinitely.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        if (!_committed && !_rolledBack)
        {
            try
            {
                await _transaction.RollbackAsync();
                _logger.LogDebug("Transaction auto-rolled back on dispose");
            }
            catch (Exception ex)
            {
                // LOGIC: Swallow exceptions during dispose — the transaction
                // may already be in a terminated state from a prior error.
                _logger.LogWarning(ex, "Auto-rollback on dispose failed (transaction may already be closed)");
            }
        }
    }
}
