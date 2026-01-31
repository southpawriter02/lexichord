// <copyright file="ReadabilityService.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

using System.Diagnostics;
using System.Text.RegularExpressions;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Style.Services;

/// <summary>
/// Implementation of <see cref="IReadabilityService"/> that calculates readability metrics.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.3c - The Readability Calculator provides writing difficulty analysis.</para>
/// <para>Implements three industry-standard formulas:</para>
/// <list type="bullet">
///   <item>Flesch-Kincaid Grade Level - U.S. school grade required</item>
///   <item>Gunning Fog Index - Years of formal education needed</item>
///   <item>Flesch Reading Ease - 0-100 scale (higher = easier)</item>
/// </list>
/// <para>Thread-safe: Stateless service suitable for concurrent access.</para>
/// </remarks>
public sealed partial class ReadabilityService : IReadabilityService
{
    private readonly ISentenceTokenizer _sentenceTokenizer;
    private readonly ISyllableCounter _syllableCounter;
    private readonly ILogger<ReadabilityService> _logger;

    /// <summary>
    /// Regex pattern for extracting words from text.
    /// </summary>
    /// <remarks>
    /// LOGIC: Matches word characters including hyphens for compound words.
    /// Consistent with DocumentTokenizer and SentenceTokenizer patterns.
    /// </remarks>
    [GeneratedRegex(@"\b[\w-]+\b", RegexOptions.Compiled)]
    private static partial Regex WordPattern();

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadabilityService"/> class.
    /// </summary>
    /// <param name="sentenceTokenizer">Service for splitting text into sentences.</param>
    /// <param name="syllableCounter">Service for counting syllables in words.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public ReadabilityService(
        ISentenceTokenizer sentenceTokenizer,
        ISyllableCounter syllableCounter,
        ILogger<ReadabilityService> logger)
    {
        _sentenceTokenizer = sentenceTokenizer ?? throw new ArgumentNullException(nameof(sentenceTokenizer));
        _syllableCounter = syllableCounter ?? throw new ArgumentNullException(nameof(syllableCounter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public ReadabilityMetrics Analyze(string text)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Starting readability analysis for text of length {Length}", text?.Length ?? 0);

        // LOGIC: Handle empty input with null-object pattern
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogDebug("Input is empty or whitespace, returning empty metrics");
            return ReadabilityMetrics.Empty;
        }

        // LOGIC: Step 1 - Tokenize text into sentences using ISentenceTokenizer
        var sentences = _sentenceTokenizer.Tokenize(text);
        var sentenceCount = sentences.Count;

        if (sentenceCount == 0)
        {
            _logger.LogDebug("No sentences detected, returning empty metrics");
            return ReadabilityMetrics.Empty;
        }

        // LOGIC: Step 2 - Extract all words from the text
        var words = WordPattern().Matches(text)
            .Select(m => m.Value)
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .ToList();

        var wordCount = words.Count;

        if (wordCount == 0)
        {
            _logger.LogDebug("No words detected, returning empty metrics");
            return ReadabilityMetrics.Empty;
        }

        // LOGIC: Step 3 - Count syllables and identify complex words
        var totalSyllables = 0;
        var complexWordCount = 0;

        foreach (var word in words)
        {
            var syllables = _syllableCounter.CountSyllables(word);
            totalSyllables += syllables;

            if (_syllableCounter.IsComplexWord(word))
            {
                complexWordCount++;
            }
        }

        // LOGIC: Step 4 - Calculate ratios for formulas
        var avgWordsPerSentence = (double)wordCount / sentenceCount;
        var avgSyllablesPerWord = (double)totalSyllables / wordCount;
        var complexWordRatio = (double)complexWordCount / wordCount;

        _logger.LogDebug(
            "Analysis stats: {Words} words, {Sentences} sentences, {Syllables} syllables, {Complex} complex words",
            wordCount, sentenceCount, totalSyllables, complexWordCount);
        _logger.LogDebug(
            "Ratios: {WordsPerSentence:F2} words/sentence, {SyllablesPerWord:F2} syllables/word, {ComplexRatio:P1} complex",
            avgWordsPerSentence, avgSyllablesPerWord, complexWordRatio);

        // LOGIC: Step 5 - Apply readability formulas
        var fleschKincaid = CalculateFleschKincaid(avgWordsPerSentence, avgSyllablesPerWord);
        var gunningFog = CalculateGunningFog(avgWordsPerSentence, complexWordRatio);
        var fleschReadingEase = CalculateFleschReadingEase(avgWordsPerSentence, avgSyllablesPerWord);

        stopwatch.Stop();
        _logger.LogDebug(
            "Readability analysis complete in {Duration:F2}ms: FK={FleschKincaid:F1}, Fog={Fog:F1}, FRE={Ease:F0}",
            stopwatch.Elapsed.TotalMilliseconds, fleschKincaid, gunningFog, fleschReadingEase);

        return new ReadabilityMetrics
        {
            FleschKincaidGradeLevel = fleschKincaid,
            GunningFogIndex = gunningFog,
            FleschReadingEase = fleschReadingEase,
            WordCount = wordCount,
            SentenceCount = sentenceCount,
            SyllableCount = totalSyllables,
            ComplexWordCount = complexWordCount
        };
    }

    /// <inheritdoc />
    public Task<ReadabilityMetrics> AnalyzeAsync(string text, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        _logger.LogDebug("Starting async readability analysis");
        return Task.Run(() => Analyze(text), ct);
    }

    /// <summary>
    /// Calculates the Flesch-Kincaid Grade Level.
    /// </summary>
    /// <remarks>
    /// LOGIC: Formula: 0.39 × (words/sentences) + 11.8 × (syllables/words) − 15.59
    /// Result represents the U.S. school grade level required to understand the text.
    /// Clamped to minimum 0 (negative values are not meaningful).
    /// </remarks>
    private static double CalculateFleschKincaid(double avgWordsPerSentence, double avgSyllablesPerWord)
    {
        var result = 0.39 * avgWordsPerSentence + 11.8 * avgSyllablesPerWord - 15.59;
        return Math.Max(0, result);
    }

    /// <summary>
    /// Calculates the Gunning Fog Index.
    /// </summary>
    /// <remarks>
    /// LOGIC: Formula: 0.4 × (words/sentences + 100 × complexWordRatio)
    /// Result represents years of formal education needed to understand the text.
    /// Clamped to minimum 0 (negative values are not meaningful).
    /// </remarks>
    private static double CalculateGunningFog(double avgWordsPerSentence, double complexWordRatio)
    {
        var result = 0.4 * (avgWordsPerSentence + 100 * complexWordRatio);
        return Math.Max(0, result);
    }

    /// <summary>
    /// Calculates the Flesch Reading Ease score.
    /// </summary>
    /// <remarks>
    /// LOGIC: Formula: 206.835 − 1.015 × (words/sentences) − 84.6 × (syllables/words)
    /// Result is a 0-100 scale where higher scores indicate easier text.
    /// Clamped to 0-100 range (values outside are not meaningful).
    /// </remarks>
    private static double CalculateFleschReadingEase(double avgWordsPerSentence, double avgSyllablesPerWord)
    {
        var result = 206.835 - 1.015 * avgWordsPerSentence - 84.6 * avgSyllablesPerWord;
        return Math.Clamp(result, 0, 100);
    }
}
