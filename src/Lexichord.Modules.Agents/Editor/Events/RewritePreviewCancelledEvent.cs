// -----------------------------------------------------------------------
// <copyright file="RewritePreviewCancelledEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: MediatR notification published when a rewrite preview is cancelled
//   (v0.7.3d). Published by RewriteApplicator after the original text is
//   restored and the preview state is cleared. No undo entry is created.
//
//   Consumers:
//     - Preview UI (hide preview indicator bar)
//     - Editor chrome (re-enable operations after preview)
// -----------------------------------------------------------------------

using MediatR;

namespace Lexichord.Modules.Agents.Editor.Events;

/// <summary>
/// Published when a rewrite preview is cancelled and the original text is restored.
/// </summary>
/// <remarks>
/// <para>
/// Published by <see cref="RewriteApplicator"/> after <see cref="IRewriteApplicator.CancelPreviewAsync"/>
/// restores the original text. Also published when a preview times out (5-minute limit).
/// No undo entry is created for cancelled previews.
/// </para>
/// <para><b>Introduced in:</b> v0.7.3d</para>
/// </remarks>
/// <param name="DocumentPath">Path to the document where the preview was cancelled.</param>
/// <param name="Timestamp">UTC timestamp when the event was created.</param>
public record RewritePreviewCancelledEvent(
    string DocumentPath,
    DateTime Timestamp) : INotification
{
    /// <summary>
    /// Creates a new <see cref="RewritePreviewCancelledEvent"/> with the current UTC timestamp.
    /// </summary>
    /// <param name="documentPath">Path to the document where the preview was cancelled.</param>
    /// <returns>A new <see cref="RewritePreviewCancelledEvent"/>.</returns>
    public static RewritePreviewCancelledEvent Create(
        string documentPath) =>
        new(documentPath, DateTime.UtcNow);
}
