// -----------------------------------------------------------------------
// <copyright file="SelectionChangedEventArgs.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.Editor;

/// <summary>
/// Event arguments for editor text selection changes.
/// </summary>
/// <remarks>
/// <para>
/// Published by <see cref="IEditorService"/> when the user's text selection
/// changes in the active document. Consumed by services such as
/// <c>SelectionContextService</c> to track selection state.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.7a as part of the Selection Context feature.
/// </para>
/// </remarks>
public class SelectionChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of <see cref="SelectionChangedEventArgs"/>.
    /// </summary>
    /// <param name="newSelection">The new selected text, or null if no selection.</param>
    public SelectionChangedEventArgs(string? newSelection)
    {
        NewSelection = newSelection;
    }

    /// <summary>
    /// Gets the newly selected text, or null if the selection was cleared.
    /// </summary>
    public string? NewSelection { get; }
}
