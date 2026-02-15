// -----------------------------------------------------------------------
// <copyright file="ParagraphSkipReason.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.Simplifier;

/// <summary>
/// Reasons why a paragraph may be skipped during batch simplification.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The <see cref="ParagraphSkipReason"/> indicates why a specific
/// paragraph was not processed through the simplification pipeline. This information
/// is captured in <see cref="ParagraphSimplificationResult.SkipReason"/> and can be
/// used for analytics and user feedback.
/// </para>
/// <para>
/// <b>Skip Evaluation Order:</b>
/// <list type="number">
///   <item><description><see cref="MaxParagraphsReached"/> — Checked first if limit is set</description></item>
///   <item><description><see cref="TooShort"/> — Word count below minimum threshold</description></item>
///   <item><description><see cref="IsHeading"/> — Markdown heading detected</description></item>
///   <item><description><see cref="IsCodeBlock"/> — Code block (fenced or indented)</description></item>
///   <item><description><see cref="IsBlockquote"/> — Blockquote content</description></item>
///   <item><description><see cref="AlreadySimple"/> — Already at or below target grade level</description></item>
/// </list>
/// </para>
/// <para><b>Introduced in:</b> v0.7.4d as part of the Batch Simplification feature.</para>
/// </remarks>
/// <seealso cref="ParagraphSimplificationResult"/>
/// <seealso cref="BatchSimplificationOptions"/>
public enum ParagraphSkipReason
{
    /// <summary>
    /// The paragraph was not skipped (it was processed).
    /// </summary>
    /// <remarks>
    /// This value is used when <see cref="ParagraphSimplificationResult.WasSimplified"/>
    /// is <c>true</c>. The <see cref="ParagraphSimplificationResult.SkipReason"/>
    /// property will be <c>null</c> in this case.
    /// </remarks>
    None = 0,

    /// <summary>
    /// The paragraph's readability is already at or below the target level.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Skip condition: <c>FleschKincaidGradeLevel &lt;= TargetGradeLevel + GradeLevelTolerance</c>
    /// </para>
    /// <para>
    /// This skip reason is only evaluated when <see cref="BatchSimplificationOptions.SkipAlreadySimple"/>
    /// is <c>true</c> (the default).
    /// </para>
    /// </remarks>
    AlreadySimple = 1,

    /// <summary>
    /// The paragraph has fewer words than the minimum threshold.
    /// </summary>
    /// <remarks>
    /// Skip condition: <c>WordCount &lt; BatchSimplificationOptions.MinParagraphWords</c>
    /// (default: 10 words).
    /// Short paragraphs often don't benefit from simplification and may have
    /// unreliable readability metrics.
    /// </remarks>
    TooShort = 2,

    /// <summary>
    /// The paragraph is a Markdown heading.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Skip condition: Paragraph text starts with one or more <c>#</c> characters
    /// followed by a space (e.g., <c># Heading</c>, <c>## Subheading</c>).
    /// </para>
    /// <para>
    /// This skip reason is only evaluated when <see cref="BatchSimplificationOptions.SkipHeadings"/>
    /// is <c>true</c> (the default).
    /// </para>
    /// </remarks>
    IsHeading = 3,

    /// <summary>
    /// The paragraph is a code block.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Skip condition: Paragraph is within a fenced code block (<c>```</c> or <c>~~~</c>)
    /// or is indented by 4+ spaces (Markdown indented code block).
    /// </para>
    /// <para>
    /// This skip reason is only evaluated when <see cref="BatchSimplificationOptions.SkipCodeBlocks"/>
    /// is <c>true</c> (the default).
    /// </para>
    /// </remarks>
    IsCodeBlock = 4,

    /// <summary>
    /// The paragraph is a blockquote.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Skip condition: Paragraph starts with <c>&gt;</c> (Markdown blockquote syntax).
    /// </para>
    /// <para>
    /// This skip reason is only evaluated when <see cref="BatchSimplificationOptions.SkipBlockquotes"/>
    /// is <c>true</c> (not the default).
    /// </para>
    /// </remarks>
    IsBlockquote = 5,

    /// <summary>
    /// The maximum number of paragraphs to process has been reached.
    /// </summary>
    /// <remarks>
    /// Skip condition: <c>ProcessedCount &gt;= BatchSimplificationOptions.MaxParagraphs</c>
    /// (only when <see cref="BatchSimplificationOptions.MaxParagraphs"/> is greater than 0).
    /// </remarks>
    MaxParagraphsReached = 6,

    /// <summary>
    /// The operation was cancelled by the user before this paragraph was processed.
    /// </summary>
    /// <remarks>
    /// This reason indicates that the paragraph was in the processing queue but
    /// the cancellation token was triggered before it could be processed.
    /// </remarks>
    UserCancelled = 7,

    /// <summary>
    /// The paragraph is a list item.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Skip condition: Paragraph starts with list markers such as:
    /// <list type="bullet">
    ///   <item><description><c>- </c> or <c>* </c> (unordered list)</description></item>
    ///   <item><description><c>1. </c> or <c>1) </c> (ordered list)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// List items are typically short and context-dependent, making independent
    /// simplification potentially disruptive to document structure.
    /// </para>
    /// </remarks>
    IsListItem = 8,

    /// <summary>
    /// The paragraph simplification failed due to an error.
    /// </summary>
    /// <remarks>
    /// This reason indicates that simplification was attempted but failed
    /// (e.g., LLM timeout, parsing error). The error message is available
    /// in <see cref="ParagraphSimplificationResult.ErrorMessage"/>.
    /// </remarks>
    ProcessingFailed = 9
}
