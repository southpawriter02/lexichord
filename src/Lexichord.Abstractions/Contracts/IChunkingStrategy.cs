// =============================================================================
// File: IChunkingStrategy.cs
// Project: Lexichord.Abstractions
// Description: Strategy interface for splitting text into chunks, enabling
//              pluggable chunking algorithms for the RAG pipeline.
// =============================================================================
// LOGIC: Strategy pattern interface enabling pluggable chunking algorithms.
//   - Implementations provide different algorithms for chunk boundary detection.
//   - Returns ordered TextChunk lists with position and metadata info.
//   - Contract: empty list for null/empty content, ArgumentNullException for
//     null options.
//   - Mode property enables strategy identification and factory selection.
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Strategy interface for splitting text into chunks.
/// Implementations provide different algorithms for identifying chunk boundaries.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IChunkingStrategy"/> interface defines the contract for all
/// chunking algorithms in the RAG pipeline. Each implementation uses a different
/// approach to identify optimal chunk boundaries — from simple character-count
/// splitting to header-aware Markdown parsing.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations should be stateless and thread-safe,
/// allowing concurrent calls to <see cref="Split"/> from multiple threads.
/// </para>
/// <para>
/// <b>Implementations:</b>
/// </para>
/// <list type="bullet">
///   <item><description><c>FixedSizeChunkingStrategy</c> (v0.4.3b): Character-count based splitting.</description></item>
///   <item><description><c>ParagraphChunkingStrategy</c> (v0.4.3c): Paragraph boundary detection.</description></item>
///   <item><description><c>MarkdownHeaderChunkingStrategy</c> (v0.4.3d): Hierarchical header-based chunking.</description></item>
/// </list>
/// </remarks>
public interface IChunkingStrategy
{
    /// <summary>
    /// Gets the chunking mode this strategy implements.
    /// </summary>
    /// <value>
    /// The <see cref="ChunkingMode"/> value identifying this strategy's algorithm.
    /// </value>
    ChunkingMode Mode { get; }

    /// <summary>
    /// Splits the provided content into chunks.
    /// </summary>
    /// <param name="content">
    /// The text content to split. Must not be null.
    /// </param>
    /// <param name="options">
    /// Chunking configuration options controlling size, overlap, and behavior.
    /// </param>
    /// <returns>
    /// An ordered list of text chunks with position information and metadata.
    /// Returns an empty list for null or empty content.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> is null.
    /// </exception>
    IReadOnlyList<TextChunk> Split(string content, ChunkingOptions options);
}
