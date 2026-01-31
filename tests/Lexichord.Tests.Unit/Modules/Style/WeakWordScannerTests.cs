// <copyright file="WeakWordScannerTests.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Style.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for <see cref="WeakWordScanner"/>.
/// Tests cover weak word detection, profile constraints, statistics,
/// and suggestion retrieval.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.3.4c")]
public class WeakWordScannerTests
{
    private readonly WeakWordScanner _sut;
    private readonly VoiceProfile _defaultProfile;

    public WeakWordScannerTests()
    {
        _sut = new WeakWordScanner(NullLogger<WeakWordScanner>.Instance);
        _defaultProfile = new VoiceProfile
        {
            Id = Guid.NewGuid(),
            Name = "Test Profile",
            FlagAdverbs = true,
            FlagWeaselWords = true
        };
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new WeakWordScanner(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region Scan - Adverb Detection

    [Theory]
    [InlineData("This is very important.", "very")]
    [InlineData("She really enjoyed the show.", "really")]
    [InlineData("The code is extremely complex.", "extremely")]
    [InlineData("It was absolutely perfect.", "absolutely")]
    [InlineData("He completely forgot.", "completely")]
    public void Scan_AdverbPresent_ReturnsAdverbMatch(string text, string expectedWord)
    {
        var matches = _sut.Scan(text, _defaultProfile);

        matches.Should().ContainSingle(m =>
            m.Word == expectedWord.ToLowerInvariant() &&
            m.Category == WeakWordCategory.Adverb);
    }

    [Theory]
    [InlineData("very", WeakWordCategory.Adverb)]
    [InlineData("really", WeakWordCategory.Adverb)]
    [InlineData("quickly", WeakWordCategory.Adverb)]
    [InlineData("obviously", WeakWordCategory.Adverb)]
    public void Scan_CommonAdverbs_DetectsCorrectly(string adverb, WeakWordCategory expectedCategory)
    {
        var text = $"The task was {adverb} completed.";

        var matches = _sut.Scan(text, _defaultProfile);

        matches.Should().Contain(m =>
            m.Word == adverb.ToLowerInvariant() &&
            m.Category == expectedCategory);
    }

    #endregion

    #region Scan - Weasel Word Detection

    [Theory]
    [InlineData("Perhaps we should reconsider.", "perhaps")]
    [InlineData("Maybe it will work.", "maybe")]
    [InlineData("This is probably correct.", "probably")]
    [InlineData("Many users prefer this.", "many")]
    [InlineData("It seems to work.", "seems")]
    public void Scan_WeaselWordPresent_ReturnsWeaselWordMatch(string text, string expectedWord)
    {
        var matches = _sut.Scan(text, _defaultProfile);

        matches.Should().ContainSingle(m =>
            m.Word == expectedWord.ToLowerInvariant() &&
            m.Category == WeakWordCategory.WeaselWord);
    }

    #endregion

    #region Scan - Filler Detection

    [Theory]
    [InlineData("Basically, this is how it works.", "basically")]
    [InlineData("It's essentially the same thing.", "essentially")]
    [InlineData("I literally can't believe it.", "literally")]
    [InlineData("It's practically done.", "practically")]
    public void Scan_FillerPresent_ReturnsFillerMatch(string text, string expectedWord)
    {
        var matches = _sut.Scan(text, _defaultProfile);

        matches.Should().ContainSingle(m =>
            m.Word == expectedWord.ToLowerInvariant() &&
            m.Category == WeakWordCategory.Filler);
    }

    [Fact]
    public void Scan_FillerWord_AlwaysFlaggedRegardlessOfProfileSettings()
    {
        var profileWithNoFlags = new VoiceProfile
        {
            Id = Guid.NewGuid(),
            Name = "No Flags",
            FlagAdverbs = false,
            FlagWeaselWords = false
        };

        var text = "Basically, this is the essence of it all.";

        var matches = _sut.Scan(text, profileWithNoFlags);

        // Fillers should still be detected even with FlagAdverbs/FlagWeaselWords = false
        matches.Should().Contain(m =>
            m.Word == "basically" &&
            m.Category == WeakWordCategory.Filler);
    }

    #endregion

    #region Scan - Profile Constraints

    [Fact]
    public void Scan_FlagAdverbsFalse_DoesNotDetectAdverbs()
    {
        var profile = new VoiceProfile
        {
            Id = Guid.NewGuid(),
            Name = "No Adverbs",
            FlagAdverbs = false,
            FlagWeaselWords = true
        };

        var text = "This is very important and perhaps we should reconsider.";

        var matches = _sut.Scan(text, profile);

        matches.Should().NotContain(m => m.Category == WeakWordCategory.Adverb);
        matches.Should().Contain(m =>
            m.Word == "perhaps" &&
            m.Category == WeakWordCategory.WeaselWord);
    }

    [Fact]
    public void Scan_FlagWeaselWordsFalse_DoesNotDetectWeaselWords()
    {
        var profile = new VoiceProfile
        {
            Id = Guid.NewGuid(),
            Name = "No Weasel",
            FlagAdverbs = true,
            FlagWeaselWords = false
        };

        var text = "This is very important and perhaps we should reconsider.";

        var matches = _sut.Scan(text, profile);

        matches.Should().Contain(m =>
            m.Word == "very" &&
            m.Category == WeakWordCategory.Adverb);
        matches.Should().NotContain(m => m.Category == WeakWordCategory.WeaselWord);
    }

    [Fact]
    public void Scan_BothFlagsFalse_OnlyDetectsFillers()
    {
        var profile = new VoiceProfile
        {
            Id = Guid.NewGuid(),
            Name = "Fillers Only",
            FlagAdverbs = false,
            FlagWeaselWords = false
        };

        var text = "This is very important, basically a perhaps statement.";

        var matches = _sut.Scan(text, profile);

        matches.Should().ContainSingle();
        matches[0].Word.Should().Be("basically");
        matches[0].Category.Should().Be(WeakWordCategory.Filler);
    }

    #endregion

    #region Scan - Edge Cases

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Scan_EmptyOrNullText_ReturnsEmptyList(string? text)
    {
        var matches = _sut.Scan(text!, _defaultProfile);

        matches.Should().BeEmpty();
    }

    [Fact]
    public void Scan_NullProfile_ThrowsArgumentNullException()
    {
        var act = () => _sut.Scan("Some text", null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("profile");
    }

    [Fact]
    public void Scan_NoWeakWords_ReturnsEmptyList()
    {
        var text = "The code compiles and the tests pass.";

        var matches = _sut.Scan(text, _defaultProfile);

        matches.Should().BeEmpty();
    }

    [Fact]
    public void Scan_MultipleWeakWords_ReturnsAllMatchesSortedByPosition()
    {
        var text = "This is very really extremely important.";

        var matches = _sut.Scan(text, _defaultProfile);

        matches.Should().HaveCount(3);
        matches.Should().BeInAscendingOrder(m => m.StartIndex);
    }

    [Fact]
    public void Scan_CaseInsensitive_DetectsUppercaseWords()
    {
        // LOGIC: Avoid words like 'should' which would also be flagged
        var text = "VERY important MAYBE they BASICALLY do it.";

        var matches = _sut.Scan(text, _defaultProfile);

        matches.Should().HaveCount(3);
        matches.Should().Contain(m => m.Word == "very");
        matches.Should().Contain(m => m.Word == "maybe");
        matches.Should().Contain(m => m.Word == "basically");
    }

    #endregion

    #region Scan - Position Tracking

    [Fact]
    public void Scan_ReturnsCorrectPositions()
    {
        var text = "The very end.";
        //          0123456789...

        var matches = _sut.Scan(text, _defaultProfile);

        matches.Should().ContainSingle();
        var match = matches[0];
        match.StartIndex.Should().Be(4); // "very" starts at index 4
        match.EndIndex.Should().Be(8);   // "very" ends at index 8
        match.Length.Should().Be(4);     // "very" has 4 characters
    }

    #endregion

    #region GetStatistics

    [Fact]
    public void GetStatistics_EmptyText_ReturnsZeroStats()
    {
        var stats = _sut.GetStatistics(string.Empty, _defaultProfile);

        stats.TotalWords.Should().Be(0);
        stats.TotalWeakWords.Should().Be(0);
        stats.WeakWordPercentage.Should().Be(0.0);
        stats.HasWeakWords.Should().BeFalse();
        stats.Matches.Should().BeEmpty();
        stats.CountByCategory.Should().BeEmpty();
    }

    [Fact]
    public void GetStatistics_NoWeakWords_ReturnsZeroWeakWords()
    {
        var text = "The code compiles and tests pass.";

        var stats = _sut.GetStatistics(text, _defaultProfile);

        stats.TotalWords.Should().Be(6);
        stats.TotalWeakWords.Should().Be(0);
        stats.WeakWordPercentage.Should().Be(0.0);
        stats.HasWeakWords.Should().BeFalse();
    }

    [Fact]
    public void GetStatistics_MixedWeakWords_ReturnsCategoryBreakdown()
    {
        // LOGIC: 'should' is also a weasel word, so avoid it in test data
        var text = "This is very important, perhaps they basically agree.";

        var stats = _sut.GetStatistics(text, _defaultProfile);

        stats.TotalWeakWords.Should().Be(3);
        stats.HasWeakWords.Should().BeTrue();
        stats.CountByCategory.Should().ContainKey(WeakWordCategory.Adverb);
        stats.CountByCategory.Should().ContainKey(WeakWordCategory.WeaselWord);
        stats.CountByCategory.Should().ContainKey(WeakWordCategory.Filler);
        stats.CountByCategory[WeakWordCategory.Adverb].Should().Be(1);
        stats.CountByCategory[WeakWordCategory.WeaselWord].Should().Be(1);
        stats.CountByCategory[WeakWordCategory.Filler].Should().Be(1);
    }

    [Fact]
    public void GetStatistics_CalculatesPercentageCorrectly()
    {
        var text = "very very very very text"; // 5 words, 4 weak

        var stats = _sut.GetStatistics(text, _defaultProfile);

        stats.TotalWords.Should().Be(5);
        stats.TotalWeakWords.Should().Be(4);
        stats.WeakWordPercentage.Should().BeApproximately(80.0, 0.1);
    }

    [Fact]
    public void GetStatistics_NullProfile_ThrowsArgumentNullException()
    {
        var act = () => _sut.GetStatistics("Some text", null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("profile");
    }

    #endregion

    #region GetSuggestion

    [Theory]
    [InlineData("very", "Use a stronger adjective")]
    [InlineData("really", "Consider removing")]
    [InlineData("basically", "Remove—adds no meaning")]
    [InlineData("perhaps", "Commit to the statement")]
    [InlineData("many", "Provide a number")]
    public void GetSuggestion_KnownWord_ReturnsSpecificSuggestion(string word, string expectedSubstring)
    {
        var suggestion = _sut.GetSuggestion(word, WeakWordCategory.Adverb);

        suggestion.Should().Contain(expectedSubstring);
    }

    [Fact]
    public void GetSuggestion_UnknownAdverb_ReturnsGenericAdverbSuggestion()
    {
        var suggestion = _sut.GetSuggestion("unknownword", WeakWordCategory.Adverb);

        suggestion.Should().Contain("stronger verb");
    }

    [Fact]
    public void GetSuggestion_UnknownWeaselWord_ReturnsGenericWeaselSuggestion()
    {
        var suggestion = _sut.GetSuggestion("unknownword", WeakWordCategory.WeaselWord);

        suggestion.Should().Contain("specific");
    }

    [Fact]
    public void GetSuggestion_UnknownFiller_ReturnsGenericFillerSuggestion()
    {
        var suggestion = _sut.GetSuggestion("unknownword", WeakWordCategory.Filler);

        suggestion.Should().Contain("removing");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetSuggestion_EmptyWord_ReturnsGenericSuggestion(string? word)
    {
        var suggestion = _sut.GetSuggestion(word!, WeakWordCategory.Adverb);

        suggestion.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region WeakWordMatch Record

    [Fact]
    public void WeakWordMatch_Length_ReturnsCorrectValue()
    {
        var match = new WeakWordMatch(
            Word: "very",
            Category: WeakWordCategory.Adverb,
            StartIndex: 10,
            EndIndex: 14);

        match.Length.Should().Be(4);
    }

    #endregion

    #region WeakWordStats Record

    [Fact]
    public void WeakWordStats_WeakWordPercentage_ZeroWhenNoWords()
    {
        var stats = new WeakWordStats(
            TotalWords: 0,
            TotalWeakWords: 0,
            CountByCategory: new Dictionary<WeakWordCategory, int>(),
            Matches: Array.Empty<WeakWordMatch>());

        stats.WeakWordPercentage.Should().Be(0.0);
    }

    [Fact]
    public void WeakWordStats_HasWeakWords_TrueWhenWeakWordsExist()
    {
        var stats = new WeakWordStats(
            TotalWords: 10,
            TotalWeakWords: 2,
            CountByCategory: new Dictionary<WeakWordCategory, int> { [WeakWordCategory.Adverb] = 2 },
            Matches: new List<WeakWordMatch>
            {
                new("very", WeakWordCategory.Adverb, 0, 4),
                new("really", WeakWordCategory.Adverb, 5, 11)
            });

        stats.HasWeakWords.Should().BeTrue();
    }

    [Fact]
    public void WeakWordStats_HasWeakWords_FalseWhenNoWeakWords()
    {
        var stats = new WeakWordStats(
            TotalWords: 10,
            TotalWeakWords: 0,
            CountByCategory: new Dictionary<WeakWordCategory, int>(),
            Matches: Array.Empty<WeakWordMatch>());

        stats.HasWeakWords.Should().BeFalse();
    }

    #endregion
}
