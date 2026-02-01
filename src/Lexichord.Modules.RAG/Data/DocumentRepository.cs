// =============================================================================
// File: DocumentRepository.cs
// Project: Lexichord.Modules.RAG
// Description: Dapper implementation of IDocumentRepository for RAG documents.
// =============================================================================
// LOGIC: Provides CRUD operations for documents in the RAG system.
//   - Uses IDbConnectionFactory for connection management.
//   - All queries use parameterized SQL to prevent injection.
//   - Comprehensive logging at Debug level for troubleshooting.
//   - Thread-safe: connections are short-lived and disposed.
// =============================================================================

using System.Data;
using Dapper;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Microsoft.Extensions.Logging;
using DapperCommandDefinition = Dapper.CommandDefinition;

namespace Lexichord.Modules.RAG.Data;

/// <summary>
/// Dapper-based implementation of <see cref="IDocumentRepository"/> for managing
/// indexed documents in the RAG system.
/// </summary>
/// <remarks>
/// <para>
/// This repository provides all CRUD operations for the <c>documents</c> table,
/// including status management for the indexing lifecycle.
/// </para>
/// <para>
/// <b>Connection Management:</b> Connections are obtained via <see cref="IDbConnectionFactory"/>
/// and disposed using <c>await using</c> to prevent connection leaks.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This implementation is thread-safe. Each method creates
/// its own connection and uses parameterized queries. Suitable for concurrent use.
/// </para>
/// </remarks>
public sealed class DocumentRepository : IDocumentRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<DocumentRepository> _logger;

    private const string TableName = "documents";

    /// <summary>
    /// Creates a new <see cref="DocumentRepository"/> instance.
    /// </summary>
    /// <param name="connectionFactory">Factory for database connections.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="connectionFactory"/> or <paramref name="logger"/> is null.
    /// </exception>
    public DocumentRepository(
        IDbConnectionFactory connectionFactory,
        ILogger<DocumentRepository> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Read Operations

    /// <inheritdoc />
    public async Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        const string sql = @"
            SELECT id AS ""Id"", 
                   project_id AS ""ProjectId"", 
                   file_path AS ""FilePath"", 
                   title AS ""Title"", 
                   hash AS ""Hash"", 
                   status AS ""Status"", 
                   indexed_at AS ""IndexedAt"", 
                   failure_reason AS ""FailureReason""
            FROM documents
            WHERE id = @Id";

        var command = new DapperCommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        var result = await connection.QuerySingleOrDefaultAsync<Document>(command);

        _logger.LogDebug("GetById {Table} Id={Id}: {Result}", TableName, id, result is not null ? "Found" : "NotFound");
        return result;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Document>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        const string sql = @"
            SELECT id AS ""Id"", 
                   project_id AS ""ProjectId"", 
                   file_path AS ""FilePath"", 
                   title AS ""Title"", 
                   hash AS ""Hash"", 
                   status AS ""Status"", 
                   indexed_at AS ""IndexedAt"", 
                   failure_reason AS ""FailureReason""
            FROM documents
            WHERE project_id = @ProjectId
            ORDER BY file_path";

        var command = new DapperCommandDefinition(sql, new { ProjectId = projectId }, cancellationToken: cancellationToken);
        var results = await connection.QueryAsync<Document>(command);
        var resultList = results.ToList();

        _logger.LogDebug("GetByProject {Table} ProjectId={ProjectId}: {Count} documents", 
            TableName, projectId, resultList.Count);
        return resultList;
    }

    /// <inheritdoc />
    public async Task<Document?> GetByFilePathAsync(Guid projectId, string filePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path cannot be empty.", nameof(filePath));

        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        const string sql = @"
            SELECT id AS ""Id"", 
                   project_id AS ""ProjectId"", 
                   file_path AS ""FilePath"", 
                   title AS ""Title"", 
                   hash AS ""Hash"", 
                   status AS ""Status"", 
                   indexed_at AS ""IndexedAt"", 
                   failure_reason AS ""FailureReason""
            FROM documents
            WHERE project_id = @ProjectId AND file_path = @FilePath";

        var command = new DapperCommandDefinition(sql, new { ProjectId = projectId, FilePath = filePath }, cancellationToken: cancellationToken);
        var result = await connection.QuerySingleOrDefaultAsync<Document>(command);

        _logger.LogDebug("GetByFilePath {Table} ProjectId={ProjectId} FilePath={FilePath}: {Result}", 
            TableName, projectId, filePath, result is not null ? "Found" : "NotFound");
        return result;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Document>> GetByStatusAsync(DocumentStatus status, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        const string sql = @"
            SELECT id AS ""Id"", 
                   project_id AS ""ProjectId"", 
                   file_path AS ""FilePath"", 
                   title AS ""Title"", 
                   hash AS ""Hash"", 
                   status AS ""Status"", 
                   indexed_at AS ""IndexedAt"", 
                   failure_reason AS ""FailureReason""
            FROM documents
            WHERE status = @Status
            ORDER BY file_path";

        var command = new DapperCommandDefinition(sql, new { Status = status.ToString() }, cancellationToken: cancellationToken);
        var results = await connection.QueryAsync<Document>(command);
        var resultList = results.ToList();

        _logger.LogDebug("GetByStatus {Table} Status={Status}: {Count} documents", 
            TableName, status, resultList.Count);
        return resultList;
    }

    #endregion

    #region Write Operations

    /// <inheritdoc />
    public async Task<Document> AddAsync(Document document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        // LOGIC: INSERT with RETURNING to get the database-assigned ID and any defaults.
        const string sql = @"
            INSERT INTO documents (id, project_id, file_path, title, hash, status, indexed_at, failure_reason)
            VALUES (@Id, @ProjectId, @FilePath, @Title, @Hash, @Status, @IndexedAt, @FailureReason)
            RETURNING id AS ""Id"", 
                      project_id AS ""ProjectId"", 
                      file_path AS ""FilePath"", 
                      title AS ""Title"", 
                      hash AS ""Hash"", 
                      status AS ""Status"", 
                      indexed_at AS ""IndexedAt"", 
                      failure_reason AS ""FailureReason""";

        // LOGIC: Generate ID if not provided (Guid.Empty indicates database should assign).
        var documentId = document.Id == Guid.Empty ? Guid.NewGuid() : document.Id;

        var parameters = new
        {
            Id = documentId,
            document.ProjectId,
            document.FilePath,
            document.Title,
            document.Hash,
            Status = document.Status.ToString(),
            document.IndexedAt,
            document.FailureReason
        };

        var command = new DapperCommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        var result = await connection.QuerySingleAsync<Document>(command);

        _logger.LogDebug("Add {Table}: Created document Id={Id} FilePath={FilePath}", 
            TableName, result.Id, result.FilePath);
        return result;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(Document document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        const string sql = @"
            UPDATE documents SET
                file_path = @FilePath,
                title = @Title,
                hash = @Hash,
                status = @Status,
                indexed_at = @IndexedAt,
                failure_reason = @FailureReason
            WHERE id = @Id";

        var parameters = new
        {
            document.Id,
            document.FilePath,
            document.Title,
            document.Hash,
            Status = document.Status.ToString(),
            document.IndexedAt,
            document.FailureReason
        };

        var command = new DapperCommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        var affected = await connection.ExecuteAsync(command);

        _logger.LogDebug("Update {Table} Id={Id}: {Result}", 
            TableName, document.Id, affected > 0 ? "Success" : "NotFound");
    }

    /// <inheritdoc />
    public async Task UpdateStatusAsync(
        Guid id,
        DocumentStatus status,
        string? failureReason = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        // LOGIC: When transitioning to Indexed status, also update the IndexedAt timestamp.
        string sql;
        object parameters;

        if (status == DocumentStatus.Indexed)
        {
            sql = @"
                UPDATE documents SET
                    status = @Status,
                    indexed_at = @IndexedAt,
                    failure_reason = NULL
                WHERE id = @Id";
            parameters = new { Id = id, Status = status.ToString(), IndexedAt = DateTime.UtcNow };
        }
        else
        {
            sql = @"
                UPDATE documents SET
                    status = @Status,
                    failure_reason = @FailureReason
                WHERE id = @Id";
            parameters = new { Id = id, Status = status.ToString(), FailureReason = failureReason };
        }

        var command = new DapperCommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        var affected = await connection.ExecuteAsync(command);

        _logger.LogDebug("UpdateStatus {Table} Id={Id} Status={Status}: {Result}", 
            TableName, id, status, affected > 0 ? "Success" : "NotFound");
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        // LOGIC: Cascade delete of chunks is handled by the database FK constraint.
        const string sql = "DELETE FROM documents WHERE id = @Id";
        var command = new DapperCommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        var affected = await connection.ExecuteAsync(command);

        _logger.LogDebug("Delete {Table} Id={Id}: {Result}", 
            TableName, id, affected > 0 ? "Success" : "NotFound");
    }

    #endregion
}
