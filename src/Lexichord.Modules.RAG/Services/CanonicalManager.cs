// =============================================================================
// File: CanonicalManager.cs
// Project: Lexichord.Modules.RAG
// Description: Implementation of ICanonicalManager for canonical record management.
// =============================================================================
// VERSION: v0.5.9c (Canonical Record Management)
// LOGIC: Manages canonical records for semantic deduplication:
//   - Creates canonical records for unique chunks
//   - Merges variants using atomic transactions
//   - Supports promotion and detachment operations
//   - Tracks provenance for audit trails
//   - License gating: Writer Pro required
// =============================================================================

using System.Data;
using Dapper;
using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Abstractions.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using DapperCommandDefinition = Dapper.CommandDefinition;

namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// Implementation of <see cref="ICanonicalManager"/> for managing canonical records,
/// chunk variants, and provenance tracking.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9c as part of the Semantic Memory Deduplication feature.
/// </para>
/// <para>
/// This service provides atomic operations for deduplication:
/// </para>
/// <list type="bullet">
///   <item><description>Create canonical records for unique facts.</description></item>
///   <item><description>Merge duplicate chunks as variants with relationship tracking.</description></item>
///   <item><description>Promote variants to canonical when better representations are found.</description></item>
///   <item><description>Detach variants to restore them as independent entities.</description></item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> This service is thread-safe. All mutation operations use
/// database transactions to ensure atomicity.
/// </para>
/// </remarks>
public sealed class CanonicalManager : ICanonicalManager
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILicenseContext _licenseContext;
    private readonly IMediator _mediator;
    private readonly ILogger<CanonicalManager> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CanonicalManager"/> class.
    /// </summary>
    /// <param name="connectionFactory">Factory for database connections.</param>
    /// <param name="licenseContext">License context for feature gating.</param>
    /// <param name="mediator">MediatR for publishing domain events.</param>
    /// <param name="logger">Logger for structured diagnostics.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any required parameter is <c>null</c>.
    /// </exception>
    public CanonicalManager(
        IDbConnectionFactory connectionFactory,
        ILicenseContext licenseContext,
        IMediator mediator,
        ILogger<CanonicalManager> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("CanonicalManager initialized");
    }

    /// <inheritdoc/>
    public async Task<CanonicalRecord> CreateCanonicalAsync(Chunk chunk, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        EnsureLicensed();

        _logger.LogDebug(
            "Creating canonical record for ChunkId={ChunkId}",
            chunk.Id);

        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        // LOGIC: Check if chunk already has a canonical record (as canonical or variant).
        var existing = await GetCanonicalForChunkInternalAsync(connection, chunk.Id, ct);
        if (existing is not null)
        {
            throw new InvalidOperationException(
                $"Chunk {chunk.Id} already has a canonical record (as {(existing.CanonicalChunkId == chunk.Id ? "canonical" : "variant")}).");
        }

        var now = DateTimeOffset.UtcNow;
        var id = Guid.NewGuid();

        const string sql = @"
            INSERT INTO ""CanonicalRecords"" (""Id"", ""CanonicalChunkId"", ""CreatedAt"", ""UpdatedAt"", ""MergeCount"")
            VALUES (@Id, @CanonicalChunkId, @CreatedAt, @UpdatedAt, @MergeCount)";

        var command = new DapperCommandDefinition(sql, new
        {
            Id = id,
            CanonicalChunkId = chunk.Id,
            CreatedAt = now,
            UpdatedAt = now,
            MergeCount = 0
        }, cancellationToken: ct);

        await connection.ExecuteAsync(command);

        var record = new CanonicalRecord(id, chunk.Id, now, now, 0)
        {
            CanonicalChunk = chunk
        };

        _logger.LogInformation(
            "Created canonical record: Id={CanonicalRecordId}, ChunkId={ChunkId}",
            id,
            chunk.Id);

        // LOGIC: Publish event for downstream consumers.
        await _mediator.Publish(new CanonicalRecordCreatedEvent(id, chunk.Id, now), ct);

        return record;
    }

    /// <inheritdoc/>
    public async Task MergeIntoCanonicalAsync(
        Guid canonicalId,
        Chunk variant,
        RelationshipType type,
        float similarity,
        CancellationToken ct = default)
    {
        if (canonicalId == Guid.Empty)
            throw new ArgumentException("Canonical ID cannot be empty.", nameof(canonicalId));
        ArgumentNullException.ThrowIfNull(variant);
        EnsureLicensed();

        _logger.LogDebug(
            "Merging variant ChunkId={VariantChunkId} into CanonicalId={CanonicalId}, " +
            "Type={RelationshipType}, Similarity={Similarity:F3}",
            variant.Id,
            canonicalId,
            type,
            similarity);

        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        try
        {
            // LOGIC: Verify canonical record exists.
            const string checkCanonicalSql = @"
                SELECT COUNT(1) FROM ""CanonicalRecords"" WHERE ""Id"" = @CanonicalId";
            var canonicalExists = await connection.ExecuteScalarAsync<int>(
                new DapperCommandDefinition(checkCanonicalSql, new { CanonicalId = canonicalId }, transaction, cancellationToken: ct));

            if (canonicalExists == 0)
                throw new KeyNotFoundException($"Canonical record {canonicalId} not found.");

            // LOGIC: Check if variant is already associated with a canonical.
            var existing = await GetCanonicalForChunkInternalAsync(connection, variant.Id, ct, transaction);
            if (existing is not null)
            {
                throw new InvalidOperationException(
                    $"Chunk {variant.Id} is already associated with canonical record {existing.Id}.");
            }

            var now = DateTimeOffset.UtcNow;
            var variantRecordId = Guid.NewGuid();

            // LOGIC: Atomic operation - insert variant and increment merge count.
            const string insertVariantSql = @"
                INSERT INTO ""ChunkVariants"" (""Id"", ""CanonicalRecordId"", ""VariantChunkId"", ""RelationshipType"", ""SimilarityScore"", ""MergedAt"")
                VALUES (@Id, @CanonicalRecordId, @VariantChunkId, @RelationshipType, @SimilarityScore, @MergedAt)";

            await connection.ExecuteAsync(new DapperCommandDefinition(insertVariantSql, new
            {
                Id = variantRecordId,
                CanonicalRecordId = canonicalId,
                VariantChunkId = variant.Id,
                RelationshipType = (int)type,
                SimilarityScore = similarity,
                MergedAt = now
            }, transaction, cancellationToken: ct));

            const string updateMergeCountSql = @"
                UPDATE ""CanonicalRecords"" SET ""MergeCount"" = ""MergeCount"" + 1 WHERE ""Id"" = @CanonicalId";

            await connection.ExecuteAsync(new DapperCommandDefinition(
                updateMergeCountSql, new { CanonicalId = canonicalId }, transaction, cancellationToken: ct));

            await transaction.CommitAsync(ct);

            _logger.LogInformation(
                "Merged variant: VariantId={VariantRecordId}, ChunkId={VariantChunkId}, " +
                "CanonicalId={CanonicalId}, Type={RelationshipType}",
                variantRecordId,
                variant.Id,
                canonicalId,
                type);

            // LOGIC: Publish event for downstream consumers.
            await _mediator.Publish(new ChunkDeduplicatedEvent(canonicalId, variant.Id, type, similarity), ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<CanonicalRecord?> GetCanonicalForChunkAsync(Guid chunkId, CancellationToken ct = default)
    {
        if (chunkId == Guid.Empty)
            throw new ArgumentException("Chunk ID cannot be empty.", nameof(chunkId));

        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        return await GetCanonicalForChunkInternalAsync(connection, chunkId, ct);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ChunkVariant>> GetVariantsAsync(Guid canonicalId, CancellationToken ct = default)
    {
        if (canonicalId == Guid.Empty)
            throw new ArgumentException("Canonical ID cannot be empty.", nameof(canonicalId));

        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            SELECT ""Id"", ""CanonicalRecordId"", ""VariantChunkId"", ""RelationshipType"", ""SimilarityScore"", ""MergedAt""
            FROM ""ChunkVariants""
            WHERE ""CanonicalRecordId"" = @CanonicalId
            ORDER BY ""MergedAt"" DESC";

        var command = new DapperCommandDefinition(sql, new { CanonicalId = canonicalId }, cancellationToken: ct);
        var rows = await connection.QueryAsync<VariantRow>(command);

        var variants = rows.Select(r => new ChunkVariant(
            r.Id,
            r.CanonicalRecordId,
            r.VariantChunkId,
            (RelationshipType)r.RelationshipType,
            r.SimilarityScore,
            r.MergedAt)).ToList();

        _logger.LogDebug(
            "Retrieved {Count} variants for CanonicalId={CanonicalId}",
            variants.Count,
            canonicalId);

        return variants;
    }

    /// <inheritdoc/>
    public async Task PromoteVariantAsync(
        Guid canonicalId,
        Guid newCanonicalChunkId,
        string reason,
        CancellationToken ct = default)
    {
        if (canonicalId == Guid.Empty)
            throw new ArgumentException("Canonical ID cannot be empty.", nameof(canonicalId));
        if (newCanonicalChunkId == Guid.Empty)
            throw new ArgumentException("New canonical chunk ID cannot be empty.", nameof(newCanonicalChunkId));
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentNullException(nameof(reason));
        EnsureLicensed();

        _logger.LogDebug(
            "Promoting variant ChunkId={NewCanonicalChunkId} to canonical for CanonicalId={CanonicalId}",
            newCanonicalChunkId,
            canonicalId);

        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        try
        {
            // LOGIC: Get current canonical record.
            const string getCanonicalSql = @"
                SELECT ""Id"", ""CanonicalChunkId"", ""CreatedAt"", ""UpdatedAt"", ""MergeCount""
                FROM ""CanonicalRecords""
                WHERE ""Id"" = @CanonicalId";

            var canonical = await connection.QuerySingleOrDefaultAsync<CanonicalRow>(
                new DapperCommandDefinition(getCanonicalSql, new { CanonicalId = canonicalId }, transaction, cancellationToken: ct));

            if (canonical is null)
                throw new KeyNotFoundException($"Canonical record {canonicalId} not found.");

            var oldCanonicalChunkId = canonical.CanonicalChunkId;

            // LOGIC: Verify the new chunk is actually a variant of this canonical.
            const string checkVariantSql = @"
                SELECT COUNT(1) FROM ""ChunkVariants""
                WHERE ""CanonicalRecordId"" = @CanonicalId AND ""VariantChunkId"" = @NewChunkId";

            var isVariant = await connection.ExecuteScalarAsync<int>(
                new DapperCommandDefinition(checkVariantSql, new { CanonicalId = canonicalId, NewChunkId = newCanonicalChunkId }, transaction, cancellationToken: ct));

            if (isVariant == 0)
                throw new KeyNotFoundException($"Chunk {newCanonicalChunkId} is not a variant of canonical {canonicalId}.");

            // LOGIC: Get the variant's relationship info for converting old canonical to variant.
            const string getVariantSql = @"
                SELECT ""RelationshipType"", ""SimilarityScore""
                FROM ""ChunkVariants""
                WHERE ""CanonicalRecordId"" = @CanonicalId AND ""VariantChunkId"" = @NewChunkId";

            var variantInfo = await connection.QuerySingleAsync<(int RelationshipType, float SimilarityScore)>(
                new DapperCommandDefinition(getVariantSql, new { CanonicalId = canonicalId, NewChunkId = newCanonicalChunkId }, transaction, cancellationToken: ct));

            // LOGIC: Remove the promoted chunk from variants.
            const string deleteVariantSql = @"
                DELETE FROM ""ChunkVariants"" WHERE ""CanonicalRecordId"" = @CanonicalId AND ""VariantChunkId"" = @NewChunkId";

            await connection.ExecuteAsync(
                new DapperCommandDefinition(deleteVariantSql, new { CanonicalId = canonicalId, NewChunkId = newCanonicalChunkId }, transaction, cancellationToken: ct));

            // LOGIC: Insert old canonical as a variant.
            var now = DateTimeOffset.UtcNow;
            const string insertOldAsVariantSql = @"
                INSERT INTO ""ChunkVariants"" (""Id"", ""CanonicalRecordId"", ""VariantChunkId"", ""RelationshipType"", ""SimilarityScore"", ""MergedAt"")
                VALUES (@Id, @CanonicalRecordId, @VariantChunkId, @RelationshipType, @SimilarityScore, @MergedAt)";

            await connection.ExecuteAsync(new DapperCommandDefinition(insertOldAsVariantSql, new
            {
                Id = Guid.NewGuid(),
                CanonicalRecordId = canonicalId,
                VariantChunkId = oldCanonicalChunkId,
                variantInfo.RelationshipType,
                variantInfo.SimilarityScore,
                MergedAt = now
            }, transaction, cancellationToken: ct));

            // LOGIC: Update canonical record to point to new chunk.
            const string updateCanonicalSql = @"
                UPDATE ""CanonicalRecords"" SET ""CanonicalChunkId"" = @NewChunkId WHERE ""Id"" = @CanonicalId";

            await connection.ExecuteAsync(
                new DapperCommandDefinition(updateCanonicalSql, new { CanonicalId = canonicalId, NewChunkId = newCanonicalChunkId }, transaction, cancellationToken: ct));

            await transaction.CommitAsync(ct);

            _logger.LogInformation(
                "Promoted variant: CanonicalId={CanonicalId}, OldChunk={OldChunkId}, NewChunk={NewChunkId}, Reason={Reason}",
                canonicalId,
                oldCanonicalChunkId,
                newCanonicalChunkId,
                reason);

            // LOGIC: Publish event for downstream consumers.
            await _mediator.Publish(new VariantPromotedEvent(canonicalId, oldCanonicalChunkId, newCanonicalChunkId, reason), ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DetachVariantAsync(Guid variantChunkId, CancellationToken ct = default)
    {
        if (variantChunkId == Guid.Empty)
            throw new ArgumentException("Variant chunk ID cannot be empty.", nameof(variantChunkId));
        EnsureLicensed();

        _logger.LogDebug(
            "Detaching variant ChunkId={VariantChunkId}",
            variantChunkId);

        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        try
        {
            // LOGIC: Get the variant record to find canonical ID.
            const string getVariantSql = @"
                SELECT ""CanonicalRecordId"" FROM ""ChunkVariants"" WHERE ""VariantChunkId"" = @VariantChunkId";

            var canonicalId = await connection.QuerySingleOrDefaultAsync<Guid?>(
                new DapperCommandDefinition(getVariantSql, new { VariantChunkId = variantChunkId }, transaction, cancellationToken: ct));

            if (!canonicalId.HasValue)
                throw new KeyNotFoundException($"Variant record for chunk {variantChunkId} not found.");

            // LOGIC: Delete the variant record.
            const string deleteVariantSql = @"
                DELETE FROM ""ChunkVariants"" WHERE ""VariantChunkId"" = @VariantChunkId";

            await connection.ExecuteAsync(
                new DapperCommandDefinition(deleteVariantSql, new { VariantChunkId = variantChunkId }, transaction, cancellationToken: ct));

            // LOGIC: Decrement merge count.
            const string updateMergeCountSql = @"
                UPDATE ""CanonicalRecords"" SET ""MergeCount"" = ""MergeCount"" - 1 WHERE ""Id"" = @CanonicalId";

            await connection.ExecuteAsync(
                new DapperCommandDefinition(updateMergeCountSql, new { CanonicalId = canonicalId.Value }, transaction, cancellationToken: ct));

            await transaction.CommitAsync(ct);

            _logger.LogInformation(
                "Detached variant: ChunkId={VariantChunkId}, CanonicalId={CanonicalId}",
                variantChunkId,
                canonicalId.Value);

            // LOGIC: Publish event for downstream consumers.
            await _mediator.Publish(new VariantDetachedEvent(canonicalId.Value, variantChunkId), ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task RecordProvenanceAsync(Guid chunkId, ChunkProvenance provenance, CancellationToken ct = default)
    {
        if (chunkId == Guid.Empty)
            throw new ArgumentException("Chunk ID cannot be empty.", nameof(chunkId));
        ArgumentNullException.ThrowIfNull(provenance);
        EnsureLicensed();

        _logger.LogDebug(
            "Recording provenance for ChunkId={ChunkId}",
            chunkId);

        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        // LOGIC: Upsert provenance - insert or update if exists.
        const string sql = @"
            INSERT INTO ""ChunkProvenance"" (""Id"", ""ChunkId"", ""SourceDocumentId"", ""SourceLocation"", ""IngestedAt"", ""VerifiedAt"", ""VerifiedBy"")
            VALUES (@Id, @ChunkId, @SourceDocumentId, @SourceLocation, @IngestedAt, @VerifiedAt, @VerifiedBy)
            ON CONFLICT (""ChunkId"") DO UPDATE SET
                ""SourceDocumentId"" = EXCLUDED.""SourceDocumentId"",
                ""SourceLocation"" = EXCLUDED.""SourceLocation"",
                ""VerifiedAt"" = EXCLUDED.""VerifiedAt"",
                ""VerifiedBy"" = EXCLUDED.""VerifiedBy""";

        var id = provenance.Id == Guid.Empty ? Guid.NewGuid() : provenance.Id;
        await connection.ExecuteAsync(new DapperCommandDefinition(sql, new
        {
            Id = id,
            ChunkId = chunkId,
            provenance.SourceDocumentId,
            provenance.SourceLocation,
            provenance.IngestedAt,
            provenance.VerifiedAt,
            provenance.VerifiedBy
        }, cancellationToken: ct));

        _logger.LogInformation(
            "Recorded provenance: ChunkId={ChunkId}, SourceDoc={SourceDocumentId}",
            chunkId,
            provenance.SourceDocumentId);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ChunkProvenance>> GetProvenanceAsync(Guid canonicalId, CancellationToken ct = default)
    {
        if (canonicalId == Guid.Empty)
            throw new ArgumentException("Canonical ID cannot be empty.", nameof(canonicalId));

        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        // LOGIC: Get provenance for canonical chunk and all its variants.
        const string sql = @"
            SELECT p.""Id"", p.""ChunkId"", p.""SourceDocumentId"", p.""SourceLocation"", p.""IngestedAt"", p.""VerifiedAt"", p.""VerifiedBy""
            FROM ""ChunkProvenance"" p
            WHERE p.""ChunkId"" IN (
                SELECT ""CanonicalChunkId"" FROM ""CanonicalRecords"" WHERE ""Id"" = @CanonicalId
                UNION
                SELECT ""VariantChunkId"" FROM ""ChunkVariants"" WHERE ""CanonicalRecordId"" = @CanonicalId
            )
            ORDER BY p.""IngestedAt""";

        var command = new DapperCommandDefinition(sql, new { CanonicalId = canonicalId }, cancellationToken: ct);
        var rows = await connection.QueryAsync<ProvenanceRow>(command);

        var provenance = rows.Select(r => new ChunkProvenance(
            r.Id,
            r.ChunkId,
            r.SourceDocumentId,
            r.SourceLocation,
            r.IngestedAt,
            r.VerifiedAt,
            r.VerifiedBy)).ToList();

        _logger.LogDebug(
            "Retrieved {Count} provenance records for CanonicalId={CanonicalId}",
            provenance.Count,
            canonicalId);

        return provenance;
    }

    /// <inheritdoc/>
    public async Task<IDictionary<Guid, IReadOnlyList<ChunkProvenance>>> GetProvenanceBatchAsync(
        IEnumerable<Guid> canonicalIds,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(canonicalIds);
        var idList = canonicalIds.Distinct().ToList();

        if (idList.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyList<ChunkProvenance>>();
        }

        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        // LOGIC: Get provenance for multiple canonical records and their variants in one query.
        // We need to return which CanonicalRecordId each provenance belongs to.
        const string sql = @"
            SELECT
                p.""Id"",
                p.""ChunkId"",
                p.""SourceDocumentId"",
                p.""SourceLocation"",
                p.""IngestedAt"",
                p.""VerifiedAt"",
                p.""VerifiedBy"",
                cr.""Id"" AS ""CanonicalRecordId""
            FROM ""ChunkProvenance"" p
            INNER JOIN ""CanonicalRecords"" cr ON p.""ChunkId"" = cr.""CanonicalChunkId""
            WHERE cr.""Id"" IN @CanonicalIds

            UNION ALL

            SELECT
                p.""Id"",
                p.""ChunkId"",
                p.""SourceDocumentId"",
                p.""SourceLocation"",
                p.""IngestedAt"",
                p.""VerifiedAt"",
                p.""VerifiedBy"",
                cv.""CanonicalRecordId"" AS ""CanonicalRecordId""
            FROM ""ChunkProvenance"" p
            INNER JOIN ""ChunkVariants"" cv ON p.""ChunkId"" = cv.""VariantChunkId""
            WHERE cv.""CanonicalRecordId"" IN @CanonicalIds";

        var command = new DapperCommandDefinition(sql, new { CanonicalIds = idList }, cancellationToken: ct);
        var rows = await connection.QueryAsync<ProvenanceWithCanonicalIdRow>(command);

        var result = rows
            .GroupBy(r => r.CanonicalRecordId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<ChunkProvenance>)g.Select(r => new ChunkProvenance(
                    r.Id,
                    r.ChunkId,
                    r.SourceDocumentId,
                    r.SourceLocation,
                    r.IngestedAt,
                    r.VerifiedAt,
                    r.VerifiedBy))
                .OrderBy(p => p.IngestedAt)
                .ToList());

        // Ensure all requested IDs are in the dictionary
        foreach (var id in idList)
        {
            if (!result.ContainsKey(id))
            {
                result[id] = Array.Empty<ChunkProvenance>();
            }
        }

        _logger.LogDebug(
            "Retrieved provenance batch for {Count} canonical records",
            idList.Count);

        return result;
    }

    #region Private Methods

    /// <summary>
    /// Ensures the user has the required license for canonical record operations.
    /// </summary>
    /// <exception cref="FeatureNotLicensedException">Thrown when license is insufficient.</exception>
    private void EnsureLicensed()
    {
        if (!_licenseContext.IsFeatureEnabled(FeatureCodes.SemanticDeduplication))
        {
            _logger.LogWarning("Canonical record operation denied - license insufficient");
            throw new FeatureNotLicensedException(
                "Canonical record management requires Writer Pro license.",
                LicenseTier.WriterPro);
        }
    }

    /// <summary>
    /// Internal method to look up canonical record for a chunk.
    /// </summary>
    private async Task<CanonicalRecord?> GetCanonicalForChunkInternalAsync(
        IDbConnection connection,
        Guid chunkId,
        CancellationToken ct,
        IDbTransaction? transaction = null)
    {
        // LOGIC: Check if chunk is a canonical.
        const string canonicalSql = @"
            SELECT ""Id"", ""CanonicalChunkId"", ""CreatedAt"", ""UpdatedAt"", ""MergeCount""
            FROM ""CanonicalRecords""
            WHERE ""CanonicalChunkId"" = @ChunkId";

        var canonical = await connection.QuerySingleOrDefaultAsync<CanonicalRow>(
            new DapperCommandDefinition(canonicalSql, new { ChunkId = chunkId }, transaction, cancellationToken: ct));

        if (canonical is not null)
        {
            return new CanonicalRecord(
                canonical.Id,
                canonical.CanonicalChunkId,
                canonical.CreatedAt,
                canonical.UpdatedAt,
                canonical.MergeCount);
        }

        // LOGIC: Check if chunk is a variant.
        const string variantSql = @"
            SELECT c.""Id"", c.""CanonicalChunkId"", c.""CreatedAt"", c.""UpdatedAt"", c.""MergeCount""
            FROM ""CanonicalRecords"" c
            INNER JOIN ""ChunkVariants"" v ON c.""Id"" = v.""CanonicalRecordId""
            WHERE v.""VariantChunkId"" = @ChunkId";

        var fromVariant = await connection.QuerySingleOrDefaultAsync<CanonicalRow>(
            new DapperCommandDefinition(variantSql, new { ChunkId = chunkId }, transaction, cancellationToken: ct));

        if (fromVariant is not null)
        {
            return new CanonicalRecord(
                fromVariant.Id,
                fromVariant.CanonicalChunkId,
                fromVariant.CreatedAt,
                fromVariant.UpdatedAt,
                fromVariant.MergeCount);
        }

        return null;
    }

    #endregion

    #region Internal DTOs

    private record CanonicalRow(
        Guid Id,
        Guid CanonicalChunkId,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        int MergeCount);

    private record VariantRow(
        Guid Id,
        Guid CanonicalRecordId,
        Guid VariantChunkId,
        int RelationshipType,
        float SimilarityScore,
        DateTimeOffset MergedAt);

    private record ProvenanceRow(
        Guid Id,
        Guid ChunkId,
        Guid? SourceDocumentId,
        string? SourceLocation,
        DateTimeOffset IngestedAt,
        DateTimeOffset? VerifiedAt,
        string? VerifiedBy);

    private record ProvenanceWithCanonicalIdRow(
        Guid Id,
        Guid ChunkId,
        Guid? SourceDocumentId,
        string? SourceLocation,
        DateTimeOffset IngestedAt,
        DateTimeOffset? VerifiedAt,
        string? VerifiedBy,
        Guid CanonicalRecordId);

    #endregion
}
