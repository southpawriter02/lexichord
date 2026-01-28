using System.Data;

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Unit of work interface for transaction management across repositories.
/// </summary>
/// <remarks>
/// LOGIC: The Unit of Work pattern coordinates writes across multiple repositories
/// within a single database transaction. This ensures atomic operations where
/// either all changes succeed or all are rolled back.
///
/// Usage pattern:
/// <code>
/// await unitOfWork.BeginTransactionAsync();
/// try
/// {
///     await userRepo.InsertAsync(user);
///     await settingsRepo.SetValueAsync("last_user", user.Email);
///     await unitOfWork.CommitAsync();
/// }
/// catch
/// {
///     await unitOfWork.RollbackAsync();
///     throw;
/// }
/// </code>
/// </remarks>
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Gets the current database connection.
    /// </summary>
    /// <remarks>
    /// The connection is created lazily on first access and reused
    /// for the lifetime of the unit of work instance.
    /// </remarks>
    IDbConnection Connection { get; }

    /// <summary>
    /// Gets the current transaction, if one has been started.
    /// </summary>
    IDbTransaction? Transaction { get; }

    /// <summary>
    /// Gets a value indicating whether a transaction is currently active.
    /// </summary>
    bool HasActiveTransaction { get; }

    /// <summary>
    /// Begins a new database transaction.
    /// </summary>
    /// <param name="isolationLevel">The transaction isolation level. Defaults to ReadCommitted.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if a transaction is already active.
    /// </exception>
    Task BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if no transaction is active.
    /// </exception>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if no transaction is active.
    /// </exception>
    Task RollbackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all pending changes (alias for CommitAsync for EF-like semantics).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
