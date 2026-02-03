// =============================================================================
// File: MatchDensityCalculator.cs
// Project: Lexichord.Modules.RAG
// Description: Calculates match density for optimal snippet centering.
// =============================================================================
// LOGIC: Finds the position with highest match density.
//   - Scans text with a sliding window.
//   - Weights matches by HighlightType.
//   - Returns the center of the highest-scoring window.
// =============================================================================
// VERSION: v0.5.6c (Smart Truncation)
// =============================================================================

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// Calculates match density to find optimal snippet center positions.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MatchDensityCalculator"/> provides static methods for analyzing
/// match distribution in text content. It uses a sliding window algorithm to
/// find the position with the highest concentration of relevant matches.
/// </para>
/// <para>
/// <b>Algorithm:</b> Scans the text with a configurable window size and step,
/// scoring each window based on the weighted sum of matches it contains.
/// </para>
/// <para>
/// <b>Weights:</b>
/// <list type="bullet">
///   <item><description><see cref="HighlightType.QueryMatch"/>: 2.0</description></item>
///   <item><description><see cref="HighlightType.FuzzyMatch"/>: 1.0</description></item>
///   <item><description><see cref="HighlightType.KeyPhrase"/> and others: 0.5</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> All methods are static and thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6c as part of Smart Truncation.
/// </para>
/// </remarks>
public static class MatchDensityCalculator
{
    /// <summary>
    /// Default sliding window size in characters.
    /// </summary>
    public const int DefaultWindowSize = 100;

    /// <summary>
    /// Default step size for sliding window.
    /// </summary>
    public const int DefaultStepSize = 10;

    /// <summary>
    /// Finds the position with the highest density of matches.
    /// </summary>
    /// <param name="textLength">Length of the text being analyzed.</param>
    /// <param name="matches">List of matches as (position, length, weight) tuples.</param>
    /// <param name="windowSize">Size of the sliding window (default: 100).</param>
    /// <param name="stepSize">Step size for window movement (default: 10).</param>
    /// <returns>
    /// A tuple containing the optimal center position and the density score.
    /// Returns (0, 0) if no matches are provided.
    /// </returns>
    /// <remarks>
    /// The returned position is the center of the highest-scoring window,
    /// suitable for use as the snippet center point.
    /// </remarks>
    public static (int Position, double Score) FindHighestDensityPosition(
        int textLength,
        IReadOnlyList<(int Position, int Length, double Weight)> matches,
        int windowSize = DefaultWindowSize,
        int stepSize = DefaultStepSize)
    {
        if (matches.Count == 0)
        {
            return (0, 0);
        }

        if (matches.Count == 1)
        {
            return (matches[0].Position, matches[0].Weight);
        }

        var bestPosition = matches[0].Position;
        var bestScore = 0.0;

        // LOGIC: Scan text with sliding window.
        for (var windowStart = 0; windowStart < textLength; windowStart += stepSize)
        {
            var windowEnd = windowStart + windowSize;
            var score = 0.0;

            // LOGIC: Sum weights of matches within this window.
            foreach (var match in matches)
            {
                // LOGIC: Check if match overlaps with window.
                var matchEnd = match.Position + match.Length;
                if (match.Position < windowEnd && matchEnd > windowStart)
                {
                    score += match.Weight;
                }
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestPosition = windowStart + (windowSize / 2);
            }
        }

        // LOGIC: Clamp position to valid range.
        bestPosition = Math.Min(bestPosition, textLength);

        return (bestPosition, bestScore);
    }

    /// <summary>
    /// Gets the weight for a given highlight type.
    /// </summary>
    /// <param name="type">The highlight type.</param>
    /// <returns>Weight value for density scoring.</returns>
    /// <remarks>
    /// Weights are:
    /// <list type="bullet">
    ///   <item><description><see cref="HighlightType.QueryMatch"/>: 2.0 (exact matches are highest priority)</description></item>
    ///   <item><description><see cref="HighlightType.FuzzyMatch"/>: 1.0 (fuzzy matches are secondary)</description></item>
    ///   <item><description>Other types: 0.5 (general interest)</description></item>
    /// </list>
    /// </remarks>
    public static double GetMatchWeight(HighlightType type) => type switch
    {
        HighlightType.QueryMatch => 2.0,
        HighlightType.FuzzyMatch => 1.0,
        HighlightType.KeyPhrase => 0.5,
        HighlightType.Entity => 0.5,
        _ => 0.5
    };
}
