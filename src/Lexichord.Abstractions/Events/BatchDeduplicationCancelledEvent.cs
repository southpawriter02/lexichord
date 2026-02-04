// =============================================================================
// File: BatchDeduplicationCancelledEvent.cs
// Project: Lexichord.Abstractions
// Description: MediatR notification for batch deduplication job cancellation.
// =============================================================================
// VERSION: v0.5.9g (Batch Retroactive Deduplication)
// LOGIC: Published when a batch job is explicitly cancelled via CancelAsync.
// =============================================================================

using MediatR;

namespace Lexichord.Abstractions.Events;

/// <summary>
/// Published when a batch deduplication job is cancelled via explicit request.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9g as part of the Batch Retroactive Deduplication feature.
/// </para>
/// <para>
/// This event is published when <see cref="Contracts.RAG.IBatchDeduplicationJob.CancelAsync"/>
/// is called and the job successfully transitions to cancelled state.
/// </para>
/// <para>
/// Note: <see cref="BatchDeduplicationCompletedEvent"/> is also published for cancelled jobs.
/// This event provides additional context about the cancellation.
/// </para>
/// <para>
/// <b>Use Cases:</b>
/// </para>
/// <list type="bullet">
///   <item><description>Cleaning up resources allocated for the job.</description></item>
///   <item><description>Notifying users that their cancellation request was processed.</description></item>
///   <item><description>Logging cancellation for audit purposes.</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// public class CancellationHandler : INotificationHandler&lt;BatchDeduplicationCancelledEvent&gt;
/// {
///     public Task Handle(BatchDeduplicationCancelledEvent notification, CancellationToken ct)
///     {
///         _logger.LogWarning(
///             "Batch job {JobId} was cancelled at {Time}. Processed {Processed} chunks before cancellation.",
///             notification.JobId, notification.CancelledAt, notification.ChunksProcessedBeforeCancellation);
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
/// <param name="JobId">The unique identifier of the cancelled job.</param>
/// <param name="ChunksProcessedBeforeCancellation">Number of chunks processed before cancellation.</param>
/// <param name="CancelledAt">UTC timestamp when cancellation occurred.</param>
/// <param name="CanResume">Whether the job can be resumed from the last checkpoint.</param>
public record BatchDeduplicationCancelledEvent(
    Guid JobId,
    int ChunksProcessedBeforeCancellation,
    DateTime CancelledAt,
    bool CanResume) : INotification;
