// =============================================================================
// File: BatchDeduplicationResult.cs
// Project: Lexichord.Abstractions
// Description: Result record for completed batch deduplication jobs.
// =============================================================================
// VERSION: v0.5.9g (Batch Retroactive Deduplication)
// LOGIC: Captures final statistics and outcome for batch job execution.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Represents the final result of a batch deduplication job execution.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9g as part of the Batch Retroactive Deduplication feature.
/// </para>
/// <para>
/// This record provides comprehensive statistics about what the batch job
/// accomplished. It is returned by <see cref="IBatchDeduplicationJob.ExecuteAsync"/>
/// and <see cref="IBatchDeduplicationJob.ResumeAsync"/> upon completion.
/// </para>
/// <para>
/// <b>Dry-Run Mode:</b> When executed with <see cref="BatchDeduplicationOptions.DryRun"/>,
/// all counters reflect what <i>would</i> happen, but no actual changes are made.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = await batchJob.ExecuteAsync(options, progress, ct);
/// 
/// Console.WriteLine($"Job {result.JobId} {result.FinalState}");
/// Console.WriteLine($"Processed: {result.ChunksProcessed:N0}");
/// Console.WriteLine($"Duplicates found: {result.DuplicatesFound:N0}");
/// Console.WriteLine($"Merged: {result.MergedCount:N0}");
/// Console.WriteLine($"Duration: {result.Duration}");
/// Console.WriteLine($"Storage saved: {result.StorageSavedBytes / 1024.0:N0} KB");
/// </code>
/// </example>
/// <param name="JobId">The unique identifier for this batch job.</param>
/// <param name="FinalState">The final state of the job when it completed.</param>
/// <param name="ChunksProcessed">Total number of chunks that were analyzed.</param>
/// <param name="DuplicatesFound">Number of duplicate pairs identified.</param>
/// <param name="MergedCount">Number of chunks merged into canonical records.</param>
/// <param name="LinkedCount">Number of chunks linked as complementary.</param>
/// <param name="ContradictionsFound">Number of contradictory chunk pairs flagged.</param>
/// <param name="QueuedForReview">Number of chunks queued for manual review.</param>
/// <param name="ErrorCount">Number of chunks that failed processing due to errors.</param>
/// <param name="Duration">Total execution time from start to completion.</param>
/// <param name="StorageSavedBytes">Estimated storage saved by removing duplicate chunks.</param>
/// <param name="ErrorMessage">Error description if job failed, otherwise null.</param>
public record BatchDeduplicationResult(
    Guid JobId,
    BatchJobState FinalState,
    int ChunksProcessed,
    int DuplicatesFound,
    int MergedCount,
    int LinkedCount,
    int ContradictionsFound,
    int QueuedForReview,
    int ErrorCount,
    TimeSpan Duration,
    long StorageSavedBytes,
    string? ErrorMessage)
{
    /// <summary>
    /// Gets whether the job completed successfully.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="FinalState"/> is <see cref="BatchJobState.Completed"/>;
    /// otherwise <c>false</c>.
    /// </value>
    public bool IsSuccess => FinalState == BatchJobState.Completed;

    /// <summary>
    /// Gets whether the job was cancelled.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="FinalState"/> is <see cref="BatchJobState.Cancelled"/>;
    /// otherwise <c>false</c>.
    /// </value>
    public bool IsCancelled => FinalState == BatchJobState.Cancelled;

    /// <summary>
    /// Gets whether the job failed with an error.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="FinalState"/> is <see cref="BatchJobState.Failed"/>;
    /// otherwise <c>false</c>.
    /// </value>
    public bool IsFailed => FinalState == BatchJobState.Failed;

    /// <summary>
    /// Gets the average processing rate in chunks per second.
    /// </summary>
    /// <value>
    /// Chunks processed per second, or 0 if duration is zero.
    /// </value>
    public double ChunksPerSecond =>
        Duration.TotalSeconds > 0 ? ChunksProcessed / Duration.TotalSeconds : 0;

    /// <summary>
    /// Gets the deduplication rate as a percentage.
    /// </summary>
    /// <value>
    /// Percentage of processed chunks that were merged as duplicates.
    /// Returns 0 if no chunks were processed.
    /// </value>
    public double DeduplicationRate =>
        ChunksProcessed > 0 ? (double)MergedCount / ChunksProcessed * 100 : 0;

    /// <summary>
    /// Creates a success result for a completed job.
    /// </summary>
    /// <param name="jobId">The job identifier.</param>
    /// <param name="processed">Chunks processed.</param>
    /// <param name="duplicates">Duplicates found.</param>
    /// <param name="merged">Chunks merged.</param>
    /// <param name="linked">Chunks linked.</param>
    /// <param name="contradictions">Contradictions flagged.</param>
    /// <param name="queued">Chunks queued for review.</param>
    /// <param name="errors">Error count.</param>
    /// <param name="duration">Total duration.</param>
    /// <param name="savedBytes">Storage saved.</param>
    /// <returns>A success result.</returns>
    public static BatchDeduplicationResult Success(
        Guid jobId,
        int processed,
        int duplicates,
        int merged,
        int linked,
        int contradictions,
        int queued,
        int errors,
        TimeSpan duration,
        long savedBytes) => new(
            JobId: jobId,
            FinalState: BatchJobState.Completed,
            ChunksProcessed: processed,
            DuplicatesFound: duplicates,
            MergedCount: merged,
            LinkedCount: linked,
            ContradictionsFound: contradictions,
            QueuedForReview: queued,
            ErrorCount: errors,
            Duration: duration,
            StorageSavedBytes: savedBytes,
            ErrorMessage: null);

    /// <summary>
    /// Creates a cancelled result.
    /// </summary>
    /// <param name="jobId">The job identifier.</param>
    /// <param name="processed">Chunks processed before cancellation.</param>
    /// <param name="duplicates">Duplicates found before cancellation.</param>
    /// <param name="merged">Chunks merged before cancellation.</param>
    /// <param name="linked">Chunks linked before cancellation.</param>
    /// <param name="contradictions">Contradictions flagged before cancellation.</param>
    /// <param name="queued">Chunks queued before cancellation.</param>
    /// <param name="duration">Duration until cancellation.</param>
    /// <param name="savedBytes">Storage saved before cancellation.</param>
    /// <returns>A cancelled result.</returns>
    public static BatchDeduplicationResult Cancelled(
        Guid jobId,
        int processed,
        int duplicates,
        int merged,
        int linked,
        int contradictions,
        int queued,
        TimeSpan duration,
        long savedBytes) => new(
            JobId: jobId,
            FinalState: BatchJobState.Cancelled,
            ChunksProcessed: processed,
            DuplicatesFound: duplicates,
            MergedCount: merged,
            LinkedCount: linked,
            ContradictionsFound: contradictions,
            QueuedForReview: queued,
            ErrorCount: 0,
            Duration: duration,
            StorageSavedBytes: savedBytes,
            ErrorMessage: "Job was cancelled by user request.");

    /// <summary>
    /// Creates a failed result with error details.
    /// </summary>
    /// <param name="jobId">The job identifier.</param>
    /// <param name="processed">Chunks processed before failure.</param>
    /// <param name="duplicates">Duplicates found before failure.</param>
    /// <param name="merged">Chunks merged before failure.</param>
    /// <param name="linked">Chunks linked before failure.</param>
    /// <param name="contradictions">Contradictions flagged before failure.</param>
    /// <param name="queued">Chunks queued before failure.</param>
    /// <param name="errors">Total error count.</param>
    /// <param name="duration">Duration until failure.</param>
    /// <param name="savedBytes">Storage saved before failure.</param>
    /// <param name="errorMessage">Description of the failure.</param>
    /// <returns>A failed result.</returns>
    public static BatchDeduplicationResult Failed(
        Guid jobId,
        int processed,
        int duplicates,
        int merged,
        int linked,
        int contradictions,
        int queued,
        int errors,
        TimeSpan duration,
        long savedBytes,
        string errorMessage) => new(
            JobId: jobId,
            FinalState: BatchJobState.Failed,
            ChunksProcessed: processed,
            DuplicatesFound: duplicates,
            MergedCount: merged,
            LinkedCount: linked,
            ContradictionsFound: contradictions,
            QueuedForReview: queued,
            ErrorCount: errors,
            Duration: duration,
            StorageSavedBytes: savedBytes,
            ErrorMessage: errorMessage);
}
