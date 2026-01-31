// <copyright file="DefaultAxisProvider.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Modules.Style.Services;

/// <summary>
/// Provides default axis definitions for the Resonance spider chart.
/// </summary>
/// <remarks>
/// <para>LOGIC: Six-axis configuration covering the core writing metrics.</para>
/// <para>Each axis has appropriate normalization ranges and inversion settings.</para>
/// <para>Introduced in v0.3.5a.</para>
/// </remarks>
public sealed class DefaultAxisProvider : IResonanceAxisProvider
{
    private readonly IReadOnlyList<ResonanceAxisDefinition> _axes;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultAxisProvider"/> class.
    /// </summary>
    public DefaultAxisProvider()
    {
        _axes = new List<ResonanceAxisDefinition>
        {
            // LOGIC: Flesch Reading Ease (0-100, higher is better)
            new(
                Name: "Readability",
                MetricKey: "FleschReadingEase",
                Unit: null,
                Description: "Overall reading ease score (0-100)",
                MinValue: 0,
                MaxValue: 100,
                InvertScale: false),

            // LOGIC: Passive voice percentage (0-100%, lower is better)
            new(
                Name: "Clarity",
                MetricKey: "PassiveVoicePercentage",
                Unit: "%",
                Description: "Active voice ratio (lower passive = better)",
                MinValue: 0,
                MaxValue: 50,
                InvertScale: true),

            // LOGIC: Weak word count per 100 words (lower is better)
            new(
                Name: "Precision",
                MetricKey: "WeakWordDensity",
                Unit: "/100w",
                Description: "Precision in word choice (fewer weak words = better)",
                MinValue: 0,
                MaxValue: 20,
                InvertScale: true),

            // LOGIC: Flesch-Kincaid Grade Level (0-18, lower is more accessible)
            new(
                Name: "Accessibility",
                MetricKey: "FleschKincaidGrade",
                Unit: "grade",
                Description: "Reading level accessibility (lower grade = more accessible)",
                MinValue: 0,
                MaxValue: 18,
                InvertScale: true),

            // LOGIC: Average words per sentence (target ~15-20)
            new(
                Name: "Density",
                MetricKey: "AverageWordsPerSentence",
                Unit: "words",
                Description: "Sentence length balance (15-20 words ideal)",
                MinValue: 5,
                MaxValue: 40,
                InvertScale: false),

            // LOGIC: Sentence length variance (moderate variance is ideal)
            new(
                Name: "Flow",
                MetricKey: "SentenceLengthVariance",
                Unit: null,
                Description: "Sentence rhythm and variation",
                MinValue: 0,
                MaxValue: 100,
                InvertScale: false),
        };
    }

    /// <inheritdoc/>
    public IReadOnlyList<ResonanceAxisDefinition> GetAxes() => _axes;
}
