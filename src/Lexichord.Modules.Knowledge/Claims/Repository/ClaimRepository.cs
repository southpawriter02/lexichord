// =============================================================================
// File: ClaimRepository.cs
// Project: Lexichord.Modules.Knowledge
// Description: PostgreSQL-backed implementation of IClaimRepository.
// =============================================================================
// LOGIC: Provides CRUD, search, and bulk operations for claims.
//   - Uses Dapper for data access with parameterized queries.
//   - Caches frequently accessed claims via IMemoryCache.
//   - Publishes MediatR events on create/update/delete.
//   - Full-text search uses PostgreSQL tsvector/tsquery.
//
// v0.5.6h: Claim Repository (Knowledge Graph Claim Persistence)
// Dependencies: IDbConnectionFactory (v0.0.5b), Claim (v0.5.6e), MediatR, Dapper
// =============================================================================

using System.Text.Json;
using Dapper;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Claims;
using Lexichord.Abstractions.Contracts.Knowledge.Claims.Repository;
using Lexichord.Abstractions.Contracts.Knowledge.Claims.Repository.Events;
using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using DapperCommandDefinition = Dapper.CommandDefinition;

namespace Lexichord.Modules.Knowledge.Claims.Repository;

/// <summary>
/// PostgreSQL-backed implementation of <see cref="IClaimRepository"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Data Access:</b> Uses Dapper ORM with parameterized queries for security.
/// Connections are obtained via <see cref="IDbConnectionFactory"/> and properly disposed.
/// </para>
/// <para>
/// <b>Caching:</b> Individual claims are cached for 5 minutes to reduce database load.
/// Cache is invalidated on write operations.
/// </para>
/// <para>
/// <b>Events:</b> MediatR notifications are published on create, update, and delete
/// operations to enable downstream processing.
/// </para>
/// </remarks>
public sealed class ClaimRepository : IClaimRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IMemoryCache _cache;
    private readonly IMediator _mediator;
    private readonly ILogger<ClaimRepository> _logger;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private const string CachePrefix = "claim:";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Creates a new <see cref="ClaimRepository"/> instance.
    /// </summary>
    public ClaimRepository(
        IDbConnectionFactory connectionFactory,
        IMemoryCache cache,
        IMediator mediator,
        ILogger<ClaimRepository> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Read Operations

    /// <inheritdoc />
    public async Task<Claim?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var cacheKey = $"{CachePrefix}{id}";

        if (_cache.TryGetValue(cacheKey, out Claim? cached))
        {
            _logger.LogDebug("GetAsync cache hit for claim {ClaimId}", id);
            return cached;
        }

        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = "SELECT * FROM claims WHERE id = @Id AND is_active = true";

        var row = await connection.QueryFirstOrDefaultAsync<ClaimRow>(
            new DapperCommandDefinition(sql, new { Id = id }, cancellationToken: ct));

        if (row == null)
        {
            _logger.LogDebug("GetAsync claim {ClaimId}: NotFound", id);
            return null;
        }

        var claim = MapToClaim(row);
        _cache.Set(cacheKey, claim, CacheDuration);

        _logger.LogDebug("GetAsync claim {ClaimId}: Found", id);
        return claim;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Claim>> GetManyAsync(
        IEnumerable<Guid> ids,
        CancellationToken ct = default)
    {
        var idList = ids.ToList();
        if (idList.Count == 0) return Array.Empty<Claim>();

        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = "SELECT * FROM claims WHERE id = ANY(@Ids) AND is_active = true";

        var rows = await connection.QueryAsync<ClaimRow>(
            new DapperCommandDefinition(sql, new { Ids = idList.ToArray() }, cancellationToken: ct));

        var claims = rows.Select(MapToClaim).ToList();
        _logger.LogDebug("GetManyAsync: {RequestCount} requested, {FoundCount} found", idList.Count, claims.Count);

        return claims;
    }

    /// <inheritdoc />
    public async Task<ClaimQueryResult> SearchAsync(
        ClaimSearchCriteria criteria,
        CancellationToken ct = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        var (whereClause, parameters) = BuildWhereClause(criteria);
        var orderBy = BuildOrderBy(criteria);

        var skip = criteria.Skip ?? criteria.Page * criteria.PageSize;
        var take = criteria.Take ?? criteria.PageSize;

        // Get total count for pagination
        var countSql = $"SELECT COUNT(*) FROM claims WHERE {whereClause}";
        var totalCount = await connection.ExecuteScalarAsync<int>(
            new DapperCommandDefinition(countSql, parameters, cancellationToken: ct));

        // Get paginated results
        var querySql = $@"
            SELECT * FROM claims
            WHERE {whereClause}
            ORDER BY {orderBy}
            OFFSET @Skip LIMIT @Take";

        parameters.Add("Skip", skip);
        parameters.Add("Take", take);

        var rows = await connection.QueryAsync<ClaimRow>(
            new DapperCommandDefinition(querySql, parameters, cancellationToken: ct));

        var claims = rows.Select(MapToClaim).ToList();
        _logger.LogDebug("SearchAsync: {Count} claims found, {Total} total", claims.Count, totalCount);

        return new ClaimQueryResult
        {
            Claims = claims,
            TotalCount = totalCount,
            Page = criteria.Page,
            PageSize = criteria.PageSize
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Claim>> GetByDocumentAsync(Guid documentId, CancellationToken ct = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            SELECT * FROM claims
            WHERE document_id = @DocumentId AND is_active = true
            ORDER BY evidence_start_offset";

        var rows = await connection.QueryAsync<ClaimRow>(
            new DapperCommandDefinition(sql, new { DocumentId = documentId }, cancellationToken: ct));

        var claims = rows.Select(MapToClaim).ToList();
        _logger.LogDebug("GetByDocumentAsync {DocumentId}: {Count} claims", documentId, claims.Count);

        return claims;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Claim>> GetByEntityAsync(Guid entityId, CancellationToken ct = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            SELECT * FROM claims
            WHERE (subject_entity_id = @EntityId OR object_entity_id = @EntityId)
              AND is_active = true
            ORDER BY extracted_at DESC";

        var rows = await connection.QueryAsync<ClaimRow>(
            new DapperCommandDefinition(sql, new { EntityId = entityId }, cancellationToken: ct));

        var claims = rows.Select(MapToClaim).ToList();
        _logger.LogDebug("GetByEntityAsync {EntityId}: {Count} claims", entityId, claims.Count);

        return claims;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Claim>> GetByPredicateAsync(
        string predicate,
        Guid? projectId = null,
        CancellationToken ct = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        var sql = @"SELECT * FROM claims WHERE predicate = @Predicate AND is_active = true"
            + (projectId.HasValue ? " AND project_id = @ProjectId" : "")
            + " ORDER BY extracted_at DESC";

        var rows = await connection.QueryAsync<ClaimRow>(
            new DapperCommandDefinition(sql, new { Predicate = predicate, ProjectId = projectId }, cancellationToken: ct));

        var claims = rows.Select(MapToClaim).ToList();
        _logger.LogDebug("GetByPredicateAsync {Predicate}: {Count} claims", predicate, claims.Count);

        return claims;
    }

    /// <inheritdoc />
    public async Task<int> CountAsync(ClaimSearchCriteria? criteria = null, CancellationToken ct = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        if (criteria == null)
        {
            const string sql = "SELECT COUNT(*) FROM claims WHERE is_active = true";
            return await connection.ExecuteScalarAsync<int>(
                new DapperCommandDefinition(sql, cancellationToken: ct));
        }

        var (whereClause, parameters) = BuildWhereClause(criteria);
        var countSql = $"SELECT COUNT(*) FROM claims WHERE {whereClause}";

        return await connection.ExecuteScalarAsync<int>(
            new DapperCommandDefinition(countSql, parameters, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Claim>> FullTextSearchAsync(
        string query,
        Guid? projectId = null,
        int limit = 50,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return Array.Empty<Claim>();

        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        var sql = @"
            SELECT * FROM claims
            WHERE is_active = true
              AND (
                to_tsvector('english', evidence_sentence) @@ plainto_tsquery('english', @Query)
                OR subject_surface_form ILIKE @Pattern
                OR object_surface_form ILIKE @Pattern
              )"
            + (projectId.HasValue ? " AND project_id = @ProjectId" : "")
            + @" ORDER BY ts_rank(to_tsvector('english', evidence_sentence), plainto_tsquery('english', @Query)) DESC
            LIMIT @Limit";

        var rows = await connection.QueryAsync<ClaimRow>(
            new DapperCommandDefinition(sql, new { Query = query, Pattern = $"%{query}%", ProjectId = projectId, Limit = limit }, cancellationToken: ct));

        var claims = rows.Select(MapToClaim).ToList();
        _logger.LogDebug("FullTextSearchAsync '{Query}': {Count} claims", query, claims.Count);

        return claims;
    }

    #endregion

    #region Write Operations

    /// <inheritdoc />
    public async Task<Claim> CreateAsync(Claim claim, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(claim);

        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        var row = MapToRow(claim);
        await connection.ExecuteAsync(new DapperCommandDefinition(GetInsertSql(), row, cancellationToken: ct));

        await _mediator.Publish(new ClaimCreatedEvent { Claim = claim }, ct);
        _logger.LogInformation("Created claim {ClaimId} for document {DocumentId}", claim.Id, claim.DocumentId);

        return claim;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Claim>> CreateManyAsync(IEnumerable<Claim> claims, CancellationToken ct = default)
    {
        var claimList = claims.ToList();
        if (claimList.Count == 0) return Array.Empty<Claim>();

        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        try
        {
            var insertSql = GetInsertSql();
            foreach (var claim in claimList)
            {
                var row = MapToRow(claim);
                await connection.ExecuteAsync(new DapperCommandDefinition(insertSql, row, transaction, cancellationToken: ct));
            }

            await transaction.CommitAsync(ct);

            foreach (var claim in claimList)
            {
                await _mediator.Publish(new ClaimCreatedEvent { Claim = claim }, ct);
            }

            _logger.LogInformation("Created {Count} claims in batch", claimList.Count);
            return claimList;
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Claim> UpdateAsync(Claim claim, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(claim);

        var previous = await GetAsync(claim.Id, ct);
        if (previous == null)
            throw new InvalidOperationException($"Claim {claim.Id} not found for update.");

        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        var row = MapToRow(claim);
        await connection.ExecuteAsync(new DapperCommandDefinition(GetUpdateSql(), row, cancellationToken: ct));

        InvalidateCache(claim.Id);
        await _mediator.Publish(new ClaimUpdatedEvent { Claim = claim, PreviousClaim = previous }, ct);
        _logger.LogInformation("Updated claim {ClaimId}", claim.Id);

        return claim;
    }

    /// <inheritdoc />
    public async Task<BulkUpsertResult> UpsertManyAsync(IEnumerable<Claim> claims, CancellationToken ct = default)
    {
        var claimList = claims.ToList();
        if (claimList.Count == 0) return BulkUpsertResult.Empty;

        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        int created = 0, updated = 0, unchanged = 0;
        var errors = new List<ClaimUpsertError>();

        try
        {
            foreach (var claim in claimList)
            {
                try
                {
                    var existing = await connection.QueryFirstOrDefaultAsync<ClaimRow>(
                        new DapperCommandDefinition("SELECT * FROM claims WHERE id = @Id", new { claim.Id }, transaction, cancellationToken: ct));

                    if (existing == null)
                    {
                        await connection.ExecuteAsync(new DapperCommandDefinition(GetInsertSql(), MapToRow(claim), transaction, cancellationToken: ct));
                        created++;
                    }
                    else if (HasChanged(existing, claim))
                    {
                        await connection.ExecuteAsync(new DapperCommandDefinition(GetUpdateSql(), MapToRow(claim), transaction, cancellationToken: ct));
                        updated++;
                        InvalidateCache(claim.Id);
                    }
                    else
                    {
                        unchanged++;
                    }
                }
                catch (Exception ex)
                {
                    errors.Add(new ClaimUpsertError { ClaimId = claim.Id, Error = ex.Message });
                    _logger.LogWarning(ex, "Failed to upsert claim {ClaimId}", claim.Id);
                }
            }

            await transaction.CommitAsync(ct);

            _logger.LogInformation("UpsertManyAsync: {Created} created, {Updated} updated, {Unchanged} unchanged", created, updated, unchanged);

            return new BulkUpsertResult
            {
                CreatedCount = created,
                UpdatedCount = updated,
                UnchangedCount = unchanged,
                Errors = errors.Count > 0 ? errors : null
            };
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var existing = await GetAsync(id, ct);
        if (existing == null)
        {
            _logger.LogDebug("DeleteAsync claim {ClaimId}: NotFound", id);
            return false;
        }

        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = "UPDATE claims SET is_active = false, updated_at = @UpdatedAt WHERE id = @Id";

        var affected = await connection.ExecuteAsync(
            new DapperCommandDefinition(sql, new { Id = id, UpdatedAt = DateTimeOffset.UtcNow }, cancellationToken: ct));

        if (affected > 0)
        {
            InvalidateCache(id);
            await _mediator.Publish(new ClaimDeletedEvent { ClaimId = id, DocumentId = existing.DocumentId }, ct);
            _logger.LogInformation("Deleted claim {ClaimId}", id);
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public async Task<int> DeleteByDocumentAsync(Guid documentId, CancellationToken ct = default)
    {
        var existingClaims = await GetByDocumentAsync(documentId, ct);

        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = "UPDATE claims SET is_active = false, updated_at = @UpdatedAt WHERE document_id = @DocumentId AND is_active = true";

        var affected = await connection.ExecuteAsync(
            new DapperCommandDefinition(sql, new { DocumentId = documentId, UpdatedAt = DateTimeOffset.UtcNow }, cancellationToken: ct));

        if (affected > 0)
        {
            foreach (var claim in existingClaims)
            {
                InvalidateCache(claim.Id);
            }

            await _mediator.Publish(new DocumentClaimsReplacedEvent
            {
                DocumentId = documentId,
                OldClaimCount = existingClaims.Count,
                NewClaimCount = 0
            }, ct);

            _logger.LogInformation("Deleted {Count} claims for document {DocumentId}", affected, documentId);
        }

        return affected;
    }

    #endregion

    #region SQL Helpers

    private static string GetInsertSql() => """
        INSERT INTO claims (
            id, project_id, document_id,
            subject_entity_id, subject_entity_type, subject_surface_form,
            subject_normalized_form, subject_start_offset, subject_end_offset,
            predicate,
            object_type, object_entity_id, object_entity_type, object_surface_form,
            object_literal_value, object_literal_type, object_unit,
            confidence, validation_status, validation_messages_json,
            is_reviewed, review_notes,
            extraction_method, pattern_id,
            evidence_sentence, evidence_start_offset, evidence_end_offset, evidence_section,
            metadata_json, is_active, version, extracted_at, updated_at
        ) VALUES (
            @Id, @ProjectId, @DocumentId,
            @SubjectEntityId, @SubjectEntityType, @SubjectSurfaceForm,
            @SubjectNormalizedForm, @SubjectStartOffset, @SubjectEndOffset,
            @Predicate,
            @ObjectType, @ObjectEntityId, @ObjectEntityType, @ObjectSurfaceForm,
            @ObjectLiteralValue, @ObjectLiteralType, @ObjectUnit,
            @Confidence, @ValidationStatus, @ValidationMessagesJson,
            @IsReviewed, @ReviewNotes,
            @ExtractionMethod, @PatternId,
            @EvidenceSentence, @EvidenceStartOffset, @EvidenceEndOffset, @EvidenceSection,
            @MetadataJson, @IsActive, @Version, @ExtractedAt, @UpdatedAt
        )
        """;

    private static string GetUpdateSql() => """
        UPDATE claims SET
            subject_entity_id = @SubjectEntityId,
            subject_entity_type = @SubjectEntityType,
            subject_surface_form = @SubjectSurfaceForm,
            subject_normalized_form = @SubjectNormalizedForm,
            predicate = @Predicate,
            object_type = @ObjectType,
            object_entity_id = @ObjectEntityId,
            object_entity_type = @ObjectEntityType,
            object_surface_form = @ObjectSurfaceForm,
            object_literal_value = @ObjectLiteralValue,
            object_literal_type = @ObjectLiteralType,
            confidence = @Confidence,
            validation_status = @ValidationStatus,
            validation_messages_json = @ValidationMessagesJson,
            is_reviewed = @IsReviewed,
            review_notes = @ReviewNotes,
            version = version + 1,
            updated_at = @UpdatedAt
        WHERE id = @Id
        """;

    private (string whereClause, DynamicParameters parameters) BuildWhereClause(ClaimSearchCriteria criteria)
    {
        var conditions = new List<string> { "is_active = true" };
        var parameters = new DynamicParameters();

        if (criteria.ProjectId.HasValue)
        {
            conditions.Add("project_id = @ProjectId");
            parameters.Add("ProjectId", criteria.ProjectId.Value);
        }

        if (criteria.DocumentId.HasValue)
        {
            conditions.Add("document_id = @DocumentId");
            parameters.Add("DocumentId", criteria.DocumentId.Value);
        }

        if (criteria.SubjectEntityId.HasValue)
        {
            conditions.Add("subject_entity_id = @SubjectEntityId");
            parameters.Add("SubjectEntityId", criteria.SubjectEntityId.Value);
        }

        if (criteria.ObjectEntityId.HasValue)
        {
            conditions.Add("object_entity_id = @ObjectEntityId");
            parameters.Add("ObjectEntityId", criteria.ObjectEntityId.Value);
        }

        if (!string.IsNullOrEmpty(criteria.Predicate))
        {
            conditions.Add("predicate = @Predicate");
            parameters.Add("Predicate", criteria.Predicate);
        }

        if (criteria.Predicates?.Count > 0)
        {
            conditions.Add("predicate = ANY(@Predicates)");
            parameters.Add("Predicates", criteria.Predicates.ToArray());
        }

        if (criteria.ValidationStatus.HasValue)
        {
            conditions.Add("validation_status = @ValidationStatus");
            parameters.Add("ValidationStatus", criteria.ValidationStatus.Value.ToString());
        }

        if (criteria.MinConfidence.HasValue)
        {
            conditions.Add("confidence >= @MinConfidence");
            parameters.Add("MinConfidence", criteria.MinConfidence.Value);
        }

        if (criteria.MaxConfidence.HasValue)
        {
            conditions.Add("confidence <= @MaxConfidence");
            parameters.Add("MaxConfidence", criteria.MaxConfidence.Value);
        }

        if (criteria.IsReviewed.HasValue)
        {
            conditions.Add("is_reviewed = @IsReviewed");
            parameters.Add("IsReviewed", criteria.IsReviewed.Value);
        }

        if (criteria.ExtractionMethod.HasValue)
        {
            conditions.Add("extraction_method = @ExtractionMethod");
            parameters.Add("ExtractionMethod", criteria.ExtractionMethod.Value.ToString());
        }

        if (!string.IsNullOrEmpty(criteria.TextQuery))
        {
            conditions.Add("to_tsvector('english', evidence_sentence) @@ plainto_tsquery('english', @TextQuery)");
            parameters.Add("TextQuery", criteria.TextQuery);
        }

        if (criteria.ExtractedAfter.HasValue)
        {
            conditions.Add("extracted_at >= @ExtractedAfter");
            parameters.Add("ExtractedAfter", criteria.ExtractedAfter.Value);
        }

        if (criteria.ExtractedBefore.HasValue)
        {
            conditions.Add("extracted_at <= @ExtractedBefore");
            parameters.Add("ExtractedBefore", criteria.ExtractedBefore.Value);
        }

        return (string.Join(" AND ", conditions), parameters);
    }

    private static string BuildOrderBy(ClaimSearchCriteria criteria)
    {
        var field = criteria.SortBy switch
        {
            ClaimSortField.ExtractedAt => "extracted_at",
            ClaimSortField.Confidence => "confidence",
            ClaimSortField.Predicate => "predicate",
            ClaimSortField.ValidationStatus => "validation_status",
            _ => "extracted_at"
        };

        var direction = criteria.SortDirection == SortDirection.Ascending ? "ASC" : "DESC";
        return $"{field} {direction}";
    }

    #endregion

    #region Mapping

    private static Claim MapToClaim(ClaimRow row)
    {
        // Build subject entity
        var subject = new ClaimEntity
        {
            EntityId = row.SubjectEntityId,
            EntityType = row.SubjectEntityType,
            SurfaceForm = row.SubjectSurfaceForm,
            NormalizedForm = row.SubjectNormalizedForm,
            StartOffset = row.SubjectStartOffset,
            EndOffset = row.SubjectEndOffset
        };

        // Build object
        ClaimObject claimObject;
        if (row.ObjectType == "Entity" && !string.IsNullOrEmpty(row.ObjectSurfaceForm))
        {
            var objectEntity = new ClaimEntity
            {
                EntityId = row.ObjectEntityId,
                EntityType = row.ObjectEntityType ?? "",
                SurfaceForm = row.ObjectSurfaceForm
            };
            claimObject = ClaimObject.FromEntity(objectEntity);
        }
        else if (row.ObjectType == "Literal" && row.ObjectLiteralValue != null)
        {
            claimObject = new ClaimObject
            {
                Type = ClaimObjectType.Literal,
                LiteralValue = row.ObjectLiteralValue,
                LiteralType = row.ObjectLiteralType,
                Unit = row.ObjectUnit
            };
        }
        else
        {
            // Default empty entity object
            claimObject = ClaimObject.FromEntity(new ClaimEntity
            {
                EntityType = "",
                SurfaceForm = ""
            });
        }

        // Build evidence
        ClaimEvidence? evidence = null;
        if (!string.IsNullOrEmpty(row.EvidenceSentence))
        {
            evidence = new ClaimEvidence
            {
                Sentence = row.EvidenceSentence,
                StartOffset = row.EvidenceStartOffset,
                EndOffset = row.EvidenceEndOffset,
                Section = row.EvidenceSection,
                ExtractionMethod = Enum.TryParse<ClaimExtractionMethod>(row.ExtractionMethod, out var method)
                    ? method : ClaimExtractionMethod.PatternRule,
                PatternId = row.PatternId
            };
        }

        // Parse validation messages if present
        var validationMessages = Array.Empty<ClaimValidationMessage>();
        if (!string.IsNullOrEmpty(row.ValidationMessagesJson))
        {
            validationMessages = JsonSerializer.Deserialize<ClaimValidationMessage[]>(row.ValidationMessagesJson, JsonOptions)
                ?? Array.Empty<ClaimValidationMessage>();
        }

        return new Claim
        {
            Id = row.Id,
            ProjectId = row.ProjectId,
            DocumentId = row.DocumentId,
            Subject = subject,
            Predicate = row.Predicate,
            Object = claimObject,
            Confidence = row.Confidence,
            ValidationStatus = Enum.TryParse<ClaimValidationStatus>(row.ValidationStatus, out var status)
                ? status : ClaimValidationStatus.Pending,
            ValidationMessages = validationMessages,
            IsReviewed = row.IsReviewed,
            Evidence = evidence,
            Version = row.Version,
            ExtractedAt = row.ExtractedAt
        };
    }

    private static ClaimRow MapToRow(Claim claim)
    {
        string? validationMessagesJson = null;
        if (claim.ValidationMessages.Count > 0)
        {
            validationMessagesJson = JsonSerializer.Serialize(claim.ValidationMessages, JsonOptions);
        }

        return new ClaimRow
        {
            Id = claim.Id,
            ProjectId = claim.ProjectId ?? Guid.Empty,
            DocumentId = claim.DocumentId,
            SubjectEntityId = claim.Subject.EntityId,
            SubjectEntityType = claim.Subject.EntityType,
            SubjectSurfaceForm = claim.Subject.SurfaceForm,
            SubjectNormalizedForm = claim.Subject.NormalizedForm ?? "",
            SubjectStartOffset = claim.Subject.StartOffset,
            SubjectEndOffset = claim.Subject.EndOffset,
            Predicate = claim.Predicate,
            ObjectType = claim.Object.Type.ToString(),
            ObjectEntityId = claim.Object.Entity?.EntityId,
            ObjectEntityType = claim.Object.Entity?.EntityType,
            ObjectSurfaceForm = claim.Object.Entity?.SurfaceForm,
            ObjectLiteralValue = claim.Object.LiteralValue,
            ObjectLiteralType = claim.Object.LiteralType,
            ObjectUnit = claim.Object.Unit,
            Confidence = claim.Confidence,
            ValidationStatus = claim.ValidationStatus.ToString(),
            ValidationMessagesJson = validationMessagesJson,
            IsReviewed = claim.IsReviewed,
            ReviewNotes = claim.ReviewNotes,
            ExtractionMethod = claim.Evidence?.ExtractionMethod.ToString() ?? "",
            PatternId = claim.Evidence?.PatternId,
            EvidenceSentence = claim.Evidence?.Sentence ?? "",
            EvidenceStartOffset = claim.Evidence?.StartOffset ?? 0,
            EvidenceEndOffset = claim.Evidence?.EndOffset ?? 0,
            EvidenceSection = claim.Evidence?.Section,
            IsActive = claim.IsActive,
            Version = claim.Version,
            ExtractedAt = claim.ExtractedAt,
            UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    private static bool HasChanged(ClaimRow existing, Claim claim)
    {
        return existing.Predicate != claim.Predicate
            || Math.Abs(existing.Confidence - claim.Confidence) > 0.001f
            || existing.ValidationStatus != claim.ValidationStatus.ToString()
            || existing.IsReviewed != claim.IsReviewed
            || existing.SubjectSurfaceForm != claim.Subject.SurfaceForm
            || existing.ObjectLiteralValue != claim.Object.LiteralValue;
    }

    #endregion

    #region Cache Helpers

    private void InvalidateCache(Guid claimId)
    {
        var cacheKey = $"{CachePrefix}{claimId}";
        _cache.Remove(cacheKey);
    }

    #endregion
}
