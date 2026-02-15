// -----------------------------------------------------------------------
// <copyright file="ParsedParagraph.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Simplifier;

/// <summary>
/// Represents a parsed paragraph with offset information for document reconstruction.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The <see cref="ParsedParagraph"/> record is produced by the
/// <see cref="ParagraphParser"/> during document analysis. It contains the paragraph
/// text along with positional information needed to replace the original content
/// after simplification.
/// </para>
/// <para>
/// <b>Offset Semantics:</b>
/// <list type="bullet">
///   <item><description><see cref="StartOffset"/>: Zero-based character index from document start</description></item>
///   <item><description><see cref="EndOffset"/>: Exclusive end position (first character after paragraph)</description></item>
///   <item><description>Length = <c>EndOffset - StartOffset</c></description></item>
/// </list>
/// </para>
/// <para>
/// <b>Document Reconstruction:</b>
/// When applying changes, paragraphs must be processed in reverse offset order
/// (highest offset first) to preserve offset validity for remaining paragraphs.
/// </para>
/// <para><b>Introduced in:</b> v0.7.4d as part of the Batch Simplification feature.</para>
/// </remarks>
/// <example>
/// <code>
/// // Parsing a document into paragraphs
/// var paragraphs = ParagraphParser.Parse(documentContent);
///
/// foreach (var paragraph in paragraphs)
/// {
///     Console.WriteLine($"[{paragraph.Index}] Type: {paragraph.Type}");
///     Console.WriteLine($"    Offset: {paragraph.StartOffset}-{paragraph.EndOffset}");
///     Console.WriteLine($"    Text: {paragraph.TextPreview}");
/// }
///
/// // Applying changes in reverse order
/// var changes = paragraphs
///     .OrderByDescending(p => p.StartOffset)
///     .ToList();
/// </code>
/// </example>
/// <seealso cref="ParagraphParser"/>
/// <seealso cref="ParagraphType"/>
internal record ParsedParagraph
{
    /// <summary>
    /// Default preview length for paragraph text.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> 50 characters provides enough context for identification
    /// without overwhelming logs or UI displays.
    /// </remarks>
    public const int DefaultPreviewLength = 50;

    /// <summary>
    /// Gets the zero-based index of the paragraph in the document.
    /// </summary>
    /// <value>
    /// Sequential index starting from 0 for the first paragraph.
    /// Used for progress reporting and result correlation.
    /// </value>
    public required int Index { get; init; }

    /// <summary>
    /// Gets the character offset where the paragraph begins.
    /// </summary>
    /// <value>
    /// Zero-based character index from the start of the document.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> The start offset marks the first character of the paragraph
    /// content, excluding any leading whitespace from the paragraph separator.
    /// </remarks>
    public required int StartOffset { get; init; }

    /// <summary>
    /// Gets the exclusive end offset of the paragraph.
    /// </summary>
    /// <value>
    /// The character offset immediately after the last character of the paragraph.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Uses half-open interval convention [StartOffset, EndOffset)
    /// for consistency with .NET string operations.
    /// </remarks>
    public required int EndOffset { get; init; }

    /// <summary>
    /// Gets the raw text content of the paragraph.
    /// </summary>
    /// <value>
    /// The paragraph text as it appears in the document, with internal
    /// line breaks normalized but not stripped.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Text is extracted from the document at the specified offset
    /// range and may contain newlines for multi-line paragraphs.
    /// </remarks>
    public required string Text { get; init; }

    /// <summary>
    /// Gets the structural type of the paragraph.
    /// </summary>
    /// <value>
    /// The <see cref="ParagraphType"/> indicating whether this is normal prose,
    /// a heading, code block, blockquote, or list item.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Type is determined by the <see cref="ParagraphParser"/> based on
    /// Markdown syntax patterns. Used for skip detection in batch processing.
    /// </remarks>
    public required ParagraphType Type { get; init; }

    /// <summary>
    /// Gets the length of the paragraph in characters.
    /// </summary>
    /// <value>
    /// Number of characters in the paragraph, calculated as <c>EndOffset - StartOffset</c>.
    /// </value>
    public int Length => EndOffset - StartOffset;

    /// <summary>
    /// Gets a truncated preview of the paragraph text.
    /// </summary>
    /// <value>
    /// The first <see cref="DefaultPreviewLength"/> characters of the text,
    /// followed by "..." if truncated.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Preview text is useful for progress reporting and logging
    /// without including the full paragraph content.
    /// </remarks>
    public string TextPreview =>
        Text.Length <= DefaultPreviewLength
            ? Text.Replace('\n', ' ').Replace('\r', ' ')
            : Text[..(DefaultPreviewLength - 3)].Replace('\n', ' ').Replace('\r', ' ') + "...";

    /// <summary>
    /// Gets a value indicating whether this paragraph is normal prose.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="Type"/> is <see cref="ParagraphType.Normal"/>; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Normal paragraphs are the primary candidates for simplification.
    /// </remarks>
    public bool IsNormal => Type == ParagraphType.Normal;

    /// <summary>
    /// Gets a value indicating whether this paragraph is a structural element.
    /// </summary>
    /// <value>
    /// <c>true</c> if the paragraph is a heading, code block, blockquote, or list item;
    /// otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Structural elements may be skipped during batch processing
    /// based on <see cref="Lexichord.Abstractions.Agents.Simplifier.BatchSimplificationOptions"/>.
    /// </remarks>
    public bool IsStructural =>
        Type is ParagraphType.Heading
            or ParagraphType.CodeBlock
            or ParagraphType.Blockquote
            or ParagraphType.ListItem;

    /// <summary>
    /// Gets the approximate word count of the paragraph.
    /// </summary>
    /// <value>
    /// Estimated number of words based on whitespace splitting.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Simple word count using whitespace as delimiter.
    /// Used for minimum word count skip detection.
    /// </remarks>
    public int ApproximateWordCount =>
        Text.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries).Length;

    /// <summary>
    /// Creates a paragraph record with computed properties.
    /// </summary>
    /// <param name="index">Zero-based paragraph index.</param>
    /// <param name="startOffset">Character offset where paragraph begins.</param>
    /// <param name="endOffset">Exclusive end offset.</param>
    /// <param name="text">Paragraph text content.</param>
    /// <param name="type">Structural type of the paragraph.</param>
    /// <returns>A new <see cref="ParsedParagraph"/> instance.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method for creating paragraphs with validation.
    /// </remarks>
    public static ParsedParagraph Create(
        int index,
        int startOffset,
        int endOffset,
        string text,
        ParagraphType type)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfNegative(startOffset);
        ArgumentOutOfRangeException.ThrowIfLessThan(endOffset, startOffset);
        ArgumentNullException.ThrowIfNull(text);

        return new ParsedParagraph
        {
            Index = index,
            StartOffset = startOffset,
            EndOffset = endOffset,
            Text = text,
            Type = type
        };
    }
}
