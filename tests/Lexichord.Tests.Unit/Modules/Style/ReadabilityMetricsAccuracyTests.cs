// -----------------------------------------------------------------------
// <copyright file="ReadabilityMetricsAccuracyTests.cs" company="Lexichord">
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
/// Accuracy tests for ReadabilityService formula calculations.
/// </summary>
/// <remarks>
/// <para>
/// LOGIC: v0.3.8b - Validates the mathematical accuracy of readability formulas
/// against known reference values. These tests ensure the core algorithms produce
/// results consistent with established readability calculators.
/// </para>
/// <para>
/// <b>Formulas Tested:</b>
/// <list type="bullet">
///   <item>Flesch-Kincaid Grade Level (±1.0 tolerance)</item>
///   <item>Gunning Fog Index (±1.0 tolerance)</item>
///   <item>Flesch Reading Ease (±3.0 tolerance)</item>
/// </list>
/// </para>
/// <para>
/// <b>Tolerance Rationale:</b> Readability formulas are sensitive to syllable
/// counting variations. A ±1 grade tolerance is standard in the field.
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.3.8b")]
[Trait("Component", "ReadabilityService")]
public class ReadabilityMetricsAccuracyTests
{
    private readonly ReadabilityService _sut;
    private readonly ReadabilityTestCorpus _corpus;

    public ReadabilityMetricsAccuracyTests()
    {
        var sentenceLogger = new Mock<ILogger<SentenceTokenizer>>();
        var serviceLogger = new Mock<ILogger<ReadabilityService>>();

        var sentenceTokenizer = new SentenceTokenizer(sentenceLogger.Object);
        var syllableCounter = new SyllableCounter();

        _sut = new ReadabilityService(sentenceTokenizer, syllableCounter, serviceLogger.Object);
        _corpus = new ReadabilityTestCorpus();
    }

    #region Flesch-Kincaid Grade Level Accuracy

    /// <summary>
    /// Validates Flesch-Kincaid Grade Level accuracy for all corpus entries.
    /// </summary>
    /// <remarks>
    /// LOGIC: v0.3.8b - FK Grade Level: 0.39 × (words/sentences) + 11.8 × (syllables/words) − 15.59
    /// Tolerance uses entry-specific range to account for algorithmic variations.
    /// </remarks>
    [Theory]
    [InlineData("gettysburg")]
    [InlineData("simple")]
    [InlineData("academic")]
    [InlineData("hemingway")]
    [InlineData("mixed")]
    public void Analyze_CorpusEntry_FleschKincaidWithinTolerance(string entryId)
    {
        // Arrange
        var entry = _corpus.Get(entryId);
        var expected = entry.Expected.FleschKincaidGrade;
        var tolerance = entry.Expected.GradeToleranceRange;

        // Act
        var result = _sut.Analyze(entry.Text);

        // Assert
        result.FleschKincaidGradeLevel.Should().BeInRange(
            expected - tolerance,
            expected + tolerance,
            $"'{entry.Name}' FK should be ~{expected} (±{tolerance})");
    }

    /// <summary>
    /// Validates that simple text produces low FK grade.
    /// </summary>
    [Fact]
    public void Analyze_SimpleChildrensText_LowFleschKincaid()
    {
        // Arrange
        var entry = _corpus.Get("simple");

        // Act
        var result = _sut.Analyze(entry.Text);

        // Assert
        result.FleschKincaidGradeLevel.Should().BeLessThan(3.0,
            "simple children's text should be early elementary level");
    }

    /// <summary>
    /// Validates that academic text produces high FK grade.
    /// </summary>
    [Fact]
    public void Analyze_AcademicText_HighFleschKincaid()
    {
        // Arrange
        var entry = _corpus.Get("academic");

        // Act
        var result = _sut.Analyze(entry.Text);

        // Assert
        result.FleschKincaidGradeLevel.Should().BeGreaterThan(15.0,
            "academic text should require post-secondary education");
    }

    #endregion

    #region Gunning Fog Index Accuracy

    /// <summary>
    /// Validates Gunning Fog Index accuracy for all corpus entries.
    /// </summary>
    /// <remarks>
    /// LOGIC: v0.3.8b - Fog Index: 0.4 × (words/sentences + 100 × complexWordRatio)
    /// </remarks>
    [Theory]
    [InlineData("gettysburg")]
    [InlineData("simple")]
    [InlineData("academic")]
    [InlineData("hemingway")]
    [InlineData("mixed")]
    public void Analyze_CorpusEntry_GunningFogWithinTolerance(string entryId)
    {
        // Arrange
        var entry = _corpus.Get(entryId);
        var expected = entry.Expected.GunningFogIndex;
        var tolerance = entry.Expected.FogToleranceRange;

        // Act
        var result = _sut.Analyze(entry.Text);

        // Assert
        result.GunningFogIndex.Should().BeInRange(
            expected - tolerance,
            expected + tolerance,
            $"'{entry.Name}' Fog should be ~{expected} (±{tolerance})");
    }

    /// <summary>
    /// Validates that Hemingway-style text has low Fog index.
    /// </summary>
    [Fact]
    public void Analyze_HemingwayStyle_LowGunningFog()
    {
        // Arrange
        var entry = _corpus.Get("hemingway");

        // Act
        var result = _sut.Analyze(entry.Text);

        // Assert
        result.GunningFogIndex.Should().BeLessThan(6.0,
            "Hemingway-style prose should be very readable");
    }

    /// <summary>
    /// Validates that academic text has high Fog index.
    /// </summary>
    [Fact]
    public void Analyze_AcademicText_HighGunningFog()
    {
        // Arrange
        var entry = _corpus.Get("academic");

        // Act
        var result = _sut.Analyze(entry.Text);

        // Assert
        result.GunningFogIndex.Should().BeGreaterThan(18.0,
            "academic text should require graduate-level education");
    }

    #endregion

    #region Flesch Reading Ease Accuracy

    /// <summary>
    /// Validates Flesch Reading Ease accuracy for all corpus entries.
    /// </summary>
    /// <remarks>
    /// LOGIC: v0.3.8b - FRE: 206.835 − 1.015 × (words/sentences) − 84.6 × (syllables/words)
    /// Higher scores = easier to read. Clamped to 0-100 range.
    /// </remarks>
    [Theory]
    [InlineData("gettysburg")]
    [InlineData("simple")]
    [InlineData("academic")]
    [InlineData("hemingway")]
    [InlineData("mixed")]
    public void Analyze_CorpusEntry_FleschReadingEaseWithinTolerance(string entryId)
    {
        // Arrange
        var entry = _corpus.Get(entryId);
        var expected = entry.Expected.FleschReadingEase;
        var tolerance = entry.Expected.EaseToleranceRange;

        // Act
        var result = _sut.Analyze(entry.Text);

        // Assert
        result.FleschReadingEase.Should().BeInRange(
            Math.Max(0, expected - tolerance),
            Math.Min(100, expected + tolerance),
            $"'{entry.Name}' FRE should be ~{expected} (±{tolerance})");
    }

    /// <summary>
    /// Validates that simple text has high reading ease (easy to read).
    /// </summary>
    [Fact]
    public void Analyze_SimpleText_HighFleschReadingEase()
    {
        // Arrange
        var entry = _corpus.Get("simple");

        // Act
        var result = _sut.Analyze(entry.Text);

        // Assert
        result.FleschReadingEase.Should().BeGreaterThanOrEqualTo(90.0,
            "simple text should be 'Very Easy' to read");
        result.ReadingEaseInterpretation.Should().Be("Very Easy");
    }

    /// <summary>
    /// Validates that academic text has low reading ease (hard to read).
    /// </summary>
    [Fact]
    public void Analyze_AcademicText_LowFleschReadingEase()
    {
        // Arrange
        var entry = _corpus.Get("academic");

        // Act
        var result = _sut.Analyze(entry.Text);

        // Assert
        result.FleschReadingEase.Should().BeLessThanOrEqualTo(30.0,
            "academic text should be 'Very Difficult' to read");
    }

    #endregion

    #region Corpus Entry Word and Sentence Count Validation

    /// <summary>
    /// Validates that ReadabilityService correctly counts words.
    /// </summary>
    [Theory]
    [InlineData("gettysburg", 30)]
    [InlineData("simple", 20)]
    [InlineData("academic", 19)]
    [InlineData("hemingway", 25)]
    [InlineData("mixed", 25)]
    public void Analyze_CorpusEntry_WordCountMatchesExpected(string entryId, int expectedWords)
    {
        // Arrange
        var entry = _corpus.Get(entryId);

        // Act
        var result = _sut.Analyze(entry.Text);

        // Assert
        result.WordCount.Should().Be(expectedWords,
            $"'{entry.Name}' should have {expectedWords} words");
    }

    /// <summary>
    /// Validates that ReadabilityService correctly counts sentences.
    /// </summary>
    [Theory]
    [InlineData("gettysburg", 1)]
    [InlineData("simple", 4)]
    [InlineData("academic", 1)]
    [InlineData("hemingway", 5)]
    [InlineData("mixed", 5)]
    public void Analyze_CorpusEntry_SentenceCountMatchesExpected(string entryId, int expectedSentences)
    {
        // Arrange
        var entry = _corpus.Get(entryId);

        // Act
        var result = _sut.Analyze(entry.Text);

        // Assert
        result.SentenceCount.Should().Be(expectedSentences,
            $"'{entry.Name}' should have {expectedSentences} sentences");
    }

    #endregion

    #region Formula Consistency Tests

    /// <summary>
    /// Validates an inverse relationship exists between FK and FRE.
    /// </summary>
    /// <remarks>
    /// LOGIC: Higher FK grade = harder text = lower FRE score.
    /// </remarks>
    [Fact]
    public void Analyze_AllCorpusEntries_FKandFREInverseRelationship()
    {
        // Act
        var results = _corpus.Entries
            .Select(e => (Entry: e, Metrics: _sut.Analyze(e.Text)))
            .OrderBy(r => r.Metrics.FleschKincaidGradeLevel)
            .ToList();

        // Assert
        // As FK goes up, FRE should generally go down
        var freScores = results.Select(r => r.Metrics.FleschReadingEase).ToList();
        var isGenerallyDecreasing = freScores.Zip(freScores.Skip(1),
            (a, b) => a >= b - 10) // Allow 10-point tolerance for minor inversions
            .Count(x => x);

        isGenerallyDecreasing.Should().BeGreaterThanOrEqualTo(3,
            "FRE should generally decrease as FK increases");
    }

    /// <summary>
    /// Validates that complex word count affects Fog index.
    /// </summary>
    [Fact]
    public void Analyze_TextWithManyComplexWords_HighGunningFog()
    {
        // Arrange
        var academic = _sut.Analyze(_corpus.Get("academic").Text);
        var simple = _sut.Analyze(_corpus.Get("simple").Text);

        // Assert
        academic.ComplexWordCount.Should().BeGreaterThan(simple.ComplexWordCount,
            "academic text should have more complex words");
        academic.GunningFogIndex.Should().BeGreaterThan(simple.GunningFogIndex,
            "more complex words = higher Fog index");
    }

    #endregion
}
