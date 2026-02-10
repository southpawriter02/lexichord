// -----------------------------------------------------------------------
// <copyright file="DocumentContext.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Models;

/// <summary>
/// Contains structural metadata about the document at a specific cursor position.
/// </summary>
/// <remarks>
/// <para>
/// Produced by <see cref="Services.IDocumentContextAnalyzer"/> when analyzing the
/// Markdown AST at the user's cursor position. Used to enrich prompts sent to the
/// Co-pilot agent with contextual awareness of document structure.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.7c as part of the Document-Aware Prompting feature.
/// </para>
/// </remarks>
/// <param name="CurrentSection">
/// The heading text of the current section, or null if at document root.
/// </param>
/// <param name="ContentType">
/// The type of content block containing the cursor.
/// </param>
/// <param name="LocalContext">
/// Surrounding text within the configured character window.
/// </param>
/// <param name="CursorPosition">
/// The zero-based cursor position analyzed.
/// </param>
/// <param name="SuggestedAgentId">
/// The recommended agent ID based on content type, or null for default.
/// </param>
/// <param name="SectionLevel">
/// The heading level (1-6) of the current section, or 0 if none.
/// </param>
/// <param name="DocumentPath">
/// Path to the document being analyzed.
/// </param>
/// <example>
/// <code>
/// // Example context for cursor in a code block under "Implementation" heading
/// var context = new DocumentContext(
///     CurrentSection: "Implementation",
///     ContentType: ContentBlockType.CodeBlock,
///     LocalContext: "public class MyService...",
///     CursorPosition: 1542,
///     SuggestedAgentId: "code-helper",
///     SectionLevel: 2,
///     DocumentPath: "/docs/spec.md");
/// </code>
/// </example>
public record DocumentContext(
    string? CurrentSection,
    ContentBlockType ContentType,
    string LocalContext,
    int CursorPosition,
    string? SuggestedAgentId,
    int SectionLevel = 0,
    string? DocumentPath = null)
{
    /// <summary>
    /// Creates an empty context for cases where analysis is not possible.
    /// </summary>
    public static DocumentContext Empty => new(
        CurrentSection: null,
        ContentType: ContentBlockType.Prose,
        LocalContext: string.Empty,
        CursorPosition: 0,
        SuggestedAgentId: null);

    /// <summary>
    /// Gets whether this context contains a section heading.
    /// </summary>
    public bool HasSection => !string.IsNullOrEmpty(CurrentSection);

    /// <summary>
    /// Gets whether this context suggests a specialized agent.
    /// </summary>
    public bool HasSuggestedAgent => !string.IsNullOrEmpty(SuggestedAgentId);
}
