// -----------------------------------------------------------------------
// <copyright file="RewritePreviewCommittedEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: MediatR notification published when a rewrite preview is committed
//   (v0.7.3d). Published by RewriteApplicator after the preview text is
//   finalized and pushed to the undo stack as a permanent change.
//
//   Consumers:
//     - Preview UI (hide preview indicator bar)
//     - Editor chrome (re-enable operations after preview)
// -----------------------------------------------------------------------

using MediatR;

namespace Lexichord.Modules.Agents.Editor.Events;

/// <summary>
/// Published when a rewrite preview is committed as a permanent change.
/// </summary>
/// <remarks>
/// <para>
/// Published by <see cref="RewriteApplicator"/> after <see cref="IRewriteApplicator.CommitPreviewAsync"/>
/// finalizes the preview text and pushes an undo entry. The text that was previously
/// shown as a preview is now a committed change that can be undone with Ctrl+Z.
/// </para>
/// <para><b>Introduced in:</b> v0.7.3d</para>
/// </remarks>
/// <param name="DocumentPath">Path to the document where the preview was committed.</param>
/// <param name="OperationId">Unique identifier of the committed undoable operation.</param>
/// <param name="Timestamp">UTC timestamp when the event was created.</param>
public record RewritePreviewCommittedEvent(
    string DocumentPath,
    string OperationId,
    DateTime Timestamp) : INotification
{
    /// <summary>
    /// Creates a new <see cref="RewritePreviewCommittedEvent"/> with the current UTC timestamp.
    /// </summary>
    /// <param name="documentPath">Path to the document where the preview was committed.</param>
    /// <param name="operationId">Unique identifier of the committed undoable operation.</param>
    /// <returns>A new <see cref="RewritePreviewCommittedEvent"/>.</returns>
    public static RewritePreviewCommittedEvent Create(
        string documentPath,
        string operationId) =>
        new(documentPath, operationId, DateTime.UtcNow);
}
