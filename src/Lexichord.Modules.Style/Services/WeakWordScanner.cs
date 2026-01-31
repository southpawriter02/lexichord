// <copyright file="WeakWordScanner.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

using System.Text.RegularExpressions;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Style.Services;

/// <summary>
/// Scans text for weak words (adverbs, weasel words, fillers) based on Voice Profile settings.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.4c - Uses word tokenization with case-insensitive matching
/// against curated word lists.</para>
/// <para>Scanning behavior depends on the Voice Profile:</para>
/// <list type="bullet">
///   <item><c>FlagAdverbs = true</c>: Adverbs are detected and returned</item>
///   <item><c>FlagWeaselWords = true</c>: Weasel words are detected and returned</item>
///   <item>Filler words are ALWAYS detected regardless of profile settings</item>
/// </list>
/// <para>Feature-gated to Writer Pro tier.</para>
/// </remarks>
public sealed class WeakWordScanner : IWeakWordScanner
{
    private readonly ILogger<WeakWordScanner> _logger;

    /// <summary>
    /// Regex pattern for word boundary tokenization.
    /// </summary>
    /// <remarks>
    /// LOGIC: Matches sequences of word characters (\w+) which includes
    /// letters, digits, and underscores. This provides basic word extraction
    /// without complex NLP tokenization.
    /// </remarks>
    private static readonly Regex WordBoundaryPattern = new(
        @"\b\w+\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Initializes a new instance of the <see cref="WeakWordScanner"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
    public WeakWordScanner(ILogger<WeakWordScanner> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IReadOnlyList<WeakWordMatch> Scan(string text, VoiceProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogTrace("Empty text provided, returning no matches");
            return Array.Empty<WeakWordMatch>();
        }

        _logger.LogDebug(
            "Scanning text ({Length} chars) with profile '{ProfileName}' " +
            "(FlagAdverbs={FlagAdverbs}, FlagWeaselWords={FlagWeaselWords})",
            text.Length,
            profile.Name,
            profile.FlagAdverbs,
            profile.FlagWeaselWords);

        var matches = new List<WeakWordMatch>();
        var matchedRanges = new HashSet<int>();

        // LOGIC: Tokenize text into words using regex
        var wordMatches = WordBoundaryPattern.Matches(text);

        foreach (Match wordMatch in wordMatches)
        {
            var word = wordMatch.Value;
            var position = wordMatch.Index;

            // LOGIC: Skip if this position was already covered by a previous match
            // (shouldn't happen with word boundaries, but added for safety)
            if (matchedRanges.Contains(position))
            {
                continue;
            }

            var category = CategorizeWord(word, profile);
            if (category.HasValue)
            {
                var endPos = position + word.Length;
                matches.Add(new WeakWordMatch(
                    Word: word.ToLowerInvariant(),
                    Category: category.Value,
                    StartIndex: position,
                    EndIndex: endPos));

                matchedRanges.Add(position);

                _logger.LogTrace(
                    "Weak word found: '{Word}' category {Category} at position {Position}",
                    word,
                    category.Value,
                    position);
            }
        }

        // LOGIC: Sort matches by position for consistent ordering
        matches.Sort((a, b) => a.StartIndex.CompareTo(b.StartIndex));

        _logger.LogDebug(
            "Scan completed: found {MatchCount} weak words in {WordCount} total words",
            matches.Count,
            wordMatches.Count);

        return matches.AsReadOnly();
    }

    /// <inheritdoc />
    public WeakWordStats GetStatistics(string text, VoiceProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogTrace("Empty text provided, returning empty statistics");
            return new WeakWordStats(
                TotalWords: 0,
                TotalWeakWords: 0,
                CountByCategory: new Dictionary<WeakWordCategory, int>(),
                Matches: Array.Empty<WeakWordMatch>());
        }

        // LOGIC: Get total word count from tokenization
        var totalWords = WordBoundaryPattern.Matches(text).Count;

        // LOGIC: Get weak word matches
        var matches = Scan(text, profile);

        // LOGIC: Calculate counts by category
        var countByCategory = matches
            .GroupBy(m => m.Category)
            .ToDictionary(g => g.Key, g => g.Count());

        var stats = new WeakWordStats(
            TotalWords: totalWords,
            TotalWeakWords: matches.Count,
            CountByCategory: countByCategory,
            Matches: matches);

        _logger.LogDebug(
            "Statistics: {TotalWords} total, {WeakWords} weak ({Percentage:F1}%) - " +
            "Adverbs: {Adverbs}, Weasel: {Weasel}, Fillers: {Fillers}",
            stats.TotalWords,
            stats.TotalWeakWords,
            stats.WeakWordPercentage,
            countByCategory.GetValueOrDefault(WeakWordCategory.Adverb),
            countByCategory.GetValueOrDefault(WeakWordCategory.WeaselWord),
            countByCategory.GetValueOrDefault(WeakWordCategory.Filler));

        return stats;
    }

    /// <inheritdoc />
    public string GetSuggestion(string word, WeakWordCategory category)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return GetGenericSuggestion(category);
        }

        // LOGIC: Check for specific suggestion first
        if (WeakWordLists.Suggestions.TryGetValue(word, out var suggestion))
        {
            _logger.LogTrace(
                "Found specific suggestion for '{Word}': '{Suggestion}'",
                word,
                suggestion);
            return suggestion;
        }

        // LOGIC: Fall back to generic category suggestion
        return GetGenericSuggestion(category);
    }

    /// <summary>
    /// Categorizes a word based on the word lists and profile constraints.
    /// </summary>
    /// <param name="word">The word to categorize.</param>
    /// <param name="profile">The Voice Profile with constraint settings.</param>
    /// <returns>
    /// The <see cref="WeakWordCategory"/> if the word is a weak word
    /// that should be flagged; null otherwise.
    /// </returns>
    /// <remarks>
    /// LOGIC: Check order is Filler → Adverb → WeaselWord because:
    /// <list type="bullet">
    ///   <item>Fillers are ALWAYS flagged (no profile check needed)</item>
    ///   <item>Some words may appear in multiple lists; filler takes priority</item>
    /// </list>
    /// </remarks>
    private static WeakWordCategory? CategorizeWord(string word, VoiceProfile profile)
    {
        // LOGIC: Fillers are ALWAYS flagged regardless of profile settings
        if (WeakWordLists.Fillers.Contains(word))
        {
            return WeakWordCategory.Filler;
        }

        // LOGIC: Check adverbs only if profile has FlagAdverbs enabled
        if (profile.FlagAdverbs && WeakWordLists.Adverbs.Contains(word))
        {
            return WeakWordCategory.Adverb;
        }

        // LOGIC: Check weasel words only if profile has FlagWeaselWords enabled
        if (profile.FlagWeaselWords && WeakWordLists.WeaselWords.Contains(word))
        {
            return WeakWordCategory.WeaselWord;
        }

        return null;
    }

    /// <summary>
    /// Gets a generic suggestion for a weak word category.
    /// </summary>
    /// <param name="category">The weak word category.</param>
    /// <returns>A generic suggestion string for the category.</returns>
    private static string GetGenericSuggestion(WeakWordCategory category)
    {
        return category switch
        {
            WeakWordCategory.Adverb => "Consider using a stronger verb or more precise adjective",
            WeakWordCategory.WeaselWord => "Be more specific or commit to the statement",
            WeakWordCategory.Filler => "Consider removing—adds no meaning",
            _ => "Review for potential improvement"
        };
    }
}
