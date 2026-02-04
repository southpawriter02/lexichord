// =============================================================================
// File: BatchJobStatus.cs
// Project: Lexichord.Abstractions
// Description: Status record for querying batch deduplication job state.
// =============================================================================
// VERSION: v0.5.9g (Batch Retroactive Deduplication)
// LOGIC: Provides comprehensive snapshot of job state for status queries.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Represents the current status of a batch deduplication job.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9g as part of the Batch Retroactive Deduplication feature.
/// </para>
/// <para>
/// This record is returned by <see cref="IBatchDeduplicationJob.GetStatusAsync"/>
/// and provides a complete snapshot of the job's current state including:
/// </para>
/// <list type="bullet">
///   <item><description>Configuration options used for the job.</description></item>
///   <item><description>Current progress counters.</description></item>
///   <item><description>Timing information.</description></item>
///   <item><description>Checkpoint data for resumption.</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// var status = await batchJob.GetStatusAsync(jobId, ct);
/// 
/// if (status.State == BatchJobState.Running)
/// {
///     Console.WriteLine($"Progress: {status.ChunksProcessed} / {status.TotalChunks}");
///     Console.WriteLine($"Running for: {DateTime.UtcNow - status.StartedAt}");
/// }
/// </code>
/// </example>
/// <param name="JobId">The unique identifier for this batch job.</param>
/// <param name="State">The current lifecycle state of the job.</param>
/// <param name="Options">The configuration options used for this job.</param>
/// <param name="ChunksProcessed">Number of chunks processed so far.</param>
/// <param name="TotalChunks">Total chunks to process, or null if not yet counted.</param>
/// <param name="DuplicatesFound">Number of duplicate pairs identified so far.</param>
/// <param name="MergedCount">Number of chunks merged into canonical records.</param>
/// <param name="LinkedCount">Number of chunks linked as complementary.</param>
/// <param name="ContradictionsFound">Number of contradictory pairs flagged.</param>
/// <param name="QueuedForReview">Number of chunks queued for manual review.</param>
/// <param name="ErrorCount">Number of processing errors encountered.</param>
/// <param name="CreatedAt">When the job was created.</param>
/// <param name="StartedAt">When the job started executing, or null if not started.</param>
/// <param name="CompletedAt">When the job completed, or null if still running.</param>
/// <param name="LastCheckpoint">Last checkpoint chunk ID for resume capability.</param>
/// <param name="ErrorMessage">Most recent error message, or null if no errors.</param>
public record BatchJobStatus(
    Guid JobId,
    BatchJobState State,
    BatchDeduplicationOptions Options,
    int ChunksProcessed,
    int? TotalChunks,
    int DuplicatesFound,
    int MergedCount,
    int LinkedCount,
    int ContradictionsFound,
    int QueuedForReview,
    int ErrorCount,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    Guid? LastCheckpoint,
    string? ErrorMessage)
{
    /// <summary>
    /// Gets whether the job is currently running.
    /// </summary>
    public bool IsRunning => State == BatchJobState.Running;

    /// <summary>
    /// Gets whether the job is in a terminal state.
    /// </summary>
    /// <remarks>
    /// Terminal states are <see cref="BatchJobState.Completed"/>,
    /// <see cref="BatchJobState.Cancelled"/>, and <see cref="BatchJobState.Failed"/>.
    /// </remarks>
    public bool IsTerminal => State is BatchJobState.Completed
        or BatchJobState.Cancelled
        or BatchJobState.Failed;

    /// <summary>
    /// Gets whether the job can be resumed.
    /// </summary>
    /// <remarks>
    /// Jobs can be resumed if they are in <see cref="BatchJobState.Paused"/> state
    /// and have a valid checkpoint.
    /// </remarks>
    public bool CanResume => State == BatchJobState.Paused && LastCheckpoint.HasValue;

    /// <summary>
    /// Gets the percentage completion, or null if total is unknown.
    /// </summary>
    public double? PercentComplete => TotalChunks > 0
        ? (double)ChunksProcessed / TotalChunks.Value * 100
        : null;

    /// <summary>
    /// Gets the elapsed time since the job started.
    /// </summary>
    public TimeSpan? Elapsed => StartedAt.HasValue
        ? (CompletedAt ?? DateTime.UtcNow) - StartedAt.Value
        : null;

    /// <summary>
    /// Creates a status record for a newly created job.
    /// </summary>
    /// <param name="jobId">The job identifier.</param>
    /// <param name="options">The job configuration.</param>
    /// <returns>A new pending status record.</returns>
    public static BatchJobStatus CreatePending(Guid jobId, BatchDeduplicationOptions options) => new(
        JobId: jobId,
        State: BatchJobState.Pending,
        Options: options,
        ChunksProcessed: 0,
        TotalChunks: null,
        DuplicatesFound: 0,
        MergedCount: 0,
        LinkedCount: 0,
        ContradictionsFound: 0,
        QueuedForReview: 0,
        ErrorCount: 0,
        CreatedAt: DateTime.UtcNow,
        StartedAt: null,
        CompletedAt: null,
        LastCheckpoint: null,
        ErrorMessage: null);
}
