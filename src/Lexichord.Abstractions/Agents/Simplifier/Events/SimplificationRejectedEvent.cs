// -----------------------------------------------------------------------
// <copyright file="SimplificationRejectedEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using MediatR;

namespace Lexichord.Abstractions.Agents.Simplifier.Events;

/// <summary>
/// Published when the user rejects simplification changes in the preview UI.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This event is published by the <c>SimplificationPreviewViewModel</c>
/// when the user clicks "Reject" or closes the preview without accepting changes.
/// Subscribers can use this event for:
/// </para>
/// <list type="bullet">
///   <item><description>Analytics tracking of rejection reasons</description></item>
///   <item><description>Logging for debugging and improvement</description></item>
///   <item><description>Cleanup of temporary preview state</description></item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.4c as part of the Simplifier Agent Preview/Diff UI.
/// </para>
/// </remarks>
/// <param name="DocumentPath">
/// The file path of the document where simplification was rejected.
/// May be <c>null</c> for untitled documents.
/// </param>
/// <param name="Reason">
/// An optional reason for rejection. Standard values include:
/// <list type="bullet">
///   <item><description><c>"User cancelled"</c> — User clicked Reject or pressed Escape</description></item>
///   <item><description><c>"Preview closed"</c> — User closed the preview panel</description></item>
///   <item><description><c>"Document closed"</c> — The target document was closed</description></item>
///   <item><description><c>"License expired"</c> — License validation failed during review</description></item>
/// </list>
/// </param>
/// <example>
/// <code>
/// // Publishing the event when user rejects
/// await _mediator.Publish(new SimplificationRejectedEvent(
///     DocumentPath: "/path/to/document.md",
///     Reason: "User cancelled"));
///
/// // Handling the event for analytics
/// public class RejectionAnalyticsHandler : INotificationHandler&lt;SimplificationRejectedEvent&gt;
/// {
///     public Task Handle(SimplificationRejectedEvent notification, CancellationToken ct)
///     {
///         _analytics.TrackSimplificationRejected(notification.Reason);
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="SimplificationAcceptedEvent"/>
/// <seealso cref="ResimplificationRequestedEvent"/>
public record SimplificationRejectedEvent(
    string? DocumentPath,
    string? Reason) : INotification
{
    /// <summary>
    /// Standard reason for user-initiated cancellation.
    /// </summary>
    public const string ReasonUserCancelled = "User cancelled";

    /// <summary>
    /// Standard reason when preview panel is closed.
    /// </summary>
    public const string ReasonPreviewClosed = "Preview closed";

    /// <summary>
    /// Standard reason when document is closed during review.
    /// </summary>
    public const string ReasonDocumentClosed = "Document closed";

    /// <summary>
    /// Standard reason for license validation failure.
    /// </summary>
    public const string ReasonLicenseExpired = "License expired";

    /// <summary>
    /// Creates a new rejection event for user cancellation.
    /// </summary>
    /// <param name="documentPath">The document path.</param>
    /// <returns>A new <see cref="SimplificationRejectedEvent"/> with the standard cancellation reason.</returns>
    public static SimplificationRejectedEvent UserCancelled(string? documentPath) =>
        new(documentPath, ReasonUserCancelled);

    /// <summary>
    /// Creates a new rejection event for preview panel closure.
    /// </summary>
    /// <param name="documentPath">The document path.</param>
    /// <returns>A new <see cref="SimplificationRejectedEvent"/> with the standard closure reason.</returns>
    public static SimplificationRejectedEvent PreviewClosed(string? documentPath) =>
        new(documentPath, ReasonPreviewClosed);
}
