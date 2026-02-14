// -----------------------------------------------------------------------
// <copyright file="ContentDeduplicator.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Context;

/// <summary>
/// Utility for detecting duplicate or near-duplicate content between
/// <see cref="Lexichord.Abstractions.Agents.Context.ContextFragment"/> instances
/// using Jaccard word-set similarity.
/// </summary>
/// <remarks>
/// <para>
/// The deduplicator compares fragments by tokenizing their content into word sets
/// and computing the Jaccard similarity coefficient:
/// </para>
/// <code>
/// Jaccard(A, B) = |A ∩ B| / |A ∪ B|
/// </code>
/// <para>
/// Where A and B are sets of normalized words from each fragment's content.
/// A result of 1.0 indicates identical word sets, and 0.0 indicates no overlap.
/// </para>
/// <para>
/// <strong>Normalization Steps:</strong>
/// </para>
/// <list type="number">
///   <item><description>Convert to lowercase for case-insensitive comparison</description></item>
///   <item><description>Split on whitespace into word tokens</description></item>
///   <item><description>Strip punctuation from each word (keep only letters and digits)</description></item>
///   <item><description>Filter out very short words (≤2 characters) to reduce noise from common particles</description></item>
///   <item><description>Deduplicate into a <see cref="HashSet{T}"/> for efficient set operations</description></item>
/// </list>
/// <para>
/// <strong>Introduced in:</strong> v0.7.2c as part of the Context Orchestrator.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Compare two text fragments
/// var similarity = ContentDeduplicator.CalculateJaccardSimilarity(
///     "The quick brown fox jumps over the lazy dog",
///     "The quick brown fox leaps over a lazy dog");
///
/// // similarity ≈ 0.7 (high overlap)
///
/// // Check against threshold
/// if (similarity &gt;= 0.85f)
/// {
///     // Consider these duplicates
/// }
/// </code>
/// </example>
public static class ContentDeduplicator
{
    /// <summary>
    /// Calculates the Jaccard similarity coefficient between two text fragments.
    /// </summary>
    /// <param name="textA">First text fragment to compare.</param>
    /// <param name="textB">Second text fragment to compare.</param>
    /// <returns>
    /// A similarity score from 0.0 (no word overlap) to 1.0 (identical word sets).
    /// Returns 0.0 if either text is null, empty, or contains no qualifying words.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Computes Jaccard similarity using normalized word sets:
    /// </para>
    /// <code>
    /// J(A,B) = |intersection(A, B)| / |union(A, B)|
    /// </code>
    /// <para>
    /// Words are normalized by lowercasing, stripping punctuation, and filtering
    /// out tokens with 2 or fewer characters. This reduces noise from articles,
    /// prepositions, and punctuation-only tokens.
    /// </para>
    /// <para>
    /// <strong>Performance:</strong>
    /// Uses <see cref="HashSet{T}"/> for O(n) set operations. Suitable for
    /// comparing fragment content of typical size (100-5000 words).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Identical content
    /// ContentDeduplicator.CalculateJaccardSimilarity("hello world", "hello world");
    /// // Returns: 1.0
    ///
    /// // Partial overlap
    /// ContentDeduplicator.CalculateJaccardSimilarity("hello world", "goodbye world");
    /// // Returns: ~0.33 (1 shared word out of 3 unique)
    ///
    /// // No overlap
    /// ContentDeduplicator.CalculateJaccardSimilarity("hello world", "completely different");
    /// // Returns: 0.0
    /// </code>
    /// </example>
    public static float CalculateJaccardSimilarity(string textA, string textB)
    {
        // LOGIC: Tokenize both texts into normalized word sets
        var wordsA = TokenizeToWords(textA);
        var wordsB = TokenizeToWords(textB);

        // LOGIC: Return 0 if either set is empty (no meaningful comparison possible)
        if (wordsA.Count == 0 || wordsB.Count == 0)
            return 0f;

        // LOGIC: Compute Jaccard coefficient = |intersection| / |union|
        var intersection = wordsA.Intersect(wordsB).Count();
        var union = wordsA.Union(wordsB).Count();

        return (float)intersection / union;
    }

    /// <summary>
    /// Tokenizes text into a normalized set of words for comparison.
    /// </summary>
    /// <param name="text">Text to tokenize.</param>
    /// <returns>
    /// A <see cref="HashSet{T}"/> of unique, normalized words.
    /// Empty set if text is null or contains no qualifying words.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Normalization pipeline:
    /// </para>
    /// <list type="number">
    ///   <item><description>Lowercase for case-insensitive comparison</description></item>
    ///   <item><description>Split on whitespace</description></item>
    ///   <item><description>Strip punctuation (keep letters and digits only)</description></item>
    ///   <item><description>Filter words with ≤2 characters (noise reduction)</description></item>
    ///   <item><description>Deduplicate into HashSet</description></item>
    /// </list>
    /// <para>
    /// The ≤2 character filter removes common English particles ("a", "an", "in", "is", "it",
    /// "of", "on", "or", "to") and punctuation-only tokens that would inflate similarity
    /// scores between unrelated texts.
    /// </para>
    /// </remarks>
    private static HashSet<string> TokenizeToWords(string text)
    {
        // LOGIC: Guard against null or empty input
        if (string.IsNullOrWhiteSpace(text))
            return new HashSet<string>();

        return text
            .ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(StripPunctuation)
            .Where(w => w.Length > 2)
            .ToHashSet();
    }

    /// <summary>
    /// Strips all non-letter, non-digit characters from a word.
    /// </summary>
    /// <param name="word">Word to clean.</param>
    /// <returns>Word containing only letters and digits.</returns>
    /// <remarks>
    /// LOGIC: Removes punctuation like commas, periods, quotes, and brackets
    /// that would prevent matching of otherwise identical words
    /// (e.g., "hello," vs "hello" or "(world)" vs "world").
    /// </remarks>
    private static string StripPunctuation(string word)
    {
        return new string(word.Where(char.IsLetterOrDigit).ToArray());
    }
}
