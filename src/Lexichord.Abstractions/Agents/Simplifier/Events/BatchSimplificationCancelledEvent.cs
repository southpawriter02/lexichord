// -----------------------------------------------------------------------
// <copyright file="BatchSimplificationCancelledEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using MediatR;

namespace Lexichord.Abstractions.Agents.Simplifier.Events;

/// <summary>
/// Published when a batch simplification operation is cancelled by the user.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This event is published by <see cref="IBatchSimplificationService"/>
/// when the operation is cancelled via <see cref="CancellationToken"/>. Subscribers
/// can use this event for:
/// </para>
/// <list type="bullet">
///   <item><description>Analytics tracking of cancellation patterns</description></item>
///   <item><description>Cleanup of temporary resources</description></item>
///   <item><description>Logging cancellation for audit trails</description></item>
///   <item><description>Updating UI state (hiding progress dialog)</description></item>
///   <item><description>Reverting partial changes if needed</description></item>
/// </list>
/// <para>
/// <b>Cancellation vs. Failure:</b>
/// This event is specifically for user-initiated cancellation. For errors or failures,
/// the batch service returns a result with <see cref="BatchSimplificationResult.ErrorMessage"/>
/// populated.
/// </para>
/// <para>
/// <b>Partial Results:</b>
/// When cancelled, paragraphs processed before cancellation may have already been
/// applied to the document. The <see cref="ProcessedParagraphs"/> count indicates
/// how many paragraphs were handled before cancellation.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.4d as part of the Batch Simplification feature.
/// </para>
/// </remarks>
/// <param name="DocumentPath">
/// The file path of the document that was being processed.
/// </param>
/// <param name="ProcessedParagraphs">
/// The number of paragraphs that were processed before cancellation.
/// Includes both simplified and skipped paragraphs.
/// </param>
/// <param name="TotalParagraphs">
/// The total number of paragraphs that would have been processed.
/// </param>
/// <param name="Reason">
/// A human-readable description of why the operation was cancelled.
/// </param>
/// <example>
/// <code>
/// // Publishing the event when user cancels
/// await _mediator.Publish(new BatchSimplificationCancelledEvent(
///     DocumentPath: "/path/to/document.md",
///     ProcessedParagraphs: 25,
///     TotalParagraphs: 63,
///     Reason: "User clicked Cancel"));
///
/// // Handling the event for cleanup
/// public class CancellationHandler : INotificationHandler&lt;BatchSimplificationCancelledEvent&gt;
/// {
///     public Task Handle(BatchSimplificationCancelledEvent e, CancellationToken ct)
///     {
///         _logger.LogWarning(
///             "Batch simplification cancelled at {Processed}/{Total}: {Reason}",
///             e.ProcessedParagraphs, e.TotalParagraphs, e.Reason);
///
///         _progressService.HideProgress();
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="SimplificationCompletedEvent"/>
/// <seealso cref="ParagraphSimplifiedEvent"/>
/// <seealso cref="IBatchSimplificationService"/>
public record BatchSimplificationCancelledEvent(
    string DocumentPath,
    int ProcessedParagraphs,
    int TotalParagraphs,
    string Reason) : INotification
{
    /// <summary>
    /// Standard reason for user-initiated cancellation via UI button.
    /// </summary>
    public const string ReasonUserCancelled = "User clicked Cancel";

    /// <summary>
    /// Standard reason for cancellation via keyboard shortcut.
    /// </summary>
    public const string ReasonEscapePressed = "User pressed Escape";

    /// <summary>
    /// Standard reason for cancellation due to document closure.
    /// </summary>
    public const string ReasonDocumentClosed = "Document was closed";

    /// <summary>
    /// Standard reason for cancellation due to application shutdown.
    /// </summary>
    public const string ReasonApplicationShutdown = "Application is shutting down";

    /// <summary>
    /// Gets the number of paragraphs that were not processed.
    /// </summary>
    /// <value>
    /// The count of paragraphs remaining when cancellation occurred.
    /// </value>
    public int RemainingParagraphs =>
        TotalParagraphs - ProcessedParagraphs;

    /// <summary>
    /// Gets the completion percentage at cancellation.
    /// </summary>
    /// <value>
    /// Progress as a percentage (0-100) based on paragraphs processed.
    /// </value>
    public double PercentComplete =>
        TotalParagraphs > 0
            ? (double)ProcessedParagraphs / TotalParagraphs * 100
            : 0;

    /// <summary>
    /// Gets a value indicating whether the cancellation occurred early.
    /// </summary>
    /// <value>
    /// <c>true</c> if less than 25% of paragraphs were processed;
    /// otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Early cancellation may indicate the user changed their mind
    /// quickly or realized they selected the wrong document/options.
    /// </remarks>
    public bool IsEarlyCancellation =>
        PercentComplete < 25;

    /// <summary>
    /// Gets a value indicating whether most of the work was completed.
    /// </summary>
    /// <value>
    /// <c>true</c> if 75% or more of paragraphs were processed;
    /// otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Late cancellation may indicate impatience with time remaining
    /// or discovery of an issue with the results.
    /// </remarks>
    public bool IsLateCancellation =>
        PercentComplete >= 75;

    /// <summary>
    /// Creates a cancellation event for user-initiated cancellation.
    /// </summary>
    /// <param name="documentPath">The document path.</param>
    /// <param name="processedParagraphs">Paragraphs processed before cancel.</param>
    /// <param name="totalParagraphs">Total paragraphs in scope.</param>
    /// <returns>A cancellation event with standard user cancellation reason.</returns>
    public static BatchSimplificationCancelledEvent UserCancelled(
        string documentPath,
        int processedParagraphs,
        int totalParagraphs) => new(
            documentPath,
            processedParagraphs,
            totalParagraphs,
            ReasonUserCancelled);

    /// <summary>
    /// Creates a cancellation event for document closure.
    /// </summary>
    /// <param name="documentPath">The document path.</param>
    /// <param name="processedParagraphs">Paragraphs processed before closure.</param>
    /// <param name="totalParagraphs">Total paragraphs in scope.</param>
    /// <returns>A cancellation event with document closed reason.</returns>
    public static BatchSimplificationCancelledEvent DocumentClosed(
        string documentPath,
        int processedParagraphs,
        int totalParagraphs) => new(
            documentPath,
            processedParagraphs,
            totalParagraphs,
            ReasonDocumentClosed);
}
