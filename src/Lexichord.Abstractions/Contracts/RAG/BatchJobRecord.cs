// =============================================================================
// File: BatchJobRecord.cs
// Project: Lexichord.Abstractions
// Description: Lightweight record for batch job history listing.
// =============================================================================
// VERSION: v0.5.9g (Batch Retroactive Deduplication)
// LOGIC: Provides summary information for job listings without full status details.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Lightweight record representing a batch deduplication job in history listings.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9g as part of the Batch Retroactive Deduplication feature.
/// </para>
/// <para>
/// This record provides summary information for job listings without the overhead
/// of full status details. Use <see cref="IBatchDeduplicationJob.GetStatusAsync"/>
/// to retrieve complete job status.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var jobs = await batchJob.ListJobsAsync(BatchJobFilter.All, ct);
/// 
/// foreach (var job in jobs)
/// {
///     Console.WriteLine($"{job.JobId}: {job.State} - {job.Label}");
///     Console.WriteLine($"  Processed: {job.ChunksProcessed:N0}, Merged: {job.MergedCount:N0}");
///     Console.WriteLine($"  Duration: {job.Duration}");
/// }
/// </code>
/// </example>
/// <param name="JobId">The unique identifier for this batch job.</param>
/// <param name="State">The final or current state of the job.</param>
/// <param name="ProjectId">The project this job was scoped to, or null for all projects.</param>
/// <param name="Label">User-defined label for the job, or null.</param>
/// <param name="DryRun">Whether this was a dry-run (preview only) job.</param>
/// <param name="ChunksProcessed">Number of chunks processed.</param>
/// <param name="MergedCount">Number of chunks merged.</param>
/// <param name="CreatedAt">When the job was created.</param>
/// <param name="StartedAt">When the job started, or null if never started.</param>
/// <param name="CompletedAt">When the job completed, or null if not completed.</param>
public record BatchJobRecord(
    Guid JobId,
    BatchJobState State,
    Guid? ProjectId,
    string? Label,
    bool DryRun,
    int ChunksProcessed,
    int MergedCount,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt)
{
    /// <summary>
    /// Gets the duration of the job execution.
    /// </summary>
    /// <remarks>
    /// Returns <c>null</c> if the job hasn't started yet.
    /// For running jobs, returns the time elapsed since start.
    /// For completed jobs, returns the total execution time.
    /// </remarks>
    public TimeSpan? Duration
    {
        get
        {
            if (!StartedAt.HasValue) return null;
            return (CompletedAt ?? DateTime.UtcNow) - StartedAt.Value;
        }
    }

    /// <summary>
    /// Gets whether the job completed successfully.
    /// </summary>
    public bool IsSuccess => State == BatchJobState.Completed;
}
