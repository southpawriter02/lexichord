// -----------------------------------------------------------------------
// <copyright file="CloseRequestedEventArgs.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Simplifier;

/// <summary>
/// Event arguments for the preview close request event.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This event is raised by the <see cref="SimplificationPreviewViewModel"/>
/// when the preview should be closed, either because the user accepted changes,
/// rejected them, or cancelled the operation. The view subscribes to this event
/// to trigger the appropriate close behavior.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.4c as part of the Simplifier Agent Preview/Diff UI.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In ViewModel
/// CloseRequested?.Invoke(this, new CloseRequestedEventArgs(accepted: true));
///
/// // In View code-behind
/// viewModel.CloseRequested += (s, e) =>
/// {
///     if (e.Accepted)
///     {
///         // Changes were applied - show success notification
///     }
///     Close();
/// };
/// </code>
/// </example>
/// <seealso cref="SimplificationPreviewViewModel"/>
public sealed class CloseRequestedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CloseRequestedEventArgs"/> class.
    /// </summary>
    /// <param name="accepted">
    /// <c>true</c> if changes were accepted before closing;
    /// <c>false</c> if the preview was cancelled or rejected.
    /// </param>
    public CloseRequestedEventArgs(bool accepted)
    {
        Accepted = accepted;
    }

    /// <summary>
    /// Gets a value indicating whether changes were accepted before closing.
    /// </summary>
    /// <value>
    /// <c>true</c> if the user accepted changes (Accept All or Accept Selected);
    /// <c>false</c> if the user rejected or cancelled.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> The view can use this to determine whether to show a success
    /// notification or simply close silently.
    /// </remarks>
    public bool Accepted { get; }

    /// <summary>
    /// Gets a singleton instance representing an accepted close.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Provides a cached instance to reduce allocations for
    /// the common case of accepting changes.
    /// </remarks>
    public static CloseRequestedEventArgs AcceptedClose { get; } = new(accepted: true);

    /// <summary>
    /// Gets a singleton instance representing a rejected/cancelled close.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Provides a cached instance to reduce allocations for
    /// the common case of rejecting or cancelling.
    /// </remarks>
    public static CloseRequestedEventArgs RejectedClose { get; } = new(accepted: false);
}
