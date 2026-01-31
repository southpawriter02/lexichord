// -----------------------------------------------------------------------
// <copyright file="SyllableCounterAccuracyTests.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Modules.Style.Services;
using Lexichord.Tests.Unit.Modules.Style.Fixtures;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Accuracy tests for SyllableCounter against reference words and corpus entries.
/// </summary>
/// <remarks>
/// <para>
/// LOGIC: v0.3.8b - Validates the syllable counting algorithm against known
/// reference values. Accurate syllable counts are critical for all three
/// readability formulas (Flesch-Kincaid, Gunning Fog, Flesch Reading Ease).
/// </para>
/// <para>
/// <b>Test Categories:</b>
/// <list type="bullet">
///   <item>CMU Reference Words - Dictionary-verified syllable counts</item>
///   <item>Corpus Syllable Totals - Aggregate validation per corpus entry</item>
///   <item>Edge Cases - Unusual words, numbers, abbreviations</item>
/// </list>
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.3.8b")]
[Trait("Component", "SyllableCounter")]
public class SyllableCounterAccuracyTests
{
    private readonly SyllableCounter _sut = new();
    private readonly ReadabilityTestCorpus _corpus = new();

    #region CMU Dictionary Reference Words

    /// <summary>
    /// Tests syllable counting against CMU Pronouncing Dictionary reference words.
    /// </summary>
    /// <remarks>
    /// LOGIC: v0.3.8b - These words have verified syllable counts from the
    /// CMU Pronouncing Dictionary, a widely-used phonetic dictionary.
    /// </remarks>
    [Theory]
    [InlineData("a", 1)]
    [InlineData("the", 1)]
    [InlineData("about", 2)]
    [InlineData("example", 3)]
    [InlineData("beautiful", 3)]
    [InlineData("particularly", 5)]
    [InlineData("extraordinary", 5)]
    [InlineData("responsibility", 6)]
    [InlineData("internationalization", 8)]
    public void CountSyllables_CMUReferenceWords_MatchesExpected(string word, int expectedSyllables)
    {
        // Act
        var result = _sut.CountSyllables(word);

        // Assert
        result.Should().Be(expectedSyllables,
            $"'{word}' should have {expectedSyllables} syllables per CMU dictionary");
    }

    #endregion

    #region Common English Words Reference

    /// <summary>
    /// Tests common English words with known syllable counts.
    /// </summary>
    /// <remarks>
    /// LOGIC: v0.3.8b - Validates behavior for frequently-used words that
    /// significantly impact readability calculations.
    /// </remarks>
    [Theory]
    [InlineData("implementation", 5)]
    [InlineData("documentation", 5)]
    [InlineData("understanding", 4)]
    [InlineData("development", 4)]
    [InlineData("information", 4)]
    [InlineData("application", 4)]
    [InlineData("technology", 4)]
    [InlineData("professional", 4)]
    [InlineData("organization", 5)]
    [InlineData("communication", 5)]
    public void CountSyllables_CommonPolysyllabicWords_MatchesExpected(string word, int expectedSyllables)
    {
        // Act
        var result = _sut.CountSyllables(word);

        // Assert
        result.Should().Be(expectedSyllables,
            $"'{word}' should have {expectedSyllables} syllables");
    }

    #endregion

    #region Corpus Entry Syllable Totals

    /// <summary>
    /// Validates aggregate syllable count for each corpus entry.
    /// </summary>
    /// <remarks>
    /// LOGIC: v0.3.8b - Corpus-level validation ensures the algorithm performs
    /// correctly on realistic text samples. A ±10% tolerance accounts for
    /// edge cases in different syllable counting approaches.
    /// </remarks>
    [Theory]
    [InlineData("gettysburg")]
    [InlineData("simple")]
    [InlineData("academic")]
    [InlineData("hemingway")]
    [InlineData("mixed")]
    public void CountSyllables_CorpusEntry_WithinToleranceRange(string entryId)
    {
        // Arrange
        var entry = _corpus.Get(entryId);
        var words = ExtractWords(entry.Text);
        var expectedSyllables = entry.Expected.SyllableCount;

        // Act
        var actualSyllables = words.Sum(w => _sut.CountSyllables(w));

        // Assert
        // LOGIC: Allow ±10% tolerance for algorithmic variations
        var tolerance = Math.Max(3, (int)(expectedSyllables * 0.10));
        actualSyllables.Should().BeInRange(
            expectedSyllables - tolerance,
            expectedSyllables + tolerance,
            $"Corpus '{entry.Name}' syllable count should be approximately {expectedSyllables}");
    }

    #endregion

    #region Silent E Handling Accuracy

    /// <summary>
    /// Tests accurate handling of silent 'e' patterns.
    /// </summary>
    /// <remarks>
    /// LOGIC: v0.3.8b - Silent 'e' is a common source of syllable miscounts.
    /// </remarks>
    [Theory]
    [InlineData("come", 1)]     // Silent e
    [InlineData("love", 1)]     // Silent e
    [InlineData("above", 2)]    // a-bove, silent e
    [InlineData("create", 2)]   // cre-ate, not silent
    [InlineData("separate", 3)] // sep-a-rate
    [InlineData("climate", 2)]  // cli-mate
    public void CountSyllables_SilentEPatterns_MatchesExpected(string word, int expectedSyllables)
    {
        // Act
        var result = _sut.CountSyllables(word);

        // Assert
        result.Should().Be(expectedSyllables,
            $"'{word}' should correctly handle silent 'e' pattern");
    }

    #endregion

    #region Compound Word Handling

    /// <summary>
    /// Tests syllable counting in compound and hyphenated words.
    /// </summary>
    [Theory]
    [InlineData("self-aware", 3)]
    [InlineData("well-known", 2)]
    [InlineData("state-of-the-art", 5)]  // Algorithm counts each word separately
    public void CountSyllables_CompoundWords_SumsComponents(string word, int expectedSyllables)
    {
        // Act
        var result = _sut.CountSyllables(word);

        // Assert
        result.Should().Be(expectedSyllables,
            $"compound word '{word}' should sum component syllables");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Extracts words from text for syllable counting.
    /// </summary>
    private static List<string> ExtractWords(string text)
    {
        return System.Text.RegularExpressions.Regex.Matches(text, @"\b[\w-]+\b")
            .Select(m => m.Value)
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .ToList();
    }

    #endregion
}
