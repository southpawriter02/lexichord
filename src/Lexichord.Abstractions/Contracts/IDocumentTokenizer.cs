// <copyright file="IDocumentTokenizer.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Represents a token extracted from a document with its position information.
/// </summary>
/// <param name="Token">The normalized (lowercase) token text.</param>
/// <param name="StartOffset">The starting character offset in the original document.</param>
/// <param name="EndOffset">The ending character offset in the original document.</param>
/// <remarks>
/// LOGIC: v0.3.1c - Position tracking enables precise violation reporting.
/// </remarks>
public readonly record struct DocumentToken(
    string Token,
    int StartOffset,
    int EndOffset);

/// <summary>
/// Service for tokenizing document text into individual words.
/// </summary>
/// <remarks>
/// LOGIC: v0.3.1c - The document tokenizer breaks text into words for fuzzy matching.
/// Uses regex pattern \b[\w-]+\b to preserve hyphenated words as single tokens.
/// All tokens are normalized to lowercase for case-insensitive matching.
/// </remarks>
public interface IDocumentTokenizer
{
    /// <summary>
    /// Tokenizes the input text into unique, lowercase words.
    /// </summary>
    /// <param name="text">The text to tokenize.</param>
    /// <returns>A set of unique, lowercase tokens.</returns>
    /// <remarks>
    /// LOGIC: Returns a HashSet for O(1) lookup performance.
    /// Hyphenated words are preserved as single tokens.
    /// </remarks>
    IReadOnlySet<string> Tokenize(string text);

    /// <summary>
    /// Tokenizes the input text with position information.
    /// </summary>
    /// <param name="text">The text to tokenize.</param>
    /// <returns>A collection of tokens with their positions in the original text.</returns>
    /// <remarks>
    /// LOGIC: Position tracking enables accurate violation location reporting.
    /// Returns all occurrences, not just unique tokens.
    /// </remarks>
    IReadOnlyList<DocumentToken> TokenizeWithPositions(string text);
}
