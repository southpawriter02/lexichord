// -----------------------------------------------------------------------
// <copyright file="ShowCustomRewriteDialogEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts.Editor;
using MediatR;

namespace Lexichord.Modules.Agents.Editor.Events;

/// <summary>
/// Published when the custom rewrite dialog should be displayed.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This MediatR notification is published when the user selects
/// the "Custom Rewrite" option from the context menu. The event is consumed
/// by the shell or dialog service which displays a dialog for the user to
/// enter their custom rewrite instruction.
/// </para>
/// <para>
/// <b>Dialog Flow:</b>
/// <list type="number">
///   <item><description>User selects text and chooses "Custom Rewrite..." from context menu</description></item>
///   <item><description><see cref="EditorAgentContextMenuProvider"/> publishes this event</description></item>
///   <item><description>Dialog service shows input dialog with preview of selected text</description></item>
///   <item><description>User enters instruction (e.g., "Make this more persuasive")</description></item>
///   <item><description>Dialog service publishes <see cref="RewriteRequestedEvent"/> with Custom intent</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.3a as part of the Editor Agent feature.
/// </para>
/// </remarks>
/// <param name="SelectedText">The text selected by the user to be rewritten.</param>
/// <param name="SelectionSpan">The position of the selection in the document.</param>
/// <param name="DocumentPath">The file path of the active document, or null if untitled.</param>
/// <param name="Timestamp">When the dialog request was triggered.</param>
/// <seealso cref="RewriteRequestedEvent"/>
/// <seealso cref="TextSpan"/>
public record ShowCustomRewriteDialogEvent(
    string SelectedText,
    TextSpan SelectionSpan,
    string? DocumentPath,
    DateTime Timestamp
) : INotification
{
    /// <summary>
    /// Creates a new custom rewrite dialog event with the current timestamp.
    /// </summary>
    /// <param name="selectedText">The text selected by the user to be rewritten.</param>
    /// <param name="selectionSpan">The position of the selection in the document.</param>
    /// <param name="documentPath">The file path of the active document, or null if untitled.</param>
    /// <returns>A new <see cref="ShowCustomRewriteDialogEvent"/> instance.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Convenience factory method that automatically sets the
    /// <see cref="Timestamp"/> to <see cref="DateTime.UtcNow"/>.
    /// </remarks>
    public static ShowCustomRewriteDialogEvent Create(
        string selectedText,
        TextSpan selectionSpan,
        string? documentPath) =>
        new(selectedText, selectionSpan, documentPath, DateTime.UtcNow);

    /// <summary>
    /// Gets the maximum length of selected text to display in the dialog preview.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Long selections are truncated in the dialog preview to keep
    /// the UI clean. The full text is still passed to the rewrite handler.
    /// </remarks>
    public const int MaxPreviewLength = 200;

    /// <summary>
    /// Gets the selected text truncated for dialog preview.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> If <see cref="SelectedText"/> exceeds <see cref="MaxPreviewLength"/>,
    /// it is truncated with an ellipsis suffix.
    /// </remarks>
    public string PreviewText =>
        SelectedText.Length <= MaxPreviewLength
            ? SelectedText
            : SelectedText[..MaxPreviewLength] + "...";
}
