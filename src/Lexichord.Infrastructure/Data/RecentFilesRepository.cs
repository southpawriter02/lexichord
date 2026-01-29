using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Infrastructure.Data;

/// <summary>
/// Repository implementation for recent files using Dapper.
/// </summary>
/// <remarks>
/// LOGIC: Provides MRU-specific data access patterns:
/// - Upsert with ON CONFLICT for atomic insert-or-update
/// - Ordered retrieval by LastOpenedAt descending
/// - Trim operation for enforcing MaxEntries limit
///
/// All operations use parameterized queries to prevent SQL injection.
/// PostgreSQL-specific syntax is used for optimal performance.
/// </remarks>
public sealed class RecentFilesRepository : IRecentFilesRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<RecentFilesRepository> _logger;

    public RecentFilesRepository(
        IDbConnectionFactory connectionFactory,
        ILogger<RecentFilesRepository> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecentFileEntry>> GetRecentFilesAsync(
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        const string sql = """
            SELECT "FilePath", "FileName", "LastOpenedAt", "OpenCount"
            FROM "RecentFiles"
            ORDER BY "LastOpenedAt" DESC
            LIMIT @MaxCount
            """;

        var command = new CommandDefinition(
            sql,
            new { MaxCount = maxCount },
            cancellationToken: cancellationToken);

        var results = await connection.QueryAsync<RecentFileDto>(command);

        var entries = new List<RecentFileEntry>();
        foreach (var dto in results)
        {
            entries.Add(new RecentFileEntry(
                dto.FilePath,
                dto.FileName,
                dto.LastOpenedAt,
                dto.OpenCount,
                Exists: true)); // Existence checked by service layer
        }

        _logger.LogDebug("GetRecentFiles: Retrieved {Count} entries", entries.Count);
        return entries;
    }

    /// <inheritdoc />
    public async Task<RecentFileEntry?> GetByFilePathAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        const string sql = """
            SELECT "FilePath", "FileName", "LastOpenedAt", "OpenCount"
            FROM "RecentFiles"
            WHERE "FilePath" = @FilePath
            """;

        var command = new CommandDefinition(
            sql,
            new { FilePath = filePath },
            cancellationToken: cancellationToken);

        var dto = await connection.QuerySingleOrDefaultAsync<RecentFileDto>(command);

        if (dto is null)
        {
            _logger.LogDebug("GetByFilePath: '{FilePath}' not found", filePath);
            return null;
        }

        _logger.LogDebug("GetByFilePath: Found '{FilePath}'", filePath);
        return new RecentFileEntry(
            dto.FilePath,
            dto.FileName,
            dto.LastOpenedAt,
            dto.OpenCount,
            Exists: true);
    }

    /// <inheritdoc />
    public async Task UpsertAsync(RecentFileEntry entry, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        // LOGIC: PostgreSQL UPSERT - insert new or update existing
        // On conflict, update LastOpenedAt and increment OpenCount
        const string sql = """
            INSERT INTO "RecentFiles" ("Id", "FilePath", "FileName", "LastOpenedAt", "OpenCount", "CreatedAt", "UpdatedAt")
            VALUES (@Id, @FilePath, @FileName, @LastOpenedAt, @OpenCount, @CreatedAt, @UpdatedAt)
            ON CONFLICT ("FilePath") DO UPDATE SET
                "LastOpenedAt" = @LastOpenedAt,
                "OpenCount" = "RecentFiles"."OpenCount" + 1,
                "UpdatedAt" = @UpdatedAt
            """;

        var now = DateTimeOffset.UtcNow;
        var command = new CommandDefinition(
            sql,
            new
            {
                Id = Guid.NewGuid(),
                entry.FilePath,
                entry.FileName,
                LastOpenedAt = now,
                entry.OpenCount,
                CreatedAt = now,
                UpdatedAt = now
            },
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command);
        _logger.LogDebug("Upserted recent file: {FilePath}", entry.FilePath);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string filePath, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        const string sql = """DELETE FROM "RecentFiles" WHERE "FilePath" = @FilePath""";

        var command = new CommandDefinition(
            sql,
            new { FilePath = filePath },
            cancellationToken: cancellationToken);

        var affected = await connection.ExecuteAsync(command);
        var deleted = affected > 0;

        _logger.LogDebug("Delete '{FilePath}': {Result}", filePath, deleted ? "Success" : "NotFound");
        return deleted;
    }

    /// <inheritdoc />
    public async Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        const string sql = """DELETE FROM "RecentFiles" """;

        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        var affected = await connection.ExecuteAsync(command);

        _logger.LogInformation("Cleared all recent files: {Count} entries deleted", affected);
    }

    /// <inheritdoc />
    public async Task<int> TrimToCountAsync(int keepCount, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        // LOGIC: Delete all entries NOT in the top N by LastOpenedAt
        const string sql = """
            DELETE FROM "RecentFiles"
            WHERE "Id" NOT IN (
                SELECT "Id" FROM "RecentFiles"
                ORDER BY "LastOpenedAt" DESC
                LIMIT @KeepCount
            )
            """;

        var command = new CommandDefinition(
            sql,
            new { KeepCount = keepCount },
            cancellationToken: cancellationToken);

        var deleted = await connection.ExecuteAsync(command);

        if (deleted > 0)
        {
            _logger.LogDebug("Trimmed {Count} entries (keeping {Keep})", deleted, keepCount);
        }

        return deleted;
    }

    /// <inheritdoc />
    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        const string sql = """SELECT COUNT(*) FROM "RecentFiles" """;

        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        var count = await connection.ExecuteScalarAsync<int>(command);

        _logger.LogDebug("GetCount: {Count} entries", count);
        return count;
    }

    /// <summary>
    /// Internal DTO for mapping query results.
    /// </summary>
    private sealed record RecentFileDto
    {
        public required string FilePath { get; init; }
        public required string FileName { get; init; }
        public DateTimeOffset LastOpenedAt { get; init; }
        public int OpenCount { get; init; }
    }
}
