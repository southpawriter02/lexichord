using System.Data;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Lexichord.Infrastructure.Data;

/// <summary>
/// Unit of Work implementation for transaction management.
/// </summary>
/// <remarks>
/// LOGIC: Provides transaction coordination across multiple repositories.
///
/// Connection lifecycle:
/// - Connection is created lazily on first access
/// - Connection is kept open for the lifetime of the UnitOfWork
/// - Connection is disposed when UnitOfWork is disposed
///
/// Transaction lifecycle:
/// - Transaction is started explicitly via BeginTransactionAsync
/// - Must be explicitly committed or rolled back
/// - Rollback is automatic on dispose if transaction is still active
/// </remarks>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<UnitOfWork> _logger;

    private NpgsqlConnection? _connection;
    private NpgsqlTransaction? _transaction;
    private bool _disposed;

    /// <summary>
    /// Creates a new UnitOfWork instance.
    /// </summary>
    public UnitOfWork(
        IDbConnectionFactory connectionFactory,
        ILogger<UnitOfWork> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IDbConnection Connection => _connection ?? throw new InvalidOperationException(
        "Connection not initialized. Call BeginTransactionAsync first.");

    /// <inheritdoc />
    public IDbTransaction? Transaction => _transaction;

    /// <inheritdoc />
    public bool HasActiveTransaction => _transaction is not null;

    /// <inheritdoc />
    public async Task BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_transaction is not null)
        {
            throw new InvalidOperationException("A transaction is already active.");
        }

        _connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        _transaction = await _connection.BeginTransactionAsync(isolationLevel, cancellationToken);

        _logger.LogDebug("Transaction started (IsolationLevel: {IsolationLevel})", isolationLevel);
    }

    /// <inheritdoc />
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_transaction is null)
        {
            throw new InvalidOperationException("No active transaction to commit.");
        }

        await _transaction.CommitAsync(cancellationToken);
        _logger.LogDebug("Transaction committed");

        await CleanupTransactionAsync();
    }

    /// <inheritdoc />
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_transaction is null)
        {
            throw new InvalidOperationException("No active transaction to rollback.");
        }

        await _transaction.RollbackAsync(cancellationToken);
        _logger.LogDebug("Transaction rolled back");

        await CleanupTransactionAsync();
    }

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // LOGIC: For Dapper-based implementations, this is a no-op alias for commit.
        // Individual repository operations execute immediately, so there are no
        // "pending changes" to track. This method exists for EF-like semantics.
        if (_transaction is not null)
        {
            await CommitAsync(cancellationToken);
        }
        return 0;
    }

    /// <summary>
    /// Cleans up transaction resources.
    /// </summary>
    private async Task CleanupTransactionAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;

        // Rollback if transaction is still active
        if (_transaction is not null)
        {
            _logger.LogWarning("Disposing UnitOfWork with active transaction - rolling back");
            _transaction.Rollback();
            _transaction.Dispose();
            _transaction = null;
        }

        _connection?.Dispose();
        _connection = null;

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        // Rollback if transaction is still active
        if (_transaction is not null)
        {
            _logger.LogWarning("Disposing UnitOfWork with active transaction - rolling back");
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
