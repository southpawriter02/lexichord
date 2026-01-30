using Dapper;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DapperCommandDefinition = Dapper.CommandDefinition;

namespace Lexichord.Modules.Style.Data;

/// <summary>
/// Repository implementation for terminology (style term) operations with caching.
/// </summary>
/// <remarks>
/// LOGIC: Implements ITerminologyRepository with direct Dapper queries and IMemoryCache.
///
/// Design decisions:
/// - Self-contained implementation (no GenericRepository inheritance)
/// - Modules cannot reference Infrastructure, so all SQL is inline
/// - Follows the StatusBar HealthRepository pattern for module repositories
///
/// Caching strategy:
/// - Active terms are cached as a HashSet for O(1) pattern lookups
/// - Cache uses sliding expiration with absolute max duration
/// - All write operations automatically invalidate the cache
///
/// Thread safety:
/// - IMemoryCache is thread-safe
/// - HashSet is created fresh on cache miss, never mutated after caching
/// </remarks>
public class TerminologyRepository : ITerminologyRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IMemoryCache _cache;
    private readonly TerminologyCacheOptions _cacheOptions;
    private readonly ILogger<TerminologyRepository> _logger;

    private const string TableName = "style_terms";

    /// <summary>
    /// Creates a new TerminologyRepository instance.
    /// </summary>
    /// <param name="connectionFactory">Database connection factory.</param>
    /// <param name="cache">Memory cache for active terms.</param>
    /// <param name="cacheOptions">Cache configuration options.</param>
    /// <param name="logger">Logger instance.</param>
    public TerminologyRepository(
        IDbConnectionFactory connectionFactory,
        IMemoryCache cache,
        IOptions<TerminologyCacheOptions> cacheOptions,
        ILogger<TerminologyRepository> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _cacheOptions = cacheOptions?.Value ?? throw new ArgumentNullException(nameof(cacheOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Read Operations (IGenericRepository)

    /// <inheritdoc />
    public async Task<StyleTerm?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = $@"SELECT * FROM ""{TableName}"" WHERE ""Id"" = @Id";
        var command = new DapperCommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        var result = await connection.QuerySingleOrDefaultAsync<StyleTerm>(command);
        _logger.LogDebug("GetById {Table} Id={Id}: {Result}", TableName, id, result is not null ? "Found" : "NotFound");
        return result;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StyleTerm>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = $@"SELECT * FROM ""{TableName}""";
        var command = new DapperCommandDefinition(sql, cancellationToken: cancellationToken);
        var results = await connection.QueryAsync<StyleTerm>(command);
        _logger.LogDebug("GetAll {Table}: {Count} records", TableName, results.Count());
        return results;
    }

    /// <inheritdoc />
    public async Task<PagedResult<StyleTerm>> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        // Get total count
        const string countSql = $@"SELECT COUNT(*) FROM ""{TableName}""";
        var countCommand = new DapperCommandDefinition(countSql, cancellationToken: cancellationToken);
        var totalCount = await connection.ExecuteScalarAsync<long>(countCommand);

        // Get page items
        var offset = (pageNumber - 1) * pageSize;
        const string sql = $@"SELECT * FROM ""{TableName}"" ORDER BY ""Id"" LIMIT @PageSize OFFSET @Offset";
        var command = new DapperCommandDefinition(sql, new { PageSize = pageSize, Offset = offset }, cancellationToken: cancellationToken);
        var items = await connection.QueryAsync<StyleTerm>(command);

        _logger.LogDebug("GetPaged {Table} Page={Page} Size={Size}: {Count} of {Total}",
            TableName, pageNumber, pageSize, items.Count(), totalCount);

        return new PagedResult<StyleTerm>(items, pageNumber, pageSize, totalCount);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = $@"SELECT EXISTS(SELECT 1 FROM ""{TableName}"" WHERE ""Id"" = @Id)";
        var command = new DapperCommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        var exists = await connection.ExecuteScalarAsync<bool>(command);
        _logger.LogDebug("Exists {Table} Id={Id}: {Exists}", TableName, id, exists);
        return exists;
    }

    /// <inheritdoc />
    public async Task<long> CountAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = $@"SELECT COUNT(*) FROM ""{TableName}""";
        var command = new DapperCommandDefinition(sql, cancellationToken: cancellationToken);
        var count = await connection.ExecuteScalarAsync<long>(command);
        _logger.LogDebug("Count {Table}: {Count}", TableName, count);
        return count;
    }

    #endregion

    #region Write Operations (IGenericRepository)

    /// <inheritdoc />
    public async Task<StyleTerm> InsertAsync(StyleTerm entity, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        const string sql = @"
            INSERT INTO ""style_terms"" 
            (""Id"", ""StyleSheetId"", ""Term"", ""Replacement"", ""Category"", ""Severity"", ""IsActive"", ""Notes"", ""FuzzyEnabled"", ""FuzzyThreshold"", ""CreatedAt"", ""UpdatedAt"")
            VALUES 
            (@Id, @StyleSheetId, @Term, @Replacement, @Category, @Severity, @IsActive, @Notes, @FuzzyEnabled, @FuzzyThreshold, @CreatedAt, @UpdatedAt)";

        var command = new DapperCommandDefinition(sql, entity, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);

        _logger.LogDebug("Insert {Table}: Success (Id={Id})", TableName, entity.Id);
        await InvalidateCacheAsync(cancellationToken);

        // Return with updated timestamps from database
        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    /// <inheritdoc />
    public async Task<int> InsertManyAsync(IEnumerable<StyleTerm> entities, CancellationToken cancellationToken = default)
    {
        var entityList = entities.ToList();
        if (entityList.Count == 0) return 0;

        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        const string sql = @"
            INSERT INTO ""style_terms"" 
            (""Id"", ""StyleSheetId"", ""Term"", ""Replacement"", ""Category"", ""Severity"", ""IsActive"", ""Notes"", ""FuzzyEnabled"", ""FuzzyThreshold"", ""CreatedAt"", ""UpdatedAt"")
            VALUES 
            (@Id, @StyleSheetId, @Term, @Replacement, @Category, @Severity, @IsActive, @Notes, @FuzzyEnabled, @FuzzyThreshold, @CreatedAt, @UpdatedAt)";

        var count = await connection.ExecuteAsync(sql, entityList);

        _logger.LogDebug("InsertMany {Table}: {Count} entities", TableName, count);
        await InvalidateCacheAsync(cancellationToken);

        return count;
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(StyleTerm entity, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        const string sql = @"
            UPDATE ""style_terms"" SET
                ""StyleSheetId"" = @StyleSheetId,
                ""Term"" = @Term,
                ""Replacement"" = @Replacement,
                ""Category"" = @Category,
                ""Severity"" = @Severity,
                ""IsActive"" = @IsActive,
                ""Notes"" = @Notes,
                ""FuzzyEnabled"" = @FuzzyEnabled,
                ""FuzzyThreshold"" = @FuzzyThreshold,
                ""UpdatedAt"" = @UpdatedAt
            WHERE ""Id"" = @Id";

        var command = new DapperCommandDefinition(sql, entity, cancellationToken: cancellationToken);
        var affected = await connection.ExecuteAsync(command);
        var result = affected > 0;

        _logger.LogDebug("Update {Table} Id={Id}: {Result}", TableName, entity.Id, result ? "Success" : "NotFound");
        if (result) await InvalidateCacheAsync(cancellationToken);

        return result;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = $@"DELETE FROM ""{TableName}"" WHERE ""Id"" = @Id";
        var command = new DapperCommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        var affected = await connection.ExecuteAsync(command);
        var result = affected > 0;

        _logger.LogDebug("Delete {Table} Id={Id}: {Result}", TableName, id, result ? "Success" : "NotFound");
        if (result) await InvalidateCacheAsync(cancellationToken);

        return result;
    }

    #endregion

    #region Query Operations (IGenericRepository)

    /// <inheritdoc />
    public async Task<IEnumerable<TResult>> QueryAsync<TResult>(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var command = new DapperCommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        var results = await connection.QueryAsync<TResult>(command);
        _logger.LogDebug("Query executed: {Count} results", results.Count());
        return results;
    }

    /// <inheritdoc />
    public async Task<TResult?> QuerySingleOrDefaultAsync<TResult>(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var command = new DapperCommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        var result = await connection.QuerySingleOrDefaultAsync<TResult>(command);
        _logger.LogDebug("QuerySingle executed: {Found}", result is not null ? "Found" : "NotFound");
        return result;
    }

    /// <inheritdoc />
    public async Task<int> ExecuteAsync(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var command = new DapperCommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        var affected = await connection.ExecuteAsync(command);
        _logger.LogDebug("Execute completed: {Affected} rows affected", affected);
        return affected;
    }

    /// <inheritdoc />
    public async Task<TResult> ExecuteScalarAsync<TResult>(
        string sql,
        object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var command = new DapperCommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        var result = await connection.ExecuteScalarAsync<TResult>(command);
        _logger.LogDebug("ExecuteScalar completed");
        return result!;
    }

    #endregion

    #region Cached Queries (ITerminologyRepository)

    /// <inheritdoc />
    public async Task<HashSet<StyleTerm>> GetAllActiveTermsAsync(CancellationToken cancellationToken = default)
    {
        // LOGIC: Try cache first for sub-millisecond response
        if (_cache.TryGetValue(_cacheOptions.ActiveTermsCacheKey, out HashSet<StyleTerm>? cachedTerms))
        {
            _logger.LogDebug("Cache hit for active terms: {Count} terms", cachedTerms!.Count);
            return cachedTerms;
        }

        // LOGIC: Cache miss - fetch from database
        _logger.LogDebug("Cache miss for active terms, fetching from database");

        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT * FROM ""style_terms"" 
            WHERE ""IsActive"" = TRUE 
            ORDER BY ""Term""";

        var command = new DapperCommandDefinition(sql, cancellationToken: cancellationToken);
        var terms = await connection.QueryAsync<StyleTerm>(command);
        var termSet = new HashSet<StyleTerm>(terms);

        // LOGIC: Cache with sliding + absolute expiration
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(_cacheOptions.CacheDurationMinutes))
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(_cacheOptions.MaxCacheDurationMinutes));

        _cache.Set(_cacheOptions.ActiveTermsCacheKey, termSet, cacheOptions);
        _logger.LogDebug("Cached {Count} active terms", termSet.Count);

        return termSet;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StyleTerm>> GetFuzzyEnabledTermsAsync(CancellationToken cancellationToken = default)
    {
        // LOGIC: Try cache first for sub-millisecond response
        if (_cache.TryGetValue(_cacheOptions.FuzzyTermsCacheKey, out IReadOnlyList<StyleTerm>? cachedTerms))
        {
            _logger.LogDebug("Cache hit for fuzzy terms: {Count} terms", cachedTerms!.Count);
            return cachedTerms;
        }

        // LOGIC: Cache miss - fetch from database
        _logger.LogDebug("Cache miss for fuzzy terms, fetching from database");

        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT * FROM ""style_terms"" 
            WHERE ""FuzzyEnabled"" = TRUE AND ""IsActive"" = TRUE
            ORDER BY ""Term""";

        var command = new DapperCommandDefinition(sql, cancellationToken: cancellationToken);
        var terms = await connection.QueryAsync<StyleTerm>(command);
        var termList = terms.ToList().AsReadOnly();

        // LOGIC: Cache with 5-minute sliding expiration (fuzzy terms are less frequently accessed)
        var cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(5))
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(_cacheOptions.MaxCacheDurationMinutes));

        _cache.Set(_cacheOptions.FuzzyTermsCacheKey, termList, cacheOptions);
        _logger.LogDebug("Cached {Count} fuzzy-enabled terms", termList.Count);

        return termList;
    }

    #endregion

    #region Filtered Queries (ITerminologyRepository)

    /// <inheritdoc />
    public async Task<IEnumerable<StyleTerm>> GetByCategoryAsync(
        string category,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category cannot be empty", nameof(category));

        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT * FROM ""style_terms"" 
            WHERE ""Category"" = @Category 
            ORDER BY ""Term""";

        var command = new DapperCommandDefinition(sql, new { Category = category }, cancellationToken: cancellationToken);
        var terms = await connection.QueryAsync<StyleTerm>(command);

        _logger.LogDebug("GetByCategory '{Category}': {Count} terms", category, terms.Count());
        return terms;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StyleTerm>> GetBySeverityAsync(
        string severity,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(severity))
            throw new ArgumentException("Severity cannot be empty", nameof(severity));

        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        const string sql = @"
            SELECT * FROM ""style_terms"" 
            WHERE ""Severity"" = @Severity 
            ORDER BY ""Term""";

        var command = new DapperCommandDefinition(sql, new { Severity = severity }, cancellationToken: cancellationToken);
        var terms = await connection.QueryAsync<StyleTerm>(command);

        _logger.LogDebug("GetBySeverity '{Severity}': {Count} terms", severity, terms.Count());
        return terms;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StyleTerm>> SearchAsync(
        string searchTerm,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return Enumerable.Empty<StyleTerm>();

        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        // LOGIC: Use pg_trgm for fuzzy matching, ordered by similarity
        const string sql = @"
            SELECT *, similarity(""Term"", @SearchTerm) as sim
            FROM ""style_terms"" 
            WHERE ""Term"" % @SearchTerm OR ""Term"" ILIKE @WildcardTerm
            ORDER BY sim DESC, ""Term""
            LIMIT 50";

        var parameters = new
        {
            SearchTerm = searchTerm,
            WildcardTerm = $"%{searchTerm}%"
        };
        var command = new DapperCommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        var terms = await connection.QueryAsync<StyleTerm>(command);

        _logger.LogDebug("Search '{SearchTerm}': {Count} terms", searchTerm, terms.Count());
        return terms;
    }

    #endregion

    #region Cache Management (ITerminologyRepository)

    /// <inheritdoc />
    public Task InvalidateCacheAsync(CancellationToken cancellationToken = default)
    {
        _cache.Remove(_cacheOptions.ActiveTermsCacheKey);
        _cache.Remove(_cacheOptions.FuzzyTermsCacheKey);
        _logger.LogDebug("Cache invalidated for active terms and fuzzy terms");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void InvalidateFuzzyTermsCache()
    {
        _cache.Remove(_cacheOptions.FuzzyTermsCacheKey);
        _logger.LogDebug("Cache invalidated for fuzzy terms");
    }

    #endregion
}
