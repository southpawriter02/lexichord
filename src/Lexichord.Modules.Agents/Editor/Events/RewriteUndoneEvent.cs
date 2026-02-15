// -----------------------------------------------------------------------
// <copyright file="RewriteUndoneEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: MediatR notification published when a rewrite is undone (v0.7.3d).
//   Published by IUndoRedoService after the RewriteUndoableOperation.UndoAsync()
//   completes, restoring the original text.
//
//   Consumers:
//     - Analytics/telemetry handlers (undo tracking, accept vs. undo ratio)
//     - Undo history UI (state update)
// -----------------------------------------------------------------------

using MediatR;

namespace Lexichord.Modules.Agents.Editor.Events;

/// <summary>
/// Published when a rewrite operation is undone, restoring the original text.
/// </summary>
/// <remarks>
/// <para>
/// Published after <see cref="Lexichord.Abstractions.Contracts.Undo.IUndoableOperation.UndoAsync"/>
/// completes for a rewrite operation. Used for analytics (accept vs. undo ratio)
/// and UI state updates.
/// </para>
/// <para><b>Introduced in:</b> v0.7.3d</para>
/// </remarks>
/// <param name="DocumentPath">Path to the document that was modified.</param>
/// <param name="OperationId">Unique identifier of the undone operation.</param>
/// <param name="Timestamp">UTC timestamp when the event was created.</param>
public record RewriteUndoneEvent(
    string DocumentPath,
    string OperationId,
    DateTime Timestamp) : INotification
{
    /// <summary>
    /// Creates a new <see cref="RewriteUndoneEvent"/> with the current UTC timestamp.
    /// </summary>
    /// <param name="documentPath">Path to the document that was modified.</param>
    /// <param name="operationId">Unique identifier of the undone operation.</param>
    /// <returns>A new <see cref="RewriteUndoneEvent"/>.</returns>
    public static RewriteUndoneEvent Create(
        string documentPath,
        string operationId) =>
        new(documentPath, operationId, DateTime.UtcNow);
}
