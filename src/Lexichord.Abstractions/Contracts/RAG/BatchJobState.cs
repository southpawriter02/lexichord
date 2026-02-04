// =============================================================================
// File: BatchJobState.cs
// Project: Lexichord.Abstractions
// Description: Enum defining the possible states for a batch deduplication job.
// =============================================================================
// VERSION: v0.5.9g (Batch Retroactive Deduplication)
// LOGIC: Defines the lifecycle states for tracking batch job progress.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Represents the lifecycle state of a batch deduplication job.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9g as part of the Batch Retroactive Deduplication feature.
/// </para>
/// <para>
/// State transitions follow this flow:
/// </para>
/// <code>
/// Pending → Running → Completed
///              ↓
///           Paused → Running (resume)
///              ↓
///           Cancelled
///              ↓
///           Failed
/// </code>
/// <para>
/// <b>Terminal States:</b> <see cref="Completed"/>, <see cref="Cancelled"/>, and <see cref="Failed"/>
/// are terminal states. Jobs in these states cannot transition to other states.
/// </para>
/// </remarks>
public enum BatchJobState
{
    /// <summary>
    /// Job has been created but not yet started.
    /// </summary>
    /// <remarks>
    /// Initial state for newly created jobs. Transitions to <see cref="Running"/>
    /// when <see cref="IBatchDeduplicationJob.ExecuteAsync"/> is called.
    /// </remarks>
    Pending = 0,

    /// <summary>
    /// Job is actively processing chunks.
    /// </summary>
    /// <remarks>
    /// The job is consuming resources and making progress. May transition to:
    /// <list type="bullet">
    ///   <item><description><see cref="Completed"/> when all chunks are processed.</description></item>
    ///   <item><description><see cref="Paused"/> when interrupted (system restart, user pause).</description></item>
    ///   <item><description><see cref="Cancelled"/> when user cancels.</description></item>
    ///   <item><description><see cref="Failed"/> on unrecoverable error.</description></item>
    /// </list>
    /// </remarks>
    Running = 1,

    /// <summary>
    /// Job execution was interrupted and can be resumed.
    /// </summary>
    /// <remarks>
    /// The job was stopped mid-execution but checkpoint data is preserved.
    /// Use <see cref="IBatchDeduplicationJob.ResumeAsync"/> to continue processing
    /// from the last checkpoint.
    /// </remarks>
    Paused = 2,

    /// <summary>
    /// Job completed successfully, processing all chunks.
    /// </summary>
    /// <remarks>
    /// Terminal state. All chunks in scope have been processed. Statistics
    /// are finalized and available in <see cref="BatchDeduplicationResult"/>.
    /// </remarks>
    Completed = 3,

    /// <summary>
    /// Job was cancelled by user request.
    /// </summary>
    /// <remarks>
    /// Terminal state. Processing stopped before completion due to explicit
    /// cancellation via <see cref="IBatchDeduplicationJob.CancelAsync"/> or
    /// via <see cref="CancellationToken"/>.
    /// </remarks>
    Cancelled = 4,

    /// <summary>
    /// Job failed due to an unrecoverable error.
    /// </summary>
    /// <remarks>
    /// Terminal state. An exception occurred that prevented further processing.
    /// Error details are available in <see cref="BatchJobStatus.ErrorMessage"/>
    /// and <see cref="BatchDeduplicationResult.ErrorMessage"/>.
    /// </remarks>
    Failed = 5,
}
