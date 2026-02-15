// -----------------------------------------------------------------------
// <copyright file="RewritePreviewStartedEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: MediatR notification published when a rewrite preview is started
//   (v0.7.3d). Published by RewriteApplicator after the preview text is
//   applied to the document (but NOT pushed to the undo stack).
//
//   Consumers:
//     - Preview UI (show preview indicator bar, countdown timer)
//     - Editor chrome (disable conflicting operations during preview)
// -----------------------------------------------------------------------

using MediatR;

namespace Lexichord.Modules.Agents.Editor.Events;

/// <summary>
/// Published when a rewrite preview is started in the document.
/// </summary>
/// <remarks>
/// <para>
/// Published by <see cref="RewriteApplicator"/> after the preview text replaces
/// the selected text. The preview is temporary and must be committed
/// (<see cref="RewritePreviewCommittedEvent"/>) or cancelled
/// (<see cref="RewritePreviewCancelledEvent"/>).
/// </para>
/// <para><b>Introduced in:</b> v0.7.3d</para>
/// </remarks>
/// <param name="DocumentPath">Path to the document showing the preview.</param>
/// <param name="PreviewText">The preview text that was applied.</param>
/// <param name="Timestamp">UTC timestamp when the event was created.</param>
public record RewritePreviewStartedEvent(
    string DocumentPath,
    string PreviewText,
    DateTime Timestamp) : INotification
{
    /// <summary>
    /// Creates a new <see cref="RewritePreviewStartedEvent"/> with the current UTC timestamp.
    /// </summary>
    /// <param name="documentPath">Path to the document showing the preview.</param>
    /// <param name="previewText">The preview text that was applied.</param>
    /// <returns>A new <see cref="RewritePreviewStartedEvent"/>.</returns>
    public static RewritePreviewStartedEvent Create(
        string documentPath,
        string previewText) =>
        new(documentPath, previewText, DateTime.UtcNow);
}
