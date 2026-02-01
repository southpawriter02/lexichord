// =============================================================================
// File: FixedSizeChunkingStrategy.cs
// Project: Lexichord.Modules.RAG
// Description: Fixed-size chunking strategy that splits text into chunks of
//              approximately equal character count with configurable overlap.
// =============================================================================
// LOGIC: Implements the fundamental fixed-size chunking algorithm for RAG.
//   - Splits text into chunks of configurable target size.
//   - Applies overlap between consecutive chunks for context continuity.
//   - Respects word boundaries to avoid mid-word splits when possible.
//   - Two-phase word boundary search: backward 20%, then forward 10%.
//   - Serves as fallback for other strategies when handling oversized sections.
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.Chunking;

/// <summary>
/// Splits text into fixed-size chunks with configurable overlap.
/// Respects word boundaries to avoid mid-word splits when possible.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="FixedSizeChunkingStrategy"/> is the foundational chunking algorithm
/// for the RAG pipeline. It divides text into approximately equal-sized segments based
/// on character count, with configurable overlap to maintain context across chunk boundaries.
/// </para>
/// <para>
/// <b>Word Boundary Algorithm:</b> When <see cref="ChunkingOptions.RespectWordBoundaries"/>
/// is enabled, the strategy uses a two-phase search to find optimal split points:
/// </para>
/// <list type="number">
///   <item><description>Backward search: Scan the last 20% of the target size for whitespace.</description></item>
///   <item><description>Forward search: If no space found backward, scan forward up to 10% overage.</description></item>
///   <item><description>Accept split: If no suitable boundary found, accept mid-word split at target.</description></item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> This class is stateless and thread-safe. Multiple concurrent
/// calls to <see cref="Split"/> are supported.
/// </para>
/// <para>
/// <b>Usage:</b> This strategy is appropriate for unstructured text, code, or as a
/// fallback when paragraph or header-based strategies encounter oversized sections.
/// </para>
/// </remarks>
public sealed class FixedSizeChunkingStrategy : IChunkingStrategy
{
    private readonly ILogger<FixedSizeChunkingStrategy> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FixedSizeChunkingStrategy"/> class.
    /// </summary>
    /// <param name="logger">The logger instance for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="logger"/> is null.
    /// </exception>
    public FixedSizeChunkingStrategy(ILogger<FixedSizeChunkingStrategy> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    /// <value>
    /// Always returns <see cref="ChunkingMode.FixedSize"/>.
    /// </value>
    public ChunkingMode Mode => ChunkingMode.FixedSize;

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// The algorithm processes the content in a single pass, creating chunks of
    /// approximately <see cref="ChunkingOptions.TargetSize"/> characters. Each chunk
    /// overlaps with the previous by <see cref="ChunkingOptions.Overlap"/> characters
    /// to maintain context continuity.
    /// </para>
    /// <para>
    /// <b>Edge Cases:</b>
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Empty or null content returns an empty list.</description></item>
    ///   <item><description>Content smaller than target size returns a single chunk.</description></item>
    ///   <item><description>Very long words without spaces split mid-word as a last resort.</description></item>
    ///   <item><description>Unicode characters are handled correctly (character-based, not byte-based).</description></item>
    /// </list>
    /// </remarks>
    public IReadOnlyList<TextChunk> Split(string content, ChunkingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        // LOGIC: Return empty list for null or empty content per interface contract.
        if (string.IsNullOrEmpty(content))
        {
            _logger.LogDebug("Content is null or empty, returning empty chunk list");
            return Array.Empty<TextChunk>();
        }

        _logger.LogDebug(
            "Splitting {ContentLength} chars with target {TargetSize}, overlap {Overlap}",
            content.Length, options.TargetSize, options.Overlap);

        var chunks = new List<TextChunk>();
        var position = 0;
        var index = 0;

        // LOGIC: Main chunking loop - process until all content is consumed.
        while (position < content.Length)
        {
            // Calculate the end position for this chunk.
            var end = CalculateEndPosition(content, position, options);

            // Extract the chunk content with optional whitespace handling.
            var chunkContent = ExtractChunk(content, position, end, options);

            // LOGIC: Only add chunks with meaningful content unless IncludeEmptyChunks is set.
            if (!string.IsNullOrWhiteSpace(chunkContent) || options.IncludeEmptyChunks)
            {
                chunks.Add(new TextChunk(
                    chunkContent,
                    position,
                    end,
                    new ChunkMetadata(index++)));
            }

            // LOGIC: If we've reached the end of content, exit the loop.
            // This prevents creating overlapping chunks when content is smaller than target.
            if (end >= content.Length)
            {
                break;
            }

            // LOGIC: Advance position with overlap subtraction for context continuity.
            // The Math.Max ensures we always make progress to prevent infinite loops.
            var advance = end - position - options.Overlap;
            position += Math.Max(advance, 1);
        }

        // LOGIC: Update all chunks with the total count for navigation helpers.
        var totalChunks = chunks.Count;
        for (var i = 0; i < chunks.Count; i++)
        {
            chunks[i] = chunks[i] with
            {
                Metadata = chunks[i].Metadata with { TotalChunks = totalChunks }
            };
        }

        _logger.LogDebug(
            "Created {ChunkCount} chunks with {Overlap} char overlap",
            chunks.Count, options.Overlap);

        return chunks;
    }

    /// <summary>
    /// Calculates the end position for a chunk, respecting word boundaries if enabled.
    /// </summary>
    /// <param name="content">The full content being chunked.</param>
    /// <param name="position">The start position of the current chunk.</param>
    /// <param name="options">The chunking configuration options.</param>
    /// <returns>
    /// The calculated end position for the chunk, adjusted for word boundaries if applicable.
    /// </returns>
    private int CalculateEndPosition(string content, int position, ChunkingOptions options)
    {
        // LOGIC: Calculate the ideal end position based on target size.
        var idealEnd = Math.Min(position + options.TargetSize, content.Length);

        // LOGIC: No adjustment needed if we're at the end of the document.
        if (idealEnd >= content.Length)
        {
            return content.Length;
        }

        // LOGIC: Skip word boundary adjustment if disabled.
        if (!options.RespectWordBoundaries)
        {
            return idealEnd;
        }

        return FindWordBoundary(content, position, idealEnd, options.TargetSize);
    }

    /// <summary>
    /// Finds the best word boundary near the ideal end position.
    /// </summary>
    /// <param name="content">The full content being chunked.</param>
    /// <param name="start">The start position of the current chunk.</param>
    /// <param name="idealEnd">The ideal end position based on target size.</param>
    /// <param name="targetSize">The target chunk size for calculating search bounds.</param>
    /// <returns>
    /// The position of the best word boundary, or the ideal end if none found.
    /// </returns>
    /// <remarks>
    /// Uses a two-phase search strategy:
    /// <list type="number">
    ///   <item><description>Phase 1: Search backward from idealEnd within the last 20% of target size.</description></item>
    ///   <item><description>Phase 2: If not found, search forward up to 10% beyond target size.</description></item>
    ///   <item><description>Phase 3: If still not found, accept the split at idealEnd.</description></item>
    /// </list>
    /// </remarks>
    private static int FindWordBoundary(string content, int start, int idealEnd, int targetSize)
    {
        // LOGIC: Phase 1 - Search backward within the last 20% of target size.
        // This keeps chunks close to the target size while finding natural breaks.
        var backwardSearchStart = Math.Max(start, idealEnd - (int)(targetSize * 0.2));

        for (var i = idealEnd - 1; i >= backwardSearchStart; i--)
        {
            if (char.IsWhiteSpace(content[i]))
            {
                // LOGIC: Return position after the whitespace character.
                return i + 1;
            }
        }

        // LOGIC: Phase 2 - Search forward up to 10% beyond target size.
        // This allows slight overage to avoid mid-word splits.
        var forwardSearchEnd = Math.Min(content.Length, idealEnd + (int)(targetSize * 0.1));

        for (var i = idealEnd; i < forwardSearchEnd; i++)
        {
            if (char.IsWhiteSpace(content[i]))
            {
                return i + 1;
            }
        }

        // LOGIC: Phase 3 - No suitable boundary found, accept mid-word split.
        return idealEnd;
    }

    /// <summary>
    /// Extracts the chunk content from the specified range.
    /// </summary>
    /// <param name="content">The full content being chunked.</param>
    /// <param name="start">The start position of the chunk.</param>
    /// <param name="end">The end position of the chunk.</param>
    /// <param name="options">The chunking configuration options.</param>
    /// <returns>
    /// The extracted content, optionally trimmed based on
    /// <see cref="ChunkingOptions.PreserveWhitespace"/>.
    /// </returns>
    private static string ExtractChunk(
        string content, int start, int end, ChunkingOptions options)
    {
        var chunk = content[start..end];
        return options.PreserveWhitespace ? chunk : chunk.Trim();
    }
}
