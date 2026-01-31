// <copyright file="ReadabilityMetrics.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Immutable record containing readability analysis metrics.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.3c - Contains the results of readability analysis including:</para>
/// <list type="bullet">
///   <item>Flesch-Kincaid Grade Level (years of education needed)</item>
///   <item>Gunning Fog Index (years of formal education)</item>
///   <item>Flesch Reading Ease (0-100 scale, higher = easier)</item>
/// </list>
/// <para>Thread-safe: immutable record suitable for concurrent access.</para>
/// </remarks>
/// <example>
/// <code>
/// var metrics = service.Analyze("The cat sat on the mat.");
/// Console.WriteLine($"Grade Level: {metrics.FleschKincaidGradeLevel:F1}");
/// Console.WriteLine($"Reading Ease: {metrics.ReadingEaseInterpretation}");
/// </code>
/// </example>
public record ReadabilityMetrics
{
    /// <summary>
    /// Gets the Flesch-Kincaid Grade Level (0-18+).
    /// </summary>
    /// <remarks>
    /// LOGIC: Represents the U.S. school grade level required to understand the text.
    /// Values typically range from 0 (kindergarten) to 18+ (post-graduate).
    /// </remarks>
    public double FleschKincaidGradeLevel { get; init; }

    /// <summary>
    /// Gets the Gunning Fog Index (0-20+).
    /// </summary>
    /// <remarks>
    /// LOGIC: Estimates years of formal education needed to understand the text.
    /// Ideal range for general writing is 7-8 (high school freshman level).
    /// </remarks>
    public double GunningFogIndex { get; init; }

    /// <summary>
    /// Gets the Flesch Reading Ease score (0-100).
    /// </summary>
    /// <remarks>
    /// LOGIC: Higher scores indicate easier text. Scale:
    /// 90-100: Very Easy (5th grade)
    /// 80-89: Easy (6th grade)
    /// 70-79: Fairly Easy (7th grade)
    /// 60-69: Standard (8th-9th grade)
    /// 50-59: Fairly Difficult (10th-12th grade)
    /// 30-49: Difficult (college)
    /// 0-29: Very Confusing (graduate)
    /// </remarks>
    public double FleschReadingEase { get; init; }

    /// <summary>
    /// Gets the total word count analyzed.
    /// </summary>
    public int WordCount { get; init; }

    /// <summary>
    /// Gets the total sentence count.
    /// </summary>
    public int SentenceCount { get; init; }

    /// <summary>
    /// Gets the total syllable count across all words.
    /// </summary>
    public int SyllableCount { get; init; }

    /// <summary>
    /// Gets the count of complex words (3+ meaningful syllables).
    /// </summary>
    /// <remarks>
    /// LOGIC: Used for Gunning Fog Index calculation. Excludes words
    /// that only reach 3 syllables due to common suffixes (-ing, -ed, -es, -ly).
    /// </remarks>
    public int ComplexWordCount { get; init; }

    /// <summary>
    /// Gets the average number of words per sentence.
    /// </summary>
    /// <remarks>
    /// LOGIC: Computed property. Returns 0 if <see cref="SentenceCount"/> is 0.
    /// </remarks>
    public double AverageWordsPerSentence =>
        SentenceCount > 0 ? (double)WordCount / SentenceCount : 0;

    /// <summary>
    /// Gets the average number of syllables per word.
    /// </summary>
    /// <remarks>
    /// LOGIC: Computed property. Returns 0 if <see cref="WordCount"/> is 0.
    /// </remarks>
    public double AverageSyllablesPerWord =>
        WordCount > 0 ? (double)SyllableCount / WordCount : 0;

    /// <summary>
    /// Gets the ratio of complex words to total words.
    /// </summary>
    /// <remarks>
    /// LOGIC: Computed property. Returns 0 if <see cref="WordCount"/> is 0.
    /// Used in Gunning Fog Index calculation.
    /// </remarks>
    public double ComplexWordRatio =>
        WordCount > 0 ? (double)ComplexWordCount / WordCount : 0;

    /// <summary>
    /// Gets a human-readable interpretation of the Flesch Reading Ease score.
    /// </summary>
    /// <remarks>
    /// LOGIC: Maps the 0-100 score to descriptive categories for UI display.
    /// </remarks>
    public string ReadingEaseInterpretation => FleschReadingEase switch
    {
        >= 90 => "Very Easy",
        >= 80 => "Easy",
        >= 70 => "Fairly Easy",
        >= 60 => "Standard",
        >= 50 => "Fairly Difficult",
        >= 30 => "Difficult",
        _ => "Very Confusing"
    };

    /// <summary>
    /// Gets an empty metrics instance for invalid or empty input.
    /// </summary>
    /// <remarks>
    /// LOGIC: Provides a null-object pattern for edge cases.
    /// All numeric values are 0, interpretation is "Very Confusing".
    /// </remarks>
    public static ReadabilityMetrics Empty { get; } = new()
    {
        FleschKincaidGradeLevel = 0,
        GunningFogIndex = 0,
        FleschReadingEase = 0,
        WordCount = 0,
        SentenceCount = 0,
        SyllableCount = 0,
        ComplexWordCount = 0
    };
}
