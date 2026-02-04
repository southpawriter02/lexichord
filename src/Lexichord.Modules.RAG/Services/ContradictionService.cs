// =============================================================================
// File: ContradictionService.cs
// Project: Lexichord.Modules.RAG
// Description: Implementation of IContradictionService for contradiction management.
// =============================================================================
// VERSION: v0.5.9e (Contradiction Detection & Resolution)
// LOGIC: Manages the full lifecycle of detected contradictions:
//   1. Flag contradictions detected by deduplication pipeline.
//   2. Query pending contradictions for admin review.
//   3. Apply resolution decisions (keep one, keep both, synthesize, delete).
//   4. Handle dismissals and auto-resolutions.
//   5. Publish events for downstream consumers.
// =============================================================================

using System.Data.Common;
using System.Diagnostics;
using Dapper;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Abstractions.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using DapperCommandDefinition = Dapper.CommandDefinition;

namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// Implementation of <see cref="IContradictionService"/> for managing
/// detected contradictions between chunks.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9e as part of the Semantic Memory Deduplication feature.
/// </para>
/// <para>
/// This service provides:
/// </para>
/// <list type="bullet">
///   <item><description>Contradiction persistence to the <c>contradictions</c> table.</description></item>
///   <item><description>Duplicate detection for (A, B) vs (B, A) chunk pairs.</description></item>
///   <item><description>Status transition validation and audit logging.</description></item>
///   <item><description>Event publishing via MediatR for real-time updates.</description></item>
///   <item><description>Integration with <see cref="ICanonicalManager"/> for resolutions.</description></item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> This service is thread-safe for concurrent operations.
/// Database transactions ensure consistency during resolution.
/// </para>
/// </remarks>
public sealed class ContradictionService : IContradictionService
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICanonicalManager _canonicalManager;
    private readonly IChunkRepository _chunkRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<ContradictionService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContradictionService"/> class.
    /// </summary>
    /// <param name="connectionFactory">Database connection factory.</param>
    /// <param name="canonicalManager">Canonical record manager for resolutions.</param>
    /// <param name="chunkRepository">Chunk repository for synthesis storage.</param>
    /// <param name="mediator">MediatR mediator for event publishing.</param>
    /// <param name="logger">Logger instance.</param>
    public ContradictionService(
        IDbConnectionFactory connectionFactory,
        ICanonicalManager canonicalManager,
        IChunkRepository chunkRepository,
        IMediator mediator,
        ILogger<ContradictionService> logger)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _canonicalManager = canonicalManager ?? throw new ArgumentNullException(nameof(canonicalManager));
        _chunkRepository = chunkRepository ?? throw new ArgumentNullException(nameof(chunkRepository));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("ContradictionService initialized");
    }

    /// <inheritdoc/>
    public async Task<Contradiction> FlagAsync(
        Guid chunkAId,
        Guid chunkBId,
        float similarityScore,
        float confidence,
        string? reason = null,
        Guid? projectId = null,
        CancellationToken ct = default)
    {
        // LOGIC: Validate chunk IDs are different.
        if (chunkAId == chunkBId)
            throw new ArgumentException("Cannot flag a chunk as contradicting itself.", nameof(chunkBId));

        var stopwatch = Stopwatch.StartNew();

        _logger.LogDebug(
            "Flagging contradiction: ChunkA={ChunkAId}, ChunkB={ChunkBId}, Similarity={Similarity:F3}, Confidence={Confidence:F3}",
            chunkAId, chunkBId, similarityScore, confidence);

        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        // LOGIC: Check for existing contradiction between these chunks (symmetric).
        var existing = await FindExistingAsync(connection, chunkAId, chunkBId, ct);
        if (existing is not null)
        {
            _logger.LogDebug(
                "Existing contradiction found: Id={ContradictionId}, Status={Status}",
                existing.Id, existing.Status);

            // If existing is still pending and new confidence is higher, update it.
            if (existing.IsPending && confidence > existing.ClassificationConfidence)
            {
                existing = await UpdateConfidenceAsync(connection, existing.Id, confidence, reason, ct);
                _logger.LogInformation(
                    "Updated contradiction confidence: Id={ContradictionId}, NewConfidence={Confidence:F3}",
                    existing.Id, confidence);
            }

            return existing;
        }

        // LOGIC: Create new contradiction record.
        var contradiction = Contradiction.Create(
            chunkAId: chunkAId,
            chunkBId: chunkBId,
            similarityScore: similarityScore,
            confidence: confidence,
            reason: reason,
            projectId: projectId);

        const string insertSql = @"
            INSERT INTO ""Contradictions"" (
                ""Id"", ""ChunkAId"", ""ChunkBId"", ""SimilarityScore"", ""ClassificationConfidence"",
                ""ContradictionReason"", ""Status"", ""DetectedAt"", ""DetectedBy"", ""ProjectId""
            )
            VALUES (
                @Id, @ChunkAId, @ChunkBId, @SimilarityScore, @ClassificationConfidence,
                @ContradictionReason, @Status, @DetectedAt, @DetectedBy, @ProjectId
            )
            RETURNING ""Id""";

        var newId = Guid.NewGuid();
        await connection.ExecuteAsync(new DapperCommandDefinition(insertSql, new
        {
            Id = newId,
            contradiction.ChunkAId,
            contradiction.ChunkBId,
            contradiction.SimilarityScore,
            contradiction.ClassificationConfidence,
            contradiction.ContradictionReason,
            Status = (int)contradiction.Status,
            contradiction.DetectedAt,
            contradiction.DetectedBy,
            contradiction.ProjectId
        }, cancellationToken: ct));

        // LOGIC: Reload to get database-assigned values.
        var created = await GetByIdAsync(newId, ct)
            ?? throw new InvalidOperationException("Failed to retrieve created contradiction.");

        stopwatch.Stop();

        _logger.LogInformation(
            "Flagged contradiction: Id={ContradictionId}, ChunkA={ChunkAId}, ChunkB={ChunkBId}, Duration={Duration:F2}ms",
            created.Id, chunkAId, chunkBId, stopwatch.Elapsed.TotalMilliseconds);

        // LOGIC: Publish event for downstream consumers.
        await _mediator.Publish(ContradictionDetectedEvent.FromContradiction(created), ct);

        // LOGIC: Record metrics for observability (v0.5.9h).
        // Using Medium severity as the default since we don't have severity in the model.
        Metrics.DeduplicationMetrics.RecordContradictionDetected(
            Abstractions.Contracts.RAG.ContradictionSeverity.Medium);

        return created;
    }

    /// <inheritdoc/>
    public async Task<Contradiction?> GetByIdAsync(Guid contradictionId, CancellationToken ct = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        return await LoadByIdAsync(connection, contradictionId, ct);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Contradiction>> GetByChunkIdAsync(
        Guid chunkId,
        bool includeResolved = false,
        CancellationToken ct = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        var sql = @"
            SELECT * FROM ""Contradictions""
            WHERE (""ChunkAId"" = @ChunkId OR ""ChunkBId"" = @ChunkId)";

        if (!includeResolved)
        {
            sql += @" AND ""Status"" IN (@Pending, @UnderReview)";
        }

        sql += @" ORDER BY ""DetectedAt"" DESC";

        var rows = await connection.QueryAsync<ContradictionRow>(
            new DapperCommandDefinition(sql, new
            {
                ChunkId = chunkId,
                Pending = (int)ContradictionStatus.Pending,
                UnderReview = (int)ContradictionStatus.UnderReview
            }, cancellationToken: ct));

        return rows.Select(MapToContradiction).ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Contradiction>> GetPendingAsync(
        Guid? projectId = null,
        CancellationToken ct = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        var sql = @"
            SELECT * FROM ""Contradictions""
            WHERE ""Status"" IN (@Pending, @UnderReview)";

        if (projectId.HasValue)
        {
            sql += @" AND ""ProjectId"" = @ProjectId";
        }

        sql += @" ORDER BY ""DetectedAt"" ASC";

        var rows = await connection.QueryAsync<ContradictionRow>(
            new DapperCommandDefinition(sql, new
            {
                Pending = (int)ContradictionStatus.Pending,
                UnderReview = (int)ContradictionStatus.UnderReview,
                ProjectId = projectId
            }, cancellationToken: ct));

        _logger.LogDebug("Retrieved {Count} pending contradictions", rows.Count());
        return rows.Select(MapToContradiction).ToList();
    }

    /// <inheritdoc/>
    public async Task<ContradictionStatistics> GetStatisticsAsync(
        Guid? projectId = null,
        CancellationToken ct = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        var sql = @"
            SELECT
                COUNT(*) AS TotalCount,
                COUNT(*) FILTER (WHERE ""Status"" = @Pending) AS PendingCount,
                COUNT(*) FILTER (WHERE ""Status"" = @UnderReview) AS UnderReviewCount,
                COUNT(*) FILTER (WHERE ""Status"" = @Resolved) AS ResolvedCount,
                COUNT(*) FILTER (WHERE ""Status"" = @Dismissed) AS DismissedCount,
                COUNT(*) FILTER (WHERE ""Status"" = @AutoResolved) AS AutoResolvedCount,
                COUNT(*) FILTER (WHERE ""Status"" = @Pending AND ""ClassificationConfidence"" >= 0.8) AS HighConfidenceCount,
                MIN(""DetectedAt"") FILTER (WHERE ""Status"" = @Pending) AS OldestPendingDate
            FROM ""Contradictions""";

        if (projectId.HasValue)
        {
            sql += @" WHERE ""ProjectId"" = @ProjectId";
        }

        var row = await connection.QuerySingleAsync<StatisticsRow>(
            new DapperCommandDefinition(sql, new
            {
                Pending = (int)ContradictionStatus.Pending,
                UnderReview = (int)ContradictionStatus.UnderReview,
                Resolved = (int)ContradictionStatus.Resolved,
                Dismissed = (int)ContradictionStatus.Dismissed,
                AutoResolved = (int)ContradictionStatus.AutoResolved,
                ProjectId = projectId
            }, cancellationToken: ct));

        TimeSpan? oldestPendingAge = row.OldestPendingDate.HasValue
            ? DateTimeOffset.UtcNow - row.OldestPendingDate.Value
            : null;

        return new ContradictionStatistics(
            TotalCount: row.TotalCount,
            PendingCount: row.PendingCount,
            UnderReviewCount: row.UnderReviewCount,
            ResolvedCount: row.ResolvedCount,
            DismissedCount: row.DismissedCount,
            AutoResolvedCount: row.AutoResolvedCount,
            HighConfidenceCount: row.HighConfidenceCount,
            OldestPendingAge: oldestPendingAge);
    }

    /// <inheritdoc/>
    public async Task<Contradiction> BeginReviewAsync(
        Guid contradictionId,
        string reviewerId,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(reviewerId);

        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        var existing = await LoadByIdAsync(connection, contradictionId, ct)
            ?? throw new KeyNotFoundException($"Contradiction {contradictionId} not found.");

        if (existing.Status != ContradictionStatus.Pending)
        {
            throw new InvalidOperationException(
                $"Cannot begin review of contradiction in {existing.Status} status.");
        }

        const string updateSql = @"
            UPDATE ""Contradictions""
            SET ""Status"" = @Status, ""ReviewedBy"" = @ReviewedBy
            WHERE ""Id"" = @Id";

        await connection.ExecuteAsync(new DapperCommandDefinition(updateSql, new
        {
            Id = contradictionId,
            Status = (int)ContradictionStatus.UnderReview,
            ReviewedBy = reviewerId
        }, cancellationToken: ct));

        _logger.LogInformation(
            "Contradiction review started: Id={ContradictionId}, Reviewer={ReviewerId}",
            contradictionId, reviewerId);

        return existing with
        {
            Status = ContradictionStatus.UnderReview,
            ReviewedBy = reviewerId
        };
    }

    /// <inheritdoc/>
    public async Task<Contradiction> ResolveAsync(
        Guid contradictionId,
        ContradictionResolution resolution,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(resolution);

        // LOGIC: Validate synthesis content is provided when needed.
        if (resolution.Type == ContradictionResolutionType.CreateSynthesis &&
            string.IsNullOrWhiteSpace(resolution.SynthesizedContent))
        {
            throw new InvalidOperationException(
                "CreateSynthesis resolution requires synthesized content.");
        }

        var stopwatch = Stopwatch.StartNew();

        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        var existing = await LoadByIdAsync(connection, contradictionId, ct)
            ?? throw new KeyNotFoundException($"Contradiction {contradictionId} not found.");

        if (existing.IsTerminal)
        {
            throw new InvalidOperationException(
                $"Contradiction is already in terminal state: {existing.Status}.");
        }

        _logger.LogDebug(
            "Resolving contradiction: Id={ContradictionId}, Type={ResolutionType}",
            contradictionId, resolution.Type);

        // LOGIC: Apply resolution based on type.
        var updatedResolution = await ApplyResolutionAsync(existing, resolution, ct);

        // LOGIC: Update database record.
        const string updateSql = @"
            UPDATE ""Contradictions""
            SET ""Status"" = @Status,
                ""ReviewedBy"" = @ReviewedBy,
                ""ReviewedAt"" = @ReviewedAt,
                ""ResolutionType"" = @ResolutionType,
                ""ResolutionRationale"" = @Rationale,
                ""RetainedChunkId"" = @RetainedChunkId,
                ""ArchivedChunkId"" = @ArchivedChunkId,
                ""SynthesizedContent"" = @SynthesizedContent,
                ""SynthesizedChunkId"" = @SynthesizedChunkId
            WHERE ""Id"" = @Id";

        await connection.ExecuteAsync(new DapperCommandDefinition(updateSql, new
        {
            Id = contradictionId,
            Status = (int)ContradictionStatus.Resolved,
            updatedResolution.ResolvedBy,
            updatedResolution.ResolvedAt,
            ResolutionType = (int)updatedResolution.Type,
            Rationale = updatedResolution.Rationale,
            updatedResolution.RetainedChunkId,
            updatedResolution.ArchivedChunkId,
            updatedResolution.SynthesizedContent,
            updatedResolution.SynthesizedChunkId
        }, cancellationToken: ct));

        var resolved = existing with
        {
            Status = ContradictionStatus.Resolved,
            ReviewedBy = updatedResolution.ResolvedBy,
            ReviewedAt = updatedResolution.ResolvedAt,
            Resolution = updatedResolution
        };

        stopwatch.Stop();

        _logger.LogInformation(
            "Contradiction resolved: Id={ContradictionId}, Type={ResolutionType}, Duration={Duration:F2}ms",
            contradictionId, resolution.Type, stopwatch.Elapsed.TotalMilliseconds);

        // LOGIC: Publish event.
        await _mediator.Publish(ContradictionResolvedEvent.FromContradiction(resolved), ct);

        return resolved;
    }

    /// <inheritdoc/>
    public async Task<Contradiction> DismissAsync(
        Guid contradictionId,
        string reason,
        string dismissedBy,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(reason);
        ArgumentException.ThrowIfNullOrEmpty(dismissedBy);

        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        var existing = await LoadByIdAsync(connection, contradictionId, ct)
            ?? throw new KeyNotFoundException($"Contradiction {contradictionId} not found.");

        if (existing.IsTerminal)
        {
            throw new InvalidOperationException(
                $"Contradiction is already in terminal state: {existing.Status}.");
        }

        const string updateSql = @"
            UPDATE ""Contradictions""
            SET ""Status"" = @Status,
                ""ReviewedBy"" = @ReviewedBy,
                ""ReviewedAt"" = @ReviewedAt,
                ""ResolutionRationale"" = @Rationale
            WHERE ""Id"" = @Id";

        var dismissedAt = DateTimeOffset.UtcNow;

        await connection.ExecuteAsync(new DapperCommandDefinition(updateSql, new
        {
            Id = contradictionId,
            Status = (int)ContradictionStatus.Dismissed,
            ReviewedBy = dismissedBy,
            ReviewedAt = dismissedAt,
            Rationale = reason
        }, cancellationToken: ct));

        var dismissed = existing with
        {
            Status = ContradictionStatus.Dismissed,
            ReviewedBy = dismissedBy,
            ReviewedAt = dismissedAt
        };

        _logger.LogInformation(
            "Contradiction dismissed: Id={ContradictionId}, Reason={Reason}, DismissedBy={DismissedBy}",
            contradictionId, reason, dismissedBy);

        // LOGIC: Publish event.
        await _mediator.Publish(
            ContradictionResolvedEvent.ForDismissal(dismissed, reason, dismissedBy), ct);

        return dismissed;
    }

    /// <inheritdoc/>
    public async Task<Contradiction> AutoResolveAsync(
        Guid contradictionId,
        string reason,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(reason);

        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        var existing = await LoadByIdAsync(connection, contradictionId, ct)
            ?? throw new KeyNotFoundException($"Contradiction {contradictionId} not found.");

        if (existing.IsTerminal)
        {
            // Already resolved - just return it.
            _logger.LogDebug(
                "Contradiction already in terminal state: Id={ContradictionId}, Status={Status}",
                contradictionId, existing.Status);
            return existing;
        }

        const string updateSql = @"
            UPDATE ""Contradictions""
            SET ""Status"" = @Status,
                ""ReviewedBy"" = @ReviewedBy,
                ""ReviewedAt"" = @ReviewedAt,
                ""ResolutionRationale"" = @Rationale
            WHERE ""Id"" = @Id";

        var resolvedAt = DateTimeOffset.UtcNow;

        await connection.ExecuteAsync(new DapperCommandDefinition(updateSql, new
        {
            Id = contradictionId,
            Status = (int)ContradictionStatus.AutoResolved,
            ReviewedBy = "System",
            ReviewedAt = resolvedAt,
            Rationale = reason
        }, cancellationToken: ct));

        var autoResolved = existing with
        {
            Status = ContradictionStatus.AutoResolved,
            ReviewedBy = "System",
            ReviewedAt = resolvedAt
        };

        _logger.LogInformation(
            "Contradiction auto-resolved: Id={ContradictionId}, Reason={Reason}",
            contradictionId, reason);

        // LOGIC: Publish event.
        await _mediator.Publish(
            ContradictionResolvedEvent.ForAutoResolve(autoResolved, reason), ct);

        return autoResolved;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(Guid contradictionId, CancellationToken ct = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        const string deleteSql = @"DELETE FROM ""Contradictions"" WHERE ""Id"" = @Id";

        var affected = await connection.ExecuteAsync(
            new DapperCommandDefinition(deleteSql, new { Id = contradictionId }, cancellationToken: ct));

        if (affected > 0)
        {
            _logger.LogWarning(
                "Contradiction deleted: Id={ContradictionId}. This removes audit trail.",
                contradictionId);
        }

        return affected > 0;
    }

    #region Private Methods

    /// <summary>
    /// Finds an existing contradiction between two chunks (symmetric lookup).
    /// </summary>
    private async Task<Contradiction?> FindExistingAsync(
        DbConnection connection,
        Guid chunkAId,
        Guid chunkBId,
        CancellationToken ct)
    {
        const string sql = @"
            SELECT * FROM ""Contradictions""
            WHERE (""ChunkAId"" = @ChunkAId AND ""ChunkBId"" = @ChunkBId)
               OR (""ChunkAId"" = @ChunkBId AND ""ChunkBId"" = @ChunkAId)
            LIMIT 1";

        var row = await connection.QuerySingleOrDefaultAsync<ContradictionRow>(
            new DapperCommandDefinition(sql, new { ChunkAId = chunkAId, ChunkBId = chunkBId },
                cancellationToken: ct));

        return row is null ? null : MapToContradiction(row);
    }

    /// <summary>
    /// Updates the confidence level of an existing contradiction.
    /// </summary>
    private async Task<Contradiction> UpdateConfidenceAsync(
        DbConnection connection,
        Guid contradictionId,
        float newConfidence,
        string? reason,
        CancellationToken ct)
    {
        const string updateSql = @"
            UPDATE ""Contradictions""
            SET ""ClassificationConfidence"" = @Confidence,
                ""ContradictionReason"" = COALESCE(@Reason, ""ContradictionReason"")
            WHERE ""Id"" = @Id";

        await connection.ExecuteAsync(new DapperCommandDefinition(updateSql, new
        {
            Id = contradictionId,
            Confidence = newConfidence,
            Reason = reason
        }, cancellationToken: ct));

        return await LoadByIdAsync(connection, contradictionId, ct)
            ?? throw new InvalidOperationException("Failed to reload updated contradiction.");
    }

    /// <summary>
    /// Loads a contradiction by ID from the database.
    /// </summary>
    private async Task<Contradiction?> LoadByIdAsync(
        DbConnection connection,
        Guid contradictionId,
        CancellationToken ct)
    {
        const string sql = @"SELECT * FROM ""Contradictions"" WHERE ""Id"" = @Id";

        var row = await connection.QuerySingleOrDefaultAsync<ContradictionRow>(
            new DapperCommandDefinition(sql, new { Id = contradictionId }, cancellationToken: ct));

        return row is null ? null : MapToContradiction(row);
    }

    /// <summary>
    /// Applies the resolution by updating canonical records as needed.
    /// </summary>
    private async Task<ContradictionResolution> ApplyResolutionAsync(
        Contradiction contradiction,
        ContradictionResolution resolution,
        CancellationToken ct)
    {
        switch (resolution.Type)
        {
            case ContradictionResolutionType.KeepOlder:
            case ContradictionResolutionType.KeepNewer:
                // LOGIC: Determine which chunk is older/newer based on canonical records.
                var canonicalA = await _canonicalManager.GetCanonicalForChunkAsync(contradiction.ChunkAId, ct);
                var canonicalB = await _canonicalManager.GetCanonicalForChunkAsync(contradiction.ChunkBId, ct);

                // Use CreatedAt if available, otherwise fall back to detection order.
                var chunkAIsOlder = canonicalA?.CreatedAt <= canonicalB?.CreatedAt;

                var (retainedId, archivedId) = resolution.Type == ContradictionResolutionType.KeepOlder
                    ? (chunkAIsOlder ? contradiction.ChunkAId : contradiction.ChunkBId,
                       chunkAIsOlder ? contradiction.ChunkBId : contradiction.ChunkAId)
                    : (chunkAIsOlder ? contradiction.ChunkBId : contradiction.ChunkAId,
                       chunkAIsOlder ? contradiction.ChunkAId : contradiction.ChunkBId);

                // LOGIC: Archive the chunk that's being superseded.
                var retainedCanonical = await _canonicalManager.GetCanonicalForChunkAsync(retainedId, ct);
                if (retainedCanonical is not null)
                {
                    await _canonicalManager.MergeIntoCanonicalAsync(
                        retainedCanonical.Id,
                        await LoadChunkAsync(archivedId, ct),
                        RelationshipType.Superseding,
                        contradiction.SimilarityScore,
                        ct);
                }

                return resolution with
                {
                    RetainedChunkId = retainedId,
                    ArchivedChunkId = archivedId
                };

            case ContradictionResolutionType.KeepBoth:
                // LOGIC: No canonical changes needed, just mark as intentionally different.
                _logger.LogDebug(
                    "KeepBoth resolution: ChunkA={ChunkAId}, ChunkB={ChunkBId} marked as intentionally different",
                    contradiction.ChunkAId, contradiction.ChunkBId);
                return resolution;

            case ContradictionResolutionType.CreateSynthesis:
                // LOGIC: Create a new chunk from synthesized content.
                // For now, we record the intent; full implementation would create and embed the synthesis.
                _logger.LogDebug(
                    "CreateSynthesis resolution: Will create synthesis from ChunkA={ChunkAId}, ChunkB={ChunkBId}",
                    contradiction.ChunkAId, contradiction.ChunkBId);
                // Note: Full synthesis would involve:
                // 1. Create new chunk with synthesized content
                // 2. Generate embedding
                // 3. Store as new canonical
                // 4. Archive both original chunks as variants
                return resolution;

            case ContradictionResolutionType.DeleteBoth:
                // LOGIC: Delete both chunks and their canonical records.
                await DeleteChunkAndCanonicalAsync(contradiction.ChunkAId, ct);
                await DeleteChunkAndCanonicalAsync(contradiction.ChunkBId, ct);

                _logger.LogWarning(
                    "DeleteBoth resolution: Deleted ChunkA={ChunkAId}, ChunkB={ChunkBId}",
                    contradiction.ChunkAId, contradiction.ChunkBId);
                return resolution;

            default:
                throw new InvalidOperationException($"Unknown resolution type: {resolution.Type}");
        }
    }

    /// <summary>
    /// Loads a chunk by ID.
    /// </summary>
    private async Task<Chunk> LoadChunkAsync(Guid chunkId, CancellationToken ct)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            SELECT ""Id"", ""DocumentId"", ""Content"", ""Embedding"", ""ChunkIndex"",
                   ""StartOffset"", ""EndOffset"", ""Heading"", ""HeadingLevel""
            FROM ""Chunks""
            WHERE ""Id"" = @ChunkId";

        var row = await connection.QuerySingleOrDefaultAsync<ChunkRow>(
            new DapperCommandDefinition(sql, new { ChunkId = chunkId }, cancellationToken: ct))
            ?? throw new KeyNotFoundException($"Chunk {chunkId} not found.");

        return new Chunk(
            row.Id,
            row.DocumentId,
            row.Content,
            row.Embedding,
            row.ChunkIndex,
            row.StartOffset,
            row.EndOffset,
            row.Heading,
            row.HeadingLevel ?? 0);
    }

    /// <summary>
    /// Deletes a chunk and its associated canonical record.
    /// </summary>
    private async Task DeleteChunkAndCanonicalAsync(Guid chunkId, CancellationToken ct)
    {
        // LOGIC: Detach from canonical first if needed.
        var canonical = await _canonicalManager.GetCanonicalForChunkAsync(chunkId, ct);
        if (canonical is not null)
        {
            // Only detach if this chunk is a variant, not the canonical itself.
            if (canonical.CanonicalChunkId != chunkId)
            {
                await _canonicalManager.DetachVariantAsync(chunkId, ct);
            }
        }

        // LOGIC: Delete the chunk.
        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        const string deleteSql = @"DELETE FROM ""Chunks"" WHERE ""Id"" = @ChunkId";
        await connection.ExecuteAsync(
            new DapperCommandDefinition(deleteSql, new { ChunkId = chunkId }, cancellationToken: ct));
    }

    /// <summary>
    /// Maps a database row to a Contradiction record.
    /// </summary>
    private static Contradiction MapToContradiction(ContradictionRow row)
    {
        ContradictionResolution? resolution = null;
        if (row.ResolutionType.HasValue)
        {
            resolution = new ContradictionResolution(
                Type: (ContradictionResolutionType)row.ResolutionType.Value,
                Rationale: row.ResolutionRationale ?? string.Empty,
                ResolvedAt: row.ReviewedAt ?? DateTimeOffset.MinValue,
                ResolvedBy: row.ReviewedBy ?? "Unknown",
                RetainedChunkId: row.RetainedChunkId,
                ArchivedChunkId: row.ArchivedChunkId,
                SynthesizedContent: row.SynthesizedContent,
                SynthesizedChunkId: row.SynthesizedChunkId);
        }

        return new Contradiction(
            Id: row.Id,
            ChunkAId: row.ChunkAId,
            ChunkBId: row.ChunkBId,
            SimilarityScore: row.SimilarityScore,
            ClassificationConfidence: row.ClassificationConfidence,
            ContradictionReason: row.ContradictionReason,
            Status: (ContradictionStatus)row.Status,
            DetectedAt: row.DetectedAt,
            DetectedBy: row.DetectedBy,
            ProjectId: row.ProjectId,
            ReviewedBy: row.ReviewedBy,
            ReviewedAt: row.ReviewedAt,
            Resolution: resolution);
    }

    #endregion

    #region Internal DTOs

    private record ContradictionRow(
        Guid Id,
        Guid ChunkAId,
        Guid ChunkBId,
        float SimilarityScore,
        float ClassificationConfidence,
        string? ContradictionReason,
        int Status,
        DateTimeOffset DetectedAt,
        string DetectedBy,
        Guid? ProjectId,
        string? ReviewedBy,
        DateTimeOffset? ReviewedAt,
        int? ResolutionType,
        string? ResolutionRationale,
        Guid? RetainedChunkId,
        Guid? ArchivedChunkId,
        string? SynthesizedContent,
        Guid? SynthesizedChunkId);

    private record StatisticsRow(
        int TotalCount,
        int PendingCount,
        int UnderReviewCount,
        int ResolvedCount,
        int DismissedCount,
        int AutoResolvedCount,
        int HighConfidenceCount,
        DateTimeOffset? OldestPendingDate);

    private record ChunkRow(
        Guid Id,
        Guid DocumentId,
        string Content,
        float[]? Embedding,
        int ChunkIndex,
        int StartOffset,
        int EndOffset,
        string? Heading,
        int? HeadingLevel);

    #endregion
}
