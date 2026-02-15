// -----------------------------------------------------------------------
// <copyright file="IUndoRedoService.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Manages the undo/redo operation stacks for the application.
//   Provides push, undo, redo, and clear operations on a stack of
//   IUndoableOperation instances. Publishes UndoStackChanged events
//   for UI binding (CanUndo, CanRedo state).
//
//   This is a higher-level undo system that sits above the editor's
//   built-in undo (BeginUndoGroup/EndUndoGroup). The editor's undo
//   handles atomic text changes while this service tracks labeled
//   operations for history display and agent integration.
//
//   Version: v0.7.3d (referenced as v0.1.4a in roadmap, first defined here)
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.Undo;

/// <summary>
/// Manages undo/redo operation stacks for labeled, reversible operations.
/// </summary>
/// <remarks>
/// <para>
/// This service provides a higher-level undo system that tracks labeled operations
/// (e.g., "AI Rewrite (Formal)") for history display. It complements the editor's
/// built-in undo system (<see cref="Editor.IEditorService.BeginUndoGroup"/>/<see cref="Editor.IEditorService.EndUndoGroup"/>)
/// which handles atomic text changes at the editor level.
/// </para>
/// <para>
/// Operations are pushed after execution via <see cref="Push"/>. When the user triggers
/// undo, the service pops the most recent operation and calls its
/// <see cref="IUndoableOperation.UndoAsync"/> method. Redo re-applies undone operations
/// via <see cref="IUndoableOperation.RedoAsync"/>.
/// </para>
/// <para><b>Referenced as:</b> v0.1.4a in roadmap</para>
/// <para><b>First defined in:</b> v0.7.3d</para>
/// </remarks>
/// <seealso cref="IUndoableOperation"/>
/// <seealso cref="UndoRedoChangedEventArgs"/>
public interface IUndoRedoService
{
    /// <summary>
    /// Gets whether there are operations that can be undone.
    /// </summary>
    /// <remarks>
    /// LOGIC: Returns true when the undo stack is non-empty. Used by UI
    /// to enable/disable the Undo command (Ctrl+Z).
    /// </remarks>
    bool CanUndo { get; }

    /// <summary>
    /// Gets whether there are operations that can be redone.
    /// </summary>
    /// <remarks>
    /// LOGIC: Returns true when the redo stack is non-empty. The redo stack
    /// is cleared when a new operation is pushed. Used by UI to enable/disable
    /// the Redo command (Ctrl+Y).
    /// </remarks>
    bool CanRedo { get; }

    /// <summary>
    /// Pushes an already-executed operation onto the undo stack.
    /// </summary>
    /// <param name="operation">The operation to push. Must have already been executed.</param>
    /// <remarks>
    /// LOGIC: Adds the operation to the top of the undo stack and clears the
    /// redo stack (new operations invalidate the redo history). Raises
    /// <see cref="UndoStackChanged"/> after modification.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="operation"/> is null.</exception>
    void Push(IUndoableOperation operation);

    /// <summary>
    /// Undoes the most recent operation.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous undo operation.</returns>
    /// <remarks>
    /// LOGIC: Pops the most recent operation from the undo stack, calls
    /// <see cref="IUndoableOperation.UndoAsync"/>, and pushes the operation
    /// onto the redo stack. Raises <see cref="UndoStackChanged"/> after completion.
    /// No-op if <see cref="CanUndo"/> is false.
    /// </remarks>
    Task UndoAsync(CancellationToken ct = default);

    /// <summary>
    /// Redoes the most recently undone operation.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous redo operation.</returns>
    /// <remarks>
    /// LOGIC: Pops the most recent operation from the redo stack, calls
    /// <see cref="IUndoableOperation.RedoAsync"/>, and pushes the operation
    /// back onto the undo stack. Raises <see cref="UndoStackChanged"/> after completion.
    /// No-op if <see cref="CanRedo"/> is false.
    /// </remarks>
    Task RedoAsync(CancellationToken ct = default);

    /// <summary>
    /// Clears both undo and redo stacks.
    /// </summary>
    /// <remarks>
    /// LOGIC: Removes all operations from both stacks. Used when switching
    /// documents or resetting state. Raises <see cref="UndoStackChanged"/>
    /// after clearing.
    /// </remarks>
    void Clear();

    /// <summary>
    /// Event raised when the undo or redo stack changes.
    /// </summary>
    /// <remarks>
    /// LOGIC: Raised after every <see cref="Push"/>, <see cref="UndoAsync"/>,
    /// <see cref="RedoAsync"/>, or <see cref="Clear"/> call. Used by UI to
    /// update CanUndo/CanRedo command states and undo history display.
    /// </remarks>
    event EventHandler<UndoRedoChangedEventArgs>? UndoStackChanged;
}
