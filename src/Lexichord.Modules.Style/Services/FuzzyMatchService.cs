// -----------------------------------------------------------------------
// <copyright file="FuzzyMatchService.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FuzzySharp;
using Lexichord.Abstractions.Contracts;

namespace Lexichord.Modules.Style.Services;

/// <summary>
/// Provides fuzzy string matching using the FuzzySharp library.
/// </summary>
/// <remarks>
/// <para>
/// LOGIC: Wraps FuzzySharp's Levenshtein-based algorithms to provide
/// normalized fuzzy matching. All inputs are trimmed and lowercased
/// before comparison to ensure consistent matching behavior.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This service is stateless and fully thread-safe.
/// </para>
/// <para>
/// <b>Version:</b> v0.3.1a - Algorithm Integration
/// </para>
/// </remarks>
public sealed class FuzzyMatchService : IFuzzyMatchService
{
    /// <inheritdoc/>
    public int CalculateRatio(string source, string target)
    {
        ArgumentNullException.ThrowIfNull(source, nameof(source));
        ArgumentNullException.ThrowIfNull(target, nameof(target));

        var normalizedSource = Normalize(source);
        var normalizedTarget = Normalize(target);

        // LOGIC: Both empty after normalization = identical
        if (string.IsNullOrEmpty(normalizedSource) &&
            string.IsNullOrEmpty(normalizedTarget))
        {
            return 100;
        }

        // LOGIC: One empty, one not = no match
        if (string.IsNullOrEmpty(normalizedSource) ||
            string.IsNullOrEmpty(normalizedTarget))
        {
            return 0;
        }

        return Fuzz.Ratio(normalizedSource, normalizedTarget);
    }

    /// <inheritdoc/>
    public int CalculatePartialRatio(string source, string target)
    {
        ArgumentNullException.ThrowIfNull(source, nameof(source));
        ArgumentNullException.ThrowIfNull(target, nameof(target));

        var normalizedSource = Normalize(source);
        var normalizedTarget = Normalize(target);

        // LOGIC: Both empty after normalization = identical
        if (string.IsNullOrEmpty(normalizedSource) &&
            string.IsNullOrEmpty(normalizedTarget))
        {
            return 100;
        }

        // LOGIC: One empty, one not = no match
        if (string.IsNullOrEmpty(normalizedSource) ||
            string.IsNullOrEmpty(normalizedTarget))
        {
            return 0;
        }

        return Fuzz.PartialRatio(normalizedSource, normalizedTarget);
    }

    /// <inheritdoc/>
    public bool IsMatch(string source, string target, double threshold)
    {
        ArgumentNullException.ThrowIfNull(source, nameof(source));
        ArgumentNullException.ThrowIfNull(target, nameof(target));

        if (threshold < 0.0 || threshold > 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(threshold),
                threshold,
                "Threshold must be between 0.0 and 1.0 inclusive.");
        }

        var ratio = CalculateRatio(source, target);
        var thresholdAsPercent = (int)(threshold * 100);

        return ratio >= thresholdAsPercent;
    }

    /// <summary>
    /// Normalizes a string for fuzzy comparison.
    /// </summary>
    /// <param name="input">The string to normalize.</param>
    /// <returns>The normalized string (trimmed and lowercased).</returns>
    private static string Normalize(string input)
    {
        return input.Trim().ToLowerInvariant();
    }
}
