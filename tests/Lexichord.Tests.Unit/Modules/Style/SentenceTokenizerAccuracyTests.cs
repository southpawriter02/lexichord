// -----------------------------------------------------------------------
// <copyright file="SentenceTokenizerAccuracyTests.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Modules.Style.Services;
using Lexichord.Tests.Unit.Modules.Style.Fixtures;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Accuracy tests for SentenceTokenizer against corpus entries.
/// </summary>
/// <remarks>
/// <para>
/// LOGIC: v0.3.8b - Validates sentence tokenization accuracy against known
/// reference values. Accurate sentence counts directly impact Flesch-Kincaid
/// and Gunning Fog calculations.
/// </para>
/// <para>
/// <b>Test Categories:</b>
/// <list type="bullet">
///   <item>Corpus Sentence Counts - Exact match validation</item>
///   <item>Word Distribution - Words-per-sentence accuracy</item>
///   <item>Complex Punctuation - Abbreviations, ellipses, quotes</item>
/// </list>
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.3.8b")]
[Trait("Component", "SentenceTokenizer")]
public class SentenceTokenizerAccuracyTests
{
    private readonly SentenceTokenizer _sut;
    private readonly ReadabilityTestCorpus _corpus;

    public SentenceTokenizerAccuracyTests()
    {
        var loggerMock = new Mock<ILogger<SentenceTokenizer>>();
        _sut = new SentenceTokenizer(loggerMock.Object);
        _corpus = new ReadabilityTestCorpus();
    }

    #region Corpus Sentence Count Accuracy

    /// <summary>
    /// Validates sentence count matches expected for each corpus entry.
    /// </summary>
    /// <remarks>
    /// LOGIC: v0.3.8b - Sentence count must be exact for well-defined corpus entries.
    /// </remarks>
    [Theory]
    [InlineData("gettysburg", 1)]
    [InlineData("simple", 4)]
    [InlineData("academic", 1)]
    [InlineData("hemingway", 5)]
    [InlineData("mixed", 5)]
    public void Tokenize_CorpusEntry_MatchesExpectedSentenceCount(string entryId, int expectedSentences)
    {
        // Arrange
        var entry = _corpus.Get(entryId);

        // Act
        var sentences = _sut.Tokenize(entry.Text);

        // Assert
        sentences.Should().HaveCount(expectedSentences,
            $"'{entry.Name}' should have exactly {expectedSentences} sentence(s)");
    }

    #endregion

    #region Word Count Per Corpus Entry

    /// <summary>
    /// Validates total word count aggregated from sentences matches expected.
    /// </summary>
    /// <remarks>
    /// LOGIC: v0.3.8b - Total words from all sentences should match corpus word count.
    /// </remarks>
    [Theory]
    [InlineData("gettysburg")]
    [InlineData("simple")]
    [InlineData("academic")]
    [InlineData("hemingway")]
    [InlineData("mixed")]
    public void Tokenize_CorpusEntry_WordCountMatchesExpected(string entryId)
    {
        // Arrange
        var entry = _corpus.Get(entryId);

        // Act
        var sentences = _sut.Tokenize(entry.Text);
        var totalWords = sentences.Sum(s => s.WordCount);

        // Assert
        totalWords.Should().Be(entry.Expected.WordCount,
            $"'{entry.Name}' should have {entry.Expected.WordCount} total words");
    }

    #endregion

    #region Average Words Per Sentence

    /// <summary>
    /// Validates average words-per-sentence calculation accuracy.
    /// </summary>
    /// <remarks>
    /// LOGIC: v0.3.8b - Average words per sentence is a key input to
    /// Flesch-Kincaid and Gunning Fog formulas. Must be accurate.
    /// </remarks>
    [Theory]
    [InlineData("gettysburg", 30.0)]  // 30 words / 1 sentence
    [InlineData("simple", 5.0)]       // 20 words / 4 sentences
    [InlineData("academic", 19.0)]    // 19 words / 1 sentence
    [InlineData("hemingway", 5.0)]    // 25 words / 5 sentences
    [InlineData("mixed", 5.0)]        // 25 words / 5 sentences
    public void Tokenize_CorpusEntry_AverageWordsPerSentenceMatchesExpected(
        string entryId, double expectedAverage)
    {
        // Arrange
        var entry = _corpus.Get(entryId);

        // Act
        var sentences = _sut.Tokenize(entry.Text);
        var totalWords = sentences.Sum(s => s.WordCount);
        var avgWordsPerSentence = (double)totalWords / sentences.Count;

        // Assert
        avgWordsPerSentence.Should().BeApproximately(expectedAverage, 0.5,
            $"'{entry.Name}' should have ~{expectedAverage} words per sentence");
    }

    #endregion

    #region Reference Text Validation

    /// <summary>
    /// Validates tokenization of the well-known Gettysburg Address opening.
    /// </summary>
    /// <remarks>
    /// LOGIC: v0.3.8b - The Gettysburg Address is a canonical reference text
    /// for readability research. It should tokenize as exactly one sentence.
    /// </remarks>
    [Fact]
    public void Tokenize_GettysburgOpening_SingleSentence()
    {
        // Arrange
        var entry = _corpus.Get("gettysburg");

        // Act
        var sentences = _sut.Tokenize(entry.Text);

        // Assert
        sentences.Should().ContainSingle("Gettysburg Address opening is one complex sentence");
        sentences[0].WordCount.Should().Be(30, "Contains exactly 30 words");
    }

    /// <summary>
    /// Validates tokenization of Hemingway-style short sentences.
    /// </summary>
    /// <remarks>
    /// LOGIC: v0.3.8b - Hemingway's style features short, punchy sentences.
    /// The tokenizer must correctly identify all five sentences.
    /// </remarks>
    [Fact]
    public void Tokenize_HemingwayStyle_FiveShortSentences()
    {
        // Arrange
        var entry = _corpus.Get("hemingway");

        // Act
        var sentences = _sut.Tokenize(entry.Text);

        // Assert
        sentences.Should().HaveCount(5, "Hemingway-style text has 5 short sentences");
        sentences.All(s => s.WordCount <= 8).Should().BeTrue(
            "all sentences should be short (Hemingway style)");
    }

    #endregion

    #region Sentence Position Tracking

    /// <summary>
    /// Validates that sentence start/end positions are tracked correctly.
    /// </summary>
    [Theory]
    [InlineData("simple")]
    [InlineData("hemingway")]
    [InlineData("mixed")]
    public void Tokenize_CorpusEntry_SentencePositionsAreValid(string entryId)
    {
        // Arrange
        var entry = _corpus.Get(entryId);

        // Act
        var sentences = _sut.Tokenize(entry.Text);

        // Assert
        foreach (var sentence in sentences)
        {
            sentence.StartIndex.Should().BeGreaterThanOrEqualTo(0);
            sentence.EndIndex.Should().BeGreaterThan(sentence.StartIndex);
            sentence.EndIndex.Should().BeLessThanOrEqualTo(entry.Text.Length);
        }
    }

    #endregion
}
