namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Generic repository interface for CRUD operations.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
/// <typeparam name="TId">The primary key type.</typeparam>
/// <remarks>
/// LOGIC: This interface provides a consistent pattern for data access across all entities.
///
/// Design decisions:
/// - All methods are async for I/O-bound operations
/// - CancellationToken support for request cancellation
/// - Nullable return types where entity may not exist
/// - Transaction support via IUnitOfWork parameter
///
/// For custom queries beyond CRUD, use QueryAsync&lt;TResult&gt; or create
/// entity-specific repository interfaces that extend this one.
/// </remarks>
public interface IGenericRepository<T, TId> where T : class
{
    #region Read Operations

    /// <summary>
    /// Gets an entity by its primary key.
    /// </summary>
    /// <param name="id">The primary key value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The entity, or null if not found.</returns>
    Task<T?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all entities of this type.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All entities.</returns>
    /// <remarks>
    /// WARNING: Use with caution on large tables. Consider using
    /// GetPagedAsync or QueryAsync with filtering for production code.
    /// </remarks>
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paged result of entities.
    /// </summary>
    /// <param name="pageNumber">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paged result with total count.</returns>
    Task<PagedResult<T>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an entity with the given ID exists.
    /// </summary>
    /// <param name="id">The primary key value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the entity exists.</returns>
    Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total count of entities.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Total number of entities.</returns>
    Task<long> CountAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Write Operations

    /// <summary>
    /// Inserts a new entity.
    /// </summary>
    /// <param name="entity">The entity to insert.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The entity with any generated values populated (e.g., ID, timestamps).</returns>
    Task<T> InsertAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts multiple entities in a batch.
    /// </summary>
    /// <param name="entities">The entities to insert.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of entities inserted.</returns>
    Task<int> InsertManyAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    /// <param name="entity">The entity with updated values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the entity was updated, false if not found.</returns>
    Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an entity by its primary key.
    /// </summary>
    /// <param name="id">The primary key value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the entity was deleted, false if not found.</returns>
    Task<bool> DeleteAsync(TId id, CancellationToken cancellationToken = default);

    #endregion

    #region Query Operations

    /// <summary>
    /// Executes a custom query and maps results to the specified type.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="sql">The SQL query.</param>
    /// <param name="parameters">Query parameters (use anonymous object).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The query results.</returns>
    /// <example>
    /// <code>
    /// var users = await repo.QueryAsync&lt;User&gt;(
    ///     "SELECT * FROM \"Users\" WHERE \"IsActive\" = @IsActive",
    ///     new { IsActive = true });
    /// </code>
    /// </example>
    Task<IEnumerable<TResult>> QueryAsync<TResult>(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a custom query and returns a single result.
    /// </summary>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <param name="sql">The SQL query.</param>
    /// <param name="parameters">Query parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The single result, or null/default if not found.</returns>
    Task<TResult?> QuerySingleOrDefaultAsync<TResult>(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a non-query command (INSERT, UPDATE, DELETE).
    /// </summary>
    /// <param name="sql">The SQL command.</param>
    /// <param name="parameters">Command parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of affected rows.</returns>
    Task<int> ExecuteAsync(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a scalar query (e.g., COUNT, SUM).
    /// </summary>
    /// <typeparam name="TResult">The scalar result type.</typeparam>
    /// <param name="sql">The SQL query.</param>
    /// <param name="parameters">Query parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The scalar result.</returns>
    Task<TResult> ExecuteScalarAsync<TResult>(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Paged result containing items and pagination metadata.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
public record PagedResult<T>(
    IEnumerable<T> Items,
    int PageNumber,
    int PageSize,
    long TotalCount)
{
    /// <summary>
    /// Total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>
    /// Whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;

    /// <summary>
    /// Whether there is a next page.
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;
}
