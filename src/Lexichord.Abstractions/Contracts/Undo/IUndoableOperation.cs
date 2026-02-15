// -----------------------------------------------------------------------
// <copyright file="IUndoableOperation.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Defines the contract for an operation that can be undone and redone.
//   Each operation has a unique identifier, display name for undo history UI,
//   and timestamp for chronological ordering. The three async methods represent
//   the operation lifecycle: initial execution, undo, and redo.
//
//   Implementations should capture all state needed to reverse the operation
//   in their constructor and store it immutably for undo/redo cycles.
//
//   Version: v0.7.3d (referenced as v0.1.4a in roadmap, first implemented here)
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.Undo;

/// <summary>
/// An operation that can be undone and redone.
/// </summary>
/// <remarks>
/// <para>
/// Implementations capture the complete state needed to reverse the operation
/// at construction time. The <see cref="ExecuteAsync"/> method performs the initial
/// operation, <see cref="UndoAsync"/> reverses it, and <see cref="RedoAsync"/>
/// re-applies it after an undo.
/// </para>
/// <para>
/// The <see cref="DisplayName"/> is shown in the undo history UI (e.g., "AI Rewrite (Formal)")
/// and the <see cref="Timestamp"/> enables chronological ordering of operations.
/// </para>
/// <para><b>Referenced as:</b> v0.1.4a in roadmap</para>
/// <para><b>First implemented in:</b> v0.7.3d</para>
/// </remarks>
/// <seealso cref="IUndoRedoService"/>
public interface IUndoableOperation
{
    /// <summary>
    /// Gets the unique identifier for this operation.
    /// </summary>
    /// <remarks>
    /// LOGIC: Generated at construction time (typically via <see cref="Guid.NewGuid"/>).
    /// Used for tracking, logging, and event correlation.
    /// </remarks>
    string Id { get; }

    /// <summary>
    /// Gets the display name shown in undo history.
    /// </summary>
    /// <remarks>
    /// LOGIC: Human-readable label for the undo/redo history UI.
    /// For rewrite operations, this follows the format "AI Rewrite ({Intent})".
    /// </remarks>
    string DisplayName { get; }

    /// <summary>
    /// Gets the UTC timestamp when the operation was performed.
    /// </summary>
    /// <remarks>
    /// LOGIC: Set at construction time. Used for chronological ordering
    /// in the undo history display (e.g., "2 minutes ago").
    /// </remarks>
    DateTime Timestamp { get; }

    /// <summary>
    /// Executes the operation (initial do or redo).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// LOGIC: Performs the forward operation (e.g., replacing original text
    /// with rewritten text). Called once during initial execution and
    /// may be called again during redo if the implementation delegates
    /// <see cref="RedoAsync"/> to this method.
    /// </remarks>
    Task ExecuteAsync(CancellationToken ct = default);

    /// <summary>
    /// Undoes the operation, restoring the previous state.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// LOGIC: Reverses the effect of <see cref="ExecuteAsync"/> by restoring
    /// the state captured at construction time (e.g., replacing rewritten text
    /// with the original text).
    /// </remarks>
    Task UndoAsync(CancellationToken ct = default);

    /// <summary>
    /// Redoes the operation after an undo.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// LOGIC: Re-applies the operation after it has been undone. Typically
    /// delegates to the same logic as <see cref="ExecuteAsync"/>.
    /// </remarks>
    Task RedoAsync(CancellationToken ct = default);
}
