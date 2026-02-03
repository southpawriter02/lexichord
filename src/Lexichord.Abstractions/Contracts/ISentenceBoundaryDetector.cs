// =============================================================================
// File: ISentenceBoundaryDetector.cs
// Project: Lexichord.Abstractions
// Description: Interface for detecting sentence boundaries in text.
// =============================================================================
// LOGIC: Defines the contract for sentence boundary detection.
//   - FindSentenceStart finds the start of the sentence containing a position.
//   - FindSentenceEnd finds the end of the sentence containing a position.
//   - Used by ISnippetService for natural boundary snapping.
//   - Implementation provided in v0.5.6c; stub provided in v0.5.6a.
// =============================================================================
// VERSION: v0.5.6a (Snippet Extraction) - Interface definition
//          v0.5.6c (Sentence Boundary Detection) - Implementation
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Detects sentence boundaries within text content.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ISentenceBoundaryDetector"/> provides methods for finding natural
/// sentence boundaries in text, enabling snippet extraction to produce
/// grammatically coherent excerpts.
/// </para>
/// <para>
/// <b>Boundary Finding:</b> The detector identifies sentence boundaries using
/// punctuation patterns (. ! ? etc.) while accounting for abbreviations,
/// decimal numbers, and other edge cases that contain periods.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe as the detector
/// is registered as a singleton and may be called concurrently.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6a (interface), v0.5.6c (implementation).
/// </para>
/// </remarks>
public interface ISentenceBoundaryDetector
{
    /// <summary>
    /// Finds the start of the sentence containing the given position.
    /// </summary>
    /// <param name="content">The text content to analyze.</param>
    /// <param name="position">The position within the content.</param>
    /// <returns>
    /// The index of the first character of the sentence containing <paramref name="position"/>,
    /// or 0 if no sentence boundary is found before the position.
    /// </returns>
    /// <remarks>
    /// The returned position is the first non-whitespace character after the
    /// previous sentence-ending punctuation (or the start of the content).
    /// </remarks>
    int FindSentenceStart(string content, int position);

    /// <summary>
    /// Finds the end of the sentence containing the given position.
    /// </summary>
    /// <param name="content">The text content to analyze.</param>
    /// <param name="position">The position within the content.</param>
    /// <returns>
    /// The index immediately after the sentence-ending punctuation,
    /// or the length of the content if no sentence boundary is found.
    /// </returns>
    /// <remarks>
    /// The returned position is after the sentence-ending punctuation
    /// (typically . ! or ?), suitable for use as an exclusive end index.
    /// </remarks>
    int FindSentenceEnd(string content, int position);

    /// <summary>
    /// Gets all sentence boundaries in the text.
    /// </summary>
    /// <param name="text">The text to analyze.</param>
    /// <returns>
    /// A list of <see cref="SentenceBoundary"/> records representing each sentence.
    /// Returns an empty list for empty or whitespace-only text.
    /// </returns>
    /// <remarks>
    /// <para>Sentences are identified by terminators (. ! ?) while accounting for
    /// abbreviations and decimal numbers.</para>
    /// <para>Text without explicit terminators is treated as a single sentence.</para>
    /// </remarks>
    IReadOnlyList<SentenceBoundary> GetBoundaries(string text);

    /// <summary>
    /// Finds the nearest word boundary before or at the given position.
    /// </summary>
    /// <param name="text">The text to search.</param>
    /// <param name="position">The starting position.</param>
    /// <returns>Position of the word start (first character of the word).</returns>
    /// <remarks>
    /// Used as a fallback when sentence boundary detection would exceed length limits.
    /// Word boundaries are identified by whitespace characters.
    /// </remarks>
    int FindWordStart(string text, int position);

    /// <summary>
    /// Finds the nearest word boundary after or at the given position.
    /// </summary>
    /// <param name="text">The text to search.</param>
    /// <param name="position">The starting position.</param>
    /// <returns>Position after the word end (suitable as exclusive index).</returns>
    /// <remarks>
    /// Used as a fallback when sentence boundary detection would exceed length limits.
    /// Word boundaries are identified by whitespace characters.
    /// </remarks>
    int FindWordEnd(string text, int position);
}
