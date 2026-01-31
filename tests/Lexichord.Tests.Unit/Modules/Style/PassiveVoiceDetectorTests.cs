// <copyright file="PassiveVoiceDetectorTests.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Style.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for <see cref="PassiveVoiceDetector"/>.
/// Tests cover passive voice detection, adjective disambiguation,
/// confidence scoring, and percentage calculations.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.3.4b")]
public class PassiveVoiceDetectorTests
{
    private readonly Mock<ISentenceTokenizer> _tokenizerMock = new();
    private readonly PassiveVoiceDetector _sut;

    public PassiveVoiceDetectorTests()
    {
        _sut = new PassiveVoiceDetector(
            _tokenizerMock.Object,
            NullLogger<PassiveVoiceDetector>.Instance);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullTokenizer_ThrowsArgumentNullException()
    {
        var act = () => new PassiveVoiceDetector(
            null!,
            NullLogger<PassiveVoiceDetector>.Instance);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("sentenceTokenizer");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new PassiveVoiceDetector(
            _tokenizerMock.Object,
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region ContainsPassiveVoice - Passive Sentences

    [Theory]
    [InlineData("The code was written by the developer.")]
    [InlineData("The report was submitted yesterday.")]
    [InlineData("The book has been read by millions.")]
    [InlineData("The project is being reviewed.")]
    [InlineData("The cake was eaten.")]
    [InlineData("The file will be deleted.")]
    [InlineData("She got fired last week.")]
    public void ContainsPassiveVoice_PassiveSentences_ReturnsTrue(string sentence)
    {
        var result = _sut.ContainsPassiveVoice(sentence);

        result.Should().BeTrue();
    }

    #endregion

    #region ContainsPassiveVoice - Active Sentences

    [Theory]
    [InlineData("The developer wrote the code.")]
    [InlineData("She submitted the report.")]
    [InlineData("Millions have read the book.")]
    [InlineData("The team is reviewing the project.")]
    [InlineData("They ate the cake.")]
    public void ContainsPassiveVoice_ActiveSentences_ReturnsFalse(string sentence)
    {
        var result = _sut.ContainsPassiveVoice(sentence);

        result.Should().BeFalse();
    }

    #endregion

    #region ContainsPassiveVoice - Adjective Disambiguation

    [Theory]
    [InlineData("The door is closed.")]
    [InlineData("She seemed tired.")]
    [InlineData("The window appears broken.")]
    [InlineData("He looks worried.")]
    [InlineData("They remain concerned.")]
    public void ContainsPassiveVoice_Adjectives_ReturnsFalse(string sentence)
    {
        var result = _sut.ContainsPassiveVoice(sentence);

        result.Should().BeFalse();
    }

    #endregion

    #region ContainsPassiveVoice - Edge Cases

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ContainsPassiveVoice_EmptyOrNull_ReturnsFalse(string? sentence)
    {
        var result = _sut.ContainsPassiveVoice(sentence!);

        result.Should().BeFalse();
    }

    #endregion

    #region DetectInSentence - Pattern Types

    [Fact]
    public void DetectInSentence_ToBePassive_ReturnsCorrectType()
    {
        var sentence = "The code was written by the developer.";

        var match = _sut.DetectInSentence(sentence);

        match.Should().NotBeNull();
        match!.Type.Should().Be(PassiveType.ToBe);
        match.PassiveConstruction.Should().Contain("was written");
    }

    [Fact]
    public void DetectInSentence_ModalPassive_ReturnsCorrectType()
    {
        var sentence = "The file will be deleted tomorrow.";

        var match = _sut.DetectInSentence(sentence);

        match.Should().NotBeNull();
        match!.Type.Should().Be(PassiveType.Modal);
        match.PassiveConstruction.Should().Contain("will be deleted");
    }

    [Fact]
    public void DetectInSentence_ProgressivePassive_ReturnsCorrectType()
    {
        var sentence = "The project is being reviewed.";

        var match = _sut.DetectInSentence(sentence);

        match.Should().NotBeNull();
        match!.Type.Should().Be(PassiveType.Progressive);
        match.PassiveConstruction.Should().Contain("is being reviewed");
    }

    [Fact]
    public void DetectInSentence_GetPassive_ReturnsCorrectType()
    {
        var sentence = "She got fired last week.";

        var match = _sut.DetectInSentence(sentence);

        match.Should().NotBeNull();
        match!.Type.Should().Be(PassiveType.Get);
        match.PassiveConstruction.Should().Contain("got fired");
    }

    #endregion

    #region Confidence Scoring - By Agent

    [Fact]
    public void DetectInSentence_WithByAgent_ReturnsHighConfidence()
    {
        var sentence = "The code was written by the developer.";

        var match = _sut.DetectInSentence(sentence);

        match.Should().NotBeNull();
        match!.Confidence.Should().BeGreaterThan(0.9);
    }

    [Fact]
    public void DetectInSentence_WithoutByAgent_ReturnsModerateConfidence()
    {
        var sentence = "The report was submitted yesterday.";

        var match = _sut.DetectInSentence(sentence);

        match.Should().NotBeNull();
        match!.Confidence.Should().BeInRange(0.7, 0.85);
    }

    #endregion

    #region Confidence Scoring - Progressive Boost

    [Fact]
    public void DetectInSentence_ProgressivePassive_ReturnsHighConfidence()
    {
        var sentence = "The code is being reviewed by the team.";

        var match = _sut.DetectInSentence(sentence);

        match.Should().NotBeNull();
        match!.Confidence.Should().BeGreaterThan(0.85);
    }

    #endregion

    #region Confidence Scoring - Adjective Penalty

    [Fact]
    public void DetectInSentence_CommonAdjective_ReturnsLowConfidence()
    {
        var sentence = "The door is closed.";

        var match = _sut.DetectInSentence(sentence);

        // Should match but with low confidence due to adjective penalty
        if (match is not null)
        {
            match.Confidence.Should().BeLessThan(0.5);
            match.IsPassiveVoice.Should().BeFalse();
        }
    }

    #endregion

    #region Confidence Scoring - State Context Verb Penalty

    [Fact]
    public void DetectInSentence_StateContextVerb_ReturnsLowConfidence()
    {
        var sentence = "She seemed tired from the long journey.";

        var match = _sut.DetectInSentence(sentence);

        // Should return null or very low confidence
        if (match is not null)
        {
            match.Confidence.Should().BeLessThan(0.5);
        }
    }

    #endregion

    #region Detect - Multiple Sentences

    [Fact]
    public void Detect_MultipleSentences_ReturnsAllPassiveMatches()
    {
        var text = "The code was written by the developer. She reviewed it carefully. The tests were executed.";

        SetupTokenizer(text,
            "The code was written by the developer.",
            "She reviewed it carefully.",
            "The tests were executed.");

        var matches = _sut.Detect(text);

        matches.Should().HaveCount(2);
        matches.Should().Contain(m => m.PassiveConstruction.Contains("was written"));
        matches.Should().Contain(m => m.PassiveConstruction.Contains("were executed"));
    }

    [Fact]
    public void Detect_NoPassiveSentences_ReturnsEmptyList()
    {
        var text = "The developer wrote the code. She reviewed it.";

        SetupTokenizer(text,
            "The developer wrote the code.",
            "She reviewed it.");

        var matches = _sut.Detect(text);

        matches.Should().BeEmpty();
    }

    [Fact]
    public void Detect_EmptyText_ReturnsEmptyList()
    {
        var matches = _sut.Detect(string.Empty);

        matches.Should().BeEmpty();
    }

    #endregion

    #region GetPassiveVoicePercentage

    [Fact]
    public void GetPassiveVoicePercentage_AllPassive_Returns100()
    {
        var text = "The code was written. The tests were executed. The docs were updated.";

        SetupTokenizer(text,
            "The code was written.",
            "The tests were executed.",
            "The docs were updated.");

        var percentage = _sut.GetPassiveVoicePercentage(text, out var passiveCount, out var totalSentences);

        percentage.Should().Be(100.0);
        passiveCount.Should().Be(3);
        totalSentences.Should().Be(3);
    }

    [Fact]
    public void GetPassiveVoicePercentage_NoPassive_Returns0()
    {
        var text = "The developer wrote the code. She tested it. He documented it.";

        SetupTokenizer(text,
            "The developer wrote the code.",
            "She tested it.",
            "He documented it.");

        var percentage = _sut.GetPassiveVoicePercentage(text, out var passiveCount, out var totalSentences);

        percentage.Should().Be(0.0);
        passiveCount.Should().Be(0);
        totalSentences.Should().Be(3);
    }

    [Fact]
    public void GetPassiveVoicePercentage_MixedSentences_ReturnsCorrectPercentage()
    {
        var text = "The code was written by the developer. She tested it carefully. The docs were updated.";

        SetupTokenizer(text,
            "The code was written by the developer.",
            "She tested it carefully.",
            "The docs were updated.");

        var percentage = _sut.GetPassiveVoicePercentage(text, out var passiveCount, out var totalSentences);

        percentage.Should().BeApproximately(66.67, 0.1);
        passiveCount.Should().Be(2);
        totalSentences.Should().Be(3);
    }

    [Fact]
    public void GetPassiveVoicePercentage_EmptyText_ReturnsZero()
    {
        var percentage = _sut.GetPassiveVoicePercentage(string.Empty, out var passiveCount, out var totalSentences);

        percentage.Should().Be(0.0);
        passiveCount.Should().Be(0);
        totalSentences.Should().Be(0);
    }

    #endregion

    #region PassiveVoiceMatch Record

    [Fact]
    public void PassiveVoiceMatch_IsPassiveVoice_TrueWhenConfidenceAboveThreshold()
    {
        var match = new PassiveVoiceMatch(
            Sentence: "Test sentence.",
            PassiveConstruction: "was written",
            Type: PassiveType.ToBe,
            Confidence: 0.75,
            StartIndex: 0,
            EndIndex: 11);

        match.IsPassiveVoice.Should().BeTrue();
    }

    [Fact]
    public void PassiveVoiceMatch_IsPassiveVoice_FalseWhenConfidenceBelowThreshold()
    {
        var match = new PassiveVoiceMatch(
            Sentence: "Test sentence.",
            PassiveConstruction: "is closed",
            Type: PassiveType.ToBe,
            Confidence: 0.45,
            StartIndex: 0,
            EndIndex: 9);

        match.IsPassiveVoice.Should().BeFalse();
    }

    [Fact]
    public void PassiveVoiceMatch_Length_ReturnsCorrectValue()
    {
        var match = new PassiveVoiceMatch(
            Sentence: "Test sentence.",
            PassiveConstruction: "was written",
            Type: PassiveType.ToBe,
            Confidence: 0.75,
            StartIndex: 5,
            EndIndex: 16);

        match.Length.Should().Be(11);
    }

    #endregion

    #region Helper Methods

    private void SetupTokenizer(string fullText, params string[] sentences)
    {
        var currentIndex = 0;
        var tokenResults = new List<SentenceInfo>();
        
        foreach (var s in sentences)
        {
            var startIndex = fullText.IndexOf(s, currentIndex, StringComparison.Ordinal);
            if (startIndex < 0) startIndex = currentIndex;
            var endIndex = startIndex + s.Length;
            var wordCount = s.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            tokenResults.Add(new SentenceInfo(s, startIndex, endIndex, wordCount));
            currentIndex = endIndex;
        }
        
        _tokenizerMock
            .Setup(t => t.Tokenize(fullText))
            .Returns(tokenResults.AsReadOnly());
    }

    #endregion
}
