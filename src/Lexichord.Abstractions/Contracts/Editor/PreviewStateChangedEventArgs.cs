// -----------------------------------------------------------------------
// <copyright file="PreviewStateChangedEventArgs.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.Editor;

/// <summary>
/// Event args for preview state changes.
/// </summary>
/// <remarks>
/// <para>
/// Raised by <see cref="IEditorInsertionService"/> when the preview overlay
/// is shown, accepted, or rejected. Used by the <c>PreviewOverlayViewModel</c>
/// to synchronize UI state.
/// </para>
/// <para><b>Introduced in:</b> v0.6.7b as part of the Inline Suggestions feature.</para>
/// </remarks>
public class PreviewStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets whether a preview is now active.
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Gets the preview text if active.
    /// </summary>
    public string? PreviewText { get; init; }

    /// <summary>
    /// Gets the preview location if active.
    /// </summary>
    public TextSpan? Location { get; init; }
}
