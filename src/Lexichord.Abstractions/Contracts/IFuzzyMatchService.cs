// -----------------------------------------------------------------------
// <copyright file="IFuzzyMatchService.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Provides fuzzy string matching capabilities using Levenshtein distance.
/// </summary>
/// <remarks>
/// <para>
/// LOGIC: This service wraps fuzzy matching algorithms to enable detection of
/// typos and variations of forbidden terminology. It normalizes inputs by
/// trimming whitespace and converting to lowercase before comparison.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations MUST be thread-safe and stateless.
/// All methods can be called concurrently from multiple threads.
/// </para>
/// <para>
/// <b>Version:</b> v0.3.1a - Algorithm Integration
/// </para>
/// </remarks>
public interface IFuzzyMatchService
{
    /// <summary>
    /// Calculates the similarity ratio between two strings using Levenshtein distance.
    /// </summary>
    /// <param name="source">The source string to compare.</param>
    /// <param name="target">The target string to compare against.</param>
    /// <returns>
    /// A ratio from 0 to 100 representing the percentage similarity.
    /// 100 means identical (after normalization), 0 means no similarity.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Both strings are normalized (trimmed, lowercased) before comparison.
    /// If both strings are empty after normalization, returns 100 (identical).
    /// If one is empty and the other is not, returns 0 (no match).
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="source"/> or <paramref name="target"/> is null.
    /// </exception>
    int CalculateRatio(string source, string target);

    /// <summary>
    /// Calculates the partial ratio (best substring match) between two strings.
    /// </summary>
    /// <param name="source">The source string (typically the shorter pattern).</param>
    /// <param name="target">The target string (typically longer, being searched).</param>
    /// <returns>
    /// A ratio from 0 to 100 representing the best partial match.
    /// Useful for finding patterns within longer text.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Finds the best matching substring of the longer string against
    /// the shorter string. Both strings are normalized before comparison.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="source"/> or <paramref name="target"/> is null.
    /// </exception>
    int CalculatePartialRatio(string source, string target);

    /// <summary>
    /// Determines if two strings match above a given similarity threshold.
    /// </summary>
    /// <param name="source">The source string to compare.</param>
    /// <param name="target">The target string to compare against.</param>
    /// <param name="threshold">
    /// Minimum match ratio as a decimal (0.0 to 1.0 inclusive).
    /// Example: 0.80 means 80% similarity required.
    /// </param>
    /// <returns>
    /// <c>true</c> if the calculated ratio is greater than or equal to
    /// <paramref name="threshold"/> × 100; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Convenience method that combines ratio calculation with
    /// threshold comparison. Uses <see cref="CalculateRatio"/> internally.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="source"/> or <paramref name="target"/> is null.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="threshold"/> is not between 0.0 and 1.0 inclusive.
    /// </exception>
    bool IsMatch(string source, string target, double threshold);
}
