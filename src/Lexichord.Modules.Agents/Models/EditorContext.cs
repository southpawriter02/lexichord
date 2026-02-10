// -----------------------------------------------------------------------
// <copyright file="EditorContext.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts.Editor;

namespace Lexichord.Modules.Agents.Models;

/// <summary>
/// Captures the current editor state for context analysis.
/// </summary>
/// <remarks>
/// <para>
/// Provides a snapshot of the editor's cursor position, selection, and document
/// information. Used by <see cref="Services.ContextAwarePromptSelector"/> to
/// determine appropriate prompts and by <see cref="Services.IDocumentContextAnalyzer"/>
/// to locate the analysis point.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.7c as part of the Document-Aware Prompting feature.
/// </para>
/// </remarks>
/// <param name="DocumentPath">Path to the active document.</param>
/// <param name="CursorPosition">Current cursor position (zero-based).</param>
/// <param name="SelectionStart">Start of selection, if any.</param>
/// <param name="SelectionLength">Length of selection, if any.</param>
/// <param name="SelectedText">The selected text, if any.</param>
/// <param name="LineNumber">Current line number (1-based).</param>
/// <param name="ColumnNumber">Current column number (1-based).</param>
public record EditorContext(
    string DocumentPath,
    int CursorPosition,
    int? SelectionStart = null,
    int? SelectionLength = null,
    string? SelectedText = null,
    int LineNumber = 1,
    int ColumnNumber = 1)
{
    /// <summary>
    /// Gets whether there is an active selection.
    /// </summary>
    public bool HasSelection => SelectionLength > 0;

    /// <summary>
    /// Creates an <see cref="EditorContext"/> from an <see cref="IEditorService"/> instance.
    /// </summary>
    /// <param name="editor">The editor service to capture state from.</param>
    /// <returns>A new <see cref="EditorContext"/> reflecting the current editor state.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="editor"/> is null.</exception>
    /// <remarks>
    /// LOGIC: Captures a snapshot of the editor's current state including
    /// cursor position, selection, and document path. Safe to call even
    /// when no document is open (DocumentPath defaults to empty string).
    /// </remarks>
    public static EditorContext FromEditorService(IEditorService editor)
    {
        ArgumentNullException.ThrowIfNull(editor);

        return new EditorContext(
            DocumentPath: editor.CurrentDocumentPath ?? string.Empty,
            CursorPosition: editor.CaretOffset,
            SelectionStart: editor.HasSelection ? editor.SelectionStart : null,
            SelectionLength: editor.HasSelection ? editor.SelectionLength : null,
            SelectedText: editor.HasSelection ? editor.GetSelectedText() : null,
            LineNumber: editor.CurrentLine,
            ColumnNumber: editor.CurrentColumn);
    }
}
