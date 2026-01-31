// <copyright file="VoiceAnalysisResult.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Combined results from voice-related text analysis.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.7b - Aggregates passive voice detection and weak word scanning results.</para>
/// </remarks>
public record VoiceAnalysisResult
{
    /// <summary>
    /// Gets or sets the list of passive voice matches detected.
    /// </summary>
    public IReadOnlyList<PassiveVoiceMatch> PassiveVoiceMatches { get; init; } =
        Array.Empty<PassiveVoiceMatch>();

    /// <summary>
    /// Gets or sets the weak word scanning statistics.
    /// </summary>
    public WeakWordStats? WeakWordStats { get; init; }

    /// <summary>
    /// Gets an empty result for initialization or error cases.
    /// </summary>
    public static VoiceAnalysisResult Empty => new();

    /// <summary>
    /// Gets the total count of voice-related issues found.
    /// </summary>
    public int TotalIssueCount =>
        PassiveVoiceMatches.Count + (WeakWordStats?.TotalWeakWords ?? 0);
}

