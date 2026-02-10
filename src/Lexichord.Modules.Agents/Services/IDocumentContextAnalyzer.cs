// -----------------------------------------------------------------------
// <copyright file="IDocumentContextAnalyzer.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Modules.Agents.Models;

namespace Lexichord.Modules.Agents.Services;

/// <summary>
/// Analyzes document structure to provide context for AI prompts.
/// </summary>
/// <remarks>
/// <para>
/// This service parses the Markdown AST of the active document to determine
/// the structural context at the cursor position. It identifies section
/// headings, content block types, and extracts local context for prompt
/// enrichment.
/// </para>
/// <para>
/// The analyzer maintains a cached AST per document, invalidating on edits.
/// This ensures fast repeated queries without re-parsing the entire document.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.7c as part of the Document-Aware Prompting feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var context = await _analyzer.AnalyzeAtPositionAsync(docPath, cursorPos);
/// Console.WriteLine($"Section: {context.CurrentSection}");
/// Console.WriteLine($"Type: {context.ContentType}");
/// Console.WriteLine($"Suggested Agent: {context.SuggestedAgentId}");
/// </code>
/// </example>
/// <seealso cref="DocumentContext"/>
/// <seealso cref="ContentBlockType"/>
/// <seealso cref="DocumentContextAnalyzer"/>
public interface IDocumentContextAnalyzer
{
    /// <summary>
    /// Analyzes document structure at the specified cursor position.
    /// </summary>
    /// <param name="documentPath">Path to the document being analyzed.</param>
    /// <param name="cursorPosition">Zero-based cursor position in the document.</param>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>
    /// A <see cref="DocumentContext"/> containing structural metadata
    /// about the document at the specified position.
    /// </returns>
    /// <exception cref="FileNotFoundException">
    /// Thrown when the document path does not exist.
    /// </exception>
    /// <remarks>
    /// LOGIC: Parses the document's Markdown AST (using cached version if available),
    /// then walks the AST to determine content type, section heading, and local context
    /// at the specified cursor position. Also suggests an appropriate agent based on
    /// the detected content type and section heading keywords.
    /// </remarks>
    Task<DocumentContext> AnalyzeAtPositionAsync(
        string documentPath,
        int cursorPosition,
        CancellationToken ct = default);

    /// <summary>
    /// Detects the content block type at the specified position.
    /// </summary>
    /// <param name="position">Zero-based position in the document.</param>
    /// <returns>
    /// The detected <see cref="ContentBlockType"/>. Returns
    /// <see cref="ContentBlockType.Prose"/> if no document is active or
    /// no cached AST is available.
    /// </returns>
    /// <remarks>
    /// LOGIC: Uses the cached AST for the current document (via
    /// <see cref="Lexichord.Abstractions.Contracts.Editor.IEditorService.CurrentDocumentPath"/>).
    /// Does not trigger a parse if no cached AST exists.
    /// </remarks>
    ContentBlockType DetectContentType(int position);

    /// <summary>
    /// Gets the current section heading containing the cursor position.
    /// </summary>
    /// <param name="position">Zero-based position in the document.</param>
    /// <returns>
    /// The section heading text, or <c>null</c> if the cursor is not within
    /// a headed section or no document is active.
    /// </returns>
    /// <remarks>
    /// LOGIC: Finds the nearest <c>HeadingBlock</c> that appears before
    /// the specified position in the Markdown AST. Returns <c>null</c> if
    /// the position precedes all headings (document root/preamble).
    /// </remarks>
    string? GetCurrentSectionHeading(int position);

    /// <summary>
    /// Extracts surrounding text as local context.
    /// </summary>
    /// <param name="position">Zero-based position in the document.</param>
    /// <param name="charsBefore">
    /// Number of characters to include before position. Defaults to 500.
    /// </param>
    /// <param name="charsAfter">
    /// Number of characters to include after position. Defaults to 500.
    /// </param>
    /// <returns>
    /// The extracted local context string. Returns <see cref="string.Empty"/>
    /// if no document text is available.
    /// </returns>
    /// <remarks>
    /// LOGIC: Extracts a substring from the active document's text centered
    /// around the specified position, clamped to document boundaries.
    /// </remarks>
    string GetLocalContext(int position, int charsBefore = 500, int charsAfter = 500);

    /// <summary>
    /// Invalidates the cached AST for the specified document.
    /// </summary>
    /// <param name="documentPath">Path to the document to invalidate.</param>
    /// <remarks>
    /// LOGIC: Removes the cached <c>MarkdownDocument</c> for the specified path
    /// from the <see cref="ASTCacheProvider"/>. The next call to
    /// <see cref="AnalyzeAtPositionAsync"/> will re-parse the document.
    /// Typically called in response to <c>DocumentChanged</c> events.
    /// </remarks>
    void InvalidateCache(string documentPath);
}
