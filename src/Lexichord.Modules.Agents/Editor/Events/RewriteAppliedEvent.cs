// -----------------------------------------------------------------------
// <copyright file="RewriteAppliedEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: MediatR notification published when a rewrite is successfully applied
//   to the document (v0.7.3d). Published by RewriteApplicator after the
//   RewriteUndoableOperation executes and is pushed to the undo stack.
//
//   Consumers:
//     - Analytics/telemetry handlers (rewrite tracking)
//     - Undo history UI (display applied rewrites)
//     - Future: rewrite statistics panel
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents.Editor;
using MediatR;

namespace Lexichord.Modules.Agents.Editor.Events;

/// <summary>
/// Published when a rewrite is successfully applied to the document.
/// </summary>
/// <remarks>
/// <para>
/// Published by <see cref="RewriteApplicator"/> after the text replacement succeeds
/// and the operation is pushed to the undo stack. Carries the full rewrite details
/// for observability and downstream processing.
/// </para>
/// <para><b>Introduced in:</b> v0.7.3d</para>
/// </remarks>
/// <param name="DocumentPath">Path to the document that was modified.</param>
/// <param name="OriginalText">The original text that was replaced.</param>
/// <param name="RewrittenText">The new text that replaced the original.</param>
/// <param name="Intent">The rewrite intent that was applied.</param>
/// <param name="OperationId">Unique identifier of the undoable operation.</param>
/// <param name="Timestamp">UTC timestamp when the event was created.</param>
public record RewriteAppliedEvent(
    string DocumentPath,
    string OriginalText,
    string RewrittenText,
    RewriteIntent Intent,
    string OperationId,
    DateTime Timestamp) : INotification
{
    /// <summary>
    /// Creates a new <see cref="RewriteAppliedEvent"/> with the current UTC timestamp.
    /// </summary>
    /// <param name="documentPath">Path to the document that was modified.</param>
    /// <param name="originalText">The original text that was replaced.</param>
    /// <param name="rewrittenText">The new text that replaced the original.</param>
    /// <param name="intent">The rewrite intent that was applied.</param>
    /// <param name="operationId">Unique identifier of the undoable operation.</param>
    /// <returns>A new <see cref="RewriteAppliedEvent"/>.</returns>
    public static RewriteAppliedEvent Create(
        string documentPath,
        string originalText,
        string rewrittenText,
        RewriteIntent intent,
        string operationId) =>
        new(documentPath, originalText, rewrittenText, intent, operationId, DateTime.UtcNow);
}
