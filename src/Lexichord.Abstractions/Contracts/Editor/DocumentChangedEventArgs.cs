// -----------------------------------------------------------------------
// <copyright file="DocumentChangedEventArgs.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.Editor;

/// <summary>
/// Event arguments for document content changes in the editor.
/// </summary>
/// <remarks>
/// <para>
/// Published by <see cref="IEditorService"/> when the active document's content
/// changes (edits, saves, or file reloads). Consumed by services such as
/// <c>DocumentContextAnalyzer</c> to invalidate cached AST representations.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.7c as part of the Document-Aware Prompting feature.
/// </para>
/// </remarks>
public class DocumentChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of <see cref="DocumentChangedEventArgs"/>.
    /// </summary>
    /// <param name="documentPath">The file path of the changed document, or null if untitled.</param>
    public DocumentChangedEventArgs(string? documentPath)
    {
        DocumentPath = documentPath;
    }

    /// <summary>
    /// Gets the file path of the document that changed, or null if the document is untitled.
    /// </summary>
    public string? DocumentPath { get; }
}
