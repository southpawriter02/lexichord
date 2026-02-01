// =============================================================================
// File: ParagraphChunkingStrategy.cs
// Project: Lexichord.Modules.RAG
// Description: Chunking strategy that splits text based on paragraph
//              boundaries (double newlines), merges short paragraphs, and
//              splits oversized paragraphs using a fixed-size fallback.
// =============================================================================
// LOGIC: Implements a three-phase chunking algorithm:
//   Phase 1: Split content on paragraph boundaries (\n\n or \r\n\r\n)
//   Phase 2: Process paragraphs sequentially with intelligent merging:
//     - Short paragraphs (< MinSize) accumulate in a buffer
//     - Buffer is flushed when reaching TargetSize
//     - Oversized paragraphs (> MaxSize) use FixedSizeChunkingStrategy fallback
//   Phase 3: Finalize metadata with correct TotalChunks for all chunks
// =============================================================================
// DEPENDENCIES:
//   - IChunkingStrategy (v0.4.3a): Interface contract
//   - ChunkingOptions (v0.4.3a): Configuration parameters
//   - TextChunk, ChunkMetadata (v0.4.3a): Data structures
//   - FixedSizeChunkingStrategy (v0.4.3b): Fallback for oversized paragraphs
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.Chunking;

/// <summary>
/// Chunking strategy that splits text based on paragraph boundaries.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ParagraphChunkingStrategy"/> preserves natural document
/// structure by using double newlines as chunk boundaries. This approach
/// maintains semantic coherence within chunks, making them more suitable
/// for embedding and retrieval operations.
/// </para>
/// <para>
/// <b>Algorithm Overview:</b>
/// </para>
/// <list type="number">
///   <item>
///     <description>
///       Split content on paragraph boundaries (<c>\n\n</c> or <c>\r\n\r\n</c>).
///     </description>
///   </item>
///   <item>
///     <description>
///       For each paragraph:
///       <list type="bullet">
///         <item>If paragraph exceeds <see cref="ChunkingOptions.MaxSize"/>,
///         use <see cref="FixedSizeChunkingStrategy"/> to split it.</item>
///         <item>If paragraph is shorter than <see cref="ChunkingOptions.MinSize"/>,
///         accumulate in buffer until <see cref="ChunkingOptions.TargetSize"/> is reached.</item>
///         <item>Otherwise, flush buffer if non-empty, then add paragraph as its own chunk.</item>
///       </list>
///     </description>
///   </item>
///   <item>
///     <description>
///       Flush any remaining content in the buffer as the final chunk.
///     </description>
///   </item>
/// </list>
/// <para>
/// <b>Configuration:</b> Uses <see cref="ChunkingOptions"/> with:
/// <list type="bullet">
///   <item><see cref="ChunkingOptions.TargetSize"/>: Target chunk size (default: 1000)</item>
///   <item><see cref="ChunkingOptions.MinSize"/>: Minimum size before merging (default: 200)</item>
///   <item><see cref="ChunkingOptions.MaxSize"/>: Maximum size before splitting (default: 2000)</item>
///   <item><see cref="ChunkingOptions.Overlap"/>: Used by fallback strategy (default: 100)</item>
/// </list>
/// </para>
/// </remarks>
public sealed class ParagraphChunkingStrategy : IChunkingStrategy
{
    private readonly ILogger<ParagraphChunkingStrategy> _logger;
    private readonly FixedSizeChunkingStrategy _fallbackStrategy;

    /// <summary>
    /// Separator patterns used to identify paragraph boundaries.
    /// </summary>
    /// <remarks>
    /// Order matters: Windows line endings must be checked first to avoid
    /// partial matches with Unix line endings.
    /// </remarks>
    private static readonly string[] ParagraphSeparators = ["\r\n\r\n", "\n\n"];

    /// <summary>
    /// Initializes a new instance of the <see cref="ParagraphChunkingStrategy"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="fallbackStrategy">
    /// Fixed-size strategy used to split paragraphs that exceed <see cref="ChunkingOptions.MaxSize"/>.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="logger"/> or <paramref name="fallbackStrategy"/> is null.
    /// </exception>
    public ParagraphChunkingStrategy(
        ILogger<ParagraphChunkingStrategy> logger,
        FixedSizeChunkingStrategy fallbackStrategy)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _fallbackStrategy = fallbackStrategy ?? throw new ArgumentNullException(nameof(fallbackStrategy));
    }

    /// <inheritdoc />
    public ChunkingMode Mode => ChunkingMode.Paragraph;

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Splits content into chunks based on paragraph boundaries. Empty
    /// paragraphs are filtered out unless <see cref="ChunkingOptions.IncludeEmptyChunks"/>
    /// is <c>true</c>.
    /// </para>
    /// <para>
    /// The algorithm tracks original offsets in the source document, enabling
    /// precise location of chunks within the original content.
    /// </para>
    /// </remarks>
    public IReadOnlyList<TextChunk> Split(string content, ChunkingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        if (string.IsNullOrEmpty(content))
        {
            _logger.LogDebug("Content is null or empty, returning empty chunk list");
            return [];
        }

        _logger.LogDebug(
            "Splitting {ContentLength} chars using paragraph boundaries with target {TargetSize}, " +
            "min {MinSize}, max {MaxSize}",
            content.Length, options.TargetSize, options.MinSize, options.MaxSize);

        var paragraphs = SplitIntoParagraphs(content);

        _logger.LogDebug("Found {ParagraphCount} paragraphs after splitting", paragraphs.Count);

        var chunks = ProcessParagraphs(content, paragraphs, options);

        // Update TotalChunks in metadata
        var totalChunks = chunks.Count;
        for (var i = 0; i < chunks.Count; i++)
        {
            chunks[i] = chunks[i] with
            {
                Metadata = chunks[i].Metadata with { TotalChunks = totalChunks }
            };
        }

        _logger.LogDebug(
            "Created {ChunkCount} chunks from {ParagraphCount} paragraphs",
            chunks.Count, paragraphs.Count);

        return chunks;
    }

    /// <summary>
    /// Splits content into paragraphs based on double newline boundaries.
    /// </summary>
    /// <param name="content">The content to split.</param>
    /// <returns>
    /// List of tuples containing (paragraph text, start offset in original content).
    /// </returns>
    private List<(string Text, int StartOffset)> SplitIntoParagraphs(string content)
    {
        var paragraphs = new List<(string Text, int StartOffset)>();
        var position = 0;

        while (position < content.Length)
        {
            var (separatorIndex, separatorLength) = FindNextSeparator(content, position);

            if (separatorIndex == -1)
            {
                // No more separators, take rest of content
                var remainingText = content[position..];
                if (!string.IsNullOrWhiteSpace(remainingText))
                {
                    paragraphs.Add((remainingText, position));
                }
                break;
            }

            // Extract paragraph before the separator
            var paragraphText = content[position..separatorIndex];
            if (!string.IsNullOrWhiteSpace(paragraphText))
            {
                paragraphs.Add((paragraphText, position));
            }

            // Move past the separator
            position = separatorIndex + separatorLength;
        }

        _logger.LogTrace("Split content into {Count} non-empty paragraphs", paragraphs.Count);

        return paragraphs;
    }

    /// <summary>
    /// Finds the next paragraph separator in the content.
    /// </summary>
    /// <param name="content">The content to search.</param>
    /// <param name="startIndex">The index to start searching from.</param>
    /// <returns>
    /// Tuple of (separator index, separator length) or (-1, 0) if not found.
    /// </returns>
    private static (int Index, int Length) FindNextSeparator(string content, int startIndex)
    {
        var bestIndex = -1;
        var bestLength = 0;

        foreach (var separator in ParagraphSeparators)
        {
            var index = content.IndexOf(separator, startIndex, StringComparison.Ordinal);
            if (index != -1 && (bestIndex == -1 || index < bestIndex))
            {
                bestIndex = index;
                bestLength = separator.Length;
            }
        }

        return (bestIndex, bestLength);
    }

    /// <summary>
    /// Processes paragraphs into chunks with merging and splitting logic.
    /// </summary>
    /// <param name="originalContent">The original full content for offset calculation.</param>
    /// <param name="paragraphs">List of paragraphs with their offsets.</param>
    /// <param name="options">Chunking configuration.</param>
    /// <returns>List of text chunks.</returns>
    private List<TextChunk> ProcessParagraphs(
        string originalContent,
        List<(string Text, int StartOffset)> paragraphs,
        ChunkingOptions options)
    {
        var chunks = new List<TextChunk>();
        var buffer = new BufferState();
        var chunkIndex = 0;

        foreach (var (text, startOffset) in paragraphs)
        {
            var trimmedText = options.PreserveWhitespace ? text : text.Trim();

            if (string.IsNullOrWhiteSpace(trimmedText) && !options.IncludeEmptyChunks)
            {
                _logger.LogTrace(
                    "Skipping empty paragraph at offset {Offset}",
                    startOffset);
                continue;
            }

            // Check if paragraph exceeds MaxSize - needs fallback splitting
            if (trimmedText.Length > options.MaxSize)
            {
                _logger.LogDebug(
                    "Paragraph at offset {Offset} exceeds MaxSize ({Length} > {MaxSize}), " +
                    "using fallback strategy",
                    startOffset, trimmedText.Length, options.MaxSize);

                // Flush buffer first
                if (buffer.HasContent)
                {
                    chunks.Add(CreateChunk(buffer, chunkIndex++, options));
                    buffer.Clear();
                }

                // Use fallback strategy with adjusted options for this paragraph
                var fallbackOptions = options with { TargetSize = options.MaxSize };
                var fallbackChunks = _fallbackStrategy.Split(trimmedText, fallbackOptions);

                foreach (var fallbackChunk in fallbackChunks)
                {
                    // Adjust offsets to be relative to original content
                    var adjustedChunk = new TextChunk(
                        fallbackChunk.Content,
                        startOffset + fallbackChunk.StartOffset,
                        startOffset + fallbackChunk.EndOffset,
                        new ChunkMetadata(chunkIndex++));

                    chunks.Add(adjustedChunk);
                }

                continue;
            }

            // Check if adding this paragraph would exceed TargetSize
            var projectedLength = buffer.Length + (buffer.HasContent ? 2 : 0) + trimmedText.Length;

            if (buffer.HasContent && projectedLength > options.TargetSize)
            {
                // Flush current buffer before adding new paragraph
                _logger.LogTrace(
                    "Buffer would exceed TargetSize ({Projected} > {Target}), flushing",
                    projectedLength, options.TargetSize);

                chunks.Add(CreateChunk(buffer, chunkIndex++, options));
                buffer.Clear();
            }

            // Add paragraph to buffer
            buffer.Append(trimmedText, startOffset, startOffset + text.Length);
        }

        // Flush remaining buffer
        if (buffer.HasContent)
        {
            chunks.Add(CreateChunk(buffer, chunkIndex, options));
        }

        return chunks;
    }

    /// <summary>
    /// Creates a TextChunk from the current buffer state.
    /// </summary>
    /// <param name="buffer">The buffer containing accumulated content.</param>
    /// <param name="index">The chunk index.</param>
    /// <param name="options">Chunking options for trimming behavior.</param>
    /// <returns>A new TextChunk instance.</returns>
    private TextChunk CreateChunk(BufferState buffer, int index, ChunkingOptions options)
    {
        var content = buffer.GetContent();
        if (!options.PreserveWhitespace)
        {
            content = content.Trim();
        }

        _logger.LogTrace(
            "Creating chunk {Index}: {Length} chars, offsets [{Start}..{End}]",
            index, content.Length, buffer.StartOffset, buffer.EndOffset);

        return new TextChunk(
            content,
            buffer.StartOffset,
            buffer.EndOffset,
            new ChunkMetadata(index));
    }

    /// <summary>
    /// Internal state for accumulating paragraphs into a single chunk.
    /// </summary>
    private sealed class BufferState
    {
        private readonly List<string> _paragraphs = [];

        /// <summary>Start offset in original content.</summary>
        public int StartOffset { get; private set; } = -1;

        /// <summary>End offset in original content.</summary>
        public int EndOffset { get; private set; } = -1;

        /// <summary>Total length of buffered content including separators.</summary>
        public int Length { get; private set; }

        /// <summary>Whether the buffer contains any content.</summary>
        public bool HasContent => _paragraphs.Count > 0;

        /// <summary>
        /// Appends a paragraph to the buffer.
        /// </summary>
        /// <param name="text">The paragraph text.</param>
        /// <param name="startOffset">Start offset in original content.</param>
        /// <param name="endOffset">End offset in original content.</param>
        public void Append(string text, int startOffset, int endOffset)
        {
            if (StartOffset == -1)
            {
                StartOffset = startOffset;
            }

            EndOffset = endOffset;

            // Add separator if not first paragraph
            if (_paragraphs.Count > 0)
            {
                Length += 2; // "\n\n" separator
            }

            _paragraphs.Add(text);
            Length += text.Length;
        }

        /// <summary>
        /// Gets the combined content of all buffered paragraphs.
        /// </summary>
        /// <returns>Paragraphs joined with double newlines.</returns>
        public string GetContent() => string.Join("\n\n", _paragraphs);

        /// <summary>
        /// Clears the buffer for reuse.
        /// </summary>
        public void Clear()
        {
            _paragraphs.Clear();
            StartOffset = -1;
            EndOffset = -1;
            Length = 0;
        }
    }
}
