// -----------------------------------------------------------------------
// <copyright file="RewriteRedoneEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: MediatR notification published when a rewrite is redone (v0.7.3d).
//   Published by IUndoRedoService after the RewriteUndoableOperation.RedoAsync()
//   completes, reapplying the rewritten text.
//
//   Consumers:
//     - Analytics/telemetry handlers (redo tracking)
//     - Undo history UI (state update)
// -----------------------------------------------------------------------

using MediatR;

namespace Lexichord.Modules.Agents.Editor.Events;

/// <summary>
/// Published when a previously undone rewrite operation is redone.
/// </summary>
/// <remarks>
/// <para>
/// Published after <see cref="Lexichord.Abstractions.Contracts.Undo.IUndoableOperation.RedoAsync"/>
/// completes for a rewrite operation, reapplying the rewritten text. Used for
/// analytics and UI state updates.
/// </para>
/// <para><b>Introduced in:</b> v0.7.3d</para>
/// </remarks>
/// <param name="DocumentPath">Path to the document that was modified.</param>
/// <param name="OperationId">Unique identifier of the redone operation.</param>
/// <param name="Timestamp">UTC timestamp when the event was created.</param>
public record RewriteRedoneEvent(
    string DocumentPath,
    string OperationId,
    DateTime Timestamp) : INotification
{
    /// <summary>
    /// Creates a new <see cref="RewriteRedoneEvent"/> with the current UTC timestamp.
    /// </summary>
    /// <param name="documentPath">Path to the document that was modified.</param>
    /// <param name="operationId">Unique identifier of the redone operation.</param>
    /// <returns>A new <see cref="RewriteRedoneEvent"/>.</returns>
    public static RewriteRedoneEvent Create(
        string documentPath,
        string operationId) =>
        new(documentPath, operationId, DateTime.UtcNow);
}
