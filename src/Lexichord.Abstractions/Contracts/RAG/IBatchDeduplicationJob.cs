// =============================================================================
// File: IBatchDeduplicationJob.cs
// Project: Lexichord.Abstractions
// Description: Interface for batch retroactive deduplication job execution.
// =============================================================================
// VERSION: v0.5.9g (Batch Retroactive Deduplication)
// LOGIC: Defines the contract for processing existing chunks to identify and
//   merge historical duplicates. Supports progress tracking, resumability,
//   dry-run mode, and throttling.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Service for executing batch deduplication jobs on existing chunks.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9g as part of the Batch Retroactive Deduplication feature.
/// </para>
/// <para>
/// This service provides batch processing capabilities for identifying and merging
/// duplicate chunks that already exist in the database. Unlike the real-time
/// <see cref="IDeduplicationService"/> which processes chunks during ingestion,
/// this service is designed for retroactive cleanup of historical data.
/// </para>
/// <para>
/// <b>Key Features:</b>
/// </para>
/// <list type="bullet">
///   <item><description><b>Progress Tracking:</b> Real-time updates via <see cref="IProgress{T}"/>.</description></item>
///   <item><description><b>Resumability:</b> Jobs can be paused and resumed via checkpoints.</description></item>
///   <item><description><b>Dry-Run Mode:</b> Preview changes without modifying data.</description></item>
///   <item><description><b>Throttling:</b> Configurable batch size and delay between batches.</description></item>
///   <item><description><b>Cancellation:</b> Full support for <see cref="CancellationToken"/>.</description></item>
/// </list>
/// <para>
/// <b>Processing Flow:</b>
/// </para>
/// <list type="number">
///   <item><description>Count chunks in scope (project or all).</description></item>
///   <item><description>Iterate through chunks in batches.</description></item>
///   <item><description>For each chunk, find similar chunks via <see cref="ISimilarityDetector"/>.</description></item>
///   <item><description>Classify relationships via <see cref="IRelationshipClassifier"/>.</description></item>
///   <item><description>Route by classification (merge, link, flag, or skip).</description></item>
///   <item><description>Save checkpoint after each batch.</description></item>
///   <item><description>Report progress via <see cref="IProgress{T}"/>.</description></item>
/// </list>
/// <para>
/// <b>License:</b> Requires Teams tier. When unlicensed, all methods throw
/// <see cref="InvalidOperationException"/>.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Methods are thread-safe, but only one job per project
/// can be running at a time. Concurrent jobs on different projects are allowed.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Execute a batch deduplication job with progress tracking
/// var options = new BatchDeduplicationOptions
/// {
///     ProjectId = myProjectId,
///     SimilarityThreshold = 0.92f,
///     BatchSize = 100
/// };
/// 
/// var progress = new Progress&lt;BatchProgress&gt;(update =>
/// {
///     Console.WriteLine($"Progress: {update.PercentComplete:P1}");
/// });
/// 
/// var result = await batchJob.ExecuteAsync(options, progress, cancellationToken);
/// 
/// Console.WriteLine($"Merged {result.MergedCount} duplicates");
/// Console.WriteLine($"Saved {result.StorageSavedBytes / 1024.0:N0} KB");
/// </code>
/// </example>
public interface IBatchDeduplicationJob
{
    /// <summary>
    /// Executes a batch deduplication job with the specified options.
    /// </summary>
    /// <param name="options">
    /// Configuration for the batch job. See <see cref="BatchDeduplicationOptions"/> for details.
    /// Uses <see cref="BatchDeduplicationOptions.Default"/> if null.
    /// </param>
    /// <param name="progress">
    /// Optional progress reporter for real-time updates. Progress is reported
    /// after each batch completes.
    /// </param>
    /// <param name="ct">Cancellation token for graceful job termination.</param>
    /// <returns>
    /// Final result with statistics about processed, merged, and linked chunks.
    /// See <see cref="BatchDeduplicationResult"/> for details.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when:
    /// <list type="bullet">
    ///   <item><description>Teams license is not active.</description></item>
    ///   <item><description>Another job is already running for the same project.</description></item>
    /// </list>
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="options"/> has invalid values (e.g., negative batch size).
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Main execution flow:
    /// </para>
    /// <list type="number">
    ///   <item><description>Validate license and options.</description></item>
    ///   <item><description>Check for existing running job on same project.</description></item>
    ///   <item><description>Create job record in database.</description></item>
    ///   <item><description>Count total chunks in scope.</description></item>
    ///   <item><description>Process chunks in batches with throttling.</description></item>
    ///   <item><description>Save checkpoints at configured frequency.</description></item>
    ///   <item><description>Publish MediatR events (progress, completion).</description></item>
    /// </list>
    /// <para>
    /// <b>Dry-Run:</b> When <see cref="BatchDeduplicationOptions.DryRun"/> is true,
    /// all analysis is performed but no data modifications are made.
    /// </para>
    /// <para>
    /// <b>Cancellation:</b> Cancellation is honored at batch boundaries. Work
    /// completed before cancellation is preserved via checkpoints.
    /// </para>
    /// </remarks>
    Task<BatchDeduplicationResult> ExecuteAsync(
        BatchDeduplicationOptions? options = null,
        IProgress<BatchProgress>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves the current status of a batch deduplication job.
    /// </summary>
    /// <param name="jobId">The unique identifier of the job to query.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// Current status including state, progress, and configuration.
    /// </returns>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when no job with the specified <paramref name="jobId"/> exists.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method queries the database for the job record and returns
    /// the current state. For running jobs, the status reflects the most
    /// recent checkpoint.
    /// </para>
    /// <para>
    /// <b>Performance:</b> This is a lightweight database query suitable
    /// for polling at regular intervals (e.g., every 1-5 seconds).
    /// </para>
    /// </remarks>
    Task<BatchJobStatus> GetStatusAsync(Guid jobId, CancellationToken ct = default);

    /// <summary>
    /// Cancels a running batch deduplication job.
    /// </summary>
    /// <param name="jobId">The unique identifier of the job to cancel.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// <c>true</c> if the job was successfully cancelled;
    /// <c>false</c> if the job was not running or already completed.
    /// </returns>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when no job with the specified <paramref name="jobId"/> exists.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Cancellation is not immediate. The running job will complete its
    /// current batch before stopping. A checkpoint is saved to enable
    /// potential resumption.
    /// </para>
    /// <para>
    /// The job state transitions to <see cref="BatchJobState.Cancelled"/>
    /// when cancellation completes.
    /// </para>
    /// </remarks>
    Task<bool> CancelAsync(Guid jobId, CancellationToken ct = default);

    /// <summary>
    /// Resumes a paused or interrupted batch deduplication job.
    /// </summary>
    /// <param name="jobId">The unique identifier of the job to resume.</param>
    /// <param name="progress">Optional progress reporter for resumed execution.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// Final result when the resumed job completes.
    /// </returns>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when no job with the specified <paramref name="jobId"/> exists.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when:
    /// <list type="bullet">
    ///   <item><description>The job is not in <see cref="BatchJobState.Paused"/> state.</description></item>
    ///   <item><description>The job has no valid checkpoint to resume from.</description></item>
    ///   <item><description>Teams license is not active.</description></item>
    /// </list>
    /// </exception>
    /// <remarks>
    /// <para>
    /// Resumption continues from the last saved checkpoint. Chunks processed
    /// before the interruption are not re-processed.
    /// </para>
    /// <para>
    /// The job uses the same options that were specified when it was created.
    /// Options cannot be changed during resumption.
    /// </para>
    /// </remarks>
    Task<BatchDeduplicationResult> ResumeAsync(
        Guid jobId,
        IProgress<BatchProgress>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Lists batch deduplication jobs matching the specified filter.
    /// </summary>
    /// <param name="filter">
    /// Filter criteria for the job listing.
    /// Uses <see cref="BatchJobFilter.All"/> if null.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// List of job records matching the filter, ordered by creation date descending.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method returns lightweight <see cref="BatchJobRecord"/> objects
    /// suitable for listing. Use <see cref="GetStatusAsync"/> for full details
    /// on a specific job.
    /// </para>
    /// <para>
    /// Results are ordered by <see cref="BatchJobRecord.CreatedAt"/> descending
    /// (newest first).
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<BatchJobRecord>> ListJobsAsync(
        BatchJobFilter? filter = null,
        CancellationToken ct = default);
}
