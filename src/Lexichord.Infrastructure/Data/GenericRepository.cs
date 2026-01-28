using System.Reflection;
using Dapper;
using Dapper.Contrib.Extensions;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Infrastructure.Data;

/// <summary>
/// Generic repository implementation using Dapper.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
/// <typeparam name="TId">The primary key type.</typeparam>
/// <remarks>
/// LOGIC: Provides CRUD operations for any entity type using Dapper and Dapper.Contrib.
///
/// Table and column names are determined by:
/// 1. [Table("TableName")] attribute on entity class
/// 2. [Key] or [ExplicitKey] attribute on ID property
/// 3. Fallback to convention: typeof(T).Name + "s" for table, "Id" for column
/// </remarks>
public class GenericRepository<T, TId> : IGenericRepository<T, TId> where T : class
{
    /// <summary>
    /// Database connection factory.
    /// </summary>
    protected readonly IDbConnectionFactory _connectionFactory;

    /// <summary>
    /// Logger instance.
    /// </summary>
    protected readonly ILogger<GenericRepository<T, TId>> _logger;

    /// <summary>
    /// Cached table name.
    /// </summary>
    private readonly string _tableName;

    /// <summary>
    /// Cached ID column name.
    /// </summary>
    private readonly string _idColumn;

    /// <summary>
    /// Creates a new GenericRepository instance.
    /// </summary>
    /// <param name="connectionFactory">The connection factory.</param>
    /// <param name="logger">The logger.</param>
    public GenericRepository(
        IDbConnectionFactory connectionFactory,
        ILogger<GenericRepository<T, TId>> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _tableName = GetTableName();
        _idColumn = GetIdColumnName();
    }

    #region Read Operations

    /// <inheritdoc />
    public virtual async Task<T?> GetByIdAsync(TId id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = $@"SELECT * FROM ""{_tableName}"" WHERE ""{_idColumn}"" = @Id";
        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        var result = await connection.QuerySingleOrDefaultAsync<T>(command);
        _logger.LogDebug("GetById {Table} Id={Id}: {Result}", _tableName, id, result is not null ? "Found" : "NotFound");
        return result;
    }

    /// <inheritdoc />
    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = $@"SELECT * FROM ""{_tableName}""";
        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        var results = await connection.QueryAsync<T>(command);
        var count = results.TryGetNonEnumeratedCount(out var c) ? c : results.Count();
        _logger.LogDebug("GetAll {Table}: {Count} records", _tableName, count);
        return results;
    }

    /// <inheritdoc />
    public virtual async Task<PagedResult<T>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        // Get total count
        var countSql = $@"SELECT COUNT(*) FROM ""{_tableName}""";
        var countCommand = new CommandDefinition(countSql, cancellationToken: cancellationToken);
        var totalCount = await connection.ExecuteScalarAsync<long>(countCommand);

        // Get page items
        var offset = (pageNumber - 1) * pageSize;
        var sql = $@"SELECT * FROM ""{_tableName}"" ORDER BY ""{_idColumn}"" LIMIT @PageSize OFFSET @Offset";
        var command = new CommandDefinition(sql, new { PageSize = pageSize, Offset = offset }, cancellationToken: cancellationToken);
        var items = await connection.QueryAsync<T>(command);

        _logger.LogDebug("GetPaged {Table} Page={Page} Size={Size}: {Count} of {Total}",
            _tableName, pageNumber, pageSize, items.Count(), totalCount);

        return new PagedResult<T>(items, pageNumber, pageSize, totalCount);
    }

    /// <inheritdoc />
    public virtual async Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = $@"SELECT EXISTS(SELECT 1 FROM ""{_tableName}"" WHERE ""{_idColumn}"" = @Id)";
        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        var exists = await connection.ExecuteScalarAsync<bool>(command);
        _logger.LogDebug("Exists {Table} Id={Id}: {Exists}", _tableName, id, exists);
        return exists;
    }

    /// <inheritdoc />
    public virtual async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = $@"SELECT COUNT(*) FROM ""{_tableName}""";
        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        var count = await connection.ExecuteScalarAsync<long>(command);
        _logger.LogDebug("Count {Table}: {Count}", _tableName, count);
        return count;
    }

    #endregion

    #region Write Operations

    /// <inheritdoc />
    public virtual async Task<T> InsertAsync(T entity, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.InsertAsync(entity);
        var id = GetIdValue(entity);
        _logger.LogDebug("Insert {Table}: Success (Id={Id})", _tableName, id);
        // Re-fetch to get computed fields (CreatedAt, UpdatedAt)
        return (await GetByIdAsync(id, cancellationToken))!;
    }

    /// <inheritdoc />
    public virtual async Task<int> InsertManyAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var entityList = entities.ToList();
        var count = await connection.InsertAsync(entityList);
        _logger.LogDebug("InsertMany {Table}: {Count} entities", _tableName, entityList.Count);
        return entityList.Count;
    }

    /// <inheritdoc />
    public virtual async Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var result = await connection.UpdateAsync(entity);
        var id = GetIdValue(entity);
        _logger.LogDebug("Update {Table} Id={Id}: {Result}", _tableName, id, result ? "Success" : "NotFound");
        return result;
    }

    /// <inheritdoc />
    public virtual async Task<bool> DeleteAsync(TId id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = $@"DELETE FROM ""{_tableName}"" WHERE ""{_idColumn}"" = @Id";
        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        var affected = await connection.ExecuteAsync(command);
        var result = affected > 0;
        _logger.LogDebug("Delete {Table} Id={Id}: {Result}", _tableName, id, result ? "Success" : "NotFound");
        return result;
    }

    #endregion

    #region Query Operations

    /// <inheritdoc />
    public virtual async Task<IEnumerable<TResult>> QueryAsync<TResult>(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        var results = await connection.QueryAsync<TResult>(command);
        _logger.LogDebug("Query executed: {Count} results", results.Count());
        return results;
    }

    /// <inheritdoc />
    public virtual async Task<TResult?> QuerySingleOrDefaultAsync<TResult>(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        var result = await connection.QuerySingleOrDefaultAsync<TResult>(command);
        _logger.LogDebug("QuerySingle executed: {Found}", result is not null ? "Found" : "NotFound");
        return result;
    }

    /// <inheritdoc />
    public virtual async Task<int> ExecuteAsync(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        var affected = await connection.ExecuteAsync(command);
        _logger.LogDebug("Execute completed: {Affected} rows affected", affected);
        return affected;
    }

    /// <inheritdoc />
    public virtual async Task<TResult> ExecuteScalarAsync<TResult>(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        var result = await connection.ExecuteScalarAsync<TResult>(command);
        _logger.LogDebug("ExecuteScalar completed");
        return result!;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets the table name from the [Table] attribute or convention.
    /// </summary>
    private static string GetTableName()
    {
        var tableAttr = typeof(T).GetCustomAttribute<TableAttribute>();
        return tableAttr?.Name ?? typeof(T).Name + "s";
    }

    /// <summary>
    /// Gets the ID column name from the [Key] or [ExplicitKey] attribute.
    /// </summary>
    private static string GetIdColumnName()
    {
        var props = typeof(T).GetProperties();
        var keyProp = props.FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() is not null);
        keyProp ??= props.FirstOrDefault(p => p.GetCustomAttribute<ExplicitKeyAttribute>() is not null);
        return keyProp?.Name ?? "Id";
    }

    /// <summary>
    /// Gets the ID value from an entity.
    /// </summary>
    /// <param name="entity">The entity.</param>
    /// <returns>The ID value.</returns>
    protected TId GetIdValue(T entity)
    {
        var prop = typeof(T).GetProperty(_idColumn);
        return (TId)prop!.GetValue(entity)!;
    }

    #endregion
}
