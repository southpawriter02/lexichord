// -----------------------------------------------------------------------
// <copyright file="IEditorInsertionService.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.Editor;

/// <summary>
/// Service for inserting AI-generated text into the editor with preview support.
/// </summary>
/// <remarks>
/// <para>
/// This service provides a clean abstraction for editor text manipulation,
/// supporting both immediate insertion and preview-before-commit workflows.
/// The preview mode displays proposed text as a ghost overlay, allowing the
/// user to review changes before accepting.
/// </para>
/// <para>
/// All modifications are wrapped in undo groups using <see cref="IEditorService"/>,
/// ensuring that a single Ctrl+Z reverts the entire AI insertion regardless
/// of complexity. This applies to both direct insertions and accepted previews.
/// </para>
/// <para>
/// The service maintains preview state and provides reactive updates for UI
/// binding through the <see cref="IsPreviewActive"/> property.
/// </para>
/// <para><b>Introduced in:</b> v0.6.7b as part of the Inline Suggestions feature.</para>
/// </remarks>
/// <example>
/// <code>
/// // Show preview and wait for user decision
/// await _insertionService.ShowPreviewAsync(responseText, location);
///
/// // Later, based on user action:
/// if (userAccepts)
/// {
///     await _insertionService.AcceptPreviewAsync();
/// }
/// else
/// {
///     await _insertionService.RejectPreviewAsync();
/// }
/// </code>
/// </example>
public interface IEditorInsertionService
{
    /// <summary>
    /// Inserts text at the current cursor position.
    /// </summary>
    /// <param name="text">The text to insert.</param>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when cursor position cannot be determined.
    /// </exception>
    /// <remarks>
    /// The insertion is wrapped in an undo group. If the cursor position
    /// has changed since the operation was initiated, the service will
    /// attempt to re-acquire the current position.
    /// </remarks>
    Task InsertAtCursorAsync(string text, CancellationToken ct = default);

    /// <summary>
    /// Replaces the current selection with the specified text.
    /// </summary>
    /// <param name="text">The replacement text.</param>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no selection is active.
    /// </exception>
    /// <remarks>
    /// If no selection is active when this method is called, it throws
    /// an exception. Callers should verify selection state first.
    /// </remarks>
    Task ReplaceSelectionAsync(string text, CancellationToken ct = default);

    /// <summary>
    /// Shows a preview overlay at the specified location without committing.
    /// </summary>
    /// <param name="text">The text to preview.</param>
    /// <param name="location">The text span where preview should appear.</param>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// The preview is displayed as a ghost overlay with reduced opacity,
    /// showing the proposed text without modifying the actual document.
    /// Accept/reject controls are displayed alongside the preview.
    /// </para>
    /// <para>
    /// Only one preview can be active at a time. Calling this method while
    /// a preview is active will reject the existing preview first.
    /// </para>
    /// </remarks>
    Task ShowPreviewAsync(string text, TextSpan location, CancellationToken ct = default);

    /// <summary>
    /// Accepts the current preview and commits the text to the document.
    /// </summary>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>
    /// True if the preview was accepted and committed; false if no preview was active.
    /// </returns>
    /// <remarks>
    /// After accepting, the committed text becomes part of the document and
    /// is wrapped in an undo group for single-step reversal.
    /// </remarks>
    Task<bool> AcceptPreviewAsync(CancellationToken ct = default);

    /// <summary>
    /// Rejects the current preview and dismisses the overlay without changes.
    /// </summary>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RejectPreviewAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets whether a preview overlay is currently active.
    /// </summary>
    /// <remarks>
    /// This property can be used for UI binding to show/hide preview-related
    /// controls and for keyboard shortcut condition evaluation.
    /// </remarks>
    bool IsPreviewActive { get; }

    /// <summary>
    /// Gets the current preview text, if any.
    /// </summary>
    string? CurrentPreviewText { get; }

    /// <summary>
    /// Gets the location of the current preview.
    /// </summary>
    TextSpan? CurrentPreviewLocation { get; }

    /// <summary>
    /// Raised when preview state changes.
    /// </summary>
    event EventHandler<PreviewStateChangedEventArgs>? PreviewStateChanged;
}
