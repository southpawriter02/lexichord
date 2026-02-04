// =============================================================================
// File: BatchDeduplicationCompletedEvent.cs
// Project: Lexichord.Abstractions
// Description: MediatR notification for batch deduplication job completion.
// =============================================================================
// VERSION: v0.5.9g (Batch Retroactive Deduplication)
// LOGIC: Published when a batch deduplication job completes (success or failure).
// =============================================================================

using Lexichord.Abstractions.Contracts.RAG;
using MediatR;

namespace Lexichord.Abstractions.Events;

/// <summary>
/// Published when a batch deduplication job completes execution.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9g as part of the Batch Retroactive Deduplication feature.
/// </para>
/// <para>
/// This event is published regardless of the final job state (success, cancelled, or failed).
/// Handlers can use <see cref="Result"/> to determine the outcome.
/// </para>
/// <para>
/// <b>Use Cases:</b>
/// </para>
/// <list type="bullet">
///   <item><description>Sending notifications to users when batch jobs complete.</description></item>
///   <item><description>Triggering follow-up actions (e.g., cache invalidation).</description></item>
///   <item><description>Logging job metrics to analytics systems.</description></item>
///   <item><description>Updating UI to reflect completed state.</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// public class BatchCompletionHandler : INotificationHandler&lt;BatchDeduplicationCompletedEvent&gt;
/// {
///     public Task Handle(BatchDeduplicationCompletedEvent notification, CancellationToken ct)
///     {
///         var result = notification.Result;
///         _logger.LogInformation(
///             "Batch job {JobId} completed with state {State}. Merged {Merged} chunks.",
///             result.JobId, result.FinalState, result.MergedCount);
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
/// <param name="Result">The final result of the completed batch job.</param>
public record BatchDeduplicationCompletedEvent(BatchDeduplicationResult Result) : INotification
{
    /// <summary>
    /// Gets the job ID from the result for convenience.
    /// </summary>
    public Guid JobId => Result.JobId;

    /// <summary>
    /// Gets whether the job completed successfully.
    /// </summary>
    public bool IsSuccess => Result.IsSuccess;

    /// <summary>
    /// Gets whether the job was cancelled.
    /// </summary>
    public bool IsCancelled => Result.IsCancelled;

    /// <summary>
    /// Gets whether the job failed.
    /// </summary>
    public bool IsFailed => Result.IsFailed;
}
