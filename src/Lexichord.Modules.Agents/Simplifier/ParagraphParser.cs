// -----------------------------------------------------------------------
// <copyright file="ParagraphParser.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Text;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Simplifier;

/// <summary>
/// Parses document content into paragraphs with offset tracking and type detection.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The <see cref="ParagraphParser"/> splits document content into
/// paragraphs suitable for batch simplification. It handles:
/// </para>
/// <list type="bullet">
///   <item><description>Paragraph boundary detection (blank lines)</description></item>
///   <item><description>Offset tracking for document reconstruction</description></item>
///   <item><description>Structural element detection (headings, code blocks, etc.)</description></item>
///   <item><description>Line ending normalization (Windows/Unix)</description></item>
/// </list>
/// <para>
/// <b>Paragraph Boundaries:</b>
/// Paragraphs are delimited by one or more blank lines (lines containing only whitespace).
/// This follows standard Markdown and plain text conventions.
/// </para>
/// <para>
/// <b>Structural Detection:</b>
/// The parser identifies structural Markdown elements:
/// <list type="bullet">
///   <item><description><b>Headings:</b> Lines starting with <c>#</c> followed by space (ATX-style)</description></item>
///   <item><description><b>Code blocks:</b> Content between <c>```</c> fences or indented 4+ spaces</description></item>
///   <item><description><b>Blockquotes:</b> Lines starting with <c>&gt;</c></description></item>
///   <item><description><b>List items:</b> Lines starting with <c>- </c>, <c>* </c>, <c>+ </c>, or <c>1. </c></description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// This class is stateless and thread-safe. The <see cref="Parse"/> method can be
/// called concurrently from multiple threads.
/// </para>
/// <para><b>Introduced in:</b> v0.7.4d as part of the Batch Simplification feature.</para>
/// </remarks>
/// <example>
/// <code>
/// var parser = new ParagraphParser(logger);
/// var paragraphs = parser.Parse(documentContent);
///
/// foreach (var paragraph in paragraphs)
/// {
///     Console.WriteLine($"[{paragraph.Index}] {paragraph.Type}: {paragraph.TextPreview}");
///     Console.WriteLine($"    Offset: {paragraph.StartOffset}-{paragraph.EndOffset}");
/// }
/// </code>
/// </example>
/// <seealso cref="ParsedParagraph"/>
/// <seealso cref="ParagraphType"/>
internal sealed partial class ParagraphParser
{
    // LOGIC: Regex patterns for structural element detection
    private static readonly Regex HeadingPattern = HeadingRegex();
    private static readonly Regex OrderedListPattern = OrderedListRegex();
    private static readonly Regex UnorderedListPattern = UnorderedListRegex();
    private static readonly Regex BlockquotePattern = BlockquoteRegex();
    private static readonly Regex FencedCodeStartPattern = FencedCodeStartRegex();
    private static readonly Regex IndentedCodePattern = IndentedCodeRegex();

    private readonly ILogger<ParagraphParser> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ParagraphParser"/> class.
    /// </summary>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="logger"/> is null.
    /// </exception>
    public ParagraphParser(ILogger<ParagraphParser> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Parses document content into paragraphs with offset tracking.
    /// </summary>
    /// <param name="content">The document content to parse.</param>
    /// <returns>
    /// A read-only list of <see cref="ParsedParagraph"/> records, ordered by offset.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="content"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> The parsing algorithm works as follows:
    /// </para>
    /// <list type="number">
    ///   <item><description>Split content into lines, preserving offsets</description></item>
    ///   <item><description>Group consecutive non-blank lines into paragraphs</description></item>
    ///   <item><description>Track fenced code block state for multi-line handling</description></item>
    ///   <item><description>Detect structural type based on first line of paragraph</description></item>
    ///   <item><description>Record start/end offsets for document reconstruction</description></item>
    /// </list>
    /// <para>
    /// <b>Edge Cases:</b>
    /// <list type="bullet">
    ///   <item><description>Empty content returns empty list</description></item>
    ///   <item><description>Content with only whitespace returns empty list</description></item>
    ///   <item><description>Single paragraph (no blank lines) returns one paragraph</description></item>
    ///   <item><description>Fenced code blocks are treated as single paragraphs</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public IReadOnlyList<ParsedParagraph> Parse(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        // LOGIC: Handle empty content
        if (string.IsNullOrWhiteSpace(content))
        {
            _logger.LogDebug("Document content is empty or whitespace; returning empty paragraph list");
            return Array.Empty<ParsedParagraph>();
        }

        var paragraphs = new List<ParsedParagraph>();
        var lines = SplitIntoLines(content);

        _logger.LogDebug("Parsing document with {LineCount} lines", lines.Count);

        var currentParagraphLines = new List<(string Line, int Offset)>();
        var inFencedCodeBlock = false;
        var fencePattern = string.Empty;

        foreach (var (line, offset) in lines)
        {
            // LOGIC: Track fenced code block state
            if (!inFencedCodeBlock)
            {
                var fenceMatch = FencedCodeStartPattern.Match(line);
                if (fenceMatch.Success)
                {
                    // LOGIC: Starting a fenced code block
                    if (currentParagraphLines.Count > 0)
                    {
                        // LOGIC: Flush current paragraph before starting code block
                        var paragraph = CreateParagraph(paragraphs.Count, currentParagraphLines, content);
                        paragraphs.Add(paragraph);
                        currentParagraphLines.Clear();
                    }

                    inFencedCodeBlock = true;
                    fencePattern = fenceMatch.Groups[1].Value; // Capture ``` or ~~~
                    currentParagraphLines.Add((line, offset));
                    continue;
                }
            }
            else
            {
                // LOGIC: Inside fenced code block, look for closing fence
                currentParagraphLines.Add((line, offset));

                if (line.TrimStart().StartsWith(fencePattern, StringComparison.Ordinal))
                {
                    // LOGIC: Closing fence found
                    inFencedCodeBlock = false;
                    fencePattern = string.Empty;

                    var paragraph = CreateParagraph(paragraphs.Count, currentParagraphLines, content);
                    paragraphs.Add(paragraph);
                    currentParagraphLines.Clear();
                }

                continue;
            }

            // LOGIC: Check for blank line (paragraph boundary)
            if (string.IsNullOrWhiteSpace(line))
            {
                if (currentParagraphLines.Count > 0)
                {
                    var paragraph = CreateParagraph(paragraphs.Count, currentParagraphLines, content);
                    paragraphs.Add(paragraph);
                    currentParagraphLines.Clear();
                }

                continue;
            }

            // LOGIC: Add line to current paragraph
            currentParagraphLines.Add((line, offset));
        }

        // LOGIC: Handle remaining lines (no trailing blank line)
        if (currentParagraphLines.Count > 0)
        {
            var paragraph = CreateParagraph(paragraphs.Count, currentParagraphLines, content);
            paragraphs.Add(paragraph);
        }

        _logger.LogDebug(
            "Parsed {ParagraphCount} paragraphs: {Normal} normal, {Headings} headings, " +
            "{CodeBlocks} code blocks, {Blockquotes} blockquotes, {ListItems} list items",
            paragraphs.Count,
            paragraphs.Count(p => p.Type == ParagraphType.Normal),
            paragraphs.Count(p => p.Type == ParagraphType.Heading),
            paragraphs.Count(p => p.Type == ParagraphType.CodeBlock),
            paragraphs.Count(p => p.Type == ParagraphType.Blockquote),
            paragraphs.Count(p => p.Type == ParagraphType.ListItem));

        return paragraphs;
    }

    /// <summary>
    /// Splits content into lines with offset tracking.
    /// </summary>
    /// <param name="content">The content to split.</param>
    /// <returns>List of (line, offset) tuples.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Handles both Windows (\r\n) and Unix (\n) line endings.
    /// </remarks>
    private static List<(string Line, int Offset)> SplitIntoLines(string content)
    {
        var result = new List<(string Line, int Offset)>();
        var lineStart = 0;

        for (var i = 0; i < content.Length; i++)
        {
            var c = content[i];

            if (c == '\n')
            {
                // LOGIC: Handle both \n and \r\n
                var lineEnd = i;
                if (lineEnd > 0 && content[lineEnd - 1] == '\r')
                {
                    lineEnd--;
                }

                var line = content[lineStart..lineEnd];
                result.Add((line, lineStart));
                lineStart = i + 1;
            }
        }

        // LOGIC: Add final line if content doesn't end with newline
        if (lineStart <= content.Length)
        {
            var line = content[lineStart..];
            if (line.EndsWith('\r'))
            {
                line = line[..^1];
            }

            result.Add((line, lineStart));
        }

        return result;
    }

    /// <summary>
    /// Creates a paragraph from collected lines.
    /// </summary>
    /// <param name="index">Paragraph index.</param>
    /// <param name="lines">Lines comprising the paragraph.</param>
    /// <param name="originalContent">Original document content for offset validation.</param>
    /// <returns>A new <see cref="ParsedParagraph"/>.</returns>
    private ParsedParagraph CreateParagraph(
        int index,
        List<(string Line, int Offset)> lines,
        string originalContent)
    {
        var startOffset = lines[0].Offset;
        var lastLine = lines[^1];

        // LOGIC: Calculate end offset including line content
        var endOffset = lastLine.Offset + lastLine.Line.Length;

        // LOGIC: Combine lines into paragraph text
        var text = CombineLines(lines);

        // LOGIC: Detect paragraph type from first line
        var type = DetectParagraphType(lines);

        _logger.LogTrace(
            "Created paragraph {Index}: Type={Type}, Offset={Start}-{End}, Preview={Preview}",
            index, type, startOffset, endOffset,
            text.Length <= 30 ? text.Replace('\n', ' ') : text[..27].Replace('\n', ' ') + "...");

        return ParsedParagraph.Create(index, startOffset, endOffset, text, type);
    }

    /// <summary>
    /// Combines paragraph lines into a single text block.
    /// </summary>
    /// <param name="lines">Lines to combine.</param>
    /// <returns>Combined text preserving newlines.</returns>
    private static string CombineLines(List<(string Line, int Offset)> lines)
    {
        if (lines.Count == 1)
        {
            return lines[0].Line;
        }

        var sb = new StringBuilder();
        for (var i = 0; i < lines.Count; i++)
        {
            if (i > 0)
            {
                sb.Append('\n');
            }

            sb.Append(lines[i].Line);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Detects the structural type of a paragraph.
    /// </summary>
    /// <param name="lines">Lines comprising the paragraph.</param>
    /// <returns>The detected <see cref="ParagraphType"/>.</returns>
    private static ParagraphType DetectParagraphType(List<(string Line, int Offset)> lines)
    {
        var firstLine = lines[0].Line;

        // LOGIC: Check for fenced code block (entire paragraph is code)
        if (FencedCodeStartPattern.IsMatch(firstLine))
        {
            return ParagraphType.CodeBlock;
        }

        // LOGIC: Check for indented code block (all lines indented 4+ spaces)
        if (lines.All(l => IndentedCodePattern.IsMatch(l.Line) || string.IsNullOrWhiteSpace(l.Line)))
        {
            return ParagraphType.CodeBlock;
        }

        // LOGIC: Check for heading (ATX-style: # Heading)
        if (HeadingPattern.IsMatch(firstLine))
        {
            return ParagraphType.Heading;
        }

        // LOGIC: Check for blockquote (> quote)
        if (BlockquotePattern.IsMatch(firstLine))
        {
            return ParagraphType.Blockquote;
        }

        // LOGIC: Check for unordered list item (- item, * item, + item)
        if (UnorderedListPattern.IsMatch(firstLine))
        {
            return ParagraphType.ListItem;
        }

        // LOGIC: Check for ordered list item (1. item, 1) item)
        if (OrderedListPattern.IsMatch(firstLine))
        {
            return ParagraphType.ListItem;
        }

        // LOGIC: Default to normal prose paragraph
        return ParagraphType.Normal;
    }

    // LOGIC: Compiled regex patterns for performance

    /// <summary>
    /// ATX-style heading: one or more # followed by space.
    /// </summary>
    [GeneratedRegex(@"^\s*#{1,6}\s+", RegexOptions.Compiled)]
    private static partial Regex HeadingRegex();

    /// <summary>
    /// Ordered list item: number followed by . or ) and space.
    /// </summary>
    [GeneratedRegex(@"^\s*\d+[.)]\s+", RegexOptions.Compiled)]
    private static partial Regex OrderedListRegex();

    /// <summary>
    /// Unordered list item: -, *, or + followed by space.
    /// </summary>
    [GeneratedRegex(@"^\s*[-*+]\s+", RegexOptions.Compiled)]
    private static partial Regex UnorderedListRegex();

    /// <summary>
    /// Blockquote: > optionally followed by space.
    /// </summary>
    [GeneratedRegex(@"^\s*>\s*", RegexOptions.Compiled)]
    private static partial Regex BlockquoteRegex();

    /// <summary>
    /// Fenced code block start: ``` or ~~~ optionally followed by language.
    /// </summary>
    [GeneratedRegex(@"^\s*(```|~~~)", RegexOptions.Compiled)]
    private static partial Regex FencedCodeStartRegex();

    /// <summary>
    /// Indented code: 4 or more spaces at line start.
    /// </summary>
    [GeneratedRegex(@"^(\s{4}|\t)", RegexOptions.Compiled)]
    private static partial Regex IndentedCodeRegex();
}
