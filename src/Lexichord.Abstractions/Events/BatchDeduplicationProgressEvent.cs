// =============================================================================
// File: BatchDeduplicationProgressEvent.cs
// Project: Lexichord.Abstractions
// Description: MediatR notification for batch deduplication job progress updates.
// =============================================================================
// VERSION: v0.5.9g (Batch Retroactive Deduplication)
// LOGIC: Published periodically during batch processing for real-time monitoring.
// =============================================================================

using Lexichord.Abstractions.Contracts.RAG;
using MediatR;

namespace Lexichord.Abstractions.Events;

/// <summary>
/// Published periodically during batch deduplication job execution.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9g as part of the Batch Retroactive Deduplication feature.
/// </para>
/// <para>
/// This event is published after each batch completes, providing real-time visibility
/// into job progress. It complements the <see cref="IProgress{T}"/> parameter by
/// enabling system-wide handlers to monitor batch job progress.
/// </para>
/// <para>
/// <b>Publishing Frequency:</b> Published after each batch completes, which means
/// approximately every <see cref="BatchDeduplicationOptions.BatchSize"/> chunks.
/// </para>
/// <para>
/// <b>Use Cases:</b>
/// </para>
/// <list type="bullet">
///   <item><description>Broadcasting progress to multiple UI components via SignalR.</description></item>
///   <item><description>Logging progress milestones (e.g., 25%, 50%, 75%).</description></item>
///   <item><description>Updating real-time dashboards.</description></item>
///   <item><description>Monitoring batch job health across the system.</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// public class ProgressBroadcastHandler : INotificationHandler&lt;BatchDeduplicationProgressEvent&gt;
/// {
///     private readonly IHubContext&lt;BatchJobHub&gt; _hub;
///     
///     public async Task Handle(BatchDeduplicationProgressEvent notification, CancellationToken ct)
///     {
///         await _hub.Clients.Group($"job-{notification.JobId}")
///             .SendAsync("ProgressUpdate", notification.Progress, ct);
///     }
/// }
/// </code>
/// </example>
/// <param name="JobId">The unique identifier of the job reporting progress.</param>
/// <param name="Progress">The current progress snapshot.</param>
/// <param name="MergedInThisBatch">Number of chunks merged in the most recent batch.</param>
/// <param name="TotalMergedSoFar">Total chunks merged across all batches so far.</param>
public record BatchDeduplicationProgressEvent(
    Guid JobId,
    BatchProgress Progress,
    int MergedInThisBatch,
    int TotalMergedSoFar) : INotification
{
    /// <summary>
    /// Gets the completion percentage for convenience.
    /// </summary>
    public double? PercentComplete => Progress.PercentComplete;

    /// <summary>
    /// Gets whether this is a major milestone (25%, 50%, 75%, or 100%).
    /// </summary>
    public bool IsMilestone
    {
        get
        {
            if (!Progress.PercentComplete.HasValue) return false;
            var percent = Progress.PercentComplete.Value * 100;
            return percent is >= 24.5 and <= 25.5
                or >= 49.5 and <= 50.5
                or >= 74.5 and <= 75.5
                or >= 99.5;
        }
    }
}
