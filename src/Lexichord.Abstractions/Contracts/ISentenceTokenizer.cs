// <copyright file="ISentenceTokenizer.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Service for tokenizing text into sentences while respecting abbreviations.
/// </summary>
/// <remarks>
/// LOGIC: v0.3.3a - The sentence tokenizer breaks text into sentences for readability analysis.
/// Uses a dictionary of 50+ common English abbreviations to avoid false sentence breaks.
/// Handles edge cases including ellipsis, initials (J.F.K.), URLs, and decimal numbers.
/// Thread-safe: produces immutable results suitable for concurrent access.
/// </remarks>
public interface ISentenceTokenizer
{
    /// <summary>
    /// Splits text into individual sentences.
    /// </summary>
    /// <param name="text">The input text to tokenize.</param>
    /// <returns>
    /// A read-only list of <see cref="SentenceInfo"/> records containing each sentence's
    /// text, position within the original text, and word count.
    /// Returns an empty list for null, empty, or whitespace-only input.
    /// </returns>
    /// <remarks>
    /// LOGIC: Sentence boundaries are detected at [.!?] followed by whitespace or end-of-text.
    /// Abbreviations like "Mr.", "Dr.", "Inc." are NOT treated as sentence endings.
    /// Period-embedded abbreviations like "U.S.A." and "Ph.D." are handled specially.
    /// </remarks>
    IReadOnlyList<SentenceInfo> Tokenize(string text);
}
