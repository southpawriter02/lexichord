// -----------------------------------------------------------------------
// <copyright file="SimplificationCompletedEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using MediatR;

namespace Lexichord.Abstractions.Agents.Simplifier.Events;

/// <summary>
/// Published when a batch simplification operation completes.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This event is published by <see cref="IBatchSimplificationService"/>
/// after all paragraphs have been processed (or the operation was cancelled/failed).
/// Subscribers can use this event for:
/// </para>
/// <list type="bullet">
///   <item><description>Analytics tracking of batch operations</description></item>
///   <item><description>Logging completion for audit trails</description></item>
///   <item><description>Updating UI state (closing progress dialog)</description></item>
///   <item><description>Triggering follow-up actions (e.g., document save prompt)</description></item>
///   <item><description>Cost tracking and usage monitoring</description></item>
/// </list>
/// <para>
/// <b>Terminal States:</b>
/// This event is published regardless of how the operation ended:
/// <list type="bullet">
///   <item><description>Successful completion: <see cref="WasCancelled"/> = false</description></item>
///   <item><description>User cancellation: <see cref="WasCancelled"/> = true</description></item>
/// </list>
/// For failures, see <see cref="BatchSimplificationCancelledEvent"/>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.4d as part of the Batch Simplification feature.
/// </para>
/// </remarks>
/// <param name="DocumentPath">
/// The file path of the document that was processed.
/// </param>
/// <param name="ParagraphsSimplified">
/// The number of paragraphs that were successfully simplified.
/// </param>
/// <param name="ParagraphsSkipped">
/// The number of paragraphs that were skipped due to skip conditions.
/// </param>
/// <param name="TotalParagraphs">
/// The total number of paragraphs in the document/selection.
/// </param>
/// <param name="GradeLevelReduction">
/// The document-wide improvement in Flesch-Kincaid grade level.
/// Positive values indicate easier reading (lower grade level).
/// </param>
/// <param name="ProcessingTime">
/// The total elapsed time for the batch operation.
/// </param>
/// <param name="TokenUsage">
/// Aggregated token usage metrics for the entire batch.
/// </param>
/// <param name="WasCancelled">
/// <c>true</c> if the operation was cancelled by the user before completion;
/// otherwise, <c>false</c>.
/// </param>
/// <example>
/// <code>
/// // Publishing the event after batch completion
/// await _mediator.Publish(new SimplificationCompletedEvent(
///     DocumentPath: "/path/to/document.md",
///     ParagraphsSimplified: 42,
///     ParagraphsSkipped: 21,
///     TotalParagraphs: 63,
///     GradeLevelReduction: 4.5,
///     ProcessingTime: TimeSpan.FromMinutes(1.5),
///     TokenUsage: aggregateUsage,
///     WasCancelled: false));
///
/// // Handling the event for analytics
/// public class BatchAnalyticsHandler : INotificationHandler&lt;SimplificationCompletedEvent&gt;
/// {
///     public Task Handle(SimplificationCompletedEvent notification, CancellationToken ct)
///     {
///         _analytics.TrackBatchCompletion(
///             paragraphs: notification.ParagraphsSimplified,
///             gradeReduction: notification.GradeLevelReduction,
///             tokens: notification.TokenUsage.TotalTokens,
///             cancelled: notification.WasCancelled);
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="ParagraphSimplifiedEvent"/>
/// <seealso cref="BatchSimplificationCancelledEvent"/>
/// <seealso cref="IBatchSimplificationService"/>
public record SimplificationCompletedEvent(
    string DocumentPath,
    int ParagraphsSimplified,
    int ParagraphsSkipped,
    int TotalParagraphs,
    double GradeLevelReduction,
    TimeSpan ProcessingTime,
    UsageMetrics TokenUsage,
    bool WasCancelled) : INotification
{
    /// <summary>
    /// Gets the number of paragraphs that were actually processed.
    /// </summary>
    /// <value>
    /// The sum of <see cref="ParagraphsSimplified"/> and <see cref="ParagraphsSkipped"/>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> This may be less than <see cref="TotalParagraphs"/> if the
    /// operation was cancelled before completion.
    /// </remarks>
    public int ParagraphsProcessed => ParagraphsSimplified + ParagraphsSkipped;

    /// <summary>
    /// Gets the simplification rate as a percentage.
    /// </summary>
    /// <value>
    /// The ratio of simplified paragraphs to total processed, as a percentage (0-100).
    /// Returns 0 if no paragraphs were processed.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Useful for analytics to track what percentage of content
    /// typically benefits from simplification.
    /// </remarks>
    public double SimplificationRate =>
        ParagraphsProcessed > 0
            ? (double)ParagraphsSimplified / ParagraphsProcessed * 100
            : 0;

    /// <summary>
    /// Gets the completion rate as a percentage.
    /// </summary>
    /// <value>
    /// The ratio of processed paragraphs to total paragraphs, as a percentage (0-100).
    /// Returns 100 if <see cref="TotalParagraphs"/> is zero.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Less than 100% indicates the operation was cancelled
    /// before processing all paragraphs.
    /// </remarks>
    public double CompletionRate =>
        TotalParagraphs > 0
            ? (double)ParagraphsProcessed / TotalParagraphs * 100
            : 100;

    /// <summary>
    /// Gets the average time per paragraph.
    /// </summary>
    /// <value>
    /// The mean processing time per paragraph.
    /// Returns <see cref="TimeSpan.Zero"/> if no paragraphs were processed.
    /// </value>
    public TimeSpan AverageTimePerParagraph =>
        ParagraphsProcessed > 0
            ? ProcessingTime / ParagraphsProcessed
            : TimeSpan.Zero;

    /// <summary>
    /// Gets a value indicating whether the operation completed fully.
    /// </summary>
    /// <value>
    /// <c>true</c> if all paragraphs were processed (not cancelled);
    /// otherwise, <c>false</c>.
    /// </value>
    public bool IsComplete =>
        !WasCancelled && ParagraphsProcessed == TotalParagraphs;
}
