// -----------------------------------------------------------------------
// <copyright file="RewriteRequestedEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents.Editor;
using Lexichord.Abstractions.Contracts.Editor;
using MediatR;

namespace Lexichord.Modules.Agents.Editor.Events;

/// <summary>
/// Published when a rewrite operation is requested from the editor context menu.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This MediatR notification is published by
/// <see cref="EditorAgentContextMenuProvider"/> when the user selects a rewrite
/// option from the context menu. The event is consumed by the rewrite command
/// handler (v0.7.3b) which orchestrates the AI rewriting operation.
/// </para>
/// <para>
/// <b>Properties:</b>
/// <list type="bullet">
///   <item><description><see cref="Intent"/> - The type of rewrite transformation requested</description></item>
///   <item><description><see cref="SelectedText"/> - The text selected by the user to be rewritten</description></item>
///   <item><description><see cref="SelectionSpan"/> - The position of the selection in the document</description></item>
///   <item><description><see cref="DocumentPath"/> - The file path of the active document (null if untitled)</description></item>
///   <item><description><see cref="CustomInstruction"/> - User-provided instruction for Custom rewrites</description></item>
///   <item><description><see cref="Timestamp"/> - When the rewrite was requested</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.3a as part of the Editor Agent feature.
/// </para>
/// </remarks>
/// <param name="Intent">The type of rewrite transformation requested.</param>
/// <param name="SelectedText">The text selected by the user to be rewritten.</param>
/// <param name="SelectionSpan">The position of the selection in the document.</param>
/// <param name="DocumentPath">The file path of the active document, or null if untitled.</param>
/// <param name="CustomInstruction">User-provided instruction for Custom rewrites, or null for predefined intents.</param>
/// <param name="Timestamp">When the rewrite was requested.</param>
/// <seealso cref="RewriteIntent"/>
/// <seealso cref="TextSpan"/>
public record RewriteRequestedEvent(
    RewriteIntent Intent,
    string SelectedText,
    TextSpan SelectionSpan,
    string? DocumentPath,
    string? CustomInstruction,
    DateTime Timestamp
) : INotification
{
    /// <summary>
    /// Creates a new rewrite request with the current timestamp.
    /// </summary>
    /// <param name="intent">The type of rewrite transformation requested.</param>
    /// <param name="selectedText">The text selected by the user to be rewritten.</param>
    /// <param name="selectionSpan">The position of the selection in the document.</param>
    /// <param name="documentPath">The file path of the active document, or null if untitled.</param>
    /// <param name="customInstruction">User-provided instruction for Custom rewrites.</param>
    /// <returns>A new <see cref="RewriteRequestedEvent"/> instance.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Convenience factory method that automatically sets the
    /// <see cref="Timestamp"/> to <see cref="DateTime.UtcNow"/>.
    /// </remarks>
    public static RewriteRequestedEvent Create(
        RewriteIntent intent,
        string selectedText,
        TextSpan selectionSpan,
        string? documentPath,
        string? customInstruction = null) =>
        new(intent, selectedText, selectionSpan, documentPath, customInstruction, DateTime.UtcNow);
}
