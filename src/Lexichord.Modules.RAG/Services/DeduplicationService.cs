// =============================================================================
// File: DeduplicationService.cs
// Project: Lexichord.Modules.RAG
// Description: Implementation of IDeduplicationService for chunk deduplication.
// =============================================================================
// VERSION: v0.5.9d (Deduplication Service)
// LOGIC: Orchestrates the full deduplication pipeline:
//   1. License check → bypass if unlicensed
//   2. Find similar chunks via ISimilarityDetector
//   3. Classify relationships via IRelationshipClassifier
//   4. Route by classification type (merge, link, flag, queue, store as new)
// =============================================================================

using System.Data.Common;
using System.Diagnostics;
using System.Text.Json;
using Dapper;
using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Microsoft.Extensions.Logging;
using DapperCommandDefinition = Dapper.CommandDefinition;

namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// Implementation of <see cref="IDeduplicationService"/> that orchestrates
/// the full deduplication pipeline during chunk ingestion.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9d as part of the Semantic Memory Deduplication feature.
/// </para>
/// <para>
/// This service coordinates:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="ISimilarityDetector"/> (v0.5.9a): Finds similar chunks.</description></item>
///   <item><description><see cref="IRelationshipClassifier"/> (v0.5.9b): Classifies relationships.</description></item>
///   <item><description><see cref="ICanonicalManager"/> (v0.5.9c): Manages canonical records.</description></item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> This service is thread-safe for concurrent chunk processing.
/// </para>
/// </remarks>
public sealed class DeduplicationService : IDeduplicationService
{
    private readonly ISimilarityDetector _similarityDetector;
    private readonly IRelationshipClassifier _relationshipClassifier;
    private readonly ICanonicalManager _canonicalManager;
    private readonly IContradictionService _contradictionService;
    private readonly IChunkRepository _chunkRepository;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILicenseContext _licenseContext;
    private readonly ILogger<DeduplicationService> _logger;

    /// <summary>
    /// Confidence threshold below which manual review is triggered.
    /// </summary>
    private const float ManualReviewConfidenceThreshold = 0.7f;

    /// <summary>
    /// Confidence threshold for auto-merge operations.
    /// </summary>
    private const float AutoMergeConfidenceThreshold = 0.9f;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeduplicationService"/> class.
    /// </summary>
    public DeduplicationService(
        ISimilarityDetector similarityDetector,
        IRelationshipClassifier relationshipClassifier,
        ICanonicalManager canonicalManager,
        IContradictionService contradictionService,
        IChunkRepository chunkRepository,
        IDbConnectionFactory connectionFactory,
        ILicenseContext licenseContext,
        ILogger<DeduplicationService> logger)
    {
        _similarityDetector = similarityDetector ?? throw new ArgumentNullException(nameof(similarityDetector));
        _relationshipClassifier = relationshipClassifier ?? throw new ArgumentNullException(nameof(relationshipClassifier));
        _canonicalManager = canonicalManager ?? throw new ArgumentNullException(nameof(canonicalManager));
        _contradictionService = contradictionService ?? throw new ArgumentNullException(nameof(contradictionService));
        _chunkRepository = chunkRepository ?? throw new ArgumentNullException(nameof(chunkRepository));
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("DeduplicationService initialized");
    }

    /// <inheritdoc/>
    public async Task<DeduplicationResult> ProcessChunkAsync(
        Chunk newChunk,
        DeduplicationOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(newChunk);
        if (newChunk.Embedding is null)
            throw new ArgumentException("Chunk must have an embedding for deduplication.", nameof(newChunk));

        options ??= DeduplicationOptions.Default;
        var stopwatch = Stopwatch.StartNew();

        // LOGIC: License check - bypass deduplication for unlicensed users.
        if (!_licenseContext.IsFeatureEnabled(FeatureCodes.DeduplicationService))
        {
            _logger.LogDebug(
                "Deduplication bypassed for ChunkId={ChunkId} - license insufficient",
                newChunk.Id);
            return DeduplicationResult.StoredAsNew(newChunk.Id, stopwatch.Elapsed);
        }

        _logger.LogDebug(
            "Processing chunk for deduplication: ChunkId={ChunkId}, Threshold={Threshold}",
            newChunk.Id,
            options.SimilarityThreshold);

        try
        {
            // LOGIC: Step 1 - Find similar chunks.
            var similarResults = await _similarityDetector.FindSimilarAsync(
                newChunk,
                new SimilarityDetectorOptions
                {
                    SimilarityThreshold = options.SimilarityThreshold,
                    MaxResultsPerChunk = options.MaxCandidates
                },
                ct);

            if (similarResults.Count() == 0)
            {
                // No duplicates found - create canonical and store as new.
                await _canonicalManager.CreateCanonicalAsync(newChunk, ct);

                _logger.LogInformation(
                    "Stored as new canonical: ChunkId={ChunkId}, Duration={Duration:F2}ms",
                    newChunk.Id,
                    stopwatch.Elapsed.TotalMilliseconds);

                return DeduplicationResult.StoredAsNew(newChunk.Id, stopwatch.Elapsed);
            }

            // LOGIC: Step 2 - Load chunks and classify relationships.
            var candidates = await ClassifyCandidatesAsync(newChunk, similarResults, options, ct);

            // LOGIC: Step 3 - Find the best candidate.
            var bestCandidate = candidates
                .OrderByDescending(c => c.SimilarityScore)
                .ThenByDescending(c => c.Classification.Confidence)
                .First();

            // LOGIC: Step 4 - Route based on classification.
            return await RouteByClassificationAsync(newChunk, bestCandidate, candidates, options, stopwatch, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing chunk for deduplication: ChunkId={ChunkId}",
                newChunk.Id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DuplicateCandidate>> FindDuplicatesAsync(
        Chunk chunk,
        float similarityThreshold = 0.85f,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        if (chunk.Embedding is null)
            throw new ArgumentException("Chunk must have an embedding.", nameof(chunk));

        // LOGIC: License check - return empty if unlicensed.
        if (!_licenseContext.IsFeatureEnabled(FeatureCodes.DeduplicationService))
        {
            _logger.LogDebug("FindDuplicates bypassed - license insufficient");
            return [];
        }

        var similarResults = await _similarityDetector.FindSimilarAsync(
            chunk,
            new SimilarityDetectorOptions
            {
                SimilarityThreshold = similarityThreshold,
                MaxResultsPerChunk = 10
            },
            ct);

        if (similarResults.Count() == 0)
            return [];

        var options = new DeduplicationOptions { SimilarityThreshold = similarityThreshold };
        return await ClassifyCandidatesAsync(chunk, similarResults, options, ct);
    }

    /// <inheritdoc/>
    public async Task ProcessManualDecisionAsync(
        ManualMergeDecision decision,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(decision);

        _logger.LogDebug(
            "Processing manual decision: ReviewId={ReviewId}, Decision={Decision}",
            decision.ReviewId,
            decision.Decision);

        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        // LOGIC: Get the pending review.
        const string getPendingSql = @"
            SELECT ""Id"", ""NewChunkId"", ""Candidates"", ""QueuedAt"", ""AutoClassificationReason""
            FROM ""PendingReviews""
            WHERE ""Id"" = @ReviewId AND ""ReviewedAt"" IS NULL";

        var row = await connection.QuerySingleOrDefaultAsync<PendingReviewRow>(
            new DapperCommandDefinition(getPendingSql, new { decision.ReviewId }, cancellationToken: ct));

        if (row is null)
            throw new KeyNotFoundException($"Pending review {decision.ReviewId} not found or already processed.");

        // LOGIC: Load the chunk via direct DB query.
        var chunk = await LoadChunkByIdAsync(connection, row.NewChunkId, ct)
            ?? throw new KeyNotFoundException($"Chunk {row.NewChunkId} not found.");

        // LOGIC: Process based on decision type.
        switch (decision.Decision)
        {
            case ManualDecisionType.Merge:
                if (!decision.TargetCanonicalId.HasValue)
                    throw new InvalidOperationException("Merge decision requires a target canonical ID.");

                var existingCanonical = await _canonicalManager.GetCanonicalForChunkAsync(decision.TargetCanonicalId.Value, ct)
                    ?? throw new KeyNotFoundException($"Target canonical {decision.TargetCanonicalId} not found.");

                await _canonicalManager.MergeIntoCanonicalAsync(
                    existingCanonical.Id,
                    chunk,
                    RelationshipType.Equivalent,
                    0.0f, // Manual decision - no auto-calculated similarity
                    ct);
                break;

            case ManualDecisionType.KeepSeparate:
                await _canonicalManager.CreateCanonicalAsync(chunk, ct);
                break;

            case ManualDecisionType.Link:
                if (!decision.TargetCanonicalId.HasValue)
                    throw new InvalidOperationException("Link decision requires a target canonical ID.");
                // LOGIC: Create canonical for the new chunk and establish link.
                // Full linking implementation would go here (v0.5.9f).
                await _canonicalManager.CreateCanonicalAsync(chunk, ct);
                break;

            case ManualDecisionType.FlagContradiction:
                // LOGIC: Create canonical but mark as contradiction.
                // Full contradiction workflow integration would go here (v0.5.9e).
                await _canonicalManager.CreateCanonicalAsync(chunk, ct);
                break;

            case ManualDecisionType.Delete:
                await DeleteChunkByIdAsync(connection, chunk.Id, ct);
                break;

            default:
                throw new InvalidOperationException($"Unknown decision type: {decision.Decision}");
        }

        // LOGIC: Mark the review as processed.
        const string updateReviewSql = @"
            UPDATE ""PendingReviews""
            SET ""ReviewedAt"" = @ReviewedAt,
                ""ReviewedBy"" = @ReviewedBy,
                ""Decision"" = @Decision,
                ""DecisionNotes"" = @Notes
            WHERE ""Id"" = @ReviewId";

        await connection.ExecuteAsync(new DapperCommandDefinition(updateReviewSql, new
        {
            decision.ReviewId,
            ReviewedAt = DateTimeOffset.UtcNow,
            ReviewedBy = "System", // Would typically come from user context
            Decision = decision.Decision.ToString(),
            decision.Notes
        }, cancellationToken: ct));

        _logger.LogInformation(
            "Processed manual decision: ReviewId={ReviewId}, Decision={Decision}, ChunkId={ChunkId}",
            decision.ReviewId,
            decision.Decision,
            row.NewChunkId);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PendingReview>> GetPendingReviewsAsync(
        Guid? projectId = null,
        CancellationToken ct = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        var sql = @"
            SELECT ""Id"", ""NewChunkId"", ""Candidates"", ""QueuedAt"", ""AutoClassificationReason""
            FROM ""PendingReviews""
            WHERE ""ReviewedAt"" IS NULL";

        if (projectId.HasValue)
            sql += " AND \"ProjectId\" = @ProjectId";

        sql += " ORDER BY \"QueuedAt\" ASC";

        var rows = await connection.QueryAsync<PendingReviewRow>(
            new DapperCommandDefinition(sql, new { ProjectId = projectId }, cancellationToken: ct));

        var reviews = new List<PendingReview>();
        await using var loadConnection = await _connectionFactory.CreateConnectionAsync(ct);
        foreach (var row in rows)
        {
            var chunk = await LoadChunkByIdAsync(loadConnection, row.NewChunkId, ct);
            if (chunk is null) continue;

            var candidates = DeserializeCandidates(row.Candidates);
            reviews.Add(new PendingReview(
                row.Id,
                chunk,
                candidates,
                row.QueuedAt,
                row.AutoClassificationReason));
        }

        _logger.LogDebug("Retrieved {Count} pending reviews", reviews.Count);
        return reviews;
    }

    #region Private Methods

    /// <summary>
    /// Classifies all similarity results into duplicate candidates.
    /// </summary>
    private async Task<IReadOnlyList<DuplicateCandidate>> ClassifyCandidatesAsync(
        Chunk newChunk,
        IReadOnlyList<SimilarChunkResult> similarResults,
        DeduplicationOptions options,
        CancellationToken ct)
    {
        var candidates = new List<DuplicateCandidate>();

        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        foreach (var result in similarResults)
        {
            // Load the full chunk data via direct DB query.
            var existingChunk = await LoadChunkByIdAsync(connection, result.MatchedChunkId, ct);
            if (existingChunk is null) continue;

            // Classify the relationship.
            var classification = await _relationshipClassifier.ClassifyAsync(
                newChunk,
                existingChunk,
                (float)result.SimilarityScore,
                new ClassificationOptions
                {
                    EnableLlmClassification = options.RequireLlmConfirmation
                },
                ct);

            // Get canonical record if exists.
            var canonical = await _canonicalManager.GetCanonicalForChunkAsync(existingChunk.Id, ct);

            candidates.Add(new DuplicateCandidate(
                existingChunk,
                (float)result.SimilarityScore,
                classification,
                canonical?.Id));
        }

        return candidates;
    }

    /// <summary>
    /// Routes the chunk based on the best candidate's classification.
    /// </summary>
    private async Task<DeduplicationResult> RouteByClassificationAsync(
        Chunk newChunk,
        DuplicateCandidate bestCandidate,
        IReadOnlyList<DuplicateCandidate> allCandidates,
        DeduplicationOptions options,
        Stopwatch stopwatch,
        CancellationToken ct)
    {
        var classificationType = bestCandidate.Classification.Type;
        var confidence = bestCandidate.Classification.Confidence;
        var similarity = bestCandidate.SimilarityScore;

        // LOGIC: Check if confidence is too low for automatic decision.
        if (confidence < ManualReviewConfidenceThreshold && options.EnableManualReviewQueue)
        {
            await QueueForReviewAsync(newChunk, allCandidates, $"Low confidence ({confidence:F2})", options, ct);

            _logger.LogInformation(
                "Queued for review: ChunkId={ChunkId}, Confidence={Confidence:F2}",
                newChunk.Id,
                confidence);

            return DeduplicationResult.QueuedForReview(newChunk.Id, stopwatch.Elapsed);
        }

        return classificationType switch
        {
            RelationshipType.Equivalent => await HandleEquivalentAsync(newChunk, bestCandidate, options, stopwatch, ct),
            RelationshipType.Complementary => await HandleComplementaryAsync(newChunk, bestCandidate, stopwatch, ct),
            RelationshipType.Contradictory => await HandleContradictoryAsync(newChunk, bestCandidate, options, stopwatch, ct),
            RelationshipType.Superseding => await HandleSupersedingAsync(newChunk, bestCandidate, stopwatch, ct),
            _ => await HandleDistinctAsync(newChunk, stopwatch, ct)
        };
    }

    /// <summary>
    /// Handles equivalent chunks - merge if confidence is high enough.
    /// </summary>
    private async Task<DeduplicationResult> HandleEquivalentAsync(
        Chunk newChunk,
        DuplicateCandidate candidate,
        DeduplicationOptions options,
        Stopwatch stopwatch,
        CancellationToken ct)
    {
        var similarity = candidate.SimilarityScore;
        var confidence = candidate.Classification.Confidence;

        // LOGIC: Check auto-merge conditions.
        if (similarity >= options.AutoMergeThreshold && confidence >= AutoMergeConfidenceThreshold)
        {
            Guid canonicalId;
            if (candidate.HasCanonicalRecord)
            {
                // Merge into existing canonical.
                canonicalId = candidate.CanonicalRecordId!.Value;
                var canonical = await _canonicalManager.GetCanonicalForChunkAsync(candidate.ExistingChunk.Id, ct);
                if (canonical is not null)
                {
                    await _canonicalManager.MergeIntoCanonicalAsync(
                        canonical.Id,
                        newChunk,
                        RelationshipType.Equivalent,
                        similarity,
                        ct);
                    canonicalId = canonical.CanonicalChunkId;
                }
            }
            else
            {
                // Create canonical from existing chunk, then merge new as variant.
                var canonical = await _canonicalManager.CreateCanonicalAsync(candidate.ExistingChunk, ct);
                await _canonicalManager.MergeIntoCanonicalAsync(
                    canonical.Id,
                    newChunk,
                    RelationshipType.Equivalent,
                    similarity,
                    ct);
                canonicalId = canonical.CanonicalChunkId;
            }

            _logger.LogInformation(
                "Auto-merged chunk: ChunkId={ChunkId}, CanonicalId={CanonicalId}, Similarity={Similarity:F3}",
                newChunk.Id,
                canonicalId,
                similarity);

            return DeduplicationResult.Merged(canonicalId, newChunk.Id, stopwatch.Elapsed);
        }

        // Not confident enough for auto-merge - store as new.
        return await HandleDistinctAsync(newChunk, stopwatch, ct);
    }

    /// <summary>
    /// Handles complementary chunks - link without merging.
    /// </summary>
    private async Task<DeduplicationResult> HandleComplementaryAsync(
        Chunk newChunk,
        DuplicateCandidate candidate,
        Stopwatch stopwatch,
        CancellationToken ct)
    {
        // LOGIC: Create canonical for new chunk and establish link.
        var canonical = await _canonicalManager.CreateCanonicalAsync(newChunk, ct);

        _logger.LogInformation(
            "Linked complementary chunk: ChunkId={ChunkId}, LinkedTo={LinkedToId}",
            newChunk.Id,
            candidate.ExistingChunk.Id);

        return new DeduplicationResult(
            canonical.CanonicalChunkId,
            DeduplicationAction.LinkedToExisting,
            LinkedChunkIds: [candidate.ExistingChunk.Id],
            ProcessingDuration: stopwatch.Elapsed);
    }

    /// <summary>
    /// Handles contradictory chunks - flag for resolution via IContradictionService.
    /// </summary>
    /// <remarks>
    /// <b>v0.5.9e:</b> Now integrates with <see cref="IContradictionService.FlagAsync"/>
    /// to properly record contradictions in the dedicated table.
    /// </remarks>
    private async Task<DeduplicationResult> HandleContradictoryAsync(
        Chunk newChunk,
        DuplicateCandidate candidate,
        DeduplicationOptions options,
        Stopwatch stopwatch,
        CancellationToken ct)
    {
        if (!options.EnableContradictionDetection)
        {
            // Treat as distinct if contradiction detection is disabled.
            return await HandleDistinctAsync(newChunk, stopwatch, ct);
        }

        // LOGIC: Store chunk and flag contradiction via IContradictionService (v0.5.9e).
        var canonical = await _canonicalManager.CreateCanonicalAsync(newChunk, ct);

        // LOGIC: Record the contradiction in the dedicated contradictions table.
        // This enables admin review workflow and resolution tracking.
        await _contradictionService.FlagAsync(
            chunkAId: candidate.ExistingChunk.Id,
            chunkBId: newChunk.Id,
            similarityScore: candidate.SimilarityScore,
            confidence: candidate.Classification.Confidence,
            reason: candidate.Classification.Explanation,
            projectId: options.ProjectScope,
            ct: ct);

        _logger.LogWarning(
            "Flagged contradiction: ChunkId={ChunkId}, ConflictsWith={ConflictsWithId}, Confidence={Confidence:F3}",
            newChunk.Id,
            candidate.ExistingChunk.Id,
            candidate.Classification.Confidence);

        return new DeduplicationResult(
            canonical.CanonicalChunkId,
            DeduplicationAction.FlaggedAsContradiction,
            LinkedChunkIds: [candidate.ExistingChunk.Id],
            ProcessingDuration: stopwatch.Elapsed);
    }

    /// <summary>
    /// Handles superseding chunks - new replaces old.
    /// </summary>
    private async Task<DeduplicationResult> HandleSupersedingAsync(
        Chunk newChunk,
        DuplicateCandidate candidate,
        Stopwatch stopwatch,
        CancellationToken ct)
    {
        Guid canonicalId;

        if (candidate.HasCanonicalRecord)
        {
            // Promote new chunk to canonical, demote old.
            var existingCanonical = await _canonicalManager.GetCanonicalForChunkAsync(candidate.ExistingChunk.Id, ct);
            if (existingCanonical is not null)
            {
                // LOGIC: Merge old canonical as variant, then promote new chunk.
                await _canonicalManager.MergeIntoCanonicalAsync(
                    existingCanonical.Id,
                    newChunk,
                    RelationshipType.Superseding,
                    candidate.SimilarityScore,
                    ct);

                await _canonicalManager.PromoteVariantAsync(
                    existingCanonical.Id,
                    newChunk.Id,
                    "Superseding content",
                    ct);

                canonicalId = newChunk.Id;
            }
            else
            {
                canonicalId = (await _canonicalManager.CreateCanonicalAsync(newChunk, ct)).CanonicalChunkId;
            }
        }
        else
        {
            // Create canonical from new chunk.
            canonicalId = (await _canonicalManager.CreateCanonicalAsync(newChunk, ct)).CanonicalChunkId;
        }

        _logger.LogInformation(
            "Superseded existing content: NewChunkId={NewChunkId}, OldChunkId={OldChunkId}",
            newChunk.Id,
            candidate.ExistingChunk.Id);

        return new DeduplicationResult(
            canonicalId,
            DeduplicationAction.SupersededExisting,
            LinkedChunkIds: [candidate.ExistingChunk.Id],
            ProcessingDuration: stopwatch.Elapsed);
    }

    /// <summary>
    /// Handles distinct chunks - store as new canonical.
    /// </summary>
    private async Task<DeduplicationResult> HandleDistinctAsync(
        Chunk newChunk,
        Stopwatch stopwatch,
        CancellationToken ct)
    {
        await _canonicalManager.CreateCanonicalAsync(newChunk, ct);

        _logger.LogInformation(
            "Stored as distinct: ChunkId={ChunkId}",
            newChunk.Id);

        return DeduplicationResult.StoredAsNew(newChunk.Id, stopwatch.Elapsed);
    }

    /// <summary>
    /// Queues a chunk for manual review.
    /// </summary>
    private async Task QueueForReviewAsync(
        Chunk newChunk,
        IReadOnlyList<DuplicateCandidate> candidates,
        string reason,
        DeduplicationOptions options,
        CancellationToken ct)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        var candidatesJson = SerializeCandidates(candidates);

        const string sql = @"
            INSERT INTO ""PendingReviews"" (""Id"", ""NewChunkId"", ""ProjectId"", ""Candidates"", ""AutoClassificationReason"", ""QueuedAt"")
            VALUES (@Id, @NewChunkId, @ProjectId, @Candidates::jsonb, @Reason, @QueuedAt)";

        await connection.ExecuteAsync(new DapperCommandDefinition(sql, new
        {
            Id = Guid.NewGuid(),
            NewChunkId = newChunk.Id,
            ProjectId = options.ProjectScope,
            Candidates = candidatesJson,
            Reason = reason,
            QueuedAt = DateTimeOffset.UtcNow
        }, cancellationToken: ct));
    }

    /// <summary>
    /// Serializes candidates to JSON for storage.
    /// </summary>
    private static string SerializeCandidates(IReadOnlyList<DuplicateCandidate> candidates)
    {
        var simplified = candidates.Select(c => new
        {
            ChunkId = c.ExistingChunk.Id,
            c.SimilarityScore,
            RelationshipType = c.Classification.Type.ToString(),
            c.Classification.Confidence,
            c.CanonicalRecordId
        });

        return JsonSerializer.Serialize(simplified);
    }

    /// <summary>
    /// Deserializes candidates from JSON storage.
    /// </summary>
    private static IReadOnlyList<DuplicateCandidate> DeserializeCandidates(string json)
    {
        // LOGIC: Simplified deserialization - full chunk data would need to be loaded.
        // For now, return empty list since we'd need to reload chunks anyway.
        return [];
    }

    #endregion

    #region Database Helper Methods

    /// <summary>
    /// Loads a chunk by ID using direct database query.
    /// </summary>
    private async Task<Chunk?> LoadChunkByIdAsync(DbConnection connection, Guid chunkId, CancellationToken ct)
    {
        const string sql = @"
            SELECT ""Id"", ""DocumentId"", ""Content"", ""Embedding"", ""ChunkIndex"", ""StartOffset"", ""EndOffset"", ""Heading"", ""HeadingLevel""
            FROM ""Chunks""
            WHERE ""Id"" = @ChunkId";

        var row = await connection.QuerySingleOrDefaultAsync<ChunkRow>(
            new DapperCommandDefinition(sql, new { ChunkId = chunkId }, cancellationToken: ct));

        if (row is null) return null;

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
    /// Deletes a chunk by ID using direct database query.
    /// </summary>
    private async Task DeleteChunkByIdAsync(DbConnection connection, Guid chunkId, CancellationToken ct)
    {
        const string sql = @"DELETE FROM ""Chunks"" WHERE ""Id"" = @ChunkId";
        await connection.ExecuteAsync(new DapperCommandDefinition(sql, new { ChunkId = chunkId }, cancellationToken: ct));
    }

    #endregion

    #region Internal DTOs

    private record PendingReviewRow(
        Guid Id,
        Guid NewChunkId,
        string Candidates,
        DateTimeOffset QueuedAt,
        string? AutoClassificationReason);

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
