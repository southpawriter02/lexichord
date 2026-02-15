// -----------------------------------------------------------------------
// <copyright file="ParagraphType.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Simplifier;

/// <summary>
/// Represents the type of a parsed paragraph for skip detection.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The <see cref="ParagraphType"/> is determined by the
/// <see cref="ParagraphParser"/> during document parsing and is used
/// to evaluate skip conditions based on <see cref="Lexichord.Abstractions.Agents.Simplifier.BatchSimplificationOptions"/>.
/// </para>
/// <para>
/// <b>Detection Rules:</b>
/// <list type="bullet">
///   <item><description><see cref="Heading"/>: Starts with <c>#</c> followed by space</description></item>
///   <item><description><see cref="CodeBlock"/>: Within <c>```</c> fence or indented 4+ spaces</description></item>
///   <item><description><see cref="Blockquote"/>: Starts with <c>&gt;</c></description></item>
///   <item><description><see cref="ListItem"/>: Starts with <c>- </c>, <c>* </c>, or <c>1. </c></description></item>
///   <item><description><see cref="Normal"/>: None of the above</description></item>
/// </list>
/// </para>
/// <para><b>Introduced in:</b> v0.7.4d as part of the Batch Simplification feature.</para>
/// </remarks>
/// <seealso cref="ParsedParagraph"/>
/// <seealso cref="ParagraphParser"/>
internal enum ParagraphType
{
    /// <summary>
    /// A normal prose paragraph suitable for simplification.
    /// </summary>
    Normal = 0,

    /// <summary>
    /// A Markdown heading (e.g., <c># Heading</c>).
    /// </summary>
    /// <remarks>
    /// Headings are identified by lines starting with one or more <c>#</c>
    /// characters followed by a space. ATX-style headings only; setext-style
    /// headings (underlined with <c>=</c> or <c>-</c>) are treated as normal paragraphs.
    /// </remarks>
    Heading = 1,

    /// <summary>
    /// A code block (fenced or indented).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Fenced code blocks are delimited by <c>```</c> or <c>~~~</c> on their own lines.
    /// Indented code blocks are lines indented by 4 or more spaces.
    /// </para>
    /// <para>
    /// Code blocks should not be simplified as they contain technical content
    /// where readability metrics are not meaningful.
    /// </para>
    /// </remarks>
    CodeBlock = 2,

    /// <summary>
    /// A blockquote (lines starting with <c>&gt;</c>).
    /// </summary>
    /// <remarks>
    /// Blockquotes may contain prose that could be simplified, but they often
    /// represent quoted material that should be preserved verbatim.
    /// </remarks>
    Blockquote = 3,

    /// <summary>
    /// A list item (ordered or unordered).
    /// </summary>
    /// <remarks>
    /// List items include:
    /// <list type="bullet">
    ///   <item><description>Unordered: <c>- item</c>, <c>* item</c>, <c>+ item</c></description></item>
    ///   <item><description>Ordered: <c>1. item</c>, <c>1) item</c></description></item>
    /// </list>
    /// List items are typically short and context-dependent.
    /// </remarks>
    ListItem = 4
}
