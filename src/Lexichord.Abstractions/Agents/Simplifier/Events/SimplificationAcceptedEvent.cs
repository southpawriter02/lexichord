// -----------------------------------------------------------------------
// <copyright file="SimplificationAcceptedEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using MediatR;

namespace Lexichord.Abstractions.Agents.Simplifier.Events;

/// <summary>
/// Published when the user accepts simplification changes in the preview UI.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This event is published by the <c>SimplificationPreviewViewModel</c>
/// when the user clicks "Accept All" or "Accept Selected" to apply simplification
/// changes to the document. Subscribers can use this event for:
/// </para>
/// <list type="bullet">
///   <item><description>Analytics tracking of simplification acceptance rates</description></item>
///   <item><description>Updating document state or metadata</description></item>
///   <item><description>Logging for audit trails</description></item>
///   <item><description>Triggering follow-up actions (e.g., re-running linting)</description></item>
/// </list>
/// <para>
/// <b>Partial Acceptance:</b>
/// When <see cref="AcceptedChangeCount"/> differs from <see cref="TotalChangeCount"/>,
/// the user selectively accepted only some changes. Use <see cref="IsPartialAcceptance"/>
/// to check for this condition.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.4c as part of the Simplifier Agent Preview/Diff UI.
/// </para>
/// </remarks>
/// <param name="DocumentPath">
/// The file path of the document where changes were applied.
/// May be <c>null</c> for untitled documents.
/// </param>
/// <param name="OriginalText">
/// The original text before simplification was applied.
/// Stored for undo/audit purposes.
/// </param>
/// <param name="SimplifiedText">
/// The text that was inserted into the document.
/// For partial acceptance, this is the merged result of selected changes.
/// </param>
/// <param name="AcceptedChangeCount">
/// The number of individual changes that were accepted.
/// </param>
/// <param name="TotalChangeCount">
/// The total number of changes that were available.
/// </param>
/// <param name="GradeLevelReduction">
/// The improvement in Flesch-Kincaid grade level achieved.
/// Positive values indicate easier reading (lower grade level).
/// </param>
/// <example>
/// <code>
/// // Publishing the event after accepting changes
/// await _mediator.Publish(new SimplificationAcceptedEvent(
///     DocumentPath: "/path/to/document.md",
///     OriginalText: "The complex original text...",
///     SimplifiedText: "The simple text...",
///     AcceptedChangeCount: 5,
///     TotalChangeCount: 7,
///     GradeLevelReduction: 4.2));
///
/// // Handling the event
/// public class SimplificationAnalyticsHandler : INotificationHandler&lt;SimplificationAcceptedEvent&gt;
/// {
///     public Task Handle(SimplificationAcceptedEvent notification, CancellationToken ct)
///     {
///         var acceptanceRate = (double)notification.AcceptedChangeCount / notification.TotalChangeCount;
///         _analytics.TrackSimplificationAccepted(acceptanceRate, notification.GradeLevelReduction);
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="SimplificationRejectedEvent"/>
/// <seealso cref="ResimplificationRequestedEvent"/>
public record SimplificationAcceptedEvent(
    string? DocumentPath,
    string OriginalText,
    string SimplifiedText,
    int AcceptedChangeCount,
    int TotalChangeCount,
    double GradeLevelReduction) : INotification
{
    /// <summary>
    /// Gets a value indicating whether the user accepted only some of the available changes.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="AcceptedChangeCount"/> is less than <see cref="TotalChangeCount"/>;
    /// otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Partial acceptance occurs when the user reviews changes individually
    /// and deselects some before accepting. This is useful for tracking user behavior
    /// and identifying changes that are frequently rejected.
    /// </remarks>
    public bool IsPartialAcceptance => AcceptedChangeCount < TotalChangeCount;

    /// <summary>
    /// Gets the acceptance rate as a percentage (0.0 to 1.0).
    /// </summary>
    /// <value>
    /// The ratio of accepted changes to total changes.
    /// Returns 1.0 if <see cref="TotalChangeCount"/> is zero.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Useful for analytics to track how often users accept AI-generated
    /// simplification suggestions. Low acceptance rates may indicate prompt tuning needs.
    /// </remarks>
    public double AcceptanceRate => TotalChangeCount > 0
        ? (double)AcceptedChangeCount / TotalChangeCount
        : 1.0;
}
