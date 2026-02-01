// =============================================================================
// File: MarkdownHeaderChunkingStrategy.cs
// Project: Lexichord.Modules.RAG
// Description: Chunking strategy that splits Markdown content based on header
//              structure, creating hierarchical chunks with header metadata.
// =============================================================================
// LOGIC: Implements a header-aware Markdown chunking algorithm:
//   1. Parses Markdown using Markdig to extract HeadingBlock elements.
//   2. Groups content into sections based on header hierarchy.
//   3. Higher-level headers (H1) create boundaries that end lower-level sections.
//   4. Preamble content before the first header becomes its own chunk.
//   5. Oversized sections are split using FixedSizeChunkingStrategy as fallback.
//   6. Preserves header text and level in chunk metadata.
// =============================================================================
// SPECIFICATION: LCS-DES-v0.4.3d (Markdown Header Chunking Strategy)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.Chunking;

/// <summary>
/// Chunking strategy that splits Markdown content based on header structure.
/// Creates hierarchical chunks respecting heading levels (H1 > H2 > H3, etc.).
/// </summary>
/// <remarks>
/// <para>
/// This strategy uses Markdig to parse Markdown and identify header boundaries.
/// Each section (from one header to the next same-or-higher level header) becomes
/// a chunk. Content before the first header (preamble) is handled separately.
/// </para>
/// <para>
/// For content without headers, the strategy falls back to ParagraphChunkingStrategy.
/// For oversized sections, the strategy uses FixedSizeChunkingStrategy to split
/// while preserving header metadata.
/// </para>
/// </remarks>
public sealed class MarkdownHeaderChunkingStrategy : IChunkingStrategy
{
    // -------------------------------------------------------------------------
    // Dependencies
    // -------------------------------------------------------------------------

    private readonly ILogger<MarkdownHeaderChunkingStrategy> _logger;
    private readonly ParagraphChunkingStrategy _paragraphFallback;
    private readonly FixedSizeChunkingStrategy _fixedSizeFallback;
    private readonly MarkdownPipeline _pipeline;

    // -------------------------------------------------------------------------
    // Constants
    // -------------------------------------------------------------------------

    /// <summary>
    /// Separator used between merged chunks from fallback splitting.
    /// </summary>
    private const string ChunkSeparator = "\n\n";

    // -------------------------------------------------------------------------
    // Constructor
    // -------------------------------------------------------------------------

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkdownHeaderChunkingStrategy"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="paragraphFallback">
    /// Fallback strategy for content without headers.
    /// </param>
    /// <param name="fixedSizeFallback">
    /// Fallback strategy for splitting oversized sections.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="logger"/>, <paramref name="paragraphFallback"/>,
    /// or <paramref name="fixedSizeFallback"/> is null.
    /// </exception>
    public MarkdownHeaderChunkingStrategy(
        ILogger<MarkdownHeaderChunkingStrategy> logger,
        ParagraphChunkingStrategy paragraphFallback,
        FixedSizeChunkingStrategy fixedSizeFallback)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _paragraphFallback = paragraphFallback ?? throw new ArgumentNullException(nameof(paragraphFallback));
        _fixedSizeFallback = fixedSizeFallback ?? throw new ArgumentNullException(nameof(fixedSizeFallback));

        // LOGIC: Configure Markdig pipeline with minimal extensions.
        //        We need accurate line/column positions for offset calculation.
        _pipeline = new MarkdownPipelineBuilder()
            .UsePreciseSourceLocation()
            .Build();

        _logger.LogDebug(
            "[MarkdownHeaderChunker] Initialized with ParagraphChunkingStrategy and " +
            "FixedSizeChunkingStrategy fallbacks");
    }

    // -------------------------------------------------------------------------
    // IChunkingStrategy Implementation
    // -------------------------------------------------------------------------

    /// <summary>
    /// Gets the chunking mode identifier for this strategy.
    /// </summary>
    /// <value>Returns <see cref="ChunkingMode.MarkdownHeader"/>.</value>
    public ChunkingMode Mode => ChunkingMode.MarkdownHeader;

    /// <summary>
    /// Splits Markdown content into chunks based on header structure.
    /// </summary>
    /// <param name="content">The Markdown content to split.</param>
    /// <param name="options">Configuration options controlling chunk sizes.</param>
    /// <returns>
    /// A list of <see cref="TextChunk"/> objects, each representing a section
    /// of the document defined by header boundaries.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The algorithm processes headers in order of appearance, creating chunks
    /// at each header boundary. A section ends when a header of the same or
    /// higher level is encountered (e.g., H2 ends at H1 or H2, but not H3).
    /// </para>
    /// <para>
    /// If the content contains no headers, the strategy delegates to
    /// <see cref="ParagraphChunkingStrategy"/>. If a section exceeds
    /// <see cref="ChunkingOptions.MaxSize"/>, it is split using
    /// <see cref="FixedSizeChunkingStrategy"/> while preserving header metadata.
    /// </para>
    /// </remarks>
    public IReadOnlyList<TextChunk> Split(string content, ChunkingOptions options)
    {
        // GUARD: Validate required parameters.
        ArgumentNullException.ThrowIfNull(options, nameof(options));

        // GUARD: Handle empty or null content.
        if (string.IsNullOrEmpty(content))
        {
            _logger.LogDebug("[MarkdownHeaderChunker] Content is null or empty, returning empty list");
            return [];
        }

        _logger.LogDebug(
            "[MarkdownHeaderChunker] Splitting content of {Length} characters with " +
            "TargetSize={TargetSize}, MaxSize={MaxSize}",
            content.Length,
            options.TargetSize,
            options.MaxSize);

        // STEP 1: Parse Markdown to extract header information.
        var document = Markdown.Parse(content, _pipeline);
        var headers = ExtractHeaders(document, content);

        // STEP 2: If no headers found, fall back to paragraph chunking.
        if (headers.Count == 0)
        {
            _logger.LogDebug(
                "[MarkdownHeaderChunker] No headers found, falling back to " +
                "ParagraphChunkingStrategy");
            return _paragraphFallback.Split(content, options);
        }

        _logger.LogDebug(
            "[MarkdownHeaderChunker] Found {HeaderCount} headers in document",
            headers.Count);

        // STEP 3: Create sections from header boundaries.
        var sections = CreateSections(headers, content);

        _logger.LogDebug(
            "[MarkdownHeaderChunker] Created {SectionCount} sections from headers",
            sections.Count);

        // STEP 4: Convert sections to chunks, splitting oversized ones.
        var chunks = ProcessSections(sections, content, options);

        // STEP 5: Update metadata with final chunk count.
        return FinalizeChunks(chunks);
    }

    // -------------------------------------------------------------------------
    // Header Extraction
    // -------------------------------------------------------------------------

    /// <summary>
    /// Extracts header information from a parsed Markdown document.
    /// </summary>
    /// <param name="document">The parsed Markdown document.</param>
    /// <param name="content">The original content for text extraction.</param>
    /// <returns>List of header information ordered by position.</returns>
    private List<HeaderInfo> ExtractHeaders(MarkdownDocument document, string content)
    {
        var headers = new List<HeaderInfo>();

        foreach (var block in document.Descendants<HeadingBlock>())
        {
            // LOGIC: Extract header text from inline content.
            var headerText = ExtractHeaderText(block);

            // LOGIC: Calculate character offset from line/column.
            var startOffset = block.Span.Start;

            headers.Add(new HeaderInfo
            {
                Level = block.Level,
                Text = headerText,
                StartOffset = startOffset,
                LineNumber = block.Line + 1 // Markdig uses 0-based lines
            });

            _logger.LogTrace(
                "[MarkdownHeaderChunker] Found H{Level} at offset {Offset}: '{Text}'",
                block.Level,
                startOffset,
                headerText.Length > 50 ? headerText[..50] + "..." : headerText);
        }

        // LOGIC: Sort by position to ensure correct order.
        headers.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));

        return headers;
    }

    /// <summary>
    /// Extracts the plain text content from a header block.
    /// </summary>
    /// <param name="block">The heading block to extract text from.</param>
    /// <returns>The plain text of the header, with formatting removed.</returns>
    private static string ExtractHeaderText(HeadingBlock block)
    {
        // LOGIC: Walk inline content to extract text only.
        //        This handles bold, italic, code, links, etc.
        if (block.Inline == null)
        {
            return string.Empty;
        }

        var textBuilder = new System.Text.StringBuilder();

        // LOGIC: Iterate through all inline elements in order.
        //        LiteralInline contains regular text, CodeInline contains code spans.
        foreach (var inline in block.Inline)
        {
            ExtractInlineText(inline, textBuilder);
        }

        return textBuilder.ToString().Trim();
    }

    /// <summary>
    /// Recursively extracts text from an inline element.
    /// </summary>
    /// <param name="inline">The inline element to extract from.</param>
    /// <param name="builder">StringBuilder to append text to.</param>
    private static void ExtractInlineText(Inline inline, System.Text.StringBuilder builder)
    {
        switch (inline)
        {
            case LiteralInline literal:
                builder.Append(literal.Content);
                break;

            case CodeInline code:
                builder.Append(code.Content);
                break;

            case ContainerInline container:
                foreach (var child in container)
                {
                    ExtractInlineText(child, builder);
                }
                break;
        }
    }

    // -------------------------------------------------------------------------
    // Section Creation
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates document sections from header boundaries.
    /// </summary>
    /// <param name="headers">List of headers in the document.</param>
    /// <param name="content">The original document content.</param>
    /// <returns>List of sections with content and header metadata.</returns>
    private List<SectionInfo> CreateSections(List<HeaderInfo> headers, string content)
    {
        var sections = new List<SectionInfo>();

        // STEP 1: Handle preamble (content before first header).
        if (headers.Count > 0 && headers[0].StartOffset > 0)
        {
            var preambleContent = content[..headers[0].StartOffset].Trim();

            if (!string.IsNullOrWhiteSpace(preambleContent))
            {
                sections.Add(new SectionInfo
                {
                    Content = preambleContent,
                    StartOffset = 0,
                    EndOffset = headers[0].StartOffset,
                    HeaderLevel = null,
                    HeaderText = null
                });

                _logger.LogDebug(
                    "[MarkdownHeaderChunker] Added preamble section of {Length} characters",
                    preambleContent.Length);
            }
        }

        // STEP 2: Create a section for each header.
        for (var i = 0; i < headers.Count; i++)
        {
            var header = headers[i];

            // LOGIC: Find where this section ends.
            //        A section ends at the next header of same or higher level,
            //        or at the end of the document.
            var endOffset = content.Length;

            for (var j = i + 1; j < headers.Count; j++)
            {
                // RULE: Same or higher level header ends this section.
                //       H1 ends at H1, H2 ends at H1 or H2, etc.
                if (headers[j].Level <= header.Level)
                {
                    endOffset = headers[j].StartOffset;
                    break;
                }
            }

            var sectionContent = content[header.StartOffset..endOffset].Trim();

            if (!string.IsNullOrWhiteSpace(sectionContent))
            {
                sections.Add(new SectionInfo
                {
                    Content = sectionContent,
                    StartOffset = header.StartOffset,
                    EndOffset = endOffset,
                    HeaderLevel = header.Level,
                    HeaderText = header.Text
                });
            }
        }

        return sections;
    }

    // -------------------------------------------------------------------------
    // Section Processing
    // -------------------------------------------------------------------------

    /// <summary>
    /// Converts sections to chunks, splitting oversized sections as needed.
    /// </summary>
    /// <param name="sections">The sections to process.</param>
    /// <param name="content">The original document content.</param>
    /// <param name="options">Chunking configuration options.</param>
    /// <returns>List of text chunks.</returns>
    private List<TextChunk> ProcessSections(
        List<SectionInfo> sections,
        string content,
        ChunkingOptions options)
    {
        var chunks = new List<TextChunk>();
        var chunkIndex = 0;

        foreach (var section in sections)
        {
            // LOGIC: Check if section exceeds MaxSize.
            if (section.Content.Length > options.MaxSize)
            {
                _logger.LogDebug(
                    "[MarkdownHeaderChunker] Section at offset {Offset} exceeds MaxSize " +
                    "({Length} > {MaxSize}), splitting with fallback",
                    section.StartOffset,
                    section.Content.Length,
                    options.MaxSize);

                // LOGIC: Split oversized section using fixed-size fallback.
                var subChunks = _fixedSizeFallback.Split(section.Content, options);

                foreach (var subChunk in subChunks)
                {
                    chunks.Add(new TextChunk(
                        Content: subChunk.Content,
                        StartOffset: section.StartOffset + subChunk.StartOffset,
                        EndOffset: section.StartOffset + subChunk.EndOffset,
                        Metadata: new ChunkMetadata(
                            Index: chunkIndex++,
                            Level: section.HeaderLevel ?? 0,
                            Heading: section.HeaderText
                        ) { TotalChunks = 0 } // Will be updated in finalization
                    ));
                }
            }
            else
            {
                // LOGIC: Section fits within MaxSize, create single chunk.
                chunks.Add(new TextChunk(
                    Content: section.Content,
                    StartOffset: section.StartOffset,
                    EndOffset: section.EndOffset,
                    Metadata: new ChunkMetadata(
                        Index: chunkIndex++,
                        Level: section.HeaderLevel ?? 0,
                        Heading: section.HeaderText
                    ) { TotalChunks = 0 } // Will be updated in finalization
                ));
            }
        }

        return chunks;
    }

    /// <summary>
    /// Updates all chunks with final metadata (TotalChunks, IsFirst, IsLast).
    /// </summary>
    /// <param name="chunks">The chunks to finalize.</param>
    /// <returns>Finalized list of chunks with updated metadata.</returns>
    private static IReadOnlyList<TextChunk> FinalizeChunks(List<TextChunk> chunks)
    {
        if (chunks.Count == 0)
        {
            return chunks;
        }

        var totalChunks = chunks.Count;
        var result = new List<TextChunk>(totalChunks);

        for (var i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            result.Add(chunk with
            {
                Metadata = chunk.Metadata with
                {
                    Index = i,
                    TotalChunks = totalChunks
                }
            });
        }

        return result;
    }

    // -------------------------------------------------------------------------
    // Internal Data Structures
    // -------------------------------------------------------------------------

    /// <summary>
    /// Internal record holding extracted header information.
    /// </summary>
    private record struct HeaderInfo
    {
        /// <summary>Header level (1-6).</summary>
        public int Level { get; init; }

        /// <summary>Plain text content of the header.</summary>
        public string Text { get; init; }

        /// <summary>Character offset from start of document.</summary>
        public int StartOffset { get; init; }

        /// <summary>Line number (1-based).</summary>
        public int LineNumber { get; init; }
    }

    /// <summary>
    /// Internal record holding section information.
    /// </summary>
    private record struct SectionInfo
    {
        /// <summary>The content of this section.</summary>
        public string Content { get; init; }

        /// <summary>Character offset of section start.</summary>
        public int StartOffset { get; init; }

        /// <summary>Character offset of section end.</summary>
        public int EndOffset { get; init; }

        /// <summary>Heading level (null for preamble).</summary>
        public int? HeaderLevel { get; init; }

        /// <summary>Heading text (null for preamble).</summary>
        public string? HeaderText { get; init; }
    }
}
