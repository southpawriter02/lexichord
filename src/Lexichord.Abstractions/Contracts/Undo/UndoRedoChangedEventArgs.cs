// -----------------------------------------------------------------------
// <copyright file="UndoRedoChangedEventArgs.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Event arguments for the IUndoRedoService.UndoStackChanged event.
//   Carries the current state of the undo/redo stacks so subscribers can
//   update UI state (enable/disable undo/redo commands, display last
//   operation name in tooltip).
//
//   Version: v0.7.3d
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.Undo;

/// <summary>
/// Event arguments for <see cref="IUndoRedoService.UndoStackChanged"/>.
/// </summary>
/// <remarks>
/// <para>
/// Carries the current state of the undo/redo stacks after a modification.
/// Subscribers use this to update UI state (e.g., enable/disable Undo/Redo
/// buttons, display the last operation name in an "Undo {Name}" tooltip).
/// </para>
/// <para><b>Introduced in:</b> v0.7.3d</para>
/// </remarks>
public class UndoRedoChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of <see cref="UndoRedoChangedEventArgs"/>.
    /// </summary>
    /// <param name="canUndo">Whether there are operations that can be undone.</param>
    /// <param name="canRedo">Whether there are operations that can be redone.</param>
    /// <param name="lastOperationName">
    /// The display name of the most recent undoable operation, or null if the undo stack is empty.
    /// </param>
    public UndoRedoChangedEventArgs(bool canUndo, bool canRedo, string? lastOperationName)
    {
        CanUndo = canUndo;
        CanRedo = canRedo;
        LastOperationName = lastOperationName;
    }

    /// <summary>
    /// Gets whether there are operations that can be undone.
    /// </summary>
    /// <remarks>
    /// LOGIC: Maps directly to <see cref="IUndoRedoService.CanUndo"/>.
    /// Used to enable/disable the Undo command (Ctrl+Z).
    /// </remarks>
    public bool CanUndo { get; }

    /// <summary>
    /// Gets whether there are operations that can be redone.
    /// </summary>
    /// <remarks>
    /// LOGIC: Maps directly to <see cref="IUndoRedoService.CanRedo"/>.
    /// Used to enable/disable the Redo command (Ctrl+Y).
    /// </remarks>
    public bool CanRedo { get; }

    /// <summary>
    /// Gets the display name of the most recent undoable operation.
    /// </summary>
    /// <remarks>
    /// LOGIC: Shown in the Edit menu as "Undo {LastOperationName}" (e.g.,
    /// "Undo AI Rewrite (Formal)"). Null when the undo stack is empty.
    /// </remarks>
    public string? LastOperationName { get; }
}
