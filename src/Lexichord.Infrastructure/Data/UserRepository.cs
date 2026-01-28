using Dapper;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Entities;
using Microsoft.Extensions.Logging;

namespace Lexichord.Infrastructure.Data;

/// <summary>
/// Repository implementation for User entities.
/// </summary>
/// <remarks>
/// LOGIC: Extends GenericRepository with User-specific query methods.
/// All email comparisons are case-insensitive for user experience.
/// </remarks>
public sealed class UserRepository : GenericRepository<User, Guid>, IUserRepository
{
    private readonly ILogger<UserRepository> _userLogger;

    /// <summary>
    /// Creates a new UserRepository instance.
    /// </summary>
    public UserRepository(
        IDbConnectionFactory connectionFactory,
        ILogger<UserRepository> logger)
        : base(connectionFactory, logger)
    {
        _userLogger = logger;
    }

    /// <inheritdoc />
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = @"
            SELECT * FROM ""Users""
            WHERE LOWER(""Email"") = LOWER(@Email)";
        var command = new CommandDefinition(sql, new { Email = email }, cancellationToken: cancellationToken);
        var result = await connection.QuerySingleOrDefaultAsync<User>(command);
        _userLogger.LogDebug("GetByEmail {Email}: {Result}", email, result is not null ? "Found" : "NotFound");
        return result;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = @"SELECT * FROM ""Users"" WHERE ""IsActive"" = true ORDER BY ""DisplayName""";
        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        var results = await connection.QueryAsync<User>(command);
        _userLogger.LogDebug("GetActiveUsers: {Count} users", results.Count());
        return results;
    }

    /// <inheritdoc />
    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = @"SELECT EXISTS(SELECT 1 FROM ""Users"" WHERE LOWER(""Email"") = LOWER(@Email))";
        var command = new CommandDefinition(sql, new { Email = email }, cancellationToken: cancellationToken);
        var exists = await connection.ExecuteScalarAsync<bool>(command);
        _userLogger.LogDebug("EmailExists {Email}: {Exists}", email, exists);
        return exists;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<User>> SearchAsync(
        string searchTerm,
        int maxResults = 50,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = @"
            SELECT * FROM ""Users""
            WHERE ""DisplayName"" ILIKE @SearchPattern
               OR ""Email"" ILIKE @SearchPattern
            ORDER BY ""DisplayName""
            LIMIT @MaxResults";
        var command = new CommandDefinition(
            sql,
            new { SearchPattern = $"%{searchTerm}%", MaxResults = maxResults },
            cancellationToken: cancellationToken);
        var results = await connection.QueryAsync<User>(command);
        _userLogger.LogDebug("Search '{Term}': {Count} results", searchTerm, results.Count());
        return results;
    }

    /// <inheritdoc />
    public async Task<bool> DeactivateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = @"UPDATE ""Users"" SET ""IsActive"" = false WHERE ""Id"" = @UserId";
        var command = new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken);
        var affected = await connection.ExecuteAsync(command);
        var result = affected > 0;
        _userLogger.LogInformation("Deactivate User {UserId}: {Result}", userId, result ? "Success" : "NotFound");
        return result;
    }

    /// <inheritdoc />
    public async Task<bool> ReactivateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        var sql = @"UPDATE ""Users"" SET ""IsActive"" = true WHERE ""Id"" = @UserId";
        var command = new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken);
        var affected = await connection.ExecuteAsync(command);
        var result = affected > 0;
        _userLogger.LogInformation("Reactivate User {UserId}: {Result}", userId, result ? "Success" : "NotFound");
        return result;
    }
}
