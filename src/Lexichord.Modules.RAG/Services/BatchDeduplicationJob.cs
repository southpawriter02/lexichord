// =============================================================================
// File: BatchDeduplicationJob.cs
// Project: Lexichord.Modules.RAG
// Description: Implementation of IBatchDeduplicationJob for batch processing.
// =============================================================================
// VERSION: v0.5.9g (Batch Retroactive Deduplication)
// LOGIC: Processes existing chunks in batches to identify and merge duplicates.
//   Supports progress tracking, resumability, dry-run mode, and throttling.
//
// Dependencies:
//   - ISimilarityDetector (v0.5.9a): Find similar chunks
//   - IRelationshipClassifier (v0.5.9b): Classify relationships
//   - ICanonicalManager (v0.5.9c): Manage canonical records
//   - IDeduplicationService (v0.5.9d): Core deduplication logic
//   - IContradictionService (v0.5.9e): Flag contradictions
// =============================================================================

using System.Data.Common;
using System.Diagnostics;
using System.Text.Json;
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
/// Implementation of <see cref="IBatchDeduplicationJob"/> for processing
/// existing chunks to identify and merge historical duplicates.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9g as part of the Batch Retroactive Deduplication feature.
/// </para>
/// <para>
/// This service provides batch processing capabilities for retroactive cleanup
/// of historical duplicates. Unlike the real-time <see cref="IDeduplicationService"/>
/// which processes chunks during ingestion, this service systematically scans
/// existing data.
/// </para>
/// <para>
/// <b>Key Features:</b>
/// </para>
/// <list type="bullet">
///   <item><description>Progress tracking via <see cref="IProgress{T}"/>.</description></item>
///   <item><description>Resumability via database checkpoints.</description></item>
///   <item><description>Dry-run mode for preview without changes.</description></item>
///   <item><description>Configurable throttling to minimize system impact.</description></item>
///   <item><description>Full cancellation support.</description></item>
/// </list>
/// <para>
/// <b>License:</b> Requires Teams tier via <see cref="FeatureCodes.BatchDeduplication"/>.
/// </para>
/// </remarks>
public sealed class BatchDeduplicationJob : IBatchDeduplicationJob
{
    private readonly ISimilarityDetector _similarityDetector;
    private readonly IRelationshipClassifier _relationshipClassifier;
    private readonly ICanonicalManager _canonicalManager;
    private readonly IContradictionService _contradictionService;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILicenseContext _licenseContext;
    private readonly IMediator _mediator;
    private readonly ILogger<BatchDeduplicationJob> _logger;

    /// <summary>
    /// Lock object to prevent concurrent jobs on the same project.
    /// </summary>
    private static readonly Dictionary<string, SemaphoreSlim> ProjectLocks = new();
    private static readonly object LockSync = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchDeduplicationJob"/> class.
    /// </summary>
    /// <param name="similarityDetector">The similarity detector service.</param>
    /// <param name="relationshipClassifier">The relationship classifier service.</param>
    /// <param name="canonicalManager">The canonical record manager service.</param>
    /// <param name="contradictionService">The contradiction detection service.</param>
    /// <param name="connectionFactory">The database connection factory.</param>
    /// <param name="licenseContext">The license context for feature gating.</param>
    /// <param name="mediator">The MediatR mediator for publishing events.</param>
    /// <param name="logger">The logger instance.</param>
    public BatchDeduplicationJob(
        ISimilarityDetector similarityDetector,
        IRelationshipClassifier relationshipClassifier,
        ICanonicalManager canonicalManager,
        IContradictionService contradictionService,
        IDbConnectionFactory connectionFactory,
        ILicenseContext licenseContext,
        IMediator mediator,
        ILogger<BatchDeduplicationJob> logger)
    {
        _similarityDetector = similarityDetector ?? throw new ArgumentNullException(nameof(similarityDetector));
        _relationshipClassifier = relationshipClassifier ?? throw new ArgumentNullException(nameof(relationshipClassifier));
        _canonicalManager = canonicalManager ?? throw new ArgumentNullException(nameof(canonicalManager));
        _contradictionService = contradictionService ?? throw new ArgumentNullException(nameof(contradictionService));
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("BatchDeduplicationJob initialized");
    }

    /// <inheritdoc/>
    public async Task<BatchDeduplicationResult> ExecuteAsync(
        BatchDeduplicationOptions? options = null,
        IProgress<BatchProgress>? progress = null,
        CancellationToken ct = default)
    {
        options ??= BatchDeduplicationOptions.Default;
        var jobId = Guid.NewGuid();
        var stopwatch = Stopwatch.StartNew();

        // LOGIC: Validate license.
        if (!_licenseContext.IsFeatureEnabled(FeatureCodes.BatchDeduplication))
        {
            _logger.LogWarning("Batch deduplication requires Teams license");
            throw new InvalidOperationException("Batch deduplication requires Teams tier license.");
        }

        // LOGIC: Validate options.
        ValidateOptions(options);

        // LOGIC: Acquire project lock to prevent concurrent jobs.
        var projectLock = GetOrCreateProjectLock(options.ProjectId);
        if (!await projectLock.WaitAsync(TimeSpan.Zero, ct))
        {
            throw new InvalidOperationException(
                $"Another batch job is already running for project {options.ProjectId?.ToString() ?? "all"}.");
        }

        try
        {
            _logger.LogInformation(
                "Starting batch deduplication job: JobId={JobId}, ProjectId={ProjectId}, DryRun={DryRun}, Threshold={Threshold}",
                jobId, options.ProjectId, options.DryRun, options.SimilarityThreshold);

            await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

            // LOGIC: Create job record in database.
            await CreateJobRecordAsync(connection, jobId, options, ct);

            // LOGIC: Count total chunks in scope.
            var totalChunks = await CountChunksInScopeAsync(connection, options, ct);
            await UpdateJobTotalAsync(connection, jobId, totalChunks, ct);

            progress?.Report(BatchProgress.Initial(totalChunks));

            // LOGIC: Process chunks in batches.
            var stats = await ProcessChunksAsync(connection, jobId, options, totalChunks, progress, stopwatch, ct);

            // LOGIC: Mark job as completed.
            await UpdateJobStateAsync(connection, jobId, BatchJobState.Completed, null, ct);

            var result = BatchDeduplicationResult.Success(
                jobId, stats.Processed, stats.Duplicates, stats.Merged, stats.Linked,
                stats.Contradictions, stats.Queued, stats.Errors, stopwatch.Elapsed, stats.SavedBytes);

            // LOGIC: Publish completion event.
            await _mediator.Publish(new BatchDeduplicationCompletedEvent(result), ct);

            _logger.LogInformation(
                "Batch deduplication completed: JobId={JobId}, Processed={Processed}, Merged={Merged}, Duration={Duration}",
                jobId, result.ChunksProcessed, result.MergedCount, result.Duration);

            return result;
        }
        catch (OperationCanceledException)
        {
            await using var conn = await _connectionFactory.CreateConnectionAsync(CancellationToken.None);
            var status = await GetJobStatsAsync(conn, jobId, CancellationToken.None);

            await UpdateJobStateAsync(conn, jobId, BatchJobState.Cancelled, "Job was cancelled by user request.", CancellationToken.None);

            var result = BatchDeduplicationResult.Cancelled(
                jobId, status.Processed, status.Duplicates, status.Merged, status.Linked,
                status.Contradictions, status.Queued, stopwatch.Elapsed, status.SavedBytes);

            await _mediator.Publish(new BatchDeduplicationCompletedEvent(result), CancellationToken.None);
            await _mediator.Publish(new BatchDeduplicationCancelledEvent(
                jobId, status.Processed, DateTime.UtcNow, true), CancellationToken.None);

            _logger.LogWarning("Batch deduplication cancelled: JobId={JobId}, Processed={Processed}", jobId, status.Processed);
            return result;
        }
        catch (Exception ex)
        {
            await using var conn = await _connectionFactory.CreateConnectionAsync(CancellationToken.None);
            var status = await GetJobStatsAsync(conn, jobId, CancellationToken.None);

            await UpdateJobStateAsync(conn, jobId, BatchJobState.Failed, ex.Message, CancellationToken.None);

            var result = BatchDeduplicationResult.Failed(
                jobId, status.Processed, status.Duplicates, status.Merged, status.Linked,
                status.Contradictions, status.Queued, status.Errors, stopwatch.Elapsed, status.SavedBytes, ex.Message);

            await _mediator.Publish(new BatchDeduplicationCompletedEvent(result), CancellationToken.None);

            _logger.LogError(ex, "Batch deduplication failed: JobId={JobId}", jobId);
            return result;
        }
        finally
        {
            projectLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<BatchJobStatus> GetStatusAsync(Guid jobId, CancellationToken ct = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            SELECT ""Id"", ""OptionsJson"", ""State"", ""TotalChunks"", ""ChunksProcessed"",
                   ""DuplicatesFound"", ""MergedCount"", ""LinkedCount"", ""ContradictionsFound"",
                   ""QueuedForReview"", ""ErrorCount"", ""CreatedAt"", ""StartedAt"", ""CompletedAt"",
                   ""LastCheckpointChunkId"", ""ErrorMessage""
            FROM ""BatchDedupJobs""
            WHERE ""Id"" = @JobId";

        var row = await connection.QuerySingleOrDefaultAsync<JobRow>(
            new DapperCommandDefinition(sql, new { JobId = jobId }, cancellationToken: ct));

        if (row is null)
            throw new KeyNotFoundException($"Batch job {jobId} not found.");

        var options = DeserializeOptions(row.OptionsJson);

        return new BatchJobStatus(
            JobId: row.Id,
            State: (BatchJobState)row.State,
            Options: options,
            ChunksProcessed: row.ChunksProcessed,
            TotalChunks: row.TotalChunks,
            DuplicatesFound: row.DuplicatesFound,
            MergedCount: row.MergedCount,
            LinkedCount: row.LinkedCount,
            ContradictionsFound: row.ContradictionsFound,
            QueuedForReview: row.QueuedForReview,
            ErrorCount: row.ErrorCount,
            CreatedAt: row.CreatedAt,
            StartedAt: row.StartedAt,
            CompletedAt: row.CompletedAt,
            LastCheckpoint: row.LastCheckpointChunkId,
            ErrorMessage: row.ErrorMessage);
    }

    /// <inheritdoc/>
    public async Task<bool> CancelAsync(Guid jobId, CancellationToken ct = default)
    {
        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        // LOGIC: Check current state.
        const string checkSql = @"SELECT ""State"" FROM ""BatchDedupJobs"" WHERE ""Id"" = @JobId";
        var state = await connection.QuerySingleOrDefaultAsync<int?>(
            new DapperCommandDefinition(checkSql, new { JobId = jobId }, cancellationToken: ct));

        if (state is null)
            throw new KeyNotFoundException($"Batch job {jobId} not found.");

        var currentState = (BatchJobState)state.Value;
        if (currentState is not BatchJobState.Running and not BatchJobState.Pending)
        {
            _logger.LogDebug("Cannot cancel job {JobId} in state {State}", jobId, currentState);
            return false;
        }

        // LOGIC: Update state to cancelled.
        const string updateSql = @"
            UPDATE ""BatchDedupJobs""
            SET ""State"" = @State, ""CompletedAt"" = @CompletedAt, ""ErrorMessage"" = @Message
            WHERE ""Id"" = @JobId";

        await connection.ExecuteAsync(new DapperCommandDefinition(updateSql, new
        {
            JobId = jobId,
            State = (int)BatchJobState.Cancelled,
            CompletedAt = DateTime.UtcNow,
            Message = "Job was cancelled by user request."
        }, cancellationToken: ct));

        _logger.LogInformation("Cancelled batch job: JobId={JobId}", jobId);
        return true;
    }

    /// <inheritdoc/>
    public async Task<BatchDeduplicationResult> ResumeAsync(
        Guid jobId,
        IProgress<BatchProgress>? progress = null,
        CancellationToken ct = default)
    {
        // LOGIC: Validate license.
        if (!_licenseContext.IsFeatureEnabled(FeatureCodes.BatchDeduplication))
        {
            throw new InvalidOperationException("Batch deduplication requires Teams tier license.");
        }

        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        // LOGIC: Load job record.
        const string sql = @"
            SELECT ""Id"", ""OptionsJson"", ""State"", ""ProjectId"", ""TotalChunks"", ""ChunksProcessed"",
                   ""DuplicatesFound"", ""MergedCount"", ""LinkedCount"", ""ContradictionsFound"",
                   ""QueuedForReview"", ""ErrorCount"", ""StorageSavedBytes"", ""CreatedAt"", ""StartedAt"",
                   ""LastCheckpointChunkId""
            FROM ""BatchDedupJobs""
            WHERE ""Id"" = @JobId";

        var row = await connection.QuerySingleOrDefaultAsync<ResumeJobRow>(
            new DapperCommandDefinition(sql, new { JobId = jobId }, cancellationToken: ct));

        if (row is null)
            throw new KeyNotFoundException($"Batch job {jobId} not found.");

        var currentState = (BatchJobState)row.State;
        if (currentState != BatchJobState.Paused)
        {
            throw new InvalidOperationException(
                $"Cannot resume job in state {currentState}. Only paused jobs can be resumed.");
        }

        if (!row.LastCheckpointChunkId.HasValue)
        {
            throw new InvalidOperationException("Cannot resume job without a valid checkpoint.");
        }

        var options = DeserializeOptions(row.OptionsJson);
        var stopwatch = Stopwatch.StartNew();

        // LOGIC: Acquire project lock.
        var projectLock = GetOrCreateProjectLock(row.ProjectId);
        if (!await projectLock.WaitAsync(TimeSpan.Zero, ct))
        {
            throw new InvalidOperationException(
                $"Another batch job is already running for project {row.ProjectId?.ToString() ?? "all"}.");
        }

        try
        {
            _logger.LogInformation(
                "Resuming batch deduplication job: JobId={JobId}, LastCheckpoint={Checkpoint}",
                jobId, row.LastCheckpointChunkId);

            // LOGIC: Update state to running.
            await UpdateJobStateAsync(connection, jobId, BatchJobState.Running, null, ct);

            // LOGIC: Resume processing from checkpoint.
            var stats = new BatchStats
            {
                Processed = row.ChunksProcessed,
                Duplicates = row.DuplicatesFound,
                Merged = row.MergedCount,
                Linked = row.LinkedCount,
                Contradictions = row.ContradictionsFound,
                Queued = row.QueuedForReview,
                Errors = row.ErrorCount,
                SavedBytes = row.StorageSavedBytes
            };

            var remainingStats = await ProcessChunksAsync(
                connection, jobId, options, row.TotalChunks ?? 0, progress, stopwatch, ct,
                resumeFromChunkId: row.LastCheckpointChunkId.Value);

            // Combine stats.
            stats.Processed += remainingStats.Processed;
            stats.Duplicates += remainingStats.Duplicates;
            stats.Merged += remainingStats.Merged;
            stats.Linked += remainingStats.Linked;
            stats.Contradictions += remainingStats.Contradictions;
            stats.Queued += remainingStats.Queued;
            stats.Errors += remainingStats.Errors;
            stats.SavedBytes += remainingStats.SavedBytes;

            // LOGIC: Mark as completed.
            await UpdateJobStateAsync(connection, jobId, BatchJobState.Completed, null, ct);

            var result = BatchDeduplicationResult.Success(
                jobId, stats.Processed, stats.Duplicates, stats.Merged, stats.Linked,
                stats.Contradictions, stats.Queued, stats.Errors, stopwatch.Elapsed, stats.SavedBytes);

            await _mediator.Publish(new BatchDeduplicationCompletedEvent(result), ct);

            _logger.LogInformation(
                "Resumed batch job completed: JobId={JobId}, Processed={Processed}, Merged={Merged}",
                jobId, result.ChunksProcessed, result.MergedCount);

            return result;
        }
        catch (OperationCanceledException)
        {
            var status = await GetJobStatsAsync(connection, jobId, CancellationToken.None);
            await UpdateJobStateAsync(connection, jobId, BatchJobState.Paused, "Job paused - can be resumed.", CancellationToken.None);

            var result = BatchDeduplicationResult.Cancelled(
                jobId, status.Processed, status.Duplicates, status.Merged, status.Linked,
                status.Contradictions, status.Queued, stopwatch.Elapsed, status.SavedBytes);

            await _mediator.Publish(new BatchDeduplicationCancelledEvent(
                jobId, status.Processed, DateTime.UtcNow, true), CancellationToken.None);

            return result;
        }
        finally
        {
            projectLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BatchJobRecord>> ListJobsAsync(
        BatchJobFilter? filter = null,
        CancellationToken ct = default)
    {
        filter ??= BatchJobFilter.All;
        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        var sql = @"
            SELECT ""Id"", ""State"", ""ProjectId"", ""Label"", ""IsDryRun"",
                   ""ChunksProcessed"", ""MergedCount"", ""CreatedAt"", ""StartedAt"", ""CompletedAt""
            FROM ""BatchDedupJobs""
            WHERE 1=1";

        var parameters = new DynamicParameters();

        if (filter.ProjectId.HasValue)
        {
            sql += @" AND ""ProjectId"" = @ProjectId";
            parameters.Add("ProjectId", filter.ProjectId.Value);
        }

        if (filter.States is { Length: > 0 })
        {
            var stateValues = filter.States.Select(s => (int)s).ToArray();
            sql += @" AND ""State"" = ANY(@States)";
            parameters.Add("States", stateValues);
        }

        if (filter.CreatedAfter.HasValue)
        {
            sql += @" AND ""CreatedAt"" >= @CreatedAfter";
            parameters.Add("CreatedAfter", filter.CreatedAfter.Value);
        }

        if (filter.CreatedBefore.HasValue)
        {
            sql += @" AND ""CreatedAt"" <= @CreatedBefore";
            parameters.Add("CreatedBefore", filter.CreatedBefore.Value);
        }

        sql += @" ORDER BY ""CreatedAt"" DESC LIMIT @Limit";
        parameters.Add("Limit", filter.Limit);

        var rows = await connection.QueryAsync<ListJobRow>(
            new DapperCommandDefinition(sql, parameters, cancellationToken: ct));

        return rows.Select(r => new BatchJobRecord(
            JobId: r.Id,
            State: (BatchJobState)r.State,
            ProjectId: r.ProjectId,
            Label: r.Label,
            DryRun: r.IsDryRun,
            ChunksProcessed: r.ChunksProcessed,
            MergedCount: r.MergedCount,
            CreatedAt: r.CreatedAt,
            StartedAt: r.StartedAt,
            CompletedAt: r.CompletedAt)).ToList();
    }

    #region Private Methods

    /// <summary>
    /// Validates batch deduplication options.
    /// </summary>
    private static void ValidateOptions(BatchDeduplicationOptions options)
    {
        if (options.SimilarityThreshold is < 0 or > 1)
            throw new ArgumentException("SimilarityThreshold must be between 0 and 1.", nameof(options));

        if (options.BatchSize is < 10 or > 1000)
            throw new ArgumentException("BatchSize must be between 10 and 1000.", nameof(options));

        if (options.BatchDelayMs is < 0 or > 5000)
            throw new ArgumentException("BatchDelayMs must be between 0 and 5000.", nameof(options));

        if (options.MaxChunks < 0)
            throw new ArgumentException("MaxChunks cannot be negative.", nameof(options));

        if (options.Priority is < 0 or > 100)
            throw new ArgumentException("Priority must be between 0 and 100.", nameof(options));
    }

    /// <summary>
    /// Gets or creates a semaphore lock for the specified project.
    /// </summary>
    private static SemaphoreSlim GetOrCreateProjectLock(Guid? projectId)
    {
        var key = projectId?.ToString() ?? "__all__";
        lock (LockSync)
        {
            if (!ProjectLocks.TryGetValue(key, out var semaphore))
            {
                semaphore = new SemaphoreSlim(1, 1);
                ProjectLocks[key] = semaphore;
            }
            return semaphore;
        }
    }

    /// <summary>
    /// Creates a job record in the database.
    /// </summary>
    private async Task CreateJobRecordAsync(
        DbConnection connection, Guid jobId, BatchDeduplicationOptions options, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO ""BatchDedupJobs"" (""Id"", ""OptionsJson"", ""State"", ""ProjectId"", ""Label"", ""IsDryRun"", ""StartedAt"")
            VALUES (@Id, @OptionsJson::jsonb, @State, @ProjectId, @Label, @IsDryRun, @StartedAt)";

        var optionsJson = JsonSerializer.Serialize(options);

        await connection.ExecuteAsync(new DapperCommandDefinition(sql, new
        {
            Id = jobId,
            OptionsJson = optionsJson,
            State = (int)BatchJobState.Running,
            options.ProjectId,
            options.Label,
            IsDryRun = options.DryRun,
            StartedAt = DateTime.UtcNow
        }, cancellationToken: ct));
    }

    /// <summary>
    /// Counts chunks in scope for the batch job.
    /// </summary>
    private async Task<int> CountChunksInScopeAsync(
        DbConnection connection, BatchDeduplicationOptions options, CancellationToken ct)
    {
        var sql = @"SELECT COUNT(*) FROM ""Chunks"" c";

        if (options.ProjectId.HasValue)
        {
            sql += @" INNER JOIN ""Documents"" d ON c.""DocumentId"" = d.""Id"" WHERE d.""ProjectId"" = @ProjectId";
        }

        var count = await connection.ExecuteScalarAsync<int>(
            new DapperCommandDefinition(sql, new { options.ProjectId }, cancellationToken: ct));

        if (options.MaxChunks > 0)
            count = Math.Min(count, options.MaxChunks);

        return count;
    }

    /// <summary>
    /// Updates job total chunks count.
    /// </summary>
    private async Task UpdateJobTotalAsync(
        DbConnection connection, Guid jobId, int totalChunks, CancellationToken ct)
    {
        const string sql = @"UPDATE ""BatchDedupJobs"" SET ""TotalChunks"" = @Total WHERE ""Id"" = @JobId";
        await connection.ExecuteAsync(new DapperCommandDefinition(sql, new { JobId = jobId, Total = totalChunks }, cancellationToken: ct));
    }

    /// <summary>
    /// Updates job state.
    /// </summary>
    private async Task UpdateJobStateAsync(
        DbConnection connection, Guid jobId, BatchJobState state, string? errorMessage, CancellationToken ct)
    {
        var isTerminal = state is BatchJobState.Completed or BatchJobState.Cancelled or BatchJobState.Failed;

        const string sql = @"
            UPDATE ""BatchDedupJobs""
            SET ""State"" = @State, ""ErrorMessage"" = @ErrorMessage,
                ""CompletedAt"" = CASE WHEN @IsTerminal THEN @Now ELSE ""CompletedAt"" END
            WHERE ""Id"" = @JobId";

        await connection.ExecuteAsync(new DapperCommandDefinition(sql, new
        {
            JobId = jobId,
            State = (int)state,
            ErrorMessage = errorMessage,
            IsTerminal = isTerminal,
            Now = DateTime.UtcNow
        }, cancellationToken: ct));
    }

    /// <summary>
    /// Gets current job statistics from database.
    /// </summary>
    private async Task<BatchStats> GetJobStatsAsync(DbConnection connection, Guid jobId, CancellationToken ct)
    {
        const string sql = @"
            SELECT ""ChunksProcessed"", ""DuplicatesFound"", ""MergedCount"", ""LinkedCount"",
                   ""ContradictionsFound"", ""QueuedForReview"", ""ErrorCount"", ""StorageSavedBytes""
            FROM ""BatchDedupJobs"" WHERE ""Id"" = @JobId";

        var row = await connection.QuerySingleOrDefaultAsync<StatsRow>(
            new DapperCommandDefinition(sql, new { JobId = jobId }, cancellationToken: ct));

        return new BatchStats
        {
            Processed = row?.ChunksProcessed ?? 0,
            Duplicates = row?.DuplicatesFound ?? 0,
            Merged = row?.MergedCount ?? 0,
            Linked = row?.LinkedCount ?? 0,
            Contradictions = row?.ContradictionsFound ?? 0,
            Queued = row?.QueuedForReview ?? 0,
            Errors = row?.ErrorCount ?? 0,
            SavedBytes = row?.StorageSavedBytes ?? 0
        };
    }

    /// <summary>
    /// Main batch processing loop.
    /// </summary>
    private async Task<BatchStats> ProcessChunksAsync(
        DbConnection connection,
        Guid jobId,
        BatchDeduplicationOptions options,
        int totalChunks,
        IProgress<BatchProgress>? progress,
        Stopwatch stopwatch,
        CancellationToken ct,
        Guid? resumeFromChunkId = null)
    {
        var stats = new BatchStats();
        var processedSinceCheckpoint = 0;
        Guid? lastProcessedChunkId = resumeFromChunkId;

        // LOGIC: Iterate through chunks in batches.
        while (true)
        {
            ct.ThrowIfCancellationRequested();

            // LOGIC: Load next batch of chunks.
            var chunks = await LoadNextBatchAsync(connection, options, lastProcessedChunkId, ct);
            if (chunks.Count == 0)
                break;

            // LOGIC: Check MaxChunks limit.
            if (options.MaxChunks > 0 && stats.Processed >= options.MaxChunks)
                break;

            var batchMerged = 0;

            foreach (var chunk in chunks)
            {
                ct.ThrowIfCancellationRequested();

                if (options.MaxChunks > 0 && stats.Processed >= options.MaxChunks)
                    break;

                // LOGIC: Check if already processed (for resume).
                if (await IsChunkProcessedAsync(connection, jobId, chunk.Id, ct))
                {
                    lastProcessedChunkId = chunk.Id;
                    continue;
                }

                try
                {
                    var result = await ProcessSingleChunkAsync(chunk, options, ct);
                    stats.Processed++;
                    processedSinceCheckpoint++;

                    // Update stats based on result.
                    switch (result.Action)
                    {
                        case DeduplicationAction.MergedIntoExisting:
                            stats.Merged++;
                            stats.Duplicates++;
                            batchMerged++;
                            stats.SavedBytes += EstimateChunkSize(chunk);
                            break;
                        case DeduplicationAction.LinkedToExisting:
                            stats.Linked++;
                            stats.Duplicates++;
                            break;
                        case DeduplicationAction.FlaggedAsContradiction:
                            stats.Contradictions++;
                            break;
                        case DeduplicationAction.QueuedForReview:
                            stats.Queued++;
                            break;
                    }

                    // LOGIC: Record processed chunk.
                    await RecordProcessedChunkAsync(connection, jobId, chunk.Id, result.Action, ct);
                    lastProcessedChunkId = chunk.Id;
                }
                catch (Exception ex)
                {
                    stats.Errors++;
                    _logger.LogWarning(ex, "Error processing chunk {ChunkId}", chunk.Id);
                }
            }

            // LOGIC: Update progress counters in database.
            await UpdateJobProgressAsync(connection, jobId, stats, ct);

            // LOGIC: Save checkpoint if needed.
            if (processedSinceCheckpoint >= options.CheckpointFrequency && lastProcessedChunkId.HasValue)
            {
                await SaveCheckpointAsync(connection, jobId, lastProcessedChunkId.Value, ct);
                processedSinceCheckpoint = 0;
            }

            // LOGIC: Report progress.
            var progressUpdate = BatchProgress.Create(
                stats.Processed, totalChunks, stopwatch.Elapsed,
                $"Processing batch - {stats.Merged} merged so far");
            progress?.Report(progressUpdate);

            await _mediator.Publish(new BatchDeduplicationProgressEvent(
                jobId, progressUpdate, batchMerged, stats.Merged), ct);

            // LOGIC: Apply throttle delay.
            if (options.BatchDelayMs > 0)
            {
                await Task.Delay(options.BatchDelayMs, ct);
            }
        }

        // Final checkpoint.
        if (lastProcessedChunkId.HasValue)
        {
            await SaveCheckpointAsync(connection, jobId, lastProcessedChunkId.Value, ct);
        }

        // Final progress report.
        progress?.Report(BatchProgress.Completed(stats.Processed, stopwatch.Elapsed));

        return stats;
    }

    /// <summary>
    /// Loads the next batch of chunks for processing.
    /// </summary>
    private async Task<IReadOnlyList<Chunk>> LoadNextBatchAsync(
        DbConnection connection,
        BatchDeduplicationOptions options,
        Guid? afterChunkId,
        CancellationToken ct)
    {
        var sql = @"
            SELECT c.""Id"", c.""DocumentId"", c.""Content"", c.""Embedding"", c.""ChunkIndex"",
                   c.""StartOffset"", c.""EndOffset"", c.""Heading"", c.""HeadingLevel""
            FROM ""Chunks"" c";

        if (options.ProjectId.HasValue)
        {
            sql += @" INNER JOIN ""Documents"" d ON c.""DocumentId"" = d.""Id""
                      WHERE d.""ProjectId"" = @ProjectId";
            if (afterChunkId.HasValue)
            {
                sql += @" AND c.""Id"" > @AfterChunkId";
            }
        }
        else if (afterChunkId.HasValue)
        {
            sql += @" WHERE c.""Id"" > @AfterChunkId";
        }

        sql += @" ORDER BY c.""Id"" LIMIT @BatchSize";

        var rows = await connection.QueryAsync<ChunkRow>(new DapperCommandDefinition(sql, new
        {
            options.ProjectId,
            AfterChunkId = afterChunkId,
            options.BatchSize
        }, cancellationToken: ct));

        return rows.Select(r => new Chunk(
            r.Id, r.DocumentId, r.Content, r.Embedding, r.ChunkIndex,
            r.StartOffset, r.EndOffset, r.Heading, r.HeadingLevel ?? 0)).ToList();
    }

    /// <summary>
    /// Checks if a chunk has already been processed by this job.
    /// </summary>
    private async Task<bool> IsChunkProcessedAsync(DbConnection connection, Guid jobId, Guid chunkId, CancellationToken ct)
    {
        const string sql = @"SELECT EXISTS(SELECT 1 FROM ""BatchDedupProcessed"" WHERE ""JobId"" = @JobId AND ""ChunkId"" = @ChunkId)";
        return await connection.ExecuteScalarAsync<bool>(
            new DapperCommandDefinition(sql, new { JobId = jobId, ChunkId = chunkId }, cancellationToken: ct));
    }

    /// <summary>
    /// Processes a single chunk for deduplication.
    /// </summary>
    private async Task<ChunkProcessResult> ProcessSingleChunkAsync(
        Chunk chunk,
        BatchDeduplicationOptions options,
        CancellationToken ct)
    {
        // LOGIC: Skip chunks without embeddings.
        if (chunk.Embedding is null)
        {
            return new ChunkProcessResult(DeduplicationAction.StoredAsNew);
        }

        // LOGIC: Find similar chunks.
        var similarResults = await _similarityDetector.FindSimilarAsync(
            chunk,
            new SimilarityDetectorOptions
            {
                SimilarityThreshold = options.SimilarityThreshold,
                MaxResultsPerChunk = 10,
                ExcludeSameDocument = false // Allow same-document matches for batch processing
            },
            ct);

        if (!similarResults.Any())
        {
            return new ChunkProcessResult(DeduplicationAction.StoredAsNew);
        }

        // LOGIC: Get the best match.
        var bestMatch = similarResults.OrderByDescending(r => r.SimilarityScore).First();

        // LOGIC: Load the matched chunk and classify relationship.
        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);
        var matchedChunk = await LoadChunkByIdAsync(connection, bestMatch.MatchedChunkId, ct);
        if (matchedChunk is null)
        {
            return new ChunkProcessResult(DeduplicationAction.StoredAsNew);
        }

        var classification = await _relationshipClassifier.ClassifyAsync(
            chunk,
            matchedChunk,
            (float)bestMatch.SimilarityScore,
            new ClassificationOptions { EnableLlmClassification = options.RequireLlmConfirmation },
            ct);

        // LOGIC: Dry-run mode - don't make changes.
        if (options.DryRun)
        {
            return classification.Type switch
            {
                RelationshipType.Equivalent => new ChunkProcessResult(DeduplicationAction.MergedIntoExisting),
                RelationshipType.Complementary => new ChunkProcessResult(DeduplicationAction.LinkedToExisting),
                RelationshipType.Contradictory => new ChunkProcessResult(DeduplicationAction.FlaggedAsContradiction),
                _ => new ChunkProcessResult(DeduplicationAction.StoredAsNew)
            };
        }

        // LOGIC: Execute based on classification.
        return await ExecuteClassificationActionAsync(chunk, matchedChunk, classification, bestMatch.SimilarityScore, options, ct);
    }

    /// <summary>
    /// Executes the appropriate action based on classification.
    /// </summary>
    private async Task<ChunkProcessResult> ExecuteClassificationActionAsync(
        Chunk chunk,
        Chunk matchedChunk,
        RelationshipClassification classification,
        double similarity,
        BatchDeduplicationOptions options,
        CancellationToken ct)
    {
        switch (classification.Type)
        {
            case RelationshipType.Equivalent when classification.Confidence >= options.AutoMergeConfidenceThreshold:
                // Auto-merge.
                var canonical = await _canonicalManager.GetCanonicalForChunkAsync(matchedChunk.Id, ct);
                if (canonical is not null)
                {
                    await _canonicalManager.MergeIntoCanonicalAsync(
                        canonical.Id, chunk, RelationshipType.Equivalent, (float)similarity, ct);
                }
                else
                {
                    var newCanonical = await _canonicalManager.CreateCanonicalAsync(matchedChunk, ct);
                    await _canonicalManager.MergeIntoCanonicalAsync(
                        newCanonical.Id, chunk, RelationshipType.Equivalent, (float)similarity, ct);
                }
                return new ChunkProcessResult(DeduplicationAction.MergedIntoExisting);

            case RelationshipType.Complementary:
                // Link without merge.
                await _canonicalManager.CreateCanonicalAsync(chunk, ct);
                return new ChunkProcessResult(DeduplicationAction.LinkedToExisting);

            case RelationshipType.Contradictory when options.EnableContradictionDetection:
                // Flag contradiction.
                await _canonicalManager.CreateCanonicalAsync(chunk, ct);
                await _contradictionService.FlagAsync(
                    matchedChunk.Id, chunk.Id, (float)similarity,
                    classification.Confidence, classification.Explanation, options.ProjectId, ct);
                return new ChunkProcessResult(DeduplicationAction.FlaggedAsContradiction);

            case RelationshipType.Equivalent:
                // Low confidence - queue for review.
                return new ChunkProcessResult(DeduplicationAction.QueuedForReview);

            default:
                return new ChunkProcessResult(DeduplicationAction.StoredAsNew);
        }
    }

    /// <summary>
    /// Records a processed chunk in the database.
    /// </summary>
    private async Task RecordProcessedChunkAsync(
        DbConnection connection, Guid jobId, Guid chunkId, DeduplicationAction action, CancellationToken ct)
    {
        const string sql = @"
            INSERT INTO ""BatchDedupProcessed"" (""JobId"", ""ChunkId"", ""Action"", ""ProcessedAt"")
            VALUES (@JobId, @ChunkId, @Action, @ProcessedAt)
            ON CONFLICT DO NOTHING";

        await connection.ExecuteAsync(new DapperCommandDefinition(sql, new
        {
            JobId = jobId,
            ChunkId = chunkId,
            Action = (int)action,
            ProcessedAt = DateTime.UtcNow
        }, cancellationToken: ct));
    }

    /// <summary>
    /// Updates job progress counters.
    /// </summary>
    private async Task UpdateJobProgressAsync(DbConnection connection, Guid jobId, BatchStats stats, CancellationToken ct)
    {
        const string sql = @"
            UPDATE ""BatchDedupJobs""
            SET ""ChunksProcessed"" = @Processed, ""DuplicatesFound"" = @Duplicates,
                ""MergedCount"" = @Merged, ""LinkedCount"" = @Linked,
                ""ContradictionsFound"" = @Contradictions, ""QueuedForReview"" = @Queued,
                ""ErrorCount"" = @Errors, ""StorageSavedBytes"" = @SavedBytes
            WHERE ""Id"" = @JobId";

        await connection.ExecuteAsync(new DapperCommandDefinition(sql, new
        {
            JobId = jobId,
            stats.Processed,
            stats.Duplicates,
            stats.Merged,
            stats.Linked,
            stats.Contradictions,
            stats.Queued,
            stats.Errors,
            stats.SavedBytes
        }, cancellationToken: ct));
    }

    /// <summary>
    /// Saves a checkpoint for resume capability.
    /// </summary>
    private async Task SaveCheckpointAsync(DbConnection connection, Guid jobId, Guid chunkId, CancellationToken ct)
    {
        const string sql = @"
            UPDATE ""BatchDedupJobs""
            SET ""LastCheckpointChunkId"" = @ChunkId, ""LastCheckpointAt"" = @Now
            WHERE ""Id"" = @JobId";

        await connection.ExecuteAsync(new DapperCommandDefinition(sql, new
        {
            JobId = jobId,
            ChunkId = chunkId,
            Now = DateTime.UtcNow
        }, cancellationToken: ct));

        _logger.LogDebug("Saved checkpoint: JobId={JobId}, ChunkId={ChunkId}", jobId, chunkId);
    }

    /// <summary>
    /// Loads a chunk by ID.
    /// </summary>
    private async Task<Chunk?> LoadChunkByIdAsync(DbConnection connection, Guid chunkId, CancellationToken ct)
    {
        const string sql = @"
            SELECT ""Id"", ""DocumentId"", ""Content"", ""Embedding"", ""ChunkIndex"",
                   ""StartOffset"", ""EndOffset"", ""Heading"", ""HeadingLevel""
            FROM ""Chunks""
            WHERE ""Id"" = @ChunkId";

        var row = await connection.QuerySingleOrDefaultAsync<ChunkRow>(
            new DapperCommandDefinition(sql, new { ChunkId = chunkId }, cancellationToken: ct));

        if (row is null) return null;

        return new Chunk(
            row.Id, row.DocumentId, row.Content, row.Embedding, row.ChunkIndex,
            row.StartOffset, row.EndOffset, row.Heading, row.HeadingLevel ?? 0);
    }

    /// <summary>
    /// Estimates the size of a chunk for storage savings calculation.
    /// </summary>
    private static long EstimateChunkSize(Chunk chunk)
    {
        // Roughly: content bytes + embedding (1536 floats * 4 bytes) + metadata overhead
        return (chunk.Content?.Length ?? 0) + (chunk.Embedding?.Length ?? 0) * 4 + 500;
    }

    /// <summary>
    /// Deserializes options from JSON.
    /// </summary>
    private static BatchDeduplicationOptions DeserializeOptions(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<BatchDeduplicationOptions>(json) ?? BatchDeduplicationOptions.Default;
        }
        catch
        {
            return BatchDeduplicationOptions.Default;
        }
    }

    #endregion

    #region Internal DTOs

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

    private record JobRow(
        Guid Id,
        string OptionsJson,
        int State,
        int? TotalChunks,
        int ChunksProcessed,
        int DuplicatesFound,
        int MergedCount,
        int LinkedCount,
        int ContradictionsFound,
        int QueuedForReview,
        int ErrorCount,
        DateTime CreatedAt,
        DateTime? StartedAt,
        DateTime? CompletedAt,
        Guid? LastCheckpointChunkId,
        string? ErrorMessage);

    private record ResumeJobRow(
        Guid Id,
        string OptionsJson,
        int State,
        Guid? ProjectId,
        int? TotalChunks,
        int ChunksProcessed,
        int DuplicatesFound,
        int MergedCount,
        int LinkedCount,
        int ContradictionsFound,
        int QueuedForReview,
        int ErrorCount,
        long StorageSavedBytes,
        DateTime CreatedAt,
        DateTime? StartedAt,
        Guid? LastCheckpointChunkId);

    private record ListJobRow(
        Guid Id,
        int State,
        Guid? ProjectId,
        string? Label,
        bool IsDryRun,
        int ChunksProcessed,
        int MergedCount,
        DateTime CreatedAt,
        DateTime? StartedAt,
        DateTime? CompletedAt);

    private record StatsRow(
        int ChunksProcessed,
        int DuplicatesFound,
        int MergedCount,
        int LinkedCount,
        int ContradictionsFound,
        int QueuedForReview,
        int ErrorCount,
        long StorageSavedBytes);

    private record ChunkProcessResult(DeduplicationAction Action);

    private class BatchStats
    {
        public int Processed { get; set; }
        public int Duplicates { get; set; }
        public int Merged { get; set; }
        public int Linked { get; set; }
        public int Contradictions { get; set; }
        public int Queued { get; set; }
        public int Errors { get; set; }
        public long SavedBytes { get; set; }
    }

    #endregion
}
