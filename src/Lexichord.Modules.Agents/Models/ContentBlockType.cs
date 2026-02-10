// -----------------------------------------------------------------------
// <copyright file="ContentBlockType.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Models;

/// <summary>
/// Identifies the type of content block containing the cursor.
/// </summary>
/// <remarks>
/// <para>
/// Used for context-aware prompt selection and agent recommendation.
/// Each type may trigger different default prompts or specialized agents.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.7c as part of the Document-Aware Prompting feature.
/// </para>
/// </remarks>
public enum ContentBlockType
{
    /// <summary>
    /// Regular paragraph text (default).
    /// </summary>
    Prose = 0,

    /// <summary>
    /// Fenced or indented code block.
    /// </summary>
    CodeBlock = 1,

    /// <summary>
    /// Markdown table structure.
    /// </summary>
    Table = 2,

    /// <summary>
    /// Ordered or unordered list.
    /// </summary>
    List = 3,

    /// <summary>
    /// Heading element (H1-H6).
    /// </summary>
    Heading = 4,

    /// <summary>
    /// Blockquote section.
    /// </summary>
    Blockquote = 5,

    /// <summary>
    /// Front matter (YAML/TOML).
    /// </summary>
    FrontMatter = 6,

    /// <summary>
    /// Inline code span (not block).
    /// </summary>
    InlineCode = 7,

    /// <summary>
    /// Link or image reference.
    /// </summary>
    Link = 8
}
